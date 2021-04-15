using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000058 RID: 88
	internal class PfdTracer
	{
		// Token: 0x060002C4 RID: 708 RVA: 0x0000DE30 File Offset: 0x0000C030
		static PfdTracer()
		{
			PfdTracer.NotInterestedCookies.Add("ASP.NET_SessionId");
			PfdTracer.NotInterestedCookies.Add("cadata");
			PfdTracer.NotInterestedCookies.Add("cadataIV");
			PfdTracer.NotInterestedCookies.Add("cadataKey");
			PfdTracer.NotInterestedCookies.Add("cadataSig");
			PfdTracer.NotInterestedCookies.Add("cadataTTL");
			PfdTracer.NotInterestedCookies.Add("PBack");
			PfdTracer.NotInterestedHeaders.Add("Accept");
			PfdTracer.NotInterestedHeaders.Add("Accept-Encoding");
			PfdTracer.NotInterestedHeaders.Add("Accept-Language");
			PfdTracer.NotInterestedHeaders.Add("Connection");
			PfdTracer.NotInterestedHeaders.Add("Content-Length");
			PfdTracer.NotInterestedHeaders.Add("Content-Type");
			PfdTracer.NotInterestedHeaders.Add("Cookie");
			PfdTracer.NotInterestedHeaders.Add("Expect");
			PfdTracer.NotInterestedHeaders.Add("Host");
			PfdTracer.NotInterestedHeaders.Add("If-Modified-Since");
			PfdTracer.NotInterestedHeaders.Add("Proxy-Connection");
			PfdTracer.NotInterestedHeaders.Add("Range");
			PfdTracer.NotInterestedHeaders.Add("Referer");
			PfdTracer.NotInterestedHeaders.Add("Transfer-Encoding");
			PfdTracer.NotInterestedHeaders.Add("User-Agent");
			PfdTracer.NotInterestedHeaders.Add("Accept-Ranges");
			PfdTracer.NotInterestedHeaders.Add("Cache-Control");
			PfdTracer.NotInterestedHeaders.Add("ETag");
			PfdTracer.NotInterestedHeaders.Add("Last-Modified");
			PfdTracer.NotInterestedHeaders.Add("Server");
			PfdTracer.NotInterestedHeaders.Add("X-AspNet-Version");
			PfdTracer.NotInterestedHeaders.Add("X-Powered-By");
			PfdTracer.NotInterestedHeaders.Add("X-UA-Compatible");
			if (PfdTracer.PfdTraceToFile.Value)
			{
				PfdTracer.traceDirectory = Path.Combine(ExchangeSetupContext.InstallPath, "Logging\\HttpProxy");
			}
		}

		// Token: 0x060002C5 RID: 709 RVA: 0x0000E09C File Offset: 0x0000C29C
		public PfdTracer(int traceContext, int hashCode)
		{
			this.traceContext = traceContext;
			this.hashCode = hashCode;
			string text = HttpRuntime.AppDomainAppVirtualPath;
			if (string.IsNullOrEmpty(text))
			{
				text = "unknown";
			}
			else
			{
				text = text.Replace("\\", string.Empty).Replace("/", string.Empty);
			}
			if (!string.IsNullOrEmpty(text))
			{
				this.vdir = text;
			}
		}

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x060002C6 RID: 710 RVA: 0x0000E102 File Offset: 0x0000C302
		private static bool IsTraceDisabled
		{
			get
			{
				return !PfdTracer.PfdTraceToDebugger.Value && !PfdTracer.PfdTraceToFile.Value && (!PfdTracer.traceToEtl || !ExTraceGlobals.BriefTracer.IsTraceEnabled(8));
			}
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x060002C7 RID: 711 RVA: 0x0000E135 File Offset: 0x0000C335
		private string TraceFilePath
		{
			get
			{
				if (this.traceFilePath == null)
				{
					this.traceFilePath = Path.Combine(PfdTracer.traceDirectory, "trace-" + this.vdir + ".log");
				}
				return this.traceFilePath;
			}
		}

		// Token: 0x060002C8 RID: 712 RVA: 0x0000E16C File Offset: 0x0000C36C
		public void TraceRequest(string stage, HttpRequest request)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			string s = string.Format("{0}: {1}: {2} {3}", new object[]
			{
				this.traceContext,
				stage,
				request.HttpMethod,
				request.Url.ToString()
			});
			this.Write(s);
		}

		// Token: 0x060002C9 RID: 713 RVA: 0x0000E1C4 File Offset: 0x0000C3C4
		public void TraceRequest(string stage, HttpWebRequest request)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			string s = string.Format("{0}: {1}: {2} {3}", new object[]
			{
				this.traceContext,
				stage,
				request.Method,
				request.RequestUri.ToString()
			});
			this.Write(s);
		}

		// Token: 0x060002CA RID: 714 RVA: 0x0000E21C File Offset: 0x0000C41C
		public void TraceResponse(string stage, HttpResponse response)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			int statusCode = response.StatusCode;
			string s;
			if (statusCode == 301 || statusCode == 302 || statusCode == 303 || statusCode == 305 || statusCode == 307)
			{
				string text = response.Headers["Location"];
				s = string.Format("{0}: {1}: redirected {2} to {3}", new object[]
				{
					this.traceContext,
					stage,
					response.StatusCode,
					text ?? "null"
				});
			}
			else
			{
				s = string.Format("{0}: {1}: responds {2} {3}", new object[]
				{
					this.traceContext,
					stage,
					response.StatusCode,
					response.StatusDescription
				});
			}
			this.Write(s);
		}

		// Token: 0x060002CB RID: 715 RVA: 0x0000E2F4 File Offset: 0x0000C4F4
		public void TraceResponse(string stage, HttpWebResponse response)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			int statusCode = (int)response.StatusCode;
			string s;
			if (statusCode == 301 || statusCode == 302 || statusCode == 303 || statusCode == 305 || statusCode == 307)
			{
				s = string.Format("{0}: {1}: {2} redirected {3} to {4}", new object[]
				{
					this.traceContext,
					stage,
					response.Server,
					response.StatusCode,
					response.GetResponseHeader("Location")
				});
			}
			else
			{
				s = string.Format("{0}: {1}: {2} responds {3} {4}", new object[]
				{
					this.traceContext,
					stage,
					response.Server,
					response.StatusCode,
					response.StatusDescription
				});
			}
			this.Write(s);
		}

		// Token: 0x060002CC RID: 716 RVA: 0x0000E3CC File Offset: 0x0000C5CC
		public void TraceProxyTarget(AnchoredRoutingTarget anchor)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			string s = string.Format("{0}: {1}: {2}", this.traceContext, "AnchoredRoutingTarget", anchor.ToString());
			this.Write(s);
		}

		// Token: 0x060002CD RID: 717 RVA: 0x0000E40C File Offset: 0x0000C60C
		public void TraceProxyTarget(string key, string fqdn)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			string s = string.Format("{0}: {1}: select BE server {2} based on {3}", new object[]
			{
				this.traceContext,
				"Cookie",
				fqdn,
				key
			});
			this.Write(s);
		}

		// Token: 0x060002CE RID: 718 RVA: 0x0000E458 File Offset: 0x0000C658
		public void TraceRedirect(string stage, string url)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			string s = string.Format("{0}: {1}: force redirect to {2}", this.traceContext, stage, url);
			this.Write(s);
		}

		// Token: 0x060002CF RID: 719 RVA: 0x0000E48C File Offset: 0x0000C68C
		public void TraceHeaders(string stage, WebHeaderCollection originalHeaders, WebHeaderCollection newHeaders)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			if (originalHeaders == null || newHeaders == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(20 * originalHeaders.Count);
			stringBuilder.Append(string.Format("{0}: {1}: ", this.traceContext, stage));
			PfdTracer.TraceDiffs(originalHeaders, newHeaders, PfdTracer.NotInterestedHeaders, stringBuilder);
			this.Write(stringBuilder.ToString());
		}

		// Token: 0x060002D0 RID: 720 RVA: 0x0000E4F0 File Offset: 0x0000C6F0
		public void TraceHeaders(string stage, NameValueCollection originalHeaders, NameValueCollection newHeaders)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			if (originalHeaders == null || newHeaders == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(20 * originalHeaders.Count);
			stringBuilder.Append(string.Format("{0}: {1}: ", this.traceContext, stage));
			PfdTracer.TraceDiffs(originalHeaders, newHeaders, PfdTracer.NotInterestedHeaders, stringBuilder);
			this.Write(stringBuilder.ToString());
		}

		// Token: 0x060002D1 RID: 721 RVA: 0x0000E554 File Offset: 0x0000C754
		public void TraceCookies(string stage, HttpCookieCollection originalCookies, CookieContainer newCookies)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			if (originalCookies == null || newCookies == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(20 * originalCookies.Count);
			stringBuilder.Append(string.Format("{0}: {1}: ", this.traceContext, stage));
			PfdTracer.TraceDiffs(originalCookies, PfdTracer.CopyCookies(newCookies), PfdTracer.NotInterestedCookies, stringBuilder);
			this.Write(stringBuilder.ToString());
		}

		// Token: 0x060002D2 RID: 722 RVA: 0x0000E5BC File Offset: 0x0000C7BC
		public void TraceCookies(string stage, CookieCollection originalCookies, HttpCookieCollection newCookies)
		{
			if (PfdTracer.IsTraceDisabled)
			{
				return;
			}
			if (originalCookies == null || newCookies == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(20 * originalCookies.Count);
			stringBuilder.Append(string.Format("{0}: {1}: ", this.traceContext, stage));
			PfdTracer.TraceDiffs(PfdTracer.CopyCookies(originalCookies), newCookies, PfdTracer.NotInterestedCookies, stringBuilder);
			this.Write(stringBuilder.ToString());
		}

		// Token: 0x060002D3 RID: 723 RVA: 0x0000E624 File Offset: 0x0000C824
		private static NameValueCollection CopyCookies(CookieCollection cookies)
		{
			NameValueCollection nameValueCollection = new NameValueCollection(cookies.Count, StringComparer.OrdinalIgnoreCase);
			foreach (object obj in cookies)
			{
				Cookie cookie = (Cookie)obj;
				nameValueCollection.Add(cookie.Name, cookie.Value);
			}
			return nameValueCollection;
		}

		// Token: 0x060002D4 RID: 724 RVA: 0x0000E698 File Offset: 0x0000C898
		private static NameValueCollection CopyCookies(CookieContainer cookies)
		{
			NameValueCollection nameValueCollection = new NameValueCollection(cookies.Count, StringComparer.OrdinalIgnoreCase);
			BindingFlags invokeAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField;
			try
			{
				foreach (object obj in ((Hashtable)cookies.GetType().InvokeMember("m_domainTable", invokeAttr, null, cookies, new object[0])).Values)
				{
					foreach (object obj2 in ((SortedList)obj.GetType().InvokeMember("m_list", invokeAttr, null, obj, new object[0])).Values)
					{
						foreach (object obj3 in ((CookieCollection)obj2))
						{
							Cookie cookie = (Cookie)obj3;
							nameValueCollection.Add(cookie.Name, cookie.Value);
						}
					}
				}
			}
			catch (Exception)
			{
			}
			return nameValueCollection;
		}

		// Token: 0x060002D5 RID: 725 RVA: 0x0000E7F0 File Offset: 0x0000C9F0
		private static string GetValue(object o, string key)
		{
			NameValueCollection nameValueCollection = o as NameValueCollection;
			if (nameValueCollection != null)
			{
				return nameValueCollection[key];
			}
			CookieCollection cookieCollection = o as CookieCollection;
			if (cookieCollection != null)
			{
				Cookie cookie = cookieCollection[key];
				if (cookie != null)
				{
					return cookie.Value;
				}
				return null;
			}
			else
			{
				HttpCookieCollection httpCookieCollection = o as HttpCookieCollection;
				if (httpCookieCollection == null)
				{
					return null;
				}
				HttpCookie httpCookie = httpCookieCollection[key];
				if (httpCookie != null)
				{
					return httpCookie.Value;
				}
				return null;
			}
		}

		// Token: 0x060002D6 RID: 726 RVA: 0x0000E850 File Offset: 0x0000CA50
		private static void TraceDiffs(NameObjectCollectionBase original, NameObjectCollectionBase revised, HashSet<string> notInterestingNames, StringBuilder result)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
			foreach (object obj in original)
			{
				string text = (string)obj;
				if (!notInterestingNames.Contains(text))
				{
					string value = PfdTracer.GetValue(revised, text);
					string value2 = PfdTracer.GetValue(original, text);
					if (value == null)
					{
						result.Append("-" + text + ",");
					}
					else
					{
						hashSet.Add(text);
						if (string.Compare(value2, value, StringComparison.OrdinalIgnoreCase) != 0)
						{
							result.Append("*" + text + ",");
						}
					}
				}
			}
			foreach (object obj2 in revised)
			{
				string text2 = (string)obj2;
				if (!notInterestingNames.Contains(text2) && !hashSet.Contains(text2))
				{
					result.Append("+" + text2 + ",");
				}
			}
		}

		// Token: 0x060002D7 RID: 727 RVA: 0x0000E978 File Offset: 0x0000CB78
		private void Write(string s)
		{
			if (PfdTracer.PfdTraceToDebugger.Value)
			{
				bool isAttached = Debugger.IsAttached;
			}
			if (PfdTracer.traceToEtl && ExTraceGlobals.BriefTracer.IsTraceEnabled(8))
			{
				ExTraceGlobals.BriefTracer.TracePfd((long)this.hashCode, s);
			}
			if (PfdTracer.PfdTraceToFile.Value)
			{
				using (StreamWriter streamWriter = new StreamWriter(this.TraceFilePath, true))
				{
					streamWriter.WriteLine(s);
				}
			}
		}

		// Token: 0x040001AA RID: 426
		public const string ClientRequest = "ClientRequest";

		// Token: 0x040001AB RID: 427
		public const string ProxyRequest = "ProxyRequest";

		// Token: 0x040001AC RID: 428
		public const string ProxyLogonRequest = "ProxyLogonRequest";

		// Token: 0x040001AD RID: 429
		public const string ClientResponse = "ClientResponse";

		// Token: 0x040001AE RID: 430
		public const string ProxyResponse = "ProxyResponse";

		// Token: 0x040001AF RID: 431
		public const string ProxyLogonResponse = "ProxyLogonResponse";

		// Token: 0x040001B0 RID: 432
		public const string NeedLanguage = "EcpOwa442NeedLanguage";

		// Token: 0x040001B1 RID: 433
		public const string FbaAuth = "FbaAuth";

		// Token: 0x040001B2 RID: 434
		public static readonly BoolAppSettingsEntry PfdTraceToFile = new BoolAppSettingsEntry(HttpProxySettings.Prefix("PfdTraceToFile"), false, ExTraceGlobals.VerboseTracer);

		// Token: 0x040001B3 RID: 435
		public static readonly BoolAppSettingsEntry PfdTraceToDebugger = new BoolAppSettingsEntry(HttpProxySettings.Prefix("PfdTraceToDebugger"), false, ExTraceGlobals.VerboseTracer);

		// Token: 0x040001B4 RID: 436
		private static readonly HashSet<string> NotInterestedHeaders = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		// Token: 0x040001B5 RID: 437
		private static readonly HashSet<string> NotInterestedCookies = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		// Token: 0x040001B6 RID: 438
		private static bool traceToEtl = true;

		// Token: 0x040001B7 RID: 439
		private static string traceDirectory = null;

		// Token: 0x040001B8 RID: 440
		private readonly int traceContext;

		// Token: 0x040001B9 RID: 441
		private readonly int hashCode;

		// Token: 0x040001BA RID: 442
		private readonly string vdir;

		// Token: 0x040001BB RID: 443
		private string traceFilePath;
	}
}
