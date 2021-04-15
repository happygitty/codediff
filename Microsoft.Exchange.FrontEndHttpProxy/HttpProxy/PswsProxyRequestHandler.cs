using System;
using System.Net;
using System.Web;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Security.OAuth;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000AF RID: 175
	internal class PswsProxyRequestHandler : RwsPswsProxyRequestHandlerBase<WebServicesService>
	{
		// Token: 0x17000174 RID: 372
		// (get) Token: 0x060006E3 RID: 1763 RVA: 0x000288DD File Offset: 0x00026ADD
		protected override string ServiceName
		{
			get
			{
				return "PowerShell Web Service";
			}
		}

		// Token: 0x060006E4 RID: 1764 RVA: 0x000288E4 File Offset: 0x00026AE4
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return string.Equals(headerName, "client-request-id", StringComparison.OrdinalIgnoreCase) || (!string.Equals(headerName, "proxy", StringComparison.OrdinalIgnoreCase) && base.ShouldCopyHeaderToServerRequest(headerName));
		}

		// Token: 0x060006E5 RID: 1765 RVA: 0x0002890D File Offset: 0x00026B0D
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			headers.Add("public-server-uri", base.ClientRequest.Url.GetLeftPart(UriPartial.Authority));
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x060006E6 RID: 1766 RVA: 0x00028934 File Offset: 0x00026B34
		protected override void DoProtocolSpecificBeginProcess()
		{
			base.DoProtocolSpecificBeginProcess();
			string message;
			if (!this.AuthorizeOAuthRequest(out message))
			{
				throw new HttpException(403, message);
			}
			string domain;
			if (base.TryGetTenantDomain("organization", out domain))
			{
				base.IsDomainBasedRequest = true;
				base.Domain = domain;
			}
		}

		// Token: 0x060006E7 RID: 1767 RVA: 0x0002897C File Offset: 0x00026B7C
		private bool AuthorizeOAuthRequest(out string errorMsg)
		{
			OAuthIdentity oauthIdentity = base.HttpContext.User.Identity as OAuthIdentity;
			string empty = string.Empty;
			errorMsg = string.Empty;
			if (oauthIdentity != null && base.TryGetTenantDomain("organization", out empty))
			{
				string text = string.Empty;
				if (oauthIdentity.OrganizationId != null)
				{
					text = oauthIdentity.OrganizationId.ConfigurationUnit.ToString();
				}
				if (!string.IsNullOrEmpty(text) && string.Compare(text, empty, true) != 0)
				{
					errorMsg = string.Format("{0} is not a authorized tenant. The authorized tenant is {1}", empty, text);
					return false;
				}
			}
			return true;
		}

		// Token: 0x040003E4 RID: 996
		private const string TenantParameterName = "organization";
	}
}
