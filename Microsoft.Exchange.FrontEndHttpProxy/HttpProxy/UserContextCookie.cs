using System;
using System.Net;
using System.Text;
using System.Web;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000053 RID: 83
	public sealed class UserContextCookie
	{
		// Token: 0x0600029E RID: 670 RVA: 0x0000D2EA File Offset: 0x0000B4EA
		private UserContextCookie(string cookieId, string userContextId, string mailboxUniqueKey)
		{
			this.cookieId = cookieId;
			this.userContextId = userContextId;
			this.mailboxUniqueKey = mailboxUniqueKey;
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x0600029F RID: 671 RVA: 0x0000D307 File Offset: 0x0000B507
		internal HttpCookie HttpCookie
		{
			get
			{
				if (this.httpCookie == null)
				{
					this.httpCookie = new HttpCookie(this.CookieName, this.CookieValue);
				}
				return this.httpCookie;
			}
		}

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x060002A0 RID: 672 RVA: 0x0000D32E File Offset: 0x0000B52E
		internal Cookie NetCookie
		{
			get
			{
				if (this.netCookie == null)
				{
					this.netCookie = new Cookie(this.CookieName, this.CookieValue);
				}
				return this.netCookie;
			}
		}

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x060002A1 RID: 673 RVA: 0x0000D358 File Offset: 0x0000B558
		internal string CookieName
		{
			get
			{
				string text = "UserContext";
				if (this.cookieId != null)
				{
					text = text + "_" + this.cookieId;
				}
				return text;
			}
		}

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x060002A2 RID: 674 RVA: 0x0000D386 File Offset: 0x0000B586
		internal string UserContextId
		{
			get
			{
				return this.userContextId;
			}
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x060002A3 RID: 675 RVA: 0x0000D38E File Offset: 0x0000B58E
		internal string MailboxUniqueKey
		{
			get
			{
				return this.mailboxUniqueKey;
			}
		}

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060002A4 RID: 676 RVA: 0x0000D398 File Offset: 0x0000B598
		internal string CookieValue
		{
			get
			{
				if (this.cookieValue == null)
				{
					this.cookieValue = this.userContextId;
					if (this.mailboxUniqueKey != null)
					{
						byte[] bytes = new UTF8Encoding().GetBytes(this.mailboxUniqueKey);
						this.cookieValue = this.cookieValue + "&" + UserContextCookie.ValidTokenBase64Encode(bytes);
					}
				}
				return this.cookieValue;
			}
		}

		// Token: 0x060002A5 RID: 677 RVA: 0x0000D3F4 File Offset: 0x0000B5F4
		public static string ValidTokenBase64Encode(byte[] byteArray)
		{
			if (byteArray == null)
			{
				throw new ArgumentNullException("byteArray");
			}
			int num = (int)(1.3333333333333333 * (double)byteArray.Length);
			if (num % 4 != 0)
			{
				num += 4 - num % 4;
			}
			char[] array = new char[num];
			Convert.ToBase64CharArray(byteArray, 0, byteArray.Length, array, 0);
			int num2 = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == '\\')
				{
					array[i] = '-';
				}
				else if (array[i] == '=')
				{
					num2++;
				}
			}
			return new string(array, 0, array.Length - num2);
		}

		// Token: 0x060002A6 RID: 678 RVA: 0x0000D478 File Offset: 0x0000B678
		public static byte[] ValidTokenBase64Decode(string tokenValidBase64String)
		{
			if (tokenValidBase64String == null)
			{
				throw new ArgumentNullException("tokenValidBase64String");
			}
			long num = (long)tokenValidBase64String.Length;
			if (tokenValidBase64String.Length % 4 != 0)
			{
				num += (long)(4 - tokenValidBase64String.Length % 4);
			}
			char[] array = new char[num];
			tokenValidBase64String.CopyTo(0, array, 0, tokenValidBase64String.Length);
			for (long num2 = 0L; num2 < (long)tokenValidBase64String.Length; num2 += 1L)
			{
				checked
				{
					if (array[(int)((IntPtr)num2)] == '-')
					{
						array[(int)((IntPtr)num2)] = '\\';
					}
				}
			}
			for (long num3 = (long)tokenValidBase64String.Length; num3 < (long)array.Length; num3 += 1L)
			{
				array[(int)(checked((IntPtr)num3))] = '=';
			}
			return Convert.FromBase64CharArray(array, 0, array.Length);
		}

		// Token: 0x060002A7 RID: 679 RVA: 0x0000D514 File Offset: 0x0000B714
		public static bool IsValidGuid(string guid)
		{
			if (guid == null || guid.Length != 32)
			{
				return false;
			}
			for (int i = 0; i < 32; i++)
			{
				if (!UserContextCookie.IsHexChar(guid[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060002A8 RID: 680 RVA: 0x0000D54E File Offset: 0x0000B74E
		public static bool IsHexChar(char c)
		{
			return char.IsDigit(c) || (char.ToUpperInvariant(c) >= 'A' && char.ToUpperInvariant(c) <= 'F');
		}

		// Token: 0x060002A9 RID: 681 RVA: 0x0000D573 File Offset: 0x0000B773
		public override string ToString()
		{
			return this.CookieName + "=" + this.CookieValue;
		}

		// Token: 0x060002AA RID: 682 RVA: 0x0000D58B File Offset: 0x0000B78B
		internal static UserContextCookie Create(string cookieId, string userContextId, string mailboxUniqueKey)
		{
			return new UserContextCookie(cookieId, userContextId, mailboxUniqueKey);
		}

		// Token: 0x060002AB RID: 683 RVA: 0x0000D598 File Offset: 0x0000B798
		internal static UserContextCookie TryCreateFromHttpCookie(HttpCookie cookie)
		{
			string text = null;
			string text2 = null;
			if (string.IsNullOrEmpty(cookie.Value))
			{
				return null;
			}
			if (!UserContextCookie.TryParseCookieValue(cookie.Value, out text, out text2))
			{
				return null;
			}
			string text3 = null;
			if (!UserContextCookie.TryParseCookieName(cookie.Name, out text3))
			{
				return null;
			}
			return UserContextCookie.Create(text3, text, text2);
		}

		// Token: 0x060002AC RID: 684 RVA: 0x0000D5E8 File Offset: 0x0000B7E8
		internal static UserContextCookie TryCreateFromNetCookie(Cookie cookie)
		{
			string text = null;
			string text2 = null;
			if (string.IsNullOrEmpty(cookie.Value))
			{
				return null;
			}
			if (!UserContextCookie.TryParseCookieValue(cookie.Value, out text, out text2))
			{
				return null;
			}
			string text3 = null;
			if (!UserContextCookie.TryParseCookieName(cookie.Name, out text3))
			{
				return null;
			}
			return UserContextCookie.Create(text3, text, text2);
		}

		// Token: 0x060002AD RID: 685 RVA: 0x0000D638 File Offset: 0x0000B838
		internal static bool TryParseCookieValue(string cookieValue, out string userContextId, out string mailboxUniqueKey)
		{
			userContextId = null;
			mailboxUniqueKey = null;
			if (cookieValue.Length == 32)
			{
				userContextId = cookieValue;
			}
			else
			{
				if (cookieValue.Length < 34)
				{
					return false;
				}
				int num = cookieValue.IndexOf('&');
				if (num != 32)
				{
					return false;
				}
				num++;
				userContextId = cookieValue.Substring(0, num - 1);
				string tokenValidBase64String = cookieValue.Substring(num, cookieValue.Length - num);
				byte[] bytes = null;
				try
				{
					bytes = UserContextCookie.ValidTokenBase64Decode(tokenValidBase64String);
				}
				catch (FormatException)
				{
					return false;
				}
				UTF8Encoding utf8Encoding = new UTF8Encoding();
				mailboxUniqueKey = utf8Encoding.GetString(bytes);
			}
			return UserContextCookie.IsValidUserContextId(userContextId);
		}

		// Token: 0x060002AE RID: 686 RVA: 0x0000D6D8 File Offset: 0x0000B8D8
		internal static bool TryParseCookieName(string cookieName, out string cookieId)
		{
			cookieId = null;
			if (!cookieName.StartsWith("UserContext", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			int length = "UserContext".Length;
			if (cookieName.Length == length)
			{
				return true;
			}
			cookieId = cookieName.Substring(length + 1, cookieName.Length - length - 1);
			return UserContextCookie.IsValidGuid(cookieId);
		}

		// Token: 0x060002AF RID: 687 RVA: 0x0000D72F File Offset: 0x0000B92F
		private static bool IsValidUserContextId(string userContextId)
		{
			return UserContextCookie.IsValidGuid(userContextId);
		}

		// Token: 0x04000197 RID: 407
		public const string UserContextCookiePrefix = "UserContext";

		// Token: 0x04000198 RID: 408
		internal const int UserContextIdLength = 32;

		// Token: 0x04000199 RID: 409
		private readonly string userContextId;

		// Token: 0x0400019A RID: 410
		private readonly string mailboxUniqueKey;

		// Token: 0x0400019B RID: 411
		private readonly string cookieId;

		// Token: 0x0400019C RID: 412
		private HttpCookie httpCookie;

		// Token: 0x0400019D RID: 413
		private Cookie netCookie;

		// Token: 0x0400019E RID: 414
		private string cookieValue;
	}
}
