using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.Diagnostics.WorkloadManagement;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000086 RID: 134
	internal static class WinRMHelper
	{
		// Token: 0x170000FF RID: 255
		// (get) Token: 0x0600046B RID: 1131 RVA: 0x00018BD9 File Offset: 0x00016DD9
		internal static IntAppSettingsEntry MaxBytesToPeekIntoRequestStream
		{
			get
			{
				return WinRMHelper.maxBytesToPeekIntoRequestStream;
			}
		}

		// Token: 0x17000100 RID: 256
		// (get) Token: 0x0600046C RID: 1132 RVA: 0x00018BE0 File Offset: 0x00016DE0
		internal static BoolAppSettingsEntry WinRMParserEnabled
		{
			get
			{
				return WinRMHelper.winRMParserEnabled;
			}
		}

		// Token: 0x17000101 RID: 257
		// (get) Token: 0x0600046D RID: 1133 RVA: 0x00018BE7 File Offset: 0x00016DE7
		internal static BoolAppSettingsEntry FriendlyErrorEnabled
		{
			get
			{
				return WinRMHelper.friendlyErrorEnabled;
			}
		}

		// Token: 0x0600046E RID: 1134 RVA: 0x00018BF0 File Offset: 0x00016DF0
		internal static string GetDiagnosticsInfo(HttpContext context)
		{
			RequestDetailsLogger current = RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(context);
			Guid guid = Guid.Empty;
			IActivityScope activityScope = null;
			if (current != null)
			{
				guid = current.ActivityId;
				activityScope = current.ActivityScope;
			}
			string text = string.Format("[ClientAccessServer={0},BackEndServer={1},RequestId={2},TimeStamp={3}] ", new object[]
			{
				Environment.MachineName,
				(activityScope == null) ? "UnKnown" : activityScope.GetProperty(13),
				guid,
				DateTime.UtcNow
			});
			string text2 = string.Empty;
			if (context != null)
			{
				text2 = WinRMInfo.GetFailureCategoryInfo(context);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				text += string.Format("[FailureCategory={0}] ", text2);
			}
			string text3 = context.Response.Headers["X-BasicAuthToOAuthConversionDiagnostics"];
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text += string.Format("BasicAuthToOAuthConversionDiagnostics={0}", text3);
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[WinRMHelper::GetDiagnosticsInfo] diagnosticsInfo = {0}.", text);
			}
			return text;
		}

		// Token: 0x0600046F RID: 1135 RVA: 0x00018CE7 File Offset: 0x00016EE7
		internal static void SetDiagnosticsInfoWrittenFlag(NameValueCollection headers)
		{
			if (headers == null)
			{
				return;
			}
			headers["X-Rps-DiagInfoWritten"] = "true";
		}

		// Token: 0x06000470 RID: 1136 RVA: 0x00018CFD File Offset: 0x00016EFD
		internal static bool DiagnosticsInfoHasBeenWritten(NameValueCollection headers)
		{
			return headers != null && headers["X-Rps-DiagInfoWritten"] != null;
		}

		// Token: 0x06000471 RID: 1137 RVA: 0x00018D12 File Offset: 0x00016F12
		internal static bool TryConvertStatusCode(int originalStatusCode, out int newStatusCode)
		{
			newStatusCode = 0;
			if (originalStatusCode == 401)
			{
				newStatusCode = 400;
				return true;
			}
			if (originalStatusCode == 404)
			{
				newStatusCode = 400;
				return true;
			}
			if (originalStatusCode != 503)
			{
				return false;
			}
			newStatusCode = 500;
			return true;
		}

		// Token: 0x06000472 RID: 1138 RVA: 0x00018D50 File Offset: 0x00016F50
		internal static bool IsPingRequest(WebException ex)
		{
			if (ex.Response == null)
			{
				return false;
			}
			if (ex.Response.Headers == null)
			{
				return false;
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[WinRMHelper::IsPingRequest] ex.Response.Headers[WinRMInfo.PingHeaderKey] = {0}.", ex.Response.Headers["X-RemotePS-Ping"]);
			}
			return "Ping".Equals(ex.Response.Headers["X-RemotePS-Ping"], StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x06000473 RID: 1139 RVA: 0x00018DCC File Offset: 0x00016FCC
		internal static bool CouldBePingRequest(WebException ex)
		{
			if (ex.Status != WebExceptionStatus.ProtocolError)
			{
				return false;
			}
			if (ex.Response == null)
			{
				return false;
			}
			if (WinRMHelper.IsPingRequest(ex))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[WinRMHelper::CouldBePingRequest] ex.Response.Headers[WinRMInfo.PingHeaderKey] = {0}.", ex.Response.Headers["X-RemotePS-Ping"]);
				}
				return false;
			}
			return ex.Response is HttpWebResponse && ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.InternalServerError;
		}

		// Token: 0x06000474 RID: 1140 RVA: 0x00018E54 File Offset: 0x00017054
		internal static bool TryInsertDiagnosticsInfo(ArraySegment<byte> buffer, Func<string> getDiagnosticInfo, out byte[] updatedBuffer, out string failureHint, Action<string> logging = null)
		{
			failureHint = null;
			updatedBuffer = null;
			if (buffer.Count == 0)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug(0L, "[WinRMHelper::TryInsertDiagnosticsInfo] buffer == null || buffer.Count = 0.");
				}
				failureHint = "buffer Null/Empty";
				return false;
			}
			string @string = Encoding.UTF8.GetString(buffer.Array, 0, 15);
			if (string.IsNullOrEmpty(@string))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[WinRMHelper::TryInsertDiagnosticsInfo] EnvelopString is null/empty.", @string);
				}
				failureHint = "EnvelopString Null/Empty";
				return false;
			}
			if (!@string.StartsWith("<s:Envelope", StringComparison.OrdinalIgnoreCase))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[WinRMHelper::TryInsertDiagnosticsInfo] EnvelopString = {0}, Not start with <s:Envelop.", @string);
				}
				failureHint = "No s:Envelop";
				return false;
			}
			string text = Encoding.UTF8.GetString(buffer.Array, 0, buffer.Count);
			if (string.IsNullOrEmpty(text))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[WinRMHelper::TryInsertDiagnosticsInfo] Output is null/empty.", text);
				}
				failureHint = "Output Null/Empty";
				return false;
			}
			int num = text.IndexOf("<f:Message>");
			if (num < 0)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[WinRMHelper::TryInsertDiagnosticsInfo] Output = {0}, faultMsgIndex < 0.", text);
				}
				failureHint = "No f:Message";
				return false;
			}
			int length = text.Length;
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string, int>(0L, "[WinRMHelper::TryInsertDiagnosticsInfo] Output(faultMsgIndex + 100) = {0}, faultMsgIndex = {1}.", text.Substring(num, (num + 100 < length) ? 100 : (length - num)), num);
			}
			string text2 = "f:Message";
			int num2 = num + "<f:Message>".Length;
			if (text.IndexOf("<f:ProviderFault", num2, "<f:ProviderFault".Length) >= 0)
			{
				text2 = "<f:ProviderFault";
				text2 = text2.TrimStart(new char[]
				{
					'<'
				});
				num2 = text.IndexOf('>', num2) + 1;
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string, int>(0L, "[WinRMHelper::TryInsertDiagnosticsInfo] Output(positionToInsert + 100) = {0}, positionToInsert = {1}.", text.Substring(num, (num2 + 100 < length) ? 100 : (length - num2)), num2);
				}
			}
			if (logging != null)
			{
				int num3 = text.IndexOf("<s:Fault");
				if (num3 > 0)
				{
					int num4 = text.IndexOf("<s:Code", num3);
					if (num4 > 0)
					{
						string tagValue = WinRMHelper.GetTagValue(text, "s:Value", num4);
						string text3 = string.Empty;
						int num5 = text.IndexOf("<s:Subcode", num4);
						if (num5 > 0)
						{
							text3 = WinRMHelper.GetTagValue(text, "s:Value", num5);
						}
						else
						{
							WinRMHelper.TraceDebugTagNotFound("TryInsertDiagnosticsInfo", text, "<s:Subcode", 0);
						}
						string tagValue2 = WinRMHelper.GetTagValue(text, text2, num4);
						logging(string.Concat(new string[]
						{
							tagValue,
							"/",
							text3,
							"/",
							tagValue2
						}));
					}
					else
					{
						WinRMHelper.TraceDebugTagNotFound("TryInsertDiagnosticsInf", text, "<s:Code", 0);
					}
				}
				else
				{
					WinRMHelper.TraceDebugTagNotFound("TryInsertDiagnosticsInfo", text, "<s:Fault", 0);
				}
			}
			string value = getDiagnosticInfo();
			text = text.Insert(num2, value);
			updatedBuffer = Encoding.UTF8.GetBytes(text);
			return true;
		}

		// Token: 0x06000475 RID: 1141 RVA: 0x00019164 File Offset: 0x00017364
		private static string GetTagValue(string text, string tag, int offset)
		{
			string funcName = "GetTagValue";
			int num = text.IndexOf("<" + tag, offset);
			if (num > 0)
			{
				int num2 = num + tag.Length + 1;
				num2 = text.IndexOf(">", num);
				if (num2 > 0)
				{
					num2++;
					int num3 = text.IndexOf("</" + tag, num2);
					if (num3 > num2)
					{
						return text.Substring(num2, num3 - num2);
					}
					WinRMHelper.TraceDebugTagNotFound(funcName, text, "</" + tag, num2);
				}
				else
				{
					WinRMHelper.TraceDebugTagNotFound(funcName, text, ">", num);
				}
			}
			else
			{
				WinRMHelper.TraceDebugTagNotFound(funcName, text, "<" + tag, offset);
			}
			return string.Empty;
		}

		// Token: 0x06000476 RID: 1142 RVA: 0x0001920C File Offset: 0x0001740C
		private static void TraceDebugTagNotFound(string funcName, string text, string tag, int offset)
		{
			int num = offset + 100;
			if (num >= text.Length)
			{
				num = text.Length - offset;
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug(0L, "[WinRMHelper::{0}] {1} not found in output(+{2}) = {3}", new object[]
				{
					funcName,
					"<s:Subcode",
					offset,
					text.Substring(offset, num)
				});
			}
		}

		// Token: 0x04000303 RID: 771
		internal const string DiagnosticsInfoFlagKey = "X-Rps-DiagInfoWritten";

		// Token: 0x04000304 RID: 772
		internal const string WSManContentType = "application/soap+xml;charset=UTF-8";

		// Token: 0x04000305 RID: 773
		internal const int StreamLookAheadBufferSize = 10000;

		// Token: 0x04000306 RID: 774
		private const string EnvelopStartTag = "<s:Envelope";

		// Token: 0x04000307 RID: 775
		private const int EnvelopToPeekLength = 15;

		// Token: 0x04000308 RID: 776
		private const string MessageTag = "<f:Message>";

		// Token: 0x04000309 RID: 777
		private const string ProviderFaultStartTag = "<f:ProviderFault";

		// Token: 0x0400030A RID: 778
		private const string FaultStartTag = "<s:Fault";

		// Token: 0x0400030B RID: 779
		private const string CodeStartTag = "<s:Code";

		// Token: 0x0400030C RID: 780
		private const string SubcodeStartTag = "<s:Subcode";

		// Token: 0x0400030D RID: 781
		private const string ValueTag = "s:Value";

		// Token: 0x0400030E RID: 782
		private const string FaultMessage = "f:Message";

		// Token: 0x0400030F RID: 783
		private const int MaxBufferCharactersToLogInTrace = 100;

		// Token: 0x04000310 RID: 784
		private static IntAppSettingsEntry maxBytesToPeekIntoRequestStream = new IntAppSettingsEntry("MaxBytesToPeekIntoRequestStream", 2000, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000311 RID: 785
		private static BoolAppSettingsEntry winRMParserEnabled = new BoolAppSettingsEntry("WinRMParserEnabled", true, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000312 RID: 786
		private static BoolAppSettingsEntry friendlyErrorEnabled = new BoolAppSettingsEntry("FriendlyErrorEnabled", true, ExTraceGlobals.VerboseTracer);
	}
}
