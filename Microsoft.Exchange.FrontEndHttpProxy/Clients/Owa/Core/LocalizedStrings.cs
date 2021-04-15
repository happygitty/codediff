using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Exchange.HttpProxy;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000006 RID: 6
	public static class LocalizedStrings
	{
		// Token: 0x0600003C RID: 60 RVA: 0x00002F3E File Offset: 0x0000113E
		public static string GetHtmlEncoded(Strings.IDs localizedID)
		{
			return LocalizedStrings.GetHtmlEncodedInternal(Culture.GetUserCulture().Name, localizedID);
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002F50 File Offset: 0x00001150
		public static string GetHtmlEncoded(Strings.IDs localizedID, CultureInfo userCulture)
		{
			return LocalizedStrings.GetHtmlEncodedInternal(userCulture.Name, localizedID);
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002F5E File Offset: 0x0000115E
		public static string GetHtmlEncodedFromKey(string key, Strings.IDs localizedId)
		{
			return LocalizedStrings.GetHtmlEncodedInternal(key, localizedId);
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002F68 File Offset: 0x00001168
		internal static string GetHtmlEncodedInternal(string key, Strings.IDs localizedID)
		{
			Dictionary<uint, string> dictionary = null;
			object obj = LocalizedStrings.htmlEncodedStringsCollection[key];
			if (obj == null)
			{
				Hashtable obj2 = LocalizedStrings.htmlEncodedStringsCollection;
				lock (obj2)
				{
					if (LocalizedStrings.htmlEncodedStringsCollection[key] == null)
					{
						Strings.IDs[] array = (Strings.IDs[])Enum.GetValues(typeof(Strings.IDs));
						dictionary = new Dictionary<uint, string>(array.Length);
						for (int i = 0; i < array.Length; i++)
						{
							dictionary[array[i]] = EncodingUtilities.HtmlEncode(Strings.GetLocalizedString(array[i]));
						}
						LocalizedStrings.htmlEncodedStringsCollection[key] = dictionary;
						goto IL_B0;
					}
					dictionary = (Dictionary<uint, string>)LocalizedStrings.htmlEncodedStringsCollection[key];
					goto IL_B0;
				}
			}
			dictionary = (Dictionary<uint, string>)obj;
			IL_B0:
			return dictionary[localizedID];
		}

		// Token: 0x04000026 RID: 38
		private static Hashtable htmlEncodedStringsCollection = new Hashtable();
	}
}
