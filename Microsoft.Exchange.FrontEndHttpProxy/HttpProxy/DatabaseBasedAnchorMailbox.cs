using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200001D RID: 29
	internal abstract class DatabaseBasedAnchorMailbox : AnchorMailbox
	{
		// Token: 0x060000F7 RID: 247 RVA: 0x00006253 File Offset: 0x00004453
		public DatabaseBasedAnchorMailbox(AnchorSource anchorSource, object sourceObject, IRequestContext requestContext) : base(anchorSource, sourceObject, requestContext)
		{
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000F8 RID: 248 RVA: 0x0000625E File Offset: 0x0000445E
		// (set) Token: 0x060000F9 RID: 249 RVA: 0x00006266 File Offset: 0x00004466
		public bool UseServerCookie { get; set; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000FA RID: 250 RVA: 0x0000626F File Offset: 0x0000446F
		public virtual bool IsOrganizationMailboxDatabase
		{
			get
			{
				return base.GetCacheEntry().IsOrganizationMailboxDatabase;
			}
		}

		// Token: 0x060000FB RID: 251 RVA: 0x0000627C File Offset: 0x0000447C
		public virtual ADObjectId GetDatabase()
		{
			return base.GetCacheEntry().Database;
		}

		// Token: 0x060000FC RID: 252 RVA: 0x0000628C File Offset: 0x0000448C
		public override BackEndCookieEntryBase BuildCookieEntryForTarget(BackEndServer routingTarget, bool proxyToDownLevel, bool useResourceForest, bool organizationAware)
		{
			if (routingTarget == null)
			{
				throw new ArgumentNullException("routingTarget");
			}
			if (!proxyToDownLevel && !this.UseServerCookie)
			{
				ADObjectId database = this.GetDatabase();
				if (database != null)
				{
					if (organizationAware)
					{
						return new BackEndDatabaseOrganizationAwareCookieEntry(database.ObjectGuid, string.Empty, database.PartitionFQDN, this.IsOrganizationMailboxDatabase);
					}
					if (useResourceForest)
					{
						return new BackEndDatabaseResourceForestCookieEntry(database.ObjectGuid, string.Empty, database.PartitionFQDN);
					}
					return new BackEndDatabaseCookieEntry(database.ObjectGuid, string.Empty);
				}
			}
			return base.BuildCookieEntryForTarget(routingTarget, proxyToDownLevel, useResourceForest, organizationAware);
		}
	}
}
