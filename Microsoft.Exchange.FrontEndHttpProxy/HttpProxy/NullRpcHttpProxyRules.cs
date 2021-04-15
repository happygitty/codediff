using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C3 RID: 195
	internal class NullRpcHttpProxyRules : IRpcHttpProxyRules
	{
		// Token: 0x06000763 RID: 1891 RVA: 0x00004B1F File Offset: 0x00002D1F
		internal NullRpcHttpProxyRules()
		{
		}

		// Token: 0x06000764 RID: 1892 RVA: 0x0002B280 File Offset: 0x00029480
		public bool TryGetProxyDestination(string rpcServerFqdn, out ProxyDestination destination)
		{
			destination = null;
			return false;
		}

		// Token: 0x06000765 RID: 1893 RVA: 0x000089E0 File Offset: 0x00006BE0
		public string DiagnosticInfo()
		{
			return string.Empty;
		}
	}
}
