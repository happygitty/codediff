using System;
using System.Net;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200009E RID: 158
	internal class O365SuiteServiceProxyRequestHandler : BEServerCookieProxyRequestHandler<WebServicesService>
	{
		// Token: 0x17000130 RID: 304
		// (get) Token: 0x06000578 RID: 1400 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x06000579 RID: 1401 RVA: 0x0001E87C File Offset: 0x0001CA7C
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			headers["RPSPUID"] = (string)base.HttpContext.Items["RPSPUID"];
			headers["RPSOrgIdPUID"] = (string)base.HttpContext.Items["RPSOrgIdPUID"];
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x0600057A RID: 1402 RVA: 0x0001E8DA File Offset: 0x0001CADA
		protected override Uri GetTargetBackEndServerUrl()
		{
			return UrlUtilities.FixIntegratedAuthUrlForBackEnd(base.GetTargetBackEndServerUrl());
		}
	}
}
