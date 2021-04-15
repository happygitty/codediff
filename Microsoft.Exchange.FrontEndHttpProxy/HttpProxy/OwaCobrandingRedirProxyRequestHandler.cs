using System;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A5 RID: 165
	internal class OwaCobrandingRedirProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x060005B3 RID: 1459 RVA: 0x0001FAFF File Offset: 0x0001DCFF
		internal static bool IsCobrandingRedirRequest(HttpRequest request)
		{
			return request.Url.LocalPath.EndsWith("cobrandingredir.aspx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060005B4 RID: 1460 RVA: 0x0001FB18 File Offset: 0x0001DD18
		protected override AnchoredRoutingTarget TryDirectTargetCalculation()
		{
			BackEndServer randomDownLevelClientAccessServer = DownLevelServerManager.Instance.GetRandomDownLevelClientAccessServer();
			return new AnchoredRoutingTarget(new AnonymousAnchorMailbox(this), randomDownLevelClientAccessServer);
		}
	}
}
