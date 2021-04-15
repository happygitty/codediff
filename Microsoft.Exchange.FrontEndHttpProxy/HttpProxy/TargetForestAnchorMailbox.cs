using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web;
using Microsoft.Exchange.Collections.TimeoutCache;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200002E RID: 46
	internal class TargetForestAnchorMailbox : DatabaseBasedAnchorMailbox
	{
		// Token: 0x06000173 RID: 371 RVA: 0x00007A82 File Offset: 0x00005C82
		public TargetForestAnchorMailbox(IRequestContext requestContext, string domain, bool supportCookieBasedAffinity) : base(AnchorSource.Domain, domain, requestContext)
		{
			this.supportCookieBasedAffinity = supportCookieBasedAffinity;
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000174 RID: 372 RVA: 0x00003165 File Offset: 0x00001365
		public override bool IsOrganizationMailboxDatabase
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00007A94 File Offset: 0x00005C94
		public override ADObjectId GetDatabase()
		{
			if (!TargetForestAnchorMailbox.RandomBackEndInSameForestEnabled.Value && this.database == null)
			{
				string domain = (string)base.SourceObject;
				this.database = this.GetRandomDatabaseFromDomain(domain);
				base.RequestContext.Logger.AppendGenericInfo("TargetForest-RandomDB", (this.database == null) ? "<null>" : this.database.ObjectGuid.ToString());
			}
			return this.database;
		}

		// Token: 0x06000176 RID: 374 RVA: 0x00007B11 File Offset: 0x00005D11
		public override BackEndServer TryDirectBackEndCalculation()
		{
			if (TargetForestAnchorMailbox.RandomBackEndInSameForestEnabled.Value)
			{
				base.RequestContext.Logger.AppendString(3, "-SameForestRandomBackend");
				return HttpProxyBackEndHelper.GetAnyBackEndServer();
			}
			return base.TryDirectBackEndCalculation();
		}

		// Token: 0x06000177 RID: 375 RVA: 0x00007B46 File Offset: 0x00005D46
		public override BackEndCookieEntryBase BuildCookieEntryForTarget(BackEndServer routingTarget, bool proxyToDownLevel, bool useResourceForest, bool organizationAware)
		{
			if (this.supportCookieBasedAffinity)
			{
				return base.BuildCookieEntryForTarget(routingTarget, proxyToDownLevel, useResourceForest, organizationAware);
			}
			return null;
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00007B5D File Offset: 0x00005D5D
		public override BackEndServer AcceptBackEndCookie(HttpCookie backEndCookie)
		{
			if (this.supportCookieBasedAffinity)
			{
				return base.AcceptBackEndCookie(backEndCookie);
			}
			return null;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x00007B70 File Offset: 0x00005D70
		protected override AnchorMailboxCacheEntry LoadCacheEntryFromIncomingCookie()
		{
			if (this.supportCookieBasedAffinity)
			{
				return base.LoadCacheEntryFromIncomingCookie();
			}
			return null;
		}

		// Token: 0x0600017A RID: 378 RVA: 0x00007B84 File Offset: 0x00005D84
		private string GetResourceForestFqdnByAcceptedDomainName(string tenantAcceptedDomain)
		{
			string resourceForestFqdn;
			if (!TargetForestAnchorMailbox.domainToResourceForestMap.TryGetValue(tenantAcceptedDomain, ref resourceForestFqdn))
			{
				long latency = 0L;
				resourceForestFqdn = LatencyTracker.GetLatency<string>(delegate()
				{
					resourceForestFqdn = ADAccountPartitionLocator.GetResourceForestFqdnByAcceptedDomainName(tenantAcceptedDomain);
					return resourceForestFqdn;
				}, out latency);
				TargetForestAnchorMailbox.domainToResourceForestMap.TryInsertAbsolute(tenantAcceptedDomain, resourceForestFqdn, TargetForestAnchorMailbox.DomainForestAbsoluteTimeout.Value);
				base.RequestContext.LatencyTracker.HandleGlsLatency(latency);
			}
			return resourceForestFqdn;
		}

		// Token: 0x0600017B RID: 379 RVA: 0x00007C0C File Offset: 0x00005E0C
		private ADObjectId GetRandomDatabasesFromForest(string resourceForestFqdn)
		{
			if (HttpProxySettings.LocalForestDatabaseEnabled.Value && Utilities.IsLocalForest(resourceForestFqdn))
			{
				return LocalForestDatabaseProvider.Instance.GetRandomDatabase();
			}
			List<ADObjectId> list = null;
			if (!TargetForestAnchorMailbox.resourceForestToDatabaseMap.TryGetValue(resourceForestFqdn, out list) || list == null || list.Count <= 0)
			{
				object obj = TargetForestAnchorMailbox.forestDatabaseLock;
				lock (obj)
				{
					if (!TargetForestAnchorMailbox.resourceForestToDatabaseMap.TryGetValue(resourceForestFqdn, out list) || list == null || list.Count <= 0)
					{
						list = new List<ADObjectId>();
						PartitionId partitionId = new PartitionId(resourceForestFqdn);
						ITopologyConfigurationSession resourceForestSession = DirectorySessionFactory.Default.CreateTopologyConfigurationSession(2, ADSessionSettings.FromAccountPartitionRootOrgScopeSet(partitionId), 306, "GetRandomDatabasesFromForest", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\TargetForestAnchorMailbox.cs");
						SortBy sortBy = new SortBy(ADObjectSchema.WhenCreatedUTC, 0);
						List<PropertyDefinition> databaseSchema = new List<PropertyDefinition>
						{
							ADObjectSchema.Id
						};
						long latency = 0L;
						ADPagedReader<ADRawEntry> latency2 = LatencyTracker.GetLatency<ADPagedReader<ADRawEntry>>(() => resourceForestSession.FindPagedADRawEntry(resourceForestSession.ConfigurationNamingContext, 2, TargetForestAnchorMailbox.DatabaseQueryFilter, sortBy, TargetForestAnchorMailbox.DatabasesToLoadPerForest.Value, databaseSchema, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\TargetForestAnchorMailbox.cs", 323, "GetRandomDatabasesFromForest"), out latency);
						base.RequestContext.LatencyTracker.HandleResourceLatency(latency);
						if (latency2 != null)
						{
							foreach (ADRawEntry adrawEntry in latency2)
							{
								list.Add(adrawEntry.Id);
							}
						}
						if (list.Count > 0)
						{
							TargetForestAnchorMailbox.resourceForestToDatabaseMap[resourceForestFqdn] = list;
							if (list.Count < TargetForestAnchorMailbox.MinimumDatabasesForEffectiveLoadBalancing.Value)
							{
								base.RequestContext.Logger.AppendGenericError("TooFewDbsForLoadBalancing", string.Format("DbCount:{0}/Forest:{1}", list.Count, resourceForestFqdn));
							}
						}
					}
				}
			}
			if (list != null && list.Count > 0)
			{
				int index = TargetForestAnchorMailbox.seededRand.Next(0, list.Count);
				return list[index];
			}
			return null;
		}

		// Token: 0x0600017C RID: 380 RVA: 0x00007E14 File Offset: 0x00006014
		private ADObjectId GetRandomDatabaseFromDomain(string domain)
		{
			string resourceForestFqdnByAcceptedDomainName = this.GetResourceForestFqdnByAcceptedDomainName(domain);
			base.RequestContext.Logger.SafeSet(3, "TargetForest-RandomDatabase");
			return this.GetRandomDatabasesFromForest(resourceForestFqdnByAcceptedDomainName);
		}

		// Token: 0x040000F3 RID: 243
		private const string PrivateDatabaseObjectClass = "msExchPrivateMDB";

		// Token: 0x040000F4 RID: 244
		private const string PublicDatabaseObjectClass = "msExchPublicMDB";

		// Token: 0x040000F5 RID: 245
		private static readonly BoolAppSettingsEntry RandomBackEndInSameForestEnabled = new BoolAppSettingsEntry(HttpProxySettings.Prefix("RandomBackEndInSameForestEnabled"), true, ExTraceGlobals.VerboseTracer);

		// Token: 0x040000F6 RID: 246
		private static readonly TimeSpanAppSettingsEntry DomainForestAbsoluteTimeout = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("DomainForestAbsoluteTimeout"), 1, TimeSpan.FromMinutes(1440.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x040000F7 RID: 247
		private static readonly IntAppSettingsEntry MinimumDatabasesForEffectiveLoadBalancing = new IntAppSettingsEntry(HttpProxySettings.Prefix("MinimumDbsForEffectiveLoadBalancing"), 100, ExTraceGlobals.VerboseTracer);

		// Token: 0x040000F8 RID: 248
		private static readonly IntAppSettingsEntry DatabasesToLoadPerForest = new IntAppSettingsEntry(HttpProxySettings.Prefix("DatabasesToLoadPerForest"), 1000, ExTraceGlobals.VerboseTracer);

		// Token: 0x040000F9 RID: 249
		private static readonly IntAppSettingsEntry DomainToForestMapSize = new IntAppSettingsEntry(HttpProxySettings.Prefix("DomainToForestMapSize"), 100, ExTraceGlobals.VerboseTracer);

		// Token: 0x040000FA RID: 250
		private static readonly DateTime MaximumE14DatabaseCreationDate = new DateTime(2013, 6, 1);

		// Token: 0x040000FB RID: 251
		private static readonly QueryFilter DatabaseQueryFilter = new AndFilter(new QueryFilter[]
		{
			new ComparisonFilter(4, ADObjectSchema.WhenCreatedUTC, TargetForestAnchorMailbox.MaximumE14DatabaseCreationDate),
			new OrFilter(new QueryFilter[]
			{
				new ComparisonFilter(0, ADObjectSchema.ObjectClass, "msExchPrivateMDB"),
				new ComparisonFilter(0, ADObjectSchema.ObjectClass, "msExchPublicMDB")
			})
		});

		// Token: 0x040000FC RID: 252
		private static ExactTimeoutCache<string, string> domainToResourceForestMap = new ExactTimeoutCache<string, string>(null, null, null, TargetForestAnchorMailbox.DomainToForestMapSize.Value, false);

		// Token: 0x040000FD RID: 253
		private static object forestDatabaseLock = new object();

		// Token: 0x040000FE RID: 254
		private static ConcurrentDictionary<string, List<ADObjectId>> resourceForestToDatabaseMap = new ConcurrentDictionary<string, List<ADObjectId>>();

		// Token: 0x040000FF RID: 255
		private static Random seededRand = new Random(DateTime.Now.Millisecond);

		// Token: 0x04000100 RID: 256
		private bool supportCookieBasedAffinity;

		// Token: 0x04000101 RID: 257
		private ADObjectId database;
	}
}
