using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.Rpc;
using Microsoft.Exchange.Security.Authentication;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B4 RID: 180
	internal class RpcHttpProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x06000707 RID: 1799 RVA: 0x0001ABF5 File Offset: 0x00018DF5
		internal RpcHttpProxyRequestHandler()
		{
		}

		// Token: 0x1700017A RID: 378
		// (get) Token: 0x06000708 RID: 1800 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool ShouldForceUnbufferedClientResponseOutput
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700017B RID: 379
		// (get) Token: 0x06000709 RID: 1801 RVA: 0x0002971C File Offset: 0x0002791C
		protected override bool IsBackendServerCacheValidationEnabled
		{
			get
			{
				return base.ClientRequest != null && ((RpcHttpProxyRequestHandler.RpcHttpHeadRequestEnabled.Value && base.ClientRequest.HttpMethod == "RPC_IN_DATA") || (RpcHttpProxyRequestHandler.RpcOutHeadRequestEnabled.Value && base.ClientRequest.HttpMethod == "RPC_OUT_DATA"));
			}
		}

		// Token: 0x1700017C RID: 380
		// (get) Token: 0x0600070A RID: 1802 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool UseSmartBufferSizing
		{
			get
			{
				return true;
			}
		}

		// Token: 0x0600070B RID: 1803 RVA: 0x0002977C File Offset: 0x0002797C
		protected override void BeginValidateBackendServerCache()
		{
			Exception ex = null;
			try
			{
				Uri targetBackEndServerUrl = this.GetTargetBackEndServerUrl();
				this.headRequest = base.CreateServerRequest(targetBackEndServerUrl);
				this.headRequest.Method = "HEAD";
				this.headRequest.Timeout = RpcHttpProxyRequestHandler.RpcHttpHeadRequestTimeout.Value;
				this.headRequest.KeepAlive = HttpProxySettings.KeepAliveOutboundConnectionsEnabled.Value;
				this.headRequest.ContentLength = 0L;
				this.headRequest.SendChunked = false;
				this.headRequest.BeginGetResponse(new AsyncCallback(RpcHttpProxyRequestHandler.ValidateBackendServerCacheCallback), base.ServerAsyncState);
				base.Logger.LogCurrentTime("H-BeginGetResponse");
			}
			catch (WebException ex)
			{
			}
			catch (HttpException ex)
			{
			}
			catch (IOException ex)
			{
			}
			catch (SocketException ex)
			{
			}
			if (ex != null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<Exception, int, ProxyRequestHandler.ProxyState>((long)this.GetHashCode(), "[ProxyRequestHandler::BeginValidateBackendServerCache]: An error occurred while trying to send head request: {0}; Context {1}; State {2}", ex, base.TraceContext, base.State);
				}
				this.headRequest = null;
				base.BeginProxyRequestOrRecalculate();
			}
		}

		// Token: 0x0600070C RID: 1804 RVA: 0x000298A4 File Offset: 0x00027AA4
		protected override StreamProxy BuildResponseStreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, byte[] buffer)
		{
			return this.BuildResponseStream(() => new RpcHttpOutDataResponseStreamProxy(streamProxyType, source, target, buffer, this), () => this.<>n__0(streamProxyType, source, target, buffer));
		}

		// Token: 0x0600070D RID: 1805 RVA: 0x000298FC File Offset: 0x00027AFC
		protected override StreamProxy BuildResponseStreamProxySmartSizing(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target)
		{
			return this.BuildResponseStream(() => new RpcHttpOutDataResponseStreamProxy(streamProxyType, source, target, HttpProxySettings.ResponseBufferSize.Value, HttpProxySettings.MinimumResponseBufferSize.Value, this), () => this.<>n__1(streamProxyType, source, target));
		}

		// Token: 0x0600070E RID: 1806 RVA: 0x00029949 File Offset: 0x00027B49
		protected override void DoProtocolSpecificBeginProcess()
		{
			if (base.ClientRequest.HttpMethod.Equals("RPC_IN_DATA"))
			{
				base.ParseClientRequest<bool>(new Func<Stream, bool>(this.ParseOutAssociationGuid), 104);
			}
		}

		// Token: 0x0600070F RID: 1807 RVA: 0x00029978 File Offset: 0x00027B78
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			UriBuilder uriBuilder = new UriBuilder(base.ClientRequest.Url);
			if (string.IsNullOrEmpty(base.ClientRequest.Url.Query))
			{
				throw new HttpProxyException(HttpStatusCode.NotFound, 3001, "No proxy destination is specified!");
			}
			RpcHttpQueryString rpcHttpQueryString = new RpcHttpQueryString(uriBuilder.Query);
			this.rpcServerTarget = rpcHttpQueryString.RcaServer;
			if (SmtpAddress.IsValidSmtpAddress(this.rpcServerTarget))
			{
				string text;
				HttpContextItemParser.TryGetLiveIdMemberName(base.HttpContext.Items, ref text);
				Guid guid;
				string domain;
				string text2;
				if (RequestQueryStringParser.TryGetMailboxGuid(this.rpcServerTarget, text, ref guid, ref domain, ref text2))
				{
					MailboxGuidAnchorMailbox result;
					if (!string.IsNullOrEmpty(text2))
					{
						this.rpcServerTarget = ExchangeRpcClientAccess.CreatePersonalizedServer(guid, text2);
						base.Logger.AppendString(3, "MailboxGuidWithDomain-ChangedToUserDomain");
						result = new MailboxGuidAnchorMailbox(guid, text2, this);
					}
					else
					{
						base.Logger.AppendString(3, "MailboxGuidWithDomain");
						result = new MailboxGuidAnchorMailbox(guid, domain, this);
					}
					this.updateRpcServer = true;
					return result;
				}
				return this.ResolveToDefaultAnchorMailbox(this.rpcServerTarget, "InvalidFormat");
			}
			else
			{
				ProxyDestination proxyDestination;
				if (RpcHttpProxyRules.Instance.TryGetProxyDestination(this.rpcServerTarget, out proxyDestination))
				{
					string text3 = proxyDestination.GetHostName(this.GetKeyForCasAffinity());
					if (proxyDestination.IsDynamicTarget)
					{
						try
						{
							text3 = DownLevelServerManager.Instance.GetDownLevelClientAccessServerWithPreferredServer<RpcHttpService>(new ServerInfoAnchorMailbox(text3, this), text3, 1, base.Logger, proxyDestination.Version).Fqdn;
						}
						catch (NoAvailableDownLevelBackEndException)
						{
							throw new HttpProxyException(HttpStatusCode.NotFound, 3001, string.Format("Cannot find a healthy E12 or E14 CAS to proxy to: {0}", this.rpcServerTarget));
						}
					}
					uriBuilder.Host = text3;
					uriBuilder.Port = proxyDestination.Port;
					uriBuilder.Scheme = Uri.UriSchemeHttps;
					base.Logger.Set(3, "RpcHttpProxyRules");
					this.updateRpcServer = false;
					return new UrlAnchorMailbox(uriBuilder.Uri, this);
				}
				return this.ResolveToDefaultAnchorMailbox(this.rpcServerTarget, "UnknownServerName");
			}
		}

		// Token: 0x06000710 RID: 1808 RVA: 0x00029B70 File Offset: 0x00027D70
		protected override Uri GetTargetBackEndServerUrl()
		{
			UriBuilder uriBuilder = new UriBuilder(base.GetTargetBackEndServerUrl());
			if (this.updateRpcServer)
			{
				RpcHttpQueryString rpcHttpQueryString = new RpcHttpQueryString(uriBuilder.Query);
				if (string.IsNullOrEmpty(rpcHttpQueryString.RcaServerPort))
				{
					uriBuilder.Query = uriBuilder.Host + rpcHttpQueryString.AdditionalParameters;
				}
				else
				{
					uriBuilder.Query = uriBuilder.Host + ":" + rpcHttpQueryString.RcaServerPort + rpcHttpQueryString.AdditionalParameters;
				}
			}
			return uriBuilder.Uri;
		}

		// Token: 0x06000711 RID: 1809 RVA: 0x00029BEB File Offset: 0x00027DEB
		protected override void SetProtocolSpecificServerRequestParameters(HttpWebRequest serverRequest)
		{
			serverRequest.AllowWriteStreamBuffering = false;
		}

		// Token: 0x06000712 RID: 1810 RVA: 0x00029BF4 File Offset: 0x00027DF4
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return !RpcHttpProxyRequestHandler.ProtectedHeaderNames.Contains(headerName, StringComparer.OrdinalIgnoreCase) && base.ShouldCopyHeaderToServerRequest(headerName);
		}

		// Token: 0x06000713 RID: 1811 RVA: 0x00003165 File Offset: 0x00001365
		protected override bool ShouldLogClientDisconnectError(Exception ex)
		{
			return false;
		}

		// Token: 0x06000714 RID: 1812 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected override void DoProtocolSpecificBeginRequestLogging()
		{
		}

		// Token: 0x06000715 RID: 1813 RVA: 0x00029C14 File Offset: 0x00027E14
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			headers["X-RpcHttpProxyLogonUserName"] = EncodingUtilities.EncodeToBase64(IIdentityExtensions.GetSafeName(base.HttpContext.User.Identity, true));
			headers["X-RpcHttpProxyServerTarget"] = this.rpcServerTarget;
			if (this.associationGuid != Guid.Empty)
			{
				headers["X-AssociationGuid"] = this.associationGuid.ToString();
			}
			DatabaseBasedAnchorMailbox databaseBasedAnchorMailbox = base.AnchoredRoutingTarget.AnchorMailbox as DatabaseBasedAnchorMailbox;
			if (databaseBasedAnchorMailbox != null)
			{
				ADObjectId database = databaseBasedAnchorMailbox.GetDatabase();
				if (database != null)
				{
					headers["X-DatabaseGuid"] = database.ObjectGuid.ToString();
				}
			}
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x06000716 RID: 1814 RVA: 0x00029CCC File Offset: 0x00027ECC
		private static void ValidateBackendServerCacheCallback(IAsyncResult result)
		{
			RpcHttpProxyRequestHandler rpcHttpProxyRequestHandler = AsyncStateHolder.Unwrap<RpcHttpProxyRequestHandler>(result);
			if (result.CompletedSynchronously)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(rpcHttpProxyRequestHandler.OnValidateBackendServerCacheCompleted), result);
				return;
			}
			rpcHttpProxyRequestHandler.OnValidateBackendServerCacheCompleted(result);
		}

		// Token: 0x06000717 RID: 1815 RVA: 0x00029D04 File Offset: 0x00027F04
		private void OnValidateBackendServerCacheCompleted(object extraData)
		{
			base.CallThreadEntranceMethod(delegate
			{
				IAsyncResult asyncResult = extraData as IAsyncResult;
				HttpWebResponse httpWebResponse = null;
				Exception ex = null;
				try
				{
					this.Logger.LogCurrentTime("H-OnResponseReady");
					httpWebResponse = (HttpWebResponse)this.headRequest.EndGetResponse(asyncResult);
					this.ThrowWebExceptionForRetryOnErrorTest(httpWebResponse, new int[]
					{
						0,
						1,
						2
					});
				}
				catch (WebException ex)
				{
				}
				catch (HttpException ex)
				{
				}
				catch (IOException ex)
				{
				}
				catch (SocketException ex)
				{
				}
				finally
				{
					this.Logger.LogCurrentTime("H-EndGetResponse");
					if (httpWebResponse != null)
					{
						httpWebResponse.Close();
					}
					this.headRequest = null;
				}
				if (ex != null && ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<Exception, int, ProxyRequestHandler.ProxyState>((long)this.GetHashCode(), "[ProxyRequestHandler::OnValidateBackendServerCacheCompleted]: Head response error: {0}; Context {1}; State {2}", ex, this.TraceContext, this.State);
				}
				WebException ex2 = ex as WebException;
				bool flag = true;
				if (ex2 != null)
				{
					this.LogWebException(ex2);
					if (this.HandleWebExceptionConnectivityError(ex2))
					{
						flag = false;
					}
				}
				if (flag && this.ShouldRecalculateBackendOnHead(ex2, httpWebResponse) && this.RecalculateTargetBackend())
				{
					flag = false;
				}
				if (flag)
				{
					this.BeginProxyRequestOrRecalculate();
				}
			});
		}

		// Token: 0x06000718 RID: 1816 RVA: 0x00029D37 File Offset: 0x00027F37
		private StreamProxy BuildResponseStream(Func<StreamProxy> outDataResponseStreamFactory, Func<StreamProxy> defaultResponseStreamFactory)
		{
			if (base.ClientRequest.HttpMethod.Equals("RPC_OUT_DATA") && base.ClientRequest.ContentLength == 76)
			{
				return outDataResponseStreamFactory();
			}
			return defaultResponseStreamFactory();
		}

		// Token: 0x06000719 RID: 1817 RVA: 0x00029D6C File Offset: 0x00027F6C
		private bool ShouldRecalculateBackendOnHead(WebException webException, HttpWebResponse headResponse)
		{
			if (webException != null && webException.Response != null)
			{
				return this.AuthenticationChallengeReturned(webException) || this.RoutingErrorProcessed((HttpWebResponse)webException.Response);
			}
			return headResponse != null && this.RoutingErrorProcessed(headResponse);
		}

		// Token: 0x0600071A RID: 1818 RVA: 0x00029DA4 File Offset: 0x00027FA4
		private bool AuthenticationChallengeReturned(WebException webException)
		{
			bool flag;
			return base.AuthBehavior.AuthState != AuthState.BackEndFullAuth && base.IsAuthenticationChallengeFromBackend(webException) && base.TryFindKerberosChallenge(webException.Response.Headers[Constants.AuthenticationHeader], out flag);
		}

		// Token: 0x0600071B RID: 1819 RVA: 0x00029DE8 File Offset: 0x00027FE8
		private bool RoutingErrorProcessed(HttpWebResponse response)
		{
			bool flag = base.HandleRoutingError(response, false);
			bool flag2 = base.ProcessRoutingUpdateModuleResponse(response);
			return flag || flag2;
		}

		// Token: 0x0600071C RID: 1820 RVA: 0x00029E08 File Offset: 0x00028008
		private bool ParseOutAssociationGuid(Stream stream)
		{
			byte[] array = new byte[104];
			stream.Read(array, 0, 20);
			if (array[2] == 20 && array[8] == 104 && array[18] == 6)
			{
				stream.Read(array, 20, 84);
				byte[] array2 = new byte[16];
				Array.Copy(array, 88, array2, 0, 16);
				this.associationGuid = new Guid(array2);
			}
			return true;
		}

		// Token: 0x0600071D RID: 1821 RVA: 0x00029E6C File Offset: 0x0002806C
		private int GetKeyForCasAffinity()
		{
			IIdentity identity = base.HttpContext.User.Identity;
			if (identity is WindowsIdentity || identity is ClientSecurityContextIdentity)
			{
				return IIdentityExtensions.GetSecurityIdentifier(identity).GetHashCode();
			}
			return identity.Name.GetHashCode();
		}

		// Token: 0x0600071E RID: 1822 RVA: 0x00029EB4 File Offset: 0x000280B4
		private AnchorMailbox ResolveToDefaultAnchorMailbox(string originalRpcServerName, string reason)
		{
			string text;
			if (HttpContextItemParser.TryGetLiveIdMemberName(base.HttpContext.Items, ref text))
			{
				AnchorMailbox anchorMailbox = base.ResolveAnchorMailbox();
				if (anchorMailbox != null)
				{
					base.Logger.AppendString(3, "-" + reason);
					if (ExTraceGlobals.BriefTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.BriefTracer.TraceError((long)this.GetHashCode(), "[RpcHttpProxyRequestHandler::ResolveToDefaultAnchorMailbox]: Invalid explicit RPC server name from client: {0}; Defaulting to authenticated user {1} for routing; Context {2}; State {3}", new object[]
						{
							originalRpcServerName,
							text,
							base.TraceContext,
							base.State
						});
					}
					this.rpcServerTarget = text;
					return anchorMailbox;
				}
			}
			throw new HttpProxyException(HttpStatusCode.NotFound, 3003, string.Format("RPC server name passed in by client could not be resolved: {0}", originalRpcServerName));
		}

		// Token: 0x040003EE RID: 1006
		internal const string PreAuthRequestHeaderValue = "true";

		// Token: 0x040003EF RID: 1007
		private const int LookAheadBufferSize = 104;

		// Token: 0x040003F0 RID: 1008
		private static readonly string[] ProtectedHeaderNames = new string[]
		{
			"X-RpcHttpProxyServerTarget",
			"X-AssociationGuid",
			"X-DatabaseGuid"
		};

		// Token: 0x040003F1 RID: 1009
		private static readonly IntAppSettingsEntry RpcHttpHeadRequestTimeout = new IntAppSettingsEntry(HttpProxySettings.Prefix("RpcHttpHeadRequestTimeout"), 5000, ExTraceGlobals.VerboseTracer);

		// Token: 0x040003F2 RID: 1010
		private static readonly BoolAppSettingsEntry RpcHttpHeadRequestEnabled = new BoolAppSettingsEntry(HttpProxySettings.Prefix("RpcHttpHeadRequestEnabled"), false, ExTraceGlobals.VerboseTracer);

		// Token: 0x040003F3 RID: 1011
		private static readonly BoolAppSettingsEntry RpcOutHeadRequestEnabled = new BoolAppSettingsEntry(HttpProxySettings.Prefix("RpcOutHeadRequestEnabled"), false, ExTraceGlobals.VerboseTracer);

		// Token: 0x040003F4 RID: 1012
		private HttpWebRequest headRequest;

		// Token: 0x040003F5 RID: 1013
		private string rpcServerTarget;

		// Token: 0x040003F6 RID: 1014
		private Guid associationGuid;

		// Token: 0x040003F7 RID: 1015
		private bool updateRpcServer;
	}
}
