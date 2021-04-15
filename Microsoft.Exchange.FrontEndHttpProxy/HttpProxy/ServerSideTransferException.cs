using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Web;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200005E RID: 94
	[Serializable]
	internal class ServerSideTransferException : HttpException
	{
		// Token: 0x060002E4 RID: 740 RVA: 0x0000EAEC File Offset: 0x0000CCEC
		public ServerSideTransferException(string redirectUrl, LegacyRedirectTypeOptions redirectType)
		{
			this.RedirectUrl = redirectUrl;
			this.RedirectType = redirectType;
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x0000EB02 File Offset: 0x0000CD02
		public ServerSideTransferException(Exception innerException) : base(innerException.Message, innerException)
		{
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x0000EB11 File Offset: 0x0000CD11
		public ServerSideTransferException(string redirectUrl, string message) : base(message)
		{
			this.RedirectUrl = redirectUrl;
		}

		// Token: 0x060002E7 RID: 743 RVA: 0x0000EB21 File Offset: 0x0000CD21
		public ServerSideTransferException(string redirectUrl, string message, Exception innerException) : base(message, innerException)
		{
			this.RedirectUrl = redirectUrl;
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x0000EB32 File Offset: 0x0000CD32
		protected ServerSideTransferException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			if (info != null)
			{
				this.RedirectUrl = (string)info.GetValue("redirectUrl", typeof(string));
			}
		}

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x060002E9 RID: 745 RVA: 0x0000EB5F File Offset: 0x0000CD5F
		// (set) Token: 0x060002EA RID: 746 RVA: 0x0000EB67 File Offset: 0x0000CD67
		public string RedirectUrl { get; private set; }

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x060002EB RID: 747 RVA: 0x0000EB70 File Offset: 0x0000CD70
		// (set) Token: 0x060002EC RID: 748 RVA: 0x0000EB78 File Offset: 0x0000CD78
		public LegacyRedirectTypeOptions RedirectType { get; private set; }

		// Token: 0x060002ED RID: 749 RVA: 0x0000EB81 File Offset: 0x0000CD81
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			if (info != null)
			{
				info.AddValue("redirectUrl", this.RedirectUrl);
			}
		}
	}
}
