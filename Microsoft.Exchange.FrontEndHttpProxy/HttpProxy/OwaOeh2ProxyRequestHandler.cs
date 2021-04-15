using System;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A8 RID: 168
	internal class OwaOeh2ProxyRequestHandler : OwaProxyRequestHandler
	{
		// Token: 0x1700013B RID: 315
		// (get) Token: 0x060005BF RID: 1471 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x0001CC35 File Offset: 0x0001AE35
		protected override Uri GetTargetBackEndServerUrl()
		{
			return UrlUtilities.FixIntegratedAuthUrlForBackEnd(base.GetTargetBackEndServerUrl());
		}
	}
}
