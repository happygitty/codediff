using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000079 RID: 121
	internal class HttpRange
	{
		// Token: 0x06000419 RID: 1049 RVA: 0x00017C18 File Offset: 0x00015E18
		public HttpRange(long firstBytePosition, long lastBytePosition)
		{
			this.FirstBytePosition = firstBytePosition;
			this.LastBytePosition = lastBytePosition;
			if (this.HasFirstBytePosition && this.HasLastBytePosition)
			{
				if (this.FirstBytePosition > this.LastBytePosition)
				{
					throw new ArgumentOutOfRangeException("firstBytePosition", "FirstBytePosition cannot be larger than LastBytePosition");
				}
			}
			else if (!this.HasFirstBytePosition && !this.HasLastBytePosition && !this.HasSuffixLength)
			{
				throw new ArgumentOutOfRangeException("firstBytePosition", "At least firstBytePosition or lastBytePosition must be larger than or equal to 0.");
			}
		}

		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x0600041A RID: 1050 RVA: 0x00017C8F File Offset: 0x00015E8F
		// (set) Token: 0x0600041B RID: 1051 RVA: 0x00017C97 File Offset: 0x00015E97
		public long FirstBytePosition { get; private set; }

		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x0600041C RID: 1052 RVA: 0x00017CA0 File Offset: 0x00015EA0
		// (set) Token: 0x0600041D RID: 1053 RVA: 0x00017CA8 File Offset: 0x00015EA8
		public long LastBytePosition { get; private set; }

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x0600041E RID: 1054 RVA: 0x00017CB1 File Offset: 0x00015EB1
		public long SuffixLength
		{
			get
			{
				return this.LastBytePosition;
			}
		}

		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x0600041F RID: 1055 RVA: 0x00017CB9 File Offset: 0x00015EB9
		public bool HasFirstBytePosition
		{
			get
			{
				return this.FirstBytePosition >= 0L;
			}
		}

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x06000420 RID: 1056 RVA: 0x00017CC8 File Offset: 0x00015EC8
		public bool HasLastBytePosition
		{
			get
			{
				return this.HasFirstBytePosition && this.LastBytePosition >= 0L;
			}
		}

		// Token: 0x170000EA RID: 234
		// (get) Token: 0x06000421 RID: 1057 RVA: 0x00017CE1 File Offset: 0x00015EE1
		public bool HasSuffixLength
		{
			get
			{
				return this.FirstBytePosition < 0L && this.LastBytePosition >= 0L;
			}
		}
	}
}
