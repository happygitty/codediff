using System;
using System.Net;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.HttpProxy.Routing;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000023 RID: 35
	internal class MailboxGuidAnchorMailbox : ArchiveSupportedAnchorMailbox
	{
		// Token: 0x0600011F RID: 287 RVA: 0x00006ACE File Offset: 0x00004CCE
		public MailboxGuidAnchorMailbox(Guid mailboxGuid, string domain, IRequestContext requestContext) : base(AnchorSource.MailboxGuid, mailboxGuid, requestContext)
		{
			this.Domain = domain;
			base.NotFoundExceptionCreator = delegate()
			{
				base.UpdateNegativeCache(new NegativeAnchorMailboxCacheEntry
				{
					ErrorCode = HttpStatusCode.NotFound,
					SubErrorCode = 3003,
					SourceObject = this.ToCacheKey()
				});
				string message = string.Format("Cannot find mailbox {0} with domain {1}.", this.MailboxGuid, this.Domain);
				return new HttpProxyException(HttpStatusCode.NotFound, 3003, message);
			};
		}

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000120 RID: 288 RVA: 0x00006335 File Offset: 0x00004535
		public Guid MailboxGuid
		{
			get
			{
				return (Guid)base.SourceObject;
			}
		}

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x06000121 RID: 289 RVA: 0x00006AF7 File Offset: 0x00004CF7
		// (set) Token: 0x06000122 RID: 290 RVA: 0x00006AFF File Offset: 0x00004CFF
		public string Domain { get; private set; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x06000123 RID: 291 RVA: 0x00006B08 File Offset: 0x00004D08
		// (set) Token: 0x06000124 RID: 292 RVA: 0x00006B10 File Offset: 0x00004D10
		public string FallbackSmtp { get; set; }

		// Token: 0x06000125 RID: 293 RVA: 0x00006B1C File Offset: 0x00004D1C
		public override string GetOrganizationNameForLogging()
		{
			string organizationNameForLogging = base.GetOrganizationNameForLogging();
			if (string.IsNullOrEmpty(organizationNameForLogging) && !string.IsNullOrEmpty(this.Domain))
			{
				return this.Domain;
			}
			return organizationNameForLogging;
		}

		// Token: 0x06000126 RID: 294 RVA: 0x00006B4D File Offset: 0x00004D4D
		public override ITenantContext GetTenantContext()
		{
			return new DomainTenantContext(this.Domain);
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00006B5C File Offset: 0x00004D5C
		protected override ADRawEntry LoadADRawEntry()
		{
			IRecipientSession session = null;
			if (!string.IsNullOrEmpty(this.Domain) && SmtpAddress.IsValidDomain(this.Domain))
			{
				try
				{
					session = DirectoryHelper.GetRecipientSessionFromMailboxGuidAndDomain(this.MailboxGuid, this.Domain, base.RequestContext.Logger, base.RequestContext.LatencyTracker);
					goto IL_98;
				}
				catch (CannotResolveTenantNameException)
				{
					base.UpdateNegativeCache(new NegativeAnchorMailboxCacheEntry
					{
						ErrorCode = HttpStatusCode.NotFound,
						SubErrorCode = 3009,
						SourceObject = this.ToCacheKey()
					});
					throw;
				}
			}
			session = DirectoryHelper.GetRootOrgRecipientSession();
			IL_98:
			ADRawEntry adrawEntry;
			if (base.IsArchive != null)
			{
				adrawEntry = DirectoryHelper.InvokeAccountForest<ADRawEntry>(base.RequestContext.LatencyTracker, () => session.FindByExchangeGuidIncludingAlternate(this.MailboxGuid, this.PropertySet, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\MailboxGuidAnchorMailbox.cs", 170, "LoadADRawEntry"), base.RequestContext.Logger, session);
			}
			else
			{
				adrawEntry = DirectoryHelper.InvokeAccountForest<ADRawEntry>(base.RequestContext.LatencyTracker, () => session.FindByExchangeGuidIncludingAlternate(this.MailboxGuid, MailboxGuidAnchorMailbox.ADRawEntryWithArchivePropertySet, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\MailboxGuidAnchorMailbox.cs", 184, "LoadADRawEntry"), base.RequestContext.Logger, session);
				if (adrawEntry != null)
				{
					Guid guid = (Guid)adrawEntry[ADMailboxRecipientSchema.ArchiveGuid];
					Guid guid2 = (Guid)adrawEntry[ADMailboxRecipientSchema.ExchangeGuid];
					if (guid.Equals(this.MailboxGuid))
					{
						base.IsArchive = new bool?(true);
					}
					else if (!guid2.Equals(this.MailboxGuid))
					{
						adrawEntry = DirectoryHelper.InvokeAccountForest<ADRawEntry>(base.RequestContext.LatencyTracker, () => session.FindByExchangeGuidIncludingAlternate(this.MailboxGuid, MailboxGuidAnchorMailbox.ADRawEntryWithMailboxLocationsPropertySet, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\MailboxGuidAnchorMailbox.cs", 208, "LoadADRawEntry"), base.RequestContext.Logger, session);
						this.isLocationsMailbox = true;
					}
				}
			}
			if (adrawEntry == null && !string.IsNullOrEmpty(this.FallbackSmtp) && SmtpAddress.IsValidSmtpAddress(this.FallbackSmtp))
			{
				adrawEntry = new SmtpAnchorMailbox(this.FallbackSmtp, base.RequestContext)
				{
					IsArchive = base.IsArchive,
					NotFoundExceptionCreator = null
				}.GetADRawEntry();
			}
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(adrawEntry);
		}

		// Token: 0x06000128 RID: 296 RVA: 0x00006D5C File Offset: 0x00004F5C
		protected override string ToCacheKey()
		{
			return this.ToString();
		}

		// Token: 0x06000129 RID: 297 RVA: 0x00006D64 File Offset: 0x00004F64
		protected override IRoutingKey GetRoutingKey()
		{
			return new MailboxGuidRoutingKey(this.MailboxGuid, this.Domain);
		}

		// Token: 0x0600012A RID: 298 RVA: 0x00006D78 File Offset: 0x00004F78
		protected override ADObjectId GetDatabaseFromADRawEntry(ADRawEntry entry)
		{
			if (this.isLocationsMailbox)
			{
				IMailboxLocationInfo mailboxLocation = new MailboxLocationCollection(entry).GetMailboxLocation(this.MailboxGuid);
				base.CheckForNullAndThrowIfApplicable<IMailboxLocationInfo>(mailboxLocation);
				return mailboxLocation.DatabaseLocation;
			}
			return base.GetDatabaseFromADRawEntry(entry);
		}

		// Token: 0x040000E1 RID: 225
		protected static readonly ADPropertyDefinition[] ADRawEntryWithArchivePropertySet = new ADPropertyDefinition[]
		{
			ADObjectSchema.OrganizationId,
			ADMailboxRecipientSchema.ArchiveGuid,
			ADMailboxRecipientSchema.ExchangeGuid,
			ADMailboxRecipientSchema.Database,
			ADMailboxRecipientSchema.ArchiveDatabase,
			ADRecipientSchema.PrimarySmtpAddress
		};

		// Token: 0x040000E2 RID: 226
		protected static readonly ADPropertyDefinition[] ADRawEntryWithMailboxLocationsPropertySet = new ADPropertyDefinition[]
		{
			ADObjectSchema.OrganizationId,
			ADRecipientSchema.MailboxLocations,
			ADRecipientSchema.PrimarySmtpAddress
		};

		// Token: 0x040000E3 RID: 227
		private bool isLocationsMailbox;
	}
}
