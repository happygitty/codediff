using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B0 RID: 176
	internal class PushNotificationsProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x060006E9 RID: 1769 RVA: 0x00028A0E File Offset: 0x00026C0E
		protected override MailboxServerLocator CreateMailboxServerLocator(Guid databaseGuid, string domainName, string resourceForest)
		{
			return base.CreateMailboxServerLocator(databaseGuid, domainName, resourceForest);
		}
	}
}
