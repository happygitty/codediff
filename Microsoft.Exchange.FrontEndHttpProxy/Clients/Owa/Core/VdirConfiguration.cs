using System;
using System.DirectoryServices;
using System.IO;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Common;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.HttpProxy;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000015 RID: 21
	public abstract class VdirConfiguration
	{
		// Token: 0x060000AB RID: 171 RVA: 0x00004B80 File Offset: 0x00002D80
		internal VdirConfiguration(ExchangeWebAppVirtualDirectory virtualDirectory)
		{
			this.internalAuthenticationMethod = VdirConfiguration.ConvertAuthenticationMethods(virtualDirectory.InternalAuthenticationMethods);
			this.externalAuthenticationMethod = VdirConfiguration.ConvertAuthenticationMethods(virtualDirectory.ExternalAuthenticationMethods);
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060000AC RID: 172 RVA: 0x00004BAC File Offset: 0x00002DAC
		public static VdirConfiguration Instance
		{
			get
			{
				if (VdirConfiguration.instance == null)
				{
					object obj = VdirConfiguration.syncRoot;
					lock (obj)
					{
						if (VdirConfiguration.instance == null)
						{
							VdirConfiguration.instance = VdirConfiguration.BaseCreateInstance();
						}
					}
				}
				return VdirConfiguration.instance;
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060000AD RID: 173 RVA: 0x00004C0C File Offset: 0x00002E0C
		internal AuthenticationMethod InternalAuthenticationMethod
		{
			get
			{
				return this.internalAuthenticationMethod;
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060000AE RID: 174 RVA: 0x00004C14 File Offset: 0x00002E14
		internal AuthenticationMethod ExternalAuthenticationMethod
		{
			get
			{
				return this.externalAuthenticationMethod;
			}
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00004C1C File Offset: 0x00002E1C
		private static AuthenticationMethod ConvertAuthenticationMethods(MultiValuedProperty<AuthenticationMethod> configMethods)
		{
			AuthenticationMethod authenticationMethod = 0;
			using (MultiValuedProperty<AuthenticationMethod>.Enumerator enumerator = configMethods.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					switch (enumerator.Current)
					{
					case 0:
						authenticationMethod |= 1;
						break;
					case 1:
						authenticationMethod |= 8;
						break;
					case 2:
						authenticationMethod |= 2;
						break;
					case 3:
						authenticationMethod |= 4;
						break;
					case 4:
						authenticationMethod |= 16;
						break;
					case 5:
						authenticationMethod |= 32;
						break;
					case 6:
						authenticationMethod |= 64;
						break;
					case 7:
						authenticationMethod |= 128;
						break;
					case 8:
						authenticationMethod |= 256;
						break;
					case 9:
						authenticationMethod |= 512;
						break;
					case 10:
						authenticationMethod |= 1024;
						break;
					case 11:
						authenticationMethod |= 2048;
						break;
					case 12:
						authenticationMethod |= 4096;
						break;
					case 13:
						authenticationMethod |= 8192;
						break;
					case 14:
						authenticationMethod |= 16384;
						break;
					}
				}
			}
			return authenticationMethod;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00004D2C File Offset: 0x00002F2C
		private static VdirConfiguration BaseCreateInstance()
		{
			ITopologyConfigurationSession session = VdirConfiguration.CreateADSystemConfigurationSessionScopedToFirstOrg();
			ExchangeVirtualDirectory member = HttpProxyGlobals.VdirObject.Member;
			if (member is ADEcpVirtualDirectory)
			{
				return EcpVdirConfiguration.CreateInstance(session, member.Id);
			}
			if (member is ADOwaVirtualDirectory)
			{
				return OwaVdirConfiguration.CreateInstance(session, member.Id);
			}
			throw new ADNoSuchObjectException(new LocalizedString(string.Format("NoVdirConfiguration. AppDomainAppId:{0},VDirDN:{1}", HttpRuntime.AppDomainAppId, (member == null) ? "NULL" : member.DistinguishedName)));
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00004D9D File Offset: 0x00002F9D
		private static ITopologyConfigurationSession CreateADSystemConfigurationSessionScopedToFirstOrg()
		{
			return DirectorySessionFactory.Default.CreateTopologyConfigurationSession(0, ADSessionSettings.FromRootOrgScopeSet(), 207, "CreateADSystemConfigurationSessionScopedToFirstOrg", "d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\fba\\VdirConfiguration.cs");
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00004DC0 File Offset: 0x00002FC0
		private static string GetWebSiteName(string webSiteRootPath)
		{
			try
			{
				using (DirectoryEntry directoryEntry = new DirectoryEntry(webSiteRootPath))
				{
					using (DirectoryEntry parent = directoryEntry.Parent)
					{
						if (parent != null)
						{
							return ((string)parent.Properties["ServerComment"].Value) ?? string.Empty;
						}
					}
				}
			}
			catch (DirectoryServicesCOMException)
			{
			}
			catch (DirectoryNotFoundException)
			{
			}
			return string.Empty;
		}

		// Token: 0x040000B4 RID: 180
		private static volatile VdirConfiguration instance;

		// Token: 0x040000B5 RID: 181
		private static object syncRoot = new object();

		// Token: 0x040000B6 RID: 182
		private readonly AuthenticationMethod internalAuthenticationMethod;

		// Token: 0x040000B7 RID: 183
		private readonly AuthenticationMethod externalAuthenticationMethod;
	}
}
