using System;
using System.Net;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
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
		// Token: 0x060003FA RID: 1018 RVA: 0x0001731C File Offset: 0x0001551C
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

		// Token: 0x060003FB RID: 1019 RVA: 0x00017358 File Offset: 0x00015558
		public static RequestDetailsLogger GetLogger(this HttpContext httpContext)
		{
			if (httpContext == null)
			{
				throw new ArgumentNullException();
			}
			return RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpContext);
		}

		// Token: 0x060003FC RID: 1020 RVA: 0x0001736C File Offset: 0x0001556C
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

		// Token: 0x060003FD RID: 1021 RVA: 0x000173F0 File Offset: 0x000155F0
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

		// Token: 0x060003FE RID: 1022 RVA: 0x00017470 File Offset: 0x00015670
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

		// Token: 0x060003FF RID: 1023 RVA: 0x000174E8 File Offset: 0x000156E8
		public static IIdentity GetCallerIdentity(this IRequestContext requestContext)
		{
			IIdentity identity = requestContext.HttpContext.User.Identity;
			if (identity.GetType().Equals(typeof(GenericIdentity)) && string.Equals(identity.AuthenticationType, "LiveIdBasic", StringComparison.OrdinalIgnoreCase))
			{
				identity = LiveIdBasicHelper.GetCallerIdentity(requestContext);
			}
			return identity;
		}

		// Token: 0x06000400 RID: 1024 RVA: 0x00017538 File Offset: 0x00015738
		public static bool HasTokenSerializationRights(this WindowsIdentity identity)
		{
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}
			bool result;
			try
			{
				using (ClientSecurityContext clientSecurityContext = IdentityUtils.ClientSecurityContextFromIdentity(identity, true))
				{
					result = LocalServer.AllowsTokenSerializationBy(clientSecurityContext);
				}
			}
			catch (AuthzException ex)
			{
				throw new HttpException(401, ex.Message);
			}
			return result;
		}

		// Token: 0x06000401 RID: 1025 RVA: 0x000175A0 File Offset: 0x000157A0
		public static bool IsSystemOrTrustedMachineAccount(this WindowsIdentity identity)
		{
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}
			return identity.IsSystem || (identity.Name != null && identity.Name.EndsWith("$", StringComparison.OrdinalIgnoreCase) && identity.HasTokenSerializationRights());
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x000175E0 File Offset: 0x000157E0
		public static HttpMethod GetHttpMethod(this HttpRequest request)
		{
			HttpMethod result = HttpMethod.Unknown;
			if (!Enum.TryParse<HttpMethod>(request.HttpMethod, true, out result) && ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "Extensions.GetHttpMethod. HttpMethod unrecognised or has no enum value: {0}", request.HttpMethod);
			}
			return result;
		}

		// Token: 0x06000403 RID: 1027 RVA: 0x00017628 File Offset: 0x00015828
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

		// Token: 0x06000404 RID: 1028 RVA: 0x00017819 File Offset: 0x00015A19
		public static bool IsAnyWsSecurityRequest(this HttpRequest request)
		{
			return RequestPathParser.IsAnyWsSecurityRequest(request.Url.LocalPath);
		}

		// Token: 0x06000405 RID: 1029 RVA: 0x0001782B File Offset: 0x00015A2B
		public static bool IsWsSecurityRequest(this HttpRequest request)
		{
			return RequestPathParser.IsWsSecurityRequest(request.Url.LocalPath);
		}

		// Token: 0x06000406 RID: 1030 RVA: 0x0001783D File Offset: 0x00015A3D
		public static bool IsPartnerAuthRequest(this HttpRequest request)
		{
			return RequestPathParser.IsPartnerAuthRequest(request.Url.LocalPath);
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x0001784F File Offset: 0x00015A4F
		public static bool IsX509CertAuthRequest(this HttpRequest request)
		{
			return RequestPathParser.IsX509CertAuthRequest(request.Url.LocalPath);
		}

		// Token: 0x06000408 RID: 1032 RVA: 0x00017861 File Offset: 0x00015A61
		public static bool IsChangePasswordLogoff(this HttpRequest request)
		{
			return request.QueryString["ChgPwd"] == "1";
		}

		// Token: 0x06000409 RID: 1033 RVA: 0x00017880 File Offset: 0x00015A80
		public static bool CanHaveBody(this HttpRequest request)
		{
			HttpMethod httpMethod = request.GetHttpMethod();
			return httpMethod != HttpMethod.Get && httpMethod != HttpMethod.Head;
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x000178A4 File Offset: 0x00015AA4
		public static bool IsRequestChunked(this HttpRequest request)
		{
			string text = request.Headers["Transfer-Encoding"];
			return text != null && text.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x000178DC File Offset: 0x00015ADC
		public static bool IsChunkedResponse(this HttpWebResponse response)
		{
			string text = response.Headers["Transfer-Encoding"];
			return text != null && text.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Token: 0x0600040C RID: 1036 RVA: 0x00017911 File Offset: 0x00015B11
		public static bool HasBody(this HttpRequest request)
		{
			return request.CanHaveBody() && (request.IsRequestChunked() || request.ContentLength > 0);
		}

		// Token: 0x0600040D RID: 1037 RVA: 0x00017930 File Offset: 0x00015B30
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

		// Token: 0x0600040E RID: 1038 RVA: 0x0001798B File Offset: 0x00015B8B
		public static string GetTestBackEndUrl(this HttpRequest clientRequest)
		{
			return clientRequest.Headers[Constants.TestBackEndUrlRequestHeaderKey];
		}

		// Token: 0x0600040F RID: 1039 RVA: 0x000179A0 File Offset: 0x00015BA0
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

		// Token: 0x06000410 RID: 1040 RVA: 0x000179F7 File Offset: 0x00015BF7
		public static string GetFriendlyName(this OrganizationId organizationId)
		{
			if (organizationId != null && organizationId.OrganizationalUnit != null)
			{
				return organizationId.OrganizationalUnit.Name;
			}
			return null;
		}

		// Token: 0x06000411 RID: 1041 RVA: 0x00017A18 File Offset: 0x00015C18
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
				site = serviceTopology.GetSite(fqdn, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\Extensions.cs", "TryGetSite", 549);
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

		// Token: 0x06000412 RID: 1042 RVA: 0x00017A8C File Offset: 0x00015C8C
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

		// Token: 0x06000413 RID: 1043 RVA: 0x00017AA8 File Offset: 0x00015CA8
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

		// Token: 0x06000414 RID: 1044 RVA: 0x00017AF0 File Offset: 0x00015CF0
		internal static bool IsSystemOrMachineAccount(this CommonAccessToken token)
		{
			return token.TokenType != null && token.TokenType.Equals(Extensions.windowsAccessTokenType, StringComparison.OrdinalIgnoreCase) && token.WindowsAccessToken != null && token.WindowsAccessToken.LogonName != null && (token.WindowsAccessToken.LogonName.Equals(Extensions.windowsSystemAccountLogon, StringComparison.OrdinalIgnoreCase) || token.WindowsAccessToken.LogonName.Equals(Extensions.windowsCurrentUserName, StringComparison.OrdinalIgnoreCase) || token.WindowsAccessToken.LogonName.EndsWith("$", StringComparison.OrdinalIgnoreCase));
		}

		// Token: 0x040002EF RID: 751
		private static string windowsAccessTokenType = 0.ToString();

		// Token: 0x040002F0 RID: 752
		private static string windowsSystemAccountLogon = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)).Value;

		// Token: 0x040002F1 RID: 753
		private static string windowsCurrentUserName = WindowsIdentity.GetCurrent().Name;
	}
}
