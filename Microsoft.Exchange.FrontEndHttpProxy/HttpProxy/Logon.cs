using System;
using Microsoft.Exchange.Clients;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000065 RID: 101
	public class Logon : OwaPage
	{
		// Token: 0x170000BF RID: 191
		// (get) Token: 0x06000358 RID: 856 RVA: 0x000132DC File Offset: 0x000114DC
		protected static string UserNameLabel
		{
			get
			{
				if (Datacenter.IsPartnerHostedOnly(false))
				{
					return LocalizedStrings.GetHtmlEncoded(1677919363);
				}
				switch (OwaVdirConfiguration.Instance.LogonFormat)
				{
				case 1:
					return LocalizedStrings.GetHtmlEncoded(1677919363);
				case 2:
					return LocalizedStrings.GetHtmlEncoded(537815319);
				}
				return LocalizedStrings.GetHtmlEncoded(78658498);
			}
		}

		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x06000359 RID: 857 RVA: 0x0001333C File Offset: 0x0001153C
		protected static string UserNamePlaceholder
		{
			get
			{
				if (Datacenter.IsPartnerHostedOnly(false))
				{
					return Strings.GetLocalizedString(1677919363);
				}
				switch (OwaVdirConfiguration.Instance.LogonFormat)
				{
				case 1:
					return Strings.GetLocalizedString(-1896713583);
				case 2:
					return Strings.GetLocalizedString(-40289791);
				}
				return Strings.GetLocalizedString(609186145);
			}
		}

		// Token: 0x170000C1 RID: 193
		// (get) Token: 0x0600035A RID: 858 RVA: 0x0001339B File Offset: 0x0001159B
		protected bool ReplaceCurrent
		{
			get
			{
				return base.Request.QueryString["replaceCurrent"] == "1" || base.IsDownLevelClient;
			}
		}

		// Token: 0x170000C2 RID: 194
		// (get) Token: 0x0600035B RID: 859 RVA: 0x000133C9 File Offset: 0x000115C9
		protected bool ShowOwaLightOption
		{
			get
			{
				return !UrlUtilities.IsEcpUrl(this.Destination) && OwaVdirConfiguration.Instance.LightSelectionEnabled;
			}
		}

		// Token: 0x170000C3 RID: 195
		// (get) Token: 0x0600035C RID: 860 RVA: 0x000133E4 File Offset: 0x000115E4
		protected bool ShowPublicPrivateSelection
		{
			get
			{
				return OwaVdirConfiguration.Instance.PublicPrivateSelectionEnabled;
			}
		}

		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x0600035D RID: 861 RVA: 0x00003165 File Offset: 0x00001365
		protected override bool UseStrictMode
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x0600035E RID: 862 RVA: 0x000133F0 File Offset: 0x000115F0
		protected string LogoffUrl
		{
			get
			{
				return base.Request.Url.Scheme + "://" + base.Request.Url.Authority + OwaUrl.Logoff.GetExplicitUrl(base.Request);
			}
		}

		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x0600035F RID: 863 RVA: 0x0001342C File Offset: 0x0001162C
		protected Logon.LogonReason Reason
		{
			get
			{
				string text = base.Request.QueryString["reason"];
				if (text == null)
				{
					return Logon.LogonReason.None;
				}
				if (text == "1")
				{
					return Logon.LogonReason.Logoff;
				}
				if (text == "2")
				{
					return Logon.LogonReason.InvalidCredentials;
				}
				if (text == "3")
				{
					return Logon.LogonReason.Timeout;
				}
				if (!(text == "4"))
				{
					return Logon.LogonReason.None;
				}
				return Logon.LogonReason.ChangePasswordLogoff;
			}
		}

		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x06000360 RID: 864 RVA: 0x00013494 File Offset: 0x00011694
		protected string Destination
		{
			get
			{
				string text = base.Request.QueryString["url"];
				if (text == null || string.Equals(text, this.LogoffUrl, StringComparison.Ordinal))
				{
					return base.Request.GetBaseUrl();
				}
				return text;
			}
		}

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x06000361 RID: 865 RVA: 0x000134D8 File Offset: 0x000116D8
		protected string CloseWindowUrl
		{
			get
			{
				Uri uri;
				string result;
				if (Uri.TryCreate(this.Destination, UriKind.Absolute, out uri) && uri.AbsolutePath.EndsWith("/closewindow.aspx", StringComparison.OrdinalIgnoreCase))
				{
					result = this.Destination;
				}
				else
				{
					result = this.BaseUrl + "?ae=Dialog&t=CloseWindow&exsvurl=1";
				}
				return result;
			}
		}

		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x06000362 RID: 866 RVA: 0x00013523 File Offset: 0x00011723
		protected string PageTitle
		{
			get
			{
				return LocalizedStrings.GetHtmlEncoded(this.IsEcpDestination ? 1018921346 : -1066333875);
			}
		}

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x06000363 RID: 867 RVA: 0x0001353E File Offset: 0x0001173E
		protected string SignInHeader
		{
			get
			{
				return LocalizedStrings.GetHtmlEncoded(this.IsEcpDestination ? 1018921346 : -740205329);
			}
		}

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x06000364 RID: 868 RVA: 0x00013559 File Offset: 0x00011759
		protected bool IsEcpDestination
		{
			get
			{
				return UrlUtilities.IsEacUrl(this.Destination);
			}
		}

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x06000365 RID: 869 RVA: 0x00013566 File Offset: 0x00011766
		protected string LoadFailedMessageValue
		{
			get
			{
				return "logon page loaded";
			}
		}

		// Token: 0x170000CD RID: 205
		// (get) Token: 0x06000366 RID: 870 RVA: 0x0001356D File Offset: 0x0001176D
		private string BaseUrl
		{
			get
			{
				return base.Request.Url.Scheme + "://" + base.Request.Url.Authority + OwaUrl.ApplicationRoot.GetExplicitUrl(base.Request);
			}
		}

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x06000367 RID: 871 RVA: 0x000135A9 File Offset: 0x000117A9
		private string Default14Url
		{
			get
			{
				return base.Request.Url.Scheme + "://" + base.Request.Url.Authority + OwaUrl.Default14Page.GetExplicitUrl(base.Request);
			}
		}

		// Token: 0x06000368 RID: 872 RVA: 0x000135E5 File Offset: 0x000117E5
		protected void RenderLogonHref()
		{
			base.Response.Write("logon.aspx?replaceCurrent=1");
			if (this.Reason != Logon.LogonReason.None)
			{
				base.Response.Write("&reason=");
				base.Response.Write((int)this.Reason);
			}
		}

		// Token: 0x06000369 RID: 873 RVA: 0x00013625 File Offset: 0x00011825
		protected override void OnPreRender(EventArgs e)
		{
			base.Response.Headers.Set("X-Frame-Options", "SAMEORIGIN");
			base.OnPreRender(e);
		}

		// Token: 0x04000229 RID: 553
		private const string Option = "<option value=\"{0}\"{1}>{2}</option>";

		// Token: 0x0400022A RID: 554
		private const string DestinationParameter = "url";

		// Token: 0x0400022B RID: 555
		private const string FlagsParameter = "flags";

		// Token: 0x0400022C RID: 556
		private const string LiveIdAuthenticationModuleSaveUrlOnLogoffParameter = "&exsvurl=1";

		// Token: 0x0400022D RID: 557
		private const string EcpCloseWindowUrl = "/closewindow.aspx";

		// Token: 0x02000101 RID: 257
		protected enum LogonReason
		{
			// Token: 0x040004CA RID: 1226
			None,
			// Token: 0x040004CB RID: 1227
			Logoff,
			// Token: 0x040004CC RID: 1228
			InvalidCredentials,
			// Token: 0x040004CD RID: 1229
			Timeout,
			// Token: 0x040004CE RID: 1230
			ChangePasswordLogoff
		}
	}
}
