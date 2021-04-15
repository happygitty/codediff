using System;
using System.Globalization;
using Microsoft.Exchange.ExchangeSystem;
using Microsoft.Exchange.Extensions;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200004E RID: 78
	internal static class BackEndCookieEntryParser
	{
		// Token: 0x06000280 RID: 640 RVA: 0x0000CDE8 File Offset: 0x0000AFE8
		public static BackEndCookieEntryBase Parse(string entryValue)
		{
			BackEndCookieEntryBase result = null;
			if (!BackEndCookieEntryParser.TryParse(entryValue, out result))
			{
				throw new InvalidBackEndCookieException();
			}
			return result;
		}

		// Token: 0x06000281 RID: 641 RVA: 0x0000CE08 File Offset: 0x0000B008
		public static bool TryParse(string entryValue, out BackEndCookieEntryBase cookieEntry)
		{
			string text = null;
			return BackEndCookieEntryParser.TryParse(entryValue, out cookieEntry, out text);
		}

		// Token: 0x06000282 RID: 642 RVA: 0x0000CE20 File Offset: 0x0000B020
		public static bool TryParse(string entryValue, out BackEndCookieEntryBase cookieEntry, out string clearCookie)
		{
			cookieEntry = null;
			clearCookie = null;
			if (string.IsNullOrEmpty(entryValue))
			{
				return false;
			}
			bool result;
			try
			{
				string text = BackEndCookieEntryParser.UnObscurify(entryValue);
				clearCookie = text;
				string[] array = StringExtensions.SplitFast(text, '~', int.MaxValue, StringSplitOptions.None);
				if (array.Length < 4)
				{
					result = false;
				}
				else
				{
					BackEndCookieEntryType backEndCookieEntryType;
					if (!BackEndCookieEntryBase.TryGetBackEndCookieEntryTypeFromString(array[0], out backEndCookieEntryType))
					{
						backEndCookieEntryType = (BackEndCookieEntryType)Enum.Parse(typeof(BackEndCookieEntryType), array[0], true);
					}
					ExDateTime expiryTime;
					if (!BackEndCookieEntryParser.TryParseDateTime(array[3], out expiryTime))
					{
						result = false;
					}
					else if (backEndCookieEntryType != BackEndCookieEntryType.Server)
					{
						if (backEndCookieEntryType == BackEndCookieEntryType.Database)
						{
							Guid database = new Guid(array[1]);
							string text2 = string.IsNullOrEmpty(array[2]) ? null : array[2];
							string resourceForest = (array.Length < 5 || string.IsNullOrEmpty(array[4])) ? null : array[4];
							if (array.Length >= 6)
							{
								bool isOrganizationMailboxDatabase = string.Equals(array[5], 1.ToString(), StringComparison.OrdinalIgnoreCase);
								cookieEntry = new BackEndDatabaseOrganizationAwareCookieEntry(database, text2, resourceForest, expiryTime, isOrganizationMailboxDatabase);
							}
							else if (array.Length == 5)
							{
								cookieEntry = new BackEndDatabaseResourceForestCookieEntry(database, text2, resourceForest, expiryTime);
							}
							else
							{
								cookieEntry = new BackEndDatabaseCookieEntry(database, text2, expiryTime);
							}
							result = true;
						}
						else
						{
							result = false;
						}
					}
					else
					{
						cookieEntry = new BackEndServerCookieEntry(array[1], int.Parse(array[2]), expiryTime);
						result = true;
					}
				}
			}
			catch (ArgumentException)
			{
				result = false;
			}
			catch (FormatException)
			{
				result = false;
			}
			return result;
		}

		// Token: 0x06000283 RID: 643 RVA: 0x0000CF94 File Offset: 0x0000B194
		internal static string UnObscurify(string obscureString)
		{
			byte[] array = Convert.FromBase64String(obscureString);
			byte[] array2 = new byte[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				byte[] array3 = array2;
				int num = i;
				byte[] array4 = array;
				int num2 = i;
				array3[num] = (array4[num2] ^= BackEndCookieEntryBase.ObfuscateValue);
			}
			return BackEndCookieEntryBase.Encoding.GetString(array2);
		}

		// Token: 0x06000284 RID: 644 RVA: 0x0000CFE4 File Offset: 0x0000B1E4
		private static bool TryParseDateTime(string dateTimeString, out ExDateTime dateTime)
		{
			if (!string.IsNullOrEmpty(dateTimeString))
			{
				try
				{
					dateTime = ExDateTime.Parse(dateTimeString);
					return true;
				}
				catch (ArgumentException)
				{
				}
				catch (FormatException)
				{
				}
				try
				{
					dateTime = ExDateTime.Parse(dateTimeString, CultureInfo.InvariantCulture);
					return true;
				}
				catch (ArgumentException)
				{
				}
				catch (FormatException)
				{
				}
			}
			dateTime = default(ExDateTime);
			return false;
		}

		// Token: 0x04000190 RID: 400
		private const char CookieSeparator = '~';
	}
}
