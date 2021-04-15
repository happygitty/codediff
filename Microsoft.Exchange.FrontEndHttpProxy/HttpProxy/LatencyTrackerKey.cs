using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000088 RID: 136
	internal enum LatencyTrackerKey
	{
		// Token: 0x0400031D RID: 797
		CalculateTargetBackEndLatency,
		// Token: 0x0400031E RID: 798
		CalculateTargetBackEndSecondRoundLatency,
		// Token: 0x0400031F RID: 799
		HandlerToModuleSwitchingLatency,
		// Token: 0x04000320 RID: 800
		ModuleToHandlerSwitchingLatency,
		// Token: 0x04000321 RID: 801
		RequestHandlerLatency,
		// Token: 0x04000322 RID: 802
		ProxyModuleInitLatency,
		// Token: 0x04000323 RID: 803
		ProxyModuleLatency,
		// Token: 0x04000324 RID: 804
		AuthenticationLatency,
		// Token: 0x04000325 RID: 805
		BackendRequestInitLatency,
		// Token: 0x04000326 RID: 806
		BackendProcessingLatency,
		// Token: 0x04000327 RID: 807
		BackendResponseInitLatency,
		// Token: 0x04000328 RID: 808
		HandlerCompletionLatency,
		// Token: 0x04000329 RID: 809
		StreamingLatency,
		// Token: 0x0400032A RID: 810
		RouteRefresherLatency
	}
}
