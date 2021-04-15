using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200005C RID: 92
	[Serializable]
	internal class MissingSslCertificateException : Exception
	{
		// Token: 0x060002E1 RID: 737 RVA: 0x0000EACA File Offset: 0x0000CCCA
		public MissingSslCertificateException() : base(MissingSslCertificateException.ErrorMessage)
		{
		}

		// Token: 0x040001BE RID: 446
		private static readonly string ErrorMessage = "Failed to load SSL certificate.";
	}
}
