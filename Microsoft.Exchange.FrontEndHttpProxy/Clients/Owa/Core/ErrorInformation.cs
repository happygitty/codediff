using System;
using Microsoft.Exchange.Clients.Common;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000005 RID: 5
	public class ErrorInformation
	{
		// Token: 0x06000017 RID: 23 RVA: 0x00002D6C File Offset: 0x00000F6C
		public ErrorInformation()
		{
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002D8E File Offset: 0x00000F8E
		public ErrorInformation(int httpCode)
		{
			this.httpCode = httpCode;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002DB7 File Offset: 0x00000FB7
		public ErrorInformation(int httpCode, string details)
		{
			this.httpCode = httpCode;
			this.messageDetails = details;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002DE7 File Offset: 0x00000FE7
		public ErrorInformation(int httpCode, string details, bool sharePointApp)
		{
			this.httpCode = httpCode;
			this.messageDetails = details;
			this.SharePointApp = sharePointApp;
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600001B RID: 27 RVA: 0x00002E1E File Offset: 0x0000101E
		// (set) Token: 0x0600001C RID: 28 RVA: 0x00002E26 File Offset: 0x00001026
		public ErrorMode? ErrorMode { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600001D RID: 29 RVA: 0x00002E2F File Offset: 0x0000102F
		// (set) Token: 0x0600001E RID: 30 RVA: 0x00002E37 File Offset: 0x00001037
		public Exception Exception
		{
			get
			{
				return this.exception;
			}
			set
			{
				this.exception = value;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600001F RID: 31 RVA: 0x00002E40 File Offset: 0x00001040
		// (set) Token: 0x06000020 RID: 32 RVA: 0x00002E48 File Offset: 0x00001048
		public int HttpCode
		{
			get
			{
				return this.httpCode;
			}
			set
			{
				this.httpCode = value;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000021 RID: 33 RVA: 0x00002E51 File Offset: 0x00001051
		// (set) Token: 0x06000022 RID: 34 RVA: 0x00002E59 File Offset: 0x00001059
		public bool IsErrorMessageHtmlEncoded
		{
			get
			{
				return this.isErrorMessageHtmlEncoded;
			}
			set
			{
				this.isErrorMessageHtmlEncoded = value;
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000023 RID: 35 RVA: 0x00002E62 File Offset: 0x00001062
		// (set) Token: 0x06000024 RID: 36 RVA: 0x00002E6A File Offset: 0x0000106A
		public string MessageDetails
		{
			get
			{
				return this.messageDetails;
			}
			set
			{
				this.messageDetails = value;
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000025 RID: 37 RVA: 0x00002E73 File Offset: 0x00001073
		// (set) Token: 0x06000026 RID: 38 RVA: 0x00002E7B File Offset: 0x0000107B
		public bool IsDetailedErrorHtmlEncoded
		{
			get
			{
				return this.isDetailedErrorMessageHtmlEncoded;
			}
			set
			{
				this.isDetailedErrorMessageHtmlEncoded = value;
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000027 RID: 39 RVA: 0x00002E84 File Offset: 0x00001084
		// (set) Token: 0x06000028 RID: 40 RVA: 0x00002E8C File Offset: 0x0000108C
		public ThemeFileId Icon
		{
			get
			{
				return this.icon;
			}
			set
			{
				this.icon = value;
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000029 RID: 41 RVA: 0x00002E95 File Offset: 0x00001095
		// (set) Token: 0x0600002A RID: 42 RVA: 0x00002E9D File Offset: 0x0000109D
		public ThemeFileId Background
		{
			get
			{
				return this.background;
			}
			set
			{
				this.background = value;
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600002B RID: 43 RVA: 0x00002EA6 File Offset: 0x000010A6
		// (set) Token: 0x0600002C RID: 44 RVA: 0x00002EAE File Offset: 0x000010AE
		public OwaUrl OwaUrl
		{
			get
			{
				return this.owaUrl;
			}
			set
			{
				this.owaUrl = value;
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600002D RID: 45 RVA: 0x00002EB7 File Offset: 0x000010B7
		// (set) Token: 0x0600002E RID: 46 RVA: 0x00002EBF File Offset: 0x000010BF
		public string PreviousPageUrl
		{
			get
			{
				return this.previousPageUrl;
			}
			set
			{
				this.previousPageUrl = value;
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600002F RID: 47 RVA: 0x00002EC8 File Offset: 0x000010C8
		// (set) Token: 0x06000030 RID: 48 RVA: 0x00002ED0 File Offset: 0x000010D0
		public string ExternalPageLink
		{
			get
			{
				return this.externalPageUrl;
			}
			set
			{
				this.externalPageUrl = value;
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000031 RID: 49 RVA: 0x00002ED9 File Offset: 0x000010D9
		// (set) Token: 0x06000032 RID: 50 RVA: 0x00002EE1 File Offset: 0x000010E1
		public bool ShowLogoffAndWorkButton
		{
			get
			{
				return this.showLogOffAndContinueBrowse;
			}
			set
			{
				this.showLogOffAndContinueBrowse = value;
			}
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000033 RID: 51 RVA: 0x00002EEA File Offset: 0x000010EA
		// (set) Token: 0x06000034 RID: 52 RVA: 0x00002EF2 File Offset: 0x000010F2
		public bool SharePointApp
		{
			get
			{
				return this.sharePointApp;
			}
			set
			{
				this.sharePointApp = value;
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000035 RID: 53 RVA: 0x00002EFB File Offset: 0x000010FB
		// (set) Token: 0x06000036 RID: 54 RVA: 0x00002F03 File Offset: 0x00001103
		public bool SiteMailbox
		{
			get
			{
				return this.siteMailbox;
			}
			set
			{
				this.siteMailbox = value;
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000037 RID: 55 RVA: 0x00002F0C File Offset: 0x0000110C
		public bool GroupMailbox
		{
			get
			{
				return !string.IsNullOrEmpty(this.groupMailboxDestination);
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000038 RID: 56 RVA: 0x00002F1C File Offset: 0x0000111C
		// (set) Token: 0x06000039 RID: 57 RVA: 0x00002F24 File Offset: 0x00001124
		public string GroupMailboxDestination
		{
			get
			{
				return this.groupMailboxDestination;
			}
			set
			{
				this.groupMailboxDestination = value;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600003A RID: 58 RVA: 0x00002F2D File Offset: 0x0000112D
		// (set) Token: 0x0600003B RID: 59 RVA: 0x00002F35 File Offset: 0x00001135
		public string RedirectionUrl { get; set; }

		// Token: 0x04000016 RID: 22
		private Exception exception;

		// Token: 0x04000017 RID: 23
		private int httpCode;

		// Token: 0x04000018 RID: 24
		private string messageDetails;

		// Token: 0x04000019 RID: 25
		private ThemeFileId icon = ThemeFileId.Error;

		// Token: 0x0400001A RID: 26
		private ThemeFileId background;

		// Token: 0x0400001B RID: 27
		private OwaUrl owaUrl = OwaUrl.ErrorPage;

		// Token: 0x0400001C RID: 28
		private bool isDetailedErrorMessageHtmlEncoded;

		// Token: 0x0400001D RID: 29
		private bool isErrorMessageHtmlEncoded;

		// Token: 0x0400001E RID: 30
		private string previousPageUrl;

		// Token: 0x0400001F RID: 31
		private string externalPageUrl;

		// Token: 0x04000020 RID: 32
		private bool showLogOffAndContinueBrowse = true;

		// Token: 0x04000021 RID: 33
		private bool sharePointApp;

		// Token: 0x04000022 RID: 34
		private bool siteMailbox;

		// Token: 0x04000023 RID: 35
		private string groupMailboxDestination;
	}
}
