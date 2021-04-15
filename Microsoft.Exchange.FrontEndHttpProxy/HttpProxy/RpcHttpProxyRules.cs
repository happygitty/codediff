using System;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C5 RID: 197
	internal static class RpcHttpProxyRules
	{
		// Token: 0x17000186 RID: 390
		// (get) Token: 0x0600076F RID: 1903 RVA: 0x0002B800 File Offset: 0x00029A00
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

		// Token: 0x04000410 RID: 1040
		private static readonly NullRpcHttpProxyRules nullProxyRules = new NullRpcHttpProxyRules();

		// Token: 0x04000411 RID: 1041
		private static CoexistenceRpcHttpProxyRules coexistenceProxyRules = null;

		// Token: 0x04000412 RID: 1042
		private static object lockObject = new object();
	}
}
