using System;
using System.Net;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000094 RID: 148
	internal class EwsJsonProxyRequestHandler : OwaProxyRequestHandler
	{
		// Token: 0x17000123 RID: 291
		// (get) Token: 0x06000521 RID: 1313 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x06000522 RID: 1314 RVA: 0x0001C9C8 File Offset: 0x0001ABC8
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

		// Token: 0x06000523 RID: 1315 RVA: 0x0001CA5C File Offset: 0x0001AC5C
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return !string.Equals(headerName, "X-OWA-ProxyUri", StringComparison.OrdinalIgnoreCase) && base.ShouldCopyHeaderToServerRequest(headerName);
		}

		// Token: 0x06000524 RID: 1316 RVA: 0x0001CA75 File Offset: 0x0001AC75
		protected override Uri GetTargetBackEndServerUrl()
		{
			return UrlUtilities.FixIntegratedAuthUrlForBackEnd(base.GetTargetBackEndServerUrl());
		}

		// Token: 0x0400035A RID: 858
		private const string LiveIdPuid = "RPSPUID";

		// Token: 0x0400035B RID: 859
		private const string OrgIdPuid = "RPSOrgIdPUID";
	}
}
