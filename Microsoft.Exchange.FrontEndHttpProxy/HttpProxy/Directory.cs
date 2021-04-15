using System;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000BF RID: 191
	internal class Directory : IDirectory
	{
		// Token: 0x06000755 RID: 1877 RVA: 0x0002B3F9 File Offset: 0x000295F9
		public ADSite[] GetADSites()
		{
			ADSite[] sites = null;
			ADNotificationAdapter.TryRunADOperation(delegate()
			{
				ADPagedReader<ADSite> adpagedReader = DirectorySessionFactory.Default.CreateTopologyConfigurationSession(2, ADSessionSettings.FromRootOrgScopeSet(), 30, "GetADSites", "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs").FindPaged<ADSite>(null, 2, null, null, 0, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs", 33, "GetADSites");
				sites = adpagedReader.ReadAllPages();
			});
			return sites;
		}

		// Token: 0x06000756 RID: 1878 RVA: 0x0002B41E File Offset: 0x0002961E
		public ClientAccessArray[] GetClientAccessArrays()
		{
			ClientAccessArray[] arrays = null;
			ADNotificationAdapter.TryRunADOperation(delegate()
			{
				ADPagedReader<ClientAccessArray> adpagedReader = DirectorySessionFactory.Default.CreateTopologyConfigurationSession(2, ADSessionSettings.FromRootOrgScopeSet(), 50, "GetClientAccessArrays", "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs").FindPaged<ClientAccessArray>(null, 2, ClientAccessArray.PriorTo15ExchangeObjectVersionFilter, null, 0, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs", 53, "GetClientAccessArrays");
				arrays = adpagedReader.ReadAllPages();
			});
			return arrays;
		}

		// Token: 0x06000757 RID: 1879 RVA: 0x0002B443 File Offset: 0x00029643
		public Server[] GetServers()
		{
			Server[] servers = null;
			ADNotificationAdapter.TryRunADOperation(delegate()
			{
				ADPagedReader<Server> adpagedReader = DirectorySessionFactory.Default.CreateTopologyConfigurationSession(2, ADSessionSettings.FromRootOrgScopeSet(), 70, "GetServers", "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs").FindPaged<Server>(null, 2, null, null, 0, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RpcHttp\\Directory.cs", 73, "GetServers");
				servers = adpagedReader.ReadAllPages();
			});
			return servers;
		}
	}
}
