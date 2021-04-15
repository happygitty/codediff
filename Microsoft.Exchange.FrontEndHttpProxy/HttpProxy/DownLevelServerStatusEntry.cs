using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200003D RID: 61
	internal class DownLevelServerStatusEntry
	{
		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000202 RID: 514 RVA: 0x0000A41B File Offset: 0x0000861B
		// (set) Token: 0x06000203 RID: 515 RVA: 0x0000A423 File Offset: 0x00008623
		public BackEndServer BackEndServer { get; set; }

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x06000204 RID: 516 RVA: 0x0000A42C File Offset: 0x0000862C
		// (set) Token: 0x06000205 RID: 517 RVA: 0x0000A434 File Offset: 0x00008634
		public bool IsHealthy { get; set; }
	}
}
