using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200007F RID: 127
	internal class MailboxServerLocatorAsyncState
	{
		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x06000434 RID: 1076 RVA: 0x0001834D File Offset: 0x0001654D
		// (set) Token: 0x06000435 RID: 1077 RVA: 0x00018355 File Offset: 0x00016555
		public ProxyRequestHandler ProxyRequestHandler { get; set; }

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x06000436 RID: 1078 RVA: 0x0001835E File Offset: 0x0001655E
		// (set) Token: 0x06000437 RID: 1079 RVA: 0x00018366 File Offset: 0x00016566
		public AnchorMailbox AnchorMailbox { get; set; }

		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x06000438 RID: 1080 RVA: 0x0001836F File Offset: 0x0001656F
		// (set) Token: 0x06000439 RID: 1081 RVA: 0x00018377 File Offset: 0x00016577
		public MailboxServerLocator Locator { get; set; }
	}
}
