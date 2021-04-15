using System;
using System.Web;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B2 RID: 178
	internal class ReportingWebServiceProxyRequestHandler : RwsPswsProxyRequestHandlerBase<EcpService>
	{
		// Token: 0x17000179 RID: 377
		// (get) Token: 0x060006FE RID: 1790 RVA: 0x000293A2 File Offset: 0x000275A2
		protected override string ServiceName
		{
			get
			{
				return "Reporting Web Service";
			}
		}

		// Token: 0x060006FF RID: 1791 RVA: 0x000293A9 File Offset: 0x000275A9
		public static bool IsReportingWebServicePartnerRequest(HttpRequest request)
		{
			return !string.IsNullOrEmpty(request.Url.LocalPath) && request.Url.LocalPath.IndexOf("ReportingWebService/partner/", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Token: 0x06000700 RID: 1792 RVA: 0x000293DC File Offset: 0x000275DC
		protected override void DoProtocolSpecificBeginProcess()
		{
			base.DoProtocolSpecificBeginProcess();
			if (!ReportingWebServiceProxyRequestHandler.IsReportingWebServicePartnerRequest(base.HttpContext.Request))
			{
				string domain;
				if (base.TryGetTenantDomain("DelegatedOrg", out domain))
				{
					base.IsDomainBasedRequest = true;
					base.Domain = domain;
				}
				return;
			}
			string domain2;
			if (base.TryGetTenantDomain("tenantDomain", out domain2))
			{
				base.IsDomainBasedRequest = true;
				base.Domain = domain2;
				return;
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
			{
				ExTraceGlobals.VerboseTracer.TraceError<int>((long)this.GetHashCode(), "[ReportingWebServiceProxyRequestHandler::DoProtocolSpecificBeginProcess]: Context {0}; TenantDomain parameter isn't specified in the request URL.", base.TraceContext);
			}
			throw new HttpException(400, "TenantDomain parameter isn't specified in the request URL.");
		}

		// Token: 0x040003E8 RID: 1000
		private const string ReportingWebServicePartnerPathName = "ReportingWebService/partner/";

		// Token: 0x040003E9 RID: 1001
		private const string TenantParameterName = "tenantDomain";
	}
}
