using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using Microsoft.Exchange.Configuration.Core;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.Configuration.Core;
using Microsoft.Exchange.Security.Authentication;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000B7 RID: 183
	internal class RpsHttpProxyModule : ProxyModule
	{
		// Token: 0x06000730 RID: 1840 RVA: 0x0002A4EC File Offset: 0x000286EC
		static RpsHttpProxyModule()
		{
			string path;
			try
			{
				path = Path.Combine(ExchangeSetupContext.InstallPath, "Logging\\HttpProxy");
			}
			catch (SetupVersionInformationCorruptException)
			{
				path = "C:\\Program Files\\Microsoft\\Exchange Server\\V15";
			}
			RequestMonitor.InitRequestMonitor(Path.Combine(path, HttpProxyGlobals.ProtocolType.ToString(), "RequestMonitor"), 300000);
		}

		// Token: 0x06000731 RID: 1841 RVA: 0x0002A54C File Offset: 0x0002874C
		protected override void OnBeginRequestInternal(HttpApplication httpApplication)
		{
			if (ExTraceGlobals.HttpModuleTracer.IsTraceEnabled(7))
			{
				ExTraceGlobals.HttpModuleTracer.TraceFunction((long)this.GetHashCode(), "[RpsHttpProxyModule::OnBeginRequestInternal] Enter");
			}
			HttpContext context = httpApplication.Context;
			RequestDetailsLogger current = RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(context);
			if (current != null)
			{
				RequestMonitor.Instance.RegisterRequest(current.ActivityId);
				string text = context.Request.Headers["Authorization"];
				byte[] bytes;
				byte[] array;
				string text2;
				if (!string.IsNullOrEmpty(text) && LiveIdBasicAuthModule.ParseCredentials(context, text, false, ref bytes, ref array, ref text2))
				{
					string text3 = Encoding.UTF8.GetString(bytes).Trim();
					SmtpAddress smtpAddress;
					smtpAddress..ctor(text3);
					RequestMonitor.Instance.Log(current.ActivityId, 3, text3);
					RequestMonitor.Instance.Log(current.ActivityId, 4, smtpAddress.Domain);
					context.Items[Constants.WLIDMemberName] = text3;
					if (ExTraceGlobals.HttpModuleTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.HttpModuleTracer.TraceDebug<string>((long)this.GetHashCode(), "[RpsHttpProxyModule::OnBeginRequestInternal] LiveIdMember={0}", text3);
					}
				}
			}
			base.OnBeginRequestInternal(httpApplication);
			if (ExTraceGlobals.HttpModuleTracer.IsTraceEnabled(7))
			{
				ExTraceGlobals.HttpModuleTracer.TraceFunction((long)this.GetHashCode(), "[RpsHttpProxyModule::OnBeginRequestInternal] Exit");
			}
		}

		// Token: 0x06000732 RID: 1842 RVA: 0x0002A67C File Offset: 0x0002887C
		protected override void OnEndRequestInternal(HttpApplication httpApplication)
		{
			if (ExTraceGlobals.HttpModuleTracer.IsTraceEnabled(7))
			{
				ExTraceGlobals.HttpModuleTracer.TraceFunction((long)this.GetHashCode(), "[RpsHttpProxyModule::OnEndRequestInternal] Enter");
			}
			RequestDetailsLogger current = RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpApplication.Context);
			if (current != null)
			{
				RequestMonitor.Instance.UnRegisterRequest(current.ActivityId);
			}
			base.OnEndRequestInternal(httpApplication);
			if (ExTraceGlobals.HttpModuleTracer.IsTraceEnabled(7))
			{
				ExTraceGlobals.HttpModuleTracer.TraceFunction((long)this.GetHashCode(), "[RpsHttpProxyModule::OnEndRequestInternal] Exit");
			}
		}

		// Token: 0x06000733 RID: 1843 RVA: 0x0002A6F8 File Offset: 0x000288F8
		protected override void OnPostAuthorizeInternal(HttpApplication httpApplication)
		{
			if (ExTraceGlobals.HttpModuleTracer.IsTraceEnabled(7))
			{
				ExTraceGlobals.HttpModuleTracer.TraceFunction((long)this.GetHashCode(), "[RpsHttpProxyModule::OnPostAuthorizeInternal] Enter");
			}
			RequestDetailsLogger current = RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpApplication.Context);
			if (current != null)
			{
				Dictionary<Enum, string> authValues = ServiceCommonMetadataPublisher.GetAuthValues(new HttpContextWrapper(httpApplication.Context), false);
				if (authValues.ContainsKey(8))
				{
					RequestMonitor.Instance.Log(current.ActivityId, 3, authValues[8]);
				}
				if (authValues.ContainsKey(5))
				{
					RequestMonitor.Instance.Log(current.ActivityId, 4, authValues[5]);
				}
			}
			base.OnPostAuthorizeInternal(httpApplication);
			if (ExTraceGlobals.HttpModuleTracer.IsTraceEnabled(7))
			{
				ExTraceGlobals.HttpModuleTracer.TraceFunction((long)this.GetHashCode(), "[RpsHttpProxyModule::OnPostAuthorizeInternal] Exit");
			}
		}
	}
}
