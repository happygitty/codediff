using System;
using System.IO;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.EventLogs;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200006B RID: 107
	internal class RpsOutDataResponseStreamProxy : StreamProxy
	{
		// Token: 0x0600037E RID: 894 RVA: 0x00013E65 File Offset: 0x00012065
		internal RpsOutDataResponseStreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, byte[] buffer, IRequestContext requestContext) : base(streamProxyType, source, target, buffer, requestContext)
		{
		}

		// Token: 0x0600037F RID: 895 RVA: 0x00013E74 File Offset: 0x00012074
		protected override byte[] GetUpdatedBufferToSend(ArraySegment<byte> buffer)
		{
			try
			{
				byte[] result;
				string text;
				if (this.TryGetUpdatedBufferToSend(buffer, out result, out text))
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[RpsOutDataResponseStreamProxy::GetUpdatedBufferToSend] Context={0}, Buffer updated.", base.RequestContext.TraceContext);
					}
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(base.RequestContext.Logger, "FriendlyError", "StreamProxy");
					return result;
				}
				if (text != null)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(base.RequestContext.Logger, "FriendlyError", text);
				}
			}
			catch (Exception ex)
			{
				Diagnostics.ReportException(ex, FrontEndHttpProxyEventLogConstants.Tuple_InternalServerError, null, "Exception from RpsOutDataResponseStreamProxy::GetUpdatedBufferToSend event: {0}");
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericError(base.RequestContext.Logger, "GetUpdatedBufferToSendException", ex.ToString());
			}
			return base.GetUpdatedBufferToSend(buffer);
		}

		// Token: 0x06000380 RID: 896 RVA: 0x00013F40 File Offset: 0x00012140
		private bool TryGetUpdatedBufferToSend(ArraySegment<byte> buffer, out byte[] updatedBuffer, out string failureHint)
		{
			HttpContext context = base.RequestContext.HttpContext;
			updatedBuffer = null;
			failureHint = null;
			if (context.Response == null)
			{
				failureHint = "Response Null";
				return false;
			}
			int statusCode = context.Response.StatusCode;
			if (statusCode < 400)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, int>((long)this.GetHashCode(), "[RpsOutDataResponseStreamProxy::TryGetUpdatedBufferToSend] Context={0}, StatusCode={1}.", base.RequestContext.TraceContext, statusCode);
				}
				return false;
			}
			if (WinRMHelper.DiagnosticsInfoHasBeenWritten(context.Response.Headers))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[RpsOutDataResponseStreamProxy::TryGetUpdatedBufferToSend] Context={0}, diagnostics info has been written.", base.RequestContext.TraceContext);
				}
				failureHint = "BeenWritten";
				return false;
			}
			if (WinRMHelper.TryInsertDiagnosticsInfo(buffer, () => WinRMHelper.GetDiagnosticsInfo(context), out updatedBuffer, out failureHint, delegate(string winRMFaultMessage)
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.RequestContext.Logger, "WinRMFaultMessage", winRMFaultMessage);
				WinRMInfo.SetWSManFailureCategory(context.Response.Headers, winRMFaultMessage);
			}))
			{
				WinRMHelper.SetDiagnosticsInfoWrittenFlag(context.Response.Headers);
				return true;
			}
			return false;
		}
	}
}
