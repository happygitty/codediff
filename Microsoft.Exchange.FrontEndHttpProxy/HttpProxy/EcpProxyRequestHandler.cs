using System;
using System.Net;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;
using Microsoft.Exchange.VariantConfiguration.Global;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000091 RID: 145
	internal class EcpProxyRequestHandler : OwaEcpProxyRequestHandler<EcpService>
	{
		// Token: 0x1700011E RID: 286
		// (get) Token: 0x06000505 RID: 1285 RVA: 0x0001BB1C File Offset: 0x00019D1C
		// (set) Token: 0x06000506 RID: 1286 RVA: 0x0001BB24 File Offset: 0x00019D24
		internal bool IsCrossForestDelegated { get; set; }

		// Token: 0x1700011F RID: 287
		// (get) Token: 0x06000507 RID: 1287 RVA: 0x0001BB30 File Offset: 0x00019D30
		protected override string ProxyLogonUri
		{
			get
			{
				string explicitPath = this.GetExplicitPath(base.ClientRequest.Path);
				if (explicitPath != null)
				{
					return explicitPath + "proxyLogon.ecp";
				}
				return "proxyLogon.ecp";
			}
		}

		// Token: 0x17000120 RID: 288
		// (get) Token: 0x06000508 RID: 1288 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x0001BB64 File Offset: 0x00019D64
		internal static void AddDownLevelProxyHeaders(WebHeaderCollection headers, HttpContext context)
		{
			if (!context.Request.IsAuthenticated)
			{
				return;
			}
			if (context.User != null)
			{
				IIdentity identity = context.User.Identity;
				if ((identity is WindowsIdentity || identity is ClientSecurityContextIdentity) && null != IIdentityExtensions.GetSecurityIdentifier(identity))
				{
					string value = IIdentityExtensions.GetSecurityIdentifier(identity).ToString();
					headers["msExchLogonAccount"] = value;
					headers["msExchLogonMailbox"] = value;
					headers["msExchTargetMailbox"] = value;
				}
			}
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x0001BBE4 File Offset: 0x00019DE4
		internal static bool IsCrossForestDelegatedRequest(HttpRequest request)
		{
			if (!string.IsNullOrEmpty(request.QueryString["SecurityToken"]))
			{
				return true;
			}
			HttpCookie httpCookie = request.Cookies["SecurityToken"];
			return httpCookie != null && !string.IsNullOrEmpty(httpCookie.Value);
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x0001BC30 File Offset: 0x00019E30
		protected override bool ShouldExcludeFromExplicitLogonParsing()
		{
			bool result = base.IsResourceRequest();
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ShouldExcludeFromExplicitLogonParsing]: request is resource:{0}.", result.ToString());
			}
			return result;
		}

		// Token: 0x0600050C RID: 1292 RVA: 0x0001BC6F File Offset: 0x00019E6F
		protected override UriBuilder GetClientUrlForProxy()
		{
			return new UriBuilder(base.ClientRequest.Url);
		}

		// Token: 0x0600050D RID: 1293 RVA: 0x0001BC84 File Offset: 0x00019E84
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			headers[Constants.LiveIdEnvironment] = (string)base.HttpContext.Items[Constants.LiveIdEnvironment];
			headers[Constants.LiveIdPuid] = (string)base.HttpContext.Items[Constants.LiveIdPuid];
			headers[Constants.OrgIdPuid] = (string)base.HttpContext.Items[Constants.OrgIdPuid];
			headers[Constants.LiveIdMemberName] = (string)base.HttpContext.Items[Constants.LiveIdMemberName];
			headers["msExchClientPath"] = Uri.EscapeDataString(base.ClientRequest.Path);
			if (this.isSyndicatedAdminManageDownLevelTarget)
			{
				headers["msExchCafeForceRouteToLogonAccount"] = "1";
			}
			if (!this.IsCrossForestDelegated && base.ProxyToDownLevel)
			{
				EcpProxyRequestHandler.AddDownLevelProxyHeaders(headers, base.HttpContext);
				if (base.IsExplicitSignOn)
				{
					string value = null;
					AnchoredRoutingTarget anchoredRoutingTarget = this.isSyndicatedAdminManageDownLevelTarget ? this.originalAnchoredRoutingTarget : base.AnchoredRoutingTarget;
					if (anchoredRoutingTarget != null)
					{
						UserBasedAnchorMailbox userBasedAnchorMailbox = anchoredRoutingTarget.AnchorMailbox as UserBasedAnchorMailbox;
						if (userBasedAnchorMailbox != null)
						{
							ADRawEntry adrawEntry = userBasedAnchorMailbox.GetADRawEntry();
							if (adrawEntry != null)
							{
								SecurityIdentifier securityIdentifier = adrawEntry[ADMailboxRecipientSchema.Sid] as SecurityIdentifier;
								if (securityIdentifier != null)
								{
									value = securityIdentifier.ToString();
								}
							}
						}
					}
					headers["msExchTargetMailbox"] = value;
				}
			}
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x0600050E RID: 1294 RVA: 0x0001BDEC File Offset: 0x00019FEC
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			bool flag = !string.Equals(headerName, Constants.MsExchProxyUri, StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "msExchLogonAccount", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "msExchLogonMailbox", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "msExchTargetMailbox", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, Constants.LiveIdPuid, StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, Constants.LiveIdMemberName, StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "msExchCafeForceRouteToLogonAccount", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, Constants.LiveIdEnvironment, StringComparison.OrdinalIgnoreCase) && base.ShouldCopyHeaderToServerRequest(headerName);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string, string>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ShouldCopyHeaderToServerRequest]: {0} header '{1}'.", flag ? "copy" : "skip", headerName);
			}
			return flag;
		}

		// Token: 0x0600050F RID: 1295 RVA: 0x0001BEAC File Offset: 0x0001A0AC
		protected override void HandleLogoffRequest()
		{
			if (base.ClientRequest != null && base.ClientResponse != null && base.ClientRequest.Url.AbsolutePath.EndsWith("logoff.aspx", StringComparison.OrdinalIgnoreCase))
			{
				if (!Utilities.IsPartnerHostedOnly && !CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).NoFormBasedAuthentication.Enabled)
				{
					FbaModule.InvalidateKeyCache(base.ClientRequest);
				}
				bool flag = false;
				if (!string.IsNullOrEmpty(base.ClientRequest.UserAgent) && new UserAgent(base.ClientRequest.UserAgent, base.ClientRequest.Cookies).DoesSupportSameSiteNone())
				{
					flag = true;
				}
				Utility.DeleteFbaAuthCookies(base.ClientRequest, base.ClientResponse, flag);
			}
		}

		// Token: 0x06000510 RID: 1296 RVA: 0x0001BF60 File Offset: 0x0001A160
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			if (base.State != ProxyRequestHandler.ProxyState.CalculateBackEndSecondRound)
			{
				if (!base.AuthBehavior.IsFullyAuthenticated())
				{
					base.HasPreemptivelyCheckedForRoutingHint = true;
					string liveIdMemberName;
					if (RequestHeaderParser.TryGetAnchorMailboxUpn(base.ClientRequest.Headers, ref liveIdMemberName))
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ResolveAnchorMailbox]: From Header Routing UPN Hint, context {1}.", base.TraceContext);
						}
						base.Logger.SafeSet(3, "OwaEcpUpn");
						return new LiveIdMemberNameAnchorMailbox(liveIdMemberName, null, this);
					}
					AnchorMailbox anchorMailbox = base.CreateAnchorMailboxFromRoutingHint();
					if (anchorMailbox != null)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ResolveAnchorMailbox]: From Header Routing Hint, context {1}.", base.TraceContext);
						}
						return anchorMailbox;
					}
				}
				string text = this.TryGetExplicitLogonNode(0);
				bool flag;
				if (!string.IsNullOrEmpty(text))
				{
					if (SmtpAddress.IsValidSmtpAddress(text))
					{
						base.IsExplicitSignOn = true;
						base.ExplicitSignOnAddress = text;
						base.Logger.Set(3, "ExplicitSignOn-SMTP");
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<string, int>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ResolveAnchorMailbox]: ExplicitSignOn-SMTP. Address {0}, context {1}.", text, base.TraceContext);
						}
						return new SmtpAnchorMailbox(text, this);
					}
					if ((Utilities.IsPartnerHostedOnly || CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).SyndicatedAdmin.Enabled) && text.StartsWith("@"))
					{
						this.isSyndicatedAdmin = true;
						text = text.Substring(1);
						if (SmtpAddress.IsValidDomain(text))
						{
							string text2 = this.TryGetExplicitLogonNode(1);
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
							{
								ExTraceGlobals.VerboseTracer.TraceDebug<string, string, int>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ResolveAnchorMailbox]: SyndAdmin, domain {0}, SMTP {1}, context {2}.", text, text2, base.TraceContext);
							}
							if (!string.IsNullOrEmpty(text2) && SmtpAddress.IsValidSmtpAddress(text2))
							{
								base.IsExplicitSignOn = true;
								base.ExplicitSignOnAddress = text2;
								base.Logger.Set(3, "SyndAdmin-SMTP");
								return new SmtpAnchorMailbox(text2, this);
							}
							base.Logger.Set(3, "SyndAdmin-Domain");
							return new DomainAnchorMailbox(text, this);
						}
					}
				}
				else if (!Utilities.IsPartnerHostedOnly && !GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled)
				{
					string text3 = this.TryGetBackendParameter("TargetServer", out flag);
					if (!string.IsNullOrEmpty(text3))
					{
						base.Logger.Set(3, "TargetServer" + (flag ? "-UrlQuery" : "-Cookie"));
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<string, string, int>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ResolveAnchorMailbox]: On-Premise, TargetServer parameter {0}, from {1}, context {2}.", text3, flag ? "url query" : "cookie", base.TraceContext);
						}
						return new ServerInfoAnchorMailbox(text3, this);
					}
				}
				string text4 = this.TryGetBackendParameter("ExchClientVer", out flag);
				if (!string.IsNullOrEmpty(text4))
				{
					string text5 = Utilities.NormalizeExchClientVer(text4);
					base.Logger.Set(3, "ExchClientVer" + (flag ? "-UrlQuery" : "-Cookie"));
					if (!Utilities.IsPartnerHostedOnly && !GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<string, string, int>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ResolveAnchorMailbox]: On-Premise, Version parameter {0}, from {1}, context {2}.", text4, flag ? "url query" : "cookie", base.TraceContext);
						}
						return base.GetServerVersionAnchorMailbox(text5);
					}
					string text6 = (string)base.HttpContext.Items["AuthenticatedUserOrganization"];
					if (!string.IsNullOrEmpty(text6))
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[EcpProxyRequestHandler::ResolveAnchorMailbox]: On-Cloud, Version parameter {0}, from {1}, domain {2}, context {3}.", new object[]
							{
								text5,
								flag ? "url query" : "cookie",
								text6,
								base.TraceContext
							});
						}
						return VersionedDomainAnchorMailbox.GetAnchorMailbox(text6, text5, this);
					}
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ResolveAnchorMailbox]: AuthenticatedUserOrganization is null. Context {0}.", base.TraceContext);
					}
				}
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string, int>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ResolveAnchorMailbox]: {0}, context {1}, call base method to do regular anchor mailbox calculation.", (base.State == ProxyRequestHandler.ProxyState.CalculateBackEndSecondRound) ? "Second round" : "Nothing special", base.TraceContext);
			}
			return base.ResolveAnchorMailbox();
		}

		// Token: 0x06000511 RID: 1297 RVA: 0x0001C3BF File Offset: 0x0001A5BF
		protected override void CopySupplementalCookiesToClientResponse()
		{
			if (this.backendServerFromUrlCookie != null)
			{
				base.CopyServerCookieToClientResponse(this.backendServerFromUrlCookie);
			}
			this.CopyBEResourcePathCookie();
			base.CopySupplementalCookiesToClientResponse();
		}

		// Token: 0x06000512 RID: 1298 RVA: 0x0001C3E4 File Offset: 0x0001A5E4
		protected override bool ShouldRecalculateProxyTarget()
		{
			bool result = false;
			if (this.isSyndicatedAdmin && !this.IsCrossForestDelegated && base.State == ProxyRequestHandler.ProxyState.CalculateBackEnd && base.AnchoredRoutingTarget.BackEndServer != null && base.AnchoredRoutingTarget.BackEndServer.Version < Server.E15MinVersion)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[EcpProxyRequestHandler::ShouldRecalculateProxyTarget]: context {0}, Syndicated admin request. Target tenant is down level, start 2nd round calculation.", base.TraceContext);
				}
				this.isSyndicatedAdminManageDownLevelTarget = true;
				this.originalAnchoredRoutingTarget = base.AnchoredRoutingTarget;
				result = true;
			}
			else if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[EcpProxyRequestHandler::ShouldRecalculateProxyTarget]: context {0}, no need to do 2nd round calculation: isSyndicatedAdmin {1}, cross forest {2}, state {3}, BEServer Version {4}, lower than E15MinVer {5}", new object[]
				{
					base.TraceContext,
					this.isSyndicatedAdmin,
					this.IsCrossForestDelegated,
					base.State,
					(base.AnchoredRoutingTarget.BackEndServer != null) ? base.AnchoredRoutingTarget.BackEndServer.Version : -1,
					Server.E15MinVersion
				});
			}
			return result;
		}

		// Token: 0x06000513 RID: 1299 RVA: 0x0001C510 File Offset: 0x0001A710
		protected override void LogWebException(WebException exception)
		{
			base.LogWebException(exception);
			HttpWebResponse httpWebResponse = (HttpWebResponse)exception.Response;
			if (httpWebResponse != null && !string.IsNullOrEmpty(httpWebResponse.Headers["X-ECP-ERROR"]))
			{
				base.Logger.AppendGenericError("X-ECP-ERROR", httpWebResponse.Headers["X-ECP-ERROR"]);
			}
		}

		// Token: 0x06000514 RID: 1300 RVA: 0x0001C56C File Offset: 0x0001A76C
		private string GetExplicitPath(string requestPath)
		{
			string result = null;
			int num = requestPath.IndexOf('@');
			if (num > 0)
			{
				int num2 = requestPath.IndexOf('@', num + 1);
				if (num2 < 0)
				{
					num2 = num;
				}
				int num3 = num;
				while (num3 > 0 && requestPath[num3] != '/')
				{
					num3--;
				}
				if (num3 > 0)
				{
					int num4 = requestPath.IndexOf('/', num2);
					if (num4 > num3)
					{
						result = requestPath.Substring(num3 + 1, num4 - num3);
					}
				}
			}
			return result;
		}

		// Token: 0x06000515 RID: 1301 RVA: 0x0001C5D4 File Offset: 0x0001A7D4
		private string TryGetBackendParameter(string key, out bool isFromUrl)
		{
			string text = base.ClientRequest.QueryString[key];
			isFromUrl = false;
			if (string.IsNullOrEmpty(text))
			{
				HttpCookie httpCookie = base.ClientRequest.Cookies[key];
				text = ((httpCookie == null) ? null : httpCookie.Value);
			}
			else
			{
				isFromUrl = true;
				this.backendServerFromUrlCookie = new Cookie(key, text)
				{
					HttpOnly = false,
					Secure = true,
					Path = "/"
				};
			}
			return text;
		}

		// Token: 0x06000516 RID: 1302 RVA: 0x0001C64C File Offset: 0x0001A84C
		private void CopyBEResourcePathCookie()
		{
			string text = base.ServerResponse.Headers[Constants.BEResourcePath];
			if (!string.IsNullOrEmpty(text) && base.AnchoredRoutingTarget != null)
			{
				HttpCookie httpCookie = new HttpCookie(Constants.BEResource, base.AnchoredRoutingTarget.BackEndServer.ToString());
				httpCookie.Path = text;
				httpCookie.HttpOnly = true;
				httpCookie.Secure = base.ClientRequest.IsSecureConnection;
				base.ClientResponse.Cookies.Add(httpCookie);
				return;
			}
			if ((base.ClientRequest.Url.AbsolutePath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase) || base.ClientRequest.Url.AbsolutePath.EndsWith(".slab", StringComparison.OrdinalIgnoreCase)) && ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
			{
				ExTraceGlobals.VerboseTracer.TraceError<string, string, string>(0L, "[EcpProxyRequestHandler::CopyBEResourcePathCookie] Cannot add X-BEResource cookie to the response of {0}! Header from backend: {1}, backend server: {2}", base.ClientRequest.Url.ToString(), text, (base.AnchoredRoutingTarget == null) ? "null" : base.AnchoredRoutingTarget.BackEndServer.ToString());
			}
		}

		// Token: 0x0400034B RID: 843
		public const string ClientPathHeaderKey = "msExchClientPath";

		// Token: 0x0400034C RID: 844
		private const string LogonAccount = "msExchLogonAccount";

		// Token: 0x0400034D RID: 845
		private const string LogonMailbox = "msExchLogonMailbox";

		// Token: 0x0400034E RID: 846
		private const string TargetMailbox = "msExchTargetMailbox";

		// Token: 0x0400034F RID: 847
		private const string CafeForceRouteToLogonAccountHeaderKey = "msExchCafeForceRouteToLogonAccount";

		// Token: 0x04000350 RID: 848
		private const string EcpErrorHeaderName = "X-ECP-ERROR";

		// Token: 0x04000351 RID: 849
		private const string EcpProxyLogonUri = "proxyLogon.ecp";

		// Token: 0x04000352 RID: 850
		private const string LogoffPage = "logoff.aspx";

		// Token: 0x04000353 RID: 851
		private const string SecurityTokenParamName = "SecurityToken";

		// Token: 0x04000354 RID: 852
		private Cookie backendServerFromUrlCookie;

		// Token: 0x04000355 RID: 853
		private bool isSyndicatedAdmin;

		// Token: 0x04000356 RID: 854
		private bool isSyndicatedAdminManageDownLevelTarget;

		// Token: 0x04000357 RID: 855
		private AnchoredRoutingTarget originalAnchoredRoutingTarget;
	}
}
