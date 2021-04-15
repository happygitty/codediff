using System;
using System.Net;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000098 RID: 152
	internal class OwaDownloadProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x17000127 RID: 295
		// (get) Token: 0x0600053D RID: 1341 RVA: 0x0001D136 File Offset: 0x0001B336
		// (set) Token: 0x0600053E RID: 1342 RVA: 0x0001D13E File Offset: 0x0001B33E
		protected string ExplicitSignOnAddress { get; set; }

		// Token: 0x0600053F RID: 1343 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool ShouldBackendRequestBeAnonymous()
		{
			return true;
		}

		// Token: 0x06000540 RID: 1344 RVA: 0x0001D147 File Offset: 0x0001B347
		protected override DatacenterRedirectStrategy CreateDatacenterRedirectStrategy()
		{
			return new OwaEcpRedirectStrategy(this);
		}

		// Token: 0x06000541 RID: 1345 RVA: 0x0001D150 File Offset: 0x0001B350
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			this.ExplicitSignOnAddress = this.GetExplicitLogonAddress();
			if (string.IsNullOrEmpty(this.ExplicitSignOnAddress))
			{
				throw new HttpProxyException(HttpStatusCode.NotFound, 0, "Mailbox not specified.");
			}
			if (!SmtpAddress.IsValidSmtpAddress(this.ExplicitSignOnAddress))
			{
				throw new HttpProxyException(HttpStatusCode.NotFound, 0, "Mailbox not valid.");
			}
			base.Logger.Set(3, "ExplicitLogon-SMTP");
			return new SmtpAnchorMailbox(this.ExplicitSignOnAddress, this);
		}

		// Token: 0x06000542 RID: 1346 RVA: 0x00019686 File Offset: 0x00017886
		protected override bool IsRoutingError(HttpWebResponse response)
		{
			return OwaProxyRequestHandler.IsRoutingErrorFromOWA(this, response) || base.IsRoutingError(response);
		}

		// Token: 0x06000543 RID: 1347 RVA: 0x0001D1C8 File Offset: 0x0001B3C8
		private string GetExplicitLogonAddress()
		{
			string text = null;
			if (UrlUtilities.TryGetExplicitLogonUser(base.ClientRequest, ref text) && ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[OwaDownloadProxyRequestHandler::GetExplicitLogonAddress]: Context {0}; candidate explicit logon address: {1}", base.TraceContext, text);
			}
			return text;
		}
	}
}
