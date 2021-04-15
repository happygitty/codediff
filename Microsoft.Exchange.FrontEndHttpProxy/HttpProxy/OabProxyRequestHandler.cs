using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.OAB;
using Microsoft.Exchange.Security.OAuth;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.OAB;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200009F RID: 159
	internal sealed class OabProxyRequestHandler : BEServerCookieProxyRequestHandler<OabService>
	{
		// Token: 0x17000131 RID: 305
		// (get) Token: 0x0600057F RID: 1407 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x06000580 RID: 1408 RVA: 0x0001EA8C File Offset: 0x0001CC8C
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			AnchorMailbox anchorMailbox = null;
			AnchorMailbox anchorMailbox2 = base.ResolveAnchorMailbox();
			UserBasedAnchorMailbox userBasedAnchorMailbox = anchorMailbox2 as UserBasedAnchorMailbox;
			if (userBasedAnchorMailbox == null)
			{
				return anchorMailbox2;
			}
			ADRawEntry adrawEntry = userBasedAnchorMailbox.GetADRawEntry();
			if (OABConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).SingleUserOab.Enabled && (Globals.IsOabDownloadRequestFromConsumer(base.HttpContext.Request.Url.OriginalString) || (adrawEntry != null && adrawEntry[ADObjectSchema.OrganizationId] != null && Globals.IsConsumerOrganization((OrganizationId)adrawEntry[ADObjectSchema.OrganizationId]))))
			{
				return userBasedAnchorMailbox;
			}
			userBasedAnchorMailbox.UseServerCookie = true;
			string targetOrgMailbox = base.HttpContext.Request.Headers["TargetOrgMailbox"];
			Guid guid = Guid.Empty;
			if (!string.IsNullOrEmpty(targetOrgMailbox))
			{
				IRecipientSession session = DirectoryHelper.GetRecipientSessionFromSmtpOrLiveId(targetOrgMailbox, base.Logger, base.LatencyTracker, false);
				ADRawEntry adrawEntry2 = DirectoryHelper.InvokeAccountForest<ADUser>(base.LatencyTracker, () => OrganizationMailbox.GetOrganizationMailboxByUPNAndCapability(session, targetOrgMailbox, 42), base.Logger, session);
				if (adrawEntry2 != null)
				{
					anchorMailbox = new UserADRawEntryAnchorMailbox(adrawEntry2, this);
				}
			}
			else
			{
				AnchoredRoutingTarget anchoredRoutingTarget = this.TryFastTargetCalculationByAnchorMailbox(anchorMailbox2);
				if (anchoredRoutingTarget != null)
				{
					return anchoredRoutingTarget.AnchorMailbox;
				}
				if (adrawEntry == null)
				{
					return anchorMailbox2;
				}
				guid = OABRequestUrl.GetOabGuidFromRequest(base.HttpContext.Request);
				if (guid == Guid.Empty)
				{
					return anchorMailbox2;
				}
				OrganizationId organizationId = (OrganizationId)adrawEntry[ADObjectSchema.OrganizationId];
				string userAcceptedDomain = null;
				if (organizationId != OrganizationId.ForestWideOrgId)
				{
					userAcceptedDomain = ((SmtpAddress)adrawEntry[ADRecipientSchema.PrimarySmtpAddress]).Domain;
				}
				OABCache.OABCacheEntry oabcacheEntry = null;
				try
				{
					oabcacheEntry = OABCache.Instance.GetOABFromCacheOrAD(guid, userAcceptedDomain);
				}
				catch (ADNoSuchObjectException ex)
				{
					throw new HttpProxyException(HttpStatusCode.NotFound, 3001, "ADNoSuchObjectException: OAB is not in the cache and cannot be found from the AD! " + ex.Message);
				}
				if (oabcacheEntry.ExchangeVersion.IsOlderThan(ExchangeObjectVersion.Exchange2012))
				{
					anchorMailbox = this.GetE14CASServer(oabcacheEntry);
				}
				else
				{
					ADRawEntry adrawEntry3 = null;
					if (OABVariantConfigurationSettings.IsLinkedOABGenMailboxesEnabled && !oabcacheEntry.ShadowMailboxDistributionEnabled && oabcacheEntry.GeneratingMailbox != null)
					{
						adrawEntry3 = DirectoryHelper.GetRecipientSessionFromOrganizationId(base.LatencyTracker, organizationId, base.Logger).Read(oabcacheEntry.GeneratingMailbox, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\OabProxyRequestHandler.cs", 200, "ResolveAnchorMailbox");
					}
					if (adrawEntry3 == null)
					{
						if (OABVariantConfigurationSettings.IsSkipServiceTopologyDiscoveryEnabled)
						{
							adrawEntry3 = HttpProxyBackEndHelper.GetOrganizationMailboxWithOABGenCapability(organizationId);
						}
						else
						{
							adrawEntry3 = HttpProxyBackEndHelper.GetOrganizationMailboxInClosestSite(organizationId, 42);
						}
					}
					if (adrawEntry3 != null)
					{
						anchorMailbox = new UserADRawEntryAnchorMailbox(adrawEntry3, this)
						{
							UseServerCookie = true
						};
					}
				}
			}
			if (anchorMailbox == null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError(0L, "[OabProxyRequestHandler::ResolveAnchorMailbox] Unable to locate appropriate server for OAB");
				}
				string message;
				if (string.IsNullOrEmpty(targetOrgMailbox))
				{
					message = string.Format("Unable to locate appropriate server for OAB {0}.", guid);
				}
				else
				{
					message = string.Format("Unable to locate organization mailbox {0}", targetOrgMailbox);
				}
				throw new HttpProxyException(HttpStatusCode.InternalServerError, 3006, message);
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailbox>(0L, "[OabProxyRequestHandler::ResolveAnchorMailbox] Proxying OAB request using anchor {0}.", anchorMailbox);
			}
			string text = string.Format("{0}-{1}", base.Logger.Get(3), "OABOrgMailbox");
			base.Logger.Set(3, text);
			anchorMailbox.OriginalAnchorMailbox = anchorMailbox2;
			return anchorMailbox;
		}

		// Token: 0x06000581 RID: 1409 RVA: 0x0001EDF4 File Offset: 0x0001CFF4
		protected override BackEndServer GetDownLevelClientAccessServer(AnchorMailbox anchorMailbox, BackEndServer backEndServer)
		{
			return backEndServer;
		}

		// Token: 0x06000582 RID: 1410 RVA: 0x0001EDF8 File Offset: 0x0001CFF8
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			base.AddProtocolSpecificHeadersToServerRequest(headers);
			OAuthIdentity oauthIdentity = base.HttpContext.User.Identity as OAuthIdentity;
			if (oauthIdentity != null)
			{
				if (oauthIdentity.IsAppOnly)
				{
					throw new HttpException(403, "unsupported scenario");
				}
				if (oauthIdentity.OrganizationId.OrganizationalUnit != null)
				{
					headers["X-WLID-MemberName"] = "dummy@" + oauthIdentity.OrganizationId.OrganizationalUnit.Name;
				}
			}
		}

		// Token: 0x06000583 RID: 1411 RVA: 0x0001EE70 File Offset: 0x0001D070
		private AnchorMailbox GetE14CASServer(OABCache.OABCacheEntry oab)
		{
			ServiceTopology serviceTopology = ServiceTopology.GetCurrentLegacyServiceTopology("d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\OabProxyRequestHandler.cs", "GetE14CASServer", 319);
			Site currentSite = HttpProxyGlobals.LocalSite.Member;
			List<OabService> cheapestCASServers = new List<OabService>();
			int cheapestSiteConnectionCost = int.MaxValue;
			OabProxyRequestHandler.IsEligibleOabService isEligibleOabServiceDelegate = null;
			if (oab.GlobalWebDistributionEnabled)
			{
				isEligibleOabServiceDelegate = new OabProxyRequestHandler.IsEligibleOabService(this.IsEligibleOabServiceBasedOnVersion);
			}
			else
			{
				if (oab.VirtualDirectories == null || oab.VirtualDirectories.Count <= 0)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError(0L, "[OabProxyRequestHandler::ResolveAnchorMailbox] The OAB is distributed neither globally nor to named vdirs; there is no way to retrieve it");
					}
					throw new HttpProxyException(HttpStatusCode.InternalServerError, 3007, "The OAB is distributed neither globally nor to named vdirs; there is no way to retrieve it");
				}
				isEligibleOabServiceDelegate = new OabProxyRequestHandler.IsEligibleOabService(this.IsEligibleOabServiceBasedOnVersionAndVirtualDirectory);
			}
			serviceTopology.ForEach<OabService>(delegate(OabService oabService)
			{
				if (isEligibleOabServiceDelegate(oabService, oab))
				{
					int maxValue = int.MaxValue;
					if (currentSite != null && oabService.Site != null)
					{
						serviceTopology.TryGetConnectionCost(currentSite, oabService.Site, ref maxValue, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\OabProxyRequestHandler.cs", "GetE14CASServer", 360);
					}
					if (maxValue == cheapestSiteConnectionCost)
					{
						cheapestCASServers.Add(oabService);
						return;
					}
					if (maxValue < cheapestSiteConnectionCost)
					{
						cheapestCASServers.Clear();
						cheapestCASServers.Add(oabService);
						cheapestSiteConnectionCost = maxValue;
					}
				}
			}, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\OabProxyRequestHandler.cs", "GetE14CASServer", 351);
			if (cheapestCASServers.Count == 0)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError(0L, "[OabProxyRequestHandler::ResolveAnchorMailbox] Could not find a valid downlevel CAS server for this OAB");
				}
				throw new HttpProxyException(HttpStatusCode.InternalServerError, 3007, "Could not find a valid downlevel CAS server for this OAB");
			}
			OabService oabService2;
			if (cheapestCASServers.Count == 1)
			{
				oabService2 = cheapestCASServers[0];
			}
			else
			{
				oabService2 = cheapestCASServers[OabProxyRequestHandler.RandomNumberGenerator.Next(cheapestCASServers.Count)];
			}
			return new ServerInfoAnchorMailbox(new BackEndServer(oabService2.ServerFullyQualifiedDomainName, oabService2.ServerVersionNumber), this);
		}

		// Token: 0x06000584 RID: 1412 RVA: 0x0001F018 File Offset: 0x0001D218
		private bool IsEligibleOabServiceBasedOnVersion(OabService oabService, OABCache.OABCacheEntry oabCacheEntry)
		{
			bool result = false;
			if (oabService != null && !oabService.IsOutOfService && oabService.ServerVersionNumber < Server.E15MinVersion)
			{
				result = true;
			}
			return result;
		}

		// Token: 0x06000585 RID: 1413 RVA: 0x0001F044 File Offset: 0x0001D244
		private bool IsEligibleOabServiceBasedOnVersionAndVirtualDirectory(OabService oabService, OABCache.OABCacheEntry oabCacheEntry)
		{
			bool result = false;
			if (oabService != null && !oabService.IsOutOfService && oabService.ServerVersionNumber < Server.E15MinVersion)
			{
				foreach (ADObjectId adobjectId in oabCacheEntry.VirtualDirectories)
				{
					if (oabService.Equals(adobjectId))
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}

		// Token: 0x0400037D RID: 893
		private static readonly Random RandomNumberGenerator = new Random();

		// Token: 0x02000125 RID: 293
		// (Invoke) Token: 0x06000868 RID: 2152
		private delegate bool IsEligibleOabService(OabService oabService, OABCache.OABCacheEntry oabCacheEntry);
	}
}
