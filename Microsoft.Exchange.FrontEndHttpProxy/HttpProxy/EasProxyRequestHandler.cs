using System;
using System.Net;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authentication;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000090 RID: 144
	internal class EasProxyRequestHandler : OwaEcpProxyRequestHandler<MobileSyncService>
	{
		// Token: 0x1700011B RID: 283
		// (get) Token: 0x060004F4 RID: 1268 RVA: 0x0001B786 File Offset: 0x00019986
		protected override string ProxyLogonUri
		{
			get
			{
				return "Proxy/";
			}
		}

		// Token: 0x1700011C RID: 284
		// (get) Token: 0x060004F5 RID: 1269 RVA: 0x0001B78D File Offset: 0x0001998D
		protected override string ProxyLogonQueryString
		{
			get
			{
				return "cmd=ProxyLogin";
			}
		}

		// Token: 0x1700011D RID: 285
		// (get) Token: 0x060004F6 RID: 1270 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x060004F7 RID: 1271 RVA: 0x0001B794 File Offset: 0x00019994
		protected override DatacenterRedirectStrategy CreateDatacenterRedirectStrategy()
		{
			return new DefaultRedirectStrategy(this);
		}

		// Token: 0x060004F8 RID: 1272 RVA: 0x0001B79C File Offset: 0x0001999C
		protected override Uri GetTargetBackEndServerUrl()
		{
			return new UriBuilder(base.GetTargetBackEndServerUrl())
			{
				Path = UrlHelper.AppendProxyToApplicationPath(base.ClientRequest.ApplicationPath, base.ClientRequest.Url.AbsolutePath)
			}.Uri;
		}

		// Token: 0x060004F9 RID: 1273 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected override void DoProtocolSpecificBeginRequestLogging()
		{
		}

		// Token: 0x060004FA RID: 1274 RVA: 0x0001B7D4 File Offset: 0x000199D4
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			IIdentity identity = base.HttpContext.User.Identity;
			if (identity is WindowsIdentity || identity is ClientSecurityContextIdentity)
			{
				headers["X-EAS-Proxy"] = IIdentityExtensions.GetSecurityIdentifier(identity).ToString() + "," + IIdentityExtensions.GetSafeName(identity, true);
			}
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x060004FB RID: 1275 RVA: 0x0001B830 File Offset: 0x00019A30
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return !string.Equals(headerName, "X-EAS-Proxy", StringComparison.OrdinalIgnoreCase) && base.ShouldCopyHeaderToServerRequest(headerName);
		}

		// Token: 0x060004FC RID: 1276 RVA: 0x0001B849 File Offset: 0x00019A49
		protected override void SetProtocolSpecificProxyLogonRequestParameters(HttpWebRequest request)
		{
			request.ContentType = "text/xml";
		}

		// Token: 0x060004FD RID: 1277 RVA: 0x0001B858 File Offset: 0x00019A58
		protected override bool TryHandleProtocolSpecificRequestErrors(Exception ex)
		{
			HttpException ex2 = ex as HttpException;
			if (ex2 != null && ex2.WebEventCode == 3004)
			{
				string text = base.ClientRequest.Headers["MS-ASProtocolVersion"];
				base.Logger.AppendGenericError("RuntimeErrorPostTooLarge", ex.ToString());
				if (!string.IsNullOrEmpty(text) && (text == "14.0" || text == "14.1"))
				{
					base.ClientResponse.StatusCode = 200;
					if (base.ClientResponse.IsClientConnected)
					{
						base.ClientResponse.ContentType = "application/vnd.ms-sync.wbxml";
						base.ClientResponse.OutputStream.Write(EasProxyRequestHandler.easRequestSizeTooLargeResponseBytes, 0, EasProxyRequestHandler.easRequestSizeTooLargeResponseBytes.Length);
					}
				}
				else
				{
					base.ClientResponse.StatusCode = 500;
				}
				base.Complete();
				return true;
			}
			return base.TryHandleProtocolSpecificRequestErrors(ex);
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected override void RedirectIfNeeded(BackEndServer mailboxServer)
		{
		}

		// Token: 0x04000341 RID: 833
		private const string ProxyHeader = "X-EAS-Proxy";

		// Token: 0x04000342 RID: 834
		private const string EASProtocolVersion = "MS-ASProtocolVersion";

		// Token: 0x04000343 RID: 835
		private const string WbXmlContentType = "application/vnd.ms-sync.wbxml";

		// Token: 0x04000344 RID: 836
		private const string EasProxyLogonUri = "Proxy/";

		// Token: 0x04000345 RID: 837
		private const string EasProxyLogonQueryString = "cmd=ProxyLogin";

		// Token: 0x04000346 RID: 838
		private static byte[] easRequestSizeTooLargeResponseBytes = new byte[]
		{
			3,
			1,
			106,
			0,
			0,
			21,
			69,
			82,
			3,
			49,
			49,
			53,
			0,
			1,
			1
		};
	}
}
