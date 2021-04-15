using System;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000096 RID: 150
	internal sealed class EwsUserPhotoProxyRequestHandler : EwsProxyRequestHandler
	{
		// Token: 0x17000126 RID: 294
		// (get) Token: 0x06000536 RID: 1334 RVA: 0x00003165 File Offset: 0x00001365
		protected override bool UseBackEndCacheForDownLevelServer
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000537 RID: 1335 RVA: 0x0001D074 File Offset: 0x0001B274
		internal static bool IsUserPhotoRequest(HttpRequest request)
		{
			return RequestPathParser.IsEwsGetUserPhotoRequest(request.Path);
		}

		// Token: 0x06000538 RID: 1336 RVA: 0x0001D084 File Offset: 0x0001B284
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

		// Token: 0x06000539 RID: 1337 RVA: 0x0001D0E5 File Offset: 0x0001B2E5
		protected override string TryGetExplicitLogonNode(ExplicitLogonNode node)
		{
			return base.ClientRequest.QueryString["email"];
		}

		// Token: 0x0600053A RID: 1338 RVA: 0x0001D0FC File Offset: 0x0001B2FC
		protected override BackEndServer GetDownLevelClientAccessServer(AnchorMailbox anchorMailbox, BackEndServer mailboxServer)
		{
			BackEndServer deterministicBackEndServer = HttpProxyBackEndHelper.GetDeterministicBackEndServer<WebServicesService>(mailboxServer, anchorMailbox.ToCookieKey(), this.ClientAccessType);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, BackEndServer, BackEndServer>((long)this.GetHashCode(), "[EwsUserPhotoProxyRequestHandler::GetDownLevelClientAccessServer] Context {0}; Overriding down level target {0} with latest version backend {1}.", base.TraceContext, mailboxServer, deterministicBackEndServer);
			}
			return deterministicBackEndServer;
		}
	}
}
