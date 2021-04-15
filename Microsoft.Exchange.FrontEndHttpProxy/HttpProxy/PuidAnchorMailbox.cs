using System;
using System.Net;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.HttpProxy.Routing;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000028 RID: 40
	internal class PuidAnchorMailbox : UserBasedAnchorMailbox
	{
		// Token: 0x0600013A RID: 314 RVA: 0x000070CB File Offset: 0x000052CB
		public PuidAnchorMailbox(string puid, IRequestContext requestContext) : this(puid, Constants.ConsumerDomain, requestContext)
		{
		}

		// Token: 0x0600013B RID: 315 RVA: 0x000070DA File Offset: 0x000052DA
		public PuidAnchorMailbox(string puid, string domainName, IRequestContext requestContext) : this(puid, domainName, requestContext, string.Empty)
		{
		}

		// Token: 0x0600013C RID: 316 RVA: 0x000070EA File Offset: 0x000052EA
		public PuidAnchorMailbox(string puid, IRequestContext requestContext, string fallbackSmtp) : this(puid, Constants.ConsumerDomain, requestContext, fallbackSmtp)
		{
		}

		// Token: 0x0600013D RID: 317 RVA: 0x000070FA File Offset: 0x000052FA
		public PuidAnchorMailbox(string puid, string domainName, IRequestContext requestContext, string fallbackSmtp) : this(puid, domainName, Guid.Empty, requestContext, fallbackSmtp)
		{
			if (string.IsNullOrEmpty(domainName))
			{
				throw new ArgumentNullException("domainName");
			}
		}

		// Token: 0x0600013E RID: 318 RVA: 0x0000711F File Offset: 0x0000531F
		public PuidAnchorMailbox(string puid, Guid tenantGuid, IRequestContext requestContext, string fallbackSmtp) : this(puid, string.Empty, tenantGuid, requestContext, fallbackSmtp)
		{
			if (tenantGuid == Guid.Empty)
			{
				throw new ArgumentNullException("tenantGuid");
			}
		}

		// Token: 0x0600013F RID: 319 RVA: 0x0000714C File Offset: 0x0000534C
		private PuidAnchorMailbox(string puid, string domainName, Guid tenantGuid, IRequestContext requestContext, string fallbackSmtp) : base(AnchorSource.Puid, puid, requestContext)
		{
			this.Domain = domainName;
			this.TenantGuid = tenantGuid;
			this.FallbackSmtp = fallbackSmtp;
			base.NotFoundExceptionCreator = delegate()
			{
				string message;
				if (!string.IsNullOrEmpty(domainName))
				{
					message = string.Format("Cannot find user in domain {0} with puid {1}.", domainName, puid);
				}
				else
				{
					message = string.Format("Cannot find user in tenant Guid {0} with puid {1}.", tenantGuid, puid);
				}
				return new HttpProxyException(HttpStatusCode.NotFound, 3002, message);
			};
		}

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000140 RID: 320 RVA: 0x000071B6 File Offset: 0x000053B6
		public static bool IsEnabled
		{
			get
			{
				return HttpProxySettings.PuidAnchorMailboxEnabled.Value;
			}
		}

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000141 RID: 321 RVA: 0x0000618F File Offset: 0x0000438F
		public string Puid
		{
			get
			{
				return (string)base.SourceObject;
			}
		}

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000142 RID: 322 RVA: 0x000071C2 File Offset: 0x000053C2
		// (set) Token: 0x06000143 RID: 323 RVA: 0x000071CA File Offset: 0x000053CA
		public string Domain { get; private set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000144 RID: 324 RVA: 0x000071D3 File Offset: 0x000053D3
		// (set) Token: 0x06000145 RID: 325 RVA: 0x000071DB File Offset: 0x000053DB
		public Guid TenantGuid { get; private set; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x06000146 RID: 326 RVA: 0x000071E4 File Offset: 0x000053E4
		// (set) Token: 0x06000147 RID: 327 RVA: 0x000071EC File Offset: 0x000053EC
		public string FallbackSmtp { get; private set; }

		// Token: 0x06000148 RID: 328 RVA: 0x000071F5 File Offset: 0x000053F5
		public override ITenantContext GetTenantContext()
		{
			if (this.TenantGuid == Guid.Empty)
			{
				return new DomainTenantContext(this.Domain);
			}
			return new ExternalDirectoryOrganizationIdTenantContext(this.TenantGuid);
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00007220 File Offset: 0x00005420
		protected override string ToCacheKey()
		{
			if (!string.IsNullOrEmpty(this.Domain))
			{
				return base.ToCacheKey() + "@" + this.Domain;
			}
			return base.ToCacheKey() + "@" + this.TenantGuid;
		}

		// Token: 0x0600014A RID: 330 RVA: 0x0000726C File Offset: 0x0000546C
		protected override ADRawEntry LoadADRawEntry()
		{
			ADRawEntry adrawEntry = null;
			ITenantRecipientSession tenantRecipientSession;
			if (this.TenantGuid != Guid.Empty)
			{
				tenantRecipientSession = DirectoryHelper.GetTenantRecipientSessionFromPuidAndTenantGuid(this.Puid, this.TenantGuid, base.RequestContext.Logger, base.RequestContext.LatencyTracker, false);
			}
			else
			{
				tenantRecipientSession = DirectoryHelper.GetTenantRecipientSessionFromPuidAndDomain(this.Puid, this.Domain, base.RequestContext.Logger, base.RequestContext.LatencyTracker, !string.IsNullOrEmpty(this.FallbackSmtp));
			}
			if (tenantRecipientSession != null)
			{
				adrawEntry = DirectoryHelper.InvokeAccountForest<ADRawEntry>(base.RequestContext.LatencyTracker, () => tenantRecipientSession.FindUniqueEntryByNetID(this.Puid, (this.TenantGuid == Guid.Empty) ? this.Domain : this.TenantGuid.ToString(), this.PropertySet, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\PuidAnchorMailbox.cs", 231, "LoadADRawEntry"), base.RequestContext.Logger, tenantRecipientSession);
			}
			if (adrawEntry == null && !string.IsNullOrEmpty(this.FallbackSmtp) && SmtpAddress.IsValidSmtpAddress(this.FallbackSmtp))
			{
				adrawEntry = new SmtpAnchorMailbox(this.FallbackSmtp, base.RequestContext)
				{
					NotFoundExceptionCreator = null
				}.GetADRawEntry();
			}
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(adrawEntry);
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00007380 File Offset: 0x00005580
		protected override IRoutingKey GetRoutingKey()
		{
			IRoutingKey result = null;
			NetID netID = null;
			if (NetID.TryParse(this.Puid, ref netID))
			{
				if (!string.IsNullOrEmpty(this.Domain))
				{
					result = new PuidAndTenantDomainRoutingKey(netID, this.Domain);
				}
				else
				{
					result = new PuidAndTenantGuidRoutingKey(netID, this.TenantGuid);
				}
			}
			return result;
		}
	}
}
