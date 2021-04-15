using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Management;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.CmdletInfra;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.Security.Authorization;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B1 RID: 177
	internal class RemotePowerShellProxyRequestHandler : BEServerCookieProxyRequestHandler<WebServicesService>
	{
		// Token: 0x17000176 RID: 374
		// (get) Token: 0x060006E9 RID: 1769 RVA: 0x0001981A File Offset: 0x00017A1A
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x17000177 RID: 375
		// (get) Token: 0x060006EA RID: 1770 RVA: 0x0002878D File Offset: 0x0002698D
		protected override string[] BackEndCookieNames
		{
			get
			{
				return RemotePowerShellProxyRequestHandler.ClientSupportedBackEndCookieNames;
			}
		}

		// Token: 0x17000178 RID: 376
		// (get) Token: 0x060006EB RID: 1771 RVA: 0x00003193 File Offset: 0x00001393
		protected override int MaxBackEndCookieEntries
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x060006EC RID: 1772 RVA: 0x00028794 File Offset: 0x00026994
		protected override void OnInitializingHandler()
		{
			if (!string.IsNullOrEmpty(HttpUtility.ParseQueryString(base.HttpContext.Request.Url.Query.Replace(';', '&'))["DelegatedOrg"]))
			{
				this.isSyndicatedAdmin = true;
				return;
			}
			base.OnInitializingHandler();
		}

		// Token: 0x060006ED RID: 1773 RVA: 0x000287E3 File Offset: 0x000269E3
		protected override void ResetForRetryOnError()
		{
			if (base.ClientResponse != null && base.ClientResponse.Headers != null)
			{
				WinRMInfo.ClearFailureCategoryInfo(base.ClientResponse.Headers);
			}
			base.ResetForRetryOnError();
		}

		// Token: 0x060006EE RID: 1774 RVA: 0x00028810 File Offset: 0x00026A10
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			if (base.ClientRequest.IsAuthenticated && base.ProxyToDownLevel)
			{
				IIdentity callerIdentity = this.GetCallerIdentity();
				WindowsIdentity windowsIdentity = callerIdentity as WindowsIdentity;
				GenericSidIdentity genericSidIdentity = callerIdentity as GenericSidIdentity;
				IPrincipal user = base.HttpContext.User;
				if (windowsIdentity != null)
				{
					string text;
					if (HttpContextItemParser.TryGetLiveIdMemberName(base.HttpContext.Items, ref text))
					{
						headers["X-RemotePS-GenericIdentity"] = windowsIdentity.User.ToString();
					}
					else
					{
						headers["X-RemotePS-WindowsIdentity"] = this.GetSerializedAccessTokenString();
					}
				}
				else if (genericSidIdentity != null)
				{
					headers["X-RemotePS-GenericIdentity"] = genericSidIdentity.Sid.ToString();
				}
				else
				{
					headers["X-RemotePS-GenericIdentity"] = IIdentityExtensions.GetSafeName(base.HttpContext.User.Identity, true);
				}
			}
			if (this.isSyndicatedAdminManageDownLevelTarget)
			{
				headers["msExchCafeForceRouteToLogonAccount"] = "1";
			}
			if (LoggerHelper.IsProbePingRequest(base.ClientRequest))
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(base.Logger, 21, "ProbePingBackend");
			}
			else if (WinRMHelper.WinRMParserEnabled.Value)
			{
				try
				{
					this.winRMInfo = base.ParseClientRequest<WinRMInfo>(new Func<Stream, WinRMInfo>(this.ParseWinRMInfo), 10000);
				}
				catch (InvalidOperationException ex)
				{
					if (ExTraceGlobals.ExceptionTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.ExceptionTracer.TraceError<InvalidOperationException>((long)this.GetHashCode(), "[RemotePowerShellProxyRequestHandler::AddProtocolSpecificHeadersToServerRequest] ParseClientRequest throws exception {0}", ex);
					}
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericError(base.Logger, "ParseClientRequestException", ex.ToString());
				}
				if (this.winRMInfo != null)
				{
					WinRMInfo.StampToHttpHeaders(this.winRMInfo, headers);
				}
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
			if (!base.ShouldRetryOnError)
			{
				headers["X-Cafe-Last-Retry"] = "Y";
			}
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x060006EF RID: 1775 RVA: 0x00028A04 File Offset: 0x00026C04
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return !string.Equals(headerName, "X-RemotePS-GenericIdentity", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "X-RemotePS-WindowsIdentity", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "msExchCafeForceRouteToLogonAccount", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "X-DatabaseGuid", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "X-Cafe-Last-Retry", StringComparison.OrdinalIgnoreCase) && !WinRMInfo.IsHeaderReserverd(headerName) && base.ShouldCopyHeaderToServerRequest(headerName);
		}

		// Token: 0x060006F0 RID: 1776 RVA: 0x00028A6C File Offset: 0x00026C6C
		protected override Uri GetTargetBackEndServerUrl()
		{
			Uri targetBackEndServerUrl = base.GetTargetBackEndServerUrl();
			if (targetBackEndServerUrl.Port == 444)
			{
				return targetBackEndServerUrl;
			}
			string absolutePath = targetBackEndServerUrl.AbsolutePath;
			if (string.IsNullOrEmpty(absolutePath))
			{
				throw new HttpProxyException(HttpStatusCode.InternalServerError, 3001, string.Format("Unable to process URL: " + targetBackEndServerUrl.ToString(), Array.Empty<object>()));
			}
			UriBuilder uriBuilder = new UriBuilder(targetBackEndServerUrl);
			int num = absolutePath.IndexOf('/', 1);
			if (num > 1)
			{
				uriBuilder.Path = absolutePath.Substring(0, num) + "-proxy" + absolutePath.Substring(num);
			}
			else
			{
				uriBuilder.Path = absolutePath + "-proxy";
			}
			return uriBuilder.Uri;
		}

		// Token: 0x060006F1 RID: 1777 RVA: 0x00028B18 File Offset: 0x00026D18
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(base.HttpContext.Request.Url.Query.Replace(';', '&'));
			string text = nameValueCollection["TargetServer"];
			if (!string.IsNullOrEmpty(text))
			{
				base.Logger.Set(3, "TargetServer");
				return new ServerInfoAnchorMailbox(text, this);
			}
			if (CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).UseMailboxHintForRPSRouting.Enabled)
			{
				string text2 = nameValueCollection["Email"];
				if (!string.IsNullOrEmpty(text2))
				{
					if (!SmtpAddress.IsValidSmtpAddress(text2))
					{
						throw new HttpProxyException(HttpStatusCode.NotFound, 3002, string.Format("Invalid email address {0} for routing hint.", text2));
					}
					base.Logger.Set(3, "Email");
					return new SmtpAnchorMailbox(text2, this);
				}
			}
			string text3 = nameValueCollection["ExchClientVer"];
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text3 = Utilities.NormalizeExchClientVer(text3);
			}
			if (CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).UseExchClientVerInRPS.Enabled)
			{
				base.Logger.Set(3, "ExchClientVer");
				return base.GetServerVersionAnchorMailbox(text3);
			}
			bool flag;
			string text4;
			string routingBasedOrganization = this.GetRoutingBasedOrganization(nameValueCollection, out flag, out text4);
			if (!this.isSyndicatedAdmin && !string.IsNullOrWhiteSpace(text3))
			{
				if (!string.IsNullOrWhiteSpace(routingBasedOrganization))
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[RemotePowerShellProxyRequestHandler::ResolveAnchorMailbox]: Datacenter, Version parameter {0}, from {1}, organization {2}, context {3}.", new object[]
						{
							text3,
							text4,
							routingBasedOrganization,
							base.TraceContext
						});
					}
					base.Logger.Set(3, text4 + "-" + text3);
					return VersionedDomainAnchorMailbox.GetAnchorMailbox(routingBasedOrganization, text3, this);
				}
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string, int>((long)this.GetHashCode(), "[RemotePowerShellProxyRequestHandler::ResolveAnchorMailbox]: ExchClientVer {0} is specified, but User-Org/Org anization/DelegatedOrg is null. Go with normal routing. Context {2}.", text3, base.TraceContext);
				}
			}
			string text5 = nameValueCollection["DelegatedUser"];
			if (!string.IsNullOrEmpty(text5))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string, int>((long)this.GetHashCode(), "[RemotePowerShellProxyRequestHandler::ResolveAnchorMailbox]: User hint {0}, context {1}.", text5, base.TraceContext);
				}
				if (!string.IsNullOrEmpty(text5) && SmtpAddress.IsValidSmtpAddress(text5))
				{
					base.Logger.Set(3, "DelegatedUser-SMTP-UrlQuery");
					return new SmtpAnchorMailbox(text5, this);
				}
			}
			if (flag)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string, string, int>((long)this.GetHashCode(), "[RemotePowerShellProxyRequestHandler::ResolveAnchorMailbox]: Organization-based. Organization {0} from {1}, context {2}.", routingBasedOrganization, text4, base.TraceContext);
				}
				DomainAnchorMailbox domainAnchorMailbox = new DomainAnchorMailbox(routingBasedOrganization, this);
				if (this.isSyndicatedAdmin && !this.IsSecurityTokenPresent())
				{
					ExchangeObjectVersion exchangeObjectVersion = domainAnchorMailbox.GetADRawEntry()[OrganizationConfigSchema.AdminDisplayVersion] as ExchangeObjectVersion;
					if (exchangeObjectVersion.ExchangeBuild.Major < ExchangeObjectVersion.Exchange2012.ExchangeBuild.Major)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<ExchangeObjectVersion>((long)this.GetHashCode(), "[RemotePowerShellProxyRequestHandler::ResolveAnchorMailbox] Syndicated Admin. Target tenant is in E14 forest. Let backend generate security token and do the redirection. ExchangeVersion: {0}", exchangeObjectVersion);
						}
						this.isSyndicatedAdminManageDownLevelTarget = true;
						base.Logger.AppendGenericInfo("SyndicatedAdminTargetTenantDownLevel", true);
						return base.ResolveAnchorMailbox();
					}
				}
				base.Logger.Set(3, text4);
				return domainAnchorMailbox;
			}
			return base.ResolveAnchorMailbox();
		}

		// Token: 0x060006F2 RID: 1778 RVA: 0x00019AA7 File Offset: 0x00017CA7
		protected override Uri UpdateExternalRedirectUrl(Uri originalRedirectUrl)
		{
			return new UriBuilder(base.ClientRequest.Url)
			{
				Host = originalRedirectUrl.Host,
				Port = originalRedirectUrl.Port
			}.Uri;
		}

		// Token: 0x060006F3 RID: 1779 RVA: 0x00028E60 File Offset: 0x00027060
		protected override void ExposeExceptionToClientResponse(Exception ex)
		{
			if (!WinRMHelper.FriendlyErrorEnabled.Value)
			{
				base.ExposeExceptionToClientResponse(ex);
				return;
			}
			if (ex is WebException)
			{
				WebException ex2 = (WebException)ex;
				if (WinRMHelper.IsPingRequest(ex2))
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[RemotePowerShellProxyRequestHandler::ExposeExceptionToClientResponse]: Context={0}, Ping found.", base.TraceContext);
					}
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(base.Logger, 21, "Ping");
					base.ClientResponse.Headers["X-RemotePS-Ping"] = "Ping";
					return;
				}
				if (WinRMHelper.CouldBePingRequest(ex2))
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[RemotePowerShellProxyRequestHandler::ExposeExceptionToClientResponse]: Context={0}, Could be Ping request.", base.TraceContext);
					}
					base.ClientResponse.Headers["X-RemotePS-Ping"] = "Possible-Ping";
					return;
				}
				if (ex2.Status != WebExceptionStatus.ProtocolError)
				{
					WinRMInfo.SetFailureCategoryInfo(base.ClientResponse.Headers, 3, ex2.Status.ToString());
				}
				if (ex2.Response != null)
				{
					string text = ex2.Response.Headers["X-BasicAuthToOAuthConversionDiagnostics"];
					if (!string.IsNullOrWhiteSpace(text))
					{
						base.ClientResponse.Headers["X-BasicAuthToOAuthConversionDiagnostics"] = text + " ";
					}
				}
			}
			if (ex is HttpProxyException && !string.IsNullOrWhiteSpace(ex.Message) && !WinRMHelper.DiagnosticsInfoHasBeenWritten(base.ClientResponse.Headers))
			{
				WinRMInfo.SetFailureCategoryInfo(base.ClientResponse.Headers, 3, ex.GetType().Name);
				string diagnosticsInfo = WinRMHelper.GetDiagnosticsInfo(base.HttpContext);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[RemotePowerShellProxyRequestHandler::ExposeExceptionToClientResponse]: Context={0}, Write Message {1} to client response.", base.TraceContext, ex.Message);
				}
				WinRMHelper.SetDiagnosticsInfoWrittenFlag(base.ClientResponse.Headers);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(base.Logger, "FriendlyError", "ExposeException");
				base.ClientResponse.Write(diagnosticsInfo + ex.Message);
				return;
			}
			base.ExposeExceptionToClientResponse(ex);
		}

		// Token: 0x060006F4 RID: 1780 RVA: 0x00029081 File Offset: 0x00027281
		protected override StreamProxy BuildResponseStreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, byte[] buffer)
		{
			if (!LoggerHelper.IsProbePingRequest(base.ClientRequest) && WinRMHelper.FriendlyErrorEnabled.Value)
			{
				return new RpsOutDataResponseStreamProxy(streamProxyType, source, target, buffer, this);
			}
			return base.BuildResponseStreamProxy(streamProxyType, source, target, buffer);
		}

		// Token: 0x060006F5 RID: 1781 RVA: 0x000290B3 File Offset: 0x000272B3
		protected override void UpdateOrInvalidateAnchorMailboxCache(Guid mdbGuid, string resourceForest)
		{
			if (this.winRMInfo != null && "Remove-PSSession".Equals(this.winRMInfo.Action, StringComparison.OrdinalIgnoreCase))
			{
				base.UpdateOrInvalidateAnchorMailboxCache(mdbGuid, resourceForest);
				return;
			}
			base.InvalidateAnchorMailboxCache(mdbGuid, resourceForest);
		}

		// Token: 0x060006F6 RID: 1782 RVA: 0x000290E8 File Offset: 0x000272E8
		protected override void SetUseServerCookieFlag(AnchorMailbox anchorMailbox)
		{
			if (CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).GenerateServerCookieInRPS.Enabled)
			{
				DatabaseBasedAnchorMailbox databaseBasedAnchorMailbox = anchorMailbox as DatabaseBasedAnchorMailbox;
				if (databaseBasedAnchorMailbox != null)
				{
					databaseBasedAnchorMailbox.UseServerCookie = true;
				}
			}
		}

		// Token: 0x060006F7 RID: 1783 RVA: 0x0002911E File Offset: 0x0002731E
		protected override bool ShouldCopyCookieToClientResponse(Cookie cookie)
		{
			return !(cookie.Name == Constants.RPSBackEndServerCookieName) && base.ShouldCopyCookieToClientResponse(cookie);
		}

		// Token: 0x060006F8 RID: 1784 RVA: 0x0002913C File Offset: 0x0002733C
		private string GetRoutingBasedOrganization(NameValueCollection urlParameters, out bool routeBasedOnOrgnaizationInUrl, out string organizationRoutingHint)
		{
			string text = urlParameters["organization"];
			if (!string.IsNullOrEmpty(text))
			{
				routeBasedOnOrgnaizationInUrl = true;
				organizationRoutingHint = "Url-Organization";
				return text;
			}
			text = urlParameters["DelegatedOrg"];
			if (!string.IsNullOrEmpty(text))
			{
				routeBasedOnOrgnaizationInUrl = true;
				organizationRoutingHint = "Url-DelegatedOrg";
				return text;
			}
			routeBasedOnOrgnaizationInUrl = false;
			return this.GetExecutingUserOrganization(out organizationRoutingHint);
		}

		// Token: 0x060006F9 RID: 1785 RVA: 0x00029194 File Offset: 0x00027394
		private string GetExecutingUserOrganization(out string organizatonRoutingHint)
		{
			organizatonRoutingHint = null;
			CommonAccessToken commonAccessToken = base.HttpContext.Items["Item-CommonAccessToken"] as CommonAccessToken;
			if (commonAccessToken == null)
			{
				if (base.AuthBehavior.AuthState != AuthState.FrontEndFullAuth)
				{
					string executingUserOrganization = base.AuthBehavior.GetExecutingUserOrganization();
					if (!string.IsNullOrEmpty(executingUserOrganization))
					{
						organizatonRoutingHint = "LiveIdBasic-UserOrg";
						return executingUserOrganization;
					}
				}
				return null;
			}
			AccessTokenType accessTokenType = (AccessTokenType)Enum.Parse(typeof(AccessTokenType), commonAccessToken.TokenType, true);
			if (accessTokenType == 2)
			{
				LiveIdBasicTokenAccessor liveIdBasicTokenAccessor = LiveIdBasicTokenAccessor.Attach(commonAccessToken);
				SmtpAddress smtpAddress;
				smtpAddress..ctor(liveIdBasicTokenAccessor.LiveIdMemberName);
				organizatonRoutingHint = "LiveIdBasic-UserOrg";
				return smtpAddress.Domain;
			}
			if (accessTokenType != 3)
			{
				return null;
			}
			string result;
			commonAccessToken.ExtensionData.TryGetValue("OrganizationName", out result);
			organizatonRoutingHint = "LiveIdNego2-UserOrg";
			return result;
		}

		// Token: 0x060006FA RID: 1786 RVA: 0x00029258 File Offset: 0x00027458
		private bool IsSecurityTokenPresent()
		{
			bool flag = !string.IsNullOrEmpty(HttpUtility.ParseQueryString(base.ClientRequest.Url.Query.Replace(';', '&'))["SecurityToken"]);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<bool>((long)this.GetHashCode(), "[RemotePowerShellProxyRequestHandler::IsSecurityTokenPresent] {0}", flag);
			}
			return flag;
		}

		// Token: 0x060006FB RID: 1787 RVA: 0x000292BC File Offset: 0x000274BC
		private WinRMInfo ParseWinRMInfo(Stream stream)
		{
			WinRMInfo winRMInfo;
			string text;
			if (new WinRMParser(base.TraceContext).TryParseStream(stream, out winRMInfo, out text))
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(base.Logger, 21, winRMInfo.Action);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(base.Logger, "CommandId", winRMInfo.CommandId);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(base.Logger, "SessionId", winRMInfo.SessionId);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(base.Logger, "ShellId", winRMInfo.ShellId);
				if (!"http://schemas.microsoft.com/wbem/wsman/1/windows/shell/signal/terminate".Equals(winRMInfo.SignalCode, StringComparison.OrdinalIgnoreCase))
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(base.Logger, "SignalCode", winRMInfo.SignalCode);
				}
			}
			else
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(base.Logger, 21, text);
			}
			return winRMInfo;
		}

		// Token: 0x040003E1 RID: 993
		private const string CafeForceRouteToLogonAccountHeaderKey = "msExchCafeForceRouteToLogonAccount";

		// Token: 0x040003E2 RID: 994
		private const string SecurityTokenKey = "SecurityToken";

		// Token: 0x040003E3 RID: 995
		private static readonly string[] ClientSupportedBackEndCookieNames = new string[]
		{
			Constants.RPSBackEndServerCookieName
		};

		// Token: 0x040003E4 RID: 996
		private static readonly Regex ExchClientVerRegex = new Regex("(?<major>\\d{2})\\.(?<minor>\\d{1,})\\.(?<build>\\d{1,})\\.(?<revision>\\d{1,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x040003E5 RID: 997
		private bool isSyndicatedAdmin;

		// Token: 0x040003E6 RID: 998
		private bool isSyndicatedAdminManageDownLevelTarget;

		// Token: 0x040003E7 RID: 999
		private WinRMInfo winRMInfo;
	}
}
