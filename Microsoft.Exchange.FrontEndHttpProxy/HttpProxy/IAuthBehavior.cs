using System;
using System.Web;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000035 RID: 53
	internal interface IAuthBehavior
	{
		// Token: 0x17000062 RID: 98
		// (get) Token: 0x060001A4 RID: 420
		AuthState AuthState { get; }

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x060001A5 RID: 421
		bool ShouldDoFullAuthOnUnresolvedAnchorMailbox { get; }

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x060001A6 RID: 422
		bool ShouldCopyAuthenticationHeaderToClientResponse { get; }

		// Token: 0x060001A7 RID: 423
		void SetState(int serverVersion);

		// Token: 0x060001A8 RID: 424
		void ResetState();

		// Token: 0x060001A9 RID: 425
		string GetExecutingUserOrganization();

		// Token: 0x060001AA RID: 426
		bool IsFullyAuthenticated();

		// Token: 0x060001AB RID: 427
		AnchorMailbox CreateAuthModuleSpecificAnchorMailbox(IRequestContext requestContext);

		// Token: 0x060001AC RID: 428
		void ContinueOnAuthenticate(HttpApplication app, AsyncCallback callback);

		// Token: 0x060001AD RID: 429
		void SetFailureStatus();
	}
}
