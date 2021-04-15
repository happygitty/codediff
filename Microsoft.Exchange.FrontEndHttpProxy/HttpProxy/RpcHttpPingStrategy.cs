using System;
using System.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000041 RID: 65
	internal class RpcHttpPingStrategy : ProtocolPingStrategyBase
	{
		// Token: 0x06000210 RID: 528 RVA: 0x0000A657 File Offset: 0x00008857
		public override Uri BuildUrl(string fqdn)
		{
			if (string.IsNullOrEmpty(fqdn))
			{
				throw new ArgumentNullException("fqdn");
			}
			return new UriBuilder
			{
				Scheme = Uri.UriSchemeHttps,
				Host = fqdn,
				Path = "rpc/rpcproxy.dll"
			}.Uri;
		}

		// Token: 0x06000211 RID: 529 RVA: 0x0000A693 File Offset: 0x00008893
		protected override void PrepareRequest(HttpWebRequest request)
		{
			base.PrepareRequest(request);
			request.Method = "RPC_IN_DATA";
		}

		// Token: 0x06000212 RID: 530 RVA: 0x0000A6A8 File Offset: 0x000088A8
		protected override bool IsWebExceptionExpected(WebException exception)
		{
			HttpWebResponse httpWebResponse = exception.Response as HttpWebResponse;
			return httpWebResponse != null && httpWebResponse.StatusCode == HttpStatusCode.Unauthorized;
		}
	}
}
