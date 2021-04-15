using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000025 RID: 37
	internal class OrganizationAnchorMailbox : UserBasedAnchorMailbox
	{
		// Token: 0x06000130 RID: 304 RVA: 0x00006F14 File Offset: 0x00005114
		public OrganizationAnchorMailbox(OrganizationId orgId, IRequestContext requestContext) : base(AnchorSource.OrganizationId, orgId, requestContext)
		{
		}

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x06000131 RID: 305 RVA: 0x00006F1F File Offset: 0x0000511F
		public OrganizationId OrganizationId
		{
			get
			{
				return (OrganizationId)base.SourceObject;
			}
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00006F2C File Offset: 0x0000512C
		public override ITenantContext GetTenantContext()
		{
			return new ExternalDirectoryOrganizationIdTenantContext(this.OrganizationId.SafeToExternalDirectoryOrganizationIdGuid());
		}

		// Token: 0x06000133 RID: 307 RVA: 0x00006F3E File Offset: 0x0000513E
		public override string ToString()
		{
			return string.Format("{0}~{1}", base.AnchorSource, this.ToCookieKey());
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00006F5C File Offset: 0x0000515C
		public override string ToCookieKey()
		{
			string arg = string.Empty;
			if (this.OrganizationId.ConfigurationUnit != null)
			{
				arg = this.OrganizationId.ConfigurationUnit.Parent.Name;
			}
			return string.Format("{0}@{1}", Constants.OrganizationAnchor, arg);
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00006FA2 File Offset: 0x000051A2
		public override string GetOrganizationNameForLogging()
		{
			return this.OrganizationId.GetFriendlyName();
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00006FB0 File Offset: 0x000051B0
		protected override ADRawEntry LoadADRawEntry()
		{
			IRecipientSession session = DirectoryHelper.GetRecipientSessionFromOrganizationId(base.RequestContext.LatencyTracker, this.OrganizationId, base.RequestContext.Logger);
			ADRawEntry ret = DirectoryHelper.InvokeAccountForest<ADUser>(base.RequestContext.LatencyTracker, () => HttpProxyBackEndHelper.GetDefaultOrganizationMailbox(session, this.ToCookieKey()), base.RequestContext.Logger, session);
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(ret);
		}
	}
}
