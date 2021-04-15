using System;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000BC RID: 188
	internal class XRopProxyRequestHandler : BEServerCookieProxyRequestHandler<RpcHttpService>
	{
		// Token: 0x17000182 RID: 386
		// (get) Token: 0x06000744 RID: 1860 RVA: 0x00003193 File Offset: 0x00001393
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x06000745 RID: 1861 RVA: 0x0002AC50 File Offset: 0x00028E50
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			string text = base.ClientRequest.QueryString[Constants.AnchorMailboxHeaderName];
			if (!string.IsNullOrEmpty(text) && SmtpAddress.IsValidSmtpAddress(text))
			{
				SmtpAnchorMailbox smtpAnchorMailbox = new SmtpAnchorMailbox(text, this);
				string text2 = "AnchorMailboxQuery-SMTP";
				if (!Extensions.IsProxyTestProbeRequest(Extensions.GetHttpRequestBase(base.ClientRequest)))
				{
					smtpAnchorMailbox.IsArchive = new bool?(true);
					text2 = "AnchorMailboxQuery-Archive-SMTP";
				}
				base.Logger.Set(3, text2);
				return smtpAnchorMailbox;
			}
			return new OrganizationAnchorMailbox(OrganizationId.ForestWideOrgId, this);
		}
	}
}
