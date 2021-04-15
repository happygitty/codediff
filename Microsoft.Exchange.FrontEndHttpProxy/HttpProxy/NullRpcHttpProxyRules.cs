using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C2 RID: 194
	internal class NullRpcHttpProxyRules : IRpcHttpProxyRules
	{
		// Token: 0x0600075E RID: 1886 RVA: 0x00004B1F File Offset: 0x00002D1F
		internal NullRpcHttpProxyRules()
		{
		}

		// Token: 0x0600075F RID: 1887 RVA: 0x0002B468 File Offset: 0x00029668
		public bool TryGetProxyDestination(string rpcServerFqdn, out ProxyDestination destination)
		{
			destination = null;
			return false;
		}

		// Token: 0x06000760 RID: 1888 RVA: 0x000089E0 File Offset: 0x00006BE0
		public string DiagnosticInfo()
		{
			return string.Empty;
		}
	}
}
