using System;
using System.Web;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200007C RID: 124
	internal interface IRequestContext
	{
		// Token: 0x170000ED RID: 237
		// (get) Token: 0x0600042A RID: 1066
		HttpContext HttpContext { get; }

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x0600042B RID: 1067
		RequestDetailsLogger Logger { get; }

		// Token: 0x170000EF RID: 239
		// (get) Token: 0x0600042C RID: 1068
		LatencyTracker LatencyTracker { get; }

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x0600042D RID: 1069
		int TraceContext { get; }

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x0600042E RID: 1070
		Guid ActivityId { get; }

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x0600042F RID: 1071
		IAuthBehavior AuthBehavior { get; }
	}
}
