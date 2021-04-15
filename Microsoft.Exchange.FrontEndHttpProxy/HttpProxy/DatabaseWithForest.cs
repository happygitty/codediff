using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000043 RID: 67
	internal class DatabaseWithForest
	{
		// Token: 0x06000222 RID: 546 RVA: 0x0000AB74 File Offset: 0x00008D74
		public DatabaseWithForest(Guid database, string resourceForest, Guid initiatingRequestId)
		{
			this.Database = database;
			this.ResourceForest = resourceForest;
			this.InitiatingRequestId = initiatingRequestId;
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x06000223 RID: 547 RVA: 0x0000AB91 File Offset: 0x00008D91
		// (set) Token: 0x06000224 RID: 548 RVA: 0x0000AB99 File Offset: 0x00008D99
		public Guid Database { get; set; }

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x06000225 RID: 549 RVA: 0x0000ABA2 File Offset: 0x00008DA2
		// (set) Token: 0x06000226 RID: 550 RVA: 0x0000ABAA File Offset: 0x00008DAA
		public string ResourceForest { get; set; }

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x06000227 RID: 551 RVA: 0x0000ABB3 File Offset: 0x00008DB3
		// (set) Token: 0x06000228 RID: 552 RVA: 0x0000ABBB File Offset: 0x00008DBB
		public Guid InitiatingRequestId { get; set; }
	}
}
