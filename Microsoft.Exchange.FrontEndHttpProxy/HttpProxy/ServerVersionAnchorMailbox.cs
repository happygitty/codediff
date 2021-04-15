using System;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;
using Microsoft.Exchange.VariantConfiguration.Global;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200002A RID: 42
	internal class ServerVersionAnchorMailbox<ServiceType> : AnchorMailbox where ServiceType : HttpService
	{
		// Token: 0x06000153 RID: 339 RVA: 0x000074D0 File Offset: 0x000056D0
		public ServerVersionAnchorMailbox(ServerVersion serverVersion, ClientAccessType clientAccessType, IRequestContext requestContext) : base(AnchorSource.ServerVersion, serverVersion, requestContext)
		{
			this.ClientAccessType = clientAccessType;
			base.NotFoundExceptionCreator = (() => new ServerNotFoundException(string.Format("Cannot find Mailbox server with {0}.", this.ServerVersion), this.ServerVersion.ToString()));
		}

		// Token: 0x06000154 RID: 340 RVA: 0x000074F5 File Offset: 0x000056F5
		public ServerVersionAnchorMailbox(ServerVersion serverVersion, ClientAccessType clientAccessType, bool exactVersionMatch, IRequestContext requestContext) : this(serverVersion, clientAccessType, requestContext)
		{
			this.ExactVersionMatch = exactVersionMatch;
		}

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x06000155 RID: 341 RVA: 0x00007508 File Offset: 0x00005708
		public ServerVersion ServerVersion
		{
			get
			{
				return (ServerVersion)base.SourceObject;
			}
		}

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000156 RID: 342 RVA: 0x00007515 File Offset: 0x00005715
		// (set) Token: 0x06000157 RID: 343 RVA: 0x0000751D File Offset: 0x0000571D
		public ClientAccessType ClientAccessType { get; private set; }

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000158 RID: 344 RVA: 0x00007526 File Offset: 0x00005726
		// (set) Token: 0x06000159 RID: 345 RVA: 0x0000752E File Offset: 0x0000572E
		public bool ExactVersionMatch { get; private set; }

		// Token: 0x0600015A RID: 346 RVA: 0x00007538 File Offset: 0x00005738
		public override BackEndServer TryDirectBackEndCalculation()
		{
			if (this.ServerVersion.Major == 15 && !this.ExactVersionMatch)
			{
				BackEndServer backEndServer = LocalSiteMailboxServerCache.Instance.TryGetRandomE15Server(base.RequestContext);
				if (backEndServer != null && new ServerVersion(backEndServer.Version).Minor >= this.ServerVersion.Minor)
				{
					return backEndServer;
				}
			}
			if (GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled && CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).ServersCache.Enabled)
			{
				try
				{
					MiniServer miniServer;
					if (this.ExactVersionMatch)
					{
						miniServer = ServersCache.GetAnyBackEndServerWithExactVersion(this.ServerVersion.ToInt());
					}
					else
					{
						miniServer = ServersCache.GetAnyBackEndServerWithMinVersion(this.ServerVersion.ToInt());
					}
					return new BackEndServer(miniServer.Fqdn, miniServer.VersionNumber);
				}
				catch (ServerHasNotBeenFoundException)
				{
					return base.CheckForNullAndThrowIfApplicable<BackEndServer>(null);
				}
			}
			BackEndServer result;
			try
			{
				result = HttpProxyBackEndHelper.GetAnyBackEndServerForVersion<ServiceType>(this.ServerVersion, this.ExactVersionMatch, this.ClientAccessType, false);
			}
			catch (ServerNotFoundException)
			{
				result = base.CheckForNullAndThrowIfApplicable<BackEndServer>(null);
			}
			return result;
		}
	}
}
