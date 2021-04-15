using System;
using System.Net;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.Diagnostics.WorkloadManagement;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.Security.Authorization;
using Microsoft.Exchange.SharedCache.Client;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000077 RID: 119
	internal static class Extensions
	{
		// Token: 0x060003FA RID: 1018 RVA: 0x000172E0 File Offset: 0x000154E0
		public static int GetTraceContext(this HttpContext httpContext)
		{
			if (httpContext == null)
			{
				throw new ArgumentNullException("httpContext");
			}
			object obj = httpContext.Items[Constants.TraceContextKey];
			if (obj != null)
			{
				return (int)obj;
			}
			return httpContext.GetHashCode();
		}

		// Token: 0x060003FB RID: 1019 RVA: 0x0001731C File Offset: 0x0001551C
		public static RequestDetailsLogger GetLogger(this HttpContext httpContext)
		{
			if (httpContext == null)
			{
				throw new ArgumentNullException();
			}
			return RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpContext);
		}

		// Token: 0x060003FC RID: 1020 RVA: 0x00017330 File Offset: 0x00015530
		public static string GetSerializedAccessTokenString(this IRequestContext requestContext)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			string result = null;
			try
			{
				IIdentity callerIdentity = requestContext.GetCallerIdentity();
				using (ClientSecurityContext clientSecurityContext = IdentityUtils.ClientSecurityContextFromIdentity(callerIdentity, true))
				{
					result = new SerializedAccessToken(IIdentityExtensions.GetSafeName(callerIdentity, true), callerIdentity.AuthenticationType, clientSecurityContext).ToString();
				}
			}
			catch (AuthzException ex)
			{
				throw new HttpException(401, ex.Message);
			}
			return result;
		}

		// Token: 0x060003FD RID: 1021 RVA: 0x000173B4 File Offset: 0x000155B4
		public static SerializedClientSecurityContext GetSerializedClientSecurityContext(this IRequestContext requestContext)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			SerializedClientSecurityContext result = null;
			try
			{
				IIdentity callerIdentity = requestContext.GetCallerIdentity();
				using (ClientSecurityContext clientSecurityContext = IdentityUtils.ClientSecurityContextFromIdentity(callerIdentity, true))
				{
					result = SerializedClientSecurityContext.CreateFromClientSecurityContext(clientSecurityContext, IIdentityExtensions.GetSafeName(callerIdentity, true), callerIdentity.AuthenticationType);
				}
			}
			catch (AuthzException ex)
			{
				throw new HttpException(401, ex.Message);
			}
			return result;
		}

		// Token: 0x060003FE RID: 1022 RVA: 0x00017434 File Offset: 0x00015634
		public static byte[] CreateSerializedSecurityAccessToken(this IRequestContext requestContext)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			SerializedSecurityAccessToken serializedSecurityAccessToken = new SerializedSecurityAccessToken();
			try
			{
				using (ClientSecurityContext clientSecurityContext = IdentityUtils.ClientSecurityContextFromIdentity(requestContext.GetCallerIdentity(), true))
				{
					clientSecurityContext.SetSecurityAccessToken(serializedSecurityAccessToken);
				}
			}
			catch (AuthzException ex)
			{
				throw new HttpException(401, ex.Message);
			}
			return serializedSecurityAccessToken.GetSecurityContextBytes();
		}

		// Token: 0x060003FF RID: 1023 RVA: 0x000174AC File Offset: 0x000156AC
		public static IIdentity GetCallerIdentity(this IRequestContext requestContext)
		{
			IIdentity identity = requestContext.HttpContext.User.Identity;
			if (identity.GetType().Equals(typeof(GenericIdentity)) && string.Equals(identity.AuthenticationType, "LiveIdBasic", StringComparison.OrdinalIgnoreCase))
			{
				identity = LiveIdBasicHelper.GetCallerIdentity(requestContext);
			}
			return identity;
		}

		// Token: 0x06000400 RID: 1024 RVA: 0x000174FC File Offset: 0x000156FC
		public static HttpMethod GetHttpMethod(this HttpRequest request)
		{
			HttpMethod result = HttpMethod.Unknown;
			if (!Enum.TryParse<HttpMethod>(request.HttpMethod, true, out result) && ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "Extensions.GetHttpMethod. HttpMethod unrecognised or has no enum value: {0}", request.HttpMethod);
			}
			return result;
		}

		// Token: 0x06000401 RID: 1025 RVA: 0x00017544 File Offset: 0x00015744
		public static bool IsDownLevelClient(this HttpRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "Extensions.IsDownLevelClient. user-agent = {0}", (request.UserAgent == null) ? string.Empty : request.UserAgent);
			}
			string a;
			UserAgentParser.UserAgentVersion userAgentVersion;
			string a2;
			UserAgentParser.Parse(request.UserAgent, out a, out userAgentVersion, out a2);
			return (!string.Equals(a, "rv:", StringComparison.OrdinalIgnoreCase) || userAgentVersion.Build < 11 || !string.Equals(a2, "Windows NT", StringComparison.OrdinalIgnoreCase)) && (!string.Equals(a, "MSIE", StringComparison.OrdinalIgnoreCase) || userAgentVersion.Build < UserAgent.MSIEBrowserMinPremiumVersion || (!string.Equals(a2, "Windows NT", StringComparison.OrdinalIgnoreCase) && !string.Equals(a2, "Windows 98; Win 9x 4.90", StringComparison.OrdinalIgnoreCase) && !string.Equals(a2, "Windows 2000", StringComparison.OrdinalIgnoreCase))) && (!string.Equals(a, "Safari", StringComparison.OrdinalIgnoreCase) || ((userAgentVersion.Build < 3 || !string.Equals(a2, "Macintosh", StringComparison.OrdinalIgnoreCase)) && (!string.Equals(a2, "Linux", StringComparison.OrdinalIgnoreCase) || request.UserAgent.IndexOf("QtCarBrowser", StringComparison.OrdinalIgnoreCase) == -1))) && (!string.Equals(a, "Firefox", StringComparison.OrdinalIgnoreCase) || ((userAgentVersion.Build < 3 || (!string.Equals(a2, "Windows NT", StringComparison.OrdinalIgnoreCase) && !string.Equals(a2, "Windows 98; Win 9x 4.90", StringComparison.OrdinalIgnoreCase) && !string.Equals(a2, "Windows 2000", StringComparison.OrdinalIgnoreCase) && !string.Equals(a2, "Macintosh", StringComparison.OrdinalIgnoreCase) && !string.Equals(a2, "Linux", StringComparison.OrdinalIgnoreCase))) && (userAgentVersion.Build < 41 || !string.Equals(a2, "Android", StringComparison.OrdinalIgnoreCase) || request.UserAgent.IndexOf("Mobi", StringComparison.OrdinalIgnoreCase) == -1))) && (!string.Equals(a, "Chrome", StringComparison.OrdinalIgnoreCase) || userAgentVersion.Build < 1 || (!string.Equals(a2, "Windows NT", StringComparison.OrdinalIgnoreCase) && !string.Equals(a2, "Macintosh", StringComparison.OrdinalIgnoreCase)));
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x00017735 File Offset: 0x00015935
		public static bool IsAnyWsSecurityRequest(this HttpRequest request)
		{
			return RequestPathParser.IsAnyWsSecurityRequest(request.Url.LocalPath);
		}

		// Token: 0x06000403 RID: 1027 RVA: 0x00017747 File Offset: 0x00015947
		public static bool IsWsSecurityRequest(this HttpRequest request)
		{
			return RequestPathParser.IsWsSecurityRequest(request.Url.LocalPath);
		}

		// Token: 0x06000404 RID: 1028 RVA: 0x00017759 File Offset: 0x00015959
		public static bool IsPartnerAuthRequest(this HttpRequest request)
		{
			return RequestPathParser.IsPartnerAuthRequest(request.Url.LocalPath);
		}

		// Token: 0x06000405 RID: 1029 RVA: 0x0001776B File Offset: 0x0001596B
		public static bool IsX509CertAuthRequest(this HttpRequest request)
		{
			return RequestPathParser.IsX509CertAuthRequest(request.Url.LocalPath);
		}

		// Token: 0x06000406 RID: 1030 RVA: 0x0001777D File Offset: 0x0001597D
		public static bool IsChangePasswordLogoff(this HttpRequest request)
		{
			return request.QueryString["ChgPwd"] == "1";
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x0001779C File Offset: 0x0001599C
		public static bool CanHaveBody(this HttpRequest request)
		{
			HttpMethod httpMethod = request.GetHttpMethod();
			return httpMethod != HttpMethod.Get && httpMethod != HttpMethod.Head;
		}

		// Token: 0x06000408 RID: 1032 RVA: 0x000177C0 File Offset: 0x000159C0
		public static bool IsRequestChunked(this HttpRequest request)
		{
			string text = request.Headers["Transfer-Encoding"];
			return text != null && text.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Token: 0x06000409 RID: 1033 RVA: 0x000177F8 File Offset: 0x000159F8
		public static bool IsChunkedResponse(this HttpWebResponse response)
		{
			string text = response.Headers["Transfer-Encoding"];
			return text != null && text.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x0001782D File Offset: 0x00015A2D
		public static bool HasBody(this HttpRequest request)
		{
			return request.CanHaveBody() && (request.IsRequestChunked() || request.ContentLength > 0);
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x0001784C File Offset: 0x00015A4C
		public static string GetBaseUrl(this HttpRequest httpRequest)
		{
			return new UriBuilder
			{
				Host = httpRequest.Url.Host,
				Port = httpRequest.Url.Port,
				Scheme = httpRequest.Url.Scheme,
				Path = httpRequest.ApplicationPath
			}.Uri.ToString();
		}

		// Token: 0x0600040C RID: 1036 RVA: 0x000178A7 File Offset: 0x00015AA7
		public static string GetTestBackEndUrl(this HttpRequest clientRequest)
		{
			return clientRequest.Headers[Constants.TestBackEndUrlRequestHeaderKey];
		}

		// Token: 0x0600040D RID: 1037 RVA: 0x000178BC File Offset: 0x00015ABC
		public static void LogSharedCacheCall(this IRequestContext requestContext, SharedCacheDiagnostics diagnostics)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			requestContext.LatencyTracker.HandleSharedCacheLatency(diagnostics.Latency);
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(requestContext.Logger, "SharedCache", diagnostics.Message);
			PerfCounters.UpdateMovingAveragePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingAverageSharedCacheLatency, diagnostics.Latency);
		}

		// Token: 0x0600040E RID: 1038 RVA: 0x00017913 File Offset: 0x00015B13
		public static string GetFriendlyName(this OrganizationId organizationId)
		{
			if (organizationId != null && organizationId.OrganizationalUnit != null)
			{
				return organizationId.OrganizationalUnit.Name;
			}
			return null;
		}

		// Token: 0x0600040F RID: 1039 RVA: 0x00017934 File Offset: 0x00015B34
		public static bool TryGetSite(this ServiceTopology serviceTopology, string fqdn, out Site site)
		{
			if (serviceTopology == null)
			{
				throw new ArgumentNullException("serviceTopology");
			}
			if (string.IsNullOrEmpty(fqdn))
			{
				throw new ArgumentNullException("fqdn");
			}
			site = null;
			try
			{
				site = serviceTopology.GetSite(fqdn, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\Extensions.cs", "TryGetSite", 483);
			}
			catch (ServerNotFoundException)
			{
				return false;
			}
			catch (ServerNotInSiteException)
			{
				return false;
			}
			return true;
		}

		// Token: 0x06000410 RID: 1040 RVA: 0x000179A8 File Offset: 0x00015BA8
		internal static void SetActivityScopeOnCurrentThread(this HttpContext httpContext, RequestDetailsLogger logger)
		{
			if (httpContext == null)
			{
				throw new ArgumentNullException();
			}
			if (logger != null)
			{
				ActivityContext.SetThreadScope(logger.ActivityScope);
			}
		}

		// Token: 0x06000411 RID: 1041 RVA: 0x000179C4 File Offset: 0x00015BC4
		internal static void Shuffle<T>(this T[] array, Random random)
		{
			for (int i = array.Length - 1; i > 0; i--)
			{
				int num = random.Next(i + 1);
				T t = array[i];
				array[i] = array[num];
				array[num] = t;
			}
		}
	}
}
