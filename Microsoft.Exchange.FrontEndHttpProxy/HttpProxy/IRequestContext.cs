using System;
using System.Web;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200007C RID: 124
	internal interface IRequestContext
	{
		// Token: 0x170000ED RID: 237
		// (get) Token: 0x0600042E RID: 1070
		HttpContext HttpContext { get; }

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x0600042F RID: 1071
		RequestDetailsLogger Logger { get; }

		// Token: 0x170000EF RID: 239
		// (get) Token: 0x06000430 RID: 1072
		LatencyTracker LatencyTracker { get; }

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x06000431 RID: 1073
		int TraceContext { get; }

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x06000432 RID: 1074
		Guid ActivityId { get; }

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x06000433 RID: 1075
		IAuthBehavior AuthBehavior { get; }
	}
}
