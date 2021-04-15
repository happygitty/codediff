using System;
using System.Web;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;
using Microsoft.Exchange.Security.OAuth;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000024 RID: 36
	internal class OAuthActAsUserAnchorMailbox : UserBasedAnchorMailbox
	{
		// Token: 0x0600012D RID: 301 RVA: 0x00006E8A File Offset: 0x0000508A
		public OAuthActAsUserAnchorMailbox(OAuthActAsUser actAsUser, IRequestContext requestContext) : base(AnchorSource.OAuthActAsUser, actAsUser, requestContext)
		{
			this.actAsUser = actAsUser;
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00006E9D File Offset: 0x0000509D
		public override ITenantContext GetTenantContext()
		{
			return new ExternalDirectoryOrganizationIdTenantContext(base.GetExternalDirectoryOrganizationGuidFromADRawEntry(this.actAsUser.ADRawEntry));
		}

		// Token: 0x0600012F RID: 303 RVA: 0x00006EB8 File Offset: 0x000050B8
		protected override ADRawEntry LoadADRawEntry()
		{
			ADRawEntry result;
			try
			{
				ADRawEntry adrawEntry = this.actAsUser.ADRawEntry;
				result = base.CheckForNullAndThrowIfApplicable<ADRawEntry>(adrawEntry);
			}
			catch (InvalidOAuthTokenException ex)
			{
				throw new HttpException((ex.ErrorCategory == 2000007) ? 500 : 401, string.Empty, ex);
			}
			return result;
		}

		// Token: 0x040000E6 RID: 230
		private readonly OAuthActAsUser actAsUser;
	}
}
