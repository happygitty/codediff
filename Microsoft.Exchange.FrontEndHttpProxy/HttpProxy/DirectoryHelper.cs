﻿using System;
using System.Globalization;
using System.Net;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Global;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000076 RID: 118
	internal static class DirectoryHelper
	{
		// Token: 0x060003E3 RID: 995 RVA: 0x00016A28 File Offset: 0x00014C28
		internal static IRecipientSession GetRecipientSessionFromPartition(LatencyTracker latencyTracker, string partitionId, RequestDetailsLogger logger)
		{
			if (latencyTracker == null)
			{
				throw new ArgumentNullException("latencyTracker");
			}
			if (string.IsNullOrEmpty(partitionId))
			{
				throw new ArgumentNullException("partitionId");
			}
			ADSessionSettings adsessionSettings = null;
			PartitionId partitionIdObject = null;
			if ((Utilities.IsPartnerHostedOnly || GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled) && PartitionId.TryParse(partitionId, ref partitionIdObject))
			{
				try
				{
					adsessionSettings = DirectoryHelper.CreateADSessionSettingsWithDiagnostics(() => ADSessionSettings.FromAllTenantsPartitionId(partitionIdObject), logger, latencyTracker, true);
				}
				catch (CannotResolvePartitionException ex)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(2))
					{
						ExTraceGlobals.VerboseTracer.TraceWarning<string, CannotResolvePartitionException>(0L, "[DirectoryHelper::GetRecipientSessionFromPartition] Caught CannotResolvePartitionException when resolving partition by partition ID {0}. Exception details: {1}.", partitionId, ex);
					}
				}
			}
			if (adsessionSettings == null)
			{
				adsessionSettings = ADSessionSettings.FromRootOrgScopeSet();
			}
			return DirectoryHelper.CreateSession(adsessionSettings);
		}

		// Token: 0x060003E4 RID: 996 RVA: 0x00016AEC File Offset: 0x00014CEC
		internal static IRecipientSession GetRecipientSessionFromSmtpOrLiveId(string smtpOrLiveId, RequestDetailsLogger logger, LatencyTracker latencyTracker, bool ignoreCannotResolveTenantNameException = false)
		{
			if (string.IsNullOrEmpty(smtpOrLiveId))
			{
				throw new ArgumentNullException("smtpOrLiveId");
			}
			ADSessionSettings adsessionSettings = null;
			if ((Utilities.IsPartnerHostedOnly || GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).WindowsLiveID.Enabled) && SmtpAddress.IsValidSmtpAddress(smtpOrLiveId))
			{
				adsessionSettings = DirectoryHelper.CreateADSessionSettingsWithDiagnostics(() => ADSessionSettings.FromTenantRecipientSmtpAddress(new SmtpAddress(smtpOrLiveId), 2), logger, latencyTracker, ignoreCannotResolveTenantNameException);
			}
			if (adsessionSettings == null)
			{
				adsessionSettings = ADSessionSettings.FromRootOrgScopeSet();
			}
			return DirectoryHelper.CreateSession(adsessionSettings);
		}

		// Token: 0x060003E5 RID: 997 RVA: 0x00016B70 File Offset: 0x00014D70
		internal static IRecipientSession GetBusinessTenantRecipientSessionFromDomain(string domain, RequestDetailsLogger logger, LatencyTracker latencyTracker)
		{
			if (string.IsNullOrEmpty(domain))
			{
				throw new ArgumentNullException("domain");
			}
			ADSessionSettings sessionSettings;
			if (Utilities.IsPartnerHostedOnly || GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).WindowsLiveID.Enabled)
			{
				sessionSettings = DirectoryHelper.CreateADSessionSettingsWithDiagnostics(() => ADSessionSettings.FromBusinessTenantAcceptedDomain(domain), logger, latencyTracker, false);
			}
			else
			{
				sessionSettings = ADSessionSettings.FromRootOrgScopeSet();
			}
			return DirectoryHelper.CreateSession(sessionSettings);
		}

		// Token: 0x060003E6 RID: 998 RVA: 0x00016BE8 File Offset: 0x00014DE8
		internal static IRecipientSession GetRecipientSessionFromExternalDirectoryOrganizationId(LatencyTracker latencyTracker, Guid externalOrgId, RequestDetailsLogger logger)
		{
			if (latencyTracker == null)
			{
				throw new ArgumentNullException("latencyTracker");
			}
			if (externalOrgId == Guid.Empty)
			{
				throw new ArgumentNullException("latencyTracker");
			}
			ADSessionSettings sessionSettings = null;
			if (Utilities.IsPartnerHostedOnly || GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).WindowsLiveID.Enabled)
			{
				try
				{
					sessionSettings = DirectoryHelper.CreateADSessionSettingsWithDiagnostics(() => ADSessionSettings.FromExternalDirectoryOrganizationId(externalOrgId), logger, latencyTracker, false);
					goto IL_8F;
				}
				catch (CannotResolveExternalDirectoryOrganizationIdException ex)
				{
					throw new HttpProxyException(HttpStatusCode.NotFound, 3009, ex.Message, ex);
				}
			}
			sessionSettings = ADSessionSettings.FromRootOrgScopeSet();
			IL_8F:
			return DirectoryHelper.CreateSession(sessionSettings);
		}

		// Token: 0x060003E7 RID: 999 RVA: 0x00016C9C File Offset: 0x00014E9C
		internal static IRecipientSession GetRecipientSessionFromOrganizationId(LatencyTracker latencyTracker, OrganizationId organizationId, RequestDetailsLogger logger)
		{
			if (latencyTracker == null)
			{
				throw new ArgumentNullException("latencyTracker");
			}
			if (organizationId == null)
			{
				organizationId = OrganizationId.ForestWideOrgId;
			}
			Func<ADSessionSettings> <>9__1;
			return DirectoryHelper.CreateSession(DirectoryHelper.ExecuteFunctionAndUpdateMovingAveragePerformanceCounter<ADSessionSettings>(PerfCounters.HttpProxyCountersInstance.MovingAverageTenantLookupLatency, delegate
			{
				LatencyTracker latencyTracker2 = latencyTracker;
				Func<ADSessionSettings> glsCall;
				if ((glsCall = <>9__1) == null)
				{
					glsCall = (<>9__1 = (() => ADSessionSettings.FromOrganizationIdWithoutRbacScopesServiceOnly(organizationId)));
				}
				return DirectoryHelper.InvokeGls<ADSessionSettings>(latencyTracker2, glsCall, logger);
			}));
		}

		// Token: 0x060003E8 RID: 1000 RVA: 0x00016D10 File Offset: 0x00014F10
		internal static IRecipientSession GetRecipientSessionFromMailboxGuidAndDomain(Guid mailboxGuid, string domain, RequestDetailsLogger logger, LatencyTracker latencyTracker)
		{
			if (string.IsNullOrEmpty(domain))
			{
				throw new ArgumentNullException("domain");
			}
			ADSessionSettings sessionSettings;
			if (Utilities.IsPartnerHostedOnly || GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).WindowsLiveID.Enabled)
			{
				sessionSettings = DirectoryHelper.CreateADSessionSettingsWithDiagnostics(() => ADSessionSettings.FromTenantRecipientMailboxGuidAndDomain(mailboxGuid, domain, 2), logger, latencyTracker, false);
			}
			else
			{
				sessionSettings = ADSessionSettings.FromRootOrgScopeSet();
			}
			return DirectoryHelper.CreateSession(sessionSettings);
		}

		// Token: 0x060003E9 RID: 1001 RVA: 0x00016D90 File Offset: 0x00014F90
		internal static ITenantRecipientSession GetTenantRecipientSessionFromSmtpOrLiveId(string smtpOrLiveId, RequestDetailsLogger logger, LatencyTracker latencyTracker, bool ignoreCannotResolveTenantNameException = false)
		{
			if (string.IsNullOrEmpty(smtpOrLiveId))
			{
				throw new ArgumentNullException("smtpOrLiveId");
			}
			if (!SmtpAddress.IsValidSmtpAddress(smtpOrLiveId))
			{
				throw new ArgumentException(string.Format("{0} is not a valid SmtpAddress.", smtpOrLiveId));
			}
			if (!Utilities.IsPartnerHostedOnly && !GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).WindowsLiveID.Enabled)
			{
				throw new InvalidOperationException("Cannot create ITenantRecipientSession if WindowsLiveId feature is disabled.");
			}
			ADSessionSettings adsessionSettings = DirectoryHelper.CreateADSessionSettingsWithDiagnostics(() => ADSessionSettings.FromTenantRecipientSmtpAddress(new SmtpAddress(smtpOrLiveId), 2), logger, latencyTracker, ignoreCannotResolveTenantNameException);
			if (adsessionSettings != null)
			{
				return DirectorySessionFactory.Default.CreateTenantRecipientSession(null, null, CultureInfo.CurrentCulture.LCID, true, 2, null, adsessionSettings, 335, "GetTenantRecipientSessionFromSmtpOrLiveId", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\DirectoryHelper.cs");
			}
			return null;
		}

		// Token: 0x060003EA RID: 1002 RVA: 0x00016E54 File Offset: 0x00015054
		internal static ITenantRecipientSession GetTenantRecipientSessionFromPuidAndDomain(string puid, string domain, RequestDetailsLogger logger, LatencyTracker latencyTracker, bool ignoreCannotResolveTenantNameException = false)
		{
			if (string.IsNullOrEmpty(puid))
			{
				throw new ArgumentNullException("puid");
			}
			if (string.IsNullOrEmpty(domain))
			{
				throw new ArgumentNullException("domain");
			}
			if (!Utilities.IsPartnerHostedOnly && !GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).WindowsLiveID.Enabled)
			{
				throw new InvalidOperationException("Cannot create ITenantRecipientSession if WindowsLiveId feature is disabled.");
			}
			ADSessionSettings adsessionSettings = DirectoryHelper.CreateADSessionSettingsWithDiagnostics(() => ADSessionSettings.FromTenantRecipientPuidAndDomain(new NetID(puid).ToUInt64(), domain, 2), logger, latencyTracker, ignoreCannotResolveTenantNameException);
			if (adsessionSettings != null)
			{
				return DirectorySessionFactory.Default.CreateTenantRecipientSession(null, null, CultureInfo.CurrentCulture.LCID, true, 2, null, adsessionSettings, 395, "GetTenantRecipientSessionFromPuidAndDomain", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\DirectoryHelper.cs");
			}
			return null;
		}

		// Token: 0x060003EB RID: 1003 RVA: 0x00016F14 File Offset: 0x00015114
		internal static ITenantRecipientSession GetTenantRecipientSessionFromPuidAndTenantGuid(string puid, Guid tenantGuid, RequestDetailsLogger logger, LatencyTracker latencyTracker, bool ignoreCannotResolveTenantNameException = false)
		{
			if (string.IsNullOrEmpty(puid))
			{
				throw new ArgumentNullException("puid");
			}
			if (tenantGuid == Guid.Empty)
			{
				throw new ArgumentNullException("tenantGuid");
			}
			if (!Utilities.IsPartnerHostedOnly && !GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).WindowsLiveID.Enabled)
			{
				throw new InvalidOperationException("Cannot create ITenantRecipientSession if WindowsLiveId feature is disabled.");
			}
			ADSessionSettings adsessionSettings = DirectoryHelper.CreateADSessionSettingsWithDiagnostics(() => ADSessionSettings.FromExternalDirectoryOrganizationId(tenantGuid), logger, latencyTracker, ignoreCannotResolveTenantNameException);
			if (adsessionSettings != null)
			{
				return DirectorySessionFactory.Default.CreateTenantRecipientSession(null, null, CultureInfo.CurrentCulture.LCID, true, 2, null, adsessionSettings, 455, "GetTenantRecipientSessionFromPuidAndTenantGuid", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\DirectoryHelper.cs");
			}
			return null;
		}

		// Token: 0x060003EC RID: 1004 RVA: 0x00016FCA File Offset: 0x000151CA
		internal static ITopologyConfigurationSession GetConfigurationSession()
		{
			return DirectorySessionFactory.Default.CreateTopologyConfigurationSession(2, ADSessionSettings.FromRootOrgScopeSet(), 474, "GetConfigurationSession", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\DirectoryHelper.cs");
		}

		// Token: 0x060003ED RID: 1005 RVA: 0x00016FEB File Offset: 0x000151EB
		internal static IConfigurationSession GetRootOrgOrAllTenantsConfigurationSession(ADObjectId objectId)
		{
			return DirectorySessionFactory.Default.GetTenantOrTopologyConfigurationSession(2, ADSessionSettings.FromAllTenantsOrRootOrgAutoDetect(objectId), 486, "GetRootOrgOrAllTenantsConfigurationSession", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\DirectoryHelper.cs");
		}

		// Token: 0x060003EE RID: 1006 RVA: 0x0001700D File Offset: 0x0001520D
		internal static IConfigurationSession GetConfigurationSessionFromExchangeGuidAndDomain(Guid exchangeGuid, string domain)
		{
			return DirectorySessionFactory.Default.GetTenantOrTopologyConfigurationSession(2, string.IsNullOrEmpty(domain) ? ADSessionSettings.FromRootOrgScopeSet() : ADSessionSettings.FromTenantRecipientMailboxGuidAndDomain(exchangeGuid, domain, 2), 499, "GetConfigurationSessionFromExchangeGuidAndDomain", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\DirectoryHelper.cs");
		}

		// Token: 0x060003EF RID: 1007 RVA: 0x00017040 File Offset: 0x00015240
		internal static T InvokeGls<T>(LatencyTracker latencyTracker, Func<T> glsCall, RequestDetailsLogger logger)
		{
			return DirectoryHelper.InvokeGls<T>(latencyTracker, glsCall, Extensions.GetGenericInfoLogDelegate(logger));
		}

		// Token: 0x060003F0 RID: 1008 RVA: 0x00017050 File Offset: 0x00015250
		internal static T InvokeGls<T>(LatencyTracker latencyTracker, Func<T> glsCall, Action<string, long> loggingAction)
		{
			ArgumentValidator.ThrowIfNull("latencyTracker", latencyTracker);
			ArgumentValidator.ThrowIfNull("glsCall", glsCall);
			long latency = 0L;
			T latency2 = LatencyTracker.GetLatency<T>(() => GuardedGlsExecution.Default.Execute<T>(glsCall, loggingAction), out latency);
			latencyTracker.HandleGlsLatency(latency);
			return latency2;
		}

		// Token: 0x060003F1 RID: 1009 RVA: 0x000170A9 File Offset: 0x000152A9
		internal static T InvokeAccountForest<T>(LatencyTracker latencyTracker, Func<T> activeDirectoryFunction, RequestDetailsLogger logger, IDirectorySession session)
		{
			return DirectoryHelper.InvokeAccountForest<T>(latencyTracker, activeDirectoryFunction, Extensions.GetGenericInfoLogDelegate(logger), session);
		}

		// Token: 0x060003F2 RID: 1010 RVA: 0x000170BC File Offset: 0x000152BC
		internal static T InvokeAccountForest<T>(LatencyTracker latencyTracker, Func<T> activeDirectoryFunction, Action<string, long> loggingAction, IDirectorySession session)
		{
			ArgumentValidator.ThrowIfNull("latencyTracker", latencyTracker);
			ArgumentValidator.ThrowIfNull("activeDirectoryFunction", activeDirectoryFunction);
			long latency = 0L;
			T latency2 = LatencyTracker.GetLatency<T>(() => GuardedADAccountForestExecution.Default.Execute<T>(session, activeDirectoryFunction, loggingAction), out latency);
			latencyTracker.HandleAccountLatency(latency);
			return latency2;
		}

		// Token: 0x060003F3 RID: 1011 RVA: 0x0001711C File Offset: 0x0001531C
		internal static MailboxDatabase[] InvokeResourceForest(LatencyTracker latencyTracker, Func<MailboxDatabase[]> activeDirectoryFunction, RequestDetailsLogger logger, IDirectorySession session)
		{
			return DirectoryHelper.InvokeResourceForest(latencyTracker, activeDirectoryFunction, Extensions.GetGenericInfoLogDelegate(logger), session);
		}

		// Token: 0x060003F4 RID: 1012 RVA: 0x0001712C File Offset: 0x0001532C
		internal static MailboxDatabase[] InvokeResourceForest(LatencyTracker latencyTracker, Func<MailboxDatabase[]> activeDirectoryFunction, Action<string, long> loggingAction, IDirectorySession session)
		{
			ArgumentValidator.ThrowIfNull("latencyTracker", latencyTracker);
			ArgumentValidator.ThrowIfNull("activeDirectoryFunction", activeDirectoryFunction);
			long latency = 0L;
			MailboxDatabase[] latency2 = LatencyTracker.GetLatency<MailboxDatabase[]>(() => GuardedADResourceForestExecution.Default.Execute<MailboxDatabase[]>(session, activeDirectoryFunction, loggingAction), out latency);
			latencyTracker.HandleResourceLatency(latency);
			return latency2;
		}

		// Token: 0x060003F5 RID: 1013 RVA: 0x0001718C File Offset: 0x0001538C
		internal static ADRawEntry ResolveMailboxByProxyAddress(LatencyTracker latencyTracker, RequestDetailsLogger logger, Guid externalDirectoryOrganizationGuid, string targetMailbox, string proxyAddressPrefix)
		{
			IRecipientSession recipientSessionFromExternalDirectoryOrganizationId = DirectoryHelper.GetRecipientSessionFromExternalDirectoryOrganizationId(latencyTracker, externalDirectoryOrganizationGuid, logger);
			CustomProxyAddress customProxyAddress = new CustomProxyAddress(new CustomProxyAddressPrefix(proxyAddressPrefix), targetMailbox, true);
			return recipientSessionFromExternalDirectoryOrganizationId.FindByProxyAddress<ADUser>(customProxyAddress, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\DirectoryHelper.cs", 681, "ResolveMailboxByProxyAddress");
		}

		// Token: 0x060003F6 RID: 1014 RVA: 0x000171C5 File Offset: 0x000153C5
		internal static IRecipientSession GetRootOrgRecipientSession()
		{
			return DirectoryHelper.CreateSession(ADSessionSettings.FromRootOrgScopeSet());
		}

		// Token: 0x060003F7 RID: 1015 RVA: 0x000171D1 File Offset: 0x000153D1
		private static IRecipientSession CreateSession(ADSessionSettings sessionSettings)
		{
			return DirectorySessionFactory.Default.GetTenantOrRootOrgRecipientSession(true, 2, sessionSettings, 702, "CreateSession", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\DirectoryHelper.cs");
		}

		// Token: 0x060003F8 RID: 1016 RVA: 0x000171F0 File Offset: 0x000153F0
		private static ADSessionSettings CreateADSessionSettingsWithDiagnostics(Func<ADSessionSettings> activeDirectorySessionSettingsCreator, RequestDetailsLogger logger, LatencyTracker latencyTracker, bool ignoreCannotResolveTenantNameException)
		{
			if (latencyTracker == null)
			{
				throw new ArgumentNullException("latencyTracker");
			}
			if (logger == null)
			{
				throw new ArgumentNullException("logger");
			}
			ADSessionSettings result = null;
			try
			{
				result = DirectoryHelper.ExecuteFunctionAndUpdateMovingAveragePerformanceCounter<ADSessionSettings>(PerfCounters.HttpProxyCountersInstance.MovingAverageTenantLookupLatency, () => DirectoryHelper.InvokeGls<ADSessionSettings>(latencyTracker, activeDirectorySessionSettingsCreator, logger));
			}
			catch (CannotResolveTenantNameException ex)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(2))
				{
					ExTraceGlobals.VerboseTracer.TraceWarning<CannotResolveTenantNameException>(0L, "[DirectoryHelper::CreateADSessionSettingsWithDiagnostics] Caught CannotResolveTenantNameException when trying to get tenant recipient session. Exception details: {0}.", ex);
				}
				if (!ignoreCannotResolveTenantNameException)
				{
					throw new HttpProxyException(HttpStatusCode.NotFound, 3009, ex.Message, ex);
				}
			}
			return result;
		}

		// Token: 0x060003F9 RID: 1017 RVA: 0x000172AC File Offset: 0x000154AC
		private static T ExecuteFunctionAndUpdateMovingAveragePerformanceCounter<T>(ExPerformanceCounter performanceCounter, Func<T> operationToTrack)
		{
			long num = 0L;
			T latency;
			try
			{
				latency = LatencyTracker.GetLatency<T>(operationToTrack, out num);
			}
			finally
			{
				PerfCounters.UpdateMovingAveragePerformanceCounter(performanceCounter, num);
			}
			return latency;
		}
	}
}
