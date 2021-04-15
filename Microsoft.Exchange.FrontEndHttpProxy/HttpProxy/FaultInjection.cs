using System;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000078 RID: 120
	internal class FaultInjection
	{
		// Token: 0x06000412 RID: 1042 RVA: 0x00017A09 File Offset: 0x00015C09
		public static void GenerateFault(FaultInjection.LIDs faultLid)
		{
			if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
			{
				ExTraceGlobals.FaultInjectionTracer.TraceTest((uint)faultLid);
			}
		}

		// Token: 0x06000413 RID: 1043 RVA: 0x00017A24 File Offset: 0x00015C24
		public static T TraceTest<T>(FaultInjection.LIDs faultLid)
		{
			T result = default(T);
			if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
			{
				ExTraceGlobals.FaultInjectionTracer.TraceTest<T>((uint)faultLid, ref result);
			}
			return result;
		}

		// Token: 0x02000116 RID: 278
		internal enum LIDs : uint
		{
			// Token: 0x040004F6 RID: 1270
			ShouldFailSmtpAnchorMailboxADLookup = 1378318050U,
			// Token: 0x040004F7 RID: 1271
			ProxyToLowerVersionEws = 2357603645U,
			// Token: 0x040004F8 RID: 1272
			ProxyToLowerVersionEwsOAuthIdentityActAsUserNullSid = 3431345469U,
			// Token: 0x040004F9 RID: 1273
			ExceptionDuringProxyDownLevelCheckNullSid_ChangeValue = 3548785981U,
			// Token: 0x040004FA RID: 1274
			AnchorMailboxDatabaseCacheEntry = 4134939965U
		}
	}
}
