using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.HttpProxy.Routing.Providers;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000029 RID: 41
	internal class ServerInfoAnchorMailbox : AnchorMailbox
	{
		// Token: 0x0600014C RID: 332 RVA: 0x000073CC File Offset: 0x000055CC
		public ServerInfoAnchorMailbox(string fqdn, IRequestContext requestContext) : base(AnchorSource.ServerInfo, fqdn, requestContext)
		{
			if (string.IsNullOrEmpty(fqdn))
			{
				throw new ArgumentNullException("fqdn");
			}
			base.NotFoundExceptionCreator = (() => new ServerNotFoundException(string.Format("Cannot find server {0}.", fqdn), fqdn));
		}

		// Token: 0x0600014D RID: 333 RVA: 0x00007420 File Offset: 0x00005620
		public ServerInfoAnchorMailbox(BackEndServer backendServer, IRequestContext requestContext) : base(AnchorSource.ServerInfo, backendServer.Fqdn, requestContext)
		{
			int num;
			if (!ServerInfoAnchorMailbox.ServerProvider.TryFindServerVersion(backendServer.Fqdn, ref num))
			{
				throw new ArgumentException("Invalid value");
			}
			this.BackEndServer = backendServer;
		}

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600014E RID: 334 RVA: 0x00007462 File Offset: 0x00005662
		// (set) Token: 0x0600014F RID: 335 RVA: 0x0000746A File Offset: 0x0000566A
		public BackEndServer BackEndServer { get; private set; }

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x06000150 RID: 336 RVA: 0x0000618F File Offset: 0x0000438F
		public string Fqdn
		{
			get
			{
				return (string)base.SourceObject;
			}
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00007474 File Offset: 0x00005674
		public override BackEndServer TryDirectBackEndCalculation()
		{
			if (this.BackEndServer != null)
			{
				return this.BackEndServer;
			}
			int num;
			if (!ServerInfoAnchorMailbox.ServerProvider.TryFindServerVersion(this.Fqdn, ref num))
			{
				return base.CheckForNullAndThrowIfApplicable<BackEndServer>(null);
			}
			this.BackEndServer = new BackEndServer(this.Fqdn, num);
			return this.BackEndServer;
		}

		// Token: 0x040000EB RID: 235
		private static readonly ServerProvider ServerProvider = new ServerProvider();
	}
}
