using System;
using System.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000082 RID: 130
	internal class NullWebProxy : IWebProxy
	{
		// Token: 0x170000FC RID: 252
		// (get) Token: 0x0600044A RID: 1098 RVA: 0x000184AB File Offset: 0x000166AB
		public static NullWebProxy Instance
		{
			get
			{
				return NullWebProxy.instance;
			}
		}

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x0600044B RID: 1099 RVA: 0x000184B2 File Offset: 0x000166B2
		// (set) Token: 0x0600044C RID: 1100 RVA: 0x000184BA File Offset: 0x000166BA
		public ICredentials Credentials { get; set; }

		// Token: 0x0600044D RID: 1101 RVA: 0x000184C3 File Offset: 0x000166C3
		public Uri GetProxy(Uri destination)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600044E RID: 1102 RVA: 0x00003193 File Offset: 0x00001393
		public bool IsBypassed(Uri host)
		{
			return true;
		}

		// Token: 0x040002FF RID: 767
		private static NullWebProxy instance = new NullWebProxy();
	}
}
