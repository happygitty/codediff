using System;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200002F RID: 47
	internal class UrlAnchorMailbox : AnchorMailbox
	{
		// Token: 0x0600017E RID: 382 RVA: 0x00007F9F File Offset: 0x0000619F
		public UrlAnchorMailbox(Uri url, IRequestContext requestContext) : base(AnchorSource.Url, url, requestContext)
		{
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x0600017F RID: 383 RVA: 0x00007FAB File Offset: 0x000061AB
		public Uri Url
		{
			get
			{
				return (Uri)base.SourceObject;
			}
		}

		// Token: 0x06000180 RID: 384 RVA: 0x00007FB8 File Offset: 0x000061B8
		public override BackEndServer TryDirectBackEndCalculation()
		{
			return new BackEndServer(this.Url.Host, Server.E15MinVersion);
		}
	}
}
