using System;
using Microsoft.Exchange.Data.Common;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x0200000B RID: 11
	public class OwaVdirConfiguration : VdirConfiguration
	{
		// Token: 0x06000061 RID: 97 RVA: 0x00003CAC File Offset: 0x00001EAC
		private OwaVdirConfiguration(ADOwaVirtualDirectory owaVirtualDirectory) : base(owaVirtualDirectory)
		{
			this.logonFormat = owaVirtualDirectory.LogonFormat;
			this.publicPrivateSelectionEnabled = (owaVirtualDirectory.LogonPagePublicPrivateSelectionEnabled != null && owaVirtualDirectory.LogonPagePublicPrivateSelectionEnabled.Value);
			this.lightSelectionEnabled = (owaVirtualDirectory.LogonPageLightSelectionEnabled != null && owaVirtualDirectory.LogonPageLightSelectionEnabled.Value);
			this.logonAndErrorLanguage = owaVirtualDirectory.LogonAndErrorLanguage;
			this.redirectToOptimalOWAServer = (owaVirtualDirectory.RedirectToOptimalOWAServer ?? true);
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000062 RID: 98 RVA: 0x00003D46 File Offset: 0x00001F46
		public new static OwaVdirConfiguration Instance
		{
			get
			{
				return VdirConfiguration.Instance as OwaVdirConfiguration;
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00003D52 File Offset: 0x00001F52
		public LogonFormats LogonFormat
		{
			get
			{
				return this.logonFormat;
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000064 RID: 100 RVA: 0x00003D5A File Offset: 0x00001F5A
		public bool PublicPrivateSelectionEnabled
		{
			get
			{
				return this.publicPrivateSelectionEnabled;
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000065 RID: 101 RVA: 0x00003D62 File Offset: 0x00001F62
		public bool LightSelectionEnabled
		{
			get
			{
				return this.lightSelectionEnabled;
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000066 RID: 102 RVA: 0x00003D6A File Offset: 0x00001F6A
		public int LogonAndErrorLanguage
		{
			get
			{
				return this.logonAndErrorLanguage;
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000067 RID: 103 RVA: 0x00003D72 File Offset: 0x00001F72
		public bool RedirectToOptimalOWAServer
		{
			get
			{
				return this.redirectToOptimalOWAServer;
			}
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00003D7C File Offset: 0x00001F7C
		internal static OwaVdirConfiguration CreateInstance(ITopologyConfigurationSession session, ADObjectId virtualDirectoryDN)
		{
			ADOwaVirtualDirectory adowaVirtualDirectory = null;
			ADOwaVirtualDirectory[] array = session.Find<ADOwaVirtualDirectory>(virtualDirectoryDN, 0, null, null, 1, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\fba\\OwaVdirConfiguration.cs", 142, "CreateInstance");
			if (array != null && array.Length == 1)
			{
				adowaVirtualDirectory = array[0];
			}
			if (adowaVirtualDirectory == null)
			{
				throw new ADNoSuchObjectException(LocalizedString.Empty);
			}
			return new OwaVdirConfiguration(adowaVirtualDirectory);
		}

		// Token: 0x04000072 RID: 114
		private readonly bool publicPrivateSelectionEnabled;

		// Token: 0x04000073 RID: 115
		private readonly bool lightSelectionEnabled;

		// Token: 0x04000074 RID: 116
		private readonly bool redirectToOptimalOWAServer;

		// Token: 0x04000075 RID: 117
		private readonly int logonAndErrorLanguage;

		// Token: 0x04000076 RID: 118
		private LogonFormats logonFormat;
	}
}
