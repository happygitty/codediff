using System;
using System.Net;
using System.Web;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.Security.Authorization;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000040 RID: 64
	internal abstract class ProtocolPingStrategyBase
	{
		// Token: 0x0600020A RID: 522 RVA: 0x0000A459 File Offset: 0x00008659
		public virtual Uri BuildUrl(string fqdn)
		{
			if (string.IsNullOrEmpty(fqdn))
			{
				throw new ArgumentNullException("fqdn");
			}
			return new UriBuilder
			{
				Scheme = "https:",
				Host = fqdn,
				Path = HttpRuntime.AppDomainAppVirtualPath
			}.Uri;
		}

		// Token: 0x0600020B RID: 523 RVA: 0x0000A498 File Offset: 0x00008698
		public Exception Ping(Uri url)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<Uri>((long)this.GetHashCode(), "[ProtocolPingStrategyBase::Ctor]: Testing server with URL {0}.", url);
			}
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.ServicePoint.ConnectionLimit = HttpProxySettings.ServicePointConnectionLimit.Value;
			httpWebRequest.Method = "HEAD";
			httpWebRequest.Timeout = ProtocolPingStrategyBase.DownLevelServerPingTimeout.Value;
			httpWebRequest.PreAuthenticate = true;
			httpWebRequest.UserAgent = "HttpProxy.ClientAccessServer2010Ping";
			httpWebRequest.KeepAlive = false;
			if (!HttpProxySettings.UseDefaultWebProxy.Value)
			{
				httpWebRequest.Proxy = NullWebProxy.Instance;
			}
			httpWebRequest.ServerCertificateValidationCallback = ProxyApplication.RemoteCertificateValidationCallback;
			CertificateValidationManager.SetComponentId(httpWebRequest, Constants.CertificateValidationComponentId);
			this.PrepareRequest(httpWebRequest);
			try
			{
				using (httpWebRequest.GetResponse())
				{
				}
			}
			catch (WebException ex)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(2))
				{
					ExTraceGlobals.VerboseTracer.TraceWarning<WebException>((long)this.GetHashCode(), "[ProtocolPingStrategyBase::TestServer]: Web exception: {0}.", ex);
				}
				if (!this.IsWebExceptionExpected(ex))
				{
					return ex;
				}
			}
			catch (Exception ex2)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<Exception>((long)this.GetHashCode(), "[ProtocolPingStrategyBase::TestServer]: General exception {0}.", ex2);
				}
				return ex2;
			}
			finally
			{
				try
				{
					httpWebRequest.Abort();
				}
				catch
				{
				}
			}
			return null;
		}

		// Token: 0x0600020C RID: 524 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void PrepareRequest(HttpWebRequest request)
		{
		}

		// Token: 0x0600020D RID: 525 RVA: 0x0000A62C File Offset: 0x0000882C
		protected virtual bool IsWebExceptionExpected(WebException exception)
		{
			return HttpWebHelper.CheckConnectivityError(exception) == HttpWebHelper.ConnectivityError.None;
		}

		// Token: 0x04000124 RID: 292
		private static readonly IntAppSettingsEntry DownLevelServerPingTimeout = new IntAppSettingsEntry(HttpProxySettings.Prefix("DownLevelServerPingTimeout"), 5000, ExTraceGlobals.VerboseTracer);
	}
}
