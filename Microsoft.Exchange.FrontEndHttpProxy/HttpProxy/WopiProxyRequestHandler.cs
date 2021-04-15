using System;
using System.Net;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Net.Wopi;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000BB RID: 187
	internal class WopiProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x0600073D RID: 1853 RVA: 0x0002A870 File Offset: 0x00028A70
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			string mailboxId = WopiRequestPathHandler.GetMailboxId(base.ClientRequest);
			if (!string.IsNullOrEmpty(mailboxId))
			{
				this.targetMailboxId = mailboxId;
				AnchorMailbox result;
				if (AnchorMailboxFactory.TryCreateFromMailboxGuid(this, mailboxId, out result))
				{
					return result;
				}
				if (SmtpAddress.IsValidSmtpAddress(mailboxId))
				{
					base.Logger.Set(3, "Url-SMTP");
					return new SmtpAnchorMailbox(mailboxId, this);
				}
			}
			return base.ResolveAnchorMailbox();
		}

		// Token: 0x0600073E RID: 1854 RVA: 0x0002A8D4 File Offset: 0x00028AD4
		protected override Uri GetTargetBackEndServerUrl()
		{
			Uri uri = base.GetTargetBackEndServerUrl();
			if (base.AnchoredRoutingTarget.BackEndServer.Version < Server.E15MinVersion)
			{
				throw new HttpException(500, string.Format("Version < E14 and a WOPI request?  Should not happen....  AnchorMailbox: {0}", base.AnchoredRoutingTarget.AnchorMailbox));
			}
			if (uri.Query.Length == 0)
			{
				throw new HttpException(400, "Unexpected query string format");
			}
			if (!string.IsNullOrEmpty(this.targetMailboxId))
			{
				UriBuilder uriBuilder = new UriBuilder(uri);
				uriBuilder.Path = WopiRequestPathHandler.StripMailboxId(HttpUtility.UrlDecode(uriBuilder.Path), this.targetMailboxId);
				uriBuilder.Query = uri.Query.Substring(1) + "&UserEmail=" + HttpUtility.UrlEncode(this.targetMailboxId);
				uri = uriBuilder.Uri;
			}
			if (HttpProxySettings.DFPOWAVdirProxyEnabled.Value)
			{
				return UrlUtilities.FixDFPOWAVdirUrlForBackEnd(uri, HttpUtility.ParseQueryString(uri.Query)["vdir"]);
			}
			return uri;
		}

		// Token: 0x0600073F RID: 1855 RVA: 0x0001D121 File Offset: 0x0001B321
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			OwaProxyRequestHandler.AddProxyUriHeader(base.ClientRequest, headers);
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x06000740 RID: 1856 RVA: 0x00019686 File Offset: 0x00017886
		protected override bool IsRoutingError(HttpWebResponse response)
		{
			return OwaProxyRequestHandler.IsRoutingErrorFromOWA(this, response) || base.IsRoutingError(response);
		}

		// Token: 0x040003FA RID: 1018
		private string targetMailboxId;
	}
}
