using System;
using System.Data;
using System.Linq;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000AB RID: 171
	internal class LogExportProxyHandler : ProxyRequestHandler
	{
		// Token: 0x060005DD RID: 1501 RVA: 0x00020B18 File Offset: 0x0001ED18
		internal static bool TryGetPartitionId(string[] requestSegments, out string partitionId)
		{
			partitionId = string.Empty;
			if (!requestSegments.Last<string>().Equals("partitions", StringComparison.OrdinalIgnoreCase) && !requestSegments.Last<string>().Equals("partitions/", StringComparison.OrdinalIgnoreCase))
			{
				if (requestSegments.Any((string segment) => segment.Equals("partitions/", StringComparison.OrdinalIgnoreCase)))
				{
					for (int i = 0; i < requestSegments.Length; i++)
					{
						if (requestSegments[i].Equals("partitions/", StringComparison.OrdinalIgnoreCase))
						{
							partitionId = requestSegments[i + 1].TrimEnd(new char[]
							{
								'/'
							});
							break;
						}
					}
					return true;
				}
			}
			return false;
		}

		// Token: 0x060005DE RID: 1502 RVA: 0x00020BB4 File Offset: 0x0001EDB4
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			string text;
			if (!LogExportProxyHandler.TryGetPartitionId((from uriSegment in new Uri(new Uri("http://dummyHost", UriKind.Absolute), base.ClientRequest.Path).Segments
			where !uriSegment.Equals("/")
			select uriSegment).ToArray<string>(), out text))
			{
				return new AnonymousAnchorMailbox(this);
			}
			MiniServer miniServer;
			if (!this.TryGetMiniServerFromPartitionId(text, out miniServer))
			{
				throw new HttpException(404, string.Format("Unable to resolve server: {0}", text));
			}
			if (miniServer.CurrentServerRole.HasFlag(2))
			{
				return new ServerInfoAnchorMailbox(new BackEndServer(miniServer.Fqdn, Server.E15MinVersion), this);
			}
			MiniServer deterministicBackEndServerFromSameSite;
			try
			{
				deterministicBackEndServerFromSameSite = ServersCache.GetDeterministicBackEndServerFromSameSite(miniServer.Fqdn, 0, new Random().Next().ToString(), false);
			}
			catch (LocalServerNotFoundException ex)
			{
				base.Logger.AppendGenericError("GetDeterministicBackEndServerFromSameSite Exception", ex.ToString());
				throw new HttpException(404, string.Format("Unable to resolve identity: {0}", miniServer.Fqdn));
			}
			catch (ServerHasNotBeenFoundException ex2)
			{
				base.Logger.AppendGenericError("GetDeterministicBackEndServerFromSameSite Exception", ex2.ToString());
				throw new HttpException(404, string.Format("No server available in site to service request: {0}", miniServer.Fqdn));
			}
			if (deterministicBackEndServerFromSameSite == null)
			{
				throw new HttpException(404, string.Format("No server available in site to service request: {0}", miniServer.Fqdn));
			}
			return new ServerInfoAnchorMailbox(new BackEndServer(deterministicBackEndServerFromSameSite.Fqdn, Server.E15MinVersion), this);
		}

		// Token: 0x060005DF RID: 1503 RVA: 0x00020D48 File Offset: 0x0001EF48
		private bool TryGetMiniServerFromPartitionId(string partitionId, out MiniServer server)
		{
			bool result;
			try
			{
				if (CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).UseServerNameAsPartitionId.Enabled)
				{
					bool flag;
					server = ServersCache.GetServerOrDCByName(partitionId, ref flag);
				}
				else
				{
					Guid guid;
					if (!Guid.TryParse(partitionId, out guid))
					{
						base.Logger.AppendGenericError("Get server by partitionID exception", "Not a GUID");
						server = null;
						return false;
					}
					bool flag;
					server = ServersCache.GetServerOrDCByObjectGuid(guid, ref flag);
				}
				result = true;
			}
			catch (ObjectNotFoundException ex)
			{
				base.Logger.AppendGenericError("Get server by partitionID exception", ex.ToString());
				server = null;
				result = false;
			}
			return result;
		}
	}
}
