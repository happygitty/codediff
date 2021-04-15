using System;
using Microsoft.Exchange.Diagnostics;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000073 RID: 115
	internal class AsyncStateHolder : DisposeTrackableBase
	{
		// Token: 0x060003DC RID: 988 RVA: 0x0001667D File Offset: 0x0001487D
		public AsyncStateHolder(object owner)
		{
			if (owner == null)
			{
				throw new ArgumentNullException("owner");
			}
			this.Owner = owner;
		}

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x060003DD RID: 989 RVA: 0x0001669A File Offset: 0x0001489A
		// (set) Token: 0x060003DE RID: 990 RVA: 0x000166A2 File Offset: 0x000148A2
		public object Owner { get; private set; }

		// Token: 0x060003DF RID: 991 RVA: 0x000166AB File Offset: 0x000148AB
		public static T Unwrap<T>(IAsyncResult asyncResult)
		{
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			return (T)((object)((AsyncStateHolder)asyncResult.AsyncState).Owner);
		}

		// Token: 0x060003E0 RID: 992 RVA: 0x000166D0 File Offset: 0x000148D0
		protected override DisposeTracker InternalGetDisposeTracker()
		{
			return DisposeTracker.Get<AsyncStateHolder>(this);
		}

		// Token: 0x060003E1 RID: 993 RVA: 0x000166D8 File Offset: 0x000148D8
		protected override void InternalDispose(bool disposing)
		{
			if (disposing)
			{
				this.Owner = null;
			}
		}
	}
}
