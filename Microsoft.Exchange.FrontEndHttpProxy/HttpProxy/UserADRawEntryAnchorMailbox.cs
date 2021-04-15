using System;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000030 RID: 48
	internal class UserADRawEntryAnchorMailbox : ArchiveSupportedAnchorMailbox
	{
		// Token: 0x06000181 RID: 385 RVA: 0x00007FCF File Offset: 0x000061CF
		public UserADRawEntryAnchorMailbox(ADRawEntry activeDirectoryRawEntry, IRequestContext requestContext) : base(AnchorSource.UserADRawEntry, (activeDirectoryRawEntry != null && activeDirectoryRawEntry.Id != null) ? activeDirectoryRawEntry.Id.DistinguishedName : null, requestContext)
		{
			this.activeDirectoryRawEntry = activeDirectoryRawEntry;
		}

		// Token: 0x06000182 RID: 386 RVA: 0x00007FF9 File Offset: 0x000061F9
		public override string GetOrganizationNameForLogging()
		{
			return ((OrganizationId)this.activeDirectoryRawEntry[ADObjectSchema.OrganizationId]).GetFriendlyName();
		}

		// Token: 0x06000183 RID: 387 RVA: 0x00008015 File Offset: 0x00006215
		public override ITenantContext GetTenantContext()
		{
			return new ExternalDirectoryOrganizationIdTenantContext(base.GetExternalDirectoryOrganizationGuidFromADRawEntry(this.activeDirectoryRawEntry));
		}

		// Token: 0x06000184 RID: 388 RVA: 0x00008028 File Offset: 0x00006228
		protected override ADRawEntry LoadADRawEntry()
		{
			return this.activeDirectoryRawEntry;
		}

		// Token: 0x04000102 RID: 258
		private ADRawEntry activeDirectoryRawEntry;
	}
}
