using System;
using System.Web;
using Microsoft.Exchange.Net.Protocols;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000BD RID: 189
	internal class MessageTrackingRequestHandler : EwsProxyRequestHandler
	{
		// Token: 0x06000747 RID: 1863 RVA: 0x0002ACDD File Offset: 0x00028EDD
		internal static bool IsMessageTrackingRequest(HttpRequest request)
		{
			return request.UserAgent != null && request.UserAgent.StartsWith(MessageTrackingRequestHandler.MessageTrackingUserAgentString);
		}

		// Token: 0x06000748 RID: 1864 RVA: 0x0002ACF9 File Offset: 0x00028EF9
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			return new LocalSiteAnchorMailbox(this);
		}

		// Token: 0x040003FF RID: 1023
		private static readonly string MessageTrackingUserAgentString = WellKnownUserAgent.GetEwsNegoAuthUserAgent("Microsoft.Exchange.InfoWorker.Common.MessageTracking");
	}
}
