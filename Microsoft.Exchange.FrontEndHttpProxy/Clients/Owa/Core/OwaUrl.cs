using System;
using System.Web;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000009 RID: 9
	public class OwaUrl
	{
		// Token: 0x06000059 RID: 89 RVA: 0x0000362C File Offset: 0x0000182C
		private OwaUrl(string path)
		{
			this.path = path;
			this.url = OwaUrl.owaVDirRootPath + path;
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x0600005A RID: 90 RVA: 0x0000364C File Offset: 0x0000184C
		public string ImplicitUrl
		{
			get
			{
				return this.url;
			}
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00003654 File Offset: 0x00001854
		public string GetExplicitUrl(HttpRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			string text = string.Equals(this.path, OwaUrl.cafeErrorPageAuthVDirPath, StringComparison.OrdinalIgnoreCase) ? OwaUrl.owaVDirRootPath : OwaUrl.applicationVRoot;
			string text2 = request.Headers[OwaHttpHeader.ExplicitLogonUser];
			if (string.IsNullOrEmpty(text2))
			{
				text2 = request.Headers["msExchEcpESOUser"];
			}
			if (!string.IsNullOrEmpty(text2))
			{
				text = text + text2 + "/";
			}
			if (this.path != null)
			{
				text += this.path;
			}
			return text;
		}

		// Token: 0x0600005C RID: 92 RVA: 0x000036E4 File Offset: 0x000018E4
		private static OwaUrl Create(string path)
		{
			return new OwaUrl(path);
		}

		// Token: 0x04000049 RID: 73
		private static readonly string applicationVRoot = HttpRuntime.AppDomainAppVirtualPath + "/";

		// Token: 0x0400004A RID: 74
		private static readonly string owaVDirRootPath = "/owa/";

		// Token: 0x0400004B RID: 75
		private static readonly string authFolder = "auth";

		// Token: 0x0400004C RID: 76
		private static readonly string oeh = "ev.owa";

		// Token: 0x0400004D RID: 77
		private static readonly string cafeErrorPageAuthVDirPath = OwaUrl.authFolder + "/errorFE.aspx";

		// Token: 0x0400004E RID: 78
		public static OwaUrl ApplicationRoot = OwaUrl.Create(string.Empty);

		// Token: 0x0400004F RID: 79
		public static OwaUrl Default14Page = OwaUrl.Create("owa14.aspx");

		// Token: 0x04000050 RID: 80
		public static OwaUrl Default15Page = OwaUrl.Create("default.aspx");

		// Token: 0x04000051 RID: 81
		public static OwaUrl Oeh = OwaUrl.Create(OwaUrl.oeh);

		// Token: 0x04000052 RID: 82
		public static OwaUrl AttachmentHandler = OwaUrl.Create("attachment.ashx");

		// Token: 0x04000053 RID: 83
		public static OwaUrl AuthFolder = OwaUrl.Create(OwaUrl.authFolder + "/");

		// Token: 0x04000054 RID: 84
		public static OwaUrl ErrorPage = OwaUrl.Create(OwaUrl.authFolder + "/error.aspx");

		// Token: 0x04000055 RID: 85
		public static OwaUrl Error2Page = OwaUrl.Create(OwaUrl.authFolder + "/error2.aspx");

		// Token: 0x04000056 RID: 86
		public static OwaUrl CafeErrorPage = OwaUrl.Create(OwaUrl.cafeErrorPageAuthVDirPath);

		// Token: 0x04000057 RID: 87
		public static OwaUrl RedirectionPage = OwaUrl.Create("redir.aspx");

		// Token: 0x04000058 RID: 88
		public static OwaUrl ProxyLogon = OwaUrl.Create("proxyLogon.owa");

		// Token: 0x04000059 RID: 89
		public static OwaUrl CasRedirectPage = OwaUrl.Create("casredirect.aspx");

		// Token: 0x0400005A RID: 90
		public static OwaUrl LanguagePage = OwaUrl.Create("languageselection.aspx");

		// Token: 0x0400005B RID: 91
		public static OwaUrl LanguagePost = OwaUrl.Create("lang.owa");

		// Token: 0x0400005C RID: 92
		public static OwaUrl LogonFBA = OwaUrl.Create("auth/logon.aspx");

		// Token: 0x0400005D RID: 93
		public static OwaUrl Logoff = OwaUrl.Create("logoff.owa");

		// Token: 0x0400005E RID: 94
		public static OwaUrl LogoffChangePassword = OwaUrl.Create("logoff.owa?ChgPwd=1");

		// Token: 0x0400005F RID: 95
		public static OwaUrl LogoffPage = OwaUrl.Create(OwaUrl.authFolder + "/logoff.aspx?Cmd=logoff&src=exch");

		// Token: 0x04000060 RID: 96
		public static OwaUrl LogoffChangePasswordPage = OwaUrl.Create(OwaUrl.authFolder + "/logoff.aspx?Cmd=logoff&ChgPwd=1");

		// Token: 0x04000061 RID: 97
		public static OwaUrl InfoFailedToSaveCulture = OwaUrl.Create("info.aspx?Msg=1");

		// Token: 0x04000062 RID: 98
		public static OwaUrl ProxyHandler = OwaUrl.Create(OwaUrl.oeh + "?oeh=1&ns=HttpProxy&ev=ProxyRequest");

		// Token: 0x04000063 RID: 99
		public static OwaUrl ProxyLanguagePost = OwaUrl.Create(OwaUrl.oeh + "?oeh=1&ns=HttpProxy&ev=LanguagePost");

		// Token: 0x04000064 RID: 100
		public static OwaUrl ProxyPing = OwaUrl.Create("ping.owa");

		// Token: 0x04000065 RID: 101
		public static OwaUrl HealthPing = OwaUrl.Create(OwaUrl.oeh + "?oeh=1&ns=Monitoring&ev=Ping");

		// Token: 0x04000066 RID: 102
		public static OwaUrl KeepAlive = OwaUrl.Create("keepalive.owa");

		// Token: 0x04000067 RID: 103
		public static OwaUrl AuthPost = OwaUrl.Create("auth.owa");

		// Token: 0x04000068 RID: 104
		public static OwaUrl AuthDll = OwaUrl.Create("auth/owaauth.dll");

		// Token: 0x04000069 RID: 105
		public static OwaUrl PublishedCalendar = OwaUrl.Create("calendar.html");

		// Token: 0x0400006A RID: 106
		public static OwaUrl PublishedICal = OwaUrl.Create("calendar.ics");

		// Token: 0x0400006B RID: 107
		public static OwaUrl SuiteServiceProxyPage = OwaUrl.Create("SuiteServiceProxy.aspx");

		// Token: 0x0400006C RID: 108
		private string path;

		// Token: 0x0400006D RID: 109
		private string url;
	}
}
