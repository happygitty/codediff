using System;
using Microsoft.Exchange.Collections.TimeoutCache;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000044 RID: 68
	internal sealed class E4eBackoffListCache
	{
		// Token: 0x06000229 RID: 553 RVA: 0x0000ABC4 File Offset: 0x00008DC4
		private E4eBackoffListCache()
		{
			this.senderBackoffListCache = new ExactTimeoutCache<string, DateTime>(null, null, null, 10240, false);
			this.recipientsBackoffListCache = new ExactTimeoutCache<string, DateTime>(null, null, null, 10240, false);
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x0600022A RID: 554 RVA: 0x0000ABF4 File Offset: 0x00008DF4
		public static E4eBackoffListCache Instance
		{
			get
			{
				return E4eBackoffListCache.instance;
			}
		}

		// Token: 0x0600022B RID: 555 RVA: 0x0000ABFC File Offset: 0x00008DFC
		public void UpdateCache(string budgetType, string emailAddress, string backoffUntilUtcStr)
		{
			if (string.IsNullOrEmpty(budgetType) || string.IsNullOrEmpty(emailAddress) || string.IsNullOrEmpty(backoffUntilUtcStr))
			{
				return;
			}
			if (!SmtpAddress.IsValidSmtpAddress(emailAddress))
			{
				return;
			}
			BudgetType budgetType2;
			try
			{
				if (!Enum.TryParse<BudgetType>(budgetType, true, out budgetType2))
				{
					return;
				}
			}
			catch (ArgumentException)
			{
				return;
			}
			DateTime dateTime;
			if (!DateTime.TryParse(backoffUntilUtcStr, out dateTime) || dateTime <= DateTime.UtcNow)
			{
				return;
			}
			TimeSpan timeSpan = (dateTime == DateTime.MaxValue) ? TimeSpan.MaxValue : (dateTime - DateTime.UtcNow);
			if (timeSpan.TotalMilliseconds <= 0.0)
			{
				return;
			}
			if (budgetType2 == 17)
			{
				this.senderBackoffListCache.TryInsertAbsolute(emailAddress, dateTime, timeSpan);
				return;
			}
			if (budgetType2 == 18)
			{
				this.recipientsBackoffListCache.TryInsertAbsolute(emailAddress, dateTime, timeSpan);
			}
			return;
		}

		// Token: 0x0600022C RID: 556 RVA: 0x0000ACC4 File Offset: 0x00008EC4
		public bool ShouldBackOff(string senderEmailAddress, string recipientEmailAddress)
		{
			return (!string.IsNullOrEmpty(senderEmailAddress) && this.ContainsValidBackoffEntry(this.senderBackoffListCache, senderEmailAddress)) || (!string.IsNullOrEmpty(recipientEmailAddress) && this.ContainsValidBackoffEntry(this.recipientsBackoffListCache, recipientEmailAddress));
		}

		// Token: 0x0600022D RID: 557 RVA: 0x0000ACFC File Offset: 0x00008EFC
		private bool ContainsValidBackoffEntry(ExactTimeoutCache<string, DateTime> timeoutCache, string emailAddress)
		{
			DateTime t;
			return timeoutCache.TryGetValue(emailAddress, ref t) && t > DateTime.UtcNow;
		}

		// Token: 0x04000133 RID: 307
		private const int BackoffListCacheSize = 10240;

		// Token: 0x04000134 RID: 308
		private static E4eBackoffListCache instance = new E4eBackoffListCache();

		// Token: 0x04000135 RID: 309
		private static object staticLock = new object();

		// Token: 0x04000136 RID: 310
		private ExactTimeoutCache<string, DateTime> senderBackoffListCache;

		// Token: 0x04000137 RID: 311
		private ExactTimeoutCache<string, DateTime> recipientsBackoffListCache;
	}
}
