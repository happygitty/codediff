using System;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200001E RID: 30
	internal class DatabaseGuidAnchorMailbox : DatabaseBasedAnchorMailbox
	{
		// Token: 0x060000FD RID: 253 RVA: 0x00006313 File Offset: 0x00004513
		public DatabaseGuidAnchorMailbox(Guid databaseGuid, IRequestContext requestContext) : base(AnchorSource.DatabaseGuid, databaseGuid, requestContext)
		{
			base.NotFoundExceptionCreator = (() => new DatabaseNotFoundException(this.DatabaseGuid.ToString()));
		}

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060000FE RID: 254 RVA: 0x00006335 File Offset: 0x00004535
		public Guid DatabaseGuid
		{
			get
			{
				return (Guid)base.SourceObject;
			}
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00006342 File Offset: 0x00004542
		protected override AnchorMailboxCacheEntry RefreshCacheEntry()
		{
			return new AnchorMailboxCacheEntry
			{
				Database = new ADObjectId(Guid.Empty, (Guid)base.SourceObject)
			};
		}
	}
}
