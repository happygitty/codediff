using System;
using System.Globalization;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000AA RID: 170
	internal class OwaResourceProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x17000140 RID: 320
		// (get) Token: 0x060005DA RID: 1498 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool WillAddProtocolSpecificCookiesToClientResponse
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060005DB RID: 1499 RVA: 0x00020B40 File Offset: 0x0001ED40
		internal static bool CanHandle(HttpRequest httpRequest)
		{
			HttpCookie httpCookie = httpRequest.Cookies[Constants.AnonResource];
			return httpCookie != null && string.Compare(httpCookie.Value, "true", CultureInfo.InvariantCulture, CompareOptions.IgnoreCase) == 0 && BEResourceRequestHandler.IsResourceRequest(httpRequest.Url.LocalPath);
		}

		// Token: 0x060005DC RID: 1500 RVA: 0x00020B90 File Offset: 0x0001ED90
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			HttpCookie httpCookie = base.ClientRequest.Cookies[Constants.AnonResourceBackend];
			if (httpCookie != null)
			{
				this.savedBackendServer = httpCookie.Value;
			}
			if (!string.IsNullOrEmpty(this.savedBackendServer))
			{
				base.Logger.Set(3, Constants.AnonResourceBackend + "-Cookie");
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<HttpCookie, int>((long)this.GetHashCode(), "[OwaResourceProxyRequestHandler::ResolveAnchorMailbox]: AnonResourceBackend cookie used: {0}; context {1}.", httpCookie, base.TraceContext);
				}
				return new ServerInfoAnchorMailbox(BackEndServer.FromString(this.savedBackendServer), this);
			}
			return new AnonymousAnchorMailbox(this);
		}

		// Token: 0x060005DD RID: 1501 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool ShouldBackendRequestBeAnonymous()
		{
			return true;
		}

		// Token: 0x060005DE RID: 1502 RVA: 0x00020C34 File Offset: 0x0001EE34
		protected override void CopySupplementalCookiesToClientResponse()
		{
			string text = null;
			if (base.AnchoredRoutingTarget != null && base.AnchoredRoutingTarget.BackEndServer != null)
			{
				text = base.AnchoredRoutingTarget.BackEndServer.ToString();
			}
			if (!string.IsNullOrEmpty(text) && this.savedBackendServer != text)
			{
				HttpCookie httpCookie = new HttpCookie(Constants.AnonResourceBackend, text);
				httpCookie.HttpOnly = true;
				httpCookie.Secure = base.ClientRequest.IsSecureConnection;
				base.ClientResponse.Cookies.Add(httpCookie);
			}
			base.CopySupplementalCookiesToClientResponse();
		}

		// Token: 0x0400039E RID: 926
		private string savedBackendServer;
	}
}
