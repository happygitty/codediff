﻿using System;
using System.IO;
using System.Text;
using System.Web.Security.AntiXss;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000085 RID: 133
	public static class EncodingUtilities
	{
		// Token: 0x06000463 RID: 1123 RVA: 0x000189DB File Offset: 0x00016BDB
		public static string EncodeToBase64(string input)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
		}

		// Token: 0x06000464 RID: 1124 RVA: 0x000189ED File Offset: 0x00016BED
		public static string DecodeFromBase64(string input)
		{
			return Encoding.UTF8.GetString(Convert.FromBase64String(input));
		}

		// Token: 0x06000465 RID: 1125 RVA: 0x000189FF File Offset: 0x00016BFF
		public static string HtmlEncode(string textToEncode)
		{
			return AntiXssEncoder.HtmlEncode(textToEncode, false);
		}

		// Token: 0x06000466 RID: 1126 RVA: 0x00018A08 File Offset: 0x00016C08
		public static void HtmlEncode(string s, TextWriter writer, bool encodeSpaces)
		{
			if (s == null || s.Length == 0)
			{
				return;
			}
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			if (encodeSpaces)
			{
				for (int i = 0; i < s.Length; i++)
				{
					if (s[i] == ' ')
					{
						writer.Write("&nbsp;");
					}
					else
					{
						writer.Write(AntiXssEncoder.HtmlEncode(s.Substring(i, 1), false));
					}
				}
				return;
			}
			writer.Write(AntiXssEncoder.HtmlEncode(s, false));
		}

		// Token: 0x06000467 RID: 1127 RVA: 0x00018A7D File Offset: 0x00016C7D
		public static void HtmlEncode(string s, TextWriter writer)
		{
			EncodingUtilities.HtmlEncode(s, writer, false);
		}

		// Token: 0x06000468 RID: 1128 RVA: 0x00018A87 File Offset: 0x00016C87
		public static string JavascriptEncode(string s)
		{
			return EncodingUtilities.JavascriptEncode(s, false);
		}

		// Token: 0x06000469 RID: 1129 RVA: 0x00018A90 File Offset: 0x00016C90
		public static string JavascriptEncode(string s, bool escapeNonAscii)
		{
			if (s == null)
			{
				return string.Empty;
			}
			string result;
			using (StringWriter stringWriter = new StringWriter(new StringBuilder()))
			{
				EncodingUtilities.JavascriptEncode(s, stringWriter, escapeNonAscii);
				result = stringWriter.ToString();
			}
			return result;
		}

		// Token: 0x0600046A RID: 1130 RVA: 0x00018AE0 File Offset: 0x00016CE0
		public static void JavascriptEncode(string s, TextWriter writer, bool escapeNonAscii)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			int i = 0;
			while (i < s.Length)
			{
				char c = s[i];
				if (c <= '"')
				{
					if (c <= '\r')
					{
						if (c != '\n')
						{
							if (c != '\r')
							{
								goto IL_A8;
							}
							writer.Write('\\');
							writer.Write('r');
						}
						else
						{
							writer.Write('\\');
							writer.Write('n');
						}
					}
					else
					{
						if (c != '!' && c != '"')
						{
							goto IL_A8;
						}
						goto IL_6D;
					}
				}
				else if (c <= '/')
				{
					if (c != '\'' && c != '/')
					{
						goto IL_A8;
					}
					goto IL_6D;
				}
				else
				{
					if (c == '<' || c == '>' || c == '\\')
					{
						goto IL_6D;
					}
					goto IL_A8;
				}
				IL_DC:
				i++;
				continue;
				IL_6D:
				writer.Write('\\');
				writer.Write(s[i]);
				goto IL_DC;
				IL_A8:
				if (escapeNonAscii && s[i] > '\u007f')
				{
					writer.Write("\\u{0:x4}", (ushort)s[i]);
					goto IL_DC;
				}
				writer.Write(s[i]);
				goto IL_DC;
			}
		}
	}
}
