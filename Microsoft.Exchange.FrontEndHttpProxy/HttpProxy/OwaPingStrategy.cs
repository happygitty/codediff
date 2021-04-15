using System;
using System.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200003F RID: 63
	internal class OwaPingStrategy : ProtocolPingStrategyBase
	{
		// Token: 0x06000208 RID: 520 RVA: 0x0000A445 File Offset: 0x00008645
		protected override void PrepareRequest(HttpWebRequest request)
		{
			base.PrepareRequest(request);
			request.Method = "GET";
		}
	}
}
