using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x0200000F RID: 15
	public class OwaServerVersion
	{
		// Token: 0x06000081 RID: 129 RVA: 0x000044F4 File Offset: 0x000026F4
		private OwaServerVersion(int major, int minor, int build, int dot)
		{
			this.major = major;
			this.minor = minor;
			this.build = build;
			this.dot = dot;
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000082 RID: 130 RVA: 0x00004519 File Offset: 0x00002719
		public int Major
		{
			get
			{
				return this.major;
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000083 RID: 131 RVA: 0x00004521 File Offset: 0x00002721
		public int Minor
		{
			get
			{
				return this.minor;
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000084 RID: 132 RVA: 0x00004529 File Offset: 0x00002729
		public int Build
		{
			get
			{
				return this.build;
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000085 RID: 133 RVA: 0x00004531 File Offset: 0x00002731
		public int Dot
		{
			get
			{
				return this.dot;
			}
		}

		// Token: 0x06000086 RID: 134 RVA: 0x0000453C File Offset: 0x0000273C
		public static OwaServerVersion CreateFromVersionNumber(int versionNumber)
		{
			int num = versionNumber & 32767;
			int num2 = versionNumber >> 16 & 63;
			int num3 = versionNumber >> 22 & 63;
			int num4 = 0;
			return new OwaServerVersion(num3, num2, num, num4);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x0000456C File Offset: 0x0000276C
		public static bool IsE14SP1OrGreater(int versionNumber)
		{
			OwaServerVersion owaServerVersion = OwaServerVersion.CreateFromVersionNumber(versionNumber);
			return owaServerVersion.Major >= 14 && owaServerVersion.Minor >= 1;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00004598 File Offset: 0x00002798
		public static OwaServerVersion CreateFromVersionString(string versionString)
		{
			if (versionString == null)
			{
				throw new ArgumentNullException("versionString");
			}
			return OwaServerVersion.TryValidateVersionString(versionString);
		}

		// Token: 0x06000089 RID: 137 RVA: 0x000045B0 File Offset: 0x000027B0
		public static int Compare(OwaServerVersion a, OwaServerVersion b)
		{
			if (a == null)
			{
				throw new ArgumentNullException("a");
			}
			if (b == null)
			{
				throw new ArgumentNullException("b");
			}
			int num = a.Major - b.Major;
			if (num == 0)
			{
				num = a.Minor - b.Minor;
				if (num == 0)
				{
					num = a.Build - b.Build;
					if (num == 0)
					{
						num = a.Dot - b.Dot;
					}
				}
			}
			return num;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x0000461B File Offset: 0x0000281B
		public static bool IsEqualMajorVersion(int a, int b)
		{
			return ((a ^ b) & 264241152) == 0;
		}

		// Token: 0x0600008B RID: 139 RVA: 0x0000462C File Offset: 0x0000282C
		public override string ToString()
		{
			if (this.versionString == null)
			{
				string arg = string.Concat(new object[]
				{
					this.major,
					".",
					this.minor,
					".",
					this.build
				});
				if (this.dot != -1)
				{
					arg = arg + "." + this.dot;
				}
				this.versionString = arg;
			}
			return this.versionString;
		}

		// Token: 0x0600008C RID: 140 RVA: 0x000046B4 File Offset: 0x000028B4
		private static OwaServerVersion TryValidateVersionString(string versionString)
		{
			if (string.IsNullOrEmpty(versionString))
			{
				return null;
			}
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			bool flag = false;
			int nextVersionPart = OwaServerVersion.GetNextVersionPart(versionString, 0, 2, out num, out flag);
			if (nextVersionPart == -1 || flag)
			{
				return null;
			}
			nextVersionPart = OwaServerVersion.GetNextVersionPart(versionString, nextVersionPart, 2, out num2, out flag);
			if (nextVersionPart == -1 || flag)
			{
				return null;
			}
			nextVersionPart = OwaServerVersion.GetNextVersionPart(versionString, nextVersionPart, 4, out num3, out flag);
			if (nextVersionPart == -1)
			{
				return null;
			}
			if (!flag)
			{
				nextVersionPart = OwaServerVersion.GetNextVersionPart(versionString, nextVersionPart, 4, out num4, out flag);
				if (nextVersionPart == -1 || !flag)
				{
					return null;
				}
			}
			return new OwaServerVersion(num, num2, num3, num4);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00004748 File Offset: 0x00002948
		private static int GetNextVersionPart(string versionString, int start, int maximumLength, out int part, out bool foundEnd)
		{
			bool flag = false;
			int num = start;
			part = 0;
			foundEnd = false;
			StringBuilder stringBuilder = new StringBuilder(maximumLength);
			for (;;)
			{
				if (num == versionString.Length)
				{
					if (stringBuilder.Length == 0)
					{
						break;
					}
					flag = true;
					foundEnd = true;
				}
				else
				{
					char c = versionString[num];
					if (c == '.')
					{
						flag = true;
					}
					else
					{
						if (!char.IsDigit(c))
						{
							return -1;
						}
						if (maximumLength == stringBuilder.Length)
						{
							return -1;
						}
						stringBuilder.Append(c);
					}
				}
				num++;
				if (flag)
				{
					goto Block_6;
				}
			}
			return -1;
			Block_6:
			part = int.Parse(stringBuilder.ToString());
			return num;
		}

		// Token: 0x04000081 RID: 129
		private readonly int major;

		// Token: 0x04000082 RID: 130
		private readonly int minor;

		// Token: 0x04000083 RID: 131
		private readonly int build;

		// Token: 0x04000084 RID: 132
		private readonly int dot;

		// Token: 0x04000085 RID: 133
		private string versionString;

		// Token: 0x020000D2 RID: 210
		public class ServerVersionComparer : IEqualityComparer<OwaServerVersion>
		{
			// Token: 0x060007B3 RID: 1971 RVA: 0x0002C7CA File Offset: 0x0002A9CA
			public bool Equals(OwaServerVersion a, OwaServerVersion b)
			{
				return this.GetHashCode(a) == this.GetHashCode(b);
			}

			// Token: 0x060007B4 RID: 1972 RVA: 0x0002C7DC File Offset: 0x0002A9DC
			public int GetHashCode(OwaServerVersion owaServerVersion)
			{
				if (owaServerVersion == null)
				{
					throw new ArgumentNullException("owaServerVersion");
				}
				return (owaServerVersion.major << 26 & -67108864) | (owaServerVersion.minor << 20 & 66060288) | (owaServerVersion.build << 5 & 1048544) | (owaServerVersion.dot & 31);
			}
		}
	}
}
