﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Configuration;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000BF RID: 191
	internal class CoexistenceRpcHttpProxyRules : IRpcHttpProxyRules
	{
		// Token: 0x06000750 RID: 1872 RVA: 0x0002AB2A File Offset: 0x00028D2A
		internal CoexistenceRpcHttpProxyRules() : this(null)
		{
		}

		// Token: 0x06000751 RID: 1873 RVA: 0x0002AB33 File Offset: 0x00028D33
		internal CoexistenceRpcHttpProxyRules(IDirectory rule)
		{
			this.directory = (rule ?? new Directory());
			this.RefreshServerList(null);
		}

		// Token: 0x06000752 RID: 1874 RVA: 0x0002AB54 File Offset: 0x00028D54
		public bool TryGetProxyDestination(string rpcServerFqdn, out ProxyDestination destination)
		{
			destination = null;
			Dictionary<string, ProxyDestination> dictionary = this.proxyDestinations;
			if (dictionary != null)
			{
				dictionary.TryGetValue(rpcServerFqdn, out destination);
			}
			return destination != null;
		}

		// Token: 0x06000753 RID: 1875 RVA: 0x0002AB7C File Offset: 0x00028D7C
		public string DiagnosticInfo()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, ProxyDestination> keyValuePair in this.proxyDestinations)
			{
				stringBuilder.AppendFormat("{0} : {1}\n", keyValuePair.Key, keyValuePair.Value);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000754 RID: 1876 RVA: 0x0002ABF0 File Offset: 0x00028DF0
		public void Shutdown()
		{
			this.shutdown = true;
		}

		// Token: 0x06000755 RID: 1877 RVA: 0x0002ABFC File Offset: 0x00028DFC
		private static void ApplyManualOverrides(Dictionary<string, ProxyDestination> proxyDestinations, string manualOverrides)
		{
			Regex regex = new Regex("^\\+(.+)=(.+):(\\d+)");
			Regex regex2 = new Regex("^\\-(.+)");
			foreach (string input in manualOverrides.Split(new char[]
			{
				';'
			}))
			{
				Match match = regex.Match(input);
				if (match.Success)
				{
					string value = match.Groups[1].Value;
					string value2 = match.Groups[2].Value;
					int port;
					if (int.TryParse(match.Groups[3].Value, out port))
					{
						proxyDestinations.Add(value, CoexistenceRpcHttpProxyRules.CreateFixedDestination(Server.E15MinVersion, value2, port));
					}
				}
				Match match2 = regex2.Match(input);
				if (match2.Success)
				{
					string value3 = match2.Groups[1].Value;
					proxyDestinations.Remove(value3);
				}
			}
		}

		// Token: 0x06000756 RID: 1878 RVA: 0x0002ACE6 File Offset: 0x00028EE6
		private static void AddTwoMapsOfDestinations(Dictionary<string, ProxyDestination> dict, Server server, ProxyDestination destination)
		{
			dict[server.Fqdn] = destination;
			dict[server.Name] = destination;
		}

		// Token: 0x06000757 RID: 1879 RVA: 0x0002AD02 File Offset: 0x00028F02
		private static ProxyDestination CreateFixedDestination(int version, string serverFqdn, int port)
		{
			return new ProxyDestination(version, port, serverFqdn);
		}

		// Token: 0x06000758 RID: 1880 RVA: 0x0002AD0C File Offset: 0x00028F0C
		private void RefreshServerList(object stateInfo)
		{
			if (this.shutdown)
			{
				return;
			}
			ADSite[] adsites = this.directory.GetADSites();
			ClientAccessArray[] clientAccessArrays = this.directory.GetClientAccessArrays();
			Server[] servers = this.directory.GetServers();
			if (adsites != null && servers != null)
			{
				Dictionary<string, ProxyDestination> dictionary = new Dictionary<string, ProxyDestination>(StringComparer.OrdinalIgnoreCase);
				foreach (Server server5 in from s in servers
				where s.IsE15OrLater && s.IsMailboxServer
				select s)
				{
					CoexistenceRpcHttpProxyRules.AddTwoMapsOfDestinations(dictionary, server5, CoexistenceRpcHttpProxyRules.CreateFixedDestination(Server.E15MinVersion, server5.Fqdn, 444));
				}
				ADSite[] array = adsites;
				for (int i = 0; i < array.Length; i++)
				{
					ADSite site = array[i];
					IEnumerable<Server> source = from s in servers
					where s.ServerSite != null && s.ServerSite.Name == site.Name
					select s;
					IEnumerable<Server> enumerable = from s in source
					where s.IsE14OrLater && !s.IsE15OrLater && s.IsClientAccessServer
					select s;
					IEnumerable<Server> source2 = from s in enumerable
					where !(bool)s[ActiveDirectoryServerSchema.IsOutOfService]
					select s;
					ProxyDestination proxyDestination = null;
					if (source2.Count<Server>() > 0)
					{
						proxyDestination = new ProxyDestination(Server.E14MinVersion, 443, (from server in enumerable
						select server.Fqdn).OrderBy((string str) => str, StringComparer.OrdinalIgnoreCase).ToArray<string>(), (from server in source2
						select server.Fqdn).OrderBy((string str) => str, StringComparer.OrdinalIgnoreCase).ToArray<string>());
					}
					foreach (Server server2 in enumerable)
					{
						CoexistenceRpcHttpProxyRules.AddTwoMapsOfDestinations(dictionary, server2, CoexistenceRpcHttpProxyRules.CreateFixedDestination(Server.E14MinVersion, server2.Fqdn, 443));
					}
					if (proxyDestination != null)
					{
						foreach (Server server3 in from s in source
						where s.IsE14OrLater && !s.IsE15OrLater && !s.IsClientAccessServer && s.IsMailboxServer
						select s)
						{
							CoexistenceRpcHttpProxyRules.AddTwoMapsOfDestinations(dictionary, server3, proxyDestination);
						}
						if (clientAccessArrays != null && clientAccessArrays.Count<ClientAccessArray>() > 0)
						{
							IEnumerable<ClientAccessArray> source3 = clientAccessArrays;
							Func<ClientAccessArray, bool> predicate;
							Func<ClientAccessArray, bool> <>9__9;
							if ((predicate = <>9__9) == null)
							{
								predicate = (<>9__9 = ((ClientAccessArray arr) => arr.SiteName == site.Name));
							}
							foreach (ClientAccessArray clientAccessArray in source3.Where(predicate))
							{
								dictionary[clientAccessArray.Fqdn] = proxyDestination;
							}
						}
					}
					IEnumerable<Server> source4 = from s in source
					where !s.IsE14OrLater && s.IsExchange2007OrLater && s.IsClientAccessServer
					select s;
					ProxyDestination proxyDestination2 = null;
					if (source4.Count<Server>() > 0)
					{
						string[] array2 = (from server in source4
						select server.Fqdn).OrderBy((string str) => str, StringComparer.OrdinalIgnoreCase).ToArray<string>();
						proxyDestination2 = new ProxyDestination(Server.E2007MinVersion, 443, array2, array2);
					}
					else if (proxyDestination != null)
					{
						proxyDestination2 = proxyDestination;
					}
					if (proxyDestination2 != null)
					{
						foreach (Server server4 in from s in source
						where s.IsExchange2007OrLater && !s.IsE14OrLater && s.IsMailboxServer
						select s)
						{
							CoexistenceRpcHttpProxyRules.AddTwoMapsOfDestinations(dictionary, server4, proxyDestination2);
						}
					}
				}
				string text = WebConfigurationManager.AppSettings["OverrideProxyingRules"];
				if (!string.IsNullOrEmpty(text))
				{
					CoexistenceRpcHttpProxyRules.ApplyManualOverrides(dictionary, text);
				}
				this.proxyDestinations = dictionary;
			}
			if (this.refreshTimer != null)
			{
				this.refreshTimer.Change((int)CoexistenceRpcHttpProxyRules.TopologyRefreshInterval.TotalMilliseconds, -1);
				return;
			}
			this.refreshTimer = new Timer(new TimerCallback(this.RefreshServerList), null, (int)CoexistenceRpcHttpProxyRules.TopologyRefreshInterval.TotalMilliseconds, -1);
		}

		// Token: 0x040003FC RID: 1020
		private const int BrickBackEndPort = 444;

		// Token: 0x040003FD RID: 1021
		private const int OriginalRpcVDirPort = 443;

		// Token: 0x040003FE RID: 1022
		private const string AppSettingsOverrideProxyingRules = "OverrideProxyingRules";

		// Token: 0x040003FF RID: 1023
		private static readonly TimeSpan TopologyRefreshInterval = TimeSpan.FromMinutes(15.0);

		// Token: 0x04000400 RID: 1024
		private IDirectory directory;

		// Token: 0x04000401 RID: 1025
		private Dictionary<string, ProxyDestination> proxyDestinations;

		// Token: 0x04000402 RID: 1026
		private Timer refreshTimer;

		// Token: 0x04000403 RID: 1027
		private bool shutdown;
	}
}
