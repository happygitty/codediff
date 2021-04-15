using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Web;
using System.Web.UI;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.HttpProxy;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000008 RID: 8
	[AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	public class OwaPage : Page
	{
		// Token: 0x06000042 RID: 66 RVA: 0x00003054 File Offset: 0x00001254
		public OwaPage()
		{
		}

		// Token: 0x06000043 RID: 67 RVA: 0x0000306A File Offset: 0x0000126A
		public OwaPage(bool setNoCacheNoStore)
		{
			this.setNoCacheNoStore = setNoCacheNoStore;
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000044 RID: 68 RVA: 0x00003088 File Offset: 0x00001288
		public UserAgent UserAgent
		{
			get
			{
				if (this.userAgent == null)
				{
					UserAgent userAgent = new UserAgent(base.Request.UserAgent, base.Request.Cookies);
					if (base.Request.QueryString != null)
					{
						string text = base.Request.QueryString["layout"];
						if (text != null)
						{
							userAgent.SetLayoutFromString(text);
						}
						else
						{
							string text2 = base.Request.QueryString["url"];
							if (text2 != null)
							{
								int num = text2.IndexOf('?');
								if (num >= 0 && num < text2.Length - 1)
								{
									text = HttpUtility.ParseQueryString(text2.Substring(num + 1))["layout"];
									if (text != null)
									{
										userAgent.SetLayoutFromString(text);
									}
								}
							}
						}
					}
					this.userAgent = userAgent;
				}
				return this.userAgent;
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000045 RID: 69 RVA: 0x0000314C File Offset: 0x0000134C
		public string Identity
		{
			get
			{
				return base.GetType().BaseType.Name;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000046 RID: 70 RVA: 0x0000315E File Offset: 0x0000135E
		protected static bool IsRtl
		{
			get
			{
				return Microsoft.Exchange.Clients.Owa.Core.Culture.IsRtl;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000047 RID: 71 RVA: 0x00003165 File Offset: 0x00001365
		protected static bool SMimeEnabledPerServer
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000048 RID: 72 RVA: 0x00003168 File Offset: 0x00001368
		protected bool IsDownLevelClient
		{
			get
			{
				if (this.isDownLevelClient == -1)
				{
					this.isDownLevelClient = (base.Request.IsDownLevelClient() ? 1 : 0);
				}
				return this.isDownLevelClient == 1;
			}
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000049 RID: 73 RVA: 0x00003193 File Offset: 0x00001393
		protected virtual bool UseStrictMode
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600004A RID: 74 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool HasFrameset
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600004B RID: 75 RVA: 0x00003193 File Offset: 0x00001393
		protected virtual bool IsTextHtml
		{
			get
			{
				return true;
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003198 File Offset: 0x00001398
		public static bool IsPalEnabled(HttpContext context)
		{
			if (context.Request != null && context.Request.Cookies != null && context.Request.Cookies["PALEnabled"] != null)
			{
				return context.Request.Cookies["PALEnabled"].Value != "-1";
			}
			return context.Request.QueryString["palenabled"] == "1" || (context.Request.UserAgent != null && context.Request.UserAgent.Contains("MSAppHost"));
		}

		// Token: 0x0600004D RID: 77 RVA: 0x0000323D File Offset: 0x0000143D
		public string GetDefaultCultureCssFontFileName()
		{
			return Microsoft.Exchange.Clients.Owa.Core.Culture.GetCssFontFileNameFromCulture(Microsoft.Exchange.Clients.Owa.Core.Culture.GetUserCulture());
		}

		// Token: 0x0600004E RID: 78 RVA: 0x0000324C File Offset: 0x0000144C
		protected override void InitializeCulture()
		{
			CultureInfo cultureInfo = ClientCultures.GetBrowserDefaultCulture(base.Request);
			if (cultureInfo == null && OwaVdirConfiguration.Instance.LogonAndErrorLanguage > 0)
			{
				try
				{
					cultureInfo = CultureInfo.GetCultureInfo(OwaVdirConfiguration.Instance.LogonAndErrorLanguage);
				}
				catch (CultureNotFoundException)
				{
					cultureInfo = null;
				}
			}
			if (cultureInfo != null)
			{
				Thread.CurrentThread.CurrentUICulture = cultureInfo;
				Thread.CurrentThread.CurrentCulture = cultureInfo;
			}
			base.InitializeCulture();
		}

		// Token: 0x0600004F RID: 79 RVA: 0x000032BC File Offset: 0x000014BC
		protected void RenderIdentity()
		{
			base.Response.Output.Write("<input type=hidden name=\"");
			base.Response.Output.Write("hidpid");
			base.Response.Output.Write("\" value=\"");
			EncodingUtilities.HtmlEncode(this.Identity, base.Response.Output);
			base.Response.Output.Write("\">");
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00003334 File Offset: 0x00001534
		protected override void OnPreRender(EventArgs e)
		{
			if (this.HasFrameset)
			{
				base.Response.Write("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Frameset//EN\" \"http://www.w3.org/TR/html4/frameset.dtd\">");
				base.Response.Write("\n");
			}
			else if (this.UseStrictMode && this.IsTextHtml)
			{
				base.Response.Write("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">");
				base.Response.Write("\n");
			}
			else if (this.IsTextHtml)
			{
				base.Response.Write("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\">");
				base.Response.Write("\n");
			}
			base.Response.Write("<!-- ");
			EncodingUtilities.HtmlEncode(OwaPage.CopyrightMessage, base.Response.Output);
			base.Response.Write(" -->");
			base.Response.Write("\n<!-- OwaPage = ");
			EncodingUtilities.HtmlEncode(base.GetType().ToString(), base.Response.Output);
			base.Response.Write(" -->\n");
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00003435 File Offset: 0x00001635
		protected override void OnInit(EventArgs e)
		{
			if (this.setNoCacheNoStore)
			{
				AspNetHelper.MakePageNoCacheNoStore(base.Response);
			}
			this.EnableViewState = false;
			base.OnInit(e);
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00003458 File Offset: 0x00001658
		protected string GetNoScriptHtml()
		{
			return string.Format(LocalizedStrings.GetHtmlEncoded(719849305), "<a href=\"https://go.microsoft.com/fwlink/?linkid=2009667&clcid=0x409\">", "</a>");
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00003473 File Offset: 0x00001673
		protected string InlineJavascript(string fileName)
		{
			return this.InlineResource(fileName, "scripts\\premium\\", (string fullFilePath) => "<script>" + File.ReadAllText(fullFilePath) + "</script>", OwaPage.inlineScripts);
		}

		// Token: 0x06000054 RID: 84 RVA: 0x000034A8 File Offset: 0x000016A8
		protected string InlineImage(ThemeFileId themeFileId)
		{
			string fileName = ThemeFileList.GetNameFromId(themeFileId);
			return this.InlineResource(fileName, "themes\\resources", (string fullFilePath) => "data:" + MimeMapping.GetMimeMapping(fileName) + ";base64," + Convert.ToBase64String(File.ReadAllBytes(fullFilePath)), OwaPage.inlineImages);
		}

		// Token: 0x06000055 RID: 85 RVA: 0x000034EC File Offset: 0x000016EC
		protected string InlineCss(ThemeFileId themeFileId)
		{
			string nameFromId = ThemeFileList.GetNameFromId(themeFileId);
			return this.InlineCss(nameFromId);
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00003507 File Offset: 0x00001707
		protected string InlineCss(string fileName)
		{
			return this.InlineResource(fileName, "themes\\resources", (string fullFilePath) => "<style>" + File.ReadAllText(fullFilePath) + "</style>", OwaPage.inlineStyles);
		}

		// Token: 0x06000057 RID: 87 RVA: 0x0000353C File Offset: 0x0000173C
		private string InlineResource(string fileName, string partialFileLocation, OwaPage.ResourceCreator createResource, Dictionary<string, Tuple<string, DateTime>> resourceDictionary)
		{
			string text = HttpRuntime.AppDomainAppPath.ToLower();
			if (text.EndsWith("ecp\\"))
			{
				text = text.Replace("ecp\\", "owa\\");
			}
			string text2 = Path.Combine(text, "auth\\" + ProxyApplication.ApplicationVersion, partialFileLocation, fileName);
			DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(text2);
			Tuple<string, DateTime> tuple;
			lock (resourceDictionary)
			{
				if (!resourceDictionary.TryGetValue(text2, out tuple) || tuple.Item2 < lastWriteTimeUtc)
				{
					tuple = Tuple.Create<string, DateTime>(createResource(text2), lastWriteTimeUtc);
					resourceDictionary[text2] = tuple;
				}
			}
			return tuple.Item1;
		}

		// Token: 0x04000036 RID: 54
		public static readonly string CopyrightMessage = "Copyright (c) 2011 Microsoft Corporation.  All rights reserved.";

		// Token: 0x04000037 RID: 55
		public static readonly string SupportedBrowserHelpUrl = "http://office.com/redir/HA102824601.aspx";

		// Token: 0x04000038 RID: 56
		protected const string SilverlightXapName = "OwaSl";

		// Token: 0x04000039 RID: 57
		protected const string SilverlightRootNamespace = "Microsoft.Exchange.Clients.Owa.Silverlight";

		// Token: 0x0400003A RID: 58
		protected const string SilverlightPluginErrorHandler = "SL_OnPluginError";

		// Token: 0x0400003B RID: 59
		protected const string PALEnabledCookieName = "PALEnabled";

		// Token: 0x0400003C RID: 60
		protected const string LoadFailedCookieName = "loadFailed";

		// Token: 0x0400003D RID: 61
		private const string PageIdentityHiddenName = "hidpid";

		// Token: 0x0400003E RID: 62
		private const string LayoutParamName = "layout";

		// Token: 0x0400003F RID: 63
		private const string ScriptsPath = "scripts\\premium\\";

		// Token: 0x04000040 RID: 64
		private const string ResourcesPath = "themes\\resources";

		// Token: 0x04000041 RID: 65
		private const string OwaVDir = "owa\\";

		// Token: 0x04000042 RID: 66
		private const string EcpVDir = "ecp\\";

		// Token: 0x04000043 RID: 67
		private static Dictionary<string, Tuple<string, DateTime>> inlineScripts = new Dictionary<string, Tuple<string, DateTime>>();

		// Token: 0x04000044 RID: 68
		private static Dictionary<string, Tuple<string, DateTime>> inlineImages = new Dictionary<string, Tuple<string, DateTime>>();

		// Token: 0x04000045 RID: 69
		private static Dictionary<string, Tuple<string, DateTime>> inlineStyles = new Dictionary<string, Tuple<string, DateTime>>();

		// Token: 0x04000046 RID: 70
		private bool setNoCacheNoStore = true;

		// Token: 0x04000047 RID: 71
		private int isDownLevelClient = -1;

		// Token: 0x04000048 RID: 72
		private UserAgent userAgent;

		// Token: 0x020000CE RID: 206
		// (Invoke) Token: 0x060007A0 RID: 1952
		private delegate string ResourceCreator(string fullFileName);
	}
}
