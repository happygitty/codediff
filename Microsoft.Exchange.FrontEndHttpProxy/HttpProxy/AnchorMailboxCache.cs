using System;
using Microsoft.Exchange.Data.ConfigurationSettings;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.PartitionCache;
using Microsoft.Exchange.SharedCache.Client;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000042 RID: 66
	internal class AnchorMailboxCache
	{
		// Token: 0x06000214 RID: 532 RVA: 0x0000A6D4 File Offset: 0x000088D4
		private AnchorMailboxCache()
		{
			if (HttpProxySettings.AnchorMailboxSharedCacheEnabled.Value)
			{
				this.sharedCacheClient = new SharedCacheClient(WellKnownSharedCache.AnchorMailboxCache, "AnchorMailboxCache_" + HttpProxyGlobals.ProtocolType, GuardedSharedCacheExecution.Default.Guard);
			}
			if (AnchorMailboxCache.InMemoryCacheEnabled.Value)
			{
				TimeSpan timeSpan = HttpProxySettings.AnchorMailboxSharedCacheEnabled.Value ? AnchorMailboxCache.CacheAbsoluteTimeoutWithSharedCache.Value : AnchorMailboxCache.CacheAbsoluteTimeoutInMemoryCache.Value;
				this.inMemoryCache = new PartitionCache<string, AnchorMailboxCacheEntry>(AnchorMailboxCache.NumCachePartitions.Value, AnchorMailboxCache.CacheExpiryInterval.Value, AnchorMailboxCache.AnchorMailboxCacheSize.Value, AnchorMailboxCache.AnchorMailboxCacheSize.Value + (int)((double)AnchorMailboxCache.AnchorMailboxCacheSize.Value / 10.0), timeSpan, new ExchangeWatson());
			}
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x06000215 RID: 533 RVA: 0x0000A7A0 File Offset: 0x000089A0
		public static AnchorMailboxCache Instance
		{
			get
			{
				if (AnchorMailboxCache.instance == null)
				{
					object obj = AnchorMailboxCache.staticLock;
					lock (obj)
					{
						if (AnchorMailboxCache.instance == null)
						{
							AnchorMailboxCache.instance = new AnchorMailboxCache();
						}
					}
				}
				return AnchorMailboxCache.instance;
			}
		}

		// Token: 0x06000216 RID: 534 RVA: 0x0000A7F8 File Offset: 0x000089F8
		public bool TryGet(string key, IRequestContext requestContext, out AnchorMailboxCacheEntry entry)
		{
			PerfCounters.HttpProxyCacheCountersInstance.AnchorMailboxLocalCacheHitsRateBase.Increment();
			PerfCounters.HttpProxyCacheCountersInstance.AnchorMailboxOverallCacheHitsRateBase.Increment();
			entry = null;
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (this.TryGetFromInMemoryCache(key, out entry))
			{
				PerfCounters.HttpProxyCacheCountersInstance.AnchorMailboxLocalCacheHitsRate.Increment();
			}
			else
			{
				SharedCacheDiagnostics sharedCacheDiagnostics;
				bool flag = this.TryGetFromSharedCache(key, out entry, out sharedCacheDiagnostics);
				AnchorMailboxCache.LogSharedCacheDiagnostics(requestContext, sharedCacheDiagnostics);
				if (flag && this.TryAddToInMemoryCache(key, entry))
				{
					this.UpdateInMemoryCacheSizeCounter();
				}
			}
			if (entry != null)
			{
				PerfCounters.HttpProxyCacheCountersInstance.AnchorMailboxOverallCacheHitsRate.Increment();
				return true;
			}
			return false;
		}

		// Token: 0x06000217 RID: 535 RVA: 0x0000A890 File Offset: 0x00008A90
		public void Add(string key, AnchorMailboxCacheEntry entry, DateTime valueTimestamp, IRequestContext requestContext)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (entry == null)
			{
				throw new ArgumentNullException("entry");
			}
			if (this.TryAddToInMemoryCache(key, entry))
			{
				this.UpdateInMemoryCacheSizeCounter();
			}
			SharedCacheDiagnostics sharedCacheDiagnostics;
			this.TryAddToSharedCache(key, entry, valueTimestamp, out sharedCacheDiagnostics);
			AnchorMailboxCache.LogSharedCacheDiagnostics(requestContext, sharedCacheDiagnostics);
		}

		// Token: 0x06000218 RID: 536 RVA: 0x0000A8E0 File Offset: 0x00008AE0
		public void Remove(string key, IRequestContext requestContext)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (this.TryRemoveFromInMemoryCache(key))
			{
				this.UpdateInMemoryCacheSizeCounter();
			}
			SharedCacheDiagnostics sharedCacheDiagnostics;
			if (this.TryRemoveFromSharedCache(key, out sharedCacheDiagnostics))
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(requestContext.Logger, "SharedCache", "AMCacheRemovalSuccess");
			}
			else
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericError(requestContext.Logger, "SharedCache", "AMCacheRemovalFailure");
			}
			AnchorMailboxCache.LogSharedCacheDiagnostics(requestContext, sharedCacheDiagnostics);
		}

		// Token: 0x06000219 RID: 537 RVA: 0x0000A948 File Offset: 0x00008B48
		private static void LogSharedCacheDiagnostics(IRequestContext requestContext, SharedCacheDiagnostics sharedCacheDiagnostics)
		{
			if (requestContext != null && sharedCacheDiagnostics != null)
			{
				requestContext.LogSharedCacheCall(sharedCacheDiagnostics);
			}
		}

		// Token: 0x0600021A RID: 538 RVA: 0x0000A957 File Offset: 0x00008B57
		private void UpdateInMemoryCacheSizeCounter()
		{
			if (AnchorMailboxCache.InMemoryCacheEnabled.Value)
			{
				PerfCounters.HttpProxyCacheCountersInstance.AnchorMailboxCacheSize.RawValue = (long)this.inMemoryCache.Count;
			}
		}

		// Token: 0x0600021B RID: 539 RVA: 0x0000A980 File Offset: 0x00008B80
		private bool TryAddToInMemoryCache(string key, AnchorMailboxCacheEntry entry)
		{
			return AnchorMailboxCache.InMemoryCacheEnabled.Value && this.inMemoryCache.TryAddOrUpdate(key, entry);
		}

		// Token: 0x0600021C RID: 540 RVA: 0x0000A99D File Offset: 0x00008B9D
		private bool TryAddToSharedCache(string key, AnchorMailboxCacheEntry entry, DateTime valueTimestamp, out SharedCacheDiagnostics sharedCacheDiagnostics)
		{
			sharedCacheDiagnostics = null;
			return HttpProxySettings.AnchorMailboxSharedCacheEnabled.Value && this.sharedCacheClient.TryInsert(key, entry, valueTimestamp.Ticks, ref sharedCacheDiagnostics);
		}

		// Token: 0x0600021D RID: 541 RVA: 0x0000A9C8 File Offset: 0x00008BC8
		private bool TryRemoveFromInMemoryCache(string key)
		{
			AnchorMailboxCacheEntry anchorMailboxCacheEntry;
			return AnchorMailboxCache.InMemoryCacheEnabled.Value && this.inMemoryCache.TryRemove(key, ref anchorMailboxCacheEntry);
		}

		// Token: 0x0600021E RID: 542 RVA: 0x0000A9F1 File Offset: 0x00008BF1
		private bool TryRemoveFromSharedCache(string key, out SharedCacheDiagnostics sharedCacheDiagnostics)
		{
			sharedCacheDiagnostics = null;
			return HttpProxySettings.AnchorMailboxSharedCacheEnabled.Value && this.sharedCacheClient.TryRemove(key, ref sharedCacheDiagnostics);
		}

		// Token: 0x0600021F RID: 543 RVA: 0x0000AA11 File Offset: 0x00008C11
		private bool TryGetFromInMemoryCache(string key, out AnchorMailboxCacheEntry entry)
		{
			entry = null;
			return AnchorMailboxCache.InMemoryCacheEnabled.Value && this.inMemoryCache.TryGet(key, ref entry);
		}

		// Token: 0x06000220 RID: 544 RVA: 0x0000AA31 File Offset: 0x00008C31
		private bool TryGetFromSharedCache(string key, out AnchorMailboxCacheEntry entry, out SharedCacheDiagnostics sharedCacheDiagnostics)
		{
			entry = null;
			sharedCacheDiagnostics = null;
			return HttpProxySettings.AnchorMailboxSharedCacheEnabled.Value && this.sharedCacheClient.TryGet<AnchorMailboxCacheEntry>(key, ref entry, ref sharedCacheDiagnostics);
		}

		// Token: 0x04000125 RID: 293
		private static readonly FlightableTimeSpanAppSettingsEntry CacheAbsoluteTimeoutInMemoryCache = new FlightableTimeSpanAppSettingsEntry(HttpProxySettings.Prefix("AnchorMailboxCache.InMemoryOnly.AbsoluteTimeout"), 1, () => CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).InMemoryAnchorMailboxCacheTimeoutInMinutes.Value);

		// Token: 0x04000126 RID: 294
		private static readonly TimeSpanAppSettingsEntry CacheAbsoluteTimeoutWithSharedCache = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("AnchorMailboxCache.WithSharedCache.AbsoluteTimeout"), 1, TimeSpan.FromMinutes(6.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x04000127 RID: 295
		private static readonly IntAppSettingsEntry AnchorMailboxCacheSize = new IntAppSettingsEntry(HttpProxySettings.Prefix("AnchorMailboxCache.InMemoryMaxSize"), 100000, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000128 RID: 296
		private static readonly IntAppSettingsEntry NumCachePartitions = new IntAppSettingsEntry(HttpProxySettings.Prefix("AnchorMailboxCache.NumPartitions"), 64, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000129 RID: 297
		private static readonly TimeSpanAppSettingsEntry CacheExpiryInterval = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("AnchorMailboxCache.ExpiryInterval"), 0, TimeSpan.FromSeconds(5.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x0400012A RID: 298
		private static readonly TimeSpanAppSettingsEntry AnchorMailboxCacheSizeCounterUpdateInterval = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("AnchorMailboxCache.SizeCounterUpdateInterval"), 0, TimeSpan.FromSeconds(60.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x0400012B RID: 299
		private static readonly BoolAppSettingsEntry InMemoryCacheEnabled = new BoolAppSettingsEntry(HttpProxySettings.Prefix("AnchorMailboxCache.InMemoryCacheEnabled"), CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).AnchorMailboxCacheInMemoryCache.Enabled, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400012C RID: 300
		private static AnchorMailboxCache instance;

		// Token: 0x0400012D RID: 301
		private static object staticLock = new object();

		// Token: 0x0400012E RID: 302
		private readonly PartitionCache<string, AnchorMailboxCacheEntry> inMemoryCache;

		// Token: 0x0400012F RID: 303
		private SharedCacheClient sharedCacheClient;
	}
}
