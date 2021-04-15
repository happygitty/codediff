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
	// Token: 0x02000033 RID: 51
	internal class LocalSiteAnchorMailbox : AnchorMailbox
	{
		// Token: 0x060001A1 RID: 417 RVA: 0x00008712 File Offset: 0x00006912
		public LocalSiteAnchorMailbox(IRequestContext requestContext) : base(AnchorSource.Anonymous, LocalSiteAnchorMailbox.LocalSiteIdentifier, requestContext)
		{
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x00008724 File Offset: 0x00006924
		public override BackEndServer TryDirectBackEndCalculation()
		{
			BackEndServer backEndServer = LocalSiteMailboxServerCache.Instance.TryGetRandomE15Server(base.RequestContext);
			if (backEndServer != null)
			{
				return backEndServer;
			}
			if (GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled && CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).ServersCache.Enabled)
			{
				try
				{
					MiniServer anyBackEndServerFromLocalSite = ServersCache.GetAnyBackEndServerFromLocalSite(Server.E15MinVersion, false);
					return new BackEndServer(anyBackEndServerFromLocalSite.Fqdn, anyBackEndServerFromLocalSite.VersionNumber);
				}
				catch (ServerHasNotBeenFoundException)
				{
					return base.CheckForNullAndThrowIfApplicable<BackEndServer>(null);
				}
			}
			return HttpProxyBackEndHelper.GetAnyBackEndServerForVersion<WebServicesService>(new ServerVersion(Server.E15MinVersion), false, 2, true);
		}

		// Token: 0x0400010A RID: 266
		internal static readonly string LocalSiteIdentifier = "LocalSite";
	}
}
