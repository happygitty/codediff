using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000027 RID: 39
	internal class PstProviderAnchorMailbox : UserBasedAnchorMailbox
	{
		// Token: 0x06000138 RID: 312 RVA: 0x00007030 File Offset: 0x00005230
		public PstProviderAnchorMailbox(string pstFilePath, IRequestContext requestContext) : base(AnchorSource.GenericAnchorHint, pstFilePath, requestContext)
		{
		}

		// Token: 0x06000139 RID: 313 RVA: 0x0000703C File Offset: 0x0000523C
		protected override ADRawEntry LoadADRawEntry()
		{
			IRecipientSession recipientSession = null;
			recipientSession = DirectoryHelper.InvokeGls<IRecipientSession>(base.RequestContext.LatencyTracker, () => DirectorySessionFactory.Default.GetTenantOrRootOrgRecipientSession(true, 2, ADSessionSettings.FromOrganizationIdWithoutRbacScopesServiceOnly(OrganizationId.ForestWideOrgId), 51, "LoadADRawEntry", "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\PstProviderAnchorMailbox.cs"), base.RequestContext.Logger);
			ADRawEntry ret = DirectoryHelper.InvokeAccountForest<ADUser>(base.RequestContext.LatencyTracker, () => HttpProxyBackEndHelper.GetOrganizationMailboxInClosestSite(recipientSession, 52), base.RequestContext.Logger, recipientSession);
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(ret);
		}
	}
}
