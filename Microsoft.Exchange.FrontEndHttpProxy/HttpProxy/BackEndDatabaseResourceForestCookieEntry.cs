using System;
using Microsoft.Exchange.ExchangeSystem;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000051 RID: 81
	internal class BackEndDatabaseResourceForestCookieEntry : BackEndDatabaseCookieEntry
	{
		// Token: 0x06000291 RID: 657 RVA: 0x0000D1D8 File Offset: 0x0000B3D8
		public BackEndDatabaseResourceForestCookieEntry(Guid database, string domainName, string resourceForest) : this(database, domainName, resourceForest, ExDateTime.UtcNow + BackEndCookieEntryBase.LongLivedBackEndServerCookieLifeTime)
		{
		}

		// Token: 0x06000292 RID: 658 RVA: 0x0000D1F2 File Offset: 0x0000B3F2
		public BackEndDatabaseResourceForestCookieEntry(Guid database, string domainName, string resourceForest, ExDateTime expiryTime) : base(database, domainName, expiryTime)
		{
			this.ResourceForest = resourceForest;
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x06000293 RID: 659 RVA: 0x0000D205 File Offset: 0x0000B405
		// (set) Token: 0x06000294 RID: 660 RVA: 0x0000D20D File Offset: 0x0000B40D
		public string ResourceForest { get; private set; }

		// Token: 0x06000295 RID: 661 RVA: 0x0000D216 File Offset: 0x0000B416
		public override string ToString()
		{
			return base.ToString() + "~" + this.ResourceForest;
		}
	}
}
