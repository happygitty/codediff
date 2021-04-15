using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200007F RID: 127
	internal class MailboxServerLocatorAsyncState
	{
		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x06000438 RID: 1080 RVA: 0x0001850D File Offset: 0x0001670D
		// (set) Token: 0x06000439 RID: 1081 RVA: 0x00018515 File Offset: 0x00016715
		public ProxyRequestHandler ProxyRequestHandler { get; set; }

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x0600043A RID: 1082 RVA: 0x0001851E File Offset: 0x0001671E
		// (set) Token: 0x0600043B RID: 1083 RVA: 0x00018526 File Offset: 0x00016726
		public AnchorMailbox AnchorMailbox { get; set; }

		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x0600043C RID: 1084 RVA: 0x0001852F File Offset: 0x0001672F
		// (set) Token: 0x0600043D RID: 1085 RVA: 0x00018537 File Offset: 0x00016737
		public MailboxServerLocator Locator { get; set; }
	}
}
