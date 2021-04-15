using System;
using System.Net;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200009E RID: 158
	internal class O365SuiteServiceProxyRequestHandler : BEServerCookieProxyRequestHandler<WebServicesService>
	{
		// Token: 0x17000130 RID: 304
		// (get) Token: 0x0600057B RID: 1403 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x0600057C RID: 1404 RVA: 0x0001EA20 File Offset: 0x0001CC20
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			headers["RPSPUID"] = (string)base.HttpContext.Items["RPSPUID"];
			headers["RPSOrgIdPUID"] = (string)base.HttpContext.Items["RPSOrgIdPUID"];
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x0600057D RID: 1405 RVA: 0x0001EA7E File Offset: 0x0001CC7E
		protected override Uri GetTargetBackEndServerUrl()
		{
			return UrlUtilities.FixIntegratedAuthUrlForBackEnd(base.GetTargetBackEndServerUrl());
		}
	}
}
