using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000013 RID: 19
	internal static class ThemeFileList
	{
		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000098 RID: 152 RVA: 0x000048A3 File Offset: 0x00002AA3
		internal static int Count
		{
			get
			{
				return ThemeFileList.idTable.Count;
			}
		}

		// Token: 0x06000099 RID: 153 RVA: 0x000048B0 File Offset: 0x00002AB0
		internal static int Add(string themeFileName, bool useCssSprites)
		{
			if (!ThemeFileList.idTable.ContainsKey(themeFileName))
			{
				ThemeFileList.ThemeFile item = new ThemeFileList.ThemeFile((ThemeFileId)ThemeFileList.nameTable.Count, themeFileName, useCssSprites);
				ThemeFileList.idTable[themeFileName] = ThemeFileList.nameTable.Count;
				ThemeFileList.nameTable.Add(item);
			}
			return ThemeFileList.idTable[themeFileName];
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00004908 File Offset: 0x00002B08
		internal static int GetIdFromName(string themeFileName)
		{
			int result = 0;
			ThemeFileList.idTable.TryGetValue(themeFileName, out result);
			return result;
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00004926 File Offset: 0x00002B26
		internal static string GetNameFromId(ThemeFileId themeFileId)
		{
			return ThemeFileList.nameTable[(int)themeFileId].Name;
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00004938 File Offset: 0x00002B38
		internal static string GetClassNameFromId(int themeFileIndex)
		{
			return ThemeFileList.nameTable[themeFileIndex].ClassName;
		}

		// Token: 0x0600009D RID: 157 RVA: 0x0000494A File Offset: 0x00002B4A
		internal static bool GetPhaseIIFromId(int themeFileIndex)
		{
			return ThemeFileList.nameTable[themeFileIndex].PhaseII;
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00004926 File Offset: 0x00002B26
		internal static string GetNameFromId(int themeFileIndex)
		{
			return ThemeFileList.nameTable[themeFileIndex].Name;
		}

		// Token: 0x0600009F RID: 159 RVA: 0x0000495C File Offset: 0x00002B5C
		internal static bool CanUseCssSprites(ThemeFileId themeFileId)
		{
			return ThemeFileList.CanUseCssSprites((int)themeFileId);
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00004964 File Offset: 0x00002B64
		internal static bool CanUseCssSprites(int themeFileIndex)
		{
			return ThemeFileList.nameTable[themeFileIndex].UseCssSprites;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00004976 File Offset: 0x00002B76
		internal static bool IsResourceFile(ThemeFileId themeFileId)
		{
			return ThemeFileList.IsResourceFile((int)themeFileId);
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x0000497E File Offset: 0x00002B7E
		internal static bool IsResourceFile(int themeFileIndex)
		{
			return ThemeFileList.nameTable[themeFileIndex].IsResource;
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00004990 File Offset: 0x00002B90
		private static bool Initialize()
		{
			ThemeFileList.nameTable = new List<ThemeFileList.ThemeFile>(27);
			ThemeFileList.idTable = new Dictionary<string, int>(27);
			foreach (FieldInfo fieldInfo in typeof(ThemeFileId).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField))
			{
				ThemeFileId themeFileId = (ThemeFileId)fieldInfo.GetValue(null);
				object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(ThemeFileInfoAttribute), false);
				if (customAttributes == null || customAttributes.Length == 0)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError<ThemeFileId>(0L, "{0} doesn't define ThemeFileInfoAttribute", themeFileId);
					}
				}
				else
				{
					ThemeFileInfoAttribute themeFileInfoAttribute = (ThemeFileInfoAttribute)customAttributes[0];
					ThemeFileList.idTable[themeFileInfoAttribute.Name] = (int)themeFileId;
					ThemeFileList.nameTable.Add(new ThemeFileList.ThemeFile(themeFileId, themeFileInfoAttribute.Name, themeFileInfoAttribute.UseCssSprites, themeFileInfoAttribute.IsResource, themeFileInfoAttribute.PhaseII));
				}
			}
			return true;
		}

		// Token: 0x040000AB RID: 171
		private static List<ThemeFileList.ThemeFile> nameTable;

		// Token: 0x040000AC RID: 172
		private static Dictionary<string, int> idTable;

		// Token: 0x040000AD RID: 173
		private static bool isInitialized = ThemeFileList.Initialize();

		// Token: 0x020000D4 RID: 212
		private struct ThemeFile
		{
			// Token: 0x060007BB RID: 1979 RVA: 0x0002C647 File Offset: 0x0002A847
			public ThemeFile(ThemeFileId id, string name)
			{
				this = new ThemeFileList.ThemeFile(id, name, true);
			}

			// Token: 0x060007BC RID: 1980 RVA: 0x0002C652 File Offset: 0x0002A852
			public ThemeFile(ThemeFileId id, string name, bool useCssSprites)
			{
				this = new ThemeFileList.ThemeFile(id, name, useCssSprites, false);
			}

			// Token: 0x060007BD RID: 1981 RVA: 0x0002C65E File Offset: 0x0002A85E
			public ThemeFile(ThemeFileId id, string name, bool useCssSprites, bool isResource)
			{
				this = new ThemeFileList.ThemeFile(id, name, useCssSprites, isResource, false);
			}

			// Token: 0x060007BE RID: 1982 RVA: 0x0002C66C File Offset: 0x0002A86C
			public ThemeFile(ThemeFileId id, string name, bool useCssSprites, bool isResource, bool phaseII)
			{
				this.Id = id;
				this.Name = name;
				this.ClassName = (useCssSprites ? ("sprites-" + name.Replace(".", "-")) : string.Empty);
				this.UseCssSprites = useCssSprites;
				this.IsResource = isResource;
				this.PhaseII = phaseII;
			}

			// Token: 0x04000458 RID: 1112
			public ThemeFileId Id;

			// Token: 0x04000459 RID: 1113
			public string Name;

			// Token: 0x0400045A RID: 1114
			public string ClassName;

			// Token: 0x0400045B RID: 1115
			public bool UseCssSprites;

			// Token: 0x0400045C RID: 1116
			public bool IsResource;

			// Token: 0x0400045D RID: 1117
			public bool PhaseII;

			// Token: 0x0400045E RID: 1118
			private const string SpritesClassPrefix = "sprites-";
		}
	}
}
