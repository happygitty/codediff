using System;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A8 RID: 168
	internal class OwaOeh2ProxyRequestHandler : OwaProxyRequestHandler
	{
		// Token: 0x1700013B RID: 315
		// (get) Token: 0x060005BC RID: 1468 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x060005BD RID: 1469 RVA: 0x0001CA75 File Offset: 0x0001AC75
		protected override Uri GetTargetBackEndServerUrl()
		{
			return UrlUtilities.FixIntegratedAuthUrlForBackEnd(base.GetTargetBackEndServerUrl());
		}
	}
}
