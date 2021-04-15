using System;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.Security.Authorization;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000038 RID: 56
	internal class LiveIdBasicAuthBehavior : DefaultAuthBehavior
	{
		// Token: 0x060001CA RID: 458 RVA: 0x000087D4 File Offset: 0x000069D4
		public LiveIdBasicAuthBehavior(HttpContext httpContext, int serverVersion) : base(httpContext, serverVersion)
		{
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x060001CB RID: 459 RVA: 0x00003193 File Offset: 0x00001393
		public override bool ShouldDoFullAuthOnUnresolvedAnchorMailbox
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x060001CC RID: 460 RVA: 0x00008A05 File Offset: 0x00006C05
		protected override string VersionSupportsBackEndFullAuthString
		{
			get
			{
				return HttpProxySettings.EnableLiveIdBasicBEAuthVersion.Value;
			}
		}

		// Token: 0x060001CD RID: 461 RVA: 0x00008A14 File Offset: 0x00006C14
		public static bool IsLiveIdBasicAuth(HttpContext httpContext)
		{
			IPrincipal user = httpContext.User;
			return user != null && user.Identity != null && string.Equals(user.Identity.AuthenticationType, "LiveIdBasic", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060001CE RID: 462 RVA: 0x00008A4C File Offset: 0x00006C4C
		public override AnchorMailbox CreateAuthModuleSpecificAnchorMailbox(IRequestContext requestContext)
		{
			string liveIdMemberName;
			if (!HttpContextItemParser.TryGetLiveIdMemberName(requestContext.HttpContext.Items, ref liveIdMemberName))
			{
				return null;
			}
			string organizationContext;
			HttpContextItemParser.TryGetLiveIdOrganizationContext(requestContext.HttpContext.Items, ref organizationContext);
			requestContext.Logger.SafeSet(3, "LiveIdBasic-LiveIdMemberName");
			return new LiveIdMemberNameAnchorMailbox(liveIdMemberName, organizationContext, requestContext);
		}

		// Token: 0x060001CF RID: 463 RVA: 0x00008AA0 File Offset: 0x00006CA0
		public override string GetExecutingUserOrganization()
		{
			string text;
			if (HttpContextItemParser.TryGetLiveIdMemberName(base.HttpContext.Items, ref text))
			{
				SmtpAddress smtpAddress;
				smtpAddress..ctor(text);
				return smtpAddress.Domain;
			}
			return base.GetExecutingUserOrganization();
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x00008AD7 File Offset: 0x00006CD7
		public override bool IsFullyAuthenticated()
		{
			return base.HttpContext.Items["Item-CommonAccessToken"] is CommonAccessToken;
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x00008AF6 File Offset: 0x00006CF6
		public override void ContinueOnAuthenticate(HttpApplication app, AsyncCallback callback)
		{
			LiveIdBasicAuthModule.ContinueOnAuthenticate(app, null, callback, null);
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x00008B02 File Offset: 0x00006D02
		public override void SetFailureStatus()
		{
			if (base.HttpContext.Response.StatusCode == 200)
			{
				base.HttpContext.Response.StatusCode = 401;
			}
		}
	}
}
