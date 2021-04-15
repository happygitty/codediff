using System;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Clients.Security;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.Security.Authorization;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000039 RID: 57
	internal class LiveIdCookieAuthBehavior : DefaultAuthBehavior
	{
		// Token: 0x060001D3 RID: 467 RVA: 0x000087D4 File Offset: 0x000069D4
		public LiveIdCookieAuthBehavior(HttpContext httpContext, int serverVersion) : base(httpContext, serverVersion)
		{
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x060001D4 RID: 468 RVA: 0x00008B30 File Offset: 0x00006D30
		public override bool ShouldCopyAuthenticationHeaderToClientResponse
		{
			get
			{
				return base.AuthState == AuthState.BackEndFullAuth;
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x060001D5 RID: 469 RVA: 0x00008B3B File Offset: 0x00006D3B
		protected override string VersionSupportsBackEndFullAuthString
		{
			get
			{
				return HttpProxySettings.EnableLiveIdCookieBEAuthVersion.Value;
			}
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x00008B48 File Offset: 0x00006D48
		public static bool IsLiveIdCookieAuth(HttpContext httpContext)
		{
			IIdentity identity;
			if (HttpProxySettings.IdentityIndependentAuthBehaviorEnabled.Value)
			{
				identity = AuthCommon.GetAuthenticationBehaviorType(httpContext);
			}
			else
			{
				IPrincipal user = httpContext.User;
				identity = ((user != null) ? user.Identity : null);
			}
			return identity != null && identity is GenericIdentity && string.Equals(identity.AuthenticationType, "OrgId", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x00008BA0 File Offset: 0x00006DA0
		public override bool IsFullyAuthenticated()
		{
			return base.HttpContext.Items["Item-CommonAccessToken"] is CommonAccessToken || (base.HttpContext.Items["LiveIdSkippedAuthForAnonResource"] != null && (bool)base.HttpContext.Items["LiveIdSkippedAuthForAnonResource"]);
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x00008C00 File Offset: 0x00006E00
		public override AnchorMailbox CreateAuthModuleSpecificAnchorMailbox(IRequestContext requestContext)
		{
			string liveIdMemberName;
			AnchorMailbox result;
			if (RequestCookieParser.TryGetDefaultAnchorMailboxCookie(requestContext.HttpContext.Request.Cookies, ref liveIdMemberName))
			{
				requestContext.Logger.SafeSet(3, "DefaultAnchorMailboxCookie");
				result = new LiveIdMemberNameAnchorMailbox(liveIdMemberName, null, requestContext);
			}
			else
			{
				requestContext.Logger.SafeSet(3, "UnauthenticatedAnonymous");
				result = new AnonymousAnchorMailbox(requestContext);
			}
			return result;
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x00008C67 File Offset: 0x00006E67
		public override void ContinueOnAuthenticate(HttpApplication app, AsyncCallback callback)
		{
			LiveIdAuthenticationModule.ContinueOnAuthenticate(app.Context);
			callback(null);
		}

		// Token: 0x060001DA RID: 474 RVA: 0x00008C7B File Offset: 0x00006E7B
		public override void SetFailureStatus()
		{
		}
	}
}
