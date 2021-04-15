using System;
using System.IO;
using Microsoft.Exchange.HttpProxy;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000014 RID: 20
	public sealed class ThemeManager
	{
		// Token: 0x060000A5 RID: 165 RVA: 0x00004A7C File Offset: 0x00002C7C
		public static void RenderBaseThemeFileUrl(TextWriter writer, ThemeFileId themeFileId)
		{
			ThemeManager.RenderBaseThemeFileUrl(writer, themeFileId, false);
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00004A86 File Offset: 0x00002C86
		public static void RenderBaseThemeFileUrl(TextWriter writer, ThemeFileId themeFileId, bool useCDN)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			ThemeManager.RenderThemeFileUrl(writer, (int)themeFileId, false, useCDN);
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00004A9F File Offset: 0x00002C9F
		public static void RenderThemeFileUrl(TextWriter writer, int themeFileIndex, bool isBasicExperience, bool useCDN)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			ThemeManager.RenderThemeFilePath(writer, themeFileIndex, isBasicExperience, useCDN);
			writer.Write(ThemeFileList.GetNameFromId(themeFileIndex));
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00004AC8 File Offset: 0x00002CC8
		private static bool RenderThemeFilePath(TextWriter writer, int themeFileIndex, bool isBasicExperience, bool useCDN)
		{
			writer.Write(ThemeManager.themesFolderPath);
			bool flag = ThemeFileList.IsResourceFile(themeFileIndex);
			if (flag)
			{
				writer.Write(ThemeManager.ResourcesFolderName);
			}
			else if (isBasicExperience)
			{
				writer.Write(ThemeManager.BasicFilesFolderName);
			}
			else
			{
				writer.Write(ThemeManager.BaseThemeFolderName);
			}
			writer.Write("/");
			return !flag;
		}

		// Token: 0x040000AE RID: 174
		public static readonly string BaseThemeFolderName = "base";

		// Token: 0x040000AF RID: 175
		public static readonly string BasicFilesFolderName = "basic";

		// Token: 0x040000B0 RID: 176
		public static readonly string ResourcesFolderName = "resources";

		// Token: 0x040000B1 RID: 177
		public static readonly string DataCenterThemeStorageId = "datacenter";

		// Token: 0x040000B2 RID: 178
		private static readonly string ThemesFolderName = "themes";

		// Token: 0x040000B3 RID: 179
		private static string themesFolderPath = string.Format("{0}/{1}/", HttpProxyGlobals.ApplicationVersion, ThemeManager.ThemesFolderName);
	}
}
