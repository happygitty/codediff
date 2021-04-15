using System;
using System.Globalization;
using System.Reflection;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.ExchangeTopology;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.ExchangeSystem;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Global;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200004A RID: 74
	internal static class HttpProxyGlobals
	{
		// Token: 0x17000082 RID: 130
		// (get) Token: 0x0600026B RID: 619 RVA: 0x0000C5E5 File Offset: 0x0000A7E5
		public static bool IsMultitenant
		{
			get
			{
				return GlobalConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MultiTenancy.Enabled;
			}
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x0600026C RID: 620 RVA: 0x0000C5FD File Offset: 0x0000A7FD
		public static ProtocolType ProtocolType
		{
			get
			{
				return HttpProxyGlobals.ProtocolType;
			}
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x0600026D RID: 621 RVA: 0x0000C604 File Offset: 0x0000A804
		public static bool OnlyProxySecureConnections
		{
			get
			{
				return HttpProxyGlobals.OnlyProxySecureConnections;
			}
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x0600026E RID: 622 RVA: 0x0000C60B File Offset: 0x0000A80B
		public static string ApplicationVersion
		{
			get
			{
				return HttpProxyGlobals.ApplicationVersionInternal;
			}
		}

		// Token: 0x0600026F RID: 623 RVA: 0x0000C614 File Offset: 0x0000A814
		private static string GetSlimApplicationVersion()
		{
			string result = string.Empty;
			object[] customAttributes = typeof(HttpProxyGlobals).Assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
			if (customAttributes != null && customAttributes.Length != 0)
			{
				AssemblyFileVersionAttribute assemblyFileVersionAttribute = (AssemblyFileVersionAttribute)customAttributes[0];
				if (assemblyFileVersionAttribute != null)
				{
					result = Constants.NoLeadingZeroRegex.Replace(Constants.NoRevisionNumberRegex.Replace(assemblyFileVersionAttribute.Version, "$1"), "$1");
				}
			}
			return result;
		}

		// Token: 0x06000270 RID: 624 RVA: 0x0000C680 File Offset: 0x0000A880
		private static ExchangeVirtualDirectory LoadVirtualDirectoryFromAD()
		{
			ITopologyConfigurationSession topologyConfigurationSession = DirectorySessionFactory.Default.CreateTopologyConfigurationSession(true, 2, ADSessionSettings.FromRootOrgScopeSet(), 177, "LoadVirtualDirectoryFromAD", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs");
			Server server = topologyConfigurationSession.FindLocalServer("d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 183, "LoadVirtualDirectoryFromAD");
			if (server == null || server.Id == null)
			{
				if (ExTraceGlobals.BriefTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.BriefTracer.TraceError<ProtocolType>(0L, "Could not find Server object in AD", HttpProxyGlobals.ProtocolType);
				}
				return null;
			}
			string text = HttpRuntime.AppDomainAppId.Substring(3);
			if (text.EndsWith("owa/integrated", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Remove(text.LastIndexOf('/'));
			}
			text = string.Format(CultureInfo.InvariantCulture, "IIS://{0}{1}", server.Fqdn, text);
			ADObjectId descendantId = server.Id.GetDescendantId("Protocols", "HTTP", Array.Empty<string>());
			ComparisonFilter comparisonFilter = new ComparisonFilter(0, ExchangeVirtualDirectorySchema.MetabasePath, text);
			ExchangeVirtualDirectory[] array;
			switch (HttpProxyGlobals.ProtocolType)
			{
			case 0:
				array = topologyConfigurationSession.Find<ADMobileVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 246, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 1:
				array = topologyConfigurationSession.Find<ADEcpVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 254, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 2:
				array = topologyConfigurationSession.Find<ADWebServicesVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 262, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 3:
				array = topologyConfigurationSession.Find<ADOabVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 286, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 4:
			case 5:
				array = topologyConfigurationSession.Find<ADOwaVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 303, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 6:
			case 7:
			case 19:
				array = topologyConfigurationSession.Find<ADPowerShellVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 321, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 8:
				array = topologyConfigurationSession.Find<ADRpcHttpVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 329, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 9:
				array = topologyConfigurationSession.Find<ADAutodiscoverVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 230, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 11:
				array = topologyConfigurationSession.Find<ADPswsVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 311, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 13:
				array = topologyConfigurationSession.Find<ADPushNotificationsVirtualDirectory>(descendantId, 1, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 337, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 14:
				array = topologyConfigurationSession.Find<ADMapiVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 278, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 16:
				array = topologyConfigurationSession.Find<ADOutlookServiceVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 294, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 17:
				array = topologyConfigurationSession.Find<ADSnackyServiceVirtualDirectory>(descendantId, 1, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 353, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 18:
				array = topologyConfigurationSession.Find<ADMicroServiceVirtualDirectory>(descendantId, 1, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 361, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 20:
				array = topologyConfigurationSession.Find<ADO365SuiteServiceVirtualDirectory>(descendantId, 1, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 345, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 21:
				array = topologyConfigurationSession.Find<ADMailboxDeliveryVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 270, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 22:
				array = topologyConfigurationSession.Find<ADComplianceServiceVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 238, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			case 27:
				array = topologyConfigurationSession.Find<ADRestVirtualDirectory>(descendantId, 2, comparisonFilter, null, 2, "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\Configuration\\HttpProxyGlobals.cs", 369, "LoadVirtualDirectoryFromAD");
				goto IL_3AB;
			}
			array = null;
			IL_3AB:
			if (array == null || array.Length == 0)
			{
				if (ExTraceGlobals.BriefTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.BriefTracer.TraceError<ProtocolType>(0L, "Could not find AD virtual directory entry for protocol [0]", HttpProxyGlobals.ProtocolType);
				}
				return null;
			}
			ExchangeVirtualDirectory result;
			if (array.Length == 1)
			{
				result = array[0];
			}
			else
			{
				if (ExTraceGlobals.BriefTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.BriefTracer.TraceError<ProtocolType>(0L, "Found more than one AD virtual directory entry for protocol [0]", HttpProxyGlobals.ProtocolType);
				}
				result = array[0];
			}
			return result;
		}

		// Token: 0x04000179 RID: 377
		public static readonly LazyMember<ExchangeVirtualDirectory> VdirObject = new LazyMember<ExchangeVirtualDirectory>(() => HttpProxyGlobals.LoadVirtualDirectoryFromAD());

		// Token: 0x0400017A RID: 378
		public static readonly LazyMember<string> LocalMachineFqdn = new LazyMember<string>(() => HttpProxyGlobals.LocalMachineFqdn.Member);

		// Token: 0x0400017B RID: 379
		public static readonly LazyMember<string> LocalMachineForest = new LazyMember<string>(() => HttpProxyGlobals.LocalMachineForest.Member);

		// Token: 0x0400017C RID: 380
		public static readonly LazyMember<string> LocalMachineRegion = new LazyMember<string>(() => HttpProxyGlobals.LocalMachineForest.Member.Substring(0, 3).ToUpper());

		// Token: 0x0400017D RID: 381
		public static readonly LazyMember<Site> LocalSite = new LazyMember<Site>(() => new Site(new TopologySite(LocalSiteCache.LocalSite)));

		// Token: 0x0400017E RID: 382
		public static readonly LazyMember<string> VirtualDirectoryName = new LazyMember<string>(delegate()
		{
			string text = HttpRuntime.AppDomainAppVirtualPath;
			if (text[0] == '/')
			{
				text = text.Substring(1);
			}
			return text;
		});

		// Token: 0x0400017F RID: 383
		private static readonly string ApplicationVersionInternal = HttpProxyGlobals.GetSlimApplicationVersion();
	}
}
