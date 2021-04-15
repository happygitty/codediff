using System;
using System.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000059 RID: 89
	[Serializable]
	internal class CrossResourceForestProxyNotAllowedException : HttpProxyException
	{
		// Token: 0x060002D8 RID: 728 RVA: 0x0000E9FC File Offset: 0x0000CBFC
		public CrossResourceForestProxyNotAllowedException(string targetFQDN) : base(HttpStatusCode.ServiceUnavailable, 6001, string.Format("Cross forest proxy to {0} is blocked.", targetFQDN))
		{
		}
	}
}
