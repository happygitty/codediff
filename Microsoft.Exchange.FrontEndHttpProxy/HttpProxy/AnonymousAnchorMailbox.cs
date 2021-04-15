using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000019 RID: 25
	internal class AnonymousAnchorMailbox : DatabaseBasedAnchorMailbox
	{
		// Token: 0x060000DF RID: 223 RVA: 0x0000551F File Offset: 0x0000371F
		public AnonymousAnchorMailbox(IRequestContext requestContext) : base(AnchorSource.Anonymous, AnonymousAnchorMailbox.AnonymousIdentifier, requestContext)
		{
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000E0 RID: 224 RVA: 0x00003165 File Offset: 0x00001365
		public override bool IsOrganizationMailboxDatabase
		{
			get
			{
				return false;
			}
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000552F File Offset: 0x0000372F
		public override BackEndServer TryDirectBackEndCalculation()
		{
			if (!HttpProxySettings.LocalForestDatabaseEnabled.Value || LocalForestDatabaseProvider.Instance.GetRandomDatabase() == null)
			{
				return MailboxServerCache.Instance.GetRandomE15Server(base.RequestContext);
			}
			return null;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000500A File Offset: 0x0000320A
		public override BackEndCookieEntryBase BuildCookieEntryForTarget(BackEndServer routingTarget, bool proxyToDownLevel, bool useResourceForest, bool organizationAware)
		{
			return null;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x0000555C File Offset: 0x0000375C
		public override ADObjectId GetDatabase()
		{
			if (HttpProxySettings.LocalForestDatabaseEnabled.Value && this.database == null)
			{
				this.database = LocalForestDatabaseProvider.Instance.GetRandomDatabase();
				base.RequestContext.Logger.AppendGenericInfo("Anonymous-RandomDB", (this.database == null) ? "<null>" : this.database.ObjectGuid.ToString());
			}
			return this.database;
		}

		// Token: 0x040000D7 RID: 215
		internal static readonly string AnonymousIdentifier = "Anonymous";

		// Token: 0x040000D8 RID: 216
		private ADObjectId database;
	}
}
