using System;
using System.Web;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B2 RID: 178
	internal class ReportingWebServiceProxyRequestHandler : RwsPswsProxyRequestHandlerBase<EcpService>
	{
		// Token: 0x17000178 RID: 376
		// (get) Token: 0x06000700 RID: 1792 RVA: 0x0002962E File Offset: 0x0002782E
		protected override string ServiceName
		{
			get
			{
				return "Reporting Web Service";
			}
		}

		// Token: 0x06000701 RID: 1793 RVA: 0x00029635 File Offset: 0x00027835
		public static bool IsReportingWebServicePartnerRequest(HttpRequest request)
		{
			return !string.IsNullOrEmpty(request.Url.LocalPath) && request.Url.LocalPath.IndexOf("ReportingWebService/partner/", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Token: 0x06000702 RID: 1794 RVA: 0x00029668 File Offset: 0x00027868
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

		// Token: 0x040003EC RID: 1004
		private const string ReportingWebServicePartnerPathName = "ReportingWebService/partner/";

		// Token: 0x040003ED RID: 1005
		private const string TenantParameterName = "tenantDomain";
	}
}
