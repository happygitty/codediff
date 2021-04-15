using System;

namespace Microsoft.Exchange.HttpProxy.ErrorPageHandlers
{
	// Token: 0x020000CC RID: 204
	public interface IErrorPageHandler
	{
		// Token: 0x1700019F RID: 415
		// (get) Token: 0x06000796 RID: 1942
		string AriaDiagnosticObjectJsonString { get; }

		// Token: 0x170001A0 RID: 416
		// (get) Token: 0x06000797 RID: 1943
		string ServerDiagnosticObjectJsonString { get; }

		// Token: 0x170001A1 RID: 417
		// (get) Token: 0x06000798 RID: 1944
		string DiagnosticInformation { get; }

		// Token: 0x170001A2 RID: 418
		// (get) Token: 0x06000799 RID: 1945
		string ErrorHeader { get; }

		// Token: 0x170001A3 RID: 419
		// (get) Token: 0x0600079A RID: 1946
		string ErrorSubHeader { get; }

		// Token: 0x170001A4 RID: 420
		// (get) Token: 0x0600079B RID: 1947
		string ErrorDetails { get; }

		// Token: 0x170001A5 RID: 421
		// (get) Token: 0x0600079C RID: 1948
		string ErrorTitle { get; }

		// Token: 0x170001A6 RID: 422
		// (get) Token: 0x0600079D RID: 1949
		string RefreshButtonText { get; }

		// Token: 0x170001A7 RID: 423
		// (get) Token: 0x0600079E RID: 1950
		bool ShowRefreshButton { get; }

		// Token: 0x170001A8 RID: 424
		// (get) Token: 0x0600079F RID: 1951
		string ReturnUri { get; }
	}
}
