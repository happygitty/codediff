using System;
using System.Web.Configuration;
using Microsoft.Exchange.Clients.Owa.Core;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000066 RID: 102
	public class OutlookCN : OwaPage
	{
		// Token: 0x170000CF RID: 207
		// (get) Token: 0x0600036B RID: 875 RVA: 0x00013648 File Offset: 0x00011848
		protected string IcpLink
		{
			get
			{
				if (OutlookCN.icpLink == null)
				{
					OutlookCN.icpLink = WebConfigurationManager.AppSettings["GallatinIcpLink"];
				}
				return OutlookCN.icpLink;
			}
		}

		// Token: 0x0400022E RID: 558
		private const string IcpLinkAppSetting = "GallatinIcpLink";

		// Token: 0x0400022F RID: 559
		private static string icpLink;
	}
}
