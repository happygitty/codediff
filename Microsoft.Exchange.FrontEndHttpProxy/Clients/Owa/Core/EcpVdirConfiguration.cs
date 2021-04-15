using System;
using Microsoft.Exchange.Data.Common;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000004 RID: 4
	public class EcpVdirConfiguration : VdirConfiguration
	{
		// Token: 0x06000014 RID: 20 RVA: 0x00002D0C File Offset: 0x00000F0C
		private EcpVdirConfiguration(ADEcpVirtualDirectory ecpVirtualDirectory) : base(ecpVirtualDirectory)
		{
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000015 RID: 21 RVA: 0x00002D15 File Offset: 0x00000F15
		public new static EcpVdirConfiguration Instance
		{
			get
			{
				return VdirConfiguration.Instance as EcpVdirConfiguration;
			}
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002D24 File Offset: 0x00000F24
		internal static EcpVdirConfiguration CreateInstance(ITopologyConfigurationSession session, ADObjectId virtualDirectoryDN)
		{
			ADEcpVirtualDirectory adecpVirtualDirectory = null;
			ADEcpVirtualDirectory[] array = session.Find<ADEcpVirtualDirectory>(virtualDirectoryDN, 0, null, null, 1, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\fba\\EcpVdirConfiguration.cs", 52, "CreateInstance");
			if (array != null && array.Length == 1)
			{
				adecpVirtualDirectory = array[0];
			}
			if (adecpVirtualDirectory == null)
			{
				throw new ADNoSuchObjectException(LocalizedString.Empty);
			}
			return new EcpVdirConfiguration(adecpVirtualDirectory);
		}
	}
}
