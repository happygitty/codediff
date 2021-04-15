using System;
using System.Globalization;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x0200000A RID: 10
	internal static class UserAgentParser
	{
		// Token: 0x0600005E RID: 94 RVA: 0x00003954 File Offset: 0x00001B54
		internal static void Parse(string userAgent, out string application, out UserAgentParser.UserAgentVersion version, out string platform)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "Globals.ParseUserAgent. user-agent = {0}", (userAgent != null) ? userAgent : "<null>");
			}
			application = string.Empty;
			version = default(UserAgentParser.UserAgentVersion);
			platform = string.Empty;
			if (userAgent == null || userAgent.Length == 0)
			{
				return;
			}
			int num = int.MinValue;
			int i;
			for (i = 0; i < UserAgentParser.clientApplication.Length; i++)
			{
				if (-1 != (num = userAgent.IndexOf(UserAgentParser.clientApplication[i], StringComparison.OrdinalIgnoreCase)))
				{
					if (string.Equals(UserAgentParser.clientApplication[i], "Safari", StringComparison.OrdinalIgnoreCase))
					{
						if (-1 != userAgent.IndexOf("Chrome", StringComparison.OrdinalIgnoreCase))
						{
							goto IL_BC;
						}
					}
					else if (string.Equals(UserAgentParser.clientApplication[i], UserAgentParser.ie11ApplicationName, StringComparison.OrdinalIgnoreCase) && -1 == userAgent.IndexOf(UserAgentParser.ie11ExtraCheck, StringComparison.OrdinalIgnoreCase))
					{
						goto IL_BC;
					}
					application = UserAgentParser.clientApplication[i];
					break;
				}
				IL_BC:;
			}
			if (i == UserAgentParser.clientApplication.Length)
			{
				return;
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "Parsed out application = {0}", application);
			}
			int num2 = -1;
			if (string.Equals(application, "MSIE", StringComparison.Ordinal) || string.Equals(application, "Firefox", StringComparison.Ordinal) || string.Equals(application, "Chrome", StringComparison.Ordinal))
			{
				num += application.Length + 1;
			}
			else if (string.Equals(application, "Safari", StringComparison.Ordinal))
			{
				string text = "Version/";
				num = userAgent.IndexOf(text) + text.Length;
			}
			else
			{
				if (!string.Equals(application, UserAgentParser.ie11ApplicationName, StringComparison.Ordinal))
				{
					return;
				}
				num += application.Length;
			}
			int j;
			for (j = num; j < userAgent.Length; j++)
			{
				if (!char.IsDigit(userAgent, j) && userAgent[j] != '.')
				{
					num2 = j;
					break;
				}
			}
			if (num2 == -1)
			{
				num2 = userAgent.Length;
			}
			if (j == num)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug(0L, "Unable to parse browser version.  Could not find semicolon");
				}
				return;
			}
			string text2 = userAgent.Substring(num, num2 - num);
			try
			{
				version = new UserAgentParser.UserAgentVersion(text2);
			}
			catch (ArgumentException)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "TryParse failed, unable to parse browser version = {0}", text2);
				}
				return;
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "Parsed out version = {0}", version.ToString());
			}
			for (i = 0; i < UserAgentParser.clientPlatform.Length; i++)
			{
				if (-1 != userAgent.IndexOf(UserAgentParser.clientPlatform[i], StringComparison.OrdinalIgnoreCase))
				{
					platform = UserAgentParser.clientPlatform[i];
					break;
				}
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>(0L, "Parsed out platform = {0}", platform);
			}
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00003BF8 File Offset: 0x00001DF8
		internal static bool IsMonitoringRequest(string userAgent)
		{
			return !string.IsNullOrEmpty(userAgent) && userAgent.IndexOf("MSEXCHMON", StringComparison.OrdinalIgnoreCase) != -1;
		}

		// Token: 0x0400006E RID: 110
		private static string ie11ExtraCheck = "Trident";

		// Token: 0x0400006F RID: 111
		private static string ie11ApplicationName = "rv:";

		// Token: 0x04000070 RID: 112
		private static string[] clientApplication = new string[]
		{
			"Opera",
			"Netscape",
			"MSIE",
			"Safari",
			"Firefox",
			"Chrome",
			"rv:"
		};

		// Token: 0x04000071 RID: 113
		private static string[] clientPlatform = new string[]
		{
			"Windows NT",
			"Windows 98; Win 9x 4.90",
			"Windows 2000",
			"Macintosh",
			"Linux"
		};

		// Token: 0x020000D2 RID: 210
		internal struct UserAgentVersion : IComparable<UserAgentParser.UserAgentVersion>
		{
			// Token: 0x060007AE RID: 1966 RVA: 0x0002C39D File Offset: 0x0002A59D
			public UserAgentVersion(int buildVersion, int majorVersion, int minorVersion)
			{
				this.build = buildVersion;
				this.major = majorVersion;
				this.minor = minorVersion;
			}

			// Token: 0x060007AF RID: 1967 RVA: 0x0002C3B4 File Offset: 0x0002A5B4
			public UserAgentVersion(string version)
			{
				int[] array = new int[3];
				int num = -1;
				int num2 = 0;
				int num3 = 0;
				for (;;)
				{
					num = version.IndexOf('.', num + 1);
					if (num == -1)
					{
						num = version.Length;
					}
					if (!int.TryParse(version.Substring(num3, num - num3), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out array[num2]))
					{
						break;
					}
					num2++;
					num3 = num + 1;
					if (num2 >= array.Length || num >= version.Length)
					{
						goto IL_64;
					}
				}
				throw new ArgumentException("The version parameter is not a valid User Agent Version");
				IL_64:
				this.build = array[0];
				this.major = array[1];
				this.minor = array[2];
			}

			// Token: 0x170001AB RID: 427
			// (get) Token: 0x060007B0 RID: 1968 RVA: 0x0002C440 File Offset: 0x0002A640
			// (set) Token: 0x060007B1 RID: 1969 RVA: 0x0002C448 File Offset: 0x0002A648
			public int Build
			{
				get
				{
					return this.build;
				}
				set
				{
					this.build = value;
				}
			}

			// Token: 0x170001AC RID: 428
			// (get) Token: 0x060007B2 RID: 1970 RVA: 0x0002C451 File Offset: 0x0002A651
			// (set) Token: 0x060007B3 RID: 1971 RVA: 0x0002C459 File Offset: 0x0002A659
			public int Major
			{
				get
				{
					return this.major;
				}
				set
				{
					this.major = value;
				}
			}

			// Token: 0x170001AD RID: 429
			// (get) Token: 0x060007B4 RID: 1972 RVA: 0x0002C462 File Offset: 0x0002A662
			// (set) Token: 0x060007B5 RID: 1973 RVA: 0x0002C46A File Offset: 0x0002A66A
			public int Minor
			{
				get
				{
					return this.minor;
				}
				set
				{
					this.minor = value;
				}
			}

			// Token: 0x060007B6 RID: 1974 RVA: 0x0002C473 File Offset: 0x0002A673
			public override string ToString()
			{
				return string.Format("{0}.{1}.{2}", this.Build, this.Major, this.Minor);
			}

			// Token: 0x060007B7 RID: 1975 RVA: 0x0002C4A0 File Offset: 0x0002A6A0
			public int CompareTo(UserAgentParser.UserAgentVersion userAgentVersionComparand)
			{
				int num = (this.Minor.ToString().Length > userAgentVersionComparand.Minor.ToString().Length) ? this.Minor.ToString().Length : userAgentVersionComparand.Minor.ToString().Length;
				int num2 = (this.Major.ToString().Length > userAgentVersionComparand.Major.ToString().Length) ? this.Major.ToString().Length : userAgentVersionComparand.Major.ToString().Length;
				int num3 = this.Minor + (int)Math.Pow(10.0, (double)num) * this.Major + (int)Math.Pow(10.0, (double)(num2 + num)) * this.Build;
				num = userAgentVersionComparand.Minor.ToString().Length;
				int num4 = userAgentVersionComparand.Minor + (int)Math.Pow(10.0, (double)num) * userAgentVersionComparand.Major + (int)Math.Pow(10.0, (double)(num2 + num)) * userAgentVersionComparand.Build;
				return num3 - num4;
			}

			// Token: 0x04000454 RID: 1108
			private const string FormatToString = "{0}.{1}.{2}";

			// Token: 0x04000455 RID: 1109
			private int build;

			// Token: 0x04000456 RID: 1110
			private int major;

			// Token: 0x04000457 RID: 1111
			private int minor;
		}
	}
}
