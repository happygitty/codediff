using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A4 RID: 164
	internal abstract class OwaEcpProxyRequestHandler<ServiceType> : BEServerCookieProxyRequestHandler<ServiceType> where ServiceType : HttpService
	{
		// Token: 0x17000133 RID: 307
		// (get) Token: 0x0600058C RID: 1420 RVA: 0x0001F04F File Offset: 0x0001D24F
		// (set) Token: 0x0600058D RID: 1421 RVA: 0x0001F057 File Offset: 0x0001D257
		protected bool IsExplicitSignOn { get; set; }

		// Token: 0x17000134 RID: 308
		// (get) Token: 0x0600058E RID: 1422 RVA: 0x0001F060 File Offset: 0x0001D260
		// (set) Token: 0x0600058F RID: 1423 RVA: 0x0001F068 File Offset: 0x0001D268
		protected string ExplicitSignOnAddress { get; set; }

		// Token: 0x17000135 RID: 309
		// (get) Token: 0x06000590 RID: 1424 RVA: 0x0001F071 File Offset: 0x0001D271
		// (set) Token: 0x06000591 RID: 1425 RVA: 0x0001F079 File Offset: 0x0001D279
		protected string ExplicitSignOnDomain { get; set; }

		// Token: 0x17000136 RID: 310
		// (get) Token: 0x06000592 RID: 1426
		protected abstract string ProxyLogonUri { get; }

		// Token: 0x17000137 RID: 311
		// (get) Token: 0x06000593 RID: 1427 RVA: 0x0000500A File Offset: 0x0000320A
		protected virtual string ProxyLogonQueryString
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000138 RID: 312
		// (get) Token: 0x06000594 RID: 1428 RVA: 0x0001F082 File Offset: 0x0001D282
		protected override bool WillAddProtocolSpecificCookiesToServerRequest
		{
			get
			{
				return this.proxyLogonResponseCookies != null;
			}
		}

		// Token: 0x17000139 RID: 313
		// (get) Token: 0x06000595 RID: 1429 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool ImplementsOutOfBandProxyLogon
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700013A RID: 314
		// (get) Token: 0x06000596 RID: 1430 RVA: 0x0001F08D File Offset: 0x0001D28D
		protected override HttpStatusCode StatusCodeSignifyingOutOfBandProxyLogonNeeded
		{
			get
			{
				return (HttpStatusCode)441;
			}
		}

		// Token: 0x06000597 RID: 1431 RVA: 0x0001D147 File Offset: 0x0001B347
		protected override DatacenterRedirectStrategy CreateDatacenterRedirectStrategy()
		{
			return new OwaEcpRedirectStrategy(this);
		}

		// Token: 0x06000598 RID: 1432 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void SetProtocolSpecificProxyLogonRequestParameters(HttpWebRequest request)
		{
		}

		// Token: 0x06000599 RID: 1433 RVA: 0x0001F094 File Offset: 0x0001D294
		protected override void StartOutOfBandProxyLogon(object extraData)
		{
			object lockObject = base.LockObject;
			lock (lockObject)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[OwaEcpProxyRequestHandler::StartOutOfBandProxyLogon]: Context {0}; Remote server returned 441, this means we need to attempt to do a proxy logon", base.TraceContext);
				}
				UriBuilder uriBuilder = new UriBuilder(this.GetTargetBackEndServerUrl());
				uriBuilder.Scheme = Uri.UriSchemeHttps;
				uriBuilder.Path = base.ClientRequest.ApplicationPath + "/" + this.ProxyLogonUri;
				uriBuilder.Query = this.ProxyLogonQueryString;
				base.Logger.AppendGenericInfo("ProxyLogon", uriBuilder.Uri.ToString());
				this.proxyLogonRequest = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
				this.proxyLogonRequest.ServicePoint.ConnectionLimit = HttpProxySettings.ServicePointConnectionLimit.Value;
				this.proxyLogonRequest.Method = "POST";
				base.PrepareServerRequest(this.proxyLogonRequest);
				this.SetProtocolSpecificProxyLogonRequestParameters(this.proxyLogonRequest);
				base.PfdTracer.TraceRequest("ProxyLogonRequest", this.proxyLogonRequest);
				UTF8Encoding utf8Encoding = new UTF8Encoding(true, true);
				this.proxyLogonCSC = utf8Encoding.GetBytes(this.GetSerializedAccessTokenString());
				this.proxyLogonRequest.ContentLength = (long)this.proxyLogonCSC.Length;
				this.proxyLogonRequest.BeginGetRequestStream(new AsyncCallback(OwaEcpProxyRequestHandler<ServiceType>.ProxyLogonRequestStreamReadyCallback), base.ServerAsyncState);
				base.State = ProxyRequestHandler.ProxyState.WaitForProxyLogonRequestStream;
			}
		}

		// Token: 0x0600059A RID: 1434 RVA: 0x0001F224 File Offset: 0x0001D424
		protected bool IsResourceRequest()
		{
			return BEResourceRequestHandler.IsResourceRequest(base.ClientRequest.Url.LocalPath);
		}

		// Token: 0x0600059B RID: 1435 RVA: 0x0001F23C File Offset: 0x0001D43C
		protected override bool ShouldCopyCookieToClientResponse(Cookie cookie)
		{
			if (FbaModule.IsCadataCookie(cookie.Name))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[OwaEcpProxyRequestHandler::ShouldCopyCookieToClientResponse]: Context {0}; Unexpected cadata cookie {1} from BE", base.TraceContext, cookie.Name);
				}
				return false;
			}
			return true;
		}

		// Token: 0x0600059C RID: 1436 RVA: 0x0001F288 File Offset: 0x0001D488
		protected override void CopySupplementalCookiesToClientResponse()
		{
			if (this.proxyLogonResponseCookies != null)
			{
				foreach (object obj in this.proxyLogonResponseCookies)
				{
					Cookie cookie = (Cookie)obj;
					if (FbaModule.IsCadataCookie(cookie.Name))
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[OwaEcpProxyRequestHandler::CopySupplementalCookiesToClientResponse]: Context {0}; Unexpected cadata cookie {1} in proxy logon response from BE", base.TraceContext, cookie.Name);
						}
					}
					else
					{
						base.CopyServerCookieToClientResponse(cookie);
					}
				}
			}
			base.CopySupplementalCookiesToClientResponse();
		}

		// Token: 0x0600059D RID: 1437 RVA: 0x0001F330 File Offset: 0x0001D530
		protected override bool TryHandleProtocolSpecificResponseErrors(WebException e)
		{
			if (e.Status == WebExceptionStatus.ProtocolError && ((HttpWebResponse)e.Response).StatusCode == (HttpStatusCode)442)
			{
				this.RedirectOn442Response();
				return true;
			}
			return base.TryHandleProtocolSpecificResponseErrors(e);
		}

		// Token: 0x0600059E RID: 1438 RVA: 0x0001F361 File Offset: 0x0001D561
		protected override void AddProtocolSpecificCookiesToServerRequest(CookieContainer cookieContainer)
		{
			cookieContainer.Add(this.proxyLogonResponseCookies);
		}

		// Token: 0x0600059F RID: 1439 RVA: 0x0001F370 File Offset: 0x0001D570
		protected override void Cleanup()
		{
			if (this.proxyLogonRequestStream != null)
			{
				this.proxyLogonRequestStream.Flush();
				this.proxyLogonRequestStream.Dispose();
				this.proxyLogonRequestStream = null;
			}
			if (this.proxyLogonResponse != null)
			{
				this.proxyLogonResponse.Close();
				this.proxyLogonResponse = null;
			}
			base.Cleanup();
		}

		// Token: 0x060005A0 RID: 1440 RVA: 0x0001F3C4 File Offset: 0x0001D5C4
		protected override Uri UpdateExternalRedirectUrl(Uri originalRedirectUrl)
		{
			UriBuilder uriBuilder = new UriBuilder(originalRedirectUrl);
			if (!string.IsNullOrEmpty(this.ExplicitSignOnAddress))
			{
				uriBuilder.Path = UrlUtilities.GetPathWithExplictLogonHint(originalRedirectUrl, this.ExplicitSignOnAddress);
			}
			return uriBuilder.Uri;
		}

		// Token: 0x060005A1 RID: 1441 RVA: 0x0001F3FD File Offset: 0x0001D5FD
		protected override bool ShouldExcludeFromExplicitLogonParsing()
		{
			return OwaExplicitLogonParser.OwaShouldExcludeFromExplicitLogonParsing(base.ClientRequest.Url, base.ClientRequest.Headers);
		}

		// Token: 0x060005A2 RID: 1442 RVA: 0x0001F41A File Offset: 0x0001D61A
		protected override bool IsValidExplicitLogonNode(string node, bool nodeIsLast)
		{
			return OwaExplicitLogonParser.OwaIsValidExplicitLogonNode(node, nodeIsLast);
		}

		// Token: 0x060005A3 RID: 1443 RVA: 0x0001F424 File Offset: 0x0001D624
		protected override UriBuilder GetClientUrlForProxy()
		{
			UriBuilder uriBuilder = new UriBuilder(base.ClientRequest.Url.OriginalString);
			if (this.IsExplicitSignOn && !UrlUtilities.IsOwaDownloadRequest(base.ClientRequest.Url))
			{
				uriBuilder.Path = UrlHelper.RemoveExplicitLogonFromUrlAbsolutePath(HttpUtility.UrlDecode(base.ClientRequest.Url.AbsolutePath), HttpUtility.UrlDecode(this.ExplicitSignOnAddress));
			}
			return uriBuilder;
		}

		// Token: 0x060005A4 RID: 1444 RVA: 0x0001F490 File Offset: 0x0001D690
		protected override void RedirectIfNeeded(BackEndServer mailboxServer)
		{
			if (mailboxServer == null)
			{
				throw new ArgumentNullException("mailboxServer");
			}
			if (!Utilities.IsPartnerHostedOnly && !CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).NoCrossSiteRedirect.Enabled)
			{
				ServiceTopology currentServiceTopology = ServiceTopology.GetCurrentServiceTopology("d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\OwaEcpProxyRequestHandler.cs", "RedirectIfNeeded", 438);
				Site targetSite = currentServiceTopology.GetSite(mailboxServer.Fqdn, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\OwaEcpProxyRequestHandler.cs", "RedirectIfNeeded", 439);
				if (!LocalSiteCache.LocalSite.DistinguishedName.Equals(targetSite.DistinguishedName) && (!this.IsLocalRequest(LocalServerCache.LocalServerFqdn) || !this.IsLAMUserAgent(base.ClientRequest.UserAgent)))
				{
					HttpService targetService = currentServiceTopology.FindAny<ServiceType>(0, (ServiceType internalService) => internalService != null && internalService.IsFrontEnd && internalService.Site.DistinguishedName.Equals(targetSite.DistinguishedName), "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\OwaEcpProxyRequestHandler.cs", "RedirectIfNeeded", 451);
					if (!this.ShouldExecuteSSORedirect(targetService))
					{
						HttpService httpService = currentServiceTopology.FindAny<ServiceType>(1, (ServiceType externalService) => externalService != null && externalService.IsFrontEnd && externalService.Site.DistinguishedName.Equals(targetSite.DistinguishedName), "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\OwaEcpProxyRequestHandler.cs", "RedirectIfNeeded", 462);
						if (httpService != null)
						{
							Uri url = httpService.Url;
							if (Uri.Compare(url, base.ClientRequest.Url, UriComponents.Host, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) != 0)
							{
								UriBuilder uriBuilder = new UriBuilder(base.ClientRequest.Url);
								uriBuilder.Host = url.Host;
								if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
								{
									ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[OwaEcpProxyRequestHandler::RedirectIfNeeded]: Stop processing and redirect to {0}.", uriBuilder.Uri.AbsoluteUri);
								}
								throw new HttpException(302, this.GetCrossSiteRedirectUrl(targetSite.DistinguishedName, uriBuilder.Path, uriBuilder.Query));
							}
						}
					}
				}
			}
		}

		// Token: 0x060005A5 RID: 1445 RVA: 0x0001F648 File Offset: 0x0001D848
		private static void ProxyLogonRequestStreamReadyCallback(IAsyncResult result)
		{
			OwaEcpProxyRequestHandler<ServiceType> owaEcpProxyRequestHandler = AsyncStateHolder.Unwrap<OwaEcpProxyRequestHandler<ServiceType>>(result);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)owaEcpProxyRequestHandler.GetHashCode(), "[OwaEcpProxyRequestHandler::ProxyLogonRequestStreamReadyCallback]: Context {0}", owaEcpProxyRequestHandler.TraceContext);
			}
			if (result.CompletedSynchronously)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(owaEcpProxyRequestHandler.OnProxyLogonRequestStreamReady), result);
				return;
			}
			owaEcpProxyRequestHandler.OnProxyLogonRequestStreamReady(result);
		}

		// Token: 0x060005A6 RID: 1446 RVA: 0x0001F6A8 File Offset: 0x0001D8A8
		private static void ProxyLogonResponseReadyCallback(IAsyncResult result)
		{
			OwaEcpProxyRequestHandler<ServiceType> owaEcpProxyRequestHandler = AsyncStateHolder.Unwrap<OwaEcpProxyRequestHandler<ServiceType>>(result);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)owaEcpProxyRequestHandler.GetHashCode(), "[OwaEcpProxyRequestHandler::ProxyLogonResponseReadyCallback]: Context {0}", owaEcpProxyRequestHandler.TraceContext);
			}
			if (result.CompletedSynchronously)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(owaEcpProxyRequestHandler.OnProxyLogonResponseReady), result);
				return;
			}
			owaEcpProxyRequestHandler.OnProxyLogonResponseReady(result);
		}

		// Token: 0x060005A7 RID: 1447 RVA: 0x0001F708 File Offset: 0x0001D908
		private string GetCrossSiteRedirectUrl(string targetSiteDistinguishedName, string path, string query)
		{
			NameValueCollection nameValueCollection = new NameValueCollection();
			nameValueCollection.Add("redirectType", 0.ToString());
			nameValueCollection.Add("targetSiteDistinguishedName", targetSiteDistinguishedName);
			if (!string.IsNullOrEmpty(path))
			{
				nameValueCollection.Add("path", path);
			}
			if (!string.IsNullOrEmpty(query))
			{
				nameValueCollection.Add("query", query);
			}
			return AspNetHelper.GetCafeErrorPageRedirectUrl(base.HttpContext, nameValueCollection);
		}

		// Token: 0x060005A8 RID: 1448 RVA: 0x0001F775 File Offset: 0x0001D975
		private bool ShouldExecuteSSORedirect(HttpService targetService)
		{
			return !FbaFormPostProxyRequestHandler.DisableSSORedirects && (VdirConfiguration.Instance.InternalAuthenticationMethod & 4) == 4 && (targetService == null || (targetService.AuthenticationMethod & 4) == 4);
		}

		// Token: 0x060005A9 RID: 1449 RVA: 0x0001F7A4 File Offset: 0x0001D9A4
		private void OnProxyLogonRequestStreamReady(object extraData)
		{
			base.CallThreadEntranceMethod(delegate
			{
				IAsyncResult asyncResult = extraData as IAsyncResult;
				object lockObject = this.LockObject;
				lock (lockObject)
				{
					try
					{
						this.proxyLogonRequestStream = this.proxyLogonRequest.EndGetRequestStream(asyncResult);
						this.proxyLogonRequestStream.Write(this.proxyLogonCSC, 0, this.proxyLogonCSC.Length);
						this.proxyLogonRequestStream.Flush();
						this.proxyLogonRequestStream.Dispose();
						this.proxyLogonRequestStream = null;
						try
						{
							GuardedProxyExecution.Default.Increment(this.AnchoredRoutingTarget.BackEndServer, Extensions.GetGenericInfoLogDelegate(this.Logger));
							this.proxyLogonRequest.BeginGetResponse(new AsyncCallback(OwaEcpProxyRequestHandler<ServiceType>.ProxyLogonResponseReadyCallback), this.ServerAsyncState);
							this.State = ProxyRequestHandler.ProxyState.WaitForProxyLogonResponse;
						}
						catch (Exception)
						{
							GuardedProxyExecution.Default.Decrement(this.AnchoredRoutingTarget.BackEndServer);
							throw;
						}
					}
					catch (WebException ex)
					{
						this.CompleteWithError(ex, "[OwaEcpProxyRequestHandler::OnProxyLogonRequestStreamReady]");
					}
					catch (HttpException ex2)
					{
						this.CompleteWithError(ex2, "[OwaEcpProxyRequestHandler::OnProxyLogonRequestStreamReady]");
					}
					catch (HttpProxyException ex3)
					{
						this.CompleteWithError(ex3, "[OwaEcpProxyRequestHandler::OnProxyLogonRequestStreamReady]");
					}
					catch (IOException ex4)
					{
						this.CompleteWithError(ex4, "[OwaEcpProxyRequestHandler::OnProxyLogonRequestStreamReady]");
					}
				}
			});
		}

		// Token: 0x060005AA RID: 1450 RVA: 0x0001F7D8 File Offset: 0x0001D9D8
		private void OnProxyLogonResponseReady(object extraData)
		{
			base.CallThreadEntranceMethod(delegate
			{
				IAsyncResult asyncResult = extraData as IAsyncResult;
				object lockObject = this.LockObject;
				lock (lockObject)
				{
					try
					{
						GuardedProxyExecution.Default.Decrement(this.AnchoredRoutingTarget.BackEndServer);
						this.proxyLogonResponse = (HttpWebResponse)this.proxyLogonRequest.EndGetResponse(asyncResult);
						this.PfdTracer.TraceResponse("ProxyLogonResponse", this.proxyLogonResponse);
						this.proxyLogonResponseCookies = this.proxyLogonResponse.Cookies;
						this.proxyLogonResponse.Close();
						this.proxyLogonResponse = null;
						UserContextCookie userContextCookie = this.TryGetUserContextFromProxyLogonResponse();
						if (userContextCookie != null && userContextCookie.MailboxUniqueKey != null)
						{
							string mailboxUniqueKey = userContextCookie.MailboxUniqueKey;
							if (SmtpAddress.IsValidSmtpAddress(mailboxUniqueKey))
							{
								AnchorMailbox anchorMailbox = new SmtpAnchorMailbox(ProxyAddress.Parse("SMTP", mailboxUniqueKey).AddressString, this);
								this.AnchoredRoutingTarget = new AnchoredRoutingTarget(anchorMailbox, this.AnchoredRoutingTarget.BackEndServer);
							}
							else
							{
								try
								{
									AnchorMailbox anchorMailbox2 = new SidAnchorMailbox(new SecurityIdentifier(mailboxUniqueKey), this);
									this.AnchoredRoutingTarget = new AnchoredRoutingTarget(anchorMailbox2, this.AnchoredRoutingTarget.BackEndServer);
								}
								catch (ArgumentException)
								{
								}
							}
						}
						ThreadPool.QueueUserWorkItem(new WaitCallback(this.BeginProxyRequest));
						this.State = ProxyRequestHandler.ProxyState.PrepareServerRequest;
					}
					catch (WebException ex)
					{
						if (ex.Status == WebExceptionStatus.ProtocolError && ((HttpWebResponse)ex.Response).StatusCode == (HttpStatusCode)442)
						{
							this.RedirectOn442Response();
						}
						else
						{
							this.CompleteWithError(ex, "[OwaEcpProxyRequestHandler::OnProxyLogonResponseReady]");
						}
					}
					catch (HttpException ex2)
					{
						this.CompleteWithError(ex2, "[OwaEcpProxyRequestHandler::OnProxyLogonResponseReady]");
					}
					catch (IOException ex3)
					{
						this.CompleteWithError(ex3, "[OwaEcpProxyRequestHandler::OnProxyLogonResponseReady]");
					}
					catch (HttpProxyException ex4)
					{
						this.CompleteWithError(ex4, "[OwaEcpProxyRequestHandler::OnProxyLogonResponseReady]");
					}
				}
			});
		}

		// Token: 0x060005AB RID: 1451 RVA: 0x0001F80C File Offset: 0x0001DA0C
		private UserContextCookie TryGetUserContextFromProxyLogonResponse()
		{
			foreach (object obj in this.proxyLogonResponseCookies)
			{
				Cookie cookie = (Cookie)obj;
				if (cookie.Name.StartsWith("UserContext"))
				{
					return UserContextCookie.TryCreateFromNetCookie(cookie);
				}
			}
			return null;
		}

		// Token: 0x060005AC RID: 1452 RVA: 0x0001F87C File Offset: 0x0001DA7C
		private void RedirectOn442Response()
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[OwaEcpProxyRequestHandler::RedirectOn442Response]: Context {0}; The proxy returned 442, this means the user's language or timezone are invalid", base.TraceContext);
			}
			string text = OwaUrl.LanguagePage.GetExplicitUrl(base.ClientRequest).ToString();
			base.PfdTracer.TraceRedirect("EcpOwa442NeedLanguage", text);
			base.ClientResponse.Redirect(text, false);
			base.CompleteForRedirect(text);
		}

		// Token: 0x060005AD RID: 1453 RVA: 0x0001F8F0 File Offset: 0x0001DAF0
		private bool IsLocalRequest(string machineFqdn)
		{
			string host = base.ClientRequest.Url.Host;
			return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) || host.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) || host.Equals(machineFqdn, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060005AE RID: 1454 RVA: 0x0001F945 File Offset: 0x0001DB45
		private bool IsLAMUserAgent(string requestUserAgent)
		{
			return requestUserAgent.Equals("AMPROBE/LOCAL/CLIENTACCESS", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x0400037B RID: 891
		private const int HttpStatusNeedIdentity = 441;

		// Token: 0x0400037C RID: 892
		private const int HttpStatusNeedLanguage = 442;

		// Token: 0x0400037D RID: 893
		private HttpWebRequest proxyLogonRequest;

		// Token: 0x0400037E RID: 894
		private Stream proxyLogonRequestStream;

		// Token: 0x0400037F RID: 895
		private HttpWebResponse proxyLogonResponse;

		// Token: 0x04000380 RID: 896
		private CookieCollection proxyLogonResponseCookies;

		// Token: 0x04000381 RID: 897
		private byte[] proxyLogonCSC;
	}
}
