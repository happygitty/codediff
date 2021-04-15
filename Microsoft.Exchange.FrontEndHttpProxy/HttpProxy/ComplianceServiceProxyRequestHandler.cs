using System;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200008E RID: 142
	internal class ComplianceServiceProxyRequestHandler : BEServerCookieProxyRequestHandler<WebServicesService>
	{
		// Token: 0x17000119 RID: 281
		// (get) Token: 0x060004D9 RID: 1241 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}
	}
}
