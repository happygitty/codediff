using System;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000036 RID: 54
	internal class ConsumerEasAuthBehavior : DefaultAuthBehavior
	{
		// Token: 0x060001AE RID: 430 RVA: 0x000087D4 File Offset: 0x000069D4
		public ConsumerEasAuthBehavior(HttpContext httpContext, int serverVersion) : base(httpContext, serverVersion)
		{
		}

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x060001AF RID: 431 RVA: 0x00003165 File Offset: 0x00001365
		public override bool ShouldDoFullAuthOnUnresolvedAnchorMailbox
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x060001B0 RID: 432 RVA: 0x000087DE File Offset: 0x000069DE
		protected override string VersionSupportsBackEndFullAuthString
		{
			get
			{
				return HttpProxySettings.EnableRpsTokenBEAuthVersion.Value;
			}
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x000087EC File Offset: 0x000069EC
		public static bool IsConsumerEasTokenAuth(HttpContext httpContext)
		{
			IPrincipal user = httpContext.User;
			return user != null && user.Identity != null && (string.Equals(user.Identity.AuthenticationType, "MsaRpsTokenAuthType", StringComparison.OrdinalIgnoreCase) || string.Equals(user.Identity.AuthenticationType, "MsaRpsOAuthType", StringComparison.OrdinalIgnoreCase));
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x00008840 File Offset: 0x00006A40
		public override AnchorMailbox CreateAuthModuleSpecificAnchorMailbox(IRequestContext requestContext)
		{
			AnchorMailbox result = null;
			string text;
			if (RequestCookieParser.TryGetDefaultAnchorMailboxCookie(requestContext.HttpContext.Request.Cookies, ref text))
			{
				if (SmtpAddress.IsValidSmtpAddress(text))
				{
					string organizationContext;
					HttpContextItemParser.TryGetLiveIdOrganizationContext(requestContext.HttpContext.Items, ref organizationContext);
					requestContext.Logger.SafeSet(3, "DefaultAnchorMailboxCookie");
					result = new LiveIdMemberNameAnchorMailbox(text, organizationContext, requestContext);
				}
			}
			else
			{
				requestContext.Logger.SafeSet(3, "UnauthenticatedAnonymous");
				result = new AnonymousAnchorMailbox(requestContext);
			}
			return result;
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x00003165 File Offset: 0x00001365
		public override bool IsFullyAuthenticated()
		{
			return false;
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x000088C2 File Offset: 0x00006AC2
		public override void ContinueOnAuthenticate(HttpApplication app, AsyncCallback callback)
		{
			throw new InvalidOperationException("Full authentication for RPS token on front end is not supported.");
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x000088C2 File Offset: 0x00006AC2
		public override void SetFailureStatus()
		{
			throw new InvalidOperationException("Full authentication for RPS token on front end is not supported.");
		}
	}
}
