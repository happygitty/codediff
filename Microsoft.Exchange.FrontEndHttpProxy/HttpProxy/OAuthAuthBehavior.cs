using System;
using System.Web;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.OAuth;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200003A RID: 58
	internal class OAuthAuthBehavior : DefaultAuthBehavior
	{
		// Token: 0x060001DB RID: 475 RVA: 0x000087D4 File Offset: 0x000069D4
		public OAuthAuthBehavior(HttpContext httpContext, int serverVersion) : base(httpContext, serverVersion)
		{
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x060001DC RID: 476 RVA: 0x00003165 File Offset: 0x00001365
		public override bool ShouldDoFullAuthOnUnresolvedAnchorMailbox
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x060001DD RID: 477 RVA: 0x00008B30 File Offset: 0x00006D30
		public override bool ShouldCopyAuthenticationHeaderToClientResponse
		{
			get
			{
				return base.AuthState == AuthState.BackEndFullAuth;
			}
		}

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x060001DE RID: 478 RVA: 0x00008C7D File Offset: 0x00006E7D
		protected override string VersionSupportsBackEndFullAuthString
		{
			get
			{
				return HttpProxySettings.EnableOAuthBEAuthVersion.Value;
			}
		}

		// Token: 0x060001DF RID: 479 RVA: 0x00008C89 File Offset: 0x00006E89
		public static bool IsOAuth(HttpContext httpContext)
		{
			return HttpContextUserParser.IsOAuthAuthentication(httpContext.User);
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x00008C98 File Offset: 0x00006E98
		public override AnchorMailbox CreateAuthModuleSpecificAnchorMailbox(IRequestContext requestContext)
		{
			HttpContext httpContext = requestContext.HttpContext;
			OAuthPreAuthIdentity oauthPreAuthIdentity;
			if (HttpContextUserParser.TryGetOAuthPreAuthIdentity(httpContext.User, ref oauthPreAuthIdentity))
			{
				try
				{
					string externalDirectoryObjectId;
					if (!RequestHeaderParser.TryGetExternalDirectoryObjectId(httpContext.Request.Headers, ref externalDirectoryObjectId))
					{
						OAuthPreAuthType preAuthType = oauthPreAuthIdentity.PreAuthType;
						switch (preAuthType)
						{
						case 1:
							requestContext.Logger.SafeSet(3, "PreAuth-Smtp");
							return new SmtpAnchorMailbox(oauthPreAuthIdentity.LookupValue, requestContext);
						case 2:
							requestContext.Logger.SafeSet(3, "PreAuth-LiveID");
							return new LiveIdMemberNameAnchorMailbox(oauthPreAuthIdentity.LookupValue, null, requestContext);
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
							break;
						case 8:
							requestContext.Logger.SafeSet(3, "PreAuth-TenantGuid");
							return new DomainAnchorMailbox(oauthPreAuthIdentity.TenantGuid.ToString(), requestContext);
						case 9:
							requestContext.Logger.SafeSet(3, "PreAuth-TenantDomain");
							return new DomainAnchorMailbox(oauthPreAuthIdentity.TenantDomain, requestContext);
						case 10:
							requestContext.Logger.SafeSet(3, "PreAuth-ExternalDirectoryObjectIdTenantGuid");
							return new ExternalDirectoryObjectIdAnchorMailbox(oauthPreAuthIdentity.LookupValue, oauthPreAuthIdentity.TenantGuid, requestContext);
						case 11:
							requestContext.Logger.SafeSet(3, "PreAuth-ExternalDirectoryObjectIdTenantDomain");
							return new ExternalDirectoryObjectIdAnchorMailbox(oauthPreAuthIdentity.LookupValue, oauthPreAuthIdentity.TenantDomain, requestContext);
						default:
							switch (preAuthType)
							{
							case 99:
							{
								string arg = "Unable to parse OAuth token to locate routing key, extended error data=" + oauthPreAuthIdentity.ExtendedErrorInformation;
								RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericError(requestContext.Logger, "OAuthError", oauthPreAuthIdentity.ExtendedErrorInformation);
								MSDiagnosticsHeader.SetStandardOAuthDiagnosticsResponse(httpContext, 2000001, string.Format(OAuthErrorsUtil.GetDescription(2007), arg), null, null);
								requestContext.Logger.SafeSet(3, "PreAuth-AnonymousAnchorMailbox");
								return new AnonymousAnchorMailbox(requestContext);
							}
							case 100:
								requestContext.Logger.SafeSet(3, "PreAuth-PuidAndDomain");
								return new PuidAnchorMailbox(oauthPreAuthIdentity.LookupValue, oauthPreAuthIdentity.TenantDomain, requestContext, string.Empty);
							case 101:
								requestContext.Logger.SafeSet(3, "PreAuth-PuidAndTenantGuid");
								return new PuidAnchorMailbox(oauthPreAuthIdentity.LookupValue, oauthPreAuthIdentity.TenantGuid, requestContext, string.Empty);
							}
							break;
						}
						throw new InvalidOperationException("unknown preauth type");
					}
					requestContext.Logger.SafeSet(3, "PreAuth-ExternalDirectoryObjectId-Header");
					if (!string.IsNullOrEmpty(oauthPreAuthIdentity.TenantDomain))
					{
						return new ExternalDirectoryObjectIdAnchorMailbox(externalDirectoryObjectId, oauthPreAuthIdentity.TenantDomain, requestContext);
					}
					if (oauthPreAuthIdentity.TenantGuid != Guid.Empty)
					{
						return new ExternalDirectoryObjectIdAnchorMailbox(externalDirectoryObjectId, oauthPreAuthIdentity.TenantGuid, requestContext);
					}
					throw new InvalidOperationException("unknown preauth type");
				}
				finally
				{
					if (!string.IsNullOrEmpty(oauthPreAuthIdentity.LoggingInfo))
					{
						requestContext.Logger.AppendGenericInfo("OAuthInfo", oauthPreAuthIdentity.LoggingInfo);
					}
				}
			}
			return null;
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x00008FB8 File Offset: 0x000071B8
		public override bool IsFullyAuthenticated()
		{
			return base.HttpContext.User.Identity is OAuthIdentity;
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x00008FD2 File Offset: 0x000071D2
		public override void ContinueOnAuthenticate(HttpApplication app, AsyncCallback callback)
		{
			OAuthHttpModule.ContinueOnAuthenticate(app, callback);
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x00008C7B File Offset: 0x00006E7B
		public override void SetFailureStatus()
		{
		}
	}
}
