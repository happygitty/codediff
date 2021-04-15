using System;
using System.Net;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Routing;
using Microsoft.Exchange.HttpProxy.Routing.RoutingDestinations;
using Microsoft.Exchange.HttpProxy.Routing.RoutingEntries;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Global;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000031 RID: 49
	internal abstract class UserBasedAnchorMailbox : DatabaseBasedAnchorMailbox
	{
		// Token: 0x06000185 RID: 389 RVA: 0x00008030 File Offset: 0x00006230
		protected UserBasedAnchorMailbox(AnchorSource anchorSource, object sourceObject, IRequestContext requestContext) : base(anchorSource, sourceObject, requestContext)
		{
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000186 RID: 390 RVA: 0x0000803B File Offset: 0x0000623B
		// (set) Token: 0x06000187 RID: 391 RVA: 0x00008043 File Offset: 0x00006243
		public Func<ADRawEntry, ADObjectId> MissingDatabaseHandler { get; set; }

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000188 RID: 392 RVA: 0x0000804C File Offset: 0x0000624C
		// (set) Token: 0x06000189 RID: 393 RVA: 0x00008054 File Offset: 0x00006254
		public string CacheKeyPostfix { get; set; }

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x0600018A RID: 394 RVA: 0x0000805D File Offset: 0x0000625D
		protected virtual ADPropertyDefinition[] PropertySet
		{
			get
			{
				return UserBasedAnchorMailbox.ADRawEntryPropertySet;
			}
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x0600018B RID: 395 RVA: 0x00008064 File Offset: 0x00006264
		protected virtual ADPropertyDefinition DatabaseProperty
		{
			get
			{
				return ADMailboxRecipientSchema.Database;
			}
		}

		// Token: 0x0600018C RID: 396 RVA: 0x0000806C File Offset: 0x0000626C
		public ADRawEntry GetADRawEntry()
		{
			if (!this.activeDirectoryRawEntryLoaded)
			{
				this.loadedADRawEntry = this.LoadADRawEntry();
				if (this.loadedADRawEntry == null)
				{
					base.RequestContext.Logger.AppendString(3, "-NoUser");
				}
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<ADRawEntry, UserBasedAnchorMailbox>((long)this.GetHashCode(), "[UserBasedAnchorMailbox::GetADRawEntry]: LoadADRawEntry() resturns {0} for anchor mailbox {1}.", this.loadedADRawEntry, this);
				}
				this.activeDirectoryRawEntryLoaded = true;
			}
			return this.loadedADRawEntry;
		}

		// Token: 0x0600018D RID: 397 RVA: 0x000080E7 File Offset: 0x000062E7
		public string GetDomainName()
		{
			return base.GetCacheEntry().DomainName;
		}

		// Token: 0x0600018E RID: 398 RVA: 0x000080F4 File Offset: 0x000062F4
		public override string GetOrganizationNameForLogging()
		{
			if (this.activeDirectoryRawEntryLoaded && this.GetADRawEntry() != null)
			{
				return ((OrganizationId)this.GetADRawEntry()[ADObjectSchema.OrganizationId]).GetFriendlyName();
			}
			return base.GetOrganizationNameForLogging();
		}

		// Token: 0x0600018F RID: 399 RVA: 0x00008128 File Offset: 0x00006328
		public override BackEndCookieEntryBase BuildCookieEntryForTarget(BackEndServer routingTarget, bool proxyToDownLevel, bool useResourceForest, bool organizationAware)
		{
			if (routingTarget == null)
			{
				throw new ArgumentNullException("routingTarget");
			}
			if (!proxyToDownLevel && !base.UseServerCookie)
			{
				ADObjectId database = this.GetDatabase();
				if (database != null)
				{
					if (organizationAware)
					{
						return new BackEndDatabaseOrganizationAwareCookieEntry(database.ObjectGuid, this.GetDomainName(), database.PartitionFQDN, this.IsOrganizationMailboxDatabase);
					}
					if (useResourceForest)
					{
						return new BackEndDatabaseResourceForestCookieEntry(database.ObjectGuid, this.GetDomainName(), database.PartitionFQDN);
					}
					return new BackEndDatabaseCookieEntry(database.ObjectGuid, this.GetDomainName());
				}
			}
			return base.BuildCookieEntryForTarget(routingTarget, proxyToDownLevel, useResourceForest, organizationAware);
		}

		// Token: 0x06000190 RID: 400 RVA: 0x000081B4 File Offset: 0x000063B4
		public override IRoutingEntry GetRoutingEntry()
		{
			IRoutingKey routingKey = this.GetRoutingKey();
			DatabaseGuidRoutingDestination databaseGuidRoutingDestination = this.GetRoutingDestination() as DatabaseGuidRoutingDestination;
			if (routingKey != null && databaseGuidRoutingDestination != null)
			{
				return new SuccessfulMailboxRoutingEntry(routingKey, databaseGuidRoutingDestination, 0L);
			}
			return base.GetRoutingEntry();
		}

		// Token: 0x06000191 RID: 401
		protected abstract ADRawEntry LoadADRawEntry();

		// Token: 0x06000192 RID: 402 RVA: 0x000081F0 File Offset: 0x000063F0
		protected override AnchorMailboxCacheEntry RefreshCacheEntry()
		{
			ADRawEntry adrawEntry = this.GetADRawEntry();
			if (adrawEntry == null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<UserBasedAnchorMailbox>((long)this.GetHashCode(), "[UserBasedAnchorMailbox::RefreshCacheEntry]: Anchor mailbox {0} has no AD object. Will use random server.", this);
				}
				return new AnchorMailboxCacheEntry();
			}
			string domainNameFromADRawEntry = UserBasedAnchorMailbox.GetDomainNameFromADRawEntry(adrawEntry);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<UserBasedAnchorMailbox, string>((long)this.GetHashCode(), "[UserBasedAnchorMailbox::RefreshCacheEntry]: The domain name of anchor mailbox {0} is {1}.", this, domainNameFromADRawEntry);
			}
			ADObjectId adobjectId = this.GetDatabaseFromADRawEntry(adrawEntry);
			if (adobjectId == null && this.MissingDatabaseHandler != null)
			{
				adobjectId = this.MissingDatabaseHandler(adrawEntry);
			}
			bool isOrganizationMailboxDatabase = false;
			if (adobjectId == null)
			{
				base.RequestContext.Logger.AppendString(3, "-NoDatabase");
				OrganizationId organizationId = (OrganizationId)adrawEntry[ADObjectSchema.OrganizationId];
				ADUser defaultOrganizationMailbox = HttpProxyBackEndHelper.GetDefaultOrganizationMailbox(organizationId, ((ADObjectId)adrawEntry[ADObjectSchema.Id]).ToString());
				if (defaultOrganizationMailbox == null || defaultOrganizationMailbox.Database == null)
				{
					if (Utilities.IsPartnerHostedOnly || GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<ADObjectId>((long)this.GetHashCode(), "[UserBasedAnchorMailbox::RefreshCacheEntry]: Cannot find organization mailbox for user {0}. Will use random server.", adrawEntry.Id);
						}
						return new AnchorMailboxCacheEntry
						{
							DomainName = domainNameFromADRawEntry
						};
					}
					string text = string.Format("Unable to find organization mailbox for organization {0}", organizationId);
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[UserBasedAnchorMailbox::RefreshCacheEntry]: {0}", text);
					}
					throw new HttpProxyException(HttpStatusCode.InternalServerError, 3006, text);
				}
				else
				{
					adobjectId = defaultOrganizationMailbox.Database;
					isOrganizationMailboxDatabase = true;
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<ADObjectId, ObjectId, ADObjectId>((long)this.GetHashCode(), "[UserBasedAnchorMailbox::RefreshCacheEntry]: Anchor mailbox user {0} has no mailbox. Will use organization mailbox {1} with database {2}", adrawEntry.Id, defaultOrganizationMailbox.Identity, adobjectId);
					}
				}
			}
			return new AnchorMailboxCacheEntry
			{
				Database = adobjectId,
				DomainName = domainNameFromADRawEntry,
				IsOrganizationMailboxDatabase = isOrganizationMailboxDatabase
			};
		}

		// Token: 0x06000193 RID: 403 RVA: 0x000083D0 File Offset: 0x000065D0
		protected override AnchorMailboxCacheEntry LoadCacheEntryFromIncomingCookie()
		{
			BackEndDatabaseCookieEntry backEndDatabaseCookieEntry = base.IncomingCookieEntry as BackEndDatabaseCookieEntry;
			if (backEndDatabaseCookieEntry != null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<UserBasedAnchorMailbox, BackEndDatabaseCookieEntry>((long)this.GetHashCode(), "[UserBasedAnchorMailbox::LoadCacheEntryFromIncomingCookie]: Anchor mailbox {0} using cookie entry {1} as cache entry.", this, backEndDatabaseCookieEntry);
				}
				BackEndDatabaseResourceForestCookieEntry backEndDatabaseResourceForestCookieEntry = base.IncomingCookieEntry as BackEndDatabaseResourceForestCookieEntry;
				BackEndDatabaseOrganizationAwareCookieEntry backEndDatabaseOrganizationAwareCookieEntry = base.IncomingCookieEntry as BackEndDatabaseOrganizationAwareCookieEntry;
				return new AnchorMailboxCacheEntry
				{
					Database = new ADObjectId(backEndDatabaseCookieEntry.Database, (backEndDatabaseResourceForestCookieEntry == null) ? null : backEndDatabaseResourceForestCookieEntry.ResourceForest),
					DomainName = backEndDatabaseCookieEntry.Domain,
					IsOrganizationMailboxDatabase = (backEndDatabaseOrganizationAwareCookieEntry != null && backEndDatabaseOrganizationAwareCookieEntry.IsOrganizationMailboxDatabase)
				};
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<UserBasedAnchorMailbox>((long)this.GetHashCode(), "[UserBasedAnchorMailbox::LoadCacheEntryFromCookie]: Anchor mailbox {0} had no BackEndDatabaseCookie.", this);
			}
			return null;
		}

		// Token: 0x06000194 RID: 404 RVA: 0x0000848F File Offset: 0x0000668F
		protected override string ToCacheKey()
		{
			if (!string.IsNullOrEmpty(this.CacheKeyPostfix))
			{
				return base.ToCacheKey() + this.CacheKeyPostfix;
			}
			return base.ToCacheKey();
		}

		// Token: 0x06000195 RID: 405 RVA: 0x0000500A File Offset: 0x0000320A
		protected virtual IRoutingKey GetRoutingKey()
		{
			return null;
		}

		// Token: 0x06000196 RID: 406 RVA: 0x000084B6 File Offset: 0x000066B6
		protected virtual ADObjectId GetDatabaseFromADRawEntry(ADRawEntry entry)
		{
			return (ADObjectId)entry[this.DatabaseProperty];
		}

		// Token: 0x06000197 RID: 407 RVA: 0x000084CC File Offset: 0x000066CC
		private static string GetDomainNameFromADRawEntry(ADRawEntry activeDirectoryRawEntry)
		{
			OrganizationId organizationId = (OrganizationId)activeDirectoryRawEntry[ADObjectSchema.OrganizationId];
			if (organizationId == null || organizationId.Equals(OrganizationId.ForestWideOrgId))
			{
				return null;
			}
			SmtpAddress smtpAddress = (SmtpAddress)activeDirectoryRawEntry[ADRecipientSchema.PrimarySmtpAddress];
			if (!string.IsNullOrEmpty(smtpAddress.Domain))
			{
				return smtpAddress.Domain;
			}
			SmtpAddress smtpAddress2 = (SmtpAddress)activeDirectoryRawEntry[ADRecipientSchema.WindowsLiveID];
			if (!string.IsNullOrEmpty(smtpAddress2.Domain))
			{
				return smtpAddress2.Domain;
			}
			return organizationId.ConfigurationUnit.Parent.Name;
		}

		// Token: 0x06000198 RID: 408 RVA: 0x00008560 File Offset: 0x00006760
		private IRoutingDestination GetRoutingDestination()
		{
			string domainName = this.GetDomainName();
			if (!string.IsNullOrEmpty(domainName))
			{
				ADObjectId database = this.GetDatabase();
				if (database != null)
				{
					return new DatabaseGuidRoutingDestination(database.ObjectGuid, domainName, database.PartitionFQDN, this.IsOrganizationMailboxDatabase);
				}
			}
			return null;
		}

		// Token: 0x04000103 RID: 259
		public static readonly ADPropertyDefinition[] ADRawEntryPropertySet = new ADPropertyDefinition[]
		{
			ADObjectSchema.ExchangeVersion,
			ADObjectSchema.OrganizationId,
			ADMailboxRecipientSchema.ExchangeGuid,
			ADMailboxRecipientSchema.Database,
			ADMailboxRecipientSchema.Sid,
			ADRecipientSchema.PrimarySmtpAddress,
			ADRecipientSchema.ExternalEmailAddress
		};

		// Token: 0x04000104 RID: 260
		private ADRawEntry loadedADRawEntry;

		// Token: 0x04000105 RID: 261
		private bool activeDirectoryRawEntryLoaded;
	}
}
