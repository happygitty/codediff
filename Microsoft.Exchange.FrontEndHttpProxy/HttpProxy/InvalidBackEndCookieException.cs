using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200005B RID: 91
	[Serializable]
	internal class InvalidBackEndCookieException : Exception
	{
		// Token: 0x060002DF RID: 735 RVA: 0x0000EAB1 File Offset: 0x0000CCB1
		public InvalidBackEndCookieException() : base(InvalidBackEndCookieException.ErrorMessage)
		{
		}

		// Token: 0x040001BD RID: 445
		private static readonly string ErrorMessage = "Invalid back end cookie entry.";
	}
}
