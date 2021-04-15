using System;
using System.Text;
using System.Web;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.ExchangeSystem;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000056 RID: 86
	internal class OwaEcpRedirectStrategy : DatacenterRedirectStrategy
	{
		// Token: 0x060002BC RID: 700 RVA: 0x0000DB68 File Offset: 0x0000BD68
		public OwaEcpRedirectStrategy(IRequestContext requestContext) : base(requestContext)
		{
		}

		// Token: 0x060002BD RID: 701 RVA: 0x0000DB74 File Offset: 0x0000BD74
		protected override Uri GetRedirectUrl(string redirectServer)
		{
			Uri uri = new Uri(OwaEcpRedirectStrategy.GetPodRedirectUrl(base.RequestContext.HttpContext.Request.Url, redirectServer));
			string text = null;
			string host = base.RequestContext.HttpContext.Request.Url.Host;
			HttpCookie httpCookie = base.RequestContext.HttpContext.Request.Cookies["orgName"];
			if (httpCookie != null && !string.IsNullOrEmpty(httpCookie.Value))
			{
				text = httpCookie.Value.ToLowerInvariant();
			}
			if (text != null && !host.Contains(Constants.OutlookDomain))
			{
				uri = OwaEcpRedirectStrategy.AddRealmParameter(uri, text);
			}
			return uri;
		}

		// Token: 0x060002BE RID: 702 RVA: 0x0000DC14 File Offset: 0x0000BE14
		private static Uri AddRealmParameter(Uri uri, string org)
		{
			if (!string.IsNullOrEmpty(org))
			{
				string text = "realm=" + HttpUtility.UrlEncode(org);
				UriBuilder uriBuilder = new UriBuilder(uri);
				if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
				{
					uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + text;
				}
				else
				{
					uriBuilder.Query = text;
				}
				uri = uriBuilder.Uri;
			}
			return uri;
		}

		// Token: 0x060002BF RID: 703 RVA: 0x0000DC88 File Offset: 0x0000BE88
		private static string GetPodRedirectUrl(Uri url, string fqdn)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(url.Scheme);
			stringBuilder.Append("://");
			stringBuilder.Append(fqdn);
			if (OwaEcpRedirectStrategy.ShouldSaveUrlOnLogoff(url) || OwaEcpRedirectStrategy.IsInCalendarVDir.Member)
			{
				stringBuilder.Append(url.PathAndQuery);
			}
			else
			{
				stringBuilder.Append("/");
				stringBuilder.Append(HttpProxyGlobals.VirtualDirectoryName.Member);
				stringBuilder.Append("/");
				string value;
				if (OwaEcpRedirectStrategy.TryGetExplicitLogonUrlSegment(url, out value))
				{
					stringBuilder.Append(value);
					stringBuilder.Append("/");
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060002C0 RID: 704 RVA: 0x0000DD2C File Offset: 0x0000BF2C
		private static bool ShouldSaveUrlOnLogoff(Uri url)
		{
			return OwaEcpRedirectStrategy.ReturnToOriginalUrlByDefault.Value || url.Query.Contains("exsvurl=1") || url.Query.Contains("rru=contacts");
		}

		// Token: 0x060002C1 RID: 705 RVA: 0x0000DD60 File Offset: 0x0000BF60
		private static bool TryGetExplicitLogonUrlSegment(Uri url, out string explicitLogonSegment)
		{
			explicitLogonSegment = string.Empty;
			string originalString = url.OriginalString;
			string text = HttpProxyGlobals.VirtualDirectoryName.Member + "/";
			int num = originalString.IndexOf(text) + text.Length;
			if (num < 0 || num >= originalString.Length)
			{
				return false;
			}
			int num2 = originalString.IndexOf("/", num);
			if (num2 == -1)
			{
				return false;
			}
			int length = num2 - num;
			explicitLogonSegment = originalString.Substring(num, length);
			int num3 = explicitLogonSegment.IndexOf('@');
			return num3 > 0 && num3 < explicitLogonSegment.Length - 2;
		}

		// Token: 0x040001A4 RID: 420
		private const string OrganizationNameCookieName = "orgName";

		// Token: 0x040001A5 RID: 421
		private const string CalendarVDirPostfix = "/calendar";

		// Token: 0x040001A6 RID: 422
		private const string SaveUrlOnLogoffParameter = "exsvurl=1";

		// Token: 0x040001A7 RID: 423
		private const string RruUrlParameter = "rru=contacts";

		// Token: 0x040001A8 RID: 424
		private static readonly LazyMember<bool> IsInCalendarVDir = new LazyMember<bool>(() => !string.IsNullOrEmpty(HttpRuntime.AppDomainAppId) && HttpRuntime.AppDomainAppId.EndsWith("/calendar", StringComparison.CurrentCultureIgnoreCase));

		// Token: 0x040001A9 RID: 425
		private static readonly BoolAppSettingsEntry ReturnToOriginalUrlByDefault = new BoolAppSettingsEntry("ReturnToOriginalUrlByDefault", false, ExTraceGlobals.VerboseTracer);
	}
}
