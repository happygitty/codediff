using System;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C6 RID: 198
	internal static class RpcHttpProxyRules
	{
		// Token: 0x17000188 RID: 392
		// (get) Token: 0x06000774 RID: 1908 RVA: 0x0002B618 File Offset: 0x00029818
		internal static IRpcHttpProxyRules Instance
		{
			get
			{
				if (CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).UseCoexistenceRpcHttpProxyRules.Enabled)
				{
					CoexistenceRpcHttpProxyRules coexistenceRpcHttpProxyRules = RpcHttpProxyRules.coexistenceProxyRules;
					if (coexistenceRpcHttpProxyRules == null)
					{
						object obj = RpcHttpProxyRules.lockObject;
						lock (obj)
						{
							if (RpcHttpProxyRules.coexistenceProxyRules == null)
							{
								coexistenceRpcHttpProxyRules = new CoexistenceRpcHttpProxyRules();
								RpcHttpProxyRules.coexistenceProxyRules = coexistenceRpcHttpProxyRules;
							}
						}
					}
					return coexistenceRpcHttpProxyRules;
				}
				CoexistenceRpcHttpProxyRules coexistenceRpcHttpProxyRules2 = RpcHttpProxyRules.coexistenceProxyRules;
				if (coexistenceRpcHttpProxyRules2 != null)
				{
					object obj = RpcHttpProxyRules.lockObject;
					lock (obj)
					{
						RpcHttpProxyRules.coexistenceProxyRules = null;
					}
					coexistenceRpcHttpProxyRules2.Shutdown();
				}
				return RpcHttpProxyRules.nullProxyRules;
			}
		}

		// Token: 0x0400040C RID: 1036
		private static readonly NullRpcHttpProxyRules nullProxyRules = new NullRpcHttpProxyRules();

		// Token: 0x0400040D RID: 1037
		private static CoexistenceRpcHttpProxyRules coexistenceProxyRules = null;

		// Token: 0x0400040E RID: 1038
		private static object lockObject = new object();
	}
}
