using System;
using System.Net;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Global;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200008D RID: 141
	internal class BEResourceRequestHandler : ProxyRequestHandler
	{
		// Token: 0x060004CE RID: 1230 RVA: 0x0001AA5A File Offset: 0x00018C5A
		internal static bool CanHandle(HttpRequest httpRequest)
		{
			return !string.IsNullOrEmpty(BEResourceRequestHandler.GetBEResouceCookie(httpRequest)) && BEResourceRequestHandler.IsResourceRequest(httpRequest.Url.LocalPath);
		}

		// Token: 0x060004CF RID: 1231 RVA: 0x0001AA7B File Offset: 0x00018C7B
		internal static bool IsResourceRequest(string localPath)
		{
			return RequestPathParser.IsResourceRequest(localPath);
		}

		// Token: 0x060004D0 RID: 1232 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool ShouldBackendRequestBeAnonymous()
		{
			return true;
		}

		// Token: 0x060004D1 RID: 1233 RVA: 0x0001AA84 File Offset: 0x00018C84
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			string beresouceCookie = BEResourceRequestHandler.GetBEResouceCookie(base.ClientRequest);
			if (!string.IsNullOrEmpty(beresouceCookie))
			{
				base.Logger.Set(3, Constants.BEResource + "-Cookie");
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string, int>((long)this.GetHashCode(), "[BEResourceRequestHanlder::ResolveAnchorMailbox]: BEResource cookie used: {0}; context {1}.", beresouceCookie, base.TraceContext);
				}
				return new ServerInfoAnchorMailbox(BackEndServer.FromString(beresouceCookie), this);
			}
			return base.ResolveAnchorMailbox();
		}

		// Token: 0x060004D2 RID: 1234 RVA: 0x0001AB04 File Offset: 0x00018D04
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			base.AddProtocolSpecificHeadersToServerRequest(headers);
			if (!Utilities.IsPartnerHostedOnly && !GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled && HttpProxyGlobals.ProtocolType == 1 && base.ProxyToDownLevel)
			{
				EcpProxyRequestHandler.AddDownLevelProxyHeaders(headers, base.HttpContext);
			}
		}

		// Token: 0x060004D3 RID: 1235 RVA: 0x0001AB54 File Offset: 0x00018D54
		private static string GetBEResouceCookie(HttpRequest httpRequest)
		{
			string result = null;
			HttpCookie httpCookie = httpRequest.Cookies[Constants.BEResource];
			if (httpCookie != null)
			{
				result = httpCookie.Value;
			}
			return result;
		}
	}
}
