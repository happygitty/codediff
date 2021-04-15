using System;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C1 RID: 193
	public interface IDirectory
	{
		// Token: 0x0600075E RID: 1886
		ADSite[] GetADSites();

		// Token: 0x0600075F RID: 1887
		ClientAccessArray[] GetClientAccessArrays();

		// Token: 0x06000760 RID: 1888
		Server[] GetServers();
	}
}
