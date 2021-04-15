using System;
using Microsoft.Exchange.Data.ConfigurationSettings;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.ExchangeSystem;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000049 RID: 73
	internal static class HttpProxySettings
	{
		// Token: 0x06000268 RID: 616 RVA: 0x0000C1DF File Offset: 0x0000A3DF
		public static string Prefix(string appSettingName)
		{
			return HttpProxySettings.Prefix(appSettingName);
		}

		// Token: 0x06000269 RID: 617 RVA: 0x0000C1E8 File Offset: 0x0000A3E8
		private static int GetBufferBoundary(BufferPoolCollection.BufferSize bufferSize)
		{
			int num = 1024;
			string text = bufferSize.ToString();
			char c = text[text.Length - 1];
			if (c == 'M')
			{
				num *= 1024;
			}
			else if (c != 'K')
			{
				throw new ArgumentException(string.Format("BufferSize format for BufferSize value {0} is not supported", bufferSize));
			}
			return Convert.ToInt32(text.Substring(4, text.Length - 5)) * num;
		}

		// Token: 0x0400015B RID: 347
		public static readonly BoolAppSettingsEntry WriteDiagnosticHeaders = new BoolAppSettingsEntry(HttpProxySettings.Prefix("WriteDiagnosticHeaders"), true, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400015C RID: 348
		public static readonly BoolAppSettingsEntry UseDefaultWebProxy = new BoolAppSettingsEntry(HttpProxySettings.Prefix("UseDefaultWebProxy"), false, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400015D RID: 349
		public static readonly BoolAppSettingsEntry UseSmartBufferSizing = new BoolAppSettingsEntry(HttpProxySettings.Prefix("UseSmartBufferSizing"), true, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400015E RID: 350
		public static readonly EnumAppSettingsEntry<BufferPoolCollection.BufferSize> RequestBufferSize = new EnumAppSettingsEntry<BufferPoolCollection.BufferSize>(HttpProxySettings.Prefix("RequestBufferSize"), 9, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400015F RID: 351
		public static readonly EnumAppSettingsEntry<BufferPoolCollection.BufferSize> MinimumRequestBufferSize = new EnumAppSettingsEntry<BufferPoolCollection.BufferSize>(HttpProxySettings.Prefix("MinimumRequestBufferSize"), 2, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000160 RID: 352
		public static readonly EnumAppSettingsEntry<BufferPoolCollection.BufferSize> ResponseBufferSize = new EnumAppSettingsEntry<BufferPoolCollection.BufferSize>(HttpProxySettings.Prefix("ResponseBufferSize"), 9, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000161 RID: 353
		public static readonly EnumAppSettingsEntry<BufferPoolCollection.BufferSize> MinimumResponseBufferSize = new EnumAppSettingsEntry<BufferPoolCollection.BufferSize>(HttpProxySettings.Prefix("MinimumResponseBufferSize"), 2, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000162 RID: 354
		public static readonly LazyMember<long> RequestBufferBoundary = new LazyMember<long>(() => (long)HttpProxySettings.GetBufferBoundary(HttpProxySettings.RequestBufferSize.Value));

		// Token: 0x04000163 RID: 355
		public static readonly LazyMember<long> ResponseBufferBoundary = new LazyMember<long>(() => (long)HttpProxySettings.GetBufferBoundary(HttpProxySettings.ResponseBufferSize.Value));

		// Token: 0x04000164 RID: 356
		public static readonly BoolAppSettingsEntry TestBackEndSupportEnabled = new BoolAppSettingsEntry(HttpProxySettings.Prefix("TestBackEndSupportEnabled"), false, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000165 RID: 357
		public static readonly IntAppSettingsEntry SerializeClientAccessContext = new IntAppSettingsEntry(HttpProxySettings.Prefix("SerializeClientAccessContext"), 0, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000166 RID: 358
		public static readonly BoolAppSettingsEntry DFPOWAVdirProxyEnabled = new BoolAppSettingsEntry("DFPOWAProxyEnabled", false, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000167 RID: 359
		public static readonly StringAppSettingsEntry CaptureResponsesLocation = new StringAppSettingsEntry(HttpProxySettings.Prefix("CaptureResponsesLocation"), string.Empty, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000168 RID: 360
		public static readonly IntAppSettingsEntry ServicePointConnectionLimit = new IntAppSettingsEntry(HttpProxySettings.Prefix("ServicePointConnectionLimit"), 65000, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000169 RID: 361
		public static readonly BoolAppSettingsEntry DetailedLatencyTracingEnabled = new BoolAppSettingsEntry(HttpProxySettings.Prefix("DetailedLatencyTracingEnabled"), false, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400016A RID: 362
		public static readonly IntAppSettingsEntry MaxRetryOnError = new IntAppSettingsEntry(HttpProxySettings.Prefix("MaxRetryOnError"), CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).RetryOnError.Enabled ? 2 : 0, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400016B RID: 363
		public static readonly BoolAppSettingsEntry RetryOnConnectivityErrorEnabled = new BoolAppSettingsEntry(HttpProxySettings.Prefix("RetryOnConnectivityErrorEnabled"), false, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400016C RID: 364
		public static readonly IntAppSettingsEntry DelayOnRetryOnError = new IntAppSettingsEntry(HttpProxySettings.Prefix("DelayOnRetryOnError"), 5000, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400016D RID: 365
		public static readonly FlightableBoolAppSettingsEntry MailboxServerLocatorSharedCacheEnabled = new FlightableBoolAppSettingsEntry(HttpProxySettings.Prefix("MailboxServerLocatorSharedCacheEnabled"), () => HttpProxyGlobals.IsMultitenant && CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).MailboxServerSharedCache.Enabled);

		// Token: 0x0400016E RID: 366
		public static readonly StringAppSettingsEntry EnableLiveIdBasicBEAuthVersion = new StringAppSettingsEntry("LiveIdBasicAuthModule.EnableBEAuthVersion", string.Empty, ExTraceGlobals.VerboseTracer);

		// Token: 0x0400016F RID: 367
		public static readonly StringAppSettingsEntry EnableOAuthBEAuthVersion = new StringAppSettingsEntry("OAuthHttpModule.EnableBEAuthVersion", string.Empty, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000170 RID: 368
		public static readonly StringAppSettingsEntry EnableDefaultBEAuthVersion = new StringAppSettingsEntry("DefaultAuthBehavior.EnableBEAuthVersion", string.Empty, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000171 RID: 369
		public static readonly FlightableBoolAppSettingsEntry AnchorMailboxSharedCacheEnabled = new FlightableBoolAppSettingsEntry(HttpProxySettings.Prefix("AnchorMailboxSharedCacheEnabled"), () => HttpProxyGlobals.IsMultitenant && CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).AnchorMailboxSharedCache.Enabled);

		// Token: 0x04000172 RID: 370
		public static readonly IntAppSettingsEntry GlobalSharedCacheRpcTimeout = new IntAppSettingsEntry(HttpProxySettings.Prefix("GlobalSharedCacheRpcTimeout"), 2000, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000173 RID: 371
		public static readonly StringAppSettingsEntry EnableLiveIdCookieBEAuthVersion = new StringAppSettingsEntry("LiveIdCookieAuthModule.EnableBEAuthVersion", string.Empty, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000174 RID: 372
		public static readonly EnumAppSettingsEntry<ProxyRequestHandler.SupportBackEndCookie> SupportBackEndCookie = new EnumAppSettingsEntry<ProxyRequestHandler.SupportBackEndCookie>(HttpProxySettings.Prefix("SupportBackEndCookie"), CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).UseResourceForest.Enabled ? ProxyRequestHandler.SupportBackEndCookie.All : ProxyRequestHandler.SupportBackEndCookie.V1, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000175 RID: 373
		public static readonly FlightableBoolAppSettingsEntry CafeV1RUMEnabled = new FlightableBoolAppSettingsEntry(HttpProxySettings.Prefix("CafeV1RUMEnabled"), () => CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).CafeV1RUM.Enabled);

		// Token: 0x04000176 RID: 374
		public static readonly StringAppSettingsEntry EnableRpsTokenBEAuthVersion = new StringAppSettingsEntry("ConsumerEasAuthModule.EnableBEAuthVersion", string.Empty, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000177 RID: 375
		public static readonly IntAppSettingsEntry CompressTokenMinimumSize = new IntAppSettingsEntry(HttpProxySettings.Prefix("CompressTokenMinimumSize"), 8192, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000178 RID: 376
		public static readonly FlightableBoolAppSettingsEntry PuidAnchorMailboxEnabled = new FlightableBoolAppSettingsEntry(HttpProxySettings.Prefix("PuidAnchorMailboxEnabled"), () => CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).PuidAnchorMailboxEnabled.Enabled);

		// Token: 0x04000179 RID: 377
		public static readonly FlightableBoolAppSettingsEntry AddHostHeaderInServerRequestEnabled = new FlightableBoolAppSettingsEntry(HttpProxySettings.Prefix("AddHostHeaderInServerRequestEnabled"), () => CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).AddHostHeaderInServerRequest.Enabled);
	}
}
