using System;
using System.Collections.Generic;
using Microsoft.Exchange.Diagnostics;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000087 RID: 135
	internal class FrontEndHttpProxyCheckpoints : CheckpointBase
	{
		// Token: 0x0600047C RID: 1148 RVA: 0x00019484 File Offset: 0x00017684
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

		// Token: 0x0600047D RID: 1149 RVA: 0x00019529 File Offset: 0x00017729
		public FrontEndHttpProxyCheckpoints(string checkpointName) : base(checkpointName)
		{
		}

		// Token: 0x17000102 RID: 258
		// (get) Token: 0x0600047E RID: 1150 RVA: 0x00019532 File Offset: 0x00017732
		internal static FrontEndHttpProxyCheckpoints ProxyModuleBeginRequest
		{
			get
			{
				return FrontEndHttpProxyCheckpoints.proxyModuleBeginRequest;
			}
		}

		// Token: 0x17000103 RID: 259
		// (get) Token: 0x0600047F RID: 1151 RVA: 0x00019539 File Offset: 0x00017739
		internal static FrontEndHttpProxyCheckpoints ProxyModuleAuthenticateRequest
		{
			get
			{
				return FrontEndHttpProxyCheckpoints.proxyModuleAuthenticateRequest;
			}
		}

		// Token: 0x17000104 RID: 260
		// (get) Token: 0x06000480 RID: 1152 RVA: 0x00019540 File Offset: 0x00017740
		internal static FrontEndHttpProxyCheckpoints ProxyModulePostAuthorizeRequest
		{
			get
			{
				return FrontEndHttpProxyCheckpoints.proxyModulePostAuthorizeRequest;
			}
		}

		// Token: 0x17000105 RID: 261
		// (get) Token: 0x06000481 RID: 1153 RVA: 0x00019547 File Offset: 0x00017747
		internal static FrontEndHttpProxyCheckpoints ProxyModulePreSendRequestHeaders
		{
			get
			{
				return FrontEndHttpProxyCheckpoints.proxyModulePreSendRequestHeaders;
			}
		}

		// Token: 0x17000106 RID: 262
		// (get) Token: 0x06000482 RID: 1154 RVA: 0x0001954E File Offset: 0x0001774E
		internal static FrontEndHttpProxyCheckpoints ProxyModuleEndRequest
		{
			get
			{
				return FrontEndHttpProxyCheckpoints.proxyModuleEndRequest;
			}
		}

		// Token: 0x04000317 RID: 791
		private static FrontEndHttpProxyCheckpoints proxyModuleBeginRequest = new FrontEndHttpProxyCheckpoints("PM_ABR");

		// Token: 0x04000318 RID: 792
		private static FrontEndHttpProxyCheckpoints proxyModuleAuthenticateRequest = new FrontEndHttpProxyCheckpoints("PM_AANR");

		// Token: 0x04000319 RID: 793
		private static FrontEndHttpProxyCheckpoints proxyModulePostAuthorizeRequest = new FrontEndHttpProxyCheckpoints("PM_APAZR");

		// Token: 0x0400031A RID: 794
		private static FrontEndHttpProxyCheckpoints proxyModulePreSendRequestHeaders = new FrontEndHttpProxyCheckpoints("PM_APSRH");

		// Token: 0x0400031B RID: 795
		private static FrontEndHttpProxyCheckpoints proxyModuleEndRequest = new FrontEndHttpProxyCheckpoints("PM_AER");
	}
}
