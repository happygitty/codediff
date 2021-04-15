using System;
using System.Net;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B8 RID: 184
	internal abstract class RwsPswsProxyRequestHandlerBase<ServiceType> : BEServerCookieProxyRequestHandler<ServiceType> where ServiceType : HttpService
	{
		// Token: 0x17000180 RID: 384
		// (get) Token: 0x06000733 RID: 1843
		protected abstract string ServiceName { get; }

		// Token: 0x17000181 RID: 385
		// (get) Token: 0x06000734 RID: 1844 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x06000735 RID: 1845 RVA: 0x0002A540 File Offset: 0x00028740
		protected override Uri GetTargetBackEndServerUrl()
		{
			Uri targetBackEndServerUrl = base.GetTargetBackEndServerUrl();
			if (base.AnchoredRoutingTarget.BackEndServer.Version < Server.E15MinVersion)
			{
				string text = Utilities.FormatServerVersion(base.AnchoredRoutingTarget.BackEndServer.Version);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string, AnchoredRoutingTarget, string>((long)this.GetHashCode(), "[RwsPswsProxyRequestHandlerBase::GetTargetBackEndServerUrl]: Backend server doesn't support {0}. Backend server version: {1}; AnchoredRoutingTarget: {2}", text, base.AnchoredRoutingTarget, this.ServiceName);
				}
				string message = string.Format("The target site (version {0}) doesn't support {1}.", text, this.ServiceName);
				throw new HttpProxyException(HttpStatusCode.NotFound, 3001, message);
			}
			return targetBackEndServerUrl;
		}

		// Token: 0x06000736 RID: 1846 RVA: 0x0002A5D4 File Offset: 0x000287D4
		protected bool TryGetTenantDomain(string parameterName, out string tenantDomain)
		{
			tenantDomain = base.HttpContext.Request.QueryString[parameterName];
			if (string.IsNullOrEmpty(tenantDomain))
			{
				return false;
			}
			if (!SmtpAddress.IsValidDomain(tenantDomain))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<int, string, string>((long)this.GetHashCode(), "[RwsPswsProxyRequestHandlerBase::TryGetTenantDomain]: Context {0}; TenantDomain parameter is invalid. ParameterName: {1}; Value: {2}.", base.TraceContext, parameterName, tenantDomain);
				}
				string message = string.Format("{0} parameter is invalid.", parameterName);
				throw new HttpException(400, message);
			}
			return true;
		}
	}
}
