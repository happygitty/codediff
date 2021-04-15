using System;
using System.Net;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Net.Wopi;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000097 RID: 151
	internal class AuthenticatedWopiProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x06000539 RID: 1337 RVA: 0x0001CFAC File Offset: 0x0001B1AC
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			UriBuilder uriBuilder = new UriBuilder(base.ClientRequest.Url);
			uriBuilder.Scheme = "https";
			uriBuilder.Port = 444;
			string mailboxId = AuthenticatedWopiRequestPathHandler.GetMailboxId(base.ClientRequest);
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

		// Token: 0x0600053A RID: 1338 RVA: 0x0001D034 File Offset: 0x0001B234
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
				uriBuilder.Path = AuthenticatedWopiRequestPathHandler.StripMailboxId(HttpUtility.UrlDecode(uriBuilder.Path), this.targetMailboxId);
				uriBuilder.Query = uri.Query.Substring(1) + "&UserEmail=" + HttpUtility.UrlEncode(this.targetMailboxId);
				uri = uriBuilder.Uri;
			}
			if (HttpProxySettings.DFPOWAVdirProxyEnabled.Value)
			{
				return UrlUtilities.FixDFPOWAVdirUrlForBackEnd(uri, HttpUtility.ParseQueryString(uri.Query)["vdir"]);
			}
			return uri;
		}

		// Token: 0x0600053B RID: 1339 RVA: 0x0001D121 File Offset: 0x0001B321
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			OwaProxyRequestHandler.AddProxyUriHeader(base.ClientRequest, headers);
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x04000365 RID: 869
		private string targetMailboxId;
	}
}
