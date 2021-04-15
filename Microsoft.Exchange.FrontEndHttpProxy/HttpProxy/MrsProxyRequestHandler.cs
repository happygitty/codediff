﻿using System;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Global;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200009D RID: 157
	internal class MrsProxyRequestHandler : BEServerCookieProxyRequestHandler<WebServicesService>
	{
		// Token: 0x1700012E RID: 302
		// (get) Token: 0x06000570 RID: 1392 RVA: 0x0001981A File Offset: 0x00017A1A
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x1700012F RID: 303
		// (get) Token: 0x06000571 RID: 1393 RVA: 0x00003165 File Offset: 0x00001365
		protected override bool UseBackEndCacheForDownLevelServer
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000572 RID: 1394 RVA: 0x0001E604 File Offset: 0x0001C804
		internal static bool IsMrsRequest(HttpRequest request)
		{
			string[] segments = request.Url.Segments;
			if (segments == null || segments.Length != 3)
			{
				return false;
			}
			if (!segments[2].TrimEnd(new char[]
			{
				'/'
			}).Equals("MRSProxy.svc", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (!MrsProxyRequestHandler.IsMrsProxyEnabled())
			{
				throw new HttpException(403, "MRS proxy service is disabled");
			}
			return true;
		}

		// Token: 0x06000573 RID: 1395 RVA: 0x0001E664 File Offset: 0x0001C864
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			string text = base.ClientRequest.Headers[Constants.TargetDatabaseHeaderName];
			if (!string.IsNullOrEmpty(text))
			{
				Guid databaseGuid;
				if (Guid.TryParse(text, out databaseGuid))
				{
					base.Logger.Set(3, "TargetDatabase-GUID");
					return new DatabaseGuidAnchorMailbox(databaseGuid, this);
				}
				base.Logger.Set(3, "TargetDatabase-Name");
				return new DatabaseNameAnchorMailbox(text, this);
			}
			else
			{
				AnchorMailbox anchorMailbox = base.CreateAnchorMailboxFromRoutingHint();
				if (anchorMailbox != null)
				{
					return anchorMailbox;
				}
				if (Utilities.IsPartnerHostedOnly || GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled)
				{
					base.Logger.Set(3, "ClientVersionHeader");
					return base.GetServerVersionAnchorMailbox(base.ClientRequest.Headers[Constants.ClientVersionHeaderName]);
				}
				string text2 = base.ClientRequest.Headers["X-GenericAnchorHint"];
				if (!string.IsNullOrEmpty(text2))
				{
					return new PstProviderAnchorMailbox(text2, this);
				}
				base.Logger.Set(3, "ForestWideOrganization");
				return new OrganizationAnchorMailbox(OrganizationId.ForestWideOrgId, this);
			}
		}

		// Token: 0x06000574 RID: 1396 RVA: 0x0001E780 File Offset: 0x0001C980
		protected override Uri GetTargetBackEndServerUrl()
		{
			Uri targetBackEndServerUrl = base.GetTargetBackEndServerUrl();
			UriBuilder uriBuilder = new UriBuilder(targetBackEndServerUrl);
			if (targetBackEndServerUrl.Port == 444)
			{
				uriBuilder.Port = 443;
			}
			uriBuilder.Path = "/Microsoft.Exchange.MailboxReplicationService.ProxyService";
			return uriBuilder.Uri;
		}

		// Token: 0x06000575 RID: 1397 RVA: 0x0001E7C4 File Offset: 0x0001C9C4
		protected override BackEndServer GetDownLevelClientAccessServer(AnchorMailbox anchorMailbox, BackEndServer mailboxServer)
		{
			BackEndServer deterministicBackEndServer = HttpProxyBackEndHelper.GetDeterministicBackEndServer<WebServicesService>(mailboxServer, anchorMailbox.ToCookieKey(), this.ClientAccessType);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, BackEndServer, BackEndServer>((long)this.GetHashCode(), "[MrsProxyRequestHandler::GetDownLevelClientAccessServer] Context {0}; Overriding down level target {0} with latest version backend {1}.", base.TraceContext, mailboxServer, deterministicBackEndServer);
			}
			return deterministicBackEndServer;
		}

		// Token: 0x06000576 RID: 1398 RVA: 0x0001E810 File Offset: 0x0001CA10
		private static bool IsMrsProxyEnabled()
		{
			bool? flag = null;
			ADWebServicesVirtualDirectory adwebServicesVirtualDirectory = (ADWebServicesVirtualDirectory)HttpProxyGlobals.VdirObject.Member;
			flag = new bool?(adwebServicesVirtualDirectory.MRSProxyEnabled);
			if (flag == null && ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
			{
				ExTraceGlobals.VerboseTracer.TraceError(0L, "[MrsProxyRequestHandler::IsMrsProxyEnabled] Can not find vdir.");
			}
			return flag != null && flag.Value;
		}

		// Token: 0x04000377 RID: 887
		private const string BackEndMrsProxyPath = "/Microsoft.Exchange.MailboxReplicationService.ProxyService";

		// Token: 0x04000378 RID: 888
		private const string FrontEndMrsProxyPath = "MRSProxy.svc";
	}
}
