using System;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A5 RID: 165
	internal class OwaCobrandingRedirProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x060005B0 RID: 1456 RVA: 0x0001F95B File Offset: 0x0001DB5B
		internal static bool IsCobrandingRedirRequest(HttpRequest request)
		{
			return request.Url.LocalPath.EndsWith("cobrandingredir.aspx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060005B1 RID: 1457 RVA: 0x0001F974 File Offset: 0x0001DB74
		protected override AnchoredRoutingTarget TryDirectTargetCalculation()
		{
			BackEndServer randomDownLevelClientAccessServer = DownLevelServerManager.Instance.GetRandomDownLevelClientAccessServer();
			return new AnchoredRoutingTarget(new AnonymousAnchorMailbox(this), randomDownLevelClientAccessServer);
		}
	}
}
