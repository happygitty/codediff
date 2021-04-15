using System;
using Microsoft.Exchange.Diagnostics;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000073 RID: 115
	internal class AsyncStateHolder : DisposeTrackableBase
	{
		// Token: 0x060003DC RID: 988 RVA: 0x000166B9 File Offset: 0x000148B9
		public AsyncStateHolder(object owner)
		{
			if (owner == null)
			{
				throw new ArgumentNullException("owner");
			}
			this.Owner = owner;
		}

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x060003DD RID: 989 RVA: 0x000166D6 File Offset: 0x000148D6
		// (set) Token: 0x060003DE RID: 990 RVA: 0x000166DE File Offset: 0x000148DE
		public object Owner { get; private set; }

		// Token: 0x060003DF RID: 991 RVA: 0x000166E7 File Offset: 0x000148E7
		public static T Unwrap<T>(IAsyncResult asyncResult)
		{
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			return (T)((object)((AsyncStateHolder)asyncResult.AsyncState).Owner);
		}

		// Token: 0x060003E0 RID: 992 RVA: 0x0001670C File Offset: 0x0001490C
		protected override DisposeTracker InternalGetDisposeTracker()
		{
			return DisposeTracker.Get<AsyncStateHolder>(this);
		}

		// Token: 0x060003E1 RID: 993 RVA: 0x00016714 File Offset: 0x00014914
		protected override void InternalDispose(bool disposing)
		{
			if (disposing)
			{
				this.Owner = null;
			}
		}
	}
}
