using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A6 RID: 166
	internal class OwaExtensibilityProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x060005B3 RID: 1459 RVA: 0x0001F998 File Offset: 0x0001DB98
		internal static bool IsOwaExtensibilityRequest(HttpRequest request)
		{
			return OwaExtensibilityProxyRequestHandler.ExtPathRegex.IsMatch(request.RawUrl) || OwaExtensibilityProxyRequestHandler.ScriptsPathRegex.IsMatch(request.RawUrl) || OwaExtensibilityProxyRequestHandler.StylesPathRegex.IsMatch(request.RawUrl);
		}

		// Token: 0x060005B4 RID: 1460 RVA: 0x0001F9D0 File Offset: 0x0001DBD0
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string, Uri>((long)this.GetHashCode(), "[OwaExtensibilityProxyRequestHandler::ResolveAnchorMailbox]: Method {0}; Url {1};", base.ClientRequest.HttpMethod, base.ClientRequest.Url);
			}
			Match match = OwaExtensibilityProxyRequestHandler.ExtPathRegex.Match(base.ClientRequest.RawUrl);
			if (!match.Success)
			{
				match = OwaExtensibilityProxyRequestHandler.ScriptsPathRegex.Match(base.ClientRequest.RawUrl);
				if (!match.Success)
				{
					match = OwaExtensibilityProxyRequestHandler.StylesPathRegex.Match(base.ClientRequest.RawUrl);
				}
			}
			Guid guid;
			string text;
			if (match.Success && RegexUtilities.TryGetMailboxGuidAddressFromRegexMatch(match, ref guid, ref text))
			{
				this.routingHint = string.Format("{0}@{1}", guid, text);
				AnchorMailbox result = new MailboxGuidAnchorMailbox(guid, text, this);
				base.Logger.Set(3, "OwaExtension-MailboxGuidWithDomain");
				return result;
			}
			throw new HttpProxyException(HttpStatusCode.NotFound, 3007, string.Format("Unable to find target server for url: {0}", base.ClientRequest.Url));
		}

		// Token: 0x060005B5 RID: 1461 RVA: 0x0001FAD8 File Offset: 0x0001DCD8
		protected override UriBuilder GetClientUrlForProxy()
		{
			UriBuilder uriBuilder = new UriBuilder(base.ClientRequest.Url);
			if (!string.IsNullOrEmpty(this.routingHint))
			{
				string text = base.ClientRequest.Url.AbsolutePath;
				text = HttpUtility.UrlDecode(text);
				string text2 = "/" + this.routingHint;
				int num = text.IndexOf(text2);
				if (num != -1)
				{
					string path = text.Substring(0, num) + text.Substring(num + text2.Length);
					uriBuilder.Path = path;
				}
			}
			return uriBuilder;
		}

		// Token: 0x060005B6 RID: 1462 RVA: 0x0001E8DA File Offset: 0x0001CADA
		protected override Uri GetTargetBackEndServerUrl()
		{
			return UrlUtilities.FixIntegratedAuthUrlForBackEnd(base.GetTargetBackEndServerUrl());
		}

		// Token: 0x04000385 RID: 901
		private static readonly Regex ExtPathRegex = new Regex("/owa/((?<hint>[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}@[A-Z0-9.-]+\\.[A-Z]{2,4})/)prem/\\d{2}\\.\\d{1,}\\.\\d{1,}\\.\\d{1,}/ext/def/.*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x04000386 RID: 902
		private static readonly Regex ScriptsPathRegex = new Regex("/owa/((?<hint>[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}@[A-Z0-9.-]+\\.[A-Z]{2,4})/)prem/\\d{2}\\.\\d{1,}\\.\\d{1,}\\.\\d{1,}/scripts/.*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x04000387 RID: 903
		private static readonly Regex StylesPathRegex = new Regex("/owa/((?<hint>[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}@[A-Z0-9.-]+\\.[A-Z]{2,4})/)prem/\\d{2}\\.\\d{1,}\\.\\d{1,}\\.\\d{1,}/resources/styles/.*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x04000388 RID: 904
		private string routingHint;
	}
}
