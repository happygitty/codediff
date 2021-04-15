using System;
using Microsoft.Exchange.Data.Storage;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200005D RID: 93
	[Serializable]
	internal class NoAvailableDownLevelBackEndException : ServerNotFoundException
	{
		// Token: 0x060002E3 RID: 739 RVA: 0x0000EAE3 File Offset: 0x0000CCE3
		public NoAvailableDownLevelBackEndException(string message) : base(message)
		{
		}
	}
}
