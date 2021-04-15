using System;
using System.Web;
using Microsoft.Exchange.Net.Protocols;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000BD RID: 189
	internal class MessageTrackingRequestHandler : EwsProxyRequestHandler
	{
		// Token: 0x06000745 RID: 1861 RVA: 0x0002AA51 File Offset: 0x00028C51
		internal static bool IsMessageTrackingRequest(HttpRequest request)
		{
			return request.UserAgent != null && request.UserAgent.StartsWith(MessageTrackingRequestHandler.MessageTrackingUserAgentString);
		}

		// Token: 0x06000746 RID: 1862 RVA: 0x0002AA6D File Offset: 0x00028C6D
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			return new LocalSiteAnchorMailbox(this);
		}

		// Token: 0x040003FB RID: 1019
		private static readonly string MessageTrackingUserAgentString = WellKnownUserAgent.GetEwsNegoAuthUserAgent("Microsoft.Exchange.InfoWorker.Common.MessageTracking");
	}
}
