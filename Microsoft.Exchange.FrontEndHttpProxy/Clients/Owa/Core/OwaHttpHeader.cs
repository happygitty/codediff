using System;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000007 RID: 7
	public static class OwaHttpHeader
	{
		// Token: 0x04000027 RID: 39
		public const string Version = "X-OWA-Version";

		// Token: 0x04000028 RID: 40
		public const string ProxyVersion = "X-OWA-ProxyVersion";

		// Token: 0x04000029 RID: 41
		public const string ProxyUri = "X-OWA-ProxyUri";

		// Token: 0x0400002A RID: 42
		public const string ProxySid = "X-OWA-ProxySid";

		// Token: 0x0400002B RID: 43
		public const string ProxyCanary = "X-OWA-ProxyCanary";

		// Token: 0x0400002C RID: 44
		public const string EventResult = "X-OWA-EventResult";

		// Token: 0x0400002D RID: 45
		public const string OwaError = "X-OWA-Error";

		// Token: 0x0400002E RID: 46
		public const string OwaFEError = "X-OWA-FEError";

		// Token: 0x0400002F RID: 47
		public const string ProxyWebPart = "X-OWA-ProxyWebPart";

		// Token: 0x04000030 RID: 48
		public const string PerfConsoleRowId = "X-OWA-PerfConsoleRowId";

		// Token: 0x04000031 RID: 49
		public const string IsaNoCompression = "X-NoCompression";

		// Token: 0x04000032 RID: 50
		public const string IsaNoBuffering = "X-NoBuffering";

		// Token: 0x04000033 RID: 51
		public const string PublishedAccessPath = "X-OWA-PublishedAccessPath";

		// Token: 0x04000034 RID: 52
		public const string DoNotCache = "X-OWA-DoNotCache";

		// Token: 0x04000035 RID: 53
		public static readonly string ExplicitLogonUser = Constants.OwaExplicitLogonUser;
	}
}
