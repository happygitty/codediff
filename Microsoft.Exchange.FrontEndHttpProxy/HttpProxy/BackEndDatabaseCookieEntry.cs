using System;
using Microsoft.Exchange.ExchangeSystem;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200004F RID: 79
	internal class BackEndDatabaseCookieEntry : BackEndCookieEntryBase
	{
		// Token: 0x06000285 RID: 645 RVA: 0x0000D0A8 File Offset: 0x0000B2A8
		public BackEndDatabaseCookieEntry(Guid database, string domain, ExDateTime expiryTime) : base(BackEndCookieEntryType.Database, expiryTime)
		{
			this.Database = database;
			this.Domain = domain;
		}

		// Token: 0x06000286 RID: 646 RVA: 0x0000D0C0 File Offset: 0x0000B2C0
		public BackEndDatabaseCookieEntry(Guid database, string domain) : this(database, domain, ExDateTime.UtcNow + BackEndCookieEntryBase.LongLivedBackEndServerCookieLifeTime)
		{
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x06000287 RID: 647 RVA: 0x0000D0D9 File Offset: 0x0000B2D9
		// (set) Token: 0x06000288 RID: 648 RVA: 0x0000D0E1 File Offset: 0x0000B2E1
		public Guid Database { get; private set; }

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x06000289 RID: 649 RVA: 0x0000D0EA File Offset: 0x0000B2EA
		// (set) Token: 0x0600028A RID: 650 RVA: 0x0000D0F2 File Offset: 0x0000B2F2
		public string Domain { get; private set; }

		// Token: 0x0600028B RID: 651 RVA: 0x0000D0FC File Offset: 0x0000B2FC
		public override string ToString()
		{
			return string.Concat(new string[]
			{
				BackEndCookieEntryBase.ConvertBackEndCookieEntryTypeToString(base.EntryType),
				"~",
				this.Database.ToString(),
				"~",
				this.Domain,
				"~",
				base.ExpiryTime.ToString("s")
			});
		}
	}
}
