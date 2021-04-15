using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.ExchangeSystem;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.HttpProxy.EventLogs;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200003C RID: 60
	internal sealed class DownLevelServerPingManager : DisposeTrackableBase
	{
		// Token: 0x060001FB RID: 507 RVA: 0x00009F44 File Offset: 0x00008144
		public DownLevelServerPingManager(Func<Dictionary<string, List<DownLevelServerStatusEntry>>> downLevelServerMapGetter)
		{
			if (downLevelServerMapGetter == null)
			{
				throw new ArgumentNullException("downLevelServerMapGetter");
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[DownLevelServerPingManager::Ctor]: Instantiating.");
			}
			this.downLevelServerMapGetter = downLevelServerMapGetter;
			this.workerTimer = new Timer(delegate(object o)
			{
				this.RefreshServerStatus();
			}, null, TimeSpan.Zero, DownLevelServerPingManager.DownLevelServerPingInterval.Value);
		}

		// Token: 0x060001FC RID: 508 RVA: 0x00009FEC File Offset: 0x000081EC
		protected override void InternalDispose(bool disposing)
		{
			if (disposing && this.workerTimer != null)
			{
				this.workerTimer.Dispose();
				this.workerTimer = null;
			}
		}

		// Token: 0x060001FD RID: 509 RVA: 0x0000A00B File Offset: 0x0000820B
		protected override DisposeTracker InternalGetDisposeTracker()
		{
			return DisposeTracker.Get<DownLevelServerPingManager>(this);
		}

		// Token: 0x060001FE RID: 510 RVA: 0x0000A014 File Offset: 0x00008214
		private void RefreshServerStatus()
		{
			try
			{
				if (this.workerSignal.WaitOne(0))
				{
					this.workerSignal.Reset();
					try
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[DownLevelServerPingManager::RefreshServerStatus]: Refreshing server map.");
						}
						Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_RefreshingDownLevelServerStatus, null, new object[]
						{
							HttpProxyGlobals.ProtocolType.ToString()
						});
						this.InternalRefresh();
						PerfCounters.HttpProxyCountersInstance.DownLevelServersLastPing.RawValue = Stopwatch.GetTimestamp();
					}
					finally
					{
						this.workerSignal.Set();
					}
				}
			}
			catch (Exception ex)
			{
				Diagnostics.ReportException(ex, FrontEndHttpProxyEventLogConstants.Tuple_InternalServerError, null, "Exception from RefreshServerStatus: {0}");
			}
		}

		// Token: 0x060001FF RID: 511 RVA: 0x0000A0EC File Offset: 0x000082EC
		private void InternalRefresh()
		{
			Dictionary<string, List<DownLevelServerStatusEntry>> dictionary = this.downLevelServerMapGetter();
			int num = 0;
			int num2 = 0;
			ServiceTopology serviceTopology = null;
			try
			{
				serviceTopology = ServiceTopology.GetCurrentServiceTopology(DownLevelServerPingManager.DownLevelServerPingServiceTopologyTimeout.Value, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerPingManager.cs", "InternalRefresh", 209);
			}
			catch (ReadTopologyTimeoutException)
			{
			}
			foreach (List<DownLevelServerStatusEntry> list in dictionary.Values)
			{
				foreach (DownLevelServerStatusEntry downLevelServerStatusEntry in list)
				{
					if (serviceTopology != null && serviceTopology.IsServerOutOfService(downLevelServerStatusEntry.BackEndServer.Fqdn, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerPingManager.cs", "InternalRefresh", 220))
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(2))
						{
							ExTraceGlobals.VerboseTracer.TraceWarning<BackEndServer>((long)this.GetHashCode(), "[DownLevelServerPingManager::InternalRefresh]: Skipping server {0} because it's marked as OutOfService.", downLevelServerStatusEntry.BackEndServer);
						}
						downLevelServerStatusEntry.IsHealthy = false;
					}
					else
					{
						Uri uri = this.pingStrategy.Member.BuildUrl(downLevelServerStatusEntry.BackEndServer.Fqdn);
						Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_PingingDownLevelServer, downLevelServerStatusEntry.BackEndServer.Fqdn, new object[]
						{
							HttpProxyGlobals.ProtocolType.ToString(),
							downLevelServerStatusEntry.BackEndServer.Fqdn,
							uri
						});
						Exception ex = this.pingStrategy.Member.Ping(uri);
						if (ex != null)
						{
							ex = this.pingStrategy.Member.Ping(uri);
						}
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<Uri, Exception>((long)this.GetHashCode(), "[DownLevelServerPingManager::InternalRefresh]: Tested endpoint {0} with result {1}.", uri, ex);
						}
						if (ex != null)
						{
							downLevelServerStatusEntry.IsHealthy = false;
							Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_MarkingDownLevelServerUnhealthy, downLevelServerStatusEntry.BackEndServer.Fqdn, new object[]
							{
								HttpProxyGlobals.ProtocolType.ToString(),
								downLevelServerStatusEntry.BackEndServer.Fqdn,
								uri,
								ex.ToString()
							});
						}
						else
						{
							num2++;
							downLevelServerStatusEntry.IsHealthy = true;
						}
					}
					num++;
				}
			}
			PerfCounters.HttpProxyCountersInstance.DownLevelTotalServers.RawValue = (long)num;
			PerfCounters.HttpProxyCountersInstance.DownLevelHealthyServers.RawValue = (long)num2;
		}

		// Token: 0x0400011B RID: 283
		private static readonly TimeSpanAppSettingsEntry DownLevelServerPingInterval = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("DownLevelServerPingInterval"), 0, TimeSpan.FromSeconds(60.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x0400011C RID: 284
		private static readonly TimeSpanAppSettingsEntry DownLevelServerPingServiceTopologyTimeout = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("DownLevelServerPingServiceTopologyTimeout"), 0, TimeSpan.FromSeconds(180.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x0400011D RID: 285
		private static readonly Dictionary<ProtocolType, ProtocolPingStrategyBase> CustomStrategies = new Dictionary<ProtocolType, ProtocolPingStrategyBase>
		{
			{
				4,
				new OwaPingStrategy()
			},
			{
				8,
				new RpcHttpPingStrategy()
			}
		};

		// Token: 0x0400011E RID: 286
		private Func<Dictionary<string, List<DownLevelServerStatusEntry>>> downLevelServerMapGetter;

		// Token: 0x0400011F RID: 287
		private Timer workerTimer;

		// Token: 0x04000120 RID: 288
		private ManualResetEvent workerSignal = new ManualResetEvent(true);

		// Token: 0x04000121 RID: 289
		private LazyMember<ProtocolPingStrategyBase> pingStrategy = new LazyMember<ProtocolPingStrategyBase>(delegate()
		{
			ProtocolPingStrategyBase result = null;
			if (DownLevelServerPingManager.CustomStrategies.TryGetValue(HttpProxyGlobals.ProtocolType, out result))
			{
				return result;
			}
			return new DefaultPingStrategy();
		});
	}
}
