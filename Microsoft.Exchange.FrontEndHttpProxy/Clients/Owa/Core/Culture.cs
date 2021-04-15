using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000003 RID: 3
	public static class Culture
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public static bool IsRtl
		{
			get
			{
				return Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft;
			}
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002066 File Offset: 0x00000266
		public static string GetCssFontFileNameFromCulture()
		{
			return Culture.GetCssFontFileNameFromCulture(false);
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002070 File Offset: 0x00000270
		public static string GetCssFontFileNameFromCulture(bool isBasicExperience)
		{
			string text = Culture.GetCssFontFileNameFromCulture(Culture.GetUserCulture());
			if (isBasicExperience)
			{
				text = "basic_" + text;
			}
			return text;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002098 File Offset: 0x00000298
		public static string GetCssFontFileNameFromCulture(CultureInfo culture)
		{
			string text = null;
			Culture.fontFileNameTable.TryGetValue(culture.LCID, out text);
			if (string.IsNullOrEmpty(text))
			{
				return "owafont.css";
			}
			return text;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000020C9 File Offset: 0x000002C9
		public static Culture.SingularPluralRegularExpression GetSingularPluralRegularExpressions(int lcid)
		{
			if (Culture.regularExpressionMap.ContainsKey(lcid))
			{
				return Culture.regularExpressionMap[lcid];
			}
			return Culture.defaultRegularExpression;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000020E9 File Offset: 0x000002E9
		public static CultureInfo[] GetSupportedCultures()
		{
			return Culture.GetSupportedCultures(false);
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000020F1 File Offset: 0x000002F1
		public static CultureInfo[] GetSupportedCultures(bool sortByName)
		{
			if (!sortByName)
			{
				return Culture.supportedCultureInfosSortedByLcid;
			}
			return Culture.CreateSortedSupportedCultures(sortByName);
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002102 File Offset: 0x00000302
		public static bool IsSupportedCulture(CultureInfo culture)
		{
			return Culture.supportedCultureInfos.Contains(culture);
		}

		// Token: 0x06000009 RID: 9 RVA: 0x0000210F File Offset: 0x0000030F
		public static bool IsSupportedCulture(int lcid)
		{
			return lcid > 0 && Culture.IsSupportedCulture(CultureInfo.GetCultureInfo(lcid));
		}

		// Token: 0x0600000A RID: 10 RVA: 0x00002124 File Offset: 0x00000324
		internal static string LookUpHelpDirectoryForCulture(CultureInfo culture)
		{
			string text = null;
			string str = HttpRuntime.AppDomainAppPath + "help\\";
			while (!culture.Equals(CultureInfo.InvariantCulture))
			{
				text = culture.Name;
				if (Directory.Exists(str + text))
				{
					break;
				}
				culture = culture.Parent;
			}
			return text;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002170 File Offset: 0x00000370
		internal static CultureInfo GetUserCulture()
		{
			return Thread.CurrentThread.CurrentCulture;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000217C File Offset: 0x0000037C
		private static Dictionary<int, string> LoadFontFileNameDictionary()
		{
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			dictionary[31748] = (dictionary[3076] = (dictionary[5124] = (dictionary[1028] = "owafont_zh_cht.css")));
			dictionary[4] = (dictionary[4100] = (dictionary[2052] = "owafont_zh_chs.css"));
			dictionary[17] = (dictionary[1041] = "owafont_ja.css");
			dictionary[18] = (dictionary[1042] = "owafont_ko.css");
			dictionary[1066] = "owafont_vi.css";
			return dictionary;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002238 File Offset: 0x00000438
		private static Dictionary<int, Culture.SingularPluralRegularExpression> LoadRemindersDueInRegularExpressionsMap()
		{
			Dictionary<int, Culture.SingularPluralRegularExpression> dictionary = new Dictionary<int, Culture.SingularPluralRegularExpression>();
			Culture.SingularPluralRegularExpression value = new Culture.SingularPluralRegularExpression("^1$|[^1]1$", "^[234]$|[^1][234]$");
			Culture.SingularPluralRegularExpression value2 = new Culture.SingularPluralRegularExpression(".", "^[234]$");
			dictionary[1029] = value2;
			dictionary[1051] = value2;
			dictionary[1060] = value2;
			dictionary[1058] = value2;
			dictionary[1045] = new Culture.SingularPluralRegularExpression(".", "^[234]$|[^1][234]$");
			dictionary[1049] = value;
			dictionary[2074] = value;
			dictionary[3098] = value;
			dictionary[1063] = value;
			dictionary[1062] = value;
			return dictionary;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000022F4 File Offset: 0x000004F4
		private static Dictionary<string, string[]> LoadOneLetterDayNamesMap()
		{
			Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			string[] array = new string[]
			{
				Encoding.Unicode.GetString(new byte[]
				{
					229,
					101
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					0,
					78
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					140,
					78
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					9,
					78
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					219,
					86
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					148,
					78
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					109,
					81
				})
			};
			dictionary["zh-MO"] = array;
			dictionary["zh-TW"] = array;
			dictionary["zh-CN"] = array;
			dictionary["zh-SG"] = array;
			array = new string[]
			{
				Encoding.Unicode.GetString(new byte[]
				{
					45,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					70,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					43,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					49,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					46,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					44,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					51,
					6
				})
			};
			dictionary["ar"] = array;
			dictionary["ar-SA"] = array;
			dictionary["ar-IQ"] = array;
			dictionary["ar-EG"] = array;
			dictionary["ar-LY"] = array;
			dictionary["ar-DZ"] = array;
			dictionary["ar-MA"] = array;
			dictionary["ar-TN"] = array;
			dictionary["ar-OM"] = array;
			dictionary["ar-YE"] = array;
			dictionary["ar-SY"] = array;
			dictionary["ar-JO"] = array;
			dictionary["ar-LB"] = array;
			dictionary["ar-KW"] = array;
			dictionary["ar-AE"] = array;
			dictionary["ar-BH"] = array;
			dictionary["ar-QA"] = array;
			array = new string[]
			{
				Encoding.Unicode.GetString(new byte[]
				{
					204,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					47,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					51,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					134,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					126,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					44,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					52,
					6
				})
			};
			dictionary["fa"] = array;
			dictionary["fa-IR"] = array;
			array = new string[]
			{
				Encoding.Unicode.GetString(new byte[]
				{
					208,
					5
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					209,
					5
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					210,
					5
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					211,
					5
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					212,
					5
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					213,
					5
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					233,
					5
				})
			};
			dictionary["he"] = array;
			dictionary["he-IL"] = array;
			dictionary["hi"] = new string[]
			{
				Encoding.Unicode.GetString(new byte[]
				{
					48,
					9
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					56,
					9,
					75,
					9
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					46,
					9,
					2,
					9
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					44,
					9,
					65,
					9
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					23,
					9,
					65,
					9
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					54,
					9,
					65,
					9
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					54,
					9
				})
			};
			dictionary["th"] = new string[]
			{
				Encoding.Unicode.GetString(new byte[]
				{
					45,
					14
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					8,
					14
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					45,
					14
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					30,
					14
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					30,
					14
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					40,
					14
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					42,
					14
				})
			};
			array = new string[]
			{
				"I",
				"A",
				"R",
				"Z",
				"G",
				"O",
				"L"
			};
			dictionary["eu"] = array;
			dictionary["eu-ES"] = array;
			array = new string[]
			{
				"D",
				"L",
				"M",
				"X",
				"J",
				"V",
				"S"
			};
			dictionary["ca"] = array;
			dictionary["ca-ES"] = array;
			array = new string[]
			{
				"s",
				"m",
				"t",
				"w",
				"t",
				"f",
				"s"
			};
			dictionary["vi"] = array;
			dictionary["vi-VN"] = array;
			array = new string[]
			{
				Encoding.Unicode.GetString(new byte[]
				{
					39,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					126,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					69,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					40,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					44,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					44,
					6
				}),
				Encoding.Unicode.GetString(new byte[]
				{
					71,
					6
				})
			};
			dictionary["ur"] = array;
			dictionary["ur-PK"] = array;
			CultureInfo[] supportedCultures = Culture.GetSupportedCultures();
			for (int i = 0; i < supportedCultures.Length; i++)
			{
				if (!dictionary.ContainsKey(supportedCultures[i].Name))
				{
					string[] abbreviatedDayNames = supportedCultures[i].DateTimeFormat.AbbreviatedDayNames;
					array = new string[7];
					for (int j = 0; j < abbreviatedDayNames.Length; j++)
					{
						array[j] = abbreviatedDayNames[j][0].ToString();
					}
					dictionary[supportedCultures[i].Name] = array;
				}
			}
			return dictionary;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002BAC File Offset: 0x00000DAC
		private static int CompareCultureNames(CultureInfo x, CultureInfo y)
		{
			return string.Compare(x.NativeName, y.NativeName, StringComparison.CurrentCulture);
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002BC0 File Offset: 0x00000DC0
		private static int CompareCultureLCIDs(CultureInfo x, CultureInfo y)
		{
			return x.LCID.CompareTo(y.LCID);
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002BE4 File Offset: 0x00000DE4
		private static List<CultureInfo> CreateCultureInfosFromNames(string[] cultureNames)
		{
			List<CultureInfo> list = new List<CultureInfo>(cultureNames.Length);
			foreach (string name in cultureNames)
			{
				list.Add(CultureInfo.GetCultureInfo(name));
			}
			return list;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002C1C File Offset: 0x00000E1C
		private static CultureInfo[] CreateSortedSupportedCultures(bool sortByName)
		{
			CultureInfo[] array = new CultureInfo[Culture.supportedCultureInfos.Count];
			int num = 0;
			foreach (CultureInfo cultureInfo in Culture.supportedCultureInfos)
			{
				array[num++] = CultureInfo.GetCultureInfo(cultureInfo.LCID);
			}
			if (sortByName)
			{
				Array.Sort<CultureInfo>(array, new Comparison<CultureInfo>(Culture.CompareCultureNames));
			}
			else
			{
				Array.Sort<CultureInfo>(array, new Comparison<CultureInfo>(Culture.CompareCultureLCIDs));
			}
			return array;
		}

		// Token: 0x04000008 RID: 8
		public const string LtrDirectionMark = "&#x200E;";

		// Token: 0x04000009 RID: 9
		public const string RtlDirectionMark = "&#x200F;";

		// Token: 0x0400000A RID: 10
		private const string DefaultSingularExpression = "^1$";

		// Token: 0x0400000B RID: 11
		private const string DefaultPluralExpression = ".";

		// Token: 0x0400000C RID: 12
		private const string CzechPluralExpression = "^[234]$";

		// Token: 0x0400000D RID: 13
		private const string RussianOrPolishPluralExpression = "^[234]$|[^1][234]$";

		// Token: 0x0400000E RID: 14
		private const string RussianSingularExpression = "^1$|[^1]1$";

		// Token: 0x0400000F RID: 15
		private const string DefaultCssFontFileName = "owafont.css";

		// Token: 0x04000010 RID: 16
		private static readonly List<CultureInfo> supportedCultureInfos = ClientCultures.SupportedCultureInfos;

		// Token: 0x04000011 RID: 17
		private static readonly CultureInfo[] supportedCultureInfosSortedByLcid = Culture.CreateSortedSupportedCultures(false);

		// Token: 0x04000012 RID: 18
		private static readonly Dictionary<int, Culture.SingularPluralRegularExpression> regularExpressionMap = Culture.LoadRemindersDueInRegularExpressionsMap();

		// Token: 0x04000013 RID: 19
		private static readonly Dictionary<string, string[]> oneLetterDayNamesMap = Culture.LoadOneLetterDayNamesMap();

		// Token: 0x04000014 RID: 20
		private static Culture.SingularPluralRegularExpression defaultRegularExpression = new Culture.SingularPluralRegularExpression("^1$", ".");

		// Token: 0x04000015 RID: 21
		private static Dictionary<int, string> fontFileNameTable = Culture.LoadFontFileNameDictionary();

		// Token: 0x020000CD RID: 205
		public struct SingularPluralRegularExpression
		{
			// Token: 0x0600079C RID: 1948 RVA: 0x0002C504 File Offset: 0x0002A704
			internal SingularPluralRegularExpression(string singularExpression, string pluralExpression)
			{
				this.singularExpression = singularExpression;
				this.pluralExpression = pluralExpression;
			}

			// Token: 0x170001A7 RID: 423
			// (get) Token: 0x0600079D RID: 1949 RVA: 0x0002C514 File Offset: 0x0002A714
			internal string SingularExpression
			{
				get
				{
					return this.singularExpression;
				}
			}

			// Token: 0x170001A8 RID: 424
			// (get) Token: 0x0600079E RID: 1950 RVA: 0x0002C51C File Offset: 0x0002A71C
			internal string PluralExpression
			{
				get
				{
					return this.pluralExpression;
				}
			}

			// Token: 0x04000452 RID: 1106
			private string singularExpression;

			// Token: 0x04000453 RID: 1107
			private string pluralExpression;
		}
	}
}
