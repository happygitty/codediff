using System;
using System.Security.Principal;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A1 RID: 161
	internal sealed class RestProxyRequestHandler : EwsProxyRequestHandler
	{
		// Token: 0x06000587 RID: 1415 RVA: 0x0001EF50 File Offset: 0x0001D150
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			IIdentity identity = base.HttpContext.User.Identity;
			string text;
			if (!RequestPathParser.IsRestGroupUserActionRequest(base.ClientRequest.Url.AbsolutePath) && RequestPathParser.TryGetTargetMailbox(base.ClientRequest.Url.PathAndQuery, ref text) && SmtpAddress.IsValidSmtpAddress(text))
			{
				Guid guid;
				Guid guid2;
				if (RequestPathParser.TryParseExternalDirectoryId(text, ref guid, ref guid2))
				{
					base.Logger.SafeSet(3, "TargetMailbox-ExternalDirectoryObjectId");
					return new ExternalDirectoryObjectIdAnchorMailbox(guid.ToString(), guid2, this);
				}
				if (!RequestPathParser.TryParseSpoProxy(text, ref guid2))
				{
					base.Logger.Set(3, "TargetMailbox-SMTP");
					return new SmtpAnchorMailbox(text, this);
				}
				ADRawEntry adrawEntry = DirectoryHelper.ResolveMailboxByProxyAddress(base.LatencyTracker, base.Logger, guid2, text, "SPO");
				if (adrawEntry != null)
				{
					base.Logger.SafeSet(3, "TargetMailbox-SpoProxy");
					return new ProxyAddressAnchorMailbox(adrawEntry, this);
				}
			}
			return base.ResolveAnchorMailbox();
		}
	}
}
