using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A0 RID: 160
	internal class OutlookServiceProxyRequestHandler : EwsProxyRequestHandler
	{
		// Token: 0x06000585 RID: 1413 RVA: 0x0001EF28 File Offset: 0x0001D128
		protected override Uri GetTargetBackEndServerUrl()
		{
			return new UriBuilder(base.GetTargetBackEndServerUrl())
			{
				Path = "/OutlookService/ServiceChannel.hxs",
				Query = string.Empty
			}.Uri;
		}

		// Token: 0x0400037A RID: 890
		private const string OutlookServicePath = "/OutlookService/ServiceChannel.hxs";
	}
}
