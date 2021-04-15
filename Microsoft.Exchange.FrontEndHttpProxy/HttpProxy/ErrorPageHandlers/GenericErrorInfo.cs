using System;
using System.Web;
using System.Web.Security.AntiXss;

namespace Microsoft.Exchange.HttpProxy.ErrorPageHandlers
{
	// Token: 0x020000C7 RID: 199
	public sealed class GenericErrorInfo
	{
		// Token: 0x06000771 RID: 1905 RVA: 0x0002B8D0 File Offset: 0x00029AD0
		public GenericErrorInfo(HttpRequest request)
		{
			this.errorPageRequest = request;
		}

		// Token: 0x17000187 RID: 391
		// (get) Token: 0x06000772 RID: 1906 RVA: 0x0002B8DF File Offset: 0x00029ADF
		public string HttpCode
		{
			get
			{
				return this.errorPageRequest.QueryString["st"];
			}
		}

		// Token: 0x17000188 RID: 392
		// (get) Token: 0x06000773 RID: 1907 RVA: 0x0002B8F6 File Offset: 0x00029AF6
		public string ReturnUri
		{
			get
			{
				return AntiXssEncoder.HtmlEncode(this.errorPageRequest.QueryString["ru"], false);
			}
		}

		// Token: 0x0400041C RID: 1052
		public const string NotFoundHttpStatus = "404";

		// Token: 0x0400041D RID: 1053
		public const string ServiceUnavailableHttpStatus = "503";

		// Token: 0x0400041E RID: 1054
		public const string InternalServerErrorHttpStatus = "500";

		// Token: 0x0400041F RID: 1055
		public const string HttpStatusCodeQueryKey = "st";

		// Token: 0x04000420 RID: 1056
		private const string ReturnUriQueryParamKey = "ru";

		// Token: 0x04000421 RID: 1057
		private HttpRequest errorPageRequest;
	}
}
