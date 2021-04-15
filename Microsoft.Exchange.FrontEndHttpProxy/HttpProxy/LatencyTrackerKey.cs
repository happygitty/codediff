using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000088 RID: 136
	internal enum LatencyTrackerKey
	{
		// Token: 0x04000319 RID: 793
		CalculateTargetBackEndLatency,
		// Token: 0x0400031A RID: 794
		CalculateTargetBackEndSecondRoundLatency,
		// Token: 0x0400031B RID: 795
		HandlerToModuleSwitchingLatency,
		// Token: 0x0400031C RID: 796
		ModuleToHandlerSwitchingLatency,
		// Token: 0x0400031D RID: 797
		RequestHandlerLatency,
		// Token: 0x0400031E RID: 798
		ProxyModuleInitLatency,
		// Token: 0x0400031F RID: 799
		ProxyModuleLatency,
		// Token: 0x04000320 RID: 800
		AuthenticationLatency,
		// Token: 0x04000321 RID: 801
		BackendRequestInitLatency,
		// Token: 0x04000322 RID: 802
		BackendProcessingLatency,
		// Token: 0x04000323 RID: 803
		BackendResponseInitLatency,
		// Token: 0x04000324 RID: 804
		HandlerCompletionLatency,
		// Token: 0x04000325 RID: 805
		StreamingLatency,
		// Token: 0x04000326 RID: 806
		RouteRefresherLatency
	}
}
