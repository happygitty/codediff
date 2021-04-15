using System;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C0 RID: 192
	internal class Directory : IDirectory
	{
		// Token: 0x0600075A RID: 1882 RVA: 0x0002B211 File Offset: 0x00029411
		public ADSite[] GetADSites()
		{
			ADSite[] sites = null;
			ADNotificationAdapter.TryRunADOperation(delegate()
			{
				ADPagedReader<ADSite> adpagedReader = DirectorySessionFactory.Default.CreateTopologyConfigurationSession(2, ADSessionSettings.FromRootOrgScopeSet(), 30, "GetADSites", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs").FindPaged<ADSite>(null, 2, null, null, 0, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs", 33, "GetADSites");
				sites = adpagedReader.ReadAllPages();
			});
			return sites;
		}

		// Token: 0x0600075B RID: 1883 RVA: 0x0002B236 File Offset: 0x00029436
		public ClientAccessArray[] GetClientAccessArrays()
		{
			ClientAccessArray[] arrays = null;
			ADNotificationAdapter.TryRunADOperation(delegate()
			{
				ADPagedReader<ClientAccessArray> adpagedReader = DirectorySessionFactory.Default.CreateTopologyConfigurationSession(2, ADSessionSettings.FromRootOrgScopeSet(), 50, "GetClientAccessArrays", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs").FindPaged<ClientAccessArray>(null, 2, ClientAccessArray.PriorTo15ExchangeObjectVersionFilter, null, 0, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs", 53, "GetClientAccessArrays");
				arrays = adpagedReader.ReadAllPages();
			});
			return arrays;
		}

		// Token: 0x0600075C RID: 1884 RVA: 0x0002B25B File Offset: 0x0002945B
		public Server[] GetServers()
		{
			Server[] servers = null;
			ADNotificationAdapter.TryRunADOperation(delegate()
			{
				ADPagedReader<Server> adpagedReader = DirectorySessionFactory.Default.CreateTopologyConfigurationSession(2, ADSessionSettings.FromRootOrgScopeSet(), 70, "GetServers", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs").FindPaged<Server>(null, 2, null, null, 0, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs", 73, "GetServers");
				servers = adpagedReader.ReadAllPages();
			});
			return servers;
		}
	}
}
