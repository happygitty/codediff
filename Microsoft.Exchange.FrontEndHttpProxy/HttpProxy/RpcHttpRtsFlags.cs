using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C6 RID: 198
	[Flags]
	internal enum RpcHttpRtsFlags
	{
		// Token: 0x04000414 RID: 1044
		None = 0,
		// Token: 0x04000415 RID: 1045
		Ping = 1,
		// Token: 0x04000416 RID: 1046
		OtherCommand = 2,
		// Token: 0x04000417 RID: 1047
		RecycleChannel = 4,
		// Token: 0x04000418 RID: 1048
		InChannel = 8,
		// Token: 0x04000419 RID: 1049
		OutChannel = 16,
		// Token: 0x0400041A RID: 1050
		EndOfFile = 32,
		// Token: 0x0400041B RID: 1051
		Echo = 64
	}
}
