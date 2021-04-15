using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200007A RID: 122
	internal class HttpRangeSpecifier
	{
		// Token: 0x0600041E RID: 1054 RVA: 0x00017B3C File Offset: 0x00015D3C
		public HttpRangeSpecifier()
		{
			this.RangeUnitSpecifier = "bytes";
		}

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x0600041F RID: 1055 RVA: 0x00017B5A File Offset: 0x00015D5A
		public Collection<HttpRange> RangeCollection
		{
			get
			{
				return this.rangeCollection;
			}
		}

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x06000420 RID: 1056 RVA: 0x00017B62 File Offset: 0x00015D62
		// (set) Token: 0x06000421 RID: 1057 RVA: 0x00017B6A File Offset: 0x00015D6A
		public string RangeUnitSpecifier { get; set; }

		// Token: 0x06000422 RID: 1058 RVA: 0x00017B74 File Offset: 0x00015D74
		public static HttpRangeSpecifier Parse(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentNullException("value");
			}
			HttpRangeSpecifier httpRangeSpecifier = new HttpRangeSpecifier();
			string message;
			if (!HttpRangeSpecifier.TryParseInternal(value, httpRangeSpecifier, out message))
			{
				throw new ArgumentException(message);
			}
			return httpRangeSpecifier;
		}

		// Token: 0x06000423 RID: 1059 RVA: 0x00017BB0 File Offset: 0x00015DB0
		private static bool TryParseInternal(string value, HttpRangeSpecifier specifier, out string parseFailureReason)
		{
			if (specifier == null)
			{
				throw new ArgumentNullException("specifier");
			}
			HttpRangeSpecifier.StrSegment strSegment = new HttpRangeSpecifier.StrSegment(value);
			HttpRangeSpecifier.ParseState parseState = HttpRangeSpecifier.ParseState.Start;
			parseFailureReason = string.Empty;
			int i = 0;
			int length = value.Length;
			long rangeStart = -1L;
			while (i < length)
			{
				char c = value[i];
				switch (parseState)
				{
				case HttpRangeSpecifier.ParseState.Start:
					if (c != ' ' && c != '\t')
					{
						if (strSegment.Start == -1)
						{
							strSegment.Start = i;
						}
						if (c == '=')
						{
							strSegment.SetLengthFromTerminatingIndex(i);
							strSegment.Trim();
							specifier.RangeUnitSpecifier = strSegment.ToString();
							parseState = HttpRangeSpecifier.ParseState.RangeStart;
							rangeStart = -1L;
							strSegment.Reset();
						}
					}
					break;
				case HttpRangeSpecifier.ParseState.RangeStart:
					if (c != ' ' && c != '\t')
					{
						if (strSegment.Start == -1)
						{
							strSegment.Start = i;
						}
						if (c == '-' || c == ',')
						{
							strSegment.SetLengthFromTerminatingIndex(i);
							strSegment.Trim();
							if (c != '-')
							{
								parseFailureReason = "Invalid range, missing '-' character at " + (strSegment.Start + strSegment.Length);
								return false;
							}
							if (strSegment.Length > 0 && !long.TryParse(strSegment.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeStart))
							{
								parseFailureReason = "Could not parse first-byte-pos at " + strSegment.Start;
								return false;
							}
							parseState = HttpRangeSpecifier.ParseState.RangeEnd;
							strSegment.Reset();
						}
					}
					break;
				case HttpRangeSpecifier.ParseState.RangeEnd:
					if (c != ' ' && c != '\t')
					{
						if (strSegment.Start == -1)
						{
							strSegment.Start = i;
						}
						if (c == ',')
						{
							strSegment.SetLengthFromTerminatingIndex(i);
							strSegment.Trim();
							if (!HttpRangeSpecifier.ProcessRangeEnd(specifier, ref parseFailureReason, strSegment, rangeStart))
							{
								return false;
							}
							rangeStart = -1L;
							parseState = HttpRangeSpecifier.ParseState.RangeStart;
							strSegment.Reset();
						}
					}
					break;
				}
				i++;
			}
			if (strSegment.Start != -1)
			{
				strSegment.SetLengthFromTerminatingIndex(i);
				strSegment.Trim();
				if (parseState == HttpRangeSpecifier.ParseState.Start)
				{
					specifier.RangeUnitSpecifier = strSegment.ToString();
				}
				if (parseState == HttpRangeSpecifier.ParseState.RangeStart)
				{
					parseFailureReason = "Invalid range, missing '-' character at " + (strSegment.Start + strSegment.Length);
					return false;
				}
			}
			else
			{
				if (parseState == HttpRangeSpecifier.ParseState.Start)
				{
					parseFailureReason = "Did not find range unit specifier";
					return false;
				}
				if (parseState == HttpRangeSpecifier.ParseState.RangeStart)
				{
					parseFailureReason = "Expected range value at the end.";
					return false;
				}
			}
			if (parseState == HttpRangeSpecifier.ParseState.RangeEnd && !HttpRangeSpecifier.ProcessRangeEnd(specifier, ref parseFailureReason, strSegment, rangeStart))
			{
				return false;
			}
			if (specifier.RangeCollection.Count == 0)
			{
				parseFailureReason = "No ranges found.";
				return false;
			}
			return true;
		}

		// Token: 0x06000424 RID: 1060 RVA: 0x00017DE8 File Offset: 0x00015FE8
		private static bool ProcessRangeEnd(HttpRangeSpecifier specifier, ref string parseFailureReason, HttpRangeSpecifier.StrSegment currentSegment, long rangeStart)
		{
			long rangeEnd = -1L;
			if (currentSegment.Start >= 0 && currentSegment.Length > 0 && !long.TryParse(currentSegment.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeEnd))
			{
				parseFailureReason = "Could not parse last-byte-pos at " + currentSegment.Start;
				return false;
			}
			if (!HttpRangeSpecifier.AddRange(specifier, rangeStart, rangeEnd))
			{
				parseFailureReason = "Invalid range specification near " + currentSegment.Start;
				return false;
			}
			return true;
		}

		// Token: 0x06000425 RID: 1061 RVA: 0x00017E5C File Offset: 0x0001605C
		private static bool AddRange(HttpRangeSpecifier specifier, long rangeStart, long rangeEnd)
		{
			try
			{
				specifier.RangeCollection.Add(new HttpRange(rangeStart, rangeEnd));
			}
			catch (ArgumentOutOfRangeException)
			{
				return false;
			}
			return true;
		}

		// Token: 0x040002F0 RID: 752
		private readonly Collection<HttpRange> rangeCollection = new Collection<HttpRange>();

		// Token: 0x02000117 RID: 279
		private enum ParseState
		{
			// Token: 0x040004FC RID: 1276
			Start,
			// Token: 0x040004FD RID: 1277
			RangeStart,
			// Token: 0x040004FE RID: 1278
			RangeEnd
		}

		// Token: 0x02000118 RID: 280
		private class StrSegment
		{
			// Token: 0x06000852 RID: 2130 RVA: 0x0002D555 File Offset: 0x0002B755
			public StrSegment(string source)
			{
				if (source == null)
				{
					throw new ArgumentNullException("source");
				}
				this.source = source;
				this.Reset();
			}

			// Token: 0x170001B3 RID: 435
			// (get) Token: 0x06000853 RID: 2131 RVA: 0x0002D578 File Offset: 0x0002B778
			// (set) Token: 0x06000854 RID: 2132 RVA: 0x0002D580 File Offset: 0x0002B780
			public int Start { get; set; }

			// Token: 0x170001B4 RID: 436
			// (get) Token: 0x06000855 RID: 2133 RVA: 0x0002D589 File Offset: 0x0002B789
			// (set) Token: 0x06000856 RID: 2134 RVA: 0x0002D591 File Offset: 0x0002B791
			public int Length { get; set; }

			// Token: 0x06000857 RID: 2135 RVA: 0x0002D59A File Offset: 0x0002B79A
			public void SetLengthFromTerminatingIndex(int terminatingIndex)
			{
				this.Length = terminatingIndex - this.Start;
			}

			// Token: 0x06000858 RID: 2136 RVA: 0x0002D5AC File Offset: 0x0002B7AC
			public void Trim()
			{
				if (this.Start + this.Length > this.source.Length)
				{
					throw new InvalidOperationException("Source too short.");
				}
				while (this.Length > 0 && this.Start < this.source.Length)
				{
					if (!char.IsWhiteSpace(this.source[this.Start]))
					{
						break;
					}
					int num = this.Start;
					this.Start = num + 1;
					num = this.Length;
					this.Length = num - 1;
				}
				while (this.Length > 0 && char.IsWhiteSpace(this.source[this.Start + this.Length - 1]))
				{
					int num = this.Length;
					this.Length = num - 1;
				}
			}

			// Token: 0x06000859 RID: 2137 RVA: 0x0002D66E File Offset: 0x0002B86E
			public void Reset()
			{
				this.Start = -1;
				this.Length = 0;
			}

			// Token: 0x0600085A RID: 2138 RVA: 0x0002D67E File Offset: 0x0002B87E
			public override string ToString()
			{
				return this.source.Substring(this.Start, this.Length);
			}

			// Token: 0x040004FF RID: 1279
			private readonly string source;
		}
	}
}
