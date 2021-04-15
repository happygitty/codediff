using System;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200001B RID: 27
	internal abstract class ArchiveSupportedAnchorMailbox : UserBasedAnchorMailbox
	{
		// Token: 0x060000EC RID: 236 RVA: 0x00006058 File Offset: 0x00004258
		protected ArchiveSupportedAnchorMailbox(AnchorSource anchorSource, object sourceObject, IRequestContext requestContext) : base(anchorSource, sourceObject, requestContext)
		{
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000ED RID: 237 RVA: 0x00006063 File Offset: 0x00004263
		// (set) Token: 0x060000EE RID: 238 RVA: 0x0000606B File Offset: 0x0000426B
		public bool? IsArchive { get; set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060000EF RID: 239 RVA: 0x00006074 File Offset: 0x00004274
		protected override ADPropertyDefinition[] PropertySet
		{
			get
			{
				if (this.IsArchive != null && this.IsArchive.Value)
				{
					return ArchiveSupportedAnchorMailbox.ArchiveMailboxADRawEntryPropertySet;
				}
				return base.PropertySet;
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000F0 RID: 240 RVA: 0x000060B0 File Offset: 0x000042B0
		protected override ADPropertyDefinition DatabaseProperty
		{
			get
			{
				if (this.IsArchive != null && this.IsArchive.Value)
				{
					return ADMailboxRecipientSchema.ArchiveDatabase;
				}
				return base.DatabaseProperty;
			}
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x000060EC File Offset: 0x000042EC
		protected override string ToCacheKey()
		{
			string text = base.ToCacheKey();
			if (this.IsArchive != null && this.IsArchive.Value)
			{
				text += "_Archive";
			}
			return text;
		}

		// Token: 0x040000D9 RID: 217
		protected static readonly ADPropertyDefinition[] ArchiveMailboxADRawEntryPropertySet = new ADPropertyDefinition[]
		{
			ADObjectSchema.OrganizationId,
			ADMailboxRecipientSchema.ArchiveDatabase,
			ADRecipientSchema.PrimarySmtpAddress
		};
	}
}
