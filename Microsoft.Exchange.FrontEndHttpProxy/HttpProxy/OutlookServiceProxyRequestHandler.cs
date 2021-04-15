using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A0 RID: 160
	internal class OutlookServiceProxyRequestHandler : EwsProxyRequestHandler
	{
		// Token: 0x06000588 RID: 1416 RVA: 0x0001F0CC File Offset: 0x0001D2CC
		protected override Uri GetTargetBackEndServerUrl()
		{
			return new UriBuilder(base.GetTargetBackEndServerUrl())
			{
				Path = "/OutlookService/ServiceChannel.hxs",
				Query = string.Empty
			}.Uri;
		}

		// Token: 0x0400037E RID: 894
		private const string OutlookServicePath = "/OutlookService/ServiceChannel.hxs";
	}
}
