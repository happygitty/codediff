using System;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000078 RID: 120
	internal class FaultInjection
	{
		// Token: 0x06000416 RID: 1046 RVA: 0x00017BC9 File Offset: 0x00015DC9
		public static void GenerateFault(FaultInjection.LIDs faultLid)
		{
			if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
			{
				ExTraceGlobals.FaultInjectionTracer.TraceTest((uint)faultLid);
			}
		}

		// Token: 0x06000417 RID: 1047 RVA: 0x00017BE4 File Offset: 0x00015DE4
		public static T TraceTest<T>(FaultInjection.LIDs faultLid)
		{
			T result = default(T);
			if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
			{
				ExTraceGlobals.FaultInjectionTracer.TraceTest<T>((uint)faultLid, ref result);
			}
			return result;
		}

		// Token: 0x02000115 RID: 277
		internal enum LIDs : uint
		{
			// Token: 0x040004FA RID: 1274
			ShouldFailSmtpAnchorMailboxADLookup = 1378318050U,
			// Token: 0x040004FB RID: 1275
			ProxyToLowerVersionEws = 2357603645U,
			// Token: 0x040004FC RID: 1276
			ProxyToLowerVersionEwsOAuthIdentityActAsUserNullSid = 3431345469U,
			// Token: 0x040004FD RID: 1277
			ExceptionDuringProxyDownLevelCheckNullSid_ChangeValue = 3548785981U,
			// Token: 0x040004FE RID: 1278
			AnchorMailboxDatabaseCacheEntry = 4134939965U
		}
	}
}
