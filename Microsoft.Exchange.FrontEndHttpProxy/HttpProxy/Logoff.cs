using System;
using Microsoft.Exchange.Clients.Owa.Core;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000064 RID: 100
	public class Logoff : OwaPage
	{
		// Token: 0x170000BC RID: 188
		// (get) Token: 0x06000353 RID: 851 RVA: 0x00013248 File Offset: 0x00011448
		protected Logoff.LogoffReason Reason
		{
			get
			{
				return this.reason;
			}
		}

		// Token: 0x170000BD RID: 189
		// (get) Token: 0x06000354 RID: 852 RVA: 0x00003165 File Offset: 0x00001365
		protected override bool UseStrictMode
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170000BE RID: 190
		// (get) Token: 0x06000355 RID: 853 RVA: 0x00013250 File Offset: 0x00011450
		protected string Message
		{
			get
			{
				if (this.Reason != Logoff.LogoffReason.ChangePassword)
				{
					return LocalizedStrings.GetHtmlEncoded(1735477837);
				}
				if (base.IsDownLevelClient)
				{
					return LocalizedStrings.GetHtmlEncoded(252488134);
				}
				return LocalizedStrings.GetHtmlEncoded(575439440);
			}
		}

		// Token: 0x06000356 RID: 854 RVA: 0x00013283 File Offset: 0x00011483
		protected override void OnLoad(EventArgs e)
		{
			if (base.Request.IsChangePasswordLogoff())
			{
				this.reason = Logoff.LogoffReason.ChangePassword;
			}
			base.OnLoad(e);
		}

		// Token: 0x04000227 RID: 551
		private Logoff.LogoffReason reason;

		// Token: 0x02000101 RID: 257
		protected enum LogoffReason
		{
			// Token: 0x040004C3 RID: 1219
			UserInitiated,
			// Token: 0x040004C4 RID: 1220
			ChangePassword
		}
	}
}
