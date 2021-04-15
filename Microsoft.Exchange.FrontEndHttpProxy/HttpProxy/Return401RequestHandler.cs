using System;
using System.Web;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B3 RID: 179
	internal sealed class Return401RequestHandler : IHttpHandler
	{
		// Token: 0x06000702 RID: 1794 RVA: 0x00004B1F File Offset: 0x00002D1F
		internal Return401RequestHandler()
		{
		}

		// Token: 0x1700017A RID: 378
		// (get) Token: 0x06000703 RID: 1795 RVA: 0x00003193 File Offset: 0x00001393
		public bool IsReusable
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06000704 RID: 1796 RVA: 0x0002947D File Offset: 0x0002767D
		public void ProcessRequest(HttpContext context)
		{
			context.Response.StatusCode = 401;
		}
	}
}
