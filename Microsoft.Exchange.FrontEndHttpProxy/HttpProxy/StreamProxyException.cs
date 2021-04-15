using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200005F RID: 95
	[Serializable]
	internal class StreamProxyException : Exception
	{
		// Token: 0x060002EE RID: 750 RVA: 0x0000EBDB File Offset: 0x0000CDDB
		public StreamProxyException(Exception innerException) : base(innerException.Message, innerException)
		{
		}
	}
}
