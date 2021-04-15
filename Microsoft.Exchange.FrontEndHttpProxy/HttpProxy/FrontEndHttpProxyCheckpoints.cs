using System;
using System.Collections.Generic;
using Microsoft.Exchange.Diagnostics;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000087 RID: 135
	internal class FrontEndHttpProxyCheckpoints : CheckpointBase
	{
		// Token: 0x06000478 RID: 1144 RVA: 0x000192C4 File Offset: 0x000174C4
		static FrontEndHttpProxyCheckpoints()
		{
			List<CheckpointBase> list = new List<CheckpointBase>
			{
				FrontEndHttpProxyCheckpoints.ProxyModuleBeginRequest,
				FrontEndHttpProxyCheckpoints.ProxyModuleAuthenticateRequest,
				FrontEndHttpProxyCheckpoints.ProxyModulePostAuthorizeRequest,
				FrontEndHttpProxyCheckpoints.ProxyModulePreSendRequestHeaders,
				FrontEndHttpProxyCheckpoints.ProxyModuleEndRequest
			};
			CheckpointTracker.Register(typeof(FrontEndHttpProxyCheckpoints), list);
		}

		// Token: 0x06000479 RID: 1145 RVA: 0x00019369 File Offset: 0x00017569
		public FrontEndHttpProxyCheckpoints(string checkpointName) : base(checkpointName)
		{
		}

		// Token: 0x17000102 RID: 258
		// (get) Token: 0x0600047A RID: 1146 RVA: 0x00019372 File Offset: 0x00017572
		internal static FrontEndHttpProxyCheckpoints ProxyModuleBeginRequest
		{
			get
			{
				return FrontEndHttpProxyCheckpoints.proxyModuleBeginRequest;
			}
		}

		// Token: 0x17000103 RID: 259
		// (get) Token: 0x0600047B RID: 1147 RVA: 0x00019379 File Offset: 0x00017579
		internal static FrontEndHttpProxyCheckpoints ProxyModuleAuthenticateRequest
		{
			get
			{
				return FrontEndHttpProxyCheckpoints.proxyModuleAuthenticateRequest;
			}
		}

		// Token: 0x17000104 RID: 260
		// (get) Token: 0x0600047C RID: 1148 RVA: 0x00019380 File Offset: 0x00017580
		internal static FrontEndHttpProxyCheckpoints ProxyModulePostAuthorizeRequest
		{
			get
			{
				return FrontEndHttpProxyCheckpoints.proxyModulePostAuthorizeRequest;
			}
		}

		// Token: 0x17000105 RID: 261
		// (get) Token: 0x0600047D RID: 1149 RVA: 0x00019387 File Offset: 0x00017587
		internal static FrontEndHttpProxyCheckpoints ProxyModulePreSendRequestHeaders
		{
			get
			{
				return FrontEndHttpProxyCheckpoints.proxyModulePreSendRequestHeaders;
			}
		}

		// Token: 0x17000106 RID: 262
		// (get) Token: 0x0600047E RID: 1150 RVA: 0x0001938E File Offset: 0x0001758E
		internal static FrontEndHttpProxyCheckpoints ProxyModuleEndRequest
		{
			get
			{
				return FrontEndHttpProxyCheckpoints.proxyModuleEndRequest;
			}
		}

		// Token: 0x04000313 RID: 787
		private static FrontEndHttpProxyCheckpoints proxyModuleBeginRequest = new FrontEndHttpProxyCheckpoints("PM_ABR");

		// Token: 0x04000314 RID: 788
		private static FrontEndHttpProxyCheckpoints proxyModuleAuthenticateRequest = new FrontEndHttpProxyCheckpoints("PM_AANR");

		// Token: 0x04000315 RID: 789
		private static FrontEndHttpProxyCheckpoints proxyModulePostAuthorizeRequest = new FrontEndHttpProxyCheckpoints("PM_APAZR");

		// Token: 0x04000316 RID: 790
		private static FrontEndHttpProxyCheckpoints proxyModulePreSendRequestHeaders = new FrontEndHttpProxyCheckpoints("PM_APSRH");

		// Token: 0x04000317 RID: 791
		private static FrontEndHttpProxyCheckpoints proxyModuleEndRequest = new FrontEndHttpProxyCheckpoints("PM_AER");
	}
}
