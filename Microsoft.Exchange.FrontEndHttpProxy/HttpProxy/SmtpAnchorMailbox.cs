using System;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.HttpProxy.Routing;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200002C RID: 44
	internal class SmtpAnchorMailbox : ArchiveSupportedAnchorMailbox
	{
		// Token: 0x06000168 RID: 360 RVA: 0x0000784F File Offset: 0x00005A4F
		public SmtpAnchorMailbox(string smtp, IRequestContext requestContext) : base(AnchorSource.Smtp, smtp, requestContext)
		{
			this.FailOnDomainNotFound = true;
		}

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x06000169 RID: 361 RVA: 0x0000618F File Offset: 0x0000438F
		public string Smtp
		{
			get
			{
				return (string)base.SourceObject;
			}
		}

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x0600016A RID: 362 RVA: 0x00007861 File Offset: 0x00005A61
		// (set) Token: 0x0600016B RID: 363 RVA: 0x00007869 File Offset: 0x00005A69
		public bool FailOnDomainNotFound { get; set; }

		// Token: 0x0600016C RID: 364 RVA: 0x00007874 File Offset: 0x00005A74
		public override string GetOrganizationNameForLogging()
		{
			string organizationNameForLogging = base.GetOrganizationNameForLogging();
			if (!string.IsNullOrEmpty(organizationNameForLogging))
			{
				return organizationNameForLogging;
			}
			return this.GetOrganizationFromSmtp(this.Smtp);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x000078A0 File Offset: 0x00005AA0
		public override ITenantContext GetTenantContext()
		{
			string organizationFromSmtp = this.GetOrganizationFromSmtp(this.Smtp);
			if (!string.IsNullOrEmpty(organizationFromSmtp))
			{
				return new DomainTenantContext(organizationFromSmtp);
			}
			return new ExternalDirectoryOrganizationIdTenantContext(Guid.Empty);
		}

		// Token: 0x0600016E RID: 366 RVA: 0x000078D4 File Offset: 0x00005AD4
		protected override ADRawEntry LoadADRawEntry()
		{
			IRecipientSession session = DirectoryHelper.GetRecipientSessionFromSmtpOrLiveId(this.Smtp, base.RequestContext.Logger, base.RequestContext.LatencyTracker, !this.FailOnDomainNotFound);
			ADRawEntry ret = null;
			if (session != null)
			{
				ret = DirectoryHelper.InvokeAccountForest<ADRawEntry>(base.RequestContext.LatencyTracker, delegate()
				{
					if (FaultInjection.TraceTest<bool>(FaultInjection.LIDs.ShouldFailSmtpAnchorMailboxADLookup))
					{
						return null;
					}
					return session.FindByProxyAddress(new SmtpProxyAddress(this.Smtp, true), this.PropertySet, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\SmtpAnchorMailbox.cs", 107, "LoadADRawEntry");
				}, base.RequestContext.Logger, session);
			}
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(ret);
		}

		// Token: 0x0600016F RID: 367 RVA: 0x0000795D File Offset: 0x00005B5D
		protected override IRoutingKey GetRoutingKey()
		{
			return new SmtpRoutingKey(new SmtpAddress(this.Smtp));
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00007970 File Offset: 0x00005B70
		private string GetOrganizationFromSmtp(string smtp)
		{
			if (this.Smtp != null && SmtpAddress.IsValidSmtpAddress(this.Smtp))
			{
				return SmtpAddress.Parse(this.Smtp).Domain;
			}
			return string.Empty;
		}
	}
}
