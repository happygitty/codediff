using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C7 RID: 199
	[Flags]
	internal enum RpcHttpRtsFlags
	{
		// Token: 0x04000410 RID: 1040
		None = 0,
		// Token: 0x04000411 RID: 1041
		Ping = 1,
		// Token: 0x04000412 RID: 1042
		OtherCommand = 2,
		// Token: 0x04000413 RID: 1043
		RecycleChannel = 4,
		// Token: 0x04000414 RID: 1044
		InChannel = 8,
		// Token: 0x04000415 RID: 1045
		OutChannel = 16,
		// Token: 0x04000416 RID: 1046
		EndOfFile = 32,
		// Token: 0x04000417 RID: 1047
		Echo = 64
	}
}
