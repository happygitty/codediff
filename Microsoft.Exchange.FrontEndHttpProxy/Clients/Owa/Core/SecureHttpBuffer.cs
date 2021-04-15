using System;
using System.Security;
using System.Web;
using Microsoft.Exchange.Common;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Extensions;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x0200000D RID: 13
	public class SecureHttpBuffer : DisposeTrackableBase
	{
		// Token: 0x0600006C RID: 108 RVA: 0x00004058 File Offset: 0x00002258
		public SecureHttpBuffer(int size, HttpResponse response)
		{
			if (response == null)
			{
				throw new ArgumentNullException("response");
			}
			if (size < 0)
			{
				throw new ArgumentException("Size is not valid");
			}
			this.buffer = new SecureArray<char>(new char[size]);
			this.response = response;
			this.currentPosition = 0;
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600006D RID: 109 RVA: 0x000040A7 File Offset: 0x000022A7
		public int Size
		{
			get
			{
				base.CheckDisposed();
				return this.buffer.ArrayValue.Length;
			}
		}

		// Token: 0x0600006E RID: 110 RVA: 0x000040BC File Offset: 0x000022BC
		public void CopyAtCurrentPosition(string value)
		{
			base.CheckDisposed();
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			this.AdjustSizeAtCurrentPosition(value.Length);
			value.CopyTo(0, this.buffer.ArrayValue, this.currentPosition, value.Length);
			this.currentPosition += value.Length;
		}

		// Token: 0x0600006F RID: 111 RVA: 0x0000411C File Offset: 0x0000231C
		public void CopyAtCurrentPosition(SecureString secureValue)
		{
			base.CheckDisposed();
			if (secureValue == null)
			{
				throw new ArgumentNullException("secureValue");
			}
			using (SecureArray<char> secureArray = SecureStringExtensions.ConvertToSecureCharArray(secureValue))
			{
				this.CopyAtCurrentPosition(secureArray);
			}
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00004168 File Offset: 0x00002368
		public void CopyAtCurrentPosition(SecureArray<char> secureArray)
		{
			base.CheckDisposed();
			if (secureArray == null)
			{
				throw new ArgumentNullException("secureArray");
			}
			this.AdjustSizeAtCurrentPosition(secureArray.ArrayValue.Length);
			secureArray.ArrayValue.CopyTo(this.buffer.ArrayValue, this.currentPosition);
			this.currentPosition += secureArray.ArrayValue.Length;
		}

		// Token: 0x06000071 RID: 113 RVA: 0x000041C8 File Offset: 0x000023C8
		public void Flush()
		{
			base.CheckDisposed();
			if (this.currentPosition > 0)
			{
				this.response.Write(this.buffer.ArrayValue, 0, this.currentPosition);
				Array.Clear(this.buffer.ArrayValue, 0, this.buffer.ArrayValue.Length);
				this.currentPosition = 0;
				this.response.Flush();
			}
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00004231 File Offset: 0x00002431
		protected override void InternalDispose(bool disposing)
		{
			if (disposing && this.buffer != null)
			{
				this.Flush();
				this.buffer.Dispose();
				this.buffer = null;
			}
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00004256 File Offset: 0x00002456
		protected override DisposeTracker InternalGetDisposeTracker()
		{
			return DisposeTracker.Get<SecureHttpBuffer>(this);
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00004260 File Offset: 0x00002460
		private void AdjustSizeAtCurrentPosition(int length)
		{
			int num = this.currentPosition + 1 + length;
			if (num > this.Size)
			{
				this.Resize(Math.Max(this.Size * 2, num));
			}
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00004298 File Offset: 0x00002498
		private void Resize(int newSize)
		{
			using (SecureArray<char> secureArray = this.buffer)
			{
				this.buffer = new SecureArray<char>(newSize);
				secureArray.ArrayValue.CopyTo(this.buffer.ArrayValue, 0);
			}
		}

		// Token: 0x0400007A RID: 122
		private SecureArray<char> buffer;

		// Token: 0x0400007B RID: 123
		private int currentPosition;

		// Token: 0x0400007C RID: 124
		private HttpResponse response;
	}
}
