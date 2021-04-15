using System;
using System.Net;
using System.Web;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000BE RID: 190
	internal class PsgwProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x17000184 RID: 388
		// (get) Token: 0x06000749 RID: 1865 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool ProxyKerberosAuthentication
		{
			get
			{
				return true;
			}
		}

		// Token: 0x0600074A RID: 1866 RVA: 0x0002AA88 File Offset: 0x00028C88
		public static bool IsPsgwRequest(HttpRequest request)
		{
			return !string.IsNullOrEmpty(request.Url.AbsolutePath) && (string.Compare(request.Url.AbsolutePath, "/psgw", StringComparison.OrdinalIgnoreCase) == 0 || request.Url.AbsolutePath.IndexOf("/psgw/", StringComparison.OrdinalIgnoreCase) == 0);
		}

		// Token: 0x0600074B RID: 1867 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected override void OnInitializingHandler()
		{
		}

		// Token: 0x0600074C RID: 1868 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
		}

		// Token: 0x0600074D RID: 1869 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return true;
		}

		// Token: 0x0600074E RID: 1870 RVA: 0x0002AADC File Offset: 0x00028CDC
		protected override Uri GetTargetBackEndServerUrl()
		{
			UriBuilder uriBuilder = new UriBuilder(base.ClientRequest.Url);
			if (uriBuilder.Path.EndsWith("/healthchecktarget.htm"))
			{
				uriBuilder.Path = "/powershell/healthcheck.htm";
			}
			else
			{
				uriBuilder.Path = "/powershell";
			}
			return uriBuilder.Uri;
		}
	}
}
