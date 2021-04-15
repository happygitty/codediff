using System;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Routing;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000022 RID: 34
	internal class LiveIdMemberNameAnchorMailbox : UserBasedAnchorMailbox
	{
		// Token: 0x06000118 RID: 280 RVA: 0x0000691C File Offset: 0x00004B1C
		public LiveIdMemberNameAnchorMailbox(string liveIdMemberName, string organizationContext, IRequestContext requestContext) : base(AnchorSource.LiveIdMemberName, liveIdMemberName, requestContext)
		{
			if (string.IsNullOrEmpty(liveIdMemberName))
			{
				throw new ArgumentNullException("liveIdMemberName");
			}
			this.OrganizationContext = organizationContext;
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000119 RID: 281 RVA: 0x0000618F File Offset: 0x0000438F
		public string LiveIdMemberName
		{
			get
			{
				return (string)base.SourceObject;
			}
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x0600011A RID: 282 RVA: 0x00006942 File Offset: 0x00004B42
		// (set) Token: 0x0600011B RID: 283 RVA: 0x0000694A File Offset: 0x00004B4A
		public string OrganizationContext { get; private set; }

		// Token: 0x0600011C RID: 284 RVA: 0x00006954 File Offset: 0x00004B54
		public override ITenantContext GetTenantContext()
		{
			if (this.OrganizationContext != null)
			{
				return new DomainTenantContext(this.OrganizationContext);
			}
			if (SmtpAddress.IsValidSmtpAddress(this.LiveIdMemberName))
			{
				return new DomainTenantContext(SmtpAddress.Parse(this.LiveIdMemberName).Domain);
			}
			return new ExternalDirectoryOrganizationIdTenantContext(Guid.Empty);
		}

		// Token: 0x0600011D RID: 285 RVA: 0x000069A8 File Offset: 0x00004BA8
		protected override ADRawEntry LoadADRawEntry()
		{
			bool value = AnchorMailbox.AllowMissingTenant.Value;
			ITenantRecipientSession session = DirectoryHelper.GetTenantRecipientSessionFromSmtpOrLiveId(this.LiveIdMemberName, base.RequestContext.Logger, base.RequestContext.LatencyTracker, value);
			if (value && session == null)
			{
				return null;
			}
			ExTraceGlobals.VerboseTracer.Information<string, string, string>((long)this.GetHashCode(), "Searching GC {0} for LiveIdMemberName {1}, OrganizationContext {2}", session.DomainController ?? "<null>", this.LiveIdMemberName, this.OrganizationContext ?? "<null>");
			ADRawEntry adrawEntry = DirectoryHelper.InvokeAccountForest<ADRawEntry>(base.RequestContext.LatencyTracker, () => session.FindByLiveIdMemberName(this.LiveIdMemberName, this.PropertySet, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\LiveIdMemberNameAnchorMailbox.cs", 113, "LoadADRawEntry"), base.RequestContext.Logger, session);
			if (adrawEntry != null && base.RequestContext.HttpContext.User.Identity.Name.Equals(this.LiveIdMemberName, StringComparison.OrdinalIgnoreCase))
			{
				base.RequestContext.HttpContext.Items[Constants.CallerADRawEntryKeyName] = adrawEntry;
			}
			return adrawEntry;
		}

		// Token: 0x0600011E RID: 286 RVA: 0x00006AB6 File Offset: 0x00004CB6
		protected override IRoutingKey GetRoutingKey()
		{
			return new LiveIdMemberNameRoutingKey(new SmtpAddress(this.LiveIdMemberName), this.OrganizationContext);
		}
	}
}
