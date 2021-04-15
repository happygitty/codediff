using System;
using Microsoft.Exchange.Collections.TimeoutCache;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Common;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Diagnostics.Components.Autodiscover;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000048 RID: 72
	internal sealed class OABCache
	{
		// Token: 0x06000264 RID: 612 RVA: 0x0000C09D File Offset: 0x0000A29D
		private OABCache()
		{
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x06000265 RID: 613 RVA: 0x0000C0C0 File Offset: 0x0000A2C0
		public static OABCache Instance
		{
			get
			{
				if (OABCache.instance == null)
				{
					object obj = OABCache.staticLock;
					lock (obj)
					{
						if (OABCache.instance == null)
						{
							OABCache.instance = new OABCache();
						}
					}
				}
				return OABCache.instance;
			}
		}

		// Token: 0x06000266 RID: 614 RVA: 0x0000C118 File Offset: 0x0000A318
		public OABCache.OABCacheEntry GetOABFromCacheOrAD(Guid exchangeObjectId, string userAcceptedDomain)
		{
			OABCache.OABCacheEntry oabcacheEntry = null;
			if (this.oabTimeoutCache.TryGetValue(exchangeObjectId, ref oabcacheEntry))
			{
				return oabcacheEntry;
			}
			OfflineAddressBook offlineAddressBook = DirectoryHelper.GetConfigurationSessionFromExchangeGuidAndDomain(exchangeObjectId, userAcceptedDomain).FindByExchangeObjectId<OfflineAddressBook>(exchangeObjectId, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Cache\\OABCache.cs", 115, "GetOABFromCacheOrAD");
			if (offlineAddressBook == null)
			{
				throw new ADNoSuchObjectException(new LocalizedString(exchangeObjectId.ToString()));
			}
			oabcacheEntry = new OABCache.OABCacheEntry(offlineAddressBook);
			this.oabTimeoutCache.TryInsertAbsolute(exchangeObjectId, oabcacheEntry, OABCache.cacheTimeToLive.Value);
			return oabcacheEntry;
		}

		// Token: 0x04000156 RID: 342
		private static TimeSpanAppSettingsEntry cacheTimeToLive = new TimeSpanAppSettingsEntry("OabCacheTimeToLive", 0, TimeSpan.FromMinutes(10.0), ExTraceGlobals.FrameworkTracer);

		// Token: 0x04000157 RID: 343
		private static IntAppSettingsEntry cacheBucketSize = new IntAppSettingsEntry("OabCacheMaximumBucketSize", 1000, ExTraceGlobals.FrameworkTracer);

		// Token: 0x04000158 RID: 344
		private static OABCache instance;

		// Token: 0x04000159 RID: 345
		private static object staticLock = new object();

		// Token: 0x0400015A RID: 346
		private ExactTimeoutCache<Guid, OABCache.OABCacheEntry> oabTimeoutCache = new ExactTimeoutCache<Guid, OABCache.OABCacheEntry>(null, null, null, OABCache.cacheBucketSize.Value, false);

		// Token: 0x020000F5 RID: 245
		internal sealed class OABCacheEntry
		{
			// Token: 0x06000809 RID: 2057 RVA: 0x0002CD64 File Offset: 0x0002AF64
			internal OABCacheEntry(OfflineAddressBook oab)
			{
				this.exchangeVersion = oab.ExchangeVersion;
				this.virtualDirectories = oab.VirtualDirectories;
				this.globalWebDistributionEnabled = oab.GlobalWebDistributionEnabled;
				this.generatingMailbox = oab.GeneratingMailbox;
				this.shadowMailboxDistributionEnabled = oab.ShadowMailboxDistributionEnabled;
			}

			// Token: 0x170001AE RID: 430
			// (get) Token: 0x0600080A RID: 2058 RVA: 0x0002CDB3 File Offset: 0x0002AFB3
			internal ExchangeObjectVersion ExchangeVersion
			{
				get
				{
					return this.exchangeVersion;
				}
			}

			// Token: 0x170001AF RID: 431
			// (get) Token: 0x0600080B RID: 2059 RVA: 0x0002CDBB File Offset: 0x0002AFBB
			internal MultiValuedProperty<ADObjectId> VirtualDirectories
			{
				get
				{
					return this.virtualDirectories;
				}
			}

			// Token: 0x170001B0 RID: 432
			// (get) Token: 0x0600080C RID: 2060 RVA: 0x0002CDC3 File Offset: 0x0002AFC3
			internal bool GlobalWebDistributionEnabled
			{
				get
				{
					return this.globalWebDistributionEnabled;
				}
			}

			// Token: 0x170001B1 RID: 433
			// (get) Token: 0x0600080D RID: 2061 RVA: 0x0002CDCB File Offset: 0x0002AFCB
			internal bool ShadowMailboxDistributionEnabled
			{
				get
				{
					return this.shadowMailboxDistributionEnabled;
				}
			}

			// Token: 0x170001B2 RID: 434
			// (get) Token: 0x0600080E RID: 2062 RVA: 0x0002CDD3 File Offset: 0x0002AFD3
			internal ADObjectId GeneratingMailbox
			{
				get
				{
					return this.generatingMailbox;
				}
			}

			// Token: 0x0400049B RID: 1179
			private readonly ExchangeObjectVersion exchangeVersion;

			// Token: 0x0400049C RID: 1180
			private readonly MultiValuedProperty<ADObjectId> virtualDirectories;

			// Token: 0x0400049D RID: 1181
			private readonly bool globalWebDistributionEnabled;

			// Token: 0x0400049E RID: 1182
			private readonly bool shadowMailboxDistributionEnabled;

			// Token: 0x0400049F RID: 1183
			private readonly ADObjectId generatingMailbox;
		}
	}
}
