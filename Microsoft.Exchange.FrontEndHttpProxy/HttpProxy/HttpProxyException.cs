using System;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200005A RID: 90
	[Serializable]
	internal class HttpProxyException : Exception
	{
		// Token: 0x060002D9 RID: 729 RVA: 0x0000E9DD File Offset: 0x0000CBDD
		public HttpProxyException(HttpStatusCode statusCode, HttpProxySubErrorCode errorCode, string message) : base(message)
		{
			this.statusCode = statusCode;
			this.errorCode = errorCode;
		}

		// Token: 0x060002DA RID: 730 RVA: 0x0000E9F4 File Offset: 0x0000CBF4
		public HttpProxyException(HttpStatusCode statusCode, HttpProxySubErrorCode errorCode, string message, Exception innerException) : base(message, innerException)
		{
			this.statusCode = statusCode;
			this.errorCode = errorCode;
		}

		// Token: 0x060002DB RID: 731 RVA: 0x0000EA10 File Offset: 0x0000CC10
		protected HttpProxyException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			if (info != null)
			{
				this.statusCode = (HttpStatusCode)info.GetValue("statusCode", typeof(int));
				this.errorCode = (HttpProxySubErrorCode)info.GetValue("errorCode", typeof(HttpProxySubErrorCode));
			}
		}

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x060002DC RID: 732 RVA: 0x0000EA68 File Offset: 0x0000CC68
		public HttpStatusCode StatusCode
		{
			get
			{
				return this.statusCode;
			}
		}

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x060002DD RID: 733 RVA: 0x0000EA70 File Offset: 0x0000CC70
		public HttpProxySubErrorCode ErrorCode
		{
			get
			{
				return this.errorCode;
			}
		}

		// Token: 0x060002DE RID: 734 RVA: 0x0000EA78 File Offset: 0x0000CC78
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			if (info != null)
			{
				info.AddValue("statusCode", this.StatusCode);
				info.AddValue("errorCode", this.ErrorCode);
			}
		}

		// Token: 0x040001BB RID: 443
		private readonly HttpStatusCode statusCode;

		// Token: 0x040001BC RID: 444
		private readonly HttpProxySubErrorCode errorCode;
	}
}
