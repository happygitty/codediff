using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.ExchangeSystem;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.Security.Authorization;
using Microsoft.Exchange.Security.OAuth;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000072 RID: 114
	internal static class AspNetHelper
	{
		// Token: 0x060003D3 RID: 979 RVA: 0x00016080 File Offset: 0x00014280
		public static void EndResponse(HttpContext httpContext, HttpStatusCode statusCode)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int>(0L, "[AspNetHelper::EndResponse]: statusCode={0}", (int)statusCode);
			}
			if (httpContext == null)
			{
				throw new ArgumentNullException("httpContext");
			}
			httpContext.Response.StatusCode = (int)statusCode;
			try
			{
				httpContext.Response.Flush();
				httpContext.ApplicationInstance.CompleteRequest();
			}
			catch (HttpException ex)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<HttpException>(0L, "Failed to flush and send response to client. {0}", ex);
				}
			}
			httpContext.Response.End();
		}

		// Token: 0x060003D4 RID: 980 RVA: 0x0001611C File Offset: 0x0001431C
		public static CommonAccessToken FixupCommonAccessToken(HttpContext httpContext, int targetVersion)
		{
			if (!httpContext.Request.IsAuthenticated)
			{
				return null;
			}
			CommonAccessToken commonAccessToken = null;
			try
			{
				if (httpContext.User.Identity is OAuthIdentity)
				{
					commonAccessToken = (httpContext.User.Identity as OAuthIdentity).ToCommonAccessToken(targetVersion);
				}
				else if (httpContext.User is DelegatedPrincipal)
				{
					commonAccessToken = new CommonAccessToken(8);
					commonAccessToken.ExtensionData["DelegatedData"] = IIdentityExtensions.GetSafeName(((DelegatedPrincipal)httpContext.User).Identity, true);
				}
				else
				{
					CommonAccessToken commonAccessToken2 = httpContext.Items["Item-CommonAccessToken"] as CommonAccessToken;
					if (commonAccessToken2 != null)
					{
						return commonAccessToken2;
					}
					if (httpContext.User.Identity is WindowsIdentity)
					{
						WindowsIdentity windowsIdentity = httpContext.User.Identity as WindowsIdentity;
						string value;
						if (HttpContextItemParser.TryGetLiveIdMemberName(httpContext.Items, ref value))
						{
							commonAccessToken = new CommonAccessToken(3);
							commonAccessToken.ExtensionData["UserSid"] = windowsIdentity.User.ToString();
							commonAccessToken.ExtensionData["MemberName"] = value;
						}
						else
						{
							commonAccessToken = new CommonAccessToken(windowsIdentity);
						}
					}
				}
			}
			catch (CommonAccessTokenException ex)
			{
				if (ExTraceGlobals.BriefTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.BriefTracer.TraceError<string, CommonAccessTokenException>(0L, "[AspNetHelper::FixupCommonAccessToken] Error encountered when creating CommonAccessToken from current logong identity. User: {0} Exception: {1}.", IIdentityExtensions.GetSafeName(httpContext.User.Identity, true), ex);
				}
				throw new HttpProxyException(HttpStatusCode.Unauthorized, 3002, string.Format("Error encountered when creating common access token. User: {0}", IIdentityExtensions.GetSafeName(httpContext.User.Identity, true)));
			}
			return commonAccessToken;
		}

		// Token: 0x060003D5 RID: 981 RVA: 0x000162AC File Offset: 0x000144AC
		public static bool IsExceptionExpectedWhenDisconnected(Exception e)
		{
			if (e is IOException)
			{
				return true;
			}
			HttpException ex = e as HttpException;
			if (ex == null)
			{
				return false;
			}
			int errorCode = ex.ErrorCode;
			int num = 0;
			if (ex.InnerException != null && ex.InnerException is COMException)
			{
				num = ((COMException)ex.InnerException).ErrorCode;
			}
			return errorCode == -2147023667 || num == -2147023667 || errorCode == -2147023901 || num == -2147023901 || errorCode == -2147024832 || num == -2147024832 || errorCode == -2147024890 || num == -2147024890 || errorCode == -2147024809 || num == -2147024809 || errorCode == -2147024874 || num == -2147024874 || errorCode == -2147024895 || num == -2147024895;
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x00016370 File Offset: 0x00014570
		public static string GetCafeErrorPageRedirectUrl(HttpContext httpContext, NameValueCollection queryParams)
		{
			if (httpContext == null)
			{
				throw new ArgumentNullException("httpContext");
			}
			UriBuilder uriBuilder = new UriBuilder(httpContext.Request.Url.Scheme, httpContext.Request.Url.Host, httpContext.Request.Url.Port, OwaUrl.CafeErrorPage.GetExplicitUrl(httpContext.Request));
			if (queryParams != null && queryParams.Count != 0)
			{
				NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uriBuilder.Query);
				foreach (object obj in queryParams)
				{
					string name = (string)obj;
					nameValueCollection[name] = queryParams[name];
				}
				uriBuilder.Query = nameValueCollection.ToString();
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[AspNetHelper.GetErrorPageRedirectUrl] Redirection url: {0}", uriBuilder.Uri.AbsoluteUri);
			}
			return uriBuilder.Uri.AbsoluteUri;
		}

		// Token: 0x060003D7 RID: 983 RVA: 0x0001647C File Offset: 0x0001467C
		public static void MakePageNoCacheNoStore(HttpResponse response)
		{
			response.Cache.SetCacheability(HttpCacheability.NoCache);
			response.Cache.SetNoStore();
		}

		// Token: 0x060003D8 RID: 984 RVA: 0x00016498 File Offset: 0x00014698
		public static void SetCacheability(HttpResponse response, string cacheControlHeaderValue)
		{
			string[] separator = new string[]
			{
				"\r\n",
				" ",
				"\t",
				","
			};
			foreach (string text in cacheControlHeaderValue.Split(separator, StringSplitOptions.RemoveEmptyEntries))
			{
				if (text.Equals("private", StringComparison.OrdinalIgnoreCase))
				{
					response.Cache.SetCacheability(HttpCacheability.Private);
				}
				else if (text.Equals("public", StringComparison.OrdinalIgnoreCase))
				{
					response.Cache.SetCacheability(HttpCacheability.Public);
				}
				else if (text.Equals("no-cache", StringComparison.OrdinalIgnoreCase))
				{
					response.Cache.SetCacheability(HttpCacheability.NoCache);
				}
				else if (text.Equals("no-store", StringComparison.OrdinalIgnoreCase))
				{
					response.Cache.SetNoStore();
				}
				else if (text.StartsWith("max-age=", StringComparison.OrdinalIgnoreCase))
				{
					uint seconds = 0U;
					if (text.Length > "max-age=".Length && uint.TryParse(text.Substring("max-age=".Length), out seconds))
					{
						response.Cache.SetMaxAge(new TimeSpan(0, 0, (int)seconds));
					}
					else if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[AspNetHelper::SetCacheability] Cannot parse max-age token {0}", text);
					}
				}
				else if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "[AspNetHelper::SetCacheability] Unknown Cache-Control token {0}", text);
				}
			}
		}

		// Token: 0x060003D9 RID: 985 RVA: 0x000165FC File Offset: 0x000147FC
		public static void AddTimestampHeaderIfNecessary(NameValueCollection headers, string headerName)
		{
			try
			{
				if (headers["X-OWA-CorrelationId"] != null)
				{
					headers[headerName] = ExDateTime.Now.ToString(Constants.ISO8601DateTimeMsPattern);
				}
			}
			catch (HttpException)
			{
			}
		}

		// Token: 0x060003DA RID: 986 RVA: 0x00016644 File Offset: 0x00014844
		public static string GetRequestCorrelationId(HttpContext httpContext)
		{
			ExAssert.RetailAssert(httpContext != null, "httpContext is null");
			string text = httpContext.Request.Headers["X-OWA-CorrelationId"];
			if (string.IsNullOrEmpty(text))
			{
				text = "<empty>";
			}
			return text;
		}

		// Token: 0x060003DB RID: 987 RVA: 0x00016684 File Offset: 0x00014884
		public static void TerminateRequestWithSslRequiredResponse(HttpApplication httpApplication)
		{
			HttpResponse response = httpApplication.Context.Response;
			response.Clear();
			response.StatusCode = 403;
			response.SubStatusCode = 4;
			response.StatusDescription = "SSL required.";
			httpApplication.CompleteRequest();
		}
	}
}
