using System;
using System.Web;
using Microsoft.Exchange.Data;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000037 RID: 55
	internal class DefaultAuthBehavior : IAuthBehavior
	{
		// Token: 0x060001B6 RID: 438 RVA: 0x000088CE File Offset: 0x00006ACE
		protected DefaultAuthBehavior(HttpContext httpContext, int serverVersion)
		{
			this.HttpContext = httpContext;
			this.VersionSupportsBackEndFullAuth = Utilities.ConvertToServerVersion(this.VersionSupportsBackEndFullAuthString);
			this.SetState(serverVersion);
		}

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x060001B7 RID: 439 RVA: 0x000088F5 File Offset: 0x00006AF5
		// (set) Token: 0x060001B8 RID: 440 RVA: 0x000088FD File Offset: 0x00006AFD
		public AuthState AuthState { get; private set; }

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x060001B9 RID: 441 RVA: 0x00003165 File Offset: 0x00001365
		public virtual bool ShouldDoFullAuthOnUnresolvedAnchorMailbox
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x060001BA RID: 442 RVA: 0x00003165 File Offset: 0x00001365
		public virtual bool ShouldCopyAuthenticationHeaderToClientResponse
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x060001BB RID: 443 RVA: 0x00008906 File Offset: 0x00006B06
		// (set) Token: 0x060001BC RID: 444 RVA: 0x0000890E File Offset: 0x00006B0E
		private protected HttpContext HttpContext { protected get; private set; }

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x060001BD RID: 445 RVA: 0x00008917 File Offset: 0x00006B17
		protected virtual string VersionSupportsBackEndFullAuthString
		{
			get
			{
				return HttpProxySettings.EnableDefaultBEAuthVersion.Value;
			}
		}

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x060001BE RID: 446 RVA: 0x00008923 File Offset: 0x00006B23
		// (set) Token: 0x060001BF RID: 447 RVA: 0x0000892B File Offset: 0x00006B2B
		private ServerVersion VersionSupportsBackEndFullAuth { get; set; }

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x060001C0 RID: 448 RVA: 0x00008934 File Offset: 0x00006B34
		private bool BackEndFullAuthEnabled
		{
			get
			{
				return this.VersionSupportsBackEndFullAuth != null;
			}
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x00008942 File Offset: 0x00006B42
		public static IAuthBehavior CreateAuthBehavior(HttpContext httpContext)
		{
			return DefaultAuthBehavior.CreateAuthBehavior(httpContext, 0);
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x0000894C File Offset: 0x00006B4C
		public static IAuthBehavior CreateAuthBehavior(HttpContext httpContext, int serverVersion)
		{
			if (httpContext == null)
			{
				throw new ArgumentNullException("httpContext");
			}
			if (LiveIdBasicAuthBehavior.IsLiveIdBasicAuth(httpContext))
			{
				return new LiveIdBasicAuthBehavior(httpContext, serverVersion);
			}
			if (OAuthAuthBehavior.IsOAuth(httpContext))
			{
				return new OAuthAuthBehavior(httpContext, serverVersion);
			}
			if (LiveIdCookieAuthBehavior.IsLiveIdCookieAuth(httpContext))
			{
				return new LiveIdCookieAuthBehavior(httpContext, serverVersion);
			}
			if (ConsumerEasAuthBehavior.IsConsumerEasTokenAuth(httpContext))
			{
				return new ConsumerEasAuthBehavior(httpContext, serverVersion);
			}
			return new DefaultAuthBehavior(httpContext, serverVersion);
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x000089AE File Offset: 0x00006BAE
		public void SetState(int serverVersion)
		{
			this.AuthState = AuthState.FrontEndFullAuth;
			if (this.BackEndFullAuthEnabled)
			{
				this.AuthState = ((serverVersion >= this.VersionSupportsBackEndFullAuth.ToInt()) ? AuthState.BackEndFullAuth : AuthState.FrontEndContinueAuth);
			}
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x000089D7 File Offset: 0x00006BD7
		public void ResetState()
		{
			this.SetState(0);
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x0000500A File Offset: 0x0000320A
		public virtual AnchorMailbox CreateAuthModuleSpecificAnchorMailbox(IRequestContext requestContext)
		{
			return null;
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x000089E0 File Offset: 0x00006BE0
		public virtual string GetExecutingUserOrganization()
		{
			return string.Empty;
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x00003193 File Offset: 0x00001393
		public virtual bool IsFullyAuthenticated()
		{
			return true;
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x000089E7 File Offset: 0x00006BE7
		public virtual void ContinueOnAuthenticate(HttpApplication app, AsyncCallback callback)
		{
			throw new NotSupportedException();
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x000089EE File Offset: 0x00006BEE
		public virtual void SetFailureStatus()
		{
			this.HttpContext.Response.StatusCode = 401;
		}
	}
}
