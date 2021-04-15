using System;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B9 RID: 185
	internal class SpeechRecoProxyRequestHandler : BEServerCookieProxyRequestHandler<WebServicesService>
	{
		// Token: 0x17000181 RID: 385
		// (get) Token: 0x0600073A RID: 1850 RVA: 0x000199DA File Offset: 0x00017BDA
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 2;
			}
		}
	}
}
