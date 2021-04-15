using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Microsoft.Exchange.Clients;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Clients.Security;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.ClientsCommon;
using Microsoft.Exchange.VariantConfiguration.Global;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000061 RID: 97
	public class ErrorFE : OwaPage
	{
		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x060002FE RID: 766 RVA: 0x0000ED4C File Offset: 0x0000CF4C
		protected bool IsAriaLoggingEnabled
		{
			get
			{
				HttpRequest request = HttpContext.Current.Request;
				string text;
				string probableExceptionNameForAria = ErrorFE.GetProbableExceptionNameForAria(request, out text);
				HttpCookie httpCookie = request.Cookies["AriaEx"];
				string a = (httpCookie != null) ? httpCookie.Value : string.Empty;
				return (httpCookie == null || a != HttpUtility.UrlEncode(probableExceptionNameForAria)) && OwaServerTelemetryLogManager.IsAriaTelemetryForErrorPageEnabled();
			}
		}

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x060002FF RID: 767 RVA: 0x0000EDA5 File Offset: 0x0000CFA5
		protected string AriaTenantToken
		{
			get
			{
				return OwaServerTelemetryLogManager.GetOWABootTenantId();
			}
		}

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x06000300 RID: 768 RVA: 0x0000EDAC File Offset: 0x0000CFAC
		protected bool HasErrorDetails
		{
			get
			{
				return this.errorInformation.MessageDetails != null;
			}
		}

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x06000301 RID: 769 RVA: 0x0000EDBC File Offset: 0x0000CFBC
		protected bool IsPreviousPageLinkEnabled
		{
			get
			{
				return !string.IsNullOrEmpty(this.errorInformation.PreviousPageUrl);
			}
		}

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x06000302 RID: 770 RVA: 0x0000EDD1 File Offset: 0x0000CFD1
		protected bool IsExternalLinkPresent
		{
			get
			{
				return !string.IsNullOrEmpty(this.errorInformation.ExternalPageLink);
			}
		}

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x06000303 RID: 771 RVA: 0x0000EDE8 File Offset: 0x0000CFE8
		protected bool IsOfflineEnabledClient
		{
			get
			{
				if (HttpContext.Current != null && HttpContext.Current.Request != null)
				{
					HttpCookie httpCookie = HttpContext.Current.Request.Cookies.Get("offline");
					if (httpCookie != null && httpCookie.Value == "1")
					{
						return true;
					}
				}
				return false;
			}
		}

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x06000304 RID: 772 RVA: 0x0000EE3A File Offset: 0x0000D03A
		protected bool RenderAddToFavoritesButton
		{
			get
			{
				return !string.IsNullOrEmpty(this.errorInformation.RedirectionUrl) && this.isIE;
			}
		}

		// Token: 0x170000AB RID: 171
		// (get) Token: 0x06000305 RID: 773 RVA: 0x0000EE56 File Offset: 0x0000D056
		protected ErrorInformation ErrorInformation
		{
			get
			{
				return this.errorInformation;
			}
		}

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x06000306 RID: 774 RVA: 0x0000EE5E File Offset: 0x0000D05E
		protected bool RenderDiagnosticInfo
		{
			get
			{
				return this.renderDiagnosticInfo;
			}
		}

		// Token: 0x170000AD RID: 173
		// (get) Token: 0x06000307 RID: 775 RVA: 0x0000EE66 File Offset: 0x0000D066
		protected string DiagnosticInfo
		{
			get
			{
				return this.diagnosticInfo;
			}
		}

		// Token: 0x170000AE RID: 174
		// (get) Token: 0x06000308 RID: 776 RVA: 0x0000EE6E File Offset: 0x0000D06E
		protected string ResourcePath
		{
			get
			{
				if (this.resourcePath == null)
				{
					this.resourcePath = OwaUrl.AuthFolder.ImplicitUrl;
				}
				return this.resourcePath;
			}
		}

		// Token: 0x170000AF RID: 175
		// (get) Token: 0x06000309 RID: 777 RVA: 0x0000EE90 File Offset: 0x0000D090
		protected string LoadFailedMessageValue
		{
			get
			{
				string text = this.errorInformation.HttpCode.ToString();
				if (this.errorInformation.MessageDetails != null)
				{
					text = text + ":" + this.errorInformation.MessageDetails;
				}
				return text;
			}
		}

		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x0600030A RID: 778 RVA: 0x0000EED8 File Offset: 0x0000D0D8
		protected bool ShowRefreshButton
		{
			get
			{
				return (this.errorInformation == null || (this.errorInformation.RedirectionUrl == null && !this.errorInformation.SiteMailbox && !this.errorInformation.GroupMailbox)) && (!this.isConsumerRequest || !(this.errorInformation.ErrorMode == 3));
			}
		}

		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x0600030B RID: 779 RVA: 0x0000EF48 File Offset: 0x0000D148
		protected string GetPlt1QueryString
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendFormat("?{0}={1}", "off", "0");
				stringBuilder.AppendFormat("&{0}={1}", "PLT", "now,0");
				stringBuilder.AppendFormat("&{0}={1}", "msg", "FormErr");
				if (HttpContext.Current != null && HttpContext.Current.Request != null)
				{
					HttpRequest request = HttpContext.Current.Request;
					stringBuilder.AppendFormat("&{0}={1}", "cid", HttpUtility.UrlEncode(DiagnosticIdentifiers.GetCookieValue(HttpContext.Current)));
					stringBuilder.AppendFormat("&{0}={1}", "reqid", HttpUtility.UrlEncode(request.QueryString["reqid"]));
					stringBuilder.AppendFormat("&{0}={1}", "fe", HttpUtility.UrlEncode(request.QueryString["fe"]));
					stringBuilder.AppendFormat("&{0}={1}", "be", HttpUtility.UrlEncode(request.QueryString["be"]));
					stringBuilder.AppendFormat("&{0}={1}", "cbe", HttpUtility.UrlEncode(request.QueryString["cbe"]));
					stringBuilder.AppendFormat("&{0}={1}", "tg", HttpUtility.UrlEncode(request.QueryString["tg"]));
					stringBuilder.AppendFormat("&{0}={1}", "MDB", HttpUtility.UrlEncode(request.QueryString["MDB"]));
					stringBuilder.AppendFormat("&{0}={1}", "pal", HttpUtility.UrlEncode(request.QueryString["pal"]));
				}
				return stringBuilder.ToString();
			}
		}

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x0600030C RID: 780 RVA: 0x0000F0F1 File Offset: 0x0000D2F1
		protected string GetXOWAPLTInfoHeaderValue
		{
			get
			{
				return string.Format("&{0}={1}", "Err", HttpUtility.HtmlEncode(ErrorFE.GetExceptionFromQueryString(HttpContext.Current.Request)));
			}
		}

		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x0600030D RID: 781 RVA: 0x0000F0F1 File Offset: 0x0000D2F1
		protected string GetPlt1RequestBody
		{
			get
			{
				return string.Format("&{0}={1}", "Err", HttpUtility.HtmlEncode(ErrorFE.GetExceptionFromQueryString(HttpContext.Current.Request)));
			}
		}

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x0600030E RID: 782 RVA: 0x0000F118 File Offset: 0x0000D318
		protected bool ShouldSendOWAPlt1Request
		{
			get
			{
				string text = HttpContext.Current.Request.QueryString["rt"];
				return !string.IsNullOrEmpty(text) && text.Contains("Form15");
			}
		}

		// Token: 0x0600030F RID: 783 RVA: 0x0000F154 File Offset: 0x0000D354
		protected override void OnLoad(EventArgs e)
		{
			ErrorFE.FEErrorCodes feerrorCodes = ErrorFE.FEErrorCodes.Unknown;
			int httpCode = 500;
			string redirectionUrl = null;
			bool flag = false;
			if (HttpContext.Current != null)
			{
				this.isConsumerRequest = UrlUtilities.IsConsumerRequestForO365(HttpContext.Current);
				if (HttpContext.Current.Items != null)
				{
					if (HttpContext.Current.Items.Contains("CafeError"))
					{
						feerrorCodes = (ErrorFE.FEErrorCodes)HttpContext.Current.Items["CafeError"];
					}
					if (HttpContext.Current.Items.Contains("redirectUrl"))
					{
						redirectionUrl = (HttpContext.Current.Items["redirectUrl"] as string);
					}
				}
				if (HttpContext.Current.Request != null && feerrorCodes == ErrorFE.FEErrorCodes.Unknown)
				{
					Enum.TryParse<ErrorFE.FEErrorCodes>(HttpContext.Current.Request.QueryString["CafeError"], out feerrorCodes);
					string s;
					if (string.IsNullOrEmpty(s = HttpContext.Current.Request.QueryString["httpCode"]) || !int.TryParse(s, out httpCode))
					{
						string targetSiteDistinguishedName;
						string text;
						if (!string.IsNullOrEmpty(targetSiteDistinguishedName = HttpContext.Current.Request.QueryString["targetSiteDistinguishedName"]))
						{
							if (!Utilities.IsPartnerHostedOnly && !GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled)
							{
								ServiceTopology currentServiceTopology = ServiceTopology.GetCurrentServiceTopology("d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\fba\\ErrorFE.aspx.cs", "OnLoad", 579);
								HttpService httpService;
								if (HttpProxyGlobals.ProtocolType == 1)
								{
									httpService = currentServiceTopology.FindAny<EcpService>(1, (EcpService externalService) => externalService != null && externalService.IsFrontEnd && externalService.Site.DistinguishedName.Equals(targetSiteDistinguishedName), "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\fba\\ErrorFE.aspx.cs", "OnLoad", 586);
								}
								else
								{
									httpService = currentServiceTopology.FindAny<OwaService>(1, (OwaService externalService) => externalService != null && externalService.IsFrontEnd && externalService.Site.DistinguishedName.Equals(targetSiteDistinguishedName), "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\fba\\ErrorFE.aspx.cs", "OnLoad", 595);
								}
								if (httpService != null)
								{
									redirectionUrl = new UriBuilder(HttpContext.Current.Request.Url)
									{
										Host = httpService.Url.Host,
										Path = HttpUtility.UrlDecode(HttpContext.Current.Request.QueryString["path"] ?? string.Empty),
										Query = HttpUtility.UrlDecode(HttpContext.Current.Request.QueryString["query"] ?? string.Empty)
									}.Uri.AbsoluteUri;
									httpCode = 302;
								}
							}
						}
						else if (!string.IsNullOrEmpty(text = HttpContext.Current.Request.QueryString["extDomain"]))
						{
							string text2 = HttpContext.Current.Request.QueryString["extDirOrgId"];
							OrganizationId organizationId = null;
							Guid guid;
							if (string.IsNullOrEmpty(text2))
							{
								SmtpDomain smtpDomain;
								if (SmtpDomain.TryParse(text, ref smtpDomain))
								{
									organizationId = DomainToOrganizationIdCache.Singleton.Get(smtpDomain);
								}
							}
							else if (Guid.TryParse(text2, out guid))
							{
								organizationId = OrganizationId.FromExternalDirectoryOrganizationId(guid);
							}
							if (organizationId != null)
							{
								OrganizationIdCacheValue organizationIdCacheValue = OrganizationIdCache.Singleton.Get(organizationId);
								if (organizationIdCacheValue != null)
								{
									OrganizationRelationship organizationRelationship = organizationIdCacheValue.GetOrganizationRelationship(text);
									if (organizationRelationship != null && organizationRelationship.TargetOwaURL != null)
									{
										Uri uri = organizationRelationship.TargetOwaURL;
										string text3 = HttpContext.Current.Request.QueryString["extEmail"];
										if (!string.IsNullOrEmpty(text3))
										{
											uri = UrlUtilities.AppendSmtpAddressToUrl(uri, text3);
										}
										redirectionUrl = uri.AbsoluteUri;
										flag = true;
										httpCode = 302;
									}
								}
							}
						}
					}
				}
			}
			bool sharePointApp;
			if (!bool.TryParse(HttpContext.Current.Request.QueryString["sharepointapp"], out sharePointApp))
			{
				sharePointApp = false;
			}
			bool siteMailbox;
			if (!bool.TryParse(HttpContext.Current.Request.QueryString["sm"], out siteMailbox))
			{
				siteMailbox = false;
			}
			ErrorMode value;
			if (!Enum.TryParse<ErrorMode>(HttpContext.Current.Request.QueryString["m"], out value))
			{
				value = 0;
			}
			if (flag)
			{
				value = 5;
			}
			string groupMailboxDestination = HttpContext.Current.Request.QueryString["gm"];
			this.errorInformation = new ErrorInformation(httpCode, feerrorCodes.ToString(), sharePointApp);
			this.errorInformation.SiteMailbox = siteMailbox;
			this.errorInformation.GroupMailboxDestination = groupMailboxDestination;
			this.errorInformation.RedirectionUrl = redirectionUrl;
			this.errorInformation.ErrorMode = new ErrorMode?(value);
			this.isIE = (BrowserType.IE == Utilities.GetBrowserType(this.Context.Request.UserAgent));
			this.CompileDiagnosticInfo();
			this.AddDiagnosticsHeaders();
			this.SetHTTPResponseStatusCode();
			base.Response.Headers.Set("X-Content-Type-Options", "nosniff");
			this.OnInit(e);
		}

		// Token: 0x06000310 RID: 784 RVA: 0x0000F5F8 File Offset: 0x0000D7F8
		protected void RenderTitle()
		{
			if (this.errorInformation.ErrorMode == 1)
			{
				base.Response.Write(LocalizedStrings.GetHtmlEncoded(-594631022));
				return;
			}
			if (this.errorInformation.ErrorMode == 5)
			{
				base.Response.Write(LocalizedStrings.GetHtmlEncoded(-1716958256));
				return;
			}
			base.Response.Write(LocalizedStrings.GetHtmlEncoded(933672694));
		}

		// Token: 0x06000311 RID: 785 RVA: 0x0000F68E File Offset: 0x0000D88E
		protected void RenderIcon()
		{
			ThemeManager.RenderBaseThemeFileUrl(base.Response.Output, this.errorInformation.Icon, false);
		}

		// Token: 0x06000312 RID: 786 RVA: 0x0000F6AC File Offset: 0x0000D8AC
		protected void AddDiagnosticsHeaders()
		{
			if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.QueryString["owaError"]))
			{
				base.Response.AddHeader("X-OWA-Error", HttpContext.Current.Request.QueryString["owaError"]);
			}
			if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.QueryString["authError"]))
			{
				base.Response.AddHeader("X-Auth-Error", HttpContext.Current.Request.QueryString["authError"]);
			}
			if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.QueryString["redirError"]))
			{
				base.Response.AddHeader("X-Redir-Error", HttpContext.Current.Request.QueryString["redirError"]);
			}
			if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.QueryString["jitError"]))
			{
				base.Response.AddHeader("X-JIT-Error", HttpContext.Current.Request.QueryString["jitError"]);
			}
			if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.QueryString["owaVer"]))
			{
				base.Response.AddHeader("X-OWA-Version", HttpContext.Current.Request.QueryString["owaVer"]);
			}
			if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.QueryString["fe"]))
			{
				base.Response.AddHeader("X-FEServer", HttpContext.Current.Request.QueryString["fe"]);
			}
			else if (!string.IsNullOrWhiteSpace(Environment.MachineName))
			{
				base.Response.AddHeader("X-FEServer", Environment.MachineName);
			}
			if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.QueryString["be"]))
			{
				base.Response.AddHeader("X-BEServer", HttpContext.Current.Request.QueryString["be"]);
			}
		}

		// Token: 0x06000313 RID: 787 RVA: 0x0000F8DC File Offset: 0x0000DADC
		protected void CompileDiagnosticInfo()
		{
			this.renderDiagnosticInfo = false;
			StringBuilder stringBuilder = new StringBuilder();
			if (!this.AddQueryParamToDiagInfo(stringBuilder, "creqid", "client-request-id", 10) && !this.AddQueryParamToDiagInfo(stringBuilder, "cid", "X-OWA-CorrelationId", 10) && HttpContext.Current.Request.Cookies["ClientId"] != null)
			{
				this.renderDiagnosticInfo = true;
				stringBuilder.Append("X-ClientId: ");
				string text = DiagnosticIdentifiers.ParseToPrintableString(HttpContext.Current.Request.Cookies["ClientId"].Value);
				stringBuilder.Append(text.ToUpperInvariant());
				stringBuilder.Append("\n");
				this.AddQueryParamToDiagInfo(stringBuilder, "reqid", "request-id", 10);
			}
			this.AddQueryParamToDiagInfo(stringBuilder, "owaError", "X-OWA-Error", 0);
			this.AddQueryParamToDiagInfo(stringBuilder, "authError", "X-Auth-Error", 0);
			this.AddQueryParamToDiagInfo(stringBuilder, "redirError", "X-Redir-Error", 0);
			this.AddQueryParamToDiagInfo(stringBuilder, "jitError", "X-JIT-Error", 0);
			this.AddQueryParamToDiagInfo(stringBuilder, "owaVer", "X-OWA-Version", 24);
			if (!this.AddQueryParamToDiagInfo(stringBuilder, "fe", "X-FEServer", 108))
			{
				this.AddValueToDiagInfo(stringBuilder, Environment.MachineName, "X-FEServer", 108);
			}
			this.AddQueryParamToDiagInfo(stringBuilder, "be", "X-BEServer", 108);
			long fileTime;
			if (long.TryParse(HttpContext.Current.Request.QueryString["ts"], out fileTime))
			{
				this.renderDiagnosticInfo = true;
				stringBuilder.Append("Date:");
				stringBuilder.Append(DateTime.FromFileTimeUtc(fileTime).ToString());
				stringBuilder.Append("\n");
			}
			else if (this.renderDiagnosticInfo)
			{
				stringBuilder.Append("Date:");
				stringBuilder.Append(DateTime.UtcNow.ToString());
				stringBuilder.Append("\n");
			}
			this.AddQueryParamToDiagInfo(stringBuilder, "inex", "InnerException:", 0);
			this.diagnosticInfo = stringBuilder.ToString();
		}

		// Token: 0x06000314 RID: 788 RVA: 0x0000FAEC File Offset: 0x0000DCEC
		protected void RenderErrorHeader()
		{
			if (!this.errorInformation.SharePointApp && !this.errorInformation.GroupMailbox && !(this.errorInformation.ErrorMode == 1))
			{
				if (this.errorInformation.HttpCode == 404)
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(-392503097));
					return;
				}
				if (this.errorInformation.HttpCode != 302)
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(629133816));
				}
			}
		}

		// Token: 0x06000315 RID: 789 RVA: 0x0000FB8C File Offset: 0x0000DD8C
		protected void RenderErrorSubHeader()
		{
			if (this.errorInformation.SharePointApp)
			{
				base.Response.Write(LocalizedStrings.GetHtmlEncoded(735230835));
				return;
			}
			if (this.errorInformation.GroupMailbox)
			{
				if (this.errorInformation.GroupMailboxDestination == "conv")
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(-526376074));
					return;
				}
				if (this.errorInformation.GroupMailboxDestination == "cal")
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(1147057944));
					return;
				}
			}
			else
			{
				if (this.errorInformation.ErrorMode == 1)
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(-146632527));
					return;
				}
				if (this.errorInformation.ErrorMode == 2)
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(-1935911806));
					return;
				}
				if (this.errorInformation.ErrorMode == 3)
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(425733410));
					return;
				}
				if (this.errorInformation.ErrorMode == 4)
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(-432125413));
					return;
				}
				if (this.errorInformation.HttpCode == 404)
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(1252002283));
					return;
				}
				if (this.errorInformation.HttpCode == 503)
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(1252002321));
					return;
				}
				if (this.errorInformation.HttpCode != 302)
				{
					base.Response.Write(LocalizedStrings.GetHtmlEncoded(1252002318));
				}
			}
		}

		// Token: 0x06000316 RID: 790 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected void RenderErrorSubHeader2()
		{
		}

		// Token: 0x06000317 RID: 791 RVA: 0x0000FD88 File Offset: 0x0000DF88
		protected void RenderRefreshButtonText()
		{
			if (this.errorInformation.ErrorMode == 1)
			{
				base.Response.Write(LocalizedStrings.GetHtmlEncoded(867248262));
				return;
			}
			base.Response.Write(LocalizedStrings.GetHtmlEncoded(1939504838));
		}

		// Token: 0x06000318 RID: 792 RVA: 0x0000FDE4 File Offset: 0x0000DFE4
		protected void RenderErrorDetails()
		{
			if (!this.errorInformation.GroupMailbox)
			{
				Strings.IDs ds;
				if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.QueryString["msg"] != null && Enum.TryParse<Strings.IDs>(HttpContext.Current.Request.QueryString["msg"], out ds))
				{
					string text = ErrorFE.SafeErrorMessagesNoHtmlEncoding.Contains(ds) ? Strings.GetLocalizedString(ds) : LocalizedStrings.GetHtmlEncoded(ds);
					List<string> list = Microsoft.Exchange.Clients.Common.ErrorInformation.ParseMessageParameters(text, HttpContext.Current.Request);
					if (list != null && list.Count > 0)
					{
						for (int i = 0; i < list.Count; i++)
						{
							list[i] = EncodingUtilities.HtmlEncode(list[i]);
						}
						if (ErrorFE.MessagesToRenderLogoutLinks.Contains(ds) || ErrorFE.MessagesToRenderLoginLinks.Contains(ds))
						{
							ErrorFE.AddSafeLinkToMessageParametersList(ds, HttpContext.Current.Request, ref list);
						}
						base.Response.Write(string.Format(text, list.ToArray()));
						return;
					}
					if (!ErrorFE.MessagesToRenderLogoutLinks.Contains(ds) && !ErrorFE.MessagesToRenderLoginLinks.Contains(ds))
					{
						base.Response.Write(text);
						return;
					}
					list = new List<string>();
					ErrorFE.AddSafeLinkToMessageParametersList(ds, HttpContext.Current.Request, ref list);
					if (list.Count > 0)
					{
						base.Response.Write(string.Format(text, list.ToArray()));
						return;
					}
				}
				else
				{
					if (this.errorInformation.HttpCode == 404)
					{
						base.Response.Write(LocalizedStrings.GetHtmlEncoded(236137810));
						return;
					}
					if (this.errorInformation.HttpCode == 302)
					{
						LegacyRedirectTypeOptions? legacyRedirectTypeOptions;
						if (HttpContext.Current.Items.Contains("redirectType"))
						{
							legacyRedirectTypeOptions = (HttpContext.Current.Items["redirectType"] as LegacyRedirectTypeOptions?);
						}
						else
						{
							try
							{
								legacyRedirectTypeOptions = (LegacyRedirectTypeOptions?)Enum.Parse(typeof(LegacyRedirectTypeOptions), HttpContext.Current.Request.QueryString["redirectType"], true);
							}
							catch
							{
								legacyRedirectTypeOptions = null;
							}
						}
						if (legacyRedirectTypeOptions == null || legacyRedirectTypeOptions != 1)
						{
							base.Response.Redirect(this.errorInformation.RedirectionUrl);
							return;
						}
						base.Response.Write(LocalizedStrings.GetHtmlEncoded(967320822));
						base.Response.Write("<br/>");
						base.Response.Write(string.Format("<a href=\"{0}\">{0}</a>", this.errorInformation.RedirectionUrl));
						base.Response.Headers.Add("X-OWA-FEError", ErrorFE.FEErrorCodes.CasRedirect.ToString());
						return;
					}
					else
					{
						base.Response.Write(LocalizedStrings.GetHtmlEncoded(236137783));
					}
				}
				return;
			}
			if (this.errorInformation.GroupMailboxDestination == "conv")
			{
				base.Response.Write(LocalizedStrings.GetHtmlEncoded(-364732161));
				return;
			}
			if (this.errorInformation.GroupMailboxDestination == "cal")
			{
				base.Response.Write(LocalizedStrings.GetHtmlEncoded(-292781713));
			}
		}

		// Token: 0x06000319 RID: 793 RVA: 0x0001013C File Offset: 0x0000E33C
		protected void RenderOfflineInfo()
		{
			if (!this.IsOfflineEnabledClient)
			{
				base.Response.Write(LocalizedStrings.GetHtmlEncoded(-1051316555));
			}
		}

		// Token: 0x0600031A RID: 794 RVA: 0x0001015C File Offset: 0x0000E35C
		protected void RenderOfflineDetails()
		{
			if (!this.IsOfflineEnabledClient)
			{
				string str;
				using (StringWriter stringWriter = new StringWriter())
				{
					ThemeManager.RenderBaseThemeFileUrl(stringWriter, ThemeFileId.OwaSettings, false);
					str = stringWriter.ToString();
				}
				string s = string.Format(LocalizedStrings.GetHtmlEncoded(510910463), "<img src=\"" + OwaUrl.AuthFolder.ImplicitUrl + str + "\"/>");
				base.Response.Write(LocalizedStrings.GetHtmlEncoded(107625936));
				base.Response.Write("<br/>");
				base.Response.Write(s);
				base.Response.Write("<br/>");
				base.Response.Write(LocalizedStrings.GetHtmlEncoded(-1055173478));
				base.Response.Write("<br/>");
				base.Response.Write(LocalizedStrings.GetHtmlEncoded(-295658591));
			}
		}

		// Token: 0x0600031B RID: 795 RVA: 0x0001024C File Offset: 0x0000E44C
		protected void RenderExternalLink()
		{
			base.Response.Write(this.errorInformation.ExternalPageLink);
		}

		// Token: 0x0600031C RID: 796 RVA: 0x00010264 File Offset: 0x0000E464
		protected void RenderBackLink()
		{
			base.Response.Write(string.Format(LocalizedStrings.GetHtmlEncoded(161749640), "<a href=\"" + this.errorInformation.PreviousPageUrl + "\">", "</a>"));
		}

		// Token: 0x0600031D RID: 797 RVA: 0x0001029F File Offset: 0x0000E49F
		protected void RenderBackground()
		{
			ThemeManager.RenderBaseThemeFileUrl(base.Response.Output, this.errorInformation.Background, false);
		}

		// Token: 0x0600031E RID: 798 RVA: 0x000102BD File Offset: 0x0000E4BD
		protected bool IsNewErrorPageQuery()
		{
			return this.Context.Request.QueryString["et"] != null;
		}

		// Token: 0x0600031F RID: 799 RVA: 0x000102DC File Offset: 0x0000E4DC
		protected string CompileAriaDiagnosticInfoV2()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			HttpRequest request = this.Context.Request;
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Web_Session_Type", WebSessionTypeExtensions.GetWebSessionTypeFromRequest(this.Context).ToString());
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Client_Id", DiagnosticIdentifiers.GetCookieValue(this.Context));
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Cache_Type", request.QueryString["caTy"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Client_Version", request.QueryString["cver"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Server_Version", request.QueryString["owaVer"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Load_Time", request.QueryString["lt"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Ref_Url", request.QueryString["refurl"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Is_Pal", request.QueryString["pal"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Through_Edge", request.QueryString["te"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Is_Premium", request.QueryString["prem"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "MBX_Guid", request.QueryString["mbx"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Puid", request.QueryString["nId"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "MDB_Guid", request.QueryString["MDB"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Tenant_Guid", request.QueryString["tg"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "FE_Server", request.QueryString["fe"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "BE_Server", request.QueryString["be"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Dag", request.QueryString["dag"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Forest", request.QueryString["forest"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Error_Type", request.QueryString["et"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Http_Code", request.QueryString["httpCode"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Request_Id", request.QueryString["reqid"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Error", request.QueryString["owaError"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Error", request.QueryString["authError"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Error", request.QueryString["redirError"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Error", request.QueryString["jitError"]);
			ErrorFE.AddKeyValueWhenNotEmptyOrNullAndNotDup(dictionary, "Extra_Error_Info", request.QueryString["inex"]);
			dictionary.Add("Misc_Data", string.Format("host: {0}, query: {1}", request.Url.Host, request.Url.Query));
			return new JavaScriptSerializer().Serialize(dictionary);
		}

		// Token: 0x06000320 RID: 800 RVA: 0x000105F0 File Offset: 0x0000E7F0
		protected string CompileAriaDiagnosticInfo()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			HttpRequest request = this.Context.Request;
			if (!this.IsErrorFEQueryParameterNull("creqid"))
			{
				dictionary.Add("RequestId", HttpUtility.HtmlEncode(request.QueryString["creqid"]));
			}
			else if (!this.IsErrorFEQueryParameterNull("cid"))
			{
				dictionary.Add("RequestId", HttpUtility.HtmlEncode(request.QueryString["cid"]));
			}
			else if (!this.IsErrorFEQueryParameterNull("reqid"))
			{
				dictionary.Add("RequestId", HttpUtility.HtmlEncode(request.QueryString["reqid"]));
			}
			string value;
			string probableExceptionNameForAria = ErrorFE.GetProbableExceptionNameForAria(request, out value);
			this.SetAriaCookie(HttpContext.Current.Response, probableExceptionNameForAria);
			dictionary.Add("Exception", probableExceptionNameForAria);
			dictionary.Add("StackTrace", value);
			string cookieValue = DiagnosticIdentifiers.GetCookieValue(HttpContext.Current);
			dictionary.Add("ClientId", (cookieValue != null) ? cookieValue : string.Empty);
			if (this.ShouldSendOWAPlt1Request)
			{
				dictionary.Add("Source", "form15");
			}
			else if (HttpContext.Current.Request.Url.Query.Contains("owaError"))
			{
				dictionary.Add("Source", "owaclient");
			}
			else
			{
				dictionary.Add("Source", "unknown");
			}
			dictionary.Add("MachineName", Environment.MachineName);
			dictionary.Add("MiscData", string.Format("host: {0}, query: {1}", HttpUtility.HtmlEncode(request.Url.Host), HttpUtility.HtmlEncode(request.Url.Query)));
			dictionary.Add("HttpStatusCode", HttpUtility.HtmlEncode(request.QueryString["httpCode"]));
			dictionary.Add("Forest", HttpUtility.HtmlEncode(request.QueryString["forest"]));
			dictionary.Add("DAG", HttpUtility.HtmlEncode(request.QueryString["dag"]));
			dictionary.Add("OwaVersion", HttpUtility.HtmlEncode(request.QueryString["owaVer"]));
			dictionary.Add("FEServerName", HttpUtility.HtmlEncode(request.QueryString["fe"]));
			dictionary.Add("BEServerName", HttpUtility.HtmlEncode(request.QueryString["be"]));
			dictionary.Add("InnerException", HttpUtility.HtmlEncode(request.QueryString["inex"]));
			dictionary.Add("Message", HttpUtility.HtmlEncode(request.QueryString["msgParam"]));
			dictionary.Add("WebSessionType", WebSessionTypeExtensions.GetWebSessionTypeFromRequest(HttpContext.Current).ToString());
			dictionary.Add("cbe", HttpUtility.HtmlEncode(request.QueryString["cbe"]));
			dictionary.Add("tg", HttpUtility.HtmlEncode(request.QueryString["tg"]));
			dictionary.Add("MDB", HttpUtility.HtmlEncode(request.QueryString["MDB"]));
			dictionary.Add("pal", HttpUtility.HtmlEncode(request.QueryString["pal"]));
			return new JavaScriptSerializer().Serialize(dictionary);
		}

		// Token: 0x06000321 RID: 801 RVA: 0x0001093C File Offset: 0x0000EB3C
		private static string GetLocalizedLiveIdSignoutLinkMessage(HttpRequest request)
		{
			string explicitUrl = OwaUrl.Logoff.GetExplicitUrl(request);
			return "<BR><BR>" + string.Format(CultureInfo.InvariantCulture, Strings.LogonErrorLogoutUrlText, explicitUrl);
		}

		// Token: 0x06000322 RID: 802 RVA: 0x00010970 File Offset: 0x0000EB70
		private static void AddSafeLinkToMessageParametersList(Strings.IDs messageId, HttpRequest request, ref List<string> messageParameters)
		{
			string item = string.Empty;
			string text = string.Empty;
			if (ErrorFE.MessagesToRenderLogoutLinks.Contains(messageId))
			{
				item = ErrorFE.GetLocalizedLiveIdSignoutLinkMessage(request);
				messageParameters.Insert(0, item);
				return;
			}
			if (ErrorFE.MessagesToRenderLoginLinks.Contains(messageId))
			{
				string dnsSafeHost = request.Url.DnsSafeHost;
				if (messageParameters != null && messageParameters.Count > 0)
				{
					text = messageParameters[0];
				}
				item = Utilities.GetAccessURLFromHostnameAndRealm(dnsSafeHost, text, false);
				messageParameters.Insert(0, item);
				messageParameters.Remove(dnsSafeHost);
			}
		}

		// Token: 0x06000323 RID: 803 RVA: 0x000109F4 File Offset: 0x0000EBF4
		private static string GetExceptionFromQueryString(HttpRequest request)
		{
			string result = string.Empty;
			if (!string.IsNullOrWhiteSpace(request.QueryString["owaError"]))
			{
				result = request.QueryString["owaError"];
			}
			else if (!string.IsNullOrWhiteSpace(request.QueryString["jitError"]))
			{
				result = request.QueryString["jitError"];
			}
			else if (!string.IsNullOrWhiteSpace(request.QueryString["redirError"]))
			{
				result = request.QueryString["redirError"];
			}
			else if (!string.IsNullOrWhiteSpace(request.QueryString["authError"]))
			{
				result = request.QueryString["authError"];
			}
			else if (!string.IsNullOrWhiteSpace(request.QueryString["httpCode"]))
			{
				result = request.QueryString["httpCode"];
			}
			return result;
		}

		// Token: 0x06000324 RID: 804 RVA: 0x00010ADC File Offset: 0x0000ECDC
		private static string GetProbableExceptionNameForAria(HttpRequest request, out string trace)
		{
			string exceptionFromQueryString = ErrorFE.GetExceptionFromQueryString(request);
			trace = exceptionFromQueryString;
			string result;
			if (exceptionFromQueryString.StartsWith("ScriptLoadError"))
			{
				result = "ScriptLoadError";
			}
			else if (exceptionFromQueryString.StartsWith("ClientError"))
			{
				result = "ClientError";
			}
			else if (exceptionFromQueryString.StartsWith("USRLoadError"))
			{
				result = "USRLoadError";
			}
			else if (exceptionFromQueryString.StartsWith("SessionDataError"))
			{
				result = "SessionDataError";
			}
			else if (exceptionFromQueryString.StartsWith("Unknown;"))
			{
				result = "Unknown;";
			}
			else if (exceptionFromQueryString.LastIndexOf('.') > -1)
			{
				result = exceptionFromQueryString.Substring(exceptionFromQueryString.LastIndexOf('.') + 1);
				trace = string.Empty;
			}
			else
			{
				result = exceptionFromQueryString;
				trace = string.Empty;
			}
			return result;
		}

		// Token: 0x06000325 RID: 805 RVA: 0x00010B8E File Offset: 0x0000ED8E
		private static void AddKeyValueWhenNotEmptyOrNullAndNotDup(Dictionary<string, string> dictionary, string key, string value)
		{
			if (!dictionary.ContainsKey(key) && !string.IsNullOrEmpty(value))
			{
				dictionary.Add(key, value);
			}
		}

		// Token: 0x06000326 RID: 806 RVA: 0x00010BAC File Offset: 0x0000EDAC
		private void SetHTTPResponseStatusCode()
		{
			if (this.errorInformation.HttpCode == 200 || this.errorInformation.HttpCode == 302)
			{
				base.Response.StatusCode = 500;
				return;
			}
			base.Response.StatusCode = this.errorInformation.HttpCode;
		}

		// Token: 0x06000327 RID: 807 RVA: 0x00010C04 File Offset: 0x0000EE04
		private bool IsErrorFEQueryParameterNull(string queryParam)
		{
			return this.IsErrorValueNull(HttpContext.Current.Request.QueryString[queryParam]);
		}

		// Token: 0x06000328 RID: 808 RVA: 0x00010C21 File Offset: 0x0000EE21
		private bool IsErrorValueNull(string value)
		{
			return string.IsNullOrWhiteSpace(value) || value == "null";
		}

		// Token: 0x06000329 RID: 809 RVA: 0x00010C38 File Offset: 0x0000EE38
		private void SetAriaCookie(HttpResponse httpRespose, string exceptionName)
		{
			HttpCookie cookie = new HttpCookie("AriaEx")
			{
				Value = HttpUtility.UrlEncode(exceptionName),
				Secure = true,
				Expires = DateTime.UtcNow.AddMinutes(OwaServerTelemetryLogManager.ErrorFeSuppressDuplicateSignalMinutes())
			};
			httpRespose.Cookies.Add(cookie);
		}

		// Token: 0x0600032A RID: 810 RVA: 0x00010C87 File Offset: 0x0000EE87
		private bool AddQueryParamToDiagInfo(StringBuilder diagnosticInfo, string queryParamName, string displayName, ValidationUtilities.ValidationType type)
		{
			return this.AddValueToDiagInfo(diagnosticInfo, HttpContext.Current.Request.QueryString[queryParamName], displayName, type);
		}

		// Token: 0x0600032B RID: 811 RVA: 0x00010CA8 File Offset: 0x0000EEA8
		private bool AddValueToDiagInfo(StringBuilder diagnosticInfo, string value, string displayName, ValidationUtilities.ValidationType type)
		{
			if (!this.IsErrorValueNull(value))
			{
				if (ValidationUtilities.IsValueValid(value, type))
				{
					diagnosticInfo.Append(displayName + " ");
					diagnosticInfo.AppendLine(value);
					this.renderDiagnosticInfo = true;
					return true;
				}
				if (this.unsafeValueEncoding != null)
				{
					diagnosticInfo.Append(displayName + " ");
					diagnosticInfo.AppendLine(ValidationUtilities.EncodeUnsafeValue(value, this.unsafeValueEncoding));
					this.renderDiagnosticInfo = true;
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600032D RID: 813 RVA: 0x00010D58 File Offset: 0x0000EF58
		// Note: this type is marked as 'beforefieldinit'.
		static ErrorFE()
		{
			Strings.IDs[] array = new Strings.IDs[7];
			RuntimeHelpers.InitializeArray(array, fieldof(<PrivateImplementationDetails>.C3181359425DC88A4FB24C2EC5B370A94026499D).FieldHandle);
			ErrorFE.SafeErrorMessagesNoHtmlEncoding = array;
			Strings.IDs[] array2 = new Strings.IDs[8];
			RuntimeHelpers.InitializeArray(array2, fieldof(<PrivateImplementationDetails>.53A281C428804F4D524AF2453611A61FA1D7DF9B).FieldHandle);
			ErrorFE.MessagesToRenderLogoutLinks = array2;
			ErrorFE.MessagesToRenderLoginLinks = new Strings.IDs[]
			{
				1317300008
			};
		}

		// Token: 0x040001C3 RID: 451
		internal const string RedirectUrl = "redirectUrl";

		// Token: 0x040001C4 RID: 452
		internal const string CafeErrorKey = "CafeError";

		// Token: 0x040001C5 RID: 453
		internal const string TargetSiteDistinguishedName = "targetSiteDistinguishedName";

		// Token: 0x040001C6 RID: 454
		private const string HttpCodeQueryKey = "httpCode";

		// Token: 0x040001C7 RID: 455
		private const string ErrorMessageQueryKey = "msg";

		// Token: 0x040001C8 RID: 456
		private const string ErrorMessageParameterQueryKey = "msgParam";

		// Token: 0x040001C9 RID: 457
		private const string SharePointAppQueryKey = "sharepointapp";

		// Token: 0x040001CA RID: 458
		private const string SiteMailboxQueryKey = "sm";

		// Token: 0x040001CB RID: 459
		private const string GroupMailboxQueryKey = "gm";

		// Token: 0x040001CC RID: 460
		private const string ConversationsDestination = "conv";

		// Token: 0x040001CD RID: 461
		private const string CalendarDestination = "cal";

		// Token: 0x040001CE RID: 462
		private const string OfflineEnabledParameterName = "offline";

		// Token: 0x040001CF RID: 463
		private const string Form15ErrorPlt1MsgValue = "FormErr";

		// Token: 0x040001D0 RID: 464
		private const string Plt1CIdParameter = "cid";

		// Token: 0x040001D1 RID: 465
		private const string Plt1ErrParameter = "Err";

		// Token: 0x040001D2 RID: 466
		private const string Plt1PerfReqQueryParameter = "PLT";

		// Token: 0x040001D3 RID: 467
		private const string Plt1PerfReqQueryParameterValue = "now,0";

		// Token: 0x040001D4 RID: 468
		private const string UserEnabledOffline = "off";

		// Token: 0x040001D5 RID: 469
		private const string QueryParameterFormat = "&{0}={1}";

		// Token: 0x040001D6 RID: 470
		private const string ScriptLoadError = "ScriptLoadError";

		// Token: 0x040001D7 RID: 471
		private const string ClientError = "ClientError";

		// Token: 0x040001D8 RID: 472
		private const string USRLoadError = "USRLoadError";

		// Token: 0x040001D9 RID: 473
		private const string SessionDataError = "SessionDataError";

		// Token: 0x040001DA RID: 474
		private const string UnknownError = "Unknown;";

		// Token: 0x040001DB RID: 475
		private const string AriaCookieName = "AriaEx";

		// Token: 0x040001DC RID: 476
		private static readonly Strings.IDs[] SafeErrorMessagesNoHtmlEncoding;

		// Token: 0x040001DD RID: 477
		private static readonly Strings.IDs[] MessagesToRenderLogoutLinks;

		// Token: 0x040001DE RID: 478
		private static readonly Strings.IDs[] MessagesToRenderLoginLinks;

		// Token: 0x040001DF RID: 479
		private ErrorInformation errorInformation;

		// Token: 0x040001E0 RID: 480
		private bool renderDiagnosticInfo;

		// Token: 0x040001E1 RID: 481
		private string diagnosticInfo = string.Empty;

		// Token: 0x040001E2 RID: 482
		private string resourcePath;

		// Token: 0x040001E3 RID: 483
		private bool isIE = true;

		// Token: 0x040001E4 RID: 484
		private bool isConsumerRequest;

		// Token: 0x040001E5 RID: 485
		private UnsafeValueEncodingType unsafeValueEncoding = ClientsCommonConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).ErrorPageSettings.ErrorFEUnsafeValueEncoding;

		// Token: 0x020000FB RID: 251
		internal enum FEErrorCodes
		{
			// Token: 0x040004A7 RID: 1191
			Unknown,
			// Token: 0x040004A8 RID: 1192
			SSLCertificateProblem,
			// Token: 0x040004A9 RID: 1193
			CAS14WithNoWIA,
			// Token: 0x040004AA RID: 1194
			NoFbaSSL,
			// Token: 0x040004AB RID: 1195
			NoLegacyCAS,
			// Token: 0x040004AC RID: 1196
			CasRedirect
		}
	}
}
