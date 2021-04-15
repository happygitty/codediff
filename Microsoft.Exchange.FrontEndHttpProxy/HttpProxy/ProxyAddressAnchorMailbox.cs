using System;
using Microsoft.Exchange.Data.Directory;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000026 RID: 38
	internal class ProxyAddressAnchorMailbox : UserADRawEntryAnchorMailbox
	{
		// Token: 0x06000137 RID: 311 RVA: 0x00007026 File Offset: 0x00005226
		public ProxyAddressAnchorMailbox(ADRawEntry adRawEntry, IRequestContext requestContext) : base(adRawEntry, requestContext)
		{
		}

		// Token: 0x040000E7 RID: 231
		public const string SpoProxyAddressPrefix = "SPO";
	}
}
