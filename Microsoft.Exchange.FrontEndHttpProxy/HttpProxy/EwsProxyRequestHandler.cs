using System;
using System.IO;
using System.Net;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.OAuth;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000095 RID: 149
	internal class EwsProxyRequestHandler : EwsAutodiscoverProxyRequestHandler
	{
		// Token: 0x06000526 RID: 1318 RVA: 0x0001CA8A File Offset: 0x0001AC8A
		internal EwsProxyRequestHandler() : this(false)
		{
		}

		// Token: 0x06000527 RID: 1319 RVA: 0x0001CA93 File Offset: 0x0001AC93
		internal EwsProxyRequestHandler(bool isOwa14EwsProxyRequest)
		{
			this.isOwa14EwsProxyRequest = isOwa14EwsProxyRequest;
		}

		// Token: 0x17000124 RID: 292
		// (get) Token: 0x06000528 RID: 1320 RVA: 0x0001CAA2 File Offset: 0x0001ACA2
		protected override bool WillContentBeChangedDuringStreaming
		{
			get
			{
				return !base.IsWsSecurityRequest && base.ClientRequest.CanHaveBody() && (this.isOwa14EwsProxyRequest || base.ProxyToDownLevel || this.proxyForSameOrgExchangeOAuthCallToLowerVersion);
			}
		}

		// Token: 0x17000125 RID: 293
		// (get) Token: 0x06000529 RID: 1321 RVA: 0x0001981A File Offset: 0x00017A1A
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x0600052A RID: 1322 RVA: 0x0001CAD6 File Offset: 0x0001ACD6
		protected override bool ShouldBlockCurrentOAuthRequest()
		{
			return !this.proxyForSameOrgExchangeOAuthCallToLowerVersion && base.ShouldBlockCurrentOAuthRequest();
		}

		// Token: 0x0600052B RID: 1323 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected override void DoProtocolSpecificBeginRequestLogging()
		{
		}

		// Token: 0x0600052C RID: 1324 RVA: 0x0001CAE8 File Offset: 0x0001ACE8
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			base.AddProtocolSpecificHeadersToServerRequest(headers);
			if (this.proxyForSameOrgExchangeOAuthCallToLowerVersion)
			{
				headers.Remove("X-CommonAccessToken");
			}
		}

		// Token: 0x0600052D RID: 1325 RVA: 0x0001CB04 File Offset: 0x0001AD04
		protected override StreamProxy BuildRequestStreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, byte[] buffer)
		{
			if (base.IsWsSecurityRequest || !base.ClientRequest.CanHaveBody())
			{
				return base.BuildRequestStreamProxy(streamProxyType, source, target, buffer);
			}
			if (!this.isOwa14EwsProxyRequest && !base.ProxyToDownLevel && !this.proxyForSameOrgExchangeOAuthCallToLowerVersion)
			{
				return base.BuildRequestStreamProxy(streamProxyType, source, target, buffer);
			}
			string requestVersionToAdd = null;
			if (this.isOwa14EwsProxyRequest)
			{
				if ("12.1".Equals(base.HttpContext.Request.QueryString["rv"]))
				{
					requestVersionToAdd = "Exchange2007_SP1";
				}
				else
				{
					requestVersionToAdd = "Exchange2010_SP1";
				}
			}
			return new EwsRequestStreamProxy(streamProxyType, source, target, buffer, this, base.ProxyToDownLevel || this.proxyForSameOrgExchangeOAuthCallToLowerVersion, this.proxyForSameOrgExchangeOAuthCallToLowerVersionWithNoSidUser, requestVersionToAdd);
		}

		// Token: 0x0600052E RID: 1326 RVA: 0x0001CBB8 File Offset: 0x0001ADB8
		protected override Uri GetTargetBackEndServerUrl()
		{
			Uri targetBackEndServerUrl = base.GetTargetBackEndServerUrl();
			if (this.isOwa14EwsProxyRequest)
			{
				return new UriBuilder(targetBackEndServerUrl)
				{
					Path = "/ews/exchange.asmx",
					Query = string.Empty
				}.Uri;
			}
			if (targetBackEndServerUrl.AbsolutePath.IndexOf("ews/Nego2", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return new UriBuilder(targetBackEndServerUrl)
				{
					Path = "/ews/exchange.asmx"
				}.Uri;
			}
			OAuthIdentity oauthIdentity = base.HttpContext.User.Identity as OAuthIdentity;
			if (oauthIdentity != null && !oauthIdentity.IsAppOnly && oauthIdentity.IsKnownFromSameOrgExchange && base.HttpContext.Request.UserAgent.StartsWith("ASProxy/CrossForest", StringComparison.InvariantCultureIgnoreCase))
			{
				if (FaultInjection.TraceTest<bool>((FaultInjection.LIDs)3548785981U))
				{
					throw new InvalidOAuthTokenException(6009, null, null);
				}
				this.proxyForSameOrgExchangeOAuthCallToLowerVersion = (base.ProxyToDownLevel || FaultInjection.TraceTest<bool>((FaultInjection.LIDs)2357603645U) || FaultInjection.TraceTest<bool>((FaultInjection.LIDs)3431345469U));
				if (this.proxyForSameOrgExchangeOAuthCallToLowerVersion || oauthIdentity.ActAsUser.IsUserVerified)
				{
					this.proxyForSameOrgExchangeOAuthCallToLowerVersionWithNoSidUser = (FaultInjection.TraceTest<bool>((FaultInjection.LIDs)3431345469U) || oauthIdentity.ActAsUser.Sid == null);
				}
			}
			return targetBackEndServerUrl;
		}

		// Token: 0x0600052F RID: 1327 RVA: 0x0001CCEC File Offset: 0x0001AEEC
		protected override void OnInitializingHandler()
		{
			base.OnInitializingHandler();
			if (HttpProxyGlobals.ProtocolType == 2 && !base.ClientRequest.IsAuthenticated)
			{
				base.IsWsSecurityRequest = base.ClientRequest.IsAnyWsSecurityRequest();
			}
		}

		// Token: 0x06000530 RID: 1328 RVA: 0x0001CD1C File Offset: 0x0001AF1C
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			string text;
			string text2;
			if (RequestHeaderParser.TryGetPreferServerAffinity(base.HttpContext.Request.Headers, ref text) && text.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) && RequestCookieParser.TryGetTargetServerOverride(base.HttpContext.Request.Cookies, ref text2))
			{
				try
				{
					BackEndServer backendServer = BackEndServer.FromString(text2);
					base.Logger.Set(3, Constants.BackEndOverrideCookieName);
					return new ServerInfoAnchorMailbox(backendServer, this);
				}
				catch (ArgumentException ex)
				{
					base.Logger.AppendGenericError("Unable to parse TargetServer: {0}", text2);
					if (ExTraceGlobals.ExceptionTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.ExceptionTracer.TraceDebug<string, ArgumentException>((long)this.GetHashCode(), "[EwsProxyRequestHandler::ResolveAnchorMailbox]: exception hit where target server was '{0}': {1}", text2, ex);
					}
				}
			}
			return base.ResolveAnchorMailbox();
		}

		// Token: 0x06000531 RID: 1329 RVA: 0x0001CDE4 File Offset: 0x0001AFE4
		protected override void CopySupplementalCookiesToClientResponse()
		{
			if (base.AnchoredRoutingTarget != null && !string.IsNullOrEmpty(base.ServerResponse.Headers["X-FromBackend-ServerAffinity"]) && base.ClientRequest.Cookies[Constants.BackEndOverrideCookieName] == null)
			{
				HttpCookie httpCookie = new HttpCookie(Constants.BackEndOverrideCookieName, base.AnchoredRoutingTarget.BackEndServer.ToString());
				httpCookie.HttpOnly = true;
				httpCookie.Secure = base.ClientRequest.IsSecureConnection;
				base.ClientResponse.Cookies.Add(httpCookie);
			}
			base.CopySupplementalCookiesToClientResponse();
		}

		// Token: 0x06000532 RID: 1330 RVA: 0x0001CE78 File Offset: 0x0001B078
		protected override void ClearBackEndOverrideCookie()
		{
			HttpCookie httpCookie = base.ClientRequest.Cookies[Constants.BackEndOverrideCookieName];
			if (httpCookie != null)
			{
				httpCookie.Value = null;
				httpCookie.Expires = DateTime.UtcNow.AddYears(-1);
				base.ClientResponse.Cookies.Add(httpCookie);
			}
			base.ClearBackEndOverrideCookie();
		}

		// Token: 0x0400035C RID: 860
		private const string Owa14EwsProxyRequestVersionHeader = "rv";

		// Token: 0x0400035D RID: 861
		private const string Owa14EwsProxyE12SP1Version = "12.1";

		// Token: 0x0400035E RID: 862
		private const string Exchange2007SP1Version = "Exchange2007_SP1";

		// Token: 0x0400035F RID: 863
		private const string Exchange2010SP1Version = "Exchange2010_SP1";

		// Token: 0x04000360 RID: 864
		private const string Nego2PathPrefix = "ews/Nego2";

		// Token: 0x04000361 RID: 865
		private const string EwsPath = "/ews/exchange.asmx";

		// Token: 0x04000362 RID: 866
		private readonly bool isOwa14EwsProxyRequest;

		// Token: 0x04000363 RID: 867
		private bool proxyForSameOrgExchangeOAuthCallToLowerVersion;

		// Token: 0x04000364 RID: 868
		private bool proxyForSameOrgExchangeOAuthCallToLowerVersionWithNoSidUser;
	}
}
