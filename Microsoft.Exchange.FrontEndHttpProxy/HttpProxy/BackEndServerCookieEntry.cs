using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.ExchangeSystem;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000052 RID: 82
	internal class BackEndServerCookieEntry : BackEndCookieEntryBase
	{
		// Token: 0x06000296 RID: 662 RVA: 0x0000D1F2 File Offset: 0x0000B3F2
		public BackEndServerCookieEntry(string fqdn, int version, ExDateTime expiryTime) : base(BackEndCookieEntryType.Server, expiryTime)
		{
			if (string.IsNullOrEmpty(fqdn))
			{
				throw new ArgumentNullException("fqdn");
			}
			this.Fqdn = fqdn;
			this.Version = version;
		}

		// Token: 0x06000297 RID: 663 RVA: 0x0000D21D File Offset: 0x0000B41D
		public BackEndServerCookieEntry(string fqdn, int version) : this(fqdn, version, ExDateTime.UtcNow + BackEndCookieEntryBase.BackEndServerCookieLifeTime)
		{
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x06000298 RID: 664 RVA: 0x0000D236 File Offset: 0x0000B436
		// (set) Token: 0x06000299 RID: 665 RVA: 0x0000D23E File Offset: 0x0000B43E
		public string Fqdn { get; private set; }

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x0600029A RID: 666 RVA: 0x0000D247 File Offset: 0x0000B447
		// (set) Token: 0x0600029B RID: 667 RVA: 0x0000D24F File Offset: 0x0000B44F
		public int Version { get; private set; }

		// Token: 0x0600029C RID: 668 RVA: 0x0000D258 File Offset: 0x0000B458
		public override bool ShouldInvalidate(BackEndServer badTarget)
		{
			if (badTarget == null)
			{
				throw new ArgumentNullException("badTarget");
			}
			return string.Equals(this.Fqdn, badTarget.Fqdn, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x0600029D RID: 669 RVA: 0x0000D27C File Offset: 0x0000B47C
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
