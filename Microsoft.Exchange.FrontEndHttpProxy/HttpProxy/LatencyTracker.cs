using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using Microsoft.Exchange.Diagnostics;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000089 RID: 137
	internal class LatencyTracker
	{
		// Token: 0x06000483 RID: 1155 RVA: 0x00019558 File Offset: 0x00017758
		internal LatencyTracker()
		{
			this.latencyTrackerStopwatch.Start();
			this.glsLatencies = new List<long>(4);
			this.accountForestLatencies = new List<long>(4);
			this.resourceForestLatencies = new List<long>(4);
			this.sharedCacheLatencies = new List<long>(4);
		}

		// Token: 0x17000107 RID: 263
		// (get) Token: 0x06000484 RID: 1156 RVA: 0x000195BC File Offset: 0x000177BC
		internal string GlsLatencyBreakup
		{
			get
			{
				return LatencyTracker.GetBreakupOfLatencies(this.glsLatencies);
			}
		}

		// Token: 0x17000108 RID: 264
		// (get) Token: 0x06000485 RID: 1157 RVA: 0x000195C9 File Offset: 0x000177C9
		internal long TotalGlsLatency
		{
			get
			{
				return this.glsLatencies.Sum();
			}
		}

		// Token: 0x17000109 RID: 265
		// (get) Token: 0x06000486 RID: 1158 RVA: 0x000195D6 File Offset: 0x000177D6
		internal string AccountForestLatencyBreakup
		{
			get
			{
				return LatencyTracker.GetBreakupOfLatencies(this.accountForestLatencies);
			}
		}

		// Token: 0x1700010A RID: 266
		// (get) Token: 0x06000487 RID: 1159 RVA: 0x000195E3 File Offset: 0x000177E3
		internal long TotalAccountForestDirectoryLatency
		{
			get
			{
				return this.accountForestLatencies.Sum();
			}
		}

		// Token: 0x1700010B RID: 267
		// (get) Token: 0x06000488 RID: 1160 RVA: 0x000195F0 File Offset: 0x000177F0
		internal string ResourceForestLatencyBreakup
		{
			get
			{
				return LatencyTracker.GetBreakupOfLatencies(this.resourceForestLatencies);
			}
		}

		// Token: 0x1700010C RID: 268
		// (get) Token: 0x06000489 RID: 1161 RVA: 0x000195FD File Offset: 0x000177FD
		internal long TotalResourceForestDirectoryLatency
		{
			get
			{
				return this.resourceForestLatencies.Sum();
			}
		}

		// Token: 0x1700010D RID: 269
		// (get) Token: 0x0600048A RID: 1162 RVA: 0x0001960A File Offset: 0x0001780A
		internal long AdLatency
		{
			get
			{
				return this.TotalAccountForestDirectoryLatency + this.TotalResourceForestDirectoryLatency;
			}
		}

		// Token: 0x1700010E RID: 270
		// (get) Token: 0x0600048B RID: 1163 RVA: 0x00019619 File Offset: 0x00017819
		internal string SharedCacheLatencyBreakup
		{
			get
			{
				return LatencyTracker.GetBreakupOfLatencies(this.sharedCacheLatencies);
			}
		}

		// Token: 0x1700010F RID: 271
		// (get) Token: 0x0600048C RID: 1164 RVA: 0x00019626 File Offset: 0x00017826
		internal long TotalSharedCacheLatency
		{
			get
			{
				return this.sharedCacheLatencies.Sum();
			}
		}

		// Token: 0x0600048D RID: 1165 RVA: 0x00019633 File Offset: 0x00017833
		internal static LatencyTracker FromHttpContext(HttpContext httpContext)
		{
			return (LatencyTracker)httpContext.Items[Constants.LatencyTrackerContextKeyName];
		}

		// Token: 0x0600048E RID: 1166 RVA: 0x0001964C File Offset: 0x0001784C
		internal static void GetLatency(Action operationToTrack, out long latency)
		{
			Stopwatch stopwatch = new Stopwatch();
			latency = 0L;
			try
			{
				stopwatch.Start();
				operationToTrack();
			}
			finally
			{
				stopwatch.Stop();
				latency = stopwatch.ElapsedMilliseconds;
			}
		}

		// Token: 0x0600048F RID: 1167 RVA: 0x00019690 File Offset: 0x00017890
		internal static T GetLatency<T>(Func<T> operationToTrack, out long latency)
		{
			Stopwatch stopwatch = new Stopwatch();
			latency = 0L;
			T result;
			try
			{
				stopwatch.Start();
				result = operationToTrack();
			}
			finally
			{
				stopwatch.Stop();
				latency = stopwatch.ElapsedMilliseconds;
			}
			return result;
		}

		// Token: 0x06000490 RID: 1168 RVA: 0x000196D8 File Offset: 0x000178D8
		internal void LogElapsedTime(RequestDetailsLogger logger, string latencyName)
		{
			if (HttpProxySettings.DetailedLatencyTracingEnabled.Value)
			{
				long currentLatency = this.GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(logger, latencyName, currentLatency);
			}
		}

		// Token: 0x06000491 RID: 1169 RVA: 0x00019708 File Offset: 0x00017908
		internal void LogElapsedTimeAsLatency(RequestDetailsLogger logger, LatencyTrackerKey trackerKey, HttpProxyMetadata protocolLogKey)
		{
			long currentLatency = this.GetCurrentLatency(trackerKey);
			if (currentLatency >= 0L)
			{
				logger.UpdateLatency(protocolLogKey, (double)currentLatency);
			}
		}

		// Token: 0x06000492 RID: 1170 RVA: 0x00019730 File Offset: 0x00017930
		internal void StartTracking(LatencyTrackerKey trackingKey, bool resetValue = false)
		{
			if (!this.latencyTrackerStartTimes.ContainsKey(trackingKey))
			{
				this.latencyTrackerStartTimes.Add(trackingKey, this.latencyTrackerStopwatch.ElapsedMilliseconds);
				return;
			}
			this.latencyTrackerStartTimes[trackingKey] = this.latencyTrackerStopwatch.ElapsedMilliseconds;
		}

		// Token: 0x06000493 RID: 1171 RVA: 0x0001976F File Offset: 0x0001796F
		internal long GetCurrentLatency(LatencyTrackerKey trackingKey)
		{
			if (this.latencyTrackerStartTimes.ContainsKey(trackingKey))
			{
				return this.latencyTrackerStopwatch.ElapsedMilliseconds - this.latencyTrackerStartTimes[trackingKey];
			}
			return -1L;
		}

		// Token: 0x06000494 RID: 1172 RVA: 0x0001979A File Offset: 0x0001799A
		internal void HandleGlsLatency(long latency)
		{
			this.glsLatencies.Add(latency);
		}

		// Token: 0x06000495 RID: 1173 RVA: 0x000197A8 File Offset: 0x000179A8
		internal void HandleGlsLatency(List<long> latencies)
		{
			this.glsLatencies.AddRange(latencies);
		}

		// Token: 0x06000496 RID: 1174 RVA: 0x000197B6 File Offset: 0x000179B6
		internal void HandleAccountLatency(long latency)
		{
			this.accountForestLatencies.Add(latency);
		}

		// Token: 0x06000497 RID: 1175 RVA: 0x000197C4 File Offset: 0x000179C4
		internal void HandleResourceLatency(long latency)
		{
			this.resourceForestLatencies.Add(latency);
		}

		// Token: 0x06000498 RID: 1176 RVA: 0x000197D2 File Offset: 0x000179D2
		internal void HandleResourceLatency(List<long> latencies)
		{
			this.resourceForestLatencies.AddRange(latencies);
		}

		// Token: 0x06000499 RID: 1177 RVA: 0x000197E0 File Offset: 0x000179E0
		internal void HandleSharedCacheLatency(long latency)
		{
			this.sharedCacheLatencies.Add(latency);
		}

		// Token: 0x0600049A RID: 1178 RVA: 0x000197F0 File Offset: 0x000179F0
		private static string GetBreakupOfLatencies(List<long> latencies)
		{
			if (latencies == null)
			{
				throw new ArgumentNullException("latencies");
			}
			StringBuilder result = new StringBuilder();
			latencies.ForEach(delegate(long latency)
			{
				result.Append(latency);
				result.Append(';');
			});
			return result.ToString();
		}

		// Token: 0x0400032B RID: 811
		internal const string SelectHandlerTime = "SelectHandler";

		// Token: 0x0400032C RID: 812
		private readonly List<long> glsLatencies;

		// Token: 0x0400032D RID: 813
		private readonly List<long> accountForestLatencies;

		// Token: 0x0400032E RID: 814
		private readonly List<long> resourceForestLatencies;

		// Token: 0x0400032F RID: 815
		private readonly List<long> sharedCacheLatencies;

		// Token: 0x04000330 RID: 816
		private Stopwatch latencyTrackerStopwatch = new Stopwatch();

		// Token: 0x04000331 RID: 817
		private Dictionary<LatencyTrackerKey, long> latencyTrackerStartTimes = new Dictionary<LatencyTrackerKey, long>();
	}
}
