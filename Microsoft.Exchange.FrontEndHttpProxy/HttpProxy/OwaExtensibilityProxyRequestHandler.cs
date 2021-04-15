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
		// Token: 0x060005B6 RID: 1462 RVA: 0x0001FB3C File Offset: 0x0001DD3C
		internal static bool IsOwaExtensibilityRequest(HttpRequest request)
		{
			return OwaExtensibilityProxyRequestHandler.ExtPathRegex.IsMatch(request.RawUrl) || OwaExtensibilityProxyRequestHandler.ScriptsPathRegex.IsMatch(request.RawUrl) || OwaExtensibilityProxyRequestHandler.StylesPathRegex.IsMatch(request.RawUrl);
		}

		// Token: 0x060005B7 RID: 1463 RVA: 0x0001FB74 File Offset: 0x0001DD74
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

		// Token: 0x060005B8 RID: 1464 RVA: 0x0001FC7C File Offset: 0x0001DE7C
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

		// Token: 0x060005B9 RID: 1465 RVA: 0x0001EA7E File Offset: 0x0001CC7E
		protected override Uri GetTargetBackEndServerUrl()
		{
			return UrlUtilities.FixIntegratedAuthUrlForBackEnd(base.GetTargetBackEndServerUrl());
		}

		// Token: 0x04000389 RID: 905
		private static readonly Regex ExtPathRegex = new Regex("/owa/((?<hint>[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}@[A-Z0-9.-]+\\.[A-Z]{2,4})/)prem/\\d{2}\\.\\d{1,}\\.\\d{1,}\\.\\d{1,}/ext/def/.*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x0400038A RID: 906
		private static readonly Regex ScriptsPathRegex = new Regex("/owa/((?<hint>[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}@[A-Z0-9.-]+\\.[A-Z]{2,4})/)prem/\\d{2}\\.\\d{1,}\\.\\d{1,}\\.\\d{1,}/scripts/.*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x0400038B RID: 907
		private static readonly Regex StylesPathRegex = new Regex("/owa/((?<hint>[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}@[A-Z0-9.-]+\\.[A-Z]{2,4})/)prem/\\d{2}\\.\\d{1,}\\.\\d{1,}\\.\\d{1,}/resources/styles/.*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x0400038C RID: 908
		private string routingHint;
	}
}
