using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000079 RID: 121
	internal class HttpRange
	{
		// Token: 0x06000415 RID: 1045 RVA: 0x00017A58 File Offset: 0x00015C58
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
		// (get) Token: 0x06000416 RID: 1046 RVA: 0x00017ACF File Offset: 0x00015CCF
		// (set) Token: 0x06000417 RID: 1047 RVA: 0x00017AD7 File Offset: 0x00015CD7
		public long FirstBytePosition { get; private set; }

		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x06000418 RID: 1048 RVA: 0x00017AE0 File Offset: 0x00015CE0
		// (set) Token: 0x06000419 RID: 1049 RVA: 0x00017AE8 File Offset: 0x00015CE8
		public long LastBytePosition { get; private set; }

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x0600041A RID: 1050 RVA: 0x00017AF1 File Offset: 0x00015CF1
		public long SuffixLength
		{
			get
			{
				return this.LastBytePosition;
			}
		}

		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x0600041B RID: 1051 RVA: 0x00017AF9 File Offset: 0x00015CF9
		public bool HasFirstBytePosition
		{
			get
			{
				return this.FirstBytePosition >= 0L;
			}
		}

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x0600041C RID: 1052 RVA: 0x00017B08 File Offset: 0x00015D08
		public bool HasLastBytePosition
		{
			get
			{
				return this.HasFirstBytePosition && this.LastBytePosition >= 0L;
			}
		}

		// Token: 0x170000EA RID: 234
		// (get) Token: 0x0600041D RID: 1053 RVA: 0x00017B21 File Offset: 0x00015D21
		public bool HasSuffixLength
		{
			get
			{
				return this.FirstBytePosition < 0L && this.LastBytePosition >= 0L;
			}
		}
	}
}
