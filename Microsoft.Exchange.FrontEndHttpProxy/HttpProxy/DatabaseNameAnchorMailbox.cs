using System;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200001F RID: 31
	internal class DatabaseNameAnchorMailbox : DatabaseBasedAnchorMailbox
	{
		// Token: 0x06000101 RID: 257 RVA: 0x0000638A File Offset: 0x0000458A
		public DatabaseNameAnchorMailbox(string databaseName, IRequestContext requestContext) : base(AnchorSource.DatabaseName, databaseName, requestContext)
		{
			base.NotFoundExceptionCreator = (() => new DatabaseNotFoundException(this.DatabaseName));
		}

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x06000102 RID: 258 RVA: 0x0000618F File Offset: 0x0000438F
		public string DatabaseName
		{
			get
			{
				return (string)base.SourceObject;
			}
		}

		// Token: 0x06000103 RID: 259 RVA: 0x000063A8 File Offset: 0x000045A8
		protected override AnchorMailboxCacheEntry RefreshCacheEntry()
		{
			IConfigurationSession session = DirectoryHelper.GetConfigurationSession();
			MailboxDatabase[] array = DirectoryHelper.InvokeResourceForest(base.RequestContext.LatencyTracker, () => session.Find<MailboxDatabase>(session.GetExchangeConfigurationContainer("d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\DatabaseNameAnchorMailbox.cs", 63, "RefreshCacheEntry").Id, 2, new ComparisonFilter(0, DatabaseSchema.Name, this.DatabaseName), null, 1, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\DatabaseNameAnchorMailbox.cs", 62, "RefreshCacheEntry"), base.RequestContext.Logger, session);
			if (array.Length == 0)
			{
				base.CheckForNullAndThrowIfApplicable<ADObjectId>(null);
				return new AnchorMailboxCacheEntry();
			}
			return new AnchorMailboxCacheEntry
			{
				Database = array[0].Id
			};
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00006420 File Offset: 0x00004620
		protected override AnchorMailboxCacheEntry LoadCacheEntryFromIncomingCookie()
		{
			BackEndDatabaseCookieEntry backEndDatabaseCookieEntry = base.IncomingCookieEntry as BackEndDatabaseCookieEntry;
			if (backEndDatabaseCookieEntry != null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<DatabaseNameAnchorMailbox, BackEndDatabaseCookieEntry>((long)this.GetHashCode(), "[DatabaseNameAnchorMailbox::LoadCacheEntryFromCookie]: Anchor mailbox {0} using cookie entry {1} as cache entry.", this, backEndDatabaseCookieEntry);
				}
				BackEndDatabaseResourceForestCookieEntry backEndDatabaseResourceForestCookieEntry = base.IncomingCookieEntry as BackEndDatabaseResourceForestCookieEntry;
				BackEndDatabaseOrganizationAwareCookieEntry backEndDatabaseOrganizationAwareCookieEntry = base.IncomingCookieEntry as BackEndDatabaseOrganizationAwareCookieEntry;
				return new AnchorMailboxCacheEntry
				{
					Database = new ADObjectId(backEndDatabaseCookieEntry.Database, (backEndDatabaseResourceForestCookieEntry == null) ? null : backEndDatabaseResourceForestCookieEntry.ResourceForest),
					IsOrganizationMailboxDatabase = (backEndDatabaseOrganizationAwareCookieEntry != null && backEndDatabaseOrganizationAwareCookieEntry.IsOrganizationMailboxDatabase)
				};
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<DatabaseNameAnchorMailbox>((long)this.GetHashCode(), "[DatabaseNameAnchorMailbox::LoadCacheEntryFromCookie]: Anchor mailbox {0} had no BackEndDatabaseCookie.", this);
			}
			return null;
		}
	}
}
