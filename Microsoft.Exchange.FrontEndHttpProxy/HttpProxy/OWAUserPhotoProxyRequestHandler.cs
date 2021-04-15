using System;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000099 RID: 153
	internal sealed class OWAUserPhotoProxyRequestHandler : OwaProxyRequestHandler
	{
		// Token: 0x17000128 RID: 296
		// (get) Token: 0x06000548 RID: 1352 RVA: 0x00003165 File Offset: 0x00001365
		protected override bool UseBackEndCacheForDownLevelServer
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000549 RID: 1353 RVA: 0x0001D3B5 File Offset: 0x0001B5B5
		internal static bool IsUserPhotoRequest(HttpRequest request)
		{
			return RequestPathParser.IsOwaGetUserPhotoRequest(request.Path);
		}

		// Token: 0x0600054A RID: 1354 RVA: 0x0001D3C4 File Offset: 0x0001B5C4
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			string smtp;
			if (base.UseRoutingHintForAnchorMailbox && RequestQueryStringParser.TryGetExplicitLogonSmtp(base.ClientRequest.QueryString, ref smtp))
			{
				if (HttpProxySettings.NoMailboxFallbackRoutingEnabled.Value)
				{
					base.IsAnchorMailboxFromRoutingHint = true;
				}
				base.Logger.Set(3, "ExplicitLogon-SMTP");
				return new SmtpAnchorMailbox(smtp, this);
			}
			return base.ResolveAnchorMailbox();
		}

		// Token: 0x0600054B RID: 1355 RVA: 0x0001D0E5 File Offset: 0x0001B2E5
		protected override string TryGetExplicitLogonNode(ExplicitLogonNode node)
		{
			return base.ClientRequest.QueryString["email"];
		}

		// Token: 0x0600054C RID: 1356 RVA: 0x0001D428 File Offset: 0x0001B628
		protected override BackEndServer GetDownLevelClientAccessServer(AnchorMailbox anchorMailbox, BackEndServer mailboxServer)
		{
			BackEndServer deterministicBackEndServer = HttpProxyBackEndHelper.GetDeterministicBackEndServer<WebServicesService>(mailboxServer, anchorMailbox.ToCookieKey(), this.ClientAccessType);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, BackEndServer, BackEndServer>((long)this.GetHashCode(), "[OWAUserPhotoProxyRequestHandler::GetDownLevelClientAccessServer] Context {0}; Overriding down level target {0} with latest version backend {1}.", base.TraceContext, mailboxServer, deterministicBackEndServer);
			}
			return deterministicBackEndServer;
		}
	}
}
