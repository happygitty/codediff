﻿using System;
using System.Globalization;
using System.Text;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.ExchangeSystem;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200004D RID: 77
	internal abstract class BackEndCookieEntryBase
	{
		// Token: 0x06000274 RID: 628 RVA: 0x0000CC62 File Offset: 0x0000AE62
		protected BackEndCookieEntryBase(BackEndCookieEntryType entryType, ExDateTime expiryTime)
		{
			this.EntryType = entryType;
			this.ExpiryTime = expiryTime;
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x06000275 RID: 629 RVA: 0x0000CC78 File Offset: 0x0000AE78
		// (set) Token: 0x06000276 RID: 630 RVA: 0x0000CC80 File Offset: 0x0000AE80
		public ExDateTime ExpiryTime { get; protected set; }

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x06000277 RID: 631 RVA: 0x0000CC89 File Offset: 0x0000AE89
		public bool Expired
		{
			get
			{
				return this.ExpiryTime < ExDateTime.UtcNow;
			}
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x06000278 RID: 632 RVA: 0x0000CC9B File Offset: 0x0000AE9B
		// (set) Token: 0x06000279 RID: 633 RVA: 0x0000CCA3 File Offset: 0x0000AEA3
		public BackEndCookieEntryType EntryType { get; protected set; }

		// Token: 0x0600027A RID: 634 RVA: 0x0000CCAC File Offset: 0x0000AEAC
		public string ToObscureString()
		{
			return BackEndCookieEntryBase.Obscurify(this.ToString());
		}

		// Token: 0x0600027B RID: 635 RVA: 0x00003165 File Offset: 0x00001365
		public virtual bool ShouldInvalidate(BackEndServer badTarget)
		{
			return false;
		}

		// Token: 0x0600027C RID: 636 RVA: 0x0000CCB9 File Offset: 0x0000AEB9
		internal static string ConvertBackEndCookieEntryTypeToString(BackEndCookieEntryType entryType)
		{
			if (entryType == BackEndCookieEntryType.Server)
			{
				return BackEndCookieEntryBase.BackEndCookieEntryTypeServerName;
			}
			if (entryType != BackEndCookieEntryType.Database)
			{
				throw new InvalidOperationException(string.Format("Unknown cookie type: {0}", entryType));
			}
			return BackEndCookieEntryBase.BackEndCookieEntryTypeDatabaseName;
		}

		// Token: 0x0600027D RID: 637 RVA: 0x0000CCE5 File Offset: 0x0000AEE5
		internal static bool TryGetBackEndCookieEntryTypeFromString(string entryTypeString, out BackEndCookieEntryType entryType)
		{
			if (string.Equals(entryTypeString, BackEndCookieEntryBase.BackEndCookieEntryTypeDatabaseName, StringComparison.OrdinalIgnoreCase))
			{
				entryType = BackEndCookieEntryType.Database;
				return true;
			}
			if (string.Equals(entryTypeString, BackEndCookieEntryBase.BackEndCookieEntryTypeServerName, StringComparison.OrdinalIgnoreCase))
			{
				entryType = BackEndCookieEntryType.Server;
				return true;
			}
			entryType = BackEndCookieEntryType.Server;
			return false;
		}

		// Token: 0x0600027E RID: 638 RVA: 0x0000CD14 File Offset: 0x0000AF14
		protected static string Obscurify(string clearString)
		{
			byte[] bytes = BackEndCookieEntryBase.Encoding.GetBytes(clearString);
			byte[] array = new byte[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
			{
				byte[] array2 = array;
				int num = i;
				byte[] array3 = bytes;
				int num2 = i;
				array2[num] = (array3[num2] ^= BackEndCookieEntryBase.ObfuscateValue);
			}
			return Convert.ToBase64String(array);
		}

		// Token: 0x04000187 RID: 391
		public const int MaxBackEndServerCookieEntries = 5;

		// Token: 0x04000188 RID: 392
		public static readonly TimeSpan BackEndServerCookieLifeTime = TimeSpan.FromMinutes(10.0);

		// Token: 0x04000189 RID: 393
		public static readonly TimeSpan LongLivedBackEndServerCookieLifeTime = TimeSpan.FromDays(30.0);

		// Token: 0x0400018A RID: 394
		internal static readonly byte ObfuscateValue = byte.MaxValue;

		// Token: 0x0400018B RID: 395
		internal static readonly ASCIIEncoding Encoding = new ASCIIEncoding();

		// Token: 0x0400018C RID: 396
		internal static readonly string BackEndCookieEntryTypeServerName = string.Format(CultureInfo.InvariantCulture, "{0}", Enum.GetName(typeof(BackEndCookieEntryType), BackEndCookieEntryType.Server));

		// Token: 0x0400018D RID: 397
		internal static readonly string BackEndCookieEntryTypeDatabaseName = string.Format(CultureInfo.InvariantCulture, "{0}", Enum.GetName(typeof(BackEndCookieEntryType), BackEndCookieEntryType.Database));

		// Token: 0x0400018E RID: 398
		protected const string Separator = "~";
	}
}
