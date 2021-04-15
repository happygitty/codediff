using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200007A RID: 122
	internal class HttpRangeSpecifier
	{
		// Token: 0x06000422 RID: 1058 RVA: 0x00017CFC File Offset: 0x00015EFC
		public HttpRangeSpecifier()
		{
			this.RangeUnitSpecifier = "bytes";
		}

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x06000423 RID: 1059 RVA: 0x00017D1A File Offset: 0x00015F1A
		public Collection<HttpRange> RangeCollection
		{
			get
			{
				return this.rangeCollection;
			}
		}

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x06000424 RID: 1060 RVA: 0x00017D22 File Offset: 0x00015F22
		// (set) Token: 0x06000425 RID: 1061 RVA: 0x00017D2A File Offset: 0x00015F2A
		public string RangeUnitSpecifier { get; set; }

		// Token: 0x06000426 RID: 1062 RVA: 0x00017D34 File Offset: 0x00015F34
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

		// Token: 0x06000427 RID: 1063 RVA: 0x00017D70 File Offset: 0x00015F70
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

		// Token: 0x06000428 RID: 1064 RVA: 0x00017FA8 File Offset: 0x000161A8
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

		// Token: 0x06000429 RID: 1065 RVA: 0x0001801C File Offset: 0x0001621C
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

		// Token: 0x040002F4 RID: 756
		private readonly Collection<HttpRange> rangeCollection = new Collection<HttpRange>();

		// Token: 0x02000116 RID: 278
		private enum ParseState
		{
			// Token: 0x04000500 RID: 1280
			Start,
			// Token: 0x04000501 RID: 1281
			RangeStart,
			// Token: 0x04000502 RID: 1282
			RangeEnd
		}

		// Token: 0x02000117 RID: 279
		private class StrSegment
		{
			// Token: 0x0600084D RID: 2125 RVA: 0x0002D73D File Offset: 0x0002B93D
			public StrSegment(string source)
			{
				if (source == null)
				{
					throw new ArgumentNullException("source");
				}
				this.source = source;
				this.Reset();
			}

			// Token: 0x170001B1 RID: 433
			// (get) Token: 0x0600084E RID: 2126 RVA: 0x0002D760 File Offset: 0x0002B960
			// (set) Token: 0x0600084F RID: 2127 RVA: 0x0002D768 File Offset: 0x0002B968
			public int Start { get; set; }

			// Token: 0x170001B2 RID: 434
			// (get) Token: 0x06000850 RID: 2128 RVA: 0x0002D771 File Offset: 0x0002B971
			// (set) Token: 0x06000851 RID: 2129 RVA: 0x0002D779 File Offset: 0x0002B979
			public int Length { get; set; }

			// Token: 0x06000852 RID: 2130 RVA: 0x0002D782 File Offset: 0x0002B982
			public void SetLengthFromTerminatingIndex(int terminatingIndex)
			{
				this.Length = terminatingIndex - this.Start;
			}

			// Token: 0x06000853 RID: 2131 RVA: 0x0002D794 File Offset: 0x0002B994
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

			// Token: 0x06000854 RID: 2132 RVA: 0x0002D856 File Offset: 0x0002BA56
			public void Reset()
			{
				this.Start = -1;
				this.Length = 0;
			}

			// Token: 0x06000855 RID: 2133 RVA: 0x0002D866 File Offset: 0x0002BA66
			public override string ToString()
			{
				return this.source.Substring(this.Start, this.Length);
			}

			// Token: 0x04000503 RID: 1283
			private readonly string source;
		}
	}
}
