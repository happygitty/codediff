using System;
using Microsoft.Exchange.ExchangeSystem;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000051 RID: 81
	internal class BackEndDatabaseResourceForestCookieEntry : BackEndDatabaseCookieEntry
	{
		// Token: 0x06000291 RID: 657 RVA: 0x0000D19C File Offset: 0x0000B39C
		public BackEndDatabaseResourceForestCookieEntry(Guid database, string domainName, string resourceForest) : this(database, domainName, resourceForest, ExDateTime.UtcNow + BackEndCookieEntryBase.LongLivedBackEndServerCookieLifeTime)
		{
		}

		// Token: 0x06000292 RID: 658 RVA: 0x0000D1B6 File Offset: 0x0000B3B6
		public BackEndDatabaseResourceForestCookieEntry(Guid database, string domainName, string resourceForest, ExDateTime expiryTime) : base(database, domainName, expiryTime)
		{
			this.ResourceForest = resourceForest;
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x06000293 RID: 659 RVA: 0x0000D1C9 File Offset: 0x0000B3C9
		// (set) Token: 0x06000294 RID: 660 RVA: 0x0000D1D1 File Offset: 0x0000B3D1
		public string ResourceForest { get; private set; }

		// Token: 0x06000295 RID: 661 RVA: 0x0000D1DA File Offset: 0x0000B3DA
		public override string ToString()
		{
			return base.ToString() + "~" + this.ResourceForest;
		}
	}
}
