using System;
using System.Linq;
using System.Web;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000083 RID: 131
	internal static class UrlUtilities
	{
		// Token: 0x06000451 RID: 1105 RVA: 0x000184D8 File Offset: 0x000166D8
		public static bool IsEcpUrl(string urlString)
		{
			if (string.IsNullOrEmpty(urlString))
			{
				return false;
			}
			if (urlString.Equals("/ecp", StringComparison.OrdinalIgnoreCase) || urlString.StartsWith("/ecp/", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			Uri uri = null;
			return Uri.TryCreate(urlString, UriKind.Absolute, out uri) && uri != null && (uri.AbsolutePath.Equals("/ecp", StringComparison.OrdinalIgnoreCase) || uri.AbsolutePath.StartsWith("/ecp/", StringComparison.OrdinalIgnoreCase));
		}

		// Token: 0x06000452 RID: 1106 RVA: 0x00018550 File Offset: 0x00016750
		public static bool IsEacUrl(string urlString)
		{
			if (!UrlUtilities.IsEcpUrl(urlString))
			{
				return false;
			}
			int num = urlString.IndexOf('?');
			if (num > 0)
			{
				string[] source = urlString.Substring(num + 1).Split(new char[]
				{
					'&'
				});
				return !source.Contains("rfr=owa") && !source.Contains("rfr=olk");
			}
			return true;
		}

		// Token: 0x06000453 RID: 1107 RVA: 0x000185AD File Offset: 0x000167AD
		public static bool IsIntegratedAuthUrl(Uri url)
		{
			return RequestPathParser.IsIntegratedAuthUrl(url.AbsolutePath);
		}

		// Token: 0x06000454 RID: 1108 RVA: 0x000185BA File Offset: 0x000167BA
		public static Uri FixIntegratedAuthUrlForBackEnd(Uri url)
		{
			return UrlHelper.FixIntegratedAuthUrlForBackEnd(url);
		}

		// Token: 0x06000455 RID: 1109 RVA: 0x000185C4 File Offset: 0x000167C4
		public static Uri FixDFPOWAVdirUrlForBackEnd(Uri url, string dfpOwaVdir)
		{
			if (string.IsNullOrEmpty(dfpOwaVdir))
			{
				return url;
			}
			UriBuilder uriBuilder = new UriBuilder(url);
			string absolutePath = url.AbsolutePath;
			int num = url.AbsolutePath.IndexOf("/owa", StringComparison.OrdinalIgnoreCase);
			uriBuilder.Path = absolutePath.Substring(0, num + 1) + dfpOwaVdir;
			int num2 = num + "/owa".Length;
			if (absolutePath.Length > num2)
			{
				uriBuilder.Path += absolutePath.Substring(num2);
			}
			return uriBuilder.Uri;
		}

		// Token: 0x06000456 RID: 1110 RVA: 0x00018648 File Offset: 0x00016848
		internal static bool IsOwaMiniUrl(Uri url)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			return url.AbsolutePath.EndsWith(Constants.OMAPath, StringComparison.OrdinalIgnoreCase) || url.AbsolutePath.IndexOf(Constants.OMAPath + "/", StringComparison.OrdinalIgnoreCase) != -1;
		}

		// Token: 0x06000457 RID: 1111 RVA: 0x000186A0 File Offset: 0x000168A0
		internal static bool IsCmdWebPart(HttpRequest request)
		{
			string text = request.QueryString["cmd"];
			return !string.IsNullOrEmpty(text) && string.Equals(text, "contents", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x06000458 RID: 1112 RVA: 0x000186D4 File Offset: 0x000168D4
		internal static Uri AppendSmtpAddressToUrl(Uri url, string smtpAddress)
		{
			UriBuilder uriBuilder = new UriBuilder(url);
			if (!uriBuilder.Path.EndsWith("/", StringComparison.Ordinal))
			{
				UriBuilder uriBuilder2 = uriBuilder;
				uriBuilder2.Path += "/";
			}
			UriBuilder uriBuilder3 = uriBuilder;
			uriBuilder3.Path += smtpAddress;
			return uriBuilder.Uri;
		}

		// Token: 0x04000301 RID: 769
		private const string Command = "cmd";

		// Token: 0x04000302 RID: 770
		private const string CommandValue = "contents";
	}
}
