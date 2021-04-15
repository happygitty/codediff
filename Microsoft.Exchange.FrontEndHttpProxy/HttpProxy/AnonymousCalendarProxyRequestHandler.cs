using System;
using System.Net;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200008A RID: 138
	internal class AnonymousCalendarProxyRequestHandler : BEServerCookieProxyRequestHandler<OwaService>
	{
		// Token: 0x17000110 RID: 272
		// (get) Token: 0x06000497 RID: 1175 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x06000498 RID: 1176 RVA: 0x00019679 File Offset: 0x00017879
		internal static bool IsAnonymousCalendarRequest(HttpRequest request)
		{
			return AnonymousPublishingUrl.IsValidAnonymousPublishingUrl(request.Url);
		}

		// Token: 0x06000499 RID: 1177 RVA: 0x00019686 File Offset: 0x00017886
		protected override bool IsRoutingError(HttpWebResponse response)
		{
			return OwaProxyRequestHandler.IsRoutingErrorFromOWA(this, response) || base.IsRoutingError(response);
		}

		// Token: 0x0600049A RID: 1178 RVA: 0x0001969C File Offset: 0x0001789C
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			AnonymousPublishingUrl anonymousPublishingUrl = new AnonymousPublishingUrl(base.ClientRequest.Url);
			if (anonymousPublishingUrl.ParameterSegments.Length != 0)
			{
				AnchorMailbox result;
				if (anonymousPublishingUrl.ParameterSegments.Length > 2 && this.TryMatchByCid(anonymousPublishingUrl.Url.LocalPath.ToString(), out result))
				{
					return result;
				}
				if (this.TryMatchByGuidAtDomain(anonymousPublishingUrl, out result))
				{
					return result;
				}
				if (this.TryMatchBySmtpAddress(anonymousPublishingUrl, out result))
				{
					return result;
				}
			}
			return base.ResolveAnchorMailbox();
		}

		// Token: 0x0600049B RID: 1179 RVA: 0x0001970C File Offset: 0x0001790C
		private bool TryMatchByCid(string publishingUrl, out AnchorMailbox anchorMailbox)
		{
			anchorMailbox = null;
			CID cid;
			if (RequestPathParser.TryGetCid(publishingUrl, ref cid))
			{
				anchorMailbox = new CidAnchorMailbox(cid.ToString(), this);
			}
			return anchorMailbox != null;
		}

		// Token: 0x0600049C RID: 1180 RVA: 0x0001973C File Offset: 0x0001793C
		private bool TryMatchByGuidAtDomain(AnonymousPublishingUrl publishingUrl, out AnchorMailbox anchorMailbox)
		{
			anchorMailbox = null;
			string text = publishingUrl.ParameterSegments[0];
			Guid empty = Guid.Empty;
			string empty2 = string.Empty;
			if (RequestHeaderParser.TryGetMailboxGuid(text, ref empty, ref empty2))
			{
				string text2 = string.Format("AnonymousPublishingUrl-MailboxGuid{0}", string.IsNullOrEmpty(empty2) ? string.Empty : "WithDomainAndSmtpFallback");
				base.Logger.Set(3, text2);
				MailboxGuidAnchorMailbox mailboxGuidAnchorMailbox = new MailboxGuidAnchorMailbox(empty, empty2, this);
				if (!string.IsNullOrEmpty(empty2))
				{
					mailboxGuidAnchorMailbox.FallbackSmtp = text;
				}
				anchorMailbox = mailboxGuidAnchorMailbox;
			}
			return anchorMailbox != null;
		}

		// Token: 0x0600049D RID: 1181 RVA: 0x000197C4 File Offset: 0x000179C4
		private bool TryMatchBySmtpAddress(AnonymousPublishingUrl publishingUrl, out AnchorMailbox anchorMailbox)
		{
			anchorMailbox = null;
			string text = publishingUrl.ParameterSegments[0];
			if (!string.IsNullOrEmpty(text) && SmtpAddress.IsValidSmtpAddress(text))
			{
				base.Logger.Set(3, "AnonymousPublishingUrl-SMTP");
				anchorMailbox = new SmtpAnchorMailbox(text, this);
			}
			return anchorMailbox != null;
		}
	}
}
