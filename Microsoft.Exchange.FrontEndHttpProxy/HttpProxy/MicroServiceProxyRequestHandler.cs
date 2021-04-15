using System;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A3 RID: 163
	internal class MicroServiceProxyRequestHandler : BEServerCookieProxyRequestHandler<WebServicesService>
	{
		// Token: 0x17000132 RID: 306
		// (get) Token: 0x0600058D RID: 1421 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}
	}
}
