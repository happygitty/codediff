using System;
using System.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000082 RID: 130
	internal class NullWebProxy : IWebProxy
	{
		// Token: 0x170000FC RID: 252
		// (get) Token: 0x0600044E RID: 1102 RVA: 0x0001866B File Offset: 0x0001686B
		public static NullWebProxy Instance
		{
			get
			{
				return NullWebProxy.instance;
			}
		}

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x0600044F RID: 1103 RVA: 0x00018672 File Offset: 0x00016872
		// (set) Token: 0x06000450 RID: 1104 RVA: 0x0001867A File Offset: 0x0001687A
		public ICredentials Credentials { get; set; }

		// Token: 0x06000451 RID: 1105 RVA: 0x00018683 File Offset: 0x00016883
		public Uri GetProxy(Uri destination)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000452 RID: 1106 RVA: 0x00003193 File Offset: 0x00001393
		public bool IsBypassed(Uri host)
		{
			return true;
		}

		// Token: 0x04000303 RID: 771
		private static NullWebProxy instance = new NullWebProxy();
	}
}
