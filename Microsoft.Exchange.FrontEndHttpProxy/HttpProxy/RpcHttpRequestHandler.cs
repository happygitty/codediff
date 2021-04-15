using System;
using System.Web;
using System.Web.Configuration;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B5 RID: 181
	public class RpcHttpRequestHandler : IHttpHandler
	{
		// Token: 0x06000722 RID: 1826 RVA: 0x00004B1F File Offset: 0x00002D1F
		internal RpcHttpRequestHandler()
		{
		}

		// Token: 0x1700017D RID: 381
		// (get) Token: 0x06000723 RID: 1827 RVA: 0x00003193 File Offset: 0x00001393
		public bool IsReusable
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700017E RID: 382
		// (get) Token: 0x06000724 RID: 1828 RVA: 0x0002A008 File Offset: 0x00028208
		private bool AllowDiagnostics
		{
			get
			{
				bool result;
				bool.TryParse(WebConfigurationManager.AppSettings["EnableDiagnostics"], out result);
				return result;
			}
		}

		// Token: 0x06000725 RID: 1829 RVA: 0x0002A02D File Offset: 0x0002822D
		public static bool CanHandleRequest(HttpRequest request)
		{
			return string.IsNullOrEmpty(request.Url.Query) || !RpcHttpRequestHandler.IsRpcProxyRequest(request) || RpcHttpRequestHandler.IsProxyPreAuthenticationRequest(request) || RpcHttpRequestHandler.IsHttpProxyRequest(request);
		}

		// Token: 0x06000726 RID: 1830 RVA: 0x0002A05C File Offset: 0x0002825C
		public void ProcessRequest(HttpContext context)
		{
			if (!context.Request.IsAuthenticated)
			{
				context.Response.StatusCode = 401;
				return;
			}
			if (RpcHttpRequestHandler.IsProxyPreAuthenticationRequest(context.Request) || RpcHttpRequestHandler.IsHttpProxyRequest(context.Request))
			{
				context.Response.StatusCode = 400;
				context.Response.StatusDescription = "Detected request from another HttpProxy";
				return;
			}
			if (RpcHttpRequestHandler.IsRpcProxyRequest(context.Request) && string.IsNullOrEmpty(context.Request.Url.Query))
			{
				context.Response.StatusCode = 200;
				return;
			}
			if (context.Request.Url.AbsolutePath.StartsWith("/rpc/diagnostics/", StringComparison.OrdinalIgnoreCase) && this.AllowDiagnostics)
			{
				this.ProcessDiagnosticsRequest(context);
				return;
			}
			context.Response.StatusCode = 404;
		}

		// Token: 0x06000727 RID: 1831 RVA: 0x0002A134 File Offset: 0x00028334
		private static bool IsProxyPreAuthenticationRequest(HttpRequest request)
		{
			return string.Equals(request.Headers["X-AuthenticateOnly"], "true", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x06000728 RID: 1832 RVA: 0x0002A151 File Offset: 0x00028351
		private static bool IsHttpProxyRequest(HttpRequest request)
		{
			return string.Equals(request.Headers[Constants.XIsFromCafe], Constants.IsFromCafeHeaderValue, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x06000729 RID: 1833 RVA: 0x0002A16E File Offset: 0x0002836E
		private static bool IsRpcProxyRequest(HttpRequest request)
		{
			return string.Equals(request.Url.AbsolutePath, "/rpc/rpcproxy.dll", StringComparison.OrdinalIgnoreCase) || string.Equals(request.Url.AbsolutePath, "/rpcwithcert/rpcproxy.dll", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x0600072A RID: 1834 RVA: 0x0002A1A0 File Offset: 0x000283A0
		private void ProcessDiagnosticsRequest(HttpContext context)
		{
			if (string.Equals(context.Request.Url.AbsolutePath, "/rpc/diagnostics/", StringComparison.OrdinalIgnoreCase))
			{
				context.Response.Output.WriteLine("<HTML><BODY>");
				context.Response.Output.WriteLine("<A HREF=\"proxyrules.txt\">Proxy Rules</A>");
				context.Response.Output.WriteLine("</BODY></HTML>");
				return;
			}
			if (string.Equals(context.Request.Url.AbsolutePath, "/rpc/diagnostics/proxyrules.txt", StringComparison.OrdinalIgnoreCase))
			{
				context.Response.AddHeader("Content-Type", "text/plain");
				context.Response.AddHeader("Cache-Control", "no-cache");
				context.Response.Output.WriteLine(RpcHttpProxyRules.Instance.DiagnosticInfo());
				return;
			}
			context.Response.StatusCode = 404;
		}

		// Token: 0x040003F8 RID: 1016
		private const string AppSettingsEnableDiagnostics = "EnableDiagnostics";

		// Token: 0x040003F9 RID: 1017
		private const string RpcProxyPath = "/rpc/rpcproxy.dll";

		// Token: 0x040003FA RID: 1018
		private const string RpcWithCertProxyPath = "/rpcwithcert/rpcproxy.dll";

		// Token: 0x040003FB RID: 1019
		private const string DiagnosticsPathBase = "/rpc/diagnostics/";
	}
}
