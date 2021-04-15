using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.Exchange.Common;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000068 RID: 104
	internal sealed class HealthCheckResponder
	{
		// Token: 0x0600036F RID: 879 RVA: 0x000136AA File Offset: 0x000118AA
		private HealthCheckResponder()
		{
		}

		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x06000370 RID: 880 RVA: 0x000136C0 File Offset: 0x000118C0
		public static HealthCheckResponder Instance
		{
			get
			{
				if (HealthCheckResponder.instance == null)
				{
					object obj = HealthCheckResponder.staticLock;
					lock (obj)
					{
						if (HealthCheckResponder.instance == null)
						{
							HealthCheckResponder.instance = new HealthCheckResponder();
						}
					}
				}
				return HealthCheckResponder.instance;
			}
		}

		// Token: 0x06000371 RID: 881 RVA: 0x00013718 File Offset: 0x00011918
		public bool IsHealthCheckRequest(HttpContext httpContext)
		{
			return httpContext.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) && httpContext.Request.Url.AbsolutePath.EndsWith(Constants.HealthCheckPage, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x06000372 RID: 882 RVA: 0x00013750 File Offset: 0x00011950
		public void CheckHealthStateAndRespond(HttpContext httpContext)
		{
			if (!HealthCheckResponder.HealthCheckResponderEnabled.Value)
			{
				this.RespondSuccess(httpContext);
			}
			else
			{
				ServerComponentEnum serverComponentEnum = 0;
				if (!HealthCheckResponder.ProtocolServerComponentMap.TryGetValue(HttpProxyGlobals.ProtocolType, out serverComponentEnum))
				{
					throw new InvalidOperationException("Unknown protocol type " + HttpProxyGlobals.ProtocolType);
				}
				if (HttpProxySettings.HealthCheckResponderServerComponentOverride.Value)
				{
					serverComponentEnum = 22;
				}
				DateTime utcNow = DateTime.UtcNow;
				if (this.componentStateNextLookupTime <= utcNow)
				{
					this.isComponentOnline = ServerComponentStateManager.IsOnline(serverComponentEnum);
					this.componentStateNextLookupTime = utcNow.AddSeconds(15.0);
				}
				if (!this.isComponentOnline)
				{
					this.RespondFailure(httpContext);
				}
				else
				{
					this.RespondSuccess(httpContext);
				}
			}
			httpContext.ApplicationInstance.CompleteRequest();
		}

		// Token: 0x06000373 RID: 883 RVA: 0x0001380C File Offset: 0x00011A0C
		private void RespondSuccess(HttpContext httpContext)
		{
			PerfCounters.HttpProxyCountersInstance.LoadBalancerHealthChecks.RawValue = 1L;
			httpContext.Response.StatusCode = 200;
			httpContext.Response.Write(Constants.HealthCheckPageResponse);
			httpContext.Response.Write("<br/>");
			httpContext.Response.Write(HttpProxyGlobals.LocalMachineFqdn.Member);
		}

		// Token: 0x06000374 RID: 884 RVA: 0x0001386F File Offset: 0x00011A6F
		private void RespondFailure(HttpContext httpContext)
		{
			PerfCounters.HttpProxyCountersInstance.LoadBalancerHealthChecks.RawValue = 0L;
			httpContext.Response.Close();
		}

		// Token: 0x0400022F RID: 559
		private const int ComponentStateLookupTimeIntervalInSeconds = 15;

		// Token: 0x04000230 RID: 560
		private static readonly BoolAppSettingsEntry HealthCheckResponderEnabled = new BoolAppSettingsEntry(HttpProxySettings.Prefix("HealthCheckResponderEnabled"), true, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000231 RID: 561
		private static readonly Dictionary<ProtocolType, ServerComponentEnum> ProtocolServerComponentMap = new Dictionary<ProtocolType, ServerComponentEnum>
		{
			{
				9,
				8
			},
			{
				28,
				32
			},
			{
				0,
				9
			},
			{
				1,
				10
			},
			{
				2,
				11
			},
			{
				21,
				29
			},
			{
				14,
				25
			},
			{
				3,
				13
			},
			{
				4,
				14
			},
			{
				5,
				14
			},
			{
				13,
				16
			},
			{
				6,
				17
			},
			{
				7,
				17
			},
			{
				10,
				18
			},
			{
				27,
				31
			},
			{
				8,
				19
			},
			{
				12,
				21
			}
		};

		// Token: 0x04000232 RID: 562
		private static HealthCheckResponder instance = null;

		// Token: 0x04000233 RID: 563
		private static object staticLock = new object();

		// Token: 0x04000234 RID: 564
		private bool isComponentOnline;

		// Token: 0x04000235 RID: 565
		private DateTime componentStateNextLookupTime = DateTime.UtcNow;
	}
}
