using System;
using System.Web;
using Microsoft.Exchange.Clients.Owa.Core;

namespace Microsoft.Exchange.HttpProxy.ErrorPageHandlers
{
	// Token: 0x020000CA RID: 202
	public sealed class DefaultErrorPageHandler : IErrorPageHandler
	{
		// Token: 0x0600077B RID: 1915 RVA: 0x0002B7C7 File Offset: 0x000299C7
		public DefaultErrorPageHandler(HttpRequest request)
		{
			this.errorPageRequest = request;
			this.genericErrorInfo = new GenericErrorInfo(request);
		}

		// Token: 0x1700018B RID: 395
		// (get) Token: 0x0600077C RID: 1916 RVA: 0x000089E0 File Offset: 0x00006BE0
		public string AriaDiagnosticObjectJsonString
		{
			get
			{
				return string.Empty;
			}
		}

		// Token: 0x1700018C RID: 396
		// (get) Token: 0x0600077D RID: 1917 RVA: 0x000089E0 File Offset: 0x00006BE0
		public string ServerDiagnosticObjectJsonString
		{
			get
			{
				return string.Empty;
			}
		}

		// Token: 0x1700018D RID: 397
		// (get) Token: 0x0600077E RID: 1918 RVA: 0x000089E0 File Offset: 0x00006BE0
		public string DiagnosticInformation
		{
			get
			{
				return string.Empty;
			}
		}

		// Token: 0x1700018E RID: 398
		// (get) Token: 0x0600077F RID: 1919 RVA: 0x0002B7E4 File Offset: 0x000299E4
		public string ErrorHeader
		{
			get
			{
				if (this.genericErrorInfo.HttpCode == "404")
				{
					return LocalizedStrings.GetHtmlEncoded(-392503097);
				}
				if (this.genericErrorInfo.HttpCode == "500")
				{
					return LocalizedStrings.GetHtmlEncoded(629133816);
				}
				return string.Empty;
			}
		}

		// Token: 0x1700018F RID: 399
		// (get) Token: 0x06000780 RID: 1920 RVA: 0x0002B83C File Offset: 0x00029A3C
		public string ErrorSubHeader
		{
			get
			{
				if (this.genericErrorInfo.HttpCode == "404")
				{
					return LocalizedStrings.GetHtmlEncoded(1252002283);
				}
				if (this.genericErrorInfo.HttpCode == "503")
				{
					return LocalizedStrings.GetHtmlEncoded(1252002321);
				}
				return LocalizedStrings.GetHtmlEncoded(1252002318);
			}
		}

		// Token: 0x17000190 RID: 400
		// (get) Token: 0x06000781 RID: 1921 RVA: 0x000089E0 File Offset: 0x00006BE0
		public string ErrorDetails
		{
			get
			{
				return string.Empty;
			}
		}

		// Token: 0x17000191 RID: 401
		// (get) Token: 0x06000782 RID: 1922 RVA: 0x0002B897 File Offset: 0x00029A97
		public string ErrorTitle
		{
			get
			{
				return LocalizedStrings.GetHtmlEncoded(933672694);
			}
		}

		// Token: 0x17000192 RID: 402
		// (get) Token: 0x06000783 RID: 1923 RVA: 0x0002B8A3 File Offset: 0x00029AA3
		public string RefreshButtonText
		{
			get
			{
				return LocalizedStrings.GetHtmlEncoded(1939504838);
			}
		}

		// Token: 0x17000193 RID: 403
		// (get) Token: 0x06000784 RID: 1924 RVA: 0x00003193 File Offset: 0x00001393
		public bool ShowRefreshButton
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000194 RID: 404
		// (get) Token: 0x06000785 RID: 1925 RVA: 0x0002B8AF File Offset: 0x00029AAF
		public string ReturnUri
		{
			get
			{
				return "/";
			}
		}

		// Token: 0x04000425 RID: 1061
		private HttpRequest errorPageRequest;

		// Token: 0x04000426 RID: 1062
		private GenericErrorInfo genericErrorInfo;
	}
}
