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
		// Token: 0x060004D2 RID: 1234 RVA: 0x0001AC1A File Offset: 0x00018E1A
		internal static bool CanHandle(HttpRequest httpRequest)
		{
			return !string.IsNullOrEmpty(BEResourceRequestHandler.GetBEResouceCookie(httpRequest)) && BEResourceRequestHandler.IsResourceRequest(httpRequest.Url.LocalPath);
		}

		// Token: 0x060004D3 RID: 1235 RVA: 0x0001AC3B File Offset: 0x00018E3B
		internal static bool IsResourceRequest(string localPath)
		{
			return RequestPathParser.IsResourceRequest(localPath);
		}

		// Token: 0x060004D4 RID: 1236 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool ShouldBackendRequestBeAnonymous()
		{
			return true;
		}

		// Token: 0x060004D5 RID: 1237 RVA: 0x0001AC44 File Offset: 0x00018E44
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

		// Token: 0x060004D6 RID: 1238 RVA: 0x0001ACC4 File Offset: 0x00018EC4
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			base.AddProtocolSpecificHeadersToServerRequest(headers);
			if (!Utilities.IsPartnerHostedOnly && !GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled && HttpProxyGlobals.ProtocolType == 1 && base.ProxyToDownLevel)
			{
				EcpProxyRequestHandler.AddDownLevelProxyHeaders(headers, base.HttpContext);
			}
		}

		// Token: 0x060004D7 RID: 1239 RVA: 0x0001AD14 File Offset: 0x00018F14
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
