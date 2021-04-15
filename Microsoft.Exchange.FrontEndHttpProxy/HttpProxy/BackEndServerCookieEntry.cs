using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.ExchangeSystem;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000052 RID: 82
	internal class BackEndServerCookieEntry : BackEndCookieEntryBase
	{
		// Token: 0x06000296 RID: 662 RVA: 0x0000D22E File Offset: 0x0000B42E
		public BackEndServerCookieEntry(string fqdn, int version, ExDateTime expiryTime) : base(BackEndCookieEntryType.Server, expiryTime)
		{
			if (string.IsNullOrEmpty(fqdn))
			{
				throw new ArgumentNullException("fqdn");
			}
			this.Fqdn = fqdn;
			this.Version = version;
		}

		// Token: 0x06000297 RID: 663 RVA: 0x0000D259 File Offset: 0x0000B459
		public BackEndServerCookieEntry(string fqdn, int version) : this(fqdn, version, ExDateTime.UtcNow + BackEndCookieEntryBase.BackEndServerCookieLifeTime)
		{
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x06000298 RID: 664 RVA: 0x0000D272 File Offset: 0x0000B472
		// (set) Token: 0x06000299 RID: 665 RVA: 0x0000D27A File Offset: 0x0000B47A
		public string Fqdn { get; private set; }

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x0600029A RID: 666 RVA: 0x0000D283 File Offset: 0x0000B483
		// (set) Token: 0x0600029B RID: 667 RVA: 0x0000D28B File Offset: 0x0000B48B
		public int Version { get; private set; }

		// Token: 0x0600029C RID: 668 RVA: 0x0000D294 File Offset: 0x0000B494
		public override bool ShouldInvalidate(BackEndServer badTarget)
		{
			if (badTarget == null)
			{
				throw new ArgumentNullException("badTarget");
			}
			return string.Equals(this.Fqdn, badTarget.Fqdn, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x0600029D RID: 669 RVA: 0x0000D2B8 File Offset: 0x0000B4B8
		public override string ToString()
		{
			return string.Concat(new string[]
			{
				BackEndCookieEntryBase.ConvertBackEndCookieEntryTypeToString(base.EntryType),
				"~",
				this.Fqdn,
				"~",
				this.Version.ToString(),
				"~",
				base.ExpiryTime.ToString("s")
			});
		}
	}
}
