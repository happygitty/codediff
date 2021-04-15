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
		// Token: 0x0600073F RID: 1855 RVA: 0x0002AAFC File Offset: 0x00028CFC
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

		// Token: 0x06000740 RID: 1856 RVA: 0x0002AB60 File Offset: 0x00028D60
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

		// Token: 0x06000741 RID: 1857 RVA: 0x0001D2C5 File Offset: 0x0001B4C5
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			OwaProxyRequestHandler.AddProxyUriHeader(base.ClientRequest, headers);
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x06000742 RID: 1858 RVA: 0x00019846 File Offset: 0x00017A46
		protected override bool IsRoutingError(HttpWebResponse response)
		{
			return OwaProxyRequestHandler.IsRoutingErrorFromOWA(this, response) || base.IsRoutingError(response);
		}

		// Token: 0x040003FE RID: 1022
		private string targetMailboxId;
	}
}
