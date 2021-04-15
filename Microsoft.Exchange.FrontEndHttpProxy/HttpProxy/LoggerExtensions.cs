using System;
using Microsoft.Exchange.Diagnostics;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000057 RID: 87
	internal static class LoggerExtensions
	{
		// Token: 0x060002C3 RID: 707 RVA: 0x0000DE24 File Offset: 0x0000C024
		internal static void SafeSet(this RequestDetailsLogger logger, Enum key, object value)
		{
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(logger, key, value);
		}
	}
}
