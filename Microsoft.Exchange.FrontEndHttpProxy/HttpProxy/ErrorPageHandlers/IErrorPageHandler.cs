using System;

namespace Microsoft.Exchange.HttpProxy.ErrorPageHandlers
{
	// Token: 0x020000CB RID: 203
	public interface IErrorPageHandler
	{
		// Token: 0x1700019D RID: 413
		// (get) Token: 0x06000791 RID: 1937
		string AriaDiagnosticObjectJsonString { get; }

		// Token: 0x1700019E RID: 414
		// (get) Token: 0x06000792 RID: 1938
		string ServerDiagnosticObjectJsonString { get; }

		// Token: 0x1700019F RID: 415
		// (get) Token: 0x06000793 RID: 1939
		string DiagnosticInformation { get; }

		// Token: 0x170001A0 RID: 416
		// (get) Token: 0x06000794 RID: 1940
		string ErrorHeader { get; }

		// Token: 0x170001A1 RID: 417
		// (get) Token: 0x06000795 RID: 1941
		string ErrorSubHeader { get; }

		// Token: 0x170001A2 RID: 418
		// (get) Token: 0x06000796 RID: 1942
		string ErrorDetails { get; }

		// Token: 0x170001A3 RID: 419
		// (get) Token: 0x06000797 RID: 1943
		string ErrorTitle { get; }

		// Token: 0x170001A4 RID: 420
		// (get) Token: 0x06000798 RID: 1944
		string RefreshButtonText { get; }

		// Token: 0x170001A5 RID: 421
		// (get) Token: 0x06000799 RID: 1945
		bool ShowRefreshButton { get; }

		// Token: 0x170001A6 RID: 422
		// (get) Token: 0x0600079A RID: 1946
		string ReturnUri { get; }
	}
}
