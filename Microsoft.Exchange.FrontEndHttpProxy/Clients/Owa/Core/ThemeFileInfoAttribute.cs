using System;
using System.IO;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000011 RID: 17
	[AttributeUsage(AttributeTargets.Field)]
	internal sealed class ThemeFileInfoAttribute : Attribute
	{
		// Token: 0x0600008E RID: 142 RVA: 0x000047C5 File Offset: 0x000029C5
		internal ThemeFileInfoAttribute() : this(string.Empty, ThemeFileInfoFlags.None, null)
		{
		}

		// Token: 0x0600008F RID: 143 RVA: 0x000047D4 File Offset: 0x000029D4
		internal ThemeFileInfoAttribute(string name) : this(name, ThemeFileInfoFlags.None, null)
		{
		}

		// Token: 0x06000090 RID: 144 RVA: 0x000047DF File Offset: 0x000029DF
		internal ThemeFileInfoAttribute(string name, ThemeFileInfoFlags themeFileInfoFlags) : this(name, themeFileInfoFlags, null)
		{
		}

		// Token: 0x06000091 RID: 145 RVA: 0x000047EA File Offset: 0x000029EA
		internal ThemeFileInfoAttribute(string name, ThemeFileInfoFlags themeFileInfoFlags, string fallbackImageName)
		{
			this.name = name;
			this.themeFileInfoFlags = themeFileInfoFlags;
			this.fallbackImageName = fallbackImageName;
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000092 RID: 146 RVA: 0x00004807 File Offset: 0x00002A07
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000093 RID: 147 RVA: 0x0000480F File Offset: 0x00002A0F
		public string FallbackImageName
		{
			get
			{
				return this.fallbackImageName;
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000094 RID: 148 RVA: 0x00004818 File Offset: 0x00002A18
		public bool UseCssSprites
		{
			get
			{
				if (string.IsNullOrEmpty(this.Name))
				{
					return false;
				}
				string extension = Path.GetExtension(this.Name);
				return !string.IsNullOrEmpty(extension) && (extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)) && !ThemeFileInfoAttribute.IsFlagSet(this.themeFileInfoFlags, ThemeFileInfoFlags.LooseImage);
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000095 RID: 149 RVA: 0x00004875 File Offset: 0x00002A75
		public bool PhaseII
		{
			get
			{
				return this.UseCssSprites && ThemeFileInfoAttribute.IsFlagSet(this.themeFileInfoFlags, ThemeFileInfoFlags.PhaseII);
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000096 RID: 150 RVA: 0x0000488D File Offset: 0x00002A8D
		public bool IsResource
		{
			get
			{
				return ThemeFileInfoAttribute.IsFlagSet(this.themeFileInfoFlags, ThemeFileInfoFlags.Resource);
			}
		}

		// Token: 0x06000097 RID: 151 RVA: 0x0000489B File Offset: 0x00002A9B
		private static bool IsFlagSet(ThemeFileInfoFlags valueToTest, ThemeFileInfoFlags flag)
		{
			return (valueToTest & flag) == flag;
		}

		// Token: 0x040000A3 RID: 163
		private string name;

		// Token: 0x040000A4 RID: 164
		private ThemeFileInfoFlags themeFileInfoFlags;

		// Token: 0x040000A5 RID: 165
		private string fallbackImageName;
	}
}
