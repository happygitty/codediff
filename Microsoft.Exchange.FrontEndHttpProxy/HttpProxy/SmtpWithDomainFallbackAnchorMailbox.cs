using System;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200002D RID: 45
	internal class SmtpWithDomainFallbackAnchorMailbox : SmtpAnchorMailbox
	{
		// Token: 0x06000171 RID: 369 RVA: 0x000079AB File Offset: 0x00005BAB
		public SmtpWithDomainFallbackAnchorMailbox(string smtp, IRequestContext requestContext) : base(smtp, requestContext)
		{
		}

		// Token: 0x06000172 RID: 370 RVA: 0x000079B8 File Offset: 0x00005BB8
		protected override ADRawEntry LoadADRawEntry()
		{
			IRecipientSession session = DirectoryHelper.GetRecipientSessionFromSmtpOrLiveId(base.Smtp, base.RequestContext.Logger, base.RequestContext.LatencyTracker, false);
			ADRawEntry adrawEntry = DirectoryHelper.InvokeAccountForest<ADRawEntry>(base.RequestContext.LatencyTracker, () => session.FindByProxyAddress(new SmtpProxyAddress(this.Smtp, true), this.PropertySet, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\SmtpWithDomainFallbackAnchorMailbox.cs", 53, "LoadADRawEntry"), base.RequestContext.Logger, session);
			if (adrawEntry == null)
			{
				PerfCounters.HttpProxyCountersInstance.RedirectByTenantMailboxCount.Increment();
				adrawEntry = DirectoryHelper.InvokeAccountForest<ADUser>(base.RequestContext.LatencyTracker, () => HttpProxyBackEndHelper.GetDefaultOrganizationMailbox(session, this.ToCookieKey()), base.RequestContext.Logger, session);
			}
			else
			{
				PerfCounters.HttpProxyCountersInstance.RedirectBySenderMailboxCount.Increment();
			}
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(adrawEntry);
		}
	}
}
