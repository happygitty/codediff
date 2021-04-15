using System;
using System.Web;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B6 RID: 182
	public class RpsFriendlyErrorModule : IHttpModule
	{
		// Token: 0x0600072B RID: 1835 RVA: 0x0002A27C File Offset: 0x0002847C
		void IHttpModule.Init(HttpApplication context)
		{
			if (WinRMHelper.FriendlyErrorEnabled.Value)
			{
				context.PreSendRequestHeaders += this.OnPreSendRequestHeaders;
				context.EndRequest += this.OnEndRequest;
			}
		}

		// Token: 0x0600072C RID: 1836 RVA: 0x00008C7B File Offset: 0x00006E7B
		void IHttpModule.Dispose()
		{
		}

		// Token: 0x0600072D RID: 1837 RVA: 0x0002A2AE File Offset: 0x000284AE
		private void OnPreSendRequestHeaders(object sender, EventArgs e)
		{
			HttpContext.Current.Items["X-HeaderPreSent"] = true;
		}

		// Token: 0x0600072E RID: 1838 RVA: 0x0002A2CC File Offset: 0x000284CC
		private void OnEndRequest(object sender, EventArgs e)
		{
			HttpContext httpContext = HttpContext.Current;
			HttpResponse response = httpContext.Response;
			if (response == null)
			{
				return;
			}
			RequestDetailsLogger current = RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpContext);
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(current, "OnEndRequest.ContentType", response.ContentType);
			if (response.Headers["X-RemotePS-RevisedAction"] != null)
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(current, 21, response.Headers["X-RemotePS-RevisedAction"]);
			}
			if (httpContext.Items.Contains("X-HeaderPreSent") && (bool)httpContext.Items["X-HeaderPreSent"])
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(current, "FriendlyError", "Skip-HeaderPreSent");
				return;
			}
			try
			{
				int statusCode = response.StatusCode;
				int num;
				if (WinRMHelper.TryConvertStatusCode(statusCode, out num))
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int, int>((long)this.GetHashCode(), "[RpsFriendlyErrorModule::OnEndRequest]: Convert status code from {0} to {1}.", statusCode, num);
					}
					response.StatusCode = num;
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(current, 5, (long)statusCode);
				}
				if (statusCode >= 400 && !"Ping".Equals(response.Headers["X-RemotePS-Ping"], StringComparison.OrdinalIgnoreCase) && !"Possible-Ping".Equals(response.Headers["X-RemotePS-Ping"], StringComparison.OrdinalIgnoreCase))
				{
					response.ContentType = "application/soap+xml;charset=UTF-8";
					if (!WinRMHelper.DiagnosticsInfoHasBeenWritten(response.Headers))
					{
						string diagnosticsInfo = WinRMHelper.GetDiagnosticsInfo(httpContext);
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[RpsFriendlyErrorModule::OnEndRequest]: Original Status Code: {0}, Append diagnostics info: {1}.", statusCode, diagnosticsInfo);
						}
						if (statusCode == 401)
						{
							response.Output.Write(diagnosticsInfo + "Access Denied");
						}
						else
						{
							response.Output.Write(diagnosticsInfo);
						}
						RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(current, "FriendlyError", "HttpModule");
					}
				}
			}
			catch (Exception ex)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<Exception>((long)this.GetHashCode(), "[RpsFriendlyErrorModule::OnEndRequest]: Exception = {0}", ex);
				}
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericError(current, "RpsFriendlyErrorModule.OnEndRequest", ex.Message);
			}
		}

		// Token: 0x040003FC RID: 1020
		private const string HeaderPreSentItemKey = "X-HeaderPreSent";

		// Token: 0x040003FD RID: 1021
		private const string AccessDeniedHttpStatusText = "Access Denied";
	}
}
