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
			OfflineAddressBook offlineAddressBook = DirectoryHelper.GetConfigurationSessionFromExchangeGuidAndDomain(exchangeObjectId, userAcceptedDomain).FindByExchangeObjectId<OfflineAddressBook>(exchangeObjectId, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\Cache\\OABCache.cs", 115, "GetOABFromCacheOrAD");
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

		// Token: 0x020000F4 RID: 244
		internal sealed class OABCacheEntry
		{
			// Token: 0x06000804 RID: 2052 RVA: 0x0002CF4C File Offset: 0x0002B14C
			internal OABCacheEntry(OfflineAddressBook oab)
			{
				this.exchangeVersion = oab.ExchangeVersion;
				this.virtualDirectories = oab.VirtualDirectories;
				this.globalWebDistributionEnabled = oab.GlobalWebDistributionEnabled;
				this.generatingMailbox = oab.GeneratingMailbox;
				this.shadowMailboxDistributionEnabled = oab.ShadowMailboxDistributionEnabled;
			}

			// Token: 0x170001AC RID: 428
			// (get) Token: 0x06000805 RID: 2053 RVA: 0x0002CF9B File Offset: 0x0002B19B
			internal ExchangeObjectVersion ExchangeVersion
			{
				get
				{
					return this.exchangeVersion;
				}
			}

			// Token: 0x170001AD RID: 429
			// (get) Token: 0x06000806 RID: 2054 RVA: 0x0002CFA3 File Offset: 0x0002B1A3
			internal MultiValuedProperty<ADObjectId> VirtualDirectories
			{
				get
				{
					return this.virtualDirectories;
				}
			}

			// Token: 0x170001AE RID: 430
			// (get) Token: 0x06000807 RID: 2055 RVA: 0x0002CFAB File Offset: 0x0002B1AB
			internal bool GlobalWebDistributionEnabled
			{
				get
				{
					return this.globalWebDistributionEnabled;
				}
			}

			// Token: 0x170001AF RID: 431
			// (get) Token: 0x06000808 RID: 2056 RVA: 0x0002CFB3 File Offset: 0x0002B1B3
			internal bool ShadowMailboxDistributionEnabled
			{
				get
				{
					return this.shadowMailboxDistributionEnabled;
				}
			}

			// Token: 0x170001B0 RID: 432
			// (get) Token: 0x06000809 RID: 2057 RVA: 0x0002CFBB File Offset: 0x0002B1BB
			internal ADObjectId GeneratingMailbox
			{
				get
				{
					return this.generatingMailbox;
				}
			}

			// Token: 0x0400049F RID: 1183
			private readonly ExchangeObjectVersion exchangeVersion;

			// Token: 0x040004A0 RID: 1184
			private readonly MultiValuedProperty<ADObjectId> virtualDirectories;

			// Token: 0x040004A1 RID: 1185
			private readonly bool globalWebDistributionEnabled;

			// Token: 0x040004A2 RID: 1186
			private readonly bool shadowMailboxDistributionEnabled;

			// Token: 0x040004A3 RID: 1187
			private readonly ADObjectId generatingMailbox;
		}
	}
}
