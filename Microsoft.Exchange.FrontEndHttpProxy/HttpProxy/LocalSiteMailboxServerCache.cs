using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.ConfigurationSettings;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000046 RID: 70
	internal class LocalSiteMailboxServerCache
	{
		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000250 RID: 592 RVA: 0x0000BA6C File Offset: 0x00009C6C
		public static LocalSiteMailboxServerCache Instance
		{
			get
			{
				if (LocalSiteMailboxServerCache.instance == null)
				{
					object obj = LocalSiteMailboxServerCache.staticLock;
					lock (obj)
					{
						if (LocalSiteMailboxServerCache.instance == null)
						{
							LocalSiteMailboxServerCache.instance = new LocalSiteMailboxServerCache();
						}
					}
				}
				return LocalSiteMailboxServerCache.instance;
			}
		}

		// Token: 0x06000251 RID: 593 RVA: 0x0000BAC4 File Offset: 0x00009CC4
		public BackEndServer TryGetRandomE15Server(IRequestContext requestContext)
		{
			if (!LocalSiteMailboxServerCache.CacheLocalSiteLiveE15Servers.Value)
			{
				return null;
			}
			Guid[] array = null;
			try
			{
				this.localSiteServersLock.Wait();
				array = this.localSiteLiveE15Servers.ToArray();
			}
			finally
			{
				this.localSiteServersLock.Release();
			}
			if (array.Length != 0)
			{
				int num = this.random.Next(array.Length);
				int num2 = num;
				BackEndServer backEndServer;
				for (;;)
				{
					Guid database = array[num];
					if (MailboxServerCache.Instance.TryGet(database, requestContext, out backEndServer))
					{
						break;
					}
					num2++;
					if (num2 >= array.Length)
					{
						num2 = 0;
					}
					if (num2 == num)
					{
						goto IL_9B;
					}
				}
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<BackEndServer>((long)this.GetHashCode(), "[LocalSiteMailboxServerCache::TryGetRandomE15Server]: Found server {0} from local site E15 server list.", backEndServer);
				}
				return backEndServer;
			}
			IL_9B:
			return null;
		}

		// Token: 0x06000252 RID: 594 RVA: 0x0000BB80 File Offset: 0x00009D80
		internal void Add(Guid database, BackEndServer backEndServer, string resourceForest)
		{
			if (LocalSiteMailboxServerCache.CacheLocalSiteLiveE15Servers.Value && this.IsLocalSiteE15MailboxServer(backEndServer, resourceForest))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<Guid, BackEndServer>((long)this.GetHashCode(), "[LocalSiteMailboxServerCache::Add]: Adding Database {0} on Server {1} to local E15 mailbox server collection.", database, backEndServer);
				}
				try
				{
					this.localSiteServersLock.Wait();
					if (!this.localSiteLiveE15Servers.Contains(database))
					{
						this.localSiteLiveE15Servers.Add(database);
					}
				}
				finally
				{
					this.localSiteServersLock.Release();
				}
				this.UpdateLocalSiteMailboxServerListCounter();
			}
		}

		// Token: 0x06000253 RID: 595 RVA: 0x0000BC14 File Offset: 0x00009E14
		internal void Remove(Guid database)
		{
			if (LocalSiteMailboxServerCache.CacheLocalSiteLiveE15Servers.Value)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<Guid>((long)this.GetHashCode(), "[LocalSiteMailboxServerCache::Remove]: Removing Database {0} from the local E15 mailbox server collection.", database);
				}
				try
				{
					this.localSiteServersLock.Wait();
					this.localSiteLiveE15Servers.Remove(database);
				}
				finally
				{
					this.localSiteServersLock.Release();
				}
				this.UpdateLocalSiteMailboxServerListCounter();
			}
		}

		// Token: 0x06000254 RID: 596 RVA: 0x0000BC90 File Offset: 0x00009E90
		private bool IsLocalSiteE15MailboxServer(BackEndServer server, string resourceForest)
		{
			if (!server.IsE15OrHigher)
			{
				return false;
			}
			if ((!Utilities.IsPartnerHostedOnly && !CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).NoCrossForestServerLocate.Enabled) || string.IsNullOrEmpty(resourceForest) || string.Equals(HttpProxyGlobals.LocalMachineForest.Member, resourceForest, StringComparison.OrdinalIgnoreCase))
			{
				ServiceTopology currentServiceTopology = ServiceTopology.GetCurrentServiceTopology("d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Cache\\LocalSiteMailboxServerCache.cs", "IsLocalSiteE15MailboxServer", 238);
				Site site = null;
				try
				{
					site = currentServiceTopology.GetSite(server.Fqdn, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Cache\\LocalSiteMailboxServerCache.cs", "IsLocalSiteE15MailboxServer", 243);
				}
				catch (ServerNotFoundException)
				{
					return false;
				}
				catch (ServerNotInSiteException)
				{
					return false;
				}
				if (HttpProxyGlobals.LocalSite.Member.Equals(site))
				{
					return true;
				}
				return false;
			}
			return false;
		}

		// Token: 0x06000255 RID: 597 RVA: 0x0000BD54 File Offset: 0x00009F54
		private void UpdateLocalSiteMailboxServerListCounter()
		{
			PerfCounters.HttpProxyCacheCountersInstance.BackEndServerCacheLocalServerListCount.RawValue = (long)this.localSiteLiveE15Servers.Count;
		}

		// Token: 0x04000147 RID: 327
		private static readonly LazyFlightingSetting<bool> CacheLocalSiteLiveE15Servers = new LazyFlightingSetting<bool>(() => CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).CacheLocalSiteLiveE15Servers.Enabled);

		// Token: 0x04000148 RID: 328
		private static LocalSiteMailboxServerCache instance;

		// Token: 0x04000149 RID: 329
		private static object staticLock = new object();

		// Token: 0x0400014A RID: 330
		private List<Guid> localSiteLiveE15Servers = new List<Guid>();

		// Token: 0x0400014B RID: 331
		private SemaphoreSlim localSiteServersLock = new SemaphoreSlim(1);

		// Token: 0x0400014C RID: 332
		private Random random = new Random();
	}
}
