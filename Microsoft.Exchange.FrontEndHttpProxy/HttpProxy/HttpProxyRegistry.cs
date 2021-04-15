using System;
using System.Security;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.ExchangeSystem;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Win32;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200004B RID: 75
	internal static class HttpProxyRegistry
	{
		// Token: 0x06000272 RID: 626 RVA: 0x0000CB6C File Offset: 0x0000AD6C
		private static bool GetOWARegistryValue(string valueName, bool defaultValue)
		{
			bool result;
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\MSExchange OWA", false))
				{
					object value = registryKey.GetValue(valueName);
					if (value == null || !(value is int))
					{
						result = defaultValue;
					}
					else
					{
						result = ((int)value != 0);
					}
				}
			}
			catch (SecurityException)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string, bool>(0L, "[HttpProxyRegistry::GetOWARegistryValue] Security exception encountered while retrieving {0} registry value.  Defaulting to {1}", valueName, defaultValue);
				}
				result = defaultValue;
			}
			catch (UnauthorizedAccessException)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string, bool>(0L, "[HttpProxyRegistry::GetOWARegistryValue] Unauthorized exception encountered while retrieving {0} registry value.  Defaulting to {1}", valueName, defaultValue);
				}
				result = defaultValue;
			}
			return result;
		}

		// Token: 0x04000181 RID: 385
		public static readonly LazyMember<bool> OwaAllowInternalUntrustedCerts = new LazyMember<bool>(() => HttpProxyRegistry.GetOWARegistryValue("AllowInternalUntrustedCerts", true));

		// Token: 0x04000182 RID: 386
		public static readonly LazyMember<bool> AreGccStoredSecretKeysValid = new LazyMember<bool>(() => HttpProxyRegistry.AreGccStoredSecretKeysValid.Member);

		// Token: 0x04000183 RID: 387
		internal const string MSExchangeOWARegistryPath = "SYSTEM\\CurrentControlSet\\Services\\MSExchange OWA";
	}
}
