using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200005B RID: 91
	[Serializable]
	internal class InvalidBackEndCookieException : Exception
	{
		// Token: 0x060002DF RID: 735 RVA: 0x0000EAED File Offset: 0x0000CCED
		public InvalidBackEndCookieException() : base(InvalidBackEndCookieException.ErrorMessage)
		{
		}

		// Token: 0x040001BE RID: 446
		private static readonly string ErrorMessage = "Invalid back end cookie entry.";
	}
}
