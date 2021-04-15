using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200005C RID: 92
	[Serializable]
	internal class MissingSslCertificateException : Exception
	{
		// Token: 0x060002E1 RID: 737 RVA: 0x0000EB06 File Offset: 0x0000CD06
		public MissingSslCertificateException() : base(MissingSslCertificateException.ErrorMessage)
		{
		}

		// Token: 0x040001BF RID: 447
		private static readonly string ErrorMessage = "Failed to load SSL certificate.";
	}
}
