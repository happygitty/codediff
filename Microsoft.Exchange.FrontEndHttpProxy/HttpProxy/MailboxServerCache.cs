using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.ServerLocator;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.HttpProxy.EventLogs;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.PartitionCache;
using Microsoft.Exchange.SharedCache.Client;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000045 RID: 69
	internal class MailboxServerCache
	{
		// Token: 0x0600022F RID: 559 RVA: 0x0000AD3C File Offset: 0x00008F3C
		private MailboxServerCache()
		{
			if (HttpProxySettings.MailboxServerLocatorSharedCacheEnabled.Value)
			{
				this.sharedCacheClient = new SharedCacheClient(WellKnownSharedCache.MailboxServerLocator, "MailboxServerLocator_" + HttpProxyGlobals.ProtocolType, GuardedSharedCacheExecution.Default.Guard);
			}
			if (MailboxServerCache.InMemoryCacheEnabled.Value)
			{
				TimeSpan timeSpan = HttpProxySettings.MailboxServerLocatorSharedCacheEnabled.Value ? MailboxServerCache.MailboxServerCacheAbsoluteTimeoutWithSharedCache.Value : MailboxServerCache.MailboxServerCacheAbsoluteTimeoutInMemoryCache.Value;
				this.inMemoryCache = new PartitionCache<string, MailboxServerCacheEntry>(MailboxServerCache.NumCachePartitions.Value, MailboxServerCache.CacheExpiryInterval.Value, MailboxServerCache.MailboxServerCacheMaxSize.Value, MailboxServerCache.MailboxServerCacheMaxSize.Value + (int)((double)MailboxServerCache.MailboxServerCacheMaxSize.Value / 10.0), timeSpan, new ExchangeWatson());
				if (MailboxServerCache.InternalRefreshEnabled.Value)
				{
					ThreadPool.QueueUserWorkItem(new WaitCallback(this.CacheRefreshEntry));
				}
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x06000230 RID: 560 RVA: 0x0000AE34 File Offset: 0x00009034
		public static MailboxServerCache Instance
		{
			get
			{
				if (MailboxServerCache.instance == null)
				{
					object obj = MailboxServerCache.staticLock;
					lock (obj)
					{
						if (MailboxServerCache.instance == null)
						{
							MailboxServerCache.instance = new MailboxServerCache();
						}
					}
				}
				return MailboxServerCache.instance;
			}
		}

		// Token: 0x06000231 RID: 561 RVA: 0x0000AE8C File Offset: 0x0000908C
		public bool TryGet(Guid database, out BackEndServer backEndServer)
		{
			backEndServer = null;
			MailboxServerCacheEntry mailboxServerCacheEntry;
			if (this.TryGet(database, null, out mailboxServerCacheEntry))
			{
				backEndServer = mailboxServerCacheEntry.BackEndServer;
				return true;
			}
			return false;
		}

		// Token: 0x06000232 RID: 562 RVA: 0x0000AEB4 File Offset: 0x000090B4
		public bool TryGet(Guid database, IRequestContext requestContext, out BackEndServer backEndServer)
		{
			backEndServer = null;
			MailboxServerCacheEntry mailboxServerCacheEntry;
			if (this.TryGet(database, requestContext, out mailboxServerCacheEntry))
			{
				backEndServer = mailboxServerCacheEntry.BackEndServer;
				return true;
			}
			return false;
		}

		// Token: 0x06000233 RID: 563 RVA: 0x0000AEDB File Offset: 0x000090DB
		public bool TryGet(Guid database, out MailboxServerCacheEntry cacheEntry)
		{
			return this.TryGet(database, null, out cacheEntry);
		}

		// Token: 0x06000234 RID: 564 RVA: 0x0000AEE8 File Offset: 0x000090E8
		public bool TryGet(Guid database, IRequestContext requestContext, out MailboxServerCacheEntry cacheEntry)
		{
			cacheEntry = null;
			PerfCounters.HttpProxyCacheCountersInstance.BackEndServerLocalCacheHitsRateBase.Increment();
			PerfCounters.HttpProxyCacheCountersInstance.BackEndServerOverallCacheHitsRateBase.Increment();
			PerfCounters.IncrementMovingPercentagePerformanceCounterBase(PerfCounters.HttpProxyCacheCountersInstance.MovingPercentageBackEndServerLocalCacheHitsRate);
			PerfCounters.IncrementMovingPercentagePerformanceCounterBase(PerfCounters.HttpProxyCacheCountersInstance.MovingPercentageBackEndServerOverallCacheHitsRate);
			string key = database.ToString();
			bool flag = this.TryGetFromInMemoryCache(key, out cacheEntry);
			if (flag)
			{
				if (MailboxServerCache.IsE14ServerStale(cacheEntry))
				{
					this.Remove(database, requestContext);
					return false;
				}
				PerfCounters.HttpProxyCacheCountersInstance.BackEndServerLocalCacheHitsRate.Increment();
				PerfCounters.UpdateMovingPercentagePerformanceCounter(PerfCounters.HttpProxyCacheCountersInstance.MovingPercentageBackEndServerLocalCacheHitsRate);
			}
			else
			{
				SharedCacheDiagnostics sharedCacheDiagnostics = null;
				flag = this.TryGetFromSharedCache(key, out cacheEntry, out sharedCacheDiagnostics);
				MailboxServerCache.LogSharedCacheDiagnostics(requestContext, sharedCacheDiagnostics);
				if (flag && this.TryAddToInMemoryCache(key, cacheEntry))
				{
					this.UpdateInMemoryCacheSizeCounter();
				}
			}
			if (flag)
			{
				PerfCounters.HttpProxyCacheCountersInstance.BackEndServerOverallCacheHitsRate.Increment();
				PerfCounters.UpdateMovingPercentagePerformanceCounter(PerfCounters.HttpProxyCacheCountersInstance.MovingPercentageBackEndServerOverallCacheHitsRate);
				if (MailboxServerCache.InMemoryCacheEnabled.Value && MailboxServerCache.InternalRefreshEnabled.Value && cacheEntry.IsDueForRefresh(MailboxServerCache.GetRefreshInterval(cacheEntry.BackEndServer)))
				{
					this.RegisterRefresh(new DatabaseWithForest(database, cacheEntry.ResourceForest, requestContext.ActivityId));
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(requestContext.Logger, "ServerLocatorRefresh", database);
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(requestContext.Logger, "RefreshingCacheEntry", cacheEntry.ToString());
				}
			}
			return flag;
		}

		// Token: 0x06000235 RID: 565 RVA: 0x0000B047 File Offset: 0x00009247
		public BackEndServer ServerLocatorEndGetServer(MailboxServerLocator locator, IAsyncResult asyncResult, IRequestContext requestContext)
		{
			return this.ServerLocatorEndGetServer(locator, asyncResult, requestContext.ActivityId);
		}

		// Token: 0x06000236 RID: 566 RVA: 0x0000B057 File Offset: 0x00009257
		public BackEndServer ServerLocatorEndGetServer(MailboxServerLocator locator, IAsyncResult asyncResult, Guid initiatingRequestId)
		{
			if (locator == null)
			{
				throw new ArgumentNullException("locator");
			}
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			BackEndServer result = locator.EndGetServer(asyncResult);
			this.PopulateCache(locator.AvailabilityGroupDatabaseToServerMappings, locator.ResourceForestFqdn);
			return result;
		}

		// Token: 0x06000237 RID: 567 RVA: 0x0000B090 File Offset: 0x00009290
		public void Remove(Guid database, IRequestContext requestContext)
		{
			string key = database.ToString();
			if (this.TryRemoveFromInMemoryCache(key))
			{
				this.UpdateInMemoryCacheSizeCounter();
			}
			SharedCacheDiagnostics sharedCacheDiagnostics;
			if (this.TryRemoveFromSharedCache(key, out sharedCacheDiagnostics))
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(requestContext.Logger, "SharedCache", "MailboxServerCacheEntryRemovalSuccess");
			}
			else
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericError(requestContext.Logger, "SharedCache", "MailboxServerCacheEntryRemovalFailure");
			}
			MailboxServerCache.LogSharedCacheDiagnostics(requestContext, sharedCacheDiagnostics);
		}

		// Token: 0x06000238 RID: 568 RVA: 0x0000B0F8 File Offset: 0x000092F8
		public BackEndServer GetRandomE15Server(IRequestContext requestContext)
		{
			BackEndServer backEndServer = LocalSiteMailboxServerCache.Instance.TryGetRandomE15Server(requestContext);
			if (backEndServer != null)
			{
				return backEndServer;
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[MailboxServerCache::GetRandomE15Server]: Could not get any available server from local site E15 server list. Will query ServiceDiscovery.");
			}
			return HttpProxyBackEndHelper.GetAnyBackEndServer();
		}

		// Token: 0x06000239 RID: 569 RVA: 0x0000B140 File Offset: 0x00009340
		public void Add(Guid database, BackEndServer server, string resourceForestFqdn, long failoverSequenceNumber, IRequestContext requestContext)
		{
			MailboxServerCacheEntry entry = new MailboxServerCacheEntry(server, resourceForestFqdn, failoverSequenceNumber);
			this.Add(database, entry, requestContext);
		}

		// Token: 0x0600023A RID: 570 RVA: 0x0000B164 File Offset: 0x00009364
		private static int GetMailboxServerCacheInMemoryTimeoutValue()
		{
			int result = 1440;
			string value = CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).InMemoryMailboxServerCacheTimeoutInMinutes.Value;
			int num;
			if (!string.IsNullOrEmpty(value) && int.TryParse(value, out num))
			{
				result = num;
			}
			return result;
		}

		// Token: 0x0600023B RID: 571 RVA: 0x0000B1A4 File Offset: 0x000093A4
		private static bool IsLocalSiteE15MailboxServer(BackEndServer server, string resourceForest)
		{
			if (!server.IsE15OrHigher)
			{
				return false;
			}
			if ((!Utilities.IsPartnerHostedOnly && !CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).NoCrossForestServerLocate.Enabled) || string.IsNullOrEmpty(resourceForest) || string.Equals(HttpProxyGlobals.LocalMachineForest.Member, resourceForest, StringComparison.OrdinalIgnoreCase))
			{
				ServiceTopology currentServiceTopology = ServiceTopology.GetCurrentServiceTopology("d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Cache\\MailboxServerCache.cs", "IsLocalSiteE15MailboxServer", 475);
				Site site = null;
				if (!currentServiceTopology.TryGetSite(server.Fqdn, out site))
				{
					return false;
				}
				if (HttpProxyGlobals.LocalSite.Member.Equals(site))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600023C RID: 572 RVA: 0x0000B231 File Offset: 0x00009431
		private static bool IsE14ServerStale(MailboxServerCacheEntry cacheEntry)
		{
			return !cacheEntry.BackEndServer.IsE15OrHigher && !DownLevelServerManager.IsServerDiscoverable(cacheEntry.BackEndServer.Fqdn);
		}

		// Token: 0x0600023D RID: 573 RVA: 0x0000A948 File Offset: 0x00008B48
		private static void LogSharedCacheDiagnostics(IRequestContext requestContext, SharedCacheDiagnostics sharedCacheDiagnostics)
		{
			if (requestContext != null && sharedCacheDiagnostics != null)
			{
				requestContext.LogSharedCacheCall(sharedCacheDiagnostics);
			}
		}

		// Token: 0x0600023E RID: 574 RVA: 0x0000B257 File Offset: 0x00009457
		private static TimeSpan GetRefreshInterval(BackEndServer server)
		{
			if (server.IsE15OrHigher)
			{
				return MailboxServerCache.MailboxServerCacheRefreshInterval.Value;
			}
			return MailboxServerCache.MailboxServerCacheDownLevelServerRefreshInterval.Value;
		}

		// Token: 0x0600023F RID: 575 RVA: 0x0000B278 File Offset: 0x00009478
		private void Add(Guid database, MailboxServerCacheEntry entry, IRequestContext requestContext)
		{
			string text = database.ToString();
			if (entry == null)
			{
				throw new ArgumentNullException("entry");
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string, BackEndServer>((long)this.GetHashCode(), "[MailboxServerCache::Add]: Adding database {0} with server {1} to cache.", text, entry.BackEndServer);
			}
			if (this.TryAddToInMemoryCache(text, entry))
			{
				this.UpdateInMemoryCacheSizeCounter();
			}
			SharedCacheDiagnostics sharedCacheDiagnostics;
			this.TryAddToSharedCache(text, entry, out sharedCacheDiagnostics);
			MailboxServerCache.LogSharedCacheDiagnostics(requestContext, sharedCacheDiagnostics);
			LocalSiteMailboxServerCache.Instance.Add(database, entry.BackEndServer, entry.ResourceForest);
		}

		// Token: 0x06000240 RID: 576 RVA: 0x0000B304 File Offset: 0x00009504
		private void UpdateInMemoryCacheSizeCounter()
		{
			long rawValue = 0L;
			if (MailboxServerCache.InMemoryCacheEnabled.Value)
			{
				rawValue = (long)this.inMemoryCache.Count;
			}
			PerfCounters.HttpProxyCacheCountersInstance.BackEndServerCacheSize.RawValue = rawValue;
		}

		// Token: 0x06000241 RID: 577 RVA: 0x0000B33D File Offset: 0x0000953D
		private void UpdateQueueLengthCounter()
		{
			PerfCounters.HttpProxyCacheCountersInstance.BackEndServerCacheRefreshingQueueLength.RawValue = (long)this.refreshQueue.Count;
		}

		// Token: 0x06000242 RID: 578 RVA: 0x0000B35A File Offset: 0x0000955A
		private void UpdateRefreshingStatusCounter(bool isRefreshing)
		{
			PerfCounters.HttpProxyCacheCountersInstance.BackEndServerCacheRefreshingStatus.RawValue = (isRefreshing ? 1L : 0L);
		}

		// Token: 0x06000243 RID: 579 RVA: 0x0000B374 File Offset: 0x00009574
		private void RegisterRefresh(DatabaseWithForest database)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[MailboxServerCache::RegisterRefresh]: Enqueueing database {0}.", database.Database.ToString());
			}
			this.refreshQueue.Enqueue(database);
			this.UpdateQueueLengthCounter();
			MailboxServerCache.refreshWorkerSignal.Set();
		}

		// Token: 0x06000244 RID: 580 RVA: 0x0000B3D8 File Offset: 0x000095D8
		private void CacheRefreshEntry(object extraData)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[MailboxServerCache::CacheRefreshEntry]: Refresh thread starting.");
			}
			for (;;)
			{
				try
				{
					this.UpdateRefreshingStatusCounter(true);
					Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_RefreshingBackEndServerCache, null, new object[]
					{
						HttpProxyGlobals.ProtocolType
					});
					this.InternalRefresh();
				}
				catch (Exception ex)
				{
					Diagnostics.ReportException(ex, FrontEndHttpProxyEventLogConstants.Tuple_InternalServerError, null, "Exception from CacheRefreshEntry: {0}");
				}
				finally
				{
					this.UpdateRefreshingStatusCounter(false);
				}
				try
				{
					MailboxServerCache.refreshWorkerSignal.WaitOne();
					continue;
				}
				catch (AbandonedMutexException)
				{
				}
				break;
			}
		}

		// Token: 0x06000245 RID: 581 RVA: 0x0000B490 File Offset: 0x00009690
		private void InternalRefresh()
		{
			DatabaseWithForest databaseWithForest;
			while (this.refreshQueue.TryDequeue(out databaseWithForest))
			{
				this.UpdateQueueLengthCounter();
				if (this.IsDueForRefresh(databaseWithForest.Database))
				{
					this.RefreshDatabase(databaseWithForest);
				}
			}
		}

		// Token: 0x06000246 RID: 582 RVA: 0x0000B4CC File Offset: 0x000096CC
		private void RefreshDatabase(DatabaseWithForest database)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[MailboxServerCache::RefreshDatabase]: Refreshing cache for database {0}.", database.Database.ToString());
			}
			Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_RefreshingDatabaseBackEndServer, null, new object[]
			{
				HttpProxyGlobals.ProtocolType,
				database.Database,
				database.ResourceForest
			});
			Dictionary<Guid, DatabaseToServerMappingInfo> dictionary = null;
			try
			{
				using (MailboxServerLocator mailboxServerLocator = MailboxServerLocator.Create(database.Database, null, database.ResourceForest, true, GuardedSlsExecution.MailboxServerLocatorCallbacks, null))
				{
					PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorCalls.Increment();
					mailboxServerLocator.GetServer();
					dictionary = mailboxServerLocator.AvailabilityGroupDatabaseToServerMappings;
					bool isSourceCachedData = mailboxServerLocator.IsSourceCachedData;
					PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorLatency.RawValue = mailboxServerLocator.Latency;
					PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorAverageLatency.IncrementBy(mailboxServerLocator.Latency);
					PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorAverageLatencyBase.Increment();
					PerfCounters.UpdateMovingAveragePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingAverageMailboxServerLocatorLatency, mailboxServerLocator.Latency);
					PerfCounters.IncrementMovingPercentagePerformanceCounterBase(PerfCounters.HttpProxyCountersInstance.MovingPercentageMailboxServerLocatorRetriedCalls);
					if (mailboxServerLocator.LocatorServiceHosts.Length > 1)
					{
						PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorRetriedCalls.Increment();
						PerfCounters.UpdateMovingPercentagePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingPercentageMailboxServerLocatorRetriedCalls);
					}
				}
			}
			catch (Exception ex)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<Guid, string, Exception>((long)this.GetHashCode(), "[MailboxServerCache::RefreshDatabase]: MailboxServerLocator threw exception when locating database {0} in forest {1}. Error: {2}", database.Database, database.ResourceForest, ex);
				}
				Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_ErrorRefreshingDatabaseBackEndServer, database.Database.ToString(), new object[]
				{
					HttpProxyGlobals.ProtocolType,
					database.Database,
					database.ResourceForest,
					ex.ToString()
				});
				if (ex is ServerLocatorClientException || ex is ServerLocatorClientTransientException || ex is MailboxServerLocatorException || ex is AmServerTransientException || ex is AmServerException)
				{
					PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorFailedCalls.Increment();
					PerfCounters.UpdateMovingPercentagePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingPercentageMailboxServerLocatorFailedCalls);
				}
				else if (!(ex is DatabaseNotFoundException) && !(ex is ADTransientException) && !(ex is DataValidationException) && !(ex is DataSourceOperationException) && !(ex is DagDecomException))
				{
					throw;
				}
			}
			if (dictionary != null)
			{
				this.PopulateCache(dictionary, database.ResourceForest);
			}
		}

		// Token: 0x06000247 RID: 583 RVA: 0x0000B750 File Offset: 0x00009950
		private void PopulateCache(Dictionary<Guid, DatabaseToServerMappingInfo> databaseToServerMappingInfo, string resourceForest)
		{
			if (databaseToServerMappingInfo != null)
			{
				foreach (KeyValuePair<Guid, DatabaseToServerMappingInfo> keyValuePair in databaseToServerMappingInfo)
				{
					MailboxServerCacheEntry entry = new MailboxServerCacheEntry(new BackEndServer(keyValuePair.Value.ServerFqdn, keyValuePair.Value.ServerVersion), resourceForest, keyValuePair.Value.DatabaseFailoverSequenceNumber);
					this.Add(keyValuePair.Key, entry, null);
				}
			}
		}

		// Token: 0x06000248 RID: 584 RVA: 0x0000B7DC File Offset: 0x000099DC
		private bool IsDueForRefresh(Guid database)
		{
			MailboxServerCacheEntry mailboxServerCacheEntry;
			return !this.TryGetFromInMemoryCache(database.ToString(), out mailboxServerCacheEntry) || mailboxServerCacheEntry.IsDueForRefresh(MailboxServerCache.GetRefreshInterval(mailboxServerCacheEntry.BackEndServer));
		}

		// Token: 0x06000249 RID: 585 RVA: 0x0000B813 File Offset: 0x00009A13
		private bool TryAddToInMemoryCache(string key, MailboxServerCacheEntry entry)
		{
			return MailboxServerCache.InMemoryCacheEnabled.Value && this.inMemoryCache.TryAddOrUpdate(key, entry);
		}

		// Token: 0x0600024A RID: 586 RVA: 0x0000B830 File Offset: 0x00009A30
		private bool TryAddToSharedCache(string key, MailboxServerCacheEntry entry, out SharedCacheDiagnostics sharedCacheDiagnostics)
		{
			sharedCacheDiagnostics = null;
			return HttpProxySettings.MailboxServerLocatorSharedCacheEnabled.Value && this.sharedCacheClient.TryInsert(key, entry, entry.FailoverSequenceNumber, ref sharedCacheDiagnostics);
		}

		// Token: 0x0600024B RID: 587 RVA: 0x0000B858 File Offset: 0x00009A58
		private bool TryRemoveFromInMemoryCache(string key)
		{
			MailboxServerCacheEntry mailboxServerCacheEntry;
			return MailboxServerCache.InMemoryCacheEnabled.Value && this.inMemoryCache.TryRemove(key, ref mailboxServerCacheEntry);
		}

		// Token: 0x0600024C RID: 588 RVA: 0x0000B881 File Offset: 0x00009A81
		private bool TryRemoveFromSharedCache(string key, out SharedCacheDiagnostics sharedCacheDiagnostics)
		{
			sharedCacheDiagnostics = null;
			return HttpProxySettings.MailboxServerLocatorSharedCacheEnabled.Value && this.sharedCacheClient.TryRemove(key, ref sharedCacheDiagnostics);
		}

		// Token: 0x0600024D RID: 589 RVA: 0x0000B8A1 File Offset: 0x00009AA1
		private bool TryGetFromInMemoryCache(string key, out MailboxServerCacheEntry entry)
		{
			entry = null;
			return MailboxServerCache.InMemoryCacheEnabled.Value && this.inMemoryCache.TryGet(key, ref entry);
		}

		// Token: 0x0600024E RID: 590 RVA: 0x0000B8C1 File Offset: 0x00009AC1
		private bool TryGetFromSharedCache(string key, out MailboxServerCacheEntry entry, out SharedCacheDiagnostics sharedCacheDiagnostics)
		{
			entry = null;
			sharedCacheDiagnostics = null;
			return HttpProxySettings.MailboxServerLocatorSharedCacheEnabled.Value && this.sharedCacheClient.TryGet<MailboxServerCacheEntry>(key, ref entry, ref sharedCacheDiagnostics);
		}

		// Token: 0x04000138 RID: 312
		private static readonly TimeSpanAppSettingsEntry MailboxServerCacheAbsoluteTimeoutInMemoryCache = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("MailboxServerCache.InMemoryOnly.AbsoluteTimeout"), 1, TimeSpan.FromMinutes((double)MailboxServerCache.GetMailboxServerCacheInMemoryTimeoutValue()), ExTraceGlobals.VerboseTracer);

		// Token: 0x04000139 RID: 313
		private static readonly TimeSpanAppSettingsEntry MailboxServerCacheAbsoluteTimeoutWithSharedCache = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("MailboxServerCache.WithSharedCache.AbsoluteTimeout"), 1, TimeSpan.FromMinutes(3.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x0400013A RID: 314
		private static readonly TimeSpanAppSettingsEntry MailboxServerCacheRefreshInterval = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("MailboxServerCache.RefreshInterval"), 1, TimeSpan.FromMinutes(30.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x0400013B RID: 315
		private static readonly TimeSpanAppSettingsEntry MailboxServerCacheDownLevelServerRefreshInterval = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("MailboxServerCache.DownLevelServerRefreshInterval"), 1, TimeSpan.FromMinutes(10.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x0400013C RID: 316
		private static readonly IntAppSettingsEntry MailboxServerCacheMaxSize = new IntAppSettingsEntry(HttpProxySettings.Prefix("MailboxServerCache.MaxSize"), 200000, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400013D RID: 317
		private static readonly IntAppSettingsEntry NumCachePartitions = new IntAppSettingsEntry(HttpProxySettings.Prefix("MailboxServerCache.NumPartitions"), 64, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400013E RID: 318
		private static readonly TimeSpanAppSettingsEntry CacheExpiryInterval = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("MailboxServerCache.ExpiryInterval"), 0, TimeSpan.FromSeconds(5.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x0400013F RID: 319
		private static readonly BoolAppSettingsEntry InMemoryCacheEnabled = new BoolAppSettingsEntry(HttpProxySettings.Prefix("MailboxServerCache.InMemoryCacheEnabled"), CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MailboxServerCacheInMemoryCache.Enabled, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000140 RID: 320
		private static readonly BoolAppSettingsEntry InternalRefreshEnabled = new BoolAppSettingsEntry(HttpProxySettings.Prefix("MailboxServerCache.InternalRefreshEnabled"), CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MailboxServerCacheInternalRefresh.Enabled, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000141 RID: 321
		private static MailboxServerCache instance = null;

		// Token: 0x04000142 RID: 322
		private static object staticLock = new object();

		// Token: 0x04000143 RID: 323
		private static AutoResetEvent refreshWorkerSignal = new AutoResetEvent(false);

		// Token: 0x04000144 RID: 324
		private PartitionCache<string, MailboxServerCacheEntry> inMemoryCache;

		// Token: 0x04000145 RID: 325
		private ConcurrentQueue<DatabaseWithForest> refreshQueue = new ConcurrentQueue<DatabaseWithForest>();

		// Token: 0x04000146 RID: 326
		private SharedCacheClient sharedCacheClient;
	}
}
