using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000016 RID: 22
	internal class AnchoredRoutingTarget
	{
		// Token: 0x060000B4 RID: 180 RVA: 0x00004E6C File Offset: 0x0000306C
		public AnchoredRoutingTarget(AnchorMailbox anchorMailbox, BackEndServer backendServer)
		{
			if (anchorMailbox == null)
			{
				throw new ArgumentNullException("anchorMailbox");
			}
			if (backendServer == null)
			{
				throw new ArgumentNullException("backendServer");
			}
			this.AnchorMailbox = anchorMailbox;
			this.BackEndServer = backendServer;
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00004E9E File Offset: 0x0000309E
		public AnchoredRoutingTarget(ServerInfoAnchorMailbox serverInfoAnchorMailbox)
		{
			if (serverInfoAnchorMailbox == null)
			{
				throw new ArgumentNullException("serverAnchorMailbox");
			}
			this.AnchorMailbox = serverInfoAnchorMailbox;
			this.BackEndServer = serverInfoAnchorMailbox.BackEndServer;
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060000B6 RID: 182 RVA: 0x00004EC7 File Offset: 0x000030C7
		// (set) Token: 0x060000B7 RID: 183 RVA: 0x00004ECF File Offset: 0x000030CF
		public AnchorMailbox AnchorMailbox { get; private set; }

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060000B8 RID: 184 RVA: 0x00004ED8 File Offset: 0x000030D8
		// (set) Token: 0x060000B9 RID: 185 RVA: 0x00004EE0 File Offset: 0x000030E0
		public BackEndServer BackEndServer { get; private set; }

		// Token: 0x060000BA RID: 186 RVA: 0x00004EE9 File Offset: 0x000030E9
		public override string ToString()
		{
			return string.Format("{0}~{1}", this.AnchorMailbox, this.BackEndServer.Fqdn);
		}
	}
}
