using System;
using System.Web;
using Microsoft.Exchange.Clients.Owa.Core;

namespace Microsoft.Exchange.HttpProxy.ErrorPageHandlers
{
	// Token: 0x020000C9 RID: 201
	public sealed class DefaultErrorPageHandler : IErrorPageHandler
	{
		// Token: 0x06000776 RID: 1910 RVA: 0x0002B9AF File Offset: 0x00029BAF
		public DefaultErrorPageHandler(HttpRequest request)
		{
			this.errorPageRequest = request;
			this.genericErrorInfo = new GenericErrorInfo(request);
		}

		// Token: 0x17000189 RID: 393
		// (get) Token: 0x06000777 RID: 1911 RVA: 0x000089E0 File Offset: 0x00006BE0
		public string AriaDiagnosticObjectJsonString
		{
			get
			{
				return string.Empty;
			}
		}

		// Token: 0x1700018A RID: 394
		// (get) Token: 0x06000778 RID: 1912 RVA: 0x000089E0 File Offset: 0x00006BE0
		public string ServerDiagnosticObjectJsonString
		{
			get
			{
				return string.Empty;
			}
		}

		// Token: 0x1700018B RID: 395
		// (get) Token: 0x06000779 RID: 1913 RVA: 0x000089E0 File Offset: 0x00006BE0
		public string DiagnosticInformation
		{
			get
			{
				return string.Empty;
			}
		}

		// Token: 0x1700018C RID: 396
		// (get) Token: 0x0600077A RID: 1914 RVA: 0x0002B9CC File Offset: 0x00029BCC
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

		// Token: 0x1700018D RID: 397
		// (get) Token: 0x0600077B RID: 1915 RVA: 0x0002BA24 File Offset: 0x00029C24
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

		// Token: 0x1700018E RID: 398
		// (get) Token: 0x0600077C RID: 1916 RVA: 0x000089E0 File Offset: 0x00006BE0
		public string ErrorDetails
		{
			get
			{
				return string.Empty;
			}
		}

		// Token: 0x1700018F RID: 399
		// (get) Token: 0x0600077D RID: 1917 RVA: 0x0002BA7F File Offset: 0x00029C7F
		public string ErrorTitle
		{
			get
			{
				return LocalizedStrings.GetHtmlEncoded(933672694);
			}
		}

		// Token: 0x17000190 RID: 400
		// (get) Token: 0x0600077E RID: 1918 RVA: 0x0002BA8B File Offset: 0x00029C8B
		public string RefreshButtonText
		{
			get
			{
				return LocalizedStrings.GetHtmlEncoded(1939504838);
			}
		}

		// Token: 0x17000191 RID: 401
		// (get) Token: 0x0600077F RID: 1919 RVA: 0x00003193 File Offset: 0x00001393
		public bool ShowRefreshButton
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000192 RID: 402
		// (get) Token: 0x06000780 RID: 1920 RVA: 0x0002BA97 File Offset: 0x00029C97
		public string ReturnUri
		{
			get
			{
				return "/";
			}
		}

		// Token: 0x04000429 RID: 1065
		private HttpRequest errorPageRequest;

		// Token: 0x0400042A RID: 1066
		private GenericErrorInfo genericErrorInfo;
	}
}
