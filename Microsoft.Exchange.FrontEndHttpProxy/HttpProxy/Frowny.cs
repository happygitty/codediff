using System;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.HttpProxy.ErrorPageHandlers;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000060 RID: 96
	public class Frowny : OwaPage
	{
		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060002EF RID: 751 RVA: 0x0000EBEA File Offset: 0x0000CDEA
		protected bool RenderDiagnosticInfo
		{
			get
			{
				return !string.IsNullOrEmpty(this.errorPageHandler.DiagnosticInformation);
			}
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060002F0 RID: 752 RVA: 0x0000EBFF File Offset: 0x0000CDFF
		protected string DiagnosticInfo
		{
			get
			{
				return this.errorPageHandler.DiagnosticInformation;
			}
		}

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060002F1 RID: 753 RVA: 0x0000EC0C File Offset: 0x0000CE0C
		protected string ReturnUri
		{
			get
			{
				return this.errorPageHandler.ReturnUri;
			}
		}

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060002F2 RID: 754 RVA: 0x0000EC19 File Offset: 0x0000CE19
		protected bool ShowRefreshButton
		{
			get
			{
				return this.errorPageHandler.ShowRefreshButton;
			}
		}

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x060002F3 RID: 755 RVA: 0x0000EC26 File Offset: 0x0000CE26
		protected string AriaDiagnosticObjectJsonString
		{
			get
			{
				if (!string.IsNullOrEmpty(this.errorPageHandler.AriaDiagnosticObjectJsonString))
				{
					return this.errorPageHandler.AriaDiagnosticObjectJsonString;
				}
				return "null";
			}
		}

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x060002F4 RID: 756 RVA: 0x0000EC4B File Offset: 0x0000CE4B
		protected string ServerDiagnosticObjectJsonString
		{
			get
			{
				if (!string.IsNullOrEmpty(this.errorPageHandler.ServerDiagnosticObjectJsonString))
				{
					return this.errorPageHandler.ServerDiagnosticObjectJsonString;
				}
				return "null";
			}
		}

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x060002F5 RID: 757 RVA: 0x0000EC70 File Offset: 0x0000CE70
		private string ResourcePath
		{
			get
			{
				if (this.resourcePath == null)
				{
					this.resourcePath = OwaUrl.AuthFolder.ImplicitUrl;
				}
				return this.resourcePath;
			}
		}

		// Token: 0x060002F6 RID: 758 RVA: 0x0000EC90 File Offset: 0x0000CE90
		protected override void OnLoad(EventArgs e)
		{
			this.errorPageHandler = ErrorPageHandlerFactory.CreateErrorPageHandler(base.Request);
			base.Response.Headers.Set("X-Content-Type-Options", "nosniff");
			this.SetHTTPResponseStatusCode();
			if (!string.IsNullOrWhiteSpace(Environment.MachineName))
			{
				base.Response.AddHeader("X-FEServer", Environment.MachineName);
			}
			this.OnInit(e);
		}

		// Token: 0x060002F7 RID: 759 RVA: 0x0000ECF6 File Offset: 0x0000CEF6
		protected void RenderTitle()
		{
			base.Response.Write(this.errorPageHandler.ErrorTitle);
		}

		// Token: 0x060002F8 RID: 760 RVA: 0x0000ED0E File Offset: 0x0000CF0E
		protected void RenderErrorHeader()
		{
			base.Response.Write(this.errorPageHandler.ErrorHeader);
		}

		// Token: 0x060002F9 RID: 761 RVA: 0x0000ED26 File Offset: 0x0000CF26
		protected void RenderErrorSubHeader()
		{
			base.Response.Write(this.errorPageHandler.ErrorSubHeader);
		}

		// Token: 0x060002FA RID: 762 RVA: 0x0000ED3E File Offset: 0x0000CF3E
		protected void RenderRefreshButtonText()
		{
			base.Response.Write(this.errorPageHandler.RefreshButtonText);
		}

		// Token: 0x060002FB RID: 763 RVA: 0x0000ED56 File Offset: 0x0000CF56
		protected void RenderErrorDetails()
		{
			base.Response.Write(this.errorPageHandler.ErrorDetails);
		}

		// Token: 0x060002FC RID: 764 RVA: 0x0000ED6E File Offset: 0x0000CF6E
		protected void SetHTTPResponseStatusCode()
		{
			base.Response.StatusCode = 500;
		}

		// Token: 0x040001C2 RID: 450
		private IErrorPageHandler errorPageHandler;

		// Token: 0x040001C3 RID: 451
		private string resourcePath;
	}
}
