using System;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Global;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000BA RID: 186
	internal class SiteMailboxCreatingProxyRequestHandler : EcpProxyRequestHandler
	{
		// Token: 0x0600073A RID: 1850 RVA: 0x0002A65C File Offset: 0x0002885C
		internal static bool IsSiteMailboxCreatingProxyRequest(HttpRequest request)
		{
			if (request != null)
			{
				if (request.GetHttpMethod() == HttpMethod.Get)
				{
					string value = request.QueryString["ftr"];
					if ("TeamMailboxCreating".Equals(value, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
					if (request.Url.AbsolutePath.EndsWith("TeamMailbox/NewSharePointTeamMailbox.aspx", StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
				else if (request.GetHttpMethod() == HttpMethod.Post && request.Url.AbsolutePath.EndsWith("DDI/DDIService.svc/NewObject", StringComparison.OrdinalIgnoreCase) && "TeamMailboxProperties".Equals(request.QueryString["schema"], StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600073B RID: 1851 RVA: 0x0002A6F4 File Offset: 0x000288F4
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string, Uri>((long)this.GetHashCode(), "[SiteMailboxCreatingProxyRequestHandler::ResolveAnchorMailbox]: Method {0}; Url {1};", base.ClientRequest.HttpMethod, base.ClientRequest.Url);
			}
			if (!Utilities.IsPartnerHostedOnly && !GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled)
			{
				base.Logger.Set(3, "SiteMailboxCreating-ServerVersion");
				return new ServerVersionAnchorMailbox<EcpService>(new ServerVersion(Server.E15MinVersion), 0, this);
			}
			AnchorMailbox anchorMailbox = AnchorMailboxFactory.CreateFromCaller(this);
			if (anchorMailbox is AnonymousAnchorMailbox)
			{
				return anchorMailbox;
			}
			if (anchorMailbox is DomainAnchorMailbox || anchorMailbox is OrganizationAnchorMailbox)
			{
				return anchorMailbox;
			}
			SidAnchorMailbox sidAnchorMailbox = anchorMailbox as SidAnchorMailbox;
			if (sidAnchorMailbox != null)
			{
				if (sidAnchorMailbox.OrganizationId == null)
				{
					throw new InvalidOperationException(string.Format("OrganizationId is null for site mailbox proxy {0}.", anchorMailbox.ToString()));
				}
				base.Logger.Set(3, "SiteMailboxCreating-Organization");
				return new OrganizationAnchorMailbox(sidAnchorMailbox.OrganizationId, this);
			}
			else
			{
				UserBasedAnchorMailbox userBasedAnchorMailbox = anchorMailbox as UserBasedAnchorMailbox;
				if (userBasedAnchorMailbox == null)
				{
					throw new InvalidOperationException(string.Format("Unknown site mailbox proxy {0}.", anchorMailbox.ToString()));
				}
				OrganizationId organizationId = (OrganizationId)userBasedAnchorMailbox.GetADRawEntry()[ADObjectSchema.OrganizationId];
				if (organizationId == null)
				{
					throw new InvalidOperationException(string.Format("OrganizationId is null for site mailbox proxy {0}.", anchorMailbox.ToString()));
				}
				base.Logger.Set(3, "SiteMailboxCreating-Organization");
				return new OrganizationAnchorMailbox(organizationId, this);
			}
		}
	}
}
