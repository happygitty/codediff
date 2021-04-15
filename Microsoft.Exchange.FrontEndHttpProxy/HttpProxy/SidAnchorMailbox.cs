using System;
using System.Security.Principal;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200002B RID: 43
	internal class SidAnchorMailbox : UserBasedAnchorMailbox
	{
		// Token: 0x0600015C RID: 348 RVA: 0x00007672 File Offset: 0x00005872
		public SidAnchorMailbox(SecurityIdentifier sid, IRequestContext requestContext) : base(AnchorSource.Sid, sid, requestContext)
		{
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0000767D File Offset: 0x0000587D
		public SidAnchorMailbox(string sid, IRequestContext requestContext) : this(new SecurityIdentifier(sid), requestContext)
		{
		}

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x0600015E RID: 350 RVA: 0x0000768C File Offset: 0x0000588C
		public SecurityIdentifier Sid
		{
			get
			{
				return (SecurityIdentifier)base.SourceObject;
			}
		}

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x0600015F RID: 351 RVA: 0x00007699 File Offset: 0x00005899
		// (set) Token: 0x06000160 RID: 352 RVA: 0x000076A1 File Offset: 0x000058A1
		public OrganizationId OrganizationId { get; set; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x06000161 RID: 353 RVA: 0x000076AA File Offset: 0x000058AA
		// (set) Token: 0x06000162 RID: 354 RVA: 0x000076B2 File Offset: 0x000058B2
		public string SmtpOrLiveId { get; set; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x06000163 RID: 355 RVA: 0x000076BB File Offset: 0x000058BB
		// (set) Token: 0x06000164 RID: 356 RVA: 0x000076C3 File Offset: 0x000058C3
		public string PartitionId { get; set; }

		// Token: 0x06000165 RID: 357 RVA: 0x000076CC File Offset: 0x000058CC
		public override string GetOrganizationNameForLogging()
		{
			if (this.OrganizationId != null)
			{
				return this.OrganizationId.GetFriendlyName();
			}
			return base.GetOrganizationNameForLogging();
		}

		// Token: 0x06000166 RID: 358 RVA: 0x000076F0 File Offset: 0x000058F0
		public override ITenantContext GetTenantContext()
		{
			if (this.SmtpOrLiveId != null && SmtpAddress.IsValidSmtpAddress(this.SmtpOrLiveId))
			{
				return new DomainTenantContext(SmtpAddress.Parse(this.SmtpOrLiveId).Domain);
			}
			if (this.OrganizationId != null)
			{
				return new ExternalDirectoryOrganizationIdTenantContext(this.OrganizationId.SafeToExternalDirectoryOrganizationIdGuid());
			}
			return new ExternalDirectoryOrganizationIdTenantContext(Guid.Empty);
		}

		// Token: 0x06000167 RID: 359 RVA: 0x00007754 File Offset: 0x00005954
		protected override ADRawEntry LoadADRawEntry()
		{
			IRecipientSession session = null;
			if (this.OrganizationId != null)
			{
				session = DirectoryHelper.GetRecipientSessionFromOrganizationId(base.RequestContext.LatencyTracker, this.OrganizationId, base.RequestContext.Logger);
			}
			else if (this.PartitionId != null)
			{
				session = DirectoryHelper.GetRecipientSessionFromPartition(base.RequestContext.LatencyTracker, this.PartitionId, base.RequestContext.Logger);
			}
			else if (this.SmtpOrLiveId != null)
			{
				session = DirectoryHelper.GetRecipientSessionFromSmtpOrLiveId(this.SmtpOrLiveId, base.RequestContext.Logger, base.RequestContext.LatencyTracker, false);
			}
			else
			{
				session = DirectoryHelper.GetRootOrgRecipientSession();
			}
			ADRawEntry ret = DirectoryHelper.InvokeAccountForest<ADRawEntry>(base.RequestContext.LatencyTracker, () => session.FindADRawEntryBySid(this.Sid, this.PropertySet, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\SidAnchorMailbox.cs", 151, "LoadADRawEntry"), base.RequestContext.Logger, session);
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(ret);
		}
	}
}
