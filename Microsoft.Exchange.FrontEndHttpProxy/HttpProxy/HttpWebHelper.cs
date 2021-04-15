using System;
using System.Net;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net.Protocols;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200007B RID: 123
	internal static class HttpWebHelper
	{
		// Token: 0x06000426 RID: 1062 RVA: 0x00017E98 File Offset: 0x00016098
		public static void SetRange(HttpWebRequest destination, string value)
		{
			HttpRangeSpecifier httpRangeSpecifier = HttpRangeSpecifier.Parse(value);
			foreach (HttpRange httpRange in httpRangeSpecifier.RangeCollection)
			{
				if (httpRange.HasFirstBytePosition && httpRange.HasLastBytePosition)
				{
					destination.AddRange(httpRangeSpecifier.RangeUnitSpecifier, httpRange.FirstBytePosition, httpRange.LastBytePosition);
				}
				else if (httpRange.HasFirstBytePosition)
				{
					destination.AddRange(httpRangeSpecifier.RangeUnitSpecifier, httpRange.FirstBytePosition);
				}
				else if (httpRange.HasSuffixLength)
				{
					destination.AddRange(httpRangeSpecifier.RangeUnitSpecifier, -httpRange.SuffixLength);
				}
			}
		}

		// Token: 0x06000427 RID: 1063 RVA: 0x00017F48 File Offset: 0x00016148
		public static void SetIfModifiedSince(HttpWebRequest destination, string value)
		{
			DateTime ifModifiedSince;
			if (DateTime.TryParse(value, out ifModifiedSince))
			{
				destination.IfModifiedSince = ifModifiedSince;
				return;
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[HttpWebHelper::SetIfModifiedSince] Parse failure for IfModifiedSince header {0}", value);
			}
		}

		// Token: 0x06000428 RID: 1064 RVA: 0x00017F86 File Offset: 0x00016186
		public static void SetConnectionHeader(HttpWebRequest destination, string source)
		{
			if (HttpProxySettings.KeepAliveOutboundConnectionsEnabled.Value || source.IndexOf("keep-alive", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				destination.KeepAlive = true;
				return;
			}
			if (source.IndexOf("close", StringComparison.OrdinalIgnoreCase) == -1)
			{
				destination.Connection = source;
			}
		}

		// Token: 0x06000429 RID: 1065 RVA: 0x00017FC1 File Offset: 0x000161C1
		public static HttpWebHelper.ConnectivityError CheckConnectivityError(WebException e)
		{
			if (ConnectivityErrorHelper.IsConnectivityErrorWebExceptionStatus(e.Status))
			{
				return HttpWebHelper.ConnectivityError.Retryable;
			}
			if (ConnectivityErrorHelper.IsConnectivityErrorWebResponse(e.Response))
			{
				return HttpWebHelper.ConnectivityError.NonRetryable;
			}
			return HttpWebHelper.ConnectivityError.None;
		}

		// Token: 0x02000119 RID: 281
		public enum ConnectivityError
		{
			// Token: 0x04000503 RID: 1283
			None,
			// Token: 0x04000504 RID: 1284
			Retryable,
			// Token: 0x04000505 RID: 1285
			NonRetryable
		}
	}
}
