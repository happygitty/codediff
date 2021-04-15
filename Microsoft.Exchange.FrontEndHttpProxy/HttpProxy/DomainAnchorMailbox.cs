using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.HttpProxy.Routing;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000021 RID: 33
	internal class DomainAnchorMailbox : UserBasedAnchorMailbox
	{
		// Token: 0x06000110 RID: 272 RVA: 0x000067D1 File Offset: 0x000049D1
		public DomainAnchorMailbox(string domain, IRequestContext requestContext) : this(AnchorSource.Domain, domain, requestContext)
		{
		}

		// Token: 0x06000111 RID: 273 RVA: 0x000067DC File Offset: 0x000049DC
		protected DomainAnchorMailbox(AnchorSource anchorSource, object sourceObject, IRequestContext requestContext) : base(AnchorSource.Domain, sourceObject, requestContext)
		{
		}

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000112 RID: 274 RVA: 0x0000618F File Offset: 0x0000438F
		public virtual string Domain
		{
			get
			{
				return (string)base.SourceObject;
			}
		}

		// Token: 0x06000113 RID: 275 RVA: 0x000067E8 File Offset: 0x000049E8
		public override string GetOrganizationNameForLogging()
		{
			string organizationNameForLogging = base.GetOrganizationNameForLogging();
			if (string.IsNullOrEmpty(organizationNameForLogging))
			{
				return this.Domain;
			}
			return organizationNameForLogging;
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000680C File Offset: 0x00004A0C
		public override ITenantContext GetTenantContext()
		{
			return new DomainTenantContext(this.Domain);
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000681C File Offset: 0x00004A1C
		protected override IRoutingKey GetRoutingKey()
		{
			Guid guid;
			if (Guid.TryParse(this.Domain, out guid))
			{
				return new OrganizationGuidRoutingKey(guid);
			}
			return new DomainRoutingKey(this.Domain);
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000684C File Offset: 0x00004A4C
		protected override ADRawEntry LoadADRawEntry()
		{
			IRecipientSession session = this.GetDomainRecipientSession();
			ADRawEntry ret = DirectoryHelper.InvokeAccountForest<ADUser>(base.RequestContext.LatencyTracker, () => HttpProxyBackEndHelper.GetDefaultOrganizationMailbox(session, this.ToCookieKey()), base.RequestContext.Logger, session);
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(ret);
		}

		// Token: 0x06000117 RID: 279 RVA: 0x000068A8 File Offset: 0x00004AA8
		protected IRecipientSession GetDomainRecipientSession()
		{
			IRecipientSession result = null;
			try
			{
				Guid externalOrgId = new Guid(this.Domain);
				result = DirectoryHelper.GetRecipientSessionFromExternalDirectoryOrganizationId(base.RequestContext.LatencyTracker, externalOrgId, base.RequestContext.Logger);
			}
			catch (FormatException)
			{
				result = DirectoryHelper.GetBusinessTenantRecipientSessionFromDomain(this.Domain, base.RequestContext.Logger, base.RequestContext.LatencyTracker);
			}
			return result;
		}
	}
}
