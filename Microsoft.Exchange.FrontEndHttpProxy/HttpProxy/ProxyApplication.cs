using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration.ConfigurationSettings;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.HttpProxy.EventLogs;
using Microsoft.Exchange.Security.Authorization;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000AC RID: 172
	public class ProxyApplication : HttpApplication
	{
		// Token: 0x17000141 RID: 321
		// (get) Token: 0x060005E5 RID: 1509 RVA: 0x00020F88 File Offset: 0x0001F188
		public static string ApplicationVersion
		{
			get
			{
				return HttpProxyGlobals.ApplicationVersion;
			}
		}

		// Token: 0x060005E6 RID: 1510 RVA: 0x00020F8F File Offset: 0x0001F18F
		private static void ConfigureServicePointManager()
		{
			ServicePointManager.DefaultConnectionLimit = HttpProxySettings.ServicePointConnectionLimit.Value;
			ServicePointManager.UseNagleAlgorithm = false;
			ProxyApplication.ConfigureSecureProtocols();
		}

		// Token: 0x060005E7 RID: 1511 RVA: 0x00020FAC File Offset: 0x0001F1AC
		private static void ConfigureSecureProtocols()
		{
			if (CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).EnableTls11.Enabled)
			{
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11;
			}
			if (CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).EnableTls12.Enabled)
			{
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			}
		}

		// Token: 0x060005E8 RID: 1512 RVA: 0x0002100C File Offset: 0x0001F20C
		private void Application_Start(object sender, EventArgs e)
		{
			Diagnostics.InitializeWatsonReporting();
			if (Globals.InstanceType == null)
			{
				string text = HttpProxyGlobals.ProtocolType.ToString();
				text = "FE_" + text;
				Globals.InitializeMultiPerfCounterInstance(text);
			}
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				ProcessAccessManager.RegisterComponent(SettingOverrideSync.Instance);
				CertificateValidationManager.RegisterCallback(Constants.CertificateValidationComponentId, ProxyApplication.RemoteCertificateValidationCallback);
				ProxyApplication.ConfigureServicePointManager();
				if (DownLevelServerManager.IsApplicable)
				{
					DownLevelServerManager.Instance.Initialize();
				}
			});
			PerfCounters.UpdateHttpProxyPerArrayCounters();
			Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_ApplicationStart, null, new object[]
			{
				HttpProxyGlobals.ProtocolType
			});
		}

		// Token: 0x060005E9 RID: 1513 RVA: 0x00021098 File Offset: 0x0001F298
		private void Application_End(object sender, EventArgs e)
		{
			Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_ApplicationShutdown, null, new object[]
			{
				HttpProxyGlobals.ProtocolType
			});
			RequestDetailsLogger.FlushQueuedFileWrites();
			ProcessAccessManager.UnregisterComponent(SettingOverrideSync.Instance);
		}

		// Token: 0x060005EA RID: 1514 RVA: 0x000210CD File Offset: 0x0001F2CD
		private void Application_Error(object sender, EventArgs e)
		{
			Diagnostics.ReportException(((HttpApplication)sender).Server.GetLastError(), FrontEndHttpProxyEventLogConstants.Tuple_InternalServerError, null, "Exception from Application_Error event: {0}");
		}

		// Token: 0x0400039F RID: 927
		internal static readonly RemoteCertificateValidationCallback RemoteCertificateValidationCallback = (object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) => HttpProxyRegistry.OwaAllowInternalUntrustedCerts.Member || errors == SslPolicyErrors.None;
	}
}
