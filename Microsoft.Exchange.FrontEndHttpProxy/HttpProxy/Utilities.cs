using System;
using System.Globalization;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authorization;
using Microsoft.Exchange.SoapWebClient.EWS;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000084 RID: 132
	public static class Utilities
	{
		// Token: 0x170000FE RID: 254
		// (get) Token: 0x0600045D RID: 1117 RVA: 0x000188E9 File Offset: 0x00016AE9
		public static bool IsPartnerHostedOnly
		{
			get
			{
				return HttpProxyGlobals.IsPartnerHostedOnly;
			}
		}

		// Token: 0x0600045E RID: 1118 RVA: 0x000188F0 File Offset: 0x00016AF0
		public static BrowserType GetBrowserType(string userAgent)
		{
			if (userAgent == null)
			{
				return BrowserType.Other;
			}
			string a = null;
			string text = null;
			UserAgentParser.UserAgentVersion userAgentVersion;
			UserAgentParser.Parse(userAgent, out a, out userAgentVersion, out text);
			if (string.Equals(a, "MSIE", StringComparison.OrdinalIgnoreCase))
			{
				return BrowserType.IE;
			}
			if (string.Equals(a, "Opera", StringComparison.OrdinalIgnoreCase))
			{
				return BrowserType.Opera;
			}
			if (string.Equals(a, "Safari", StringComparison.OrdinalIgnoreCase))
			{
				return BrowserType.Safari;
			}
			if (string.Equals(a, "Firefox", StringComparison.OrdinalIgnoreCase))
			{
				return BrowserType.Firefox;
			}
			if (string.Equals(a, "Chrome", StringComparison.OrdinalIgnoreCase))
			{
				return BrowserType.Chrome;
			}
			return BrowserType.Other;
		}

		// Token: 0x0600045F RID: 1119 RVA: 0x00018963 File Offset: 0x00016B63
		public static bool IsViet()
		{
			return Utilities.IsViet(Culture.GetUserCulture());
		}

		// Token: 0x06000460 RID: 1120 RVA: 0x0001896F File Offset: 0x00016B6F
		public static bool IsViet(CultureInfo userCulture)
		{
			if (userCulture == null)
			{
				throw new ArgumentNullException("userCulture");
			}
			return userCulture.LCID == 1066;
		}

		// Token: 0x06000461 RID: 1121 RVA: 0x0001898C File Offset: 0x00016B8C
		internal static SidAndAttributesType[] SidStringAndAttributesConverter(SidStringAndAttributes[] sidStringAndAttributesArray)
		{
			if (sidStringAndAttributesArray == null)
			{
				return null;
			}
			SidAndAttributesType[] array = new SidAndAttributesType[sidStringAndAttributesArray.Length];
			for (int i = 0; i < sidStringAndAttributesArray.Length; i++)
			{
				array[i] = new SidAndAttributesType
				{
					SecurityIdentifier = sidStringAndAttributesArray[i].SecurityIdentifier,
					Attributes = sidStringAndAttributesArray[i].Attributes
				};
			}
			return array;
		}

		// Token: 0x06000462 RID: 1122 RVA: 0x000189DC File Offset: 0x00016BDC
		internal static string FormatServerVersion(int serverVersion)
		{
			ServerVersion serverVersion2 = new ServerVersion(serverVersion);
			return string.Format(CultureInfo.InvariantCulture, "{0:d}.{1:d2}.{2:d4}.{3:d3}", new object[]
			{
				serverVersion2.Major,
				serverVersion2.Minor,
				serverVersion2.Build,
				serverVersion2.Revision
			});
		}

		// Token: 0x06000463 RID: 1123 RVA: 0x00018A40 File Offset: 0x00016C40
		internal static string NormalizeExchClientVer(string version)
		{
			if (string.IsNullOrWhiteSpace(version))
			{
				return version;
			}
			string[] array = version.Split(new char[]
			{
				'.'
			});
			return string.Join(".", new string[]
			{
				array[0],
				(array.Length > 1) ? array[1] : "0",
				(array.Length > 2) ? array[2] : "1",
				(array.Length > 3) ? array[3] : "0"
			});
		}

		// Token: 0x06000464 RID: 1124 RVA: 0x00018ABA File Offset: 0x00016CBA
		internal static string GetTruncatedString(string inputString, int maxLength)
		{
			if (string.IsNullOrEmpty(inputString) || maxLength <= 0)
			{
				return inputString;
			}
			if (inputString.Length <= maxLength)
			{
				return inputString;
			}
			return inputString.Substring(0, maxLength);
		}

		// Token: 0x06000465 RID: 1125 RVA: 0x00018AE0 File Offset: 0x00016CE0
		internal static bool TryGetSiteNameFromServerFqdn(string serverFqdn, out string siteName)
		{
			siteName = string.Empty;
			if (string.IsNullOrEmpty(serverFqdn))
			{
				throw new ArgumentNullException("serverFqdn");
			}
			string[] array = serverFqdn.Split(new char[]
			{
				'.'
			});
			if ((Utilities.IsPartnerHostedOnly || CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).SiteNameFromServerFqdnTranslation.Enabled) && array[0].Length > 5)
			{
				siteName = array[0].Substring(0, array[0].Length - 5);
				return true;
			}
			siteName = array[0];
			return true;
		}

		// Token: 0x06000466 RID: 1126 RVA: 0x00018B60 File Offset: 0x00016D60
		internal static ServerVersion ConvertToServerVersion(string version)
		{
			if (string.IsNullOrEmpty(version))
			{
				return null;
			}
			Version version2 = Version.Parse(version);
			return new ServerVersion(version2.Major, version2.Minor, version2.Build, version2.Revision);
		}
	}
}
