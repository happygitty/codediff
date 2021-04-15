using System;
using Microsoft.Exchange.ExchangeSystem;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000050 RID: 80
	internal class BackEndDatabaseOrganizationAwareCookieEntry : BackEndDatabaseResourceForestCookieEntry
	{
		// Token: 0x0600028C RID: 652 RVA: 0x0000D170 File Offset: 0x0000B370
		public BackEndDatabaseOrganizationAwareCookieEntry(Guid database, string domainName, string resourceForest, bool isOrganizationMailboxDatabase) : this(database, domainName, resourceForest, ExDateTime.UtcNow + BackEndCookieEntryBase.LongLivedBackEndServerCookieLifeTime, isOrganizationMailboxDatabase)
		{
		}

		// Token: 0x0600028D RID: 653 RVA: 0x0000D18C File Offset: 0x0000B38C
		public BackEndDatabaseOrganizationAwareCookieEntry(Guid database, string domainName, string resourceForest, ExDateTime expiryTime, bool isOrganizationMailboxDatabase) : base(database, domainName, resourceForest, expiryTime)
		{
			this.IsOrganizationMailboxDatabase = isOrganizationMailboxDatabase;
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x0600028E RID: 654 RVA: 0x0000D1A1 File Offset: 0x0000B3A1
		// (set) Token: 0x0600028F RID: 655 RVA: 0x0000D1A9 File Offset: 0x0000B3A9
		public bool IsOrganizationMailboxDatabase { get; private set; }

		// Token: 0x06000290 RID: 656 RVA: 0x0000D1B2 File Offset: 0x0000B3B2
		public override string ToString()
		{
			return base.ToString() + "~" + (this.IsOrganizationMailboxDatabase ? "1" : "0");
		}
	}
}
