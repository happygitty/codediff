using System;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C0 RID: 192
	public interface IDirectory
	{
		// Token: 0x06000759 RID: 1881
		ADSite[] GetADSites();

		// Token: 0x0600075A RID: 1882
		ClientAccessArray[] GetClientAccessArrays();

		// Token: 0x0600075B RID: 1883
		Server[] GetServers();
	}
}
