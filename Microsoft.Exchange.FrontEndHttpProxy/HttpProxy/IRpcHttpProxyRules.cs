using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C2 RID: 194
	internal interface IRpcHttpProxyRules
	{
		// Token: 0x06000761 RID: 1889
		bool TryGetProxyDestination(string rpcServerFqdn, out ProxyDestination destination);

		// Token: 0x06000762 RID: 1890
		string DiagnosticInfo();
	}
}
