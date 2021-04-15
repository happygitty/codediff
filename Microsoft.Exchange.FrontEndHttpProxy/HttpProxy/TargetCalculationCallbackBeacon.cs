using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000081 RID: 129
	internal class TargetCalculationCallbackBeacon
	{
		// Token: 0x0600043B RID: 1083 RVA: 0x00018380 File Offset: 0x00016580
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

		// Token: 0x0600043C RID: 1084 RVA: 0x000183B5 File Offset: 0x000165B5
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

		// Token: 0x0600043D RID: 1085 RVA: 0x000183F0 File Offset: 0x000165F0
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
		// (get) Token: 0x0600043E RID: 1086 RVA: 0x00018445 File Offset: 0x00016645
		// (set) Token: 0x0600043F RID: 1087 RVA: 0x0001844D File Offset: 0x0001664D
		public TargetCalculationCallbackState State { get; private set; }

		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x06000440 RID: 1088 RVA: 0x00018456 File Offset: 0x00016656
		// (set) Token: 0x06000441 RID: 1089 RVA: 0x0001845E File Offset: 0x0001665E
		public AnchoredRoutingTarget AnchoredRoutingTarget { get; private set; }

		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x06000442 RID: 1090 RVA: 0x00018467 File Offset: 0x00016667
		// (set) Token: 0x06000443 RID: 1091 RVA: 0x0001846F File Offset: 0x0001666F
		public AnchorMailbox AnchorMailbox { get; private set; }

		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x06000444 RID: 1092 RVA: 0x00018478 File Offset: 0x00016678
		// (set) Token: 0x06000445 RID: 1093 RVA: 0x00018480 File Offset: 0x00016680
		public BackEndServer MailboxServer { get; private set; }

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x06000446 RID: 1094 RVA: 0x00018489 File Offset: 0x00016689
		// (set) Token: 0x06000447 RID: 1095 RVA: 0x00018491 File Offset: 0x00016691
		public MailboxServerLocatorAsyncState MailboxServerLocatorAsyncState { get; private set; }

		// Token: 0x170000FB RID: 251
		// (get) Token: 0x06000448 RID: 1096 RVA: 0x0001849A File Offset: 0x0001669A
		// (set) Token: 0x06000449 RID: 1097 RVA: 0x000184A2 File Offset: 0x000166A2
		public IAsyncResult MailboxServerLocatorAsyncResult { get; private set; }
	}
}
