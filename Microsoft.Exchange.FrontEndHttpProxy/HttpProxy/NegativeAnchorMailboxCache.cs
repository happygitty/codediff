using System;
using Microsoft.Exchange.Collections.TimeoutCache;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000047 RID: 71
	internal class NegativeAnchorMailboxCache
	{
		// Token: 0x06000258 RID: 600 RVA: 0x0000BDC4 File Offset: 0x00009FC4
		internal NegativeAnchorMailboxCache(TimeSpan cacheAbsoluteTimeout, TimeSpan gen1Timeout, TimeSpan gen2Timeout)
		{
			this.cacheAbsoluteTimeout = cacheAbsoluteTimeout;
			this.gen1Timeout = gen1Timeout;
			this.gen2Timeout = gen2Timeout;
			this.innerCache = new ExactTimeoutCache<string, NegativeAnchorMailboxCacheEntry>(delegate(string k, NegativeAnchorMailboxCacheEntry v, RemoveReason r)
			{
				this.UpdateCacheSizeCounter();
			}, null, null, NegativeAnchorMailboxCache.NegativeAnchorMailboxCacheSize.Value, false);
		}

		// Token: 0x06000259 RID: 601 RVA: 0x0000BE10 File Offset: 0x0000A010
		private NegativeAnchorMailboxCache() : this(NegativeAnchorMailboxCache.CacheAbsoluteTimeout.Value, NegativeAnchorMailboxCache.Gen1Timeout.Value, NegativeAnchorMailboxCache.Gen2Timeout.Value)
		{
		}

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x0600025A RID: 602 RVA: 0x0000BE36 File Offset: 0x0000A036
		public static NegativeAnchorMailboxCache Instance
		{
			get
			{
				return NegativeAnchorMailboxCache.StaticInstance;
			}
		}

		// Token: 0x0600025B RID: 603 RVA: 0x0000BE40 File Offset: 0x0000A040
		public void Add(string key, NegativeAnchorMailboxCacheEntry entry)
		{
			TimeSpan timeSpan = this.cacheAbsoluteTimeout;
			NegativeAnchorMailboxCacheEntry negativeAnchorMailboxCacheEntry;
			if (!this.TryGet(key, false, out negativeAnchorMailboxCacheEntry))
			{
				entry.StartTime = DateTime.UtcNow;
				entry.Generation = 1;
				this.Add(key, entry, timeSpan, true);
				return;
			}
			double num;
			NegativeAnchorMailboxCacheEntry.CacheGeneration generation;
			if (!this.IsDueForRefresh(negativeAnchorMailboxCacheEntry, out num, out generation))
			{
				return;
			}
			if (timeSpan.TotalSeconds > num)
			{
				negativeAnchorMailboxCacheEntry.Generation = generation;
				this.Add(key, negativeAnchorMailboxCacheEntry, timeSpan - TimeSpan.FromSeconds(num), false);
			}
		}

		// Token: 0x0600025C RID: 604 RVA: 0x0000BEB4 File Offset: 0x0000A0B4
		public bool TryGet(string key, out NegativeAnchorMailboxCacheEntry entry)
		{
			if (!this.TryGet(key, true, out entry))
			{
				return false;
			}
			double num;
			NegativeAnchorMailboxCacheEntry.CacheGeneration cacheGeneration;
			if (this.IsDueForRefresh(entry, out num, out cacheGeneration))
			{
				return false;
			}
			PerfCounters.HttpProxyCacheCountersInstance.NegativeAnchorMailboxLocalCacheHitsRate.Increment();
			return true;
		}

		// Token: 0x0600025D RID: 605 RVA: 0x0000BEEF File Offset: 0x0000A0EF
		public void Remove(string key)
		{
			this.innerCache.Remove(key);
		}

		// Token: 0x0600025E RID: 606 RVA: 0x0000BF00 File Offset: 0x0000A100
		private bool IsDueForRefresh(NegativeAnchorMailboxCacheEntry entry, out double timeElapsedInSeconds, out NegativeAnchorMailboxCacheEntry.CacheGeneration nextGeneration)
		{
			timeElapsedInSeconds = 0.0;
			nextGeneration = (short)65535;
			if (entry.Generation == 65535)
			{
				return false;
			}
			timeElapsedInSeconds = (DateTime.UtcNow - entry.StartTime).TotalSeconds;
			if (timeElapsedInSeconds > this.gen2Timeout.TotalSeconds)
			{
				nextGeneration = (short)65535;
				return true;
			}
			if (timeElapsedInSeconds > this.gen1Timeout.TotalSeconds)
			{
				nextGeneration = 2;
				return entry.Generation == 1;
			}
			return false;
		}

		// Token: 0x0600025F RID: 607 RVA: 0x0000BF83 File Offset: 0x0000A183
		private bool TryGet(string key, bool updatePerfCounter, out NegativeAnchorMailboxCacheEntry entry)
		{
			if (updatePerfCounter)
			{
				PerfCounters.HttpProxyCacheCountersInstance.NegativeAnchorMailboxLocalCacheHitsRateBase.Increment();
			}
			entry = null;
			return this.innerCache.TryGetValue(key, ref entry);
		}

		// Token: 0x06000260 RID: 608 RVA: 0x0000BFAD File Offset: 0x0000A1AD
		private void Add(string key, NegativeAnchorMailboxCacheEntry entry, TimeSpan timeout, bool updatePerfCounter)
		{
			this.innerCache.TryInsertAbsolute(key, entry, timeout);
			if (updatePerfCounter)
			{
				this.UpdateCacheSizeCounter();
			}
		}

		// Token: 0x06000261 RID: 609 RVA: 0x0000BFC8 File Offset: 0x0000A1C8
		private void UpdateCacheSizeCounter()
		{
			PerfCounters.HttpProxyCacheCountersInstance.NegativeAnchorMailboxCacheSize.RawValue = (long)this.innerCache.Count;
		}

		// Token: 0x0400014D RID: 333
		private static readonly TimeSpanAppSettingsEntry CacheAbsoluteTimeout = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("NegativeAnchorMailboxCacheAbsoluteTimeout"), 0, TimeSpan.FromSeconds(86400.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x0400014E RID: 334
		private static readonly TimeSpanAppSettingsEntry Gen1Timeout = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("NegativeAnchorMailboxCacheG1Timeout"), 0, TimeSpan.FromSeconds(300.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x0400014F RID: 335
		private static readonly TimeSpanAppSettingsEntry Gen2Timeout = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("NegativeAnchorMailboxCacheG2Timeout"), 0, TimeSpan.FromSeconds(2100.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x04000150 RID: 336
		private static readonly IntAppSettingsEntry NegativeAnchorMailboxCacheSize = new IntAppSettingsEntry(HttpProxySettings.Prefix("NegativeAnchorMailboxCacheSize"), 4000, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000151 RID: 337
		private static readonly NegativeAnchorMailboxCache StaticInstance = new NegativeAnchorMailboxCache();

		// Token: 0x04000152 RID: 338
		private readonly ExactTimeoutCache<string, NegativeAnchorMailboxCacheEntry> innerCache;

		// Token: 0x04000153 RID: 339
		private readonly TimeSpan cacheAbsoluteTimeout;

		// Token: 0x04000154 RID: 340
		private readonly TimeSpan gen1Timeout;

		// Token: 0x04000155 RID: 341
		private readonly TimeSpan gen2Timeout;
	}
}
