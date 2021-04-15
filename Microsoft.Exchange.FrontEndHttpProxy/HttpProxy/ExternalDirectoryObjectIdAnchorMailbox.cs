using System;
using System.Net;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.HttpProxy.Routing;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000020 RID: 32
	internal class ExternalDirectoryObjectIdAnchorMailbox : UserBasedAnchorMailbox
	{
		// Token: 0x06000106 RID: 262 RVA: 0x000064E0 File Offset: 0x000046E0
		public ExternalDirectoryObjectIdAnchorMailbox(string externalDirectoryObjectId, OrganizationId organizationId, IRequestContext requestContext) : base(AnchorSource.ExternalDirectoryObjectId, externalDirectoryObjectId, requestContext)
		{
			this.externalDirectoryObjectId = externalDirectoryObjectId;
			this.organizationId = organizationId;
			base.NotFoundExceptionCreator = delegate()
			{
				string message = string.Format("Cannot find mailbox by ExternalDirectoryObjectId={0} in organizationId={1}.", this.externalDirectoryObjectId, this.organizationId);
				return new HttpProxyException(HttpStatusCode.NotFound, 3010, message);
			};
		}

		// Token: 0x06000107 RID: 263 RVA: 0x0000650C File Offset: 0x0000470C
		public ExternalDirectoryObjectIdAnchorMailbox(string externalDirectoryObjectId, string tenantDomain, IRequestContext requestContext) : base(AnchorSource.ExternalDirectoryObjectId, externalDirectoryObjectId, requestContext)
		{
			this.externalDirectoryObjectId = externalDirectoryObjectId;
			this.tenantDomain = tenantDomain;
			base.NotFoundExceptionCreator = delegate()
			{
				string message = string.Format("Cannot find mailbox by ExternalDirectoryObjectId={0} in tenantDomain={1}.", this.externalDirectoryObjectId, this.tenantDomain);
				return new HttpProxyException(HttpStatusCode.NotFound, 3010, message);
			};
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00006538 File Offset: 0x00004738
		public ExternalDirectoryObjectIdAnchorMailbox(string externalDirectoryObjectId, Guid tenantId, IRequestContext requestContext) : base(AnchorSource.ExternalDirectoryObjectId, externalDirectoryObjectId, requestContext)
		{
			this.externalDirectoryObjectId = externalDirectoryObjectId;
			this.tenantId = tenantId;
			base.NotFoundExceptionCreator = delegate()
			{
				string message = string.Format("Cannot find mailbox by ExternalDirectoryObjectId={0} in tenantId={1}.", this.externalDirectoryObjectId, this.tenantId);
				return new HttpProxyException(HttpStatusCode.NotFound, 3010, message);
			};
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00006564 File Offset: 0x00004764
		public override ITenantContext GetTenantContext()
		{
			if (this.organizationId != null)
			{
				return new ExternalDirectoryOrganizationIdTenantContext(this.organizationId.SafeToExternalDirectoryOrganizationIdGuid());
			}
			if (this.tenantDomain != null)
			{
				return new DomainTenantContext(this.tenantDomain);
			}
			return new ExternalDirectoryOrganizationIdTenantContext(this.tenantId);
		}

		// Token: 0x0600010A RID: 266 RVA: 0x000065A4 File Offset: 0x000047A4
		protected override ADRawEntry LoadADRawEntry()
		{
			IRecipientSession recipientSession = this.GetRecipientSession();
			ADRawEntry ret = DirectoryHelper.InvokeAccountForest<ADMailboxRecipient>(base.RequestContext.LatencyTracker, () => recipientSession.FindByExternalDirectoryObjectId<ADMailboxRecipient>(this.externalDirectoryObjectId, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\ExternalDirectoryObjectIdAnchorMailbox.cs", 150, "LoadADRawEntry"), base.RequestContext.Logger, recipientSession);
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(ret);
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00006600 File Offset: 0x00004800
		protected override IRoutingKey GetRoutingKey()
		{
			if (!string.IsNullOrEmpty(this.tenantDomain))
			{
				return new ExternalDirectoryObjectIdRoutingKey(Guid.Parse(this.externalDirectoryObjectId), this.tenantDomain);
			}
			if (this.tenantId != Guid.Empty)
			{
				return new ExternalDirectoryObjectIdRoutingKey(Guid.Parse(this.externalDirectoryObjectId), this.tenantId);
			}
			if (this.organizationId != null)
			{
				return new ExternalDirectoryObjectIdRoutingKey(Guid.Parse(this.externalDirectoryObjectId), Guid.Parse(this.organizationId.ToExternalDirectoryOrganizationId()));
			}
			return null;
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000668C File Offset: 0x0000488C
		private IRecipientSession GetRecipientSession()
		{
			IRecipientSession result;
			if (!string.IsNullOrEmpty(this.tenantDomain))
			{
				result = DirectorySessionFactory.Default.GetTenantOrRootOrgRecipientSession(true, 2, ADSessionSettings.FromBusinessTenantAcceptedDomain(this.tenantDomain), 192, "GetRecipientSession", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\ExternalDirectoryObjectIdAnchorMailbox.cs");
			}
			else if (this.tenantId != Guid.Empty)
			{
				result = DirectoryHelper.GetRecipientSessionFromExternalDirectoryOrganizationId(base.RequestContext.LatencyTracker, this.tenantId, base.RequestContext.Logger);
			}
			else
			{
				result = DirectorySessionFactory.Default.GetTenantOrRootOrgRecipientSession(true, 2, ADSessionSettings.FromOrganizationIdWithoutRbacScopesServiceOnly(this.organizationId), 208, "GetRecipientSession", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\ExternalDirectoryObjectIdAnchorMailbox.cs");
			}
			return result;
		}

		// Token: 0x040000DC RID: 220
		private readonly string externalDirectoryObjectId;

		// Token: 0x040000DD RID: 221
		private readonly OrganizationId organizationId;

		// Token: 0x040000DE RID: 222
		private readonly string tenantDomain;

		// Token: 0x040000DF RID: 223
		private readonly Guid tenantId;
	}
}
