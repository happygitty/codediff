using System;
using Microsoft.Exchange.Clients.Owa.Core;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000064 RID: 100
	public class Logoff : OwaPage
	{
		// Token: 0x170000BC RID: 188
		// (get) Token: 0x06000353 RID: 851 RVA: 0x00013284 File Offset: 0x00011484
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
		// (get) Token: 0x06000355 RID: 853 RVA: 0x0001328C File Offset: 0x0001148C
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

		// Token: 0x06000356 RID: 854 RVA: 0x000132BF File Offset: 0x000114BF
		protected override void OnLoad(EventArgs e)
		{
			if (base.Request.IsChangePasswordLogoff())
			{
				this.reason = Logoff.LogoffReason.ChangePassword;
			}
			base.OnLoad(e);
		}

		// Token: 0x04000228 RID: 552
		private Logoff.LogoffReason reason;

		// Token: 0x02000100 RID: 256
		protected enum LogoffReason
		{
			// Token: 0x040004C7 RID: 1223
			UserInitiated,
			// Token: 0x040004C8 RID: 1224
			ChangePassword
		}
	}
}
