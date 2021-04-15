using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000081 RID: 129
	internal class TargetCalculationCallbackBeacon
	{
		// Token: 0x0600043F RID: 1087 RVA: 0x00018540 File Offset: 0x00016740
		public TargetCalculationCallbackBeacon(AnchoredRoutingTarget anchoredRoutingTarget)
		{
			if (anchoredRoutingTarget == null)
			{
				throw new ArgumentNullException("anchoredRoutingTarget");
			}
			this.AnchoredRoutingTarget = anchoredRoutingTarget;
			this.AnchorMailbox = this.AnchoredRoutingTarget.AnchorMailbox;
			this.State = TargetCalculationCallbackState.TargetResolved;
		}

		// Token: 0x06000440 RID: 1088 RVA: 0x00018575 File Offset: 0x00016775
		public TargetCalculationCallbackBeacon(AnchorMailbox anchorMailbox, BackEndServer mailboxServer)
		{
			if (anchorMailbox == null)
			{
				throw new ArgumentNullException("anchorMailbox");
			}
			if (mailboxServer == null)
			{
				throw new ArgumentNullException("mailboxServer");
			}
			this.AnchorMailbox = anchorMailbox;
			this.MailboxServer = mailboxServer;
			this.State = TargetCalculationCallbackState.MailboxServerResolved;
		}

		// Token: 0x06000441 RID: 1089 RVA: 0x000185B0 File Offset: 0x000167B0
		public TargetCalculationCallbackBeacon(MailboxServerLocatorAsyncState mailboxServerLocatorAsyncState, IAsyncResult mailboxServerLocatorAsyncResult)
		{
			if (mailboxServerLocatorAsyncState == null)
			{
				throw new ArgumentNullException("mailboxServerLocatorAsyncState");
			}
			if (mailboxServerLocatorAsyncResult == null)
			{
				throw new ArgumentNullException("mailboxServerLocatorAsyncResult");
			}
			this.MailboxServerLocatorAsyncState = mailboxServerLocatorAsyncState;
			this.MailboxServerLocatorAsyncResult = mailboxServerLocatorAsyncResult;
			this.AnchorMailbox = this.MailboxServerLocatorAsyncState.AnchorMailbox;
			this.State = TargetCalculationCallbackState.LocatorCallback;
		}

		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x06000442 RID: 1090 RVA: 0x00018605 File Offset: 0x00016805
		// (set) Token: 0x06000443 RID: 1091 RVA: 0x0001860D File Offset: 0x0001680D
		public TargetCalculationCallbackState State { get; private set; }

		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x06000444 RID: 1092 RVA: 0x00018616 File Offset: 0x00016816
		// (set) Token: 0x06000445 RID: 1093 RVA: 0x0001861E File Offset: 0x0001681E
		public AnchoredRoutingTarget AnchoredRoutingTarget { get; private set; }

		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x06000446 RID: 1094 RVA: 0x00018627 File Offset: 0x00016827
		// (set) Token: 0x06000447 RID: 1095 RVA: 0x0001862F File Offset: 0x0001682F
		public AnchorMailbox AnchorMailbox { get; private set; }

		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x06000448 RID: 1096 RVA: 0x00018638 File Offset: 0x00016838
		// (set) Token: 0x06000449 RID: 1097 RVA: 0x00018640 File Offset: 0x00016840
		public BackEndServer MailboxServer { get; private set; }

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x0600044A RID: 1098 RVA: 0x00018649 File Offset: 0x00016849
		// (set) Token: 0x0600044B RID: 1099 RVA: 0x00018651 File Offset: 0x00016851
		public MailboxServerLocatorAsyncState MailboxServerLocatorAsyncState { get; private set; }

		// Token: 0x170000FB RID: 251
		// (get) Token: 0x0600044C RID: 1100 RVA: 0x0001865A File Offset: 0x0001685A
		// (set) Token: 0x0600044D RID: 1101 RVA: 0x00018662 File Offset: 0x00016862
		public IAsyncResult MailboxServerLocatorAsyncResult { get; private set; }
	}
}
