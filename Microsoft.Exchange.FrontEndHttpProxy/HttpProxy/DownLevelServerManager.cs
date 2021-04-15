using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.ConfigurationSettings;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.EventLogs;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200003B RID: 59
	internal sealed class DownLevelServerManager
	{
		// Token: 0x060001E4 RID: 484 RVA: 0x00008FDB File Offset: 0x000071DB
		private DownLevelServerManager()
		{
		}

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x060001E5 RID: 485 RVA: 0x00009000 File Offset: 0x00007200
		public static bool IsApplicable
		{
			get
			{
				switch (HttpProxyGlobals.ProtocolType)
				{
				case 0:
				case 1:
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 9:
				case 12:
					return true;
				case 2:
					return CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).AllowEWSDownLevelProxy.Enabled;
				case 8:
					return CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).AllowRPCDownLevelProxy.Enabled;
				}
				return false;
			}
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x060001E6 RID: 486 RVA: 0x00009080 File Offset: 0x00007280
		public static DownLevelServerManager Instance
		{
			get
			{
				if (DownLevelServerManager.instance == null)
				{
					object obj = DownLevelServerManager.staticLock;
					lock (obj)
					{
						if (DownLevelServerManager.instance == null)
						{
							DownLevelServerManager.instance = new DownLevelServerManager();
						}
					}
				}
				return DownLevelServerManager.instance;
			}
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x000090D8 File Offset: 0x000072D8
		public static bool IsServerDiscoverable(string fqdn)
		{
			if (string.IsNullOrEmpty(fqdn))
			{
				throw new ArgumentNullException("fqdn");
			}
			try
			{
				ServiceTopology.GetCurrentLegacyServiceTopology("d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerManager.cs", "IsServerDiscoverable", 179).GetSite(fqdn, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerManager.cs", "IsServerDiscoverable", 180);
			}
			catch (ServerNotFoundException)
			{
				return false;
			}
			catch (ServerNotInSiteException)
			{
				return false;
			}
			return true;
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x00009150 File Offset: 0x00007350
		public void Initialize()
		{
			if (this.serverMapUpdateTimer != null)
			{
				return;
			}
			object obj = this.instanceLock;
			lock (obj)
			{
				if (this.serverMapUpdateTimer == null)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[DownLevelServerManager::Initialize]: Initializing.");
					}
					this.RefreshServerMap(false);
					this.serverMapUpdateTimer = new Timer(delegate(object o)
					{
						this.RefreshServerMap(true);
					}, null, DownLevelServerManager.DownLevelServerMapRefreshInterval.Value, DownLevelServerManager.DownLevelServerMapRefreshInterval.Value);
				}
			}
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x000091F4 File Offset: 0x000073F4
		public BackEndServer GetDownLevelClientAccessServerWithPreferredServer<ServiceType>(AnchorMailbox anchorMailbox, string preferredCasServerFqdn, ClientAccessType clientAccessType, RequestDetailsLogger logger, int destinationVersion) where ServiceType : HttpService
		{
			if (anchorMailbox == null)
			{
				throw new ArgumentNullException("anchorMailbox");
			}
			if (string.IsNullOrEmpty(preferredCasServerFqdn))
			{
				throw new ArgumentException("preferredCasServerFqdn cannot be empty!");
			}
			ServiceTopology currentLegacyServiceTopology = ServiceTopology.GetCurrentLegacyServiceTopology("d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerManager.cs", "GetDownLevelClientAccessServerWithPreferredServer", 259);
			Site site = currentLegacyServiceTopology.GetSite(preferredCasServerFqdn, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerManager.cs", "GetDownLevelClientAccessServerWithPreferredServer", 260);
			Dictionary<string, List<DownLevelServerStatusEntry>> downLevelServerMap = this.GetDownLevelServerMap();
			List<DownLevelServerStatusEntry> list = null;
			if (!downLevelServerMap.TryGetValue(site.DistinguishedName, out list))
			{
				string text = string.Format("Unable to find site {0} in the down level server map.", site.DistinguishedName);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[DownLevelServerManager::GetDownLevelClientAccessServerWithPreferredServer]: {0}", text);
				}
				ThreadPool.QueueUserWorkItem(delegate(object o)
				{
					this.RefreshServerMap(true);
				});
				throw new NoAvailableDownLevelBackEndException(text);
			}
			DownLevelServerStatusEntry downLevelServerStatusEntry = list.Find((DownLevelServerStatusEntry backend) => preferredCasServerFqdn.Equals(backend.BackEndServer.Fqdn, StringComparison.OrdinalIgnoreCase));
			if (downLevelServerStatusEntry == null)
			{
				string text2 = string.Format("Unable to find preferred server {0} in the back end server map.", preferredCasServerFqdn);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[DownLevelServerManager::GetDownLevelClientAccessServerWithPreferredServer]: {0}", text2);
				}
				throw new NoAvailableDownLevelBackEndException(text2);
			}
			if (downLevelServerStatusEntry.IsHealthy)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<DownLevelServerStatusEntry>((long)this.GetHashCode(), "[DownLevelServerManager::GetDownLevelClientAccessServerWithPreferredServer]: The preferred server {0} is healthy.", downLevelServerStatusEntry);
				}
				return downLevelServerStatusEntry.BackEndServer;
			}
			ServiceType serviceType = default(ServiceType);
			if (destinationVersion < Server.E14MinVersion)
			{
				try
				{
					serviceType = this.GetClientAccessServiceFromList<ServiceType>(list, currentLegacyServiceTopology, anchorMailbox, site, clientAccessType, (ServiceType service) => service.ServerVersionNumber >= Server.E2007MinVersion && service.ServerVersionNumber < Server.E14MinVersion, logger, DownLevelServerManager.DownlevelExchangeServerVersion.Exchange2007);
				}
				catch (NoAvailableDownLevelBackEndException)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError((long)this.GetHashCode(), "[DownLevelServerManager::GetDownLevelClientAccessServerWithPreferredServer]: No E12 CAS could be found for E12 destination. Looking for E14 CAS.");
					}
				}
			}
			if (serviceType == null)
			{
				serviceType = this.GetClientAccessServiceFromList<ServiceType>(list, currentLegacyServiceTopology, anchorMailbox, site, clientAccessType, (ServiceType service) => service.ServerVersionNumber >= Server.E14MinVersion && service.ServerVersionNumber < Server.E15MinVersion, logger, DownLevelServerManager.DownlevelExchangeServerVersion.Exchange2010);
			}
			return new BackEndServer(serviceType.ServerFullyQualifiedDomainName, serviceType.ServerVersionNumber);
		}

		// Token: 0x060001EA RID: 490 RVA: 0x00009428 File Offset: 0x00007628
		public BackEndServer GetDownLevelClientAccessServer<ServiceType>(AnchorMailbox anchorMailbox, BackEndServer mailboxServer, ClientAccessType clientAccessType, RequestDetailsLogger logger, bool calculateRedirectUrl, out Uri redirectUrl) where ServiceType : HttpService
		{
			if (anchorMailbox == null)
			{
				throw new ArgumentNullException("anchorMailbox");
			}
			if (mailboxServer == null)
			{
				throw new ArgumentNullException("mailboxServer");
			}
			if (logger == null)
			{
				throw new ArgumentNullException("logger");
			}
			if (!DownLevelServerManager.IsApplicable)
			{
				throw new HttpProxyException(HttpStatusCode.NotFound, 3001, string.Format("{0} does not support down level server proxy.", HttpProxyGlobals.ProtocolType));
			}
			redirectUrl = null;
			if (mailboxServer.Version < Server.E14MinVersion)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, int, string>((long)this.GetHashCode(), "[DownLevelServerManager::GetDownLevelClientAccessServer]: Found mailbox server version {0}, which was pre-E14 minimum version {1}, so returning mailbox server FQDN {2}", mailboxServer.Version, Server.E14MinVersion, mailboxServer.Fqdn);
				}
				return mailboxServer;
			}
			ServiceTopology currentLegacyServiceTopology = ServiceTopology.GetCurrentLegacyServiceTopology("d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerManager.cs", "GetDownLevelClientAccessServer", 415);
			Site site = currentLegacyServiceTopology.GetSite(mailboxServer.Fqdn, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerManager.cs", "GetDownLevelClientAccessServer", 416);
			ServiceType result = this.GetClientAccessServiceInSite<ServiceType>(currentLegacyServiceTopology, anchorMailbox, site, clientAccessType, (ServiceType service) => service.ServerVersionNumber >= Server.E14MinVersion && service.ServerVersionNumber < Server.E15MinVersion, logger);
			if (calculateRedirectUrl && !Utilities.IsPartnerHostedOnly && !CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).NoCrossSiteRedirect.Enabled && result != null && !string.IsNullOrEmpty(result.ServerFullyQualifiedDomainName) && !HttpProxyGlobals.LocalSite.Member.DistinguishedName.Equals(result.Site.DistinguishedName))
			{
				HttpService httpService = currentLegacyServiceTopology.FindAny<ServiceType>(1, (ServiceType externalService) => externalService != null && externalService.ServerFullyQualifiedDomainName.Equals(result.ServerFullyQualifiedDomainName, StringComparison.OrdinalIgnoreCase), "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerManager.cs", "GetDownLevelClientAccessServer", 441);
				if (httpService != null)
				{
					redirectUrl = httpService.Url;
				}
			}
			return new BackEndServer(result.ServerFullyQualifiedDomainName, result.ServerVersionNumber);
		}

		// Token: 0x060001EB RID: 491 RVA: 0x00009610 File Offset: 0x00007810
		public BackEndServer GetRandomDownLevelClientAccessServer()
		{
			Dictionary<string, List<DownLevelServerStatusEntry>> downLevelServerMap = this.GetDownLevelServerMap();
			string distinguishedName = HttpProxyGlobals.LocalSite.Member.DistinguishedName;
			List<DownLevelServerStatusEntry> serverList = null;
			if (downLevelServerMap.TryGetValue(distinguishedName, out serverList))
			{
				serverList = downLevelServerMap[distinguishedName];
				BackEndServer backEndServer = this.PickRandomServerInSite(serverList);
				if (backEndServer != null)
				{
					return backEndServer;
				}
			}
			for (int i = 0; i < downLevelServerMap.Count; i++)
			{
				if (!(downLevelServerMap.ElementAt(i).Key == distinguishedName))
				{
					serverList = downLevelServerMap.ElementAt(i).Value;
					BackEndServer backEndServer = this.PickRandomServerInSite(serverList);
					if (backEndServer != null)
					{
						return backEndServer;
					}
				}
			}
			string text = string.Format("Unable to find a healthy downlevel server in any site.", Array.Empty<object>());
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
			{
				ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[DownLevelServerManager::GetRandomDownlevelClientAccessServer]: {0}", text);
			}
			throw new NoAvailableDownLevelBackEndException(text);
		}

		// Token: 0x060001EC RID: 492 RVA: 0x000096E4 File Offset: 0x000078E4
		public void Close()
		{
			object obj = this.instanceLock;
			lock (obj)
			{
				if (this.pingManager != null)
				{
					this.pingManager.Dispose();
					this.pingManager = null;
				}
				if (this.serverMapUpdateTimer != null)
				{
					this.serverMapUpdateTimer.Dispose();
					this.serverMapUpdateTimer = null;
				}
			}
		}

		// Token: 0x060001ED RID: 493 RVA: 0x00009754 File Offset: 0x00007954
		internal static int[] GetShuffledList(int length, int randomNumberSeed)
		{
			Random random = new Random(randomNumberSeed);
			int[] array = new int[length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = i;
			}
			array.Shuffle(random);
			return array;
		}

		// Token: 0x060001EE RID: 494 RVA: 0x0000978C File Offset: 0x0000798C
		internal List<DownLevelServerStatusEntry> GetFilteredServerListByVersion(List<DownLevelServerStatusEntry> serverList, DownLevelServerManager.DownlevelExchangeServerVersion serverVersion)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[DownLevelServerManager::GetFilteredServerListByVersion]: Filtering ServerList by Version: {0}", serverVersion.ToString());
			}
			if (serverVersion == DownLevelServerManager.DownlevelExchangeServerVersion.Exchange2007)
			{
				return serverList.FindAll((DownLevelServerStatusEntry server) => server.BackEndServer.Version >= Server.E2007MinVersion && server.BackEndServer.Version < Server.E14MinVersion);
			}
			if (serverVersion != DownLevelServerManager.DownlevelExchangeServerVersion.Exchange2010)
			{
				return serverList;
			}
			return serverList.FindAll((DownLevelServerStatusEntry server) => server.BackEndServer.Version >= Server.E14MinVersion && server.BackEndServer.Version < Server.E15MinVersion);
		}

		// Token: 0x060001EF RID: 495 RVA: 0x0000981F File Offset: 0x00007A1F
		private Dictionary<string, List<DownLevelServerStatusEntry>> GetDownLevelServerMap()
		{
			return this.downLevelServers;
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x00009828 File Offset: 0x00007A28
		private BackEndServer PickRandomServerInSite(List<DownLevelServerStatusEntry> serverList)
		{
			int num = new Random().Next(serverList.Count);
			int num2 = num;
			DownLevelServerStatusEntry downLevelServerStatusEntry;
			for (;;)
			{
				downLevelServerStatusEntry = serverList[num2];
				if (downLevelServerStatusEntry.IsHealthy)
				{
					break;
				}
				num2++;
				if (num2 >= serverList.Count)
				{
					num2 = 0;
				}
				if (num2 == num)
				{
					goto Block_3;
				}
			}
			return downLevelServerStatusEntry.BackEndServer;
			Block_3:
			return null;
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x00009874 File Offset: 0x00007A74
		private void RefreshServerMap(bool isTimer)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[DownLevelServerManager::RefreshServerMap]: Refreshing server map.");
			}
			Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_RefreshingDownLevelServerMap, null, new object[]
			{
				HttpProxyGlobals.ProtocolType.ToString()
			});
			try
			{
				this.InternalRefresh();
			}
			catch (Exception ex)
			{
				if (!isTimer)
				{
					throw;
				}
				Diagnostics.ReportException(ex, FrontEndHttpProxyEventLogConstants.Tuple_InternalServerError, null, "Exception from RefreshServerMap: {0}");
			}
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x00009904 File Offset: 0x00007B04
		private ServiceType GetClientAccessServiceInSite<ServiceType>(ServiceTopology topology, AnchorMailbox anchorMailbox, Site targetSite, ClientAccessType clientAccessType, Predicate<ServiceType> otherFilter, RequestDetailsLogger logger) where ServiceType : HttpService
		{
			Dictionary<string, List<DownLevelServerStatusEntry>> downLevelServerMap = this.GetDownLevelServerMap();
			List<DownLevelServerStatusEntry> serverList = null;
			if (!downLevelServerMap.TryGetValue(targetSite.DistinguishedName, out serverList))
			{
				string text = string.Format("Unable to find site {0} in the down level server map.", targetSite.DistinguishedName);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[DownLevelServerManager::GetClientAccessServiceInSite]: {0}", text);
				}
				ThreadPool.QueueUserWorkItem(delegate(object o)
				{
					this.RefreshServerMap(true);
				});
				throw new NoAvailableDownLevelBackEndException(text);
			}
			return this.GetClientAccessServiceFromList<ServiceType>(serverList, topology, anchorMailbox, targetSite, clientAccessType, otherFilter, logger, DownLevelServerManager.DownlevelExchangeServerVersion.Exchange2010);
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x00009988 File Offset: 0x00007B88
		private ServiceType GetClientAccessServiceFromList<ServiceType>(List<DownLevelServerStatusEntry> serverList, ServiceTopology topology, AnchorMailbox anchorMailbox, Site targetSite, ClientAccessType clientAccessType, Predicate<ServiceType> otherFilter, RequestDetailsLogger logger, DownLevelServerManager.DownlevelExchangeServerVersion targetDownlevelExchangeServerVersion) where ServiceType : HttpService
		{
			string text = anchorMailbox.ToCookieKey();
			int hashCode = HttpProxyBackEndHelper.GetHashCode(text);
			serverList = this.GetFilteredServerListByVersion(serverList, targetDownlevelExchangeServerVersion);
			int[] shuffledList = DownLevelServerManager.GetShuffledList(serverList.Count, hashCode);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string, int, string>((long)this.GetHashCode(), "[DownLevelServerManager::GetClientAccessServiceFromList]: HashKey: {0}, HashCode: {1}, Anchor mailbox {2}.", text, hashCode, anchorMailbox.ToString());
			}
			for (int i = 0; i < shuffledList.Length; i++)
			{
				int num = shuffledList[i];
				DownLevelServerStatusEntry currentServer = serverList[num];
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string, int, bool>((long)this.GetHashCode(), "[DownLevelServerManager::GetClientAccessServiceFromList]: Back end server {0} is selected by current index {1}. IsHealthy = {2}", currentServer.BackEndServer.Fqdn, num, currentServer.IsHealthy);
				}
				if (currentServer.IsHealthy)
				{
					ServiceType serviceType = topology.FindAny<ServiceType>(clientAccessType, (ServiceType service) => service != null && service.ServerFullyQualifiedDomainName.Equals(currentServer.BackEndServer.Fqdn, StringComparison.OrdinalIgnoreCase) && !service.IsOutOfService && otherFilter(service), "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerManager.cs", "GetClientAccessServiceFromList", 799);
					if (serviceType != null)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<Uri, string>((long)this.GetHashCode(), "[DownLevelServerManager::GetClientAccessServiceFromList]: Found service {0} matching back end server {1}.", serviceType.Url, currentServer.BackEndServer.Fqdn);
						}
						RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(logger, "DownLevelTargetRandomHashing", string.Format("{0}/{1}", i, serverList.Count));
						return serviceType;
					}
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[DownLevelServerManager::GetClientAccessServiceFromList]: Back end server {0} cannot be found by ServiceDiscovery.", currentServer.BackEndServer.Fqdn);
					}
				}
				else if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(2))
				{
					ExTraceGlobals.VerboseTracer.TraceWarning<string>((long)this.GetHashCode(), "[DownLevelServerManager::GetClientAccessServiceFromList]: Back end server {0} is marked as unhealthy.", currentServer.BackEndServer.Fqdn);
				}
			}
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(logger, "DownLevelTargetRandomHashingFailure", string.Format("{0}", serverList.Count));
			this.TriggerServerMapRefreshIfNeeded(topology, serverList);
			string text2 = string.Format("Unable to find proper back end service for {0} in site {1}.", anchorMailbox, targetSite.DistinguishedName);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
			{
				ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[DownLevelServerManager::GetClientAccessServiceFromList]: {0}", text2);
			}
			throw new NoAvailableDownLevelBackEndException(text2);
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x00009BF0 File Offset: 0x00007DF0
		private void TriggerServerMapRefreshIfNeeded(ServiceTopology topology, List<DownLevelServerStatusEntry> serverList)
		{
			bool flag = false;
			if (serverList.Count == 0)
			{
				flag = true;
			}
			using (List<DownLevelServerStatusEntry>.Enumerator enumerator = serverList.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (!DownLevelServerManager.IsServerDiscoverable(enumerator.Current.BackEndServer.Fqdn))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				ThreadPool.QueueUserWorkItem(delegate(object o)
				{
					this.RefreshServerMap(true);
				});
			}
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x00009C6C File Offset: 0x00007E6C
		private void InternalRefresh()
		{
			Exception ex = null;
			Server[] array = null;
			try
			{
				array = DirectoryHelper.GetConfigurationSession().FindPaged<Server>(null, 2, DownLevelServerManager.ServerVersionFilter, null, 0, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\DownLevelServerManager\\DownLevelServerManager.cs", 910, "InternalRefresh").ReadAllPages();
			}
			catch (ADTransientException ex)
			{
			}
			catch (DataValidationException ex)
			{
			}
			catch (DataSourceOperationException ex)
			{
			}
			if (ex != null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<Exception>((long)this.GetHashCode(), "[DownLevelServerManager::RefreshServerMap]: Active Directory exception: {0}", ex);
				}
				Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_ErrorRefreshDownLevelServerMap, null, new object[]
				{
					HttpProxyGlobals.ProtocolType.ToString(),
					ex.ToString()
				});
				return;
			}
			Dictionary<string, List<DownLevelServerStatusEntry>> downLevelServerMap = this.GetDownLevelServerMap();
			Dictionary<string, List<DownLevelServerStatusEntry>> dictionary = new Dictionary<string, List<DownLevelServerStatusEntry>>(downLevelServerMap.Count, StringComparer.OrdinalIgnoreCase);
			Server[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				Server server = array2[i];
				if ((server.CurrentServerRole & 4) > 0 && server.ServerSite != null)
				{
					List<DownLevelServerStatusEntry> list = null;
					if (!dictionary.TryGetValue(server.ServerSite.DistinguishedName, out list))
					{
						list = new List<DownLevelServerStatusEntry>();
						dictionary.Add(server.ServerSite.DistinguishedName, list);
					}
					DownLevelServerStatusEntry downLevelServerStatusEntry = null;
					List<DownLevelServerStatusEntry> list2 = null;
					if (downLevelServerMap.TryGetValue(server.ServerSite.DistinguishedName, out list2))
					{
						downLevelServerStatusEntry = list2.Find((DownLevelServerStatusEntry x) => x.BackEndServer.Fqdn.Equals(server.Fqdn, StringComparison.OrdinalIgnoreCase));
					}
					if (downLevelServerStatusEntry == null)
					{
						downLevelServerStatusEntry = new DownLevelServerStatusEntry
						{
							BackEndServer = new BackEndServer(server.Fqdn, server.VersionNumber),
							IsHealthy = true
						};
					}
					list.Add(downLevelServerStatusEntry);
					list.Sort((DownLevelServerStatusEntry x, DownLevelServerStatusEntry y) => x.BackEndServer.Fqdn.CompareTo(y.BackEndServer.Fqdn));
				}
			}
			this.downLevelServers = dictionary;
			if (dictionary.Count > 0 && DownLevelServerManager.DownLevelServerPingEnabled.Value && this.pingManager == null)
			{
				this.pingManager = new DownLevelServerPingManager(new Func<Dictionary<string, List<DownLevelServerStatusEntry>>>(this.GetDownLevelServerMap));
			}
		}

		// Token: 0x04000112 RID: 274
		private static readonly QueryFilter ServerVersionFilter = new ComparisonFilter(2, ServerSchema.VersionNumber, Server.E15MinVersion);

		// Token: 0x04000113 RID: 275
		private static readonly TimeSpanAppSettingsEntry DownLevelServerMapRefreshInterval = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("DownLevelServerMapRefreshInterval"), 1, TimeSpan.FromMinutes(360.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x04000114 RID: 276
		private static readonly FlightableBoolAppSettingsEntry DownLevelServerPingEnabled = new FlightableBoolAppSettingsEntry(HttpProxySettings.Prefix("DownLevelServerPingEnabled"), () => CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).DownLevelServerPing.Enabled);

		// Token: 0x04000115 RID: 277
		private static DownLevelServerManager instance = null;

		// Token: 0x04000116 RID: 278
		private static object staticLock = new object();

		// Token: 0x04000117 RID: 279
		private object instanceLock = new object();

		// Token: 0x04000118 RID: 280
		private DownLevelServerPingManager pingManager;

		// Token: 0x04000119 RID: 281
		private Dictionary<string, List<DownLevelServerStatusEntry>> downLevelServers = new Dictionary<string, List<DownLevelServerStatusEntry>>(StringComparer.OrdinalIgnoreCase);

		// Token: 0x0400011A RID: 282
		private Timer serverMapUpdateTimer;

		// Token: 0x020000E9 RID: 233
		internal enum DownlevelExchangeServerVersion
		{
			// Token: 0x04000486 RID: 1158
			Exchange2007,
			// Token: 0x04000487 RID: 1159
			Exchange2010
		}
	}
}
