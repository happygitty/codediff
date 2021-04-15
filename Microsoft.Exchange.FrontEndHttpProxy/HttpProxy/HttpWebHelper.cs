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
		// Token: 0x0600042A RID: 1066 RVA: 0x00018058 File Offset: 0x00016258
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

		// Token: 0x0600042B RID: 1067 RVA: 0x00018108 File Offset: 0x00016308
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

		// Token: 0x0600042C RID: 1068 RVA: 0x00018146 File Offset: 0x00016346
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

		// Token: 0x0600042D RID: 1069 RVA: 0x00018181 File Offset: 0x00016381
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

		// Token: 0x02000118 RID: 280
		public enum ConnectivityError
		{
			// Token: 0x04000507 RID: 1287
			None,
			// Token: 0x04000508 RID: 1288
			Retryable,
			// Token: 0x04000509 RID: 1289
			NonRetryable
		}
	}
}
