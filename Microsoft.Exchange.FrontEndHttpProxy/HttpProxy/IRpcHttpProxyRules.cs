using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C1 RID: 193
	internal interface IRpcHttpProxyRules
	{
		// Token: 0x0600075C RID: 1884
		bool TryGetProxyDestination(string rpcServerFqdn, out ProxyDestination destination);

		// Token: 0x0600075D RID: 1885
		string DiagnosticInfo();
	}
}
