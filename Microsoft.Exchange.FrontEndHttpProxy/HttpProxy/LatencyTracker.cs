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
		// Token: 0x0600047F RID: 1151 RVA: 0x00019398 File Offset: 0x00017598
		internal LatencyTracker()
		{
			this.latencyTrackerStopwatch.Start();
			this.glsLatencies = new List<long>(4);
			this.accountForestLatencies = new List<long>(4);
			this.resourceForestLatencies = new List<long>(4);
			this.sharedCacheLatencies = new List<long>(4);
		}

		// Token: 0x17000107 RID: 263
		// (get) Token: 0x06000480 RID: 1152 RVA: 0x000193FC File Offset: 0x000175FC
		internal string GlsLatencyBreakup
		{
			get
			{
				return LatencyTracker.GetBreakupOfLatencies(this.glsLatencies);
			}
		}

		// Token: 0x17000108 RID: 264
		// (get) Token: 0x06000481 RID: 1153 RVA: 0x00019409 File Offset: 0x00017609
		internal long TotalGlsLatency
		{
			get
			{
				return this.glsLatencies.Sum();
			}
		}

		// Token: 0x17000109 RID: 265
		// (get) Token: 0x06000482 RID: 1154 RVA: 0x00019416 File Offset: 0x00017616
		internal string AccountForestLatencyBreakup
		{
			get
			{
				return LatencyTracker.GetBreakupOfLatencies(this.accountForestLatencies);
			}
		}

		// Token: 0x1700010A RID: 266
		// (get) Token: 0x06000483 RID: 1155 RVA: 0x00019423 File Offset: 0x00017623
		internal long TotalAccountForestDirectoryLatency
		{
			get
			{
				return this.accountForestLatencies.Sum();
			}
		}

		// Token: 0x1700010B RID: 267
		// (get) Token: 0x06000484 RID: 1156 RVA: 0x00019430 File Offset: 0x00017630
		internal string ResourceForestLatencyBreakup
		{
			get
			{
				return LatencyTracker.GetBreakupOfLatencies(this.resourceForestLatencies);
			}
		}

		// Token: 0x1700010C RID: 268
		// (get) Token: 0x06000485 RID: 1157 RVA: 0x0001943D File Offset: 0x0001763D
		internal long TotalResourceForestDirectoryLatency
		{
			get
			{
				return this.resourceForestLatencies.Sum();
			}
		}

		// Token: 0x1700010D RID: 269
		// (get) Token: 0x06000486 RID: 1158 RVA: 0x0001944A File Offset: 0x0001764A
		internal long AdLatency
		{
			get
			{
				return this.TotalAccountForestDirectoryLatency + this.TotalResourceForestDirectoryLatency;
			}
		}

		// Token: 0x1700010E RID: 270
		// (get) Token: 0x06000487 RID: 1159 RVA: 0x00019459 File Offset: 0x00017659
		internal string SharedCacheLatencyBreakup
		{
			get
			{
				return LatencyTracker.GetBreakupOfLatencies(this.sharedCacheLatencies);
			}
		}

		// Token: 0x1700010F RID: 271
		// (get) Token: 0x06000488 RID: 1160 RVA: 0x00019466 File Offset: 0x00017666
		internal long TotalSharedCacheLatency
		{
			get
			{
				return this.sharedCacheLatencies.Sum();
			}
		}

		// Token: 0x06000489 RID: 1161 RVA: 0x00019473 File Offset: 0x00017673
		internal static LatencyTracker FromHttpContext(HttpContext httpContext)
		{
			return (LatencyTracker)httpContext.Items[Constants.LatencyTrackerContextKeyName];
		}

		// Token: 0x0600048A RID: 1162 RVA: 0x0001948C File Offset: 0x0001768C
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

		// Token: 0x0600048B RID: 1163 RVA: 0x000194D0 File Offset: 0x000176D0
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

		// Token: 0x0600048C RID: 1164 RVA: 0x00019518 File Offset: 0x00017718
		internal void LogElapsedTime(RequestDetailsLogger logger, string latencyName)
		{
			if (HttpProxySettings.DetailedLatencyTracingEnabled.Value)
			{
				long currentLatency = this.GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(logger, latencyName, currentLatency);
			}
		}

		// Token: 0x0600048D RID: 1165 RVA: 0x00019548 File Offset: 0x00017748
		internal void LogElapsedTimeAsLatency(RequestDetailsLogger logger, LatencyTrackerKey trackerKey, HttpProxyMetadata protocolLogKey)
		{
			long currentLatency = this.GetCurrentLatency(trackerKey);
			if (currentLatency >= 0L)
			{
				logger.UpdateLatency(protocolLogKey, (double)currentLatency);
			}
		}

		// Token: 0x0600048E RID: 1166 RVA: 0x00019570 File Offset: 0x00017770
		internal void StartTracking(LatencyTrackerKey trackingKey, bool resetValue = false)
		{
			if (!this.latencyTrackerStartTimes.ContainsKey(trackingKey))
			{
				this.latencyTrackerStartTimes.Add(trackingKey, this.latencyTrackerStopwatch.ElapsedMilliseconds);
				return;
			}
			this.latencyTrackerStartTimes[trackingKey] = this.latencyTrackerStopwatch.ElapsedMilliseconds;
		}

		// Token: 0x0600048F RID: 1167 RVA: 0x000195AF File Offset: 0x000177AF
		internal long GetCurrentLatency(LatencyTrackerKey trackingKey)
		{
			if (this.latencyTrackerStartTimes.ContainsKey(trackingKey))
			{
				return this.latencyTrackerStopwatch.ElapsedMilliseconds - this.latencyTrackerStartTimes[trackingKey];
			}
			return -1L;
		}

		// Token: 0x06000490 RID: 1168 RVA: 0x000195DA File Offset: 0x000177DA
		internal void HandleGlsLatency(long latency)
		{
			this.glsLatencies.Add(latency);
		}

		// Token: 0x06000491 RID: 1169 RVA: 0x000195E8 File Offset: 0x000177E8
		internal void HandleGlsLatency(List<long> latencies)
		{
			this.glsLatencies.AddRange(latencies);
		}

		// Token: 0x06000492 RID: 1170 RVA: 0x000195F6 File Offset: 0x000177F6
		internal void HandleAccountLatency(long latency)
		{
			this.accountForestLatencies.Add(latency);
		}

		// Token: 0x06000493 RID: 1171 RVA: 0x00019604 File Offset: 0x00017804
		internal void HandleResourceLatency(long latency)
		{
			this.resourceForestLatencies.Add(latency);
		}

		// Token: 0x06000494 RID: 1172 RVA: 0x00019612 File Offset: 0x00017812
		internal void HandleResourceLatency(List<long> latencies)
		{
			this.resourceForestLatencies.AddRange(latencies);
		}

		// Token: 0x06000495 RID: 1173 RVA: 0x00019620 File Offset: 0x00017820
		internal void HandleSharedCacheLatency(long latency)
		{
			this.sharedCacheLatencies.Add(latency);
		}

		// Token: 0x06000496 RID: 1174 RVA: 0x00019630 File Offset: 0x00017830
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

		// Token: 0x04000327 RID: 807
		internal const string SelectHandlerTime = "SelectHandler";

		// Token: 0x04000328 RID: 808
		private readonly List<long> glsLatencies;

		// Token: 0x04000329 RID: 809
		private readonly List<long> accountForestLatencies;

		// Token: 0x0400032A RID: 810
		private readonly List<long> resourceForestLatencies;

		// Token: 0x0400032B RID: 811
		private readonly List<long> sharedCacheLatencies;

		// Token: 0x0400032C RID: 812
		private Stopwatch latencyTrackerStopwatch = new Stopwatch();

		// Token: 0x0400032D RID: 813
		private Dictionary<LatencyTrackerKey, long> latencyTrackerStartTimes = new Dictionary<LatencyTrackerKey, long>();
	}
}
