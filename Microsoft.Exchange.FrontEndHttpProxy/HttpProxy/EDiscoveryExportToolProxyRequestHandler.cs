using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000092 RID: 146
	internal class EDiscoveryExportToolProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x06000518 RID: 1304 RVA: 0x0001C75B File Offset: 0x0001A95B
		internal static bool IsEDiscoveryExportToolProxyRequest(HttpRequest request)
		{
			return EDiscoveryExportToolRequestPathHandler.IsEDiscoveryExportToolRequest(request);
		}

		// Token: 0x06000519 RID: 1305 RVA: 0x0001C764 File Offset: 0x0001A964
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string, Uri>((long)this.GetHashCode(), "[EDiscoveryExportToolProxyRequestHandler::ResolveAnchorMailbox]: Method {0}; Url {1};", base.ClientRequest.HttpMethod, base.ClientRequest.Url);
			}
			string[] array = base.ClientRequest.Url.AbsolutePath.Split(new char[]
			{
				'/'
			}, StringSplitOptions.RemoveEmptyEntries);
			if (((array.Length == 5 && array[4].StartsWith("microsoft.exchange.")) || (array.Length == 6 && array[5].StartsWith("microsoft.exchange."))) && array[2] == "exporttool" && array[3].Contains("."))
			{
				this.serverFqdn = array[3];
			}
			else
			{
				this.serverFqdn = null;
			}
			Match pathMatch = EDiscoveryExportToolRequestPathHandler.GetPathMatch(base.ClientRequest);
			bool exactVersionMatch = false;
			ServerVersion serverVersion;
			if (pathMatch.Success)
			{
				if (RegexUtilities.TryGetServerVersionFromRegexMatch(pathMatch, ref serverVersion))
				{
					exactVersionMatch = true;
				}
			}
			else
			{
				serverVersion = new ServerVersion(Server.CurrentExchangeMajorVersion, 0, 0, 0);
				exactVersionMatch = false;
			}
			if (((!string.IsNullOrEmpty(this.serverFqdn)) ? CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).DiscoveryExportToolDownloadRoutingMechanism.Value : string.Empty) == "ServerInfo")
			{
				base.Logger.Set(3, "EDiscoveryExportTool-ServerInfo");
				return new ServerInfoAnchorMailbox(this.serverFqdn, this);
			}
			if (EDiscoveryExportToolRequestPathHandler.IsEDiscoveryExportToolRequest(base.ClientRequest))
			{
				AnchorMailbox result = new ServerVersionAnchorMailbox<EcpService>(serverVersion, 0, exactVersionMatch, this);
				base.Logger.Set(3, "EDiscoveryExportTool-ServerVersion");
				return result;
			}
			throw new HttpProxyException(HttpStatusCode.NotFound, 3007, string.Format("Unable to find target server for url: {0}", base.ClientRequest.Url));
		}

		// Token: 0x0600051A RID: 1306 RVA: 0x0001C908 File Offset: 0x0001AB08
		protected override UriBuilder GetClientUrlForProxy()
		{
			UriBuilder uriBuilder = new UriBuilder(base.ClientRequest.Url);
			if (!string.IsNullOrEmpty(this.serverFqdn))
			{
				uriBuilder.Path = base.ClientRequest.Url.AbsolutePath.Replace("/" + this.serverFqdn + "/", "/");
			}
			return uriBuilder;
		}

		// Token: 0x04000359 RID: 857
		private string serverFqdn;
	}
}
