using System;
using System.Net;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000094 RID: 148
	internal class EwsJsonProxyRequestHandler : OwaProxyRequestHandler
	{
		// Token: 0x17000123 RID: 291
		// (get) Token: 0x06000525 RID: 1317 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x06000526 RID: 1318 RVA: 0x0001CB88 File Offset: 0x0001AD88
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			headers["RPSPUID"] = (string)base.HttpContext.Items["RPSPUID"];
			headers["RPSOrgIdPUID"] = (string)base.HttpContext.Items["RPSOrgIdPUID"];
			base.AddProtocolSpecificHeadersToServerRequest(headers);
			if (base.ClientRequest != null && string.Equals(base.ClientRequest.QueryString["action"], "GetWacIframeUrl", StringComparison.OrdinalIgnoreCase))
			{
				OwaProxyRequestHandler.AddProxyUriHeader(base.ClientRequest, headers);
			}
		}

		// Token: 0x06000527 RID: 1319 RVA: 0x0001CC1C File Offset: 0x0001AE1C
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return !string.Equals(headerName, "X-OWA-ProxyUri", StringComparison.OrdinalIgnoreCase) && base.ShouldCopyHeaderToServerRequest(headerName);
		}

		// Token: 0x06000528 RID: 1320 RVA: 0x0001CC35 File Offset: 0x0001AE35
		protected override Uri GetTargetBackEndServerUrl()
		{
			return UrlUtilities.FixIntegratedAuthUrlForBackEnd(base.GetTargetBackEndServerUrl());
		}

		// Token: 0x0400035E RID: 862
		private const string LiveIdPuid = "RPSPUID";

		// Token: 0x0400035F RID: 863
		private const string OrgIdPuid = "RPSOrgIdPUID";
	}
}
