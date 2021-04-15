using System;
using System.Web;
using System.Web.Security.AntiXss;

namespace Microsoft.Exchange.HttpProxy.ErrorPageHandlers
{
	// Token: 0x020000C8 RID: 200
	public sealed class GenericErrorInfo
	{
		// Token: 0x06000776 RID: 1910 RVA: 0x0002B6E8 File Offset: 0x000298E8
		public GenericErrorInfo(HttpRequest request)
		{
			this.errorPageRequest = request;
		}

		// Token: 0x17000189 RID: 393
		// (get) Token: 0x06000777 RID: 1911 RVA: 0x0002B6F7 File Offset: 0x000298F7
		public string HttpCode
		{
			get
			{
				return this.errorPageRequest.QueryString["st"];
			}
		}

		// Token: 0x1700018A RID: 394
		// (get) Token: 0x06000778 RID: 1912 RVA: 0x0002B70E File Offset: 0x0002990E
		public string ReturnUri
		{
			get
			{
				return AntiXssEncoder.HtmlEncode(this.errorPageRequest.QueryString["ru"], false);
			}
		}

		// Token: 0x04000418 RID: 1048
		public const string NotFoundHttpStatus = "404";

		// Token: 0x04000419 RID: 1049
		public const string ServiceUnavailableHttpStatus = "503";

		// Token: 0x0400041A RID: 1050
		public const string InternalServerErrorHttpStatus = "500";

		// Token: 0x0400041B RID: 1051
		public const string HttpStatusCodeQueryKey = "st";

		// Token: 0x0400041C RID: 1052
		private const string ReturnUriQueryParamKey = "ru";

		// Token: 0x0400041D RID: 1053
		private HttpRequest errorPageRequest;
	}
}
