using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Collections.TimeoutCache;
using Microsoft.Exchange.Common;
using Microsoft.Exchange.Conversion;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.ExchangeSystem;
using Microsoft.Exchange.Extensions;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.HttpProxy.EventLogs;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.Net.Protocols;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;
using Microsoft.Web.Administration;
using Microsoft.Win32;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000063 RID: 99
	public class FbaModule : ProxyModule
	{
		// Token: 0x0600033C RID: 828 RVA: 0x00011336 File Offset: 0x0000F536
		internal FbaModule()
		{
		}

		// Token: 0x170000BB RID: 187
		// (get) Token: 0x0600033D RID: 829 RVA: 0x0001133E File Offset: 0x0000F53E
		// (set) Token: 0x0600033E RID: 830 RVA: 0x00011345 File Offset: 0x0000F545
		private static ExactTimeoutCache<string, byte[]> KeyCache { get; set; } = new ExactTimeoutCache<string, byte[]>(delegate(string k, byte[] v, RemoveReason r)
		{
			FbaModule.UpdateCacheSizeCounter();
		}, null, null, FbaModule.FbaKeyCacheSizeLimit.Value, false);

		// Token: 0x0600033F RID: 831 RVA: 0x00011350 File Offset: 0x0000F550
		internal static bool IsCadataCookie(string cookieName)
		{
			return string.Compare(cookieName, "cadata", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(cookieName, "cadataKey", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(cookieName, "cadataIV", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(cookieName, "cadataSig", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(cookieName, "cadataTTL", StringComparison.OrdinalIgnoreCase) == 0;
		}

		// Token: 0x06000340 RID: 832 RVA: 0x000113A8 File Offset: 0x0000F5A8
		internal static void InvalidateKeyCache(HttpRequest httpRequest)
		{
			if (httpRequest == null)
			{
				throw new ArgumentNullException("httpRequest");
			}
			foreach (string text in FbaModule.KeyCacheCookieKeys)
			{
				string text2 = (httpRequest.Cookies[text] != null) ? httpRequest.Cookies[text].Value : null;
				if (!string.IsNullOrEmpty(text2))
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<string, string>(0L, "[FbaModule::InvalidateKeyCache] Removing key cache entry {0}: {1}", text, text2);
					}
					FbaModule.KeyCache.Remove(text2);
				}
			}
			FbaModule.UpdateCacheSizeCounter();
		}

		// Token: 0x06000341 RID: 833 RVA: 0x00011438 File Offset: 0x0000F638
		internal static void SetCadataCookies(HttpApplication httpApplication)
		{
			HttpContext context = httpApplication.Context;
			HttpRequest request = context.Request;
			HttpResponse response = context.Response;
			byte[] rgb = null;
			byte[] rgb2 = null;
			string s = context.Items["Authorization"] as string;
			int num = (int)context.Items["flags"];
			HttpCookieCollection cookies = request.Cookies;
			using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider())
			{
				aesCryptoServiceProvider.GenerateKey();
				aesCryptoServiceProvider.GenerateIV();
				rgb = aesCryptoServiceProvider.Key;
				rgb2 = aesCryptoServiceProvider.IV;
				using (ICryptoTransform cryptoTransform = aesCryptoServiceProvider.CreateEncryptor())
				{
					byte[] bytes = Encoding.Unicode.GetBytes(s);
					byte[] inArray = cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);
					FbaModule.CreateAndAddCookieToResponse(request, response, "cadata", Convert.ToBase64String(inArray));
				}
				FbaModule.SetCadataTtlCookie(aesCryptoServiceProvider, num, request, response);
			}
			List<X509Certificate2> list = FbaModule.SafeGetCertificates();
			RSACryptoServiceProvider rsacryptoServiceProvider;
			if (CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).OwaFbaAuthconfigCert.Enabled && !CollectionExtensions.IsNullOrEmpty(list))
			{
				rsacryptoServiceProvider = (list[0].PublicKey.Key as RSACryptoServiceProvider);
			}
			else
			{
				rsacryptoServiceProvider = (FbaModule.GetSslCertificate(request).PublicKey.Key as RSACryptoServiceProvider);
			}
			byte[] inArray2 = rsacryptoServiceProvider.Encrypt(rgb, true);
			byte[] inArray3 = rsacryptoServiceProvider.Encrypt(rgb2, true);
			FbaModule.CreateAndAddCookieToResponse(request, response, "cadataKey", Convert.ToBase64String(inArray2));
			FbaModule.CreateAndAddCookieToResponse(request, response, "cadataIV", Convert.ToBase64String(inArray3));
			byte[] bytes2 = Encoding.Unicode.GetBytes("Fba Rocks!");
			byte[] inArray4 = rsacryptoServiceProvider.Encrypt(bytes2, true);
			FbaModule.CreateAndAddCookieToResponse(request, response, "cadataSig", Convert.ToBase64String(inArray4));
		}

		// Token: 0x06000342 RID: 834 RVA: 0x000115FC File Offset: 0x0000F7FC
		protected override void OnBeginRequestInternal(HttpApplication httpApplication)
		{
			this.basicAuthString = null;
			this.destinationUrl = null;
			this.userName = null;
			this.cadataKeyString = null;
			this.cadataIVString = null;
			this.symKey = null;
			this.symIV = null;
			this.flags = 0;
			this.password = null;
			httpApplication.Context.Items["AuthType"] = "FBA";
			if (!this.HandleFbaAuthFormPost(httpApplication))
			{
				try
				{
					this.ParseCadataCookies(httpApplication);
				}
				catch (MissingSslCertificateException)
				{
					NameValueCollection nameValueCollection = new NameValueCollection();
					nameValueCollection.Add("CafeError", ErrorFE.FEErrorCodes.SSLCertificateProblem.ToString());
					throw new HttpException(302, AspNetHelper.GetCafeErrorPageRedirectUrl(httpApplication.Context, nameValueCollection));
				}
			}
			base.OnBeginRequestInternal(httpApplication);
		}

		// Token: 0x06000343 RID: 835 RVA: 0x000116C4 File Offset: 0x0000F8C4
		protected override void OnPostAuthorizeInternal(HttpApplication httpApplication)
		{
			if (this.basicAuthString != null)
			{
				HttpContext context = httpApplication.Context;
				HttpRequest request = context.Request;
				context.Items.Add("destination", this.destinationUrl);
				context.Items.Add("flags", this.flags);
				context.Items.Add("Authorization", this.basicAuthString);
				context.Items.Add("username", this.userName);
				context.Items.Add("password", this.password);
				ProxyRequestHandler proxyRequestHandler = new FbaFormPostProxyRequestHandler();
				PerfCounters.HttpProxyCountersInstance.TotalRequests.Increment();
				proxyRequestHandler.Run(context);
				return;
			}
			if (this.cadataKeyString != null && this.cadataIVString != null && this.symKey != null && this.symIV != null)
			{
				FbaModule.KeyCache.TryInsertSliding(this.cadataKeyString, this.symKey, TimeSpan.FromMinutes((double)FbaModule.DefaultPrivateKeyTimeToLiveInMinutes));
				FbaModule.KeyCache.TryInsertSliding(this.cadataIVString, this.symIV, TimeSpan.FromMinutes((double)FbaModule.DefaultPrivateKeyTimeToLiveInMinutes));
				FbaModule.UpdateCacheSizeCounter();
			}
			base.OnPostAuthorizeInternal(httpApplication);
		}

		// Token: 0x06000344 RID: 836 RVA: 0x000117EC File Offset: 0x0000F9EC
		protected override void OnEndRequestInternal(HttpApplication httpApplication)
		{
			HttpRequest request = httpApplication.Context.Request;
			HttpResponse response = httpApplication.Context.Response;
			RequestDetailsLogger current = RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpApplication.Context);
			if (httpApplication.Context.Items[Constants.RequestCompletedHttpContextKeyName] == null && !UrlUtilities.IsIntegratedAuthUrl(request.Url) && !UrlUtilities.IsOwaMiniUrl(request.Url) && (response.StatusCode == 401 || (HttpProxyGlobals.ProtocolType == 1 && (response.StatusCode == 403 || response.StatusCode == 404))))
			{
				FbaModule.LogonReason reason = FbaModule.LogonReason.None;
				if (request.Headers["Authorization"] != null)
				{
					reason = FbaModule.LogonReason.InvalidCredentials;
				}
				bool flag = request.Url.AbsolutePath.Equals("/owa/auth.owa", StringComparison.OrdinalIgnoreCase);
				if (request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) || flag)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(current, "NoCookies", "302 - GET/E14AuthPost");
					httpApplication.Response.AppendToLog("&LogoffReason=NoCookiesGetOrE14AuthPost");
					this.RedirectToFbaLogon(httpApplication, reason);
				}
				else if (request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(current, "NoCookies", "440 - POST");
					httpApplication.Response.AppendToLog("&LogoffReason=NoCookiesPost");
					this.Send440Response(httpApplication, true);
				}
				else
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(current, "NoCookies", "440 - " + request.HttpMethod);
					httpApplication.Response.AppendToLog("&LogoffReason=NoCookies" + request.HttpMethod);
					this.Send440Response(httpApplication, false);
				}
			}
			base.OnEndRequestInternal(httpApplication);
		}

		// Token: 0x06000345 RID: 837 RVA: 0x00011980 File Offset: 0x0000FB80
		private static void DetermineKeyIntervalsIfNecessary()
		{
			if (FbaModule.haveDeterminedKeyIntervals)
			{
				return;
			}
			object lockObject = FbaModule.LockObject;
			lock (lockObject)
			{
				if (!FbaModule.haveDeterminedKeyIntervals)
				{
					try
					{
						using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\MSExchange OWA", false))
						{
							if (registryKey == null)
							{
								if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
								{
									ExTraceGlobals.VerboseTracer.TraceError(0L, "[FbaModule::DetermineKeyIntervalsIfNecessary] Error opening reg key to retrieve registry value.");
								}
								return;
							}
							object value = registryKey.GetValue("PrivateTimeout");
							if (value != null && value is int)
							{
								int num = (int)value;
								if (num >= 1 && num <= 43200)
								{
									FbaModule.fbaPrivateKeyTTL = new TimeSpan(0, 0, (FbaModule.TtlReissueDivisor + 1) * (num * 60 / FbaModule.TtlReissueDivisor));
									FbaModule.fbaPrivateKeyReissueInterval = new TimeSpan(0, 0, num * 60 / FbaModule.TtlReissueDivisor);
								}
							}
							object value2 = registryKey.GetValue("PublicTimeout");
							if (value2 != null && value2 is int)
							{
								int num2 = (int)value2;
								if (num2 >= 1 && num2 <= 43200)
								{
									FbaModule.fbaPublicKeyTTL = new TimeSpan(0, 0, (FbaModule.TtlReissueDivisor + 1) * (num2 * 60 / FbaModule.TtlReissueDivisor));
									FbaModule.fbaPublicKeyReissueInterval = new TimeSpan(0, 0, num2 * 60 / FbaModule.TtlReissueDivisor);
								}
							}
							object value3 = registryKey.GetValue("MowaTimeout");
							if (value3 != null && value3 is int)
							{
								int num3 = (int)value3;
								if (num3 >= 1 && num3 <= 43200)
								{
									FbaModule.fbaMowaKeyTTL = new TimeSpan(0, 0, (FbaModule.TtlReissueDivisor + 1) * (num3 * 60 / FbaModule.TtlReissueDivisor));
									FbaModule.fbaMowaKeyReissueInterval = new TimeSpan(0, 0, num3 * 60 / FbaModule.TtlReissueDivisor);
								}
							}
							if (FbaModule.fbaPublicKeyTTL > FbaModule.fbaPrivateKeyTTL)
							{
								FbaModule.fbaPrivateKeyTTL = FbaModule.fbaPublicKeyTTL;
								FbaModule.fbaPrivateKeyReissueInterval = FbaModule.fbaPublicKeyReissueInterval;
							}
							if (FbaModule.fbaPrivateKeyTTL > FbaModule.fbaMowaKeyTTL)
							{
								FbaModule.fbaMowaKeyTTL = FbaModule.fbaPrivateKeyTTL;
								FbaModule.fbaMowaKeyReissueInterval = FbaModule.fbaPrivateKeyReissueInterval;
							}
						}
						FbaModule.haveDeterminedKeyIntervals = true;
					}
					catch (SecurityException)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
						{
							ExTraceGlobals.VerboseTracer.TraceError(0L, "[FbaModule::DetermineKeyIntervalsIfNecessary] Security exception encountered while retrieving registry value.");
						}
					}
					catch (UnauthorizedAccessException)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
						{
							ExTraceGlobals.VerboseTracer.TraceError(0L, "[FbaModule::DetermineKeyIntervalsIfNecessary] Unauthorized exception encountered while retrieving registry value.");
						}
					}
				}
			}
		}

		// Token: 0x06000346 RID: 838 RVA: 0x00011C28 File Offset: 0x0000FE28
		private static void SetCadataTtlCookie(AesCryptoServiceProvider aes, int flags, HttpRequest httpRequest, HttpResponse httpResponse)
		{
			using (ICryptoTransform cryptoTransform = aes.CreateEncryptor())
			{
				FbaModule.DetermineKeyIntervalsIfNecessary();
				bool flag = (flags & 4) == 4;
				bool flag2 = FbaModule.IsMowa(httpRequest, flag);
				ExDateTime exDateTime = ExDateTime.UtcNow.AddTicks(flag2 ? FbaModule.fbaMowaKeyTTL.Ticks : (flag ? FbaModule.fbaPrivateKeyTTL.Ticks : FbaModule.fbaPublicKeyTTL.Ticks));
				byte[] array = new byte[9];
				ExBitConverter.Write(exDateTime.UtcTicks, array, 0);
				array[8] = (byte)flags;
				byte[] inArray = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
				FbaModule.CreateAndAddCookieToResponse(httpRequest, httpResponse, "cadataTTL", Convert.ToBase64String(inArray));
			}
		}

		// Token: 0x06000347 RID: 839 RVA: 0x00011CE4 File Offset: 0x0000FEE4
		private static bool IsMowa(HttpRequest request, bool isTrusted)
		{
			return isTrusted && request.Headers["X-OWA-Protocol"] == "MOWA";
		}

		// Token: 0x06000348 RID: 840 RVA: 0x00011D08 File Offset: 0x0000FF08
		private static void CreateAndAddCookieToResponse(HttpRequest request, HttpResponse response, string name, string value)
		{
			HttpCookie httpCookie = new HttpCookie(name, value);
			httpCookie.HttpOnly = true;
			httpCookie.Secure = request.IsSecureConnection;
			if (!string.IsNullOrEmpty(request.UserAgent) && new UserAgent(request.UserAgent, request.Cookies).DoesSupportSameSiteNone())
			{
				httpCookie.Path = "/;SameSite=None";
			}
			response.Cookies.Add(httpCookie);
		}

		// Token: 0x06000349 RID: 841 RVA: 0x00011D6C File Offset: 0x0000FF6C
		private static void UpdateCacheSizeCounter()
		{
			PerfCounters.HttpProxyCacheCountersInstance.FbaModuleKeyCacheSize.RawValue = (long)FbaModule.KeyCache.Count;
		}

		// Token: 0x0600034A RID: 842 RVA: 0x00011D88 File Offset: 0x0000FF88
		private static string GetExternalUrlScheme(ref int port)
		{
			if (port == 80)
			{
				port = 443;
			}
			return Uri.UriSchemeHttps;
		}

		// Token: 0x0600034B RID: 843 RVA: 0x00011D9C File Offset: 0x0000FF9C
		private static X509Certificate2 GetSslCertificate(HttpRequest httpRequest)
		{
			if (!FbaModule.loadedSslCert)
			{
				object lockObject = FbaModule.LockObject;
				lock (lockObject)
				{
					if (!FbaModule.loadedSslCert)
					{
						X509Certificate2 x509Certificate = FbaModule.LoadSslCertificate(httpRequest);
						if (x509Certificate == null)
						{
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
							{
								ExTraceGlobals.VerboseTracer.TraceError(0L, "[FbaModule::GetSslCertificate] LoadSslCertificate returns null.");
							}
							Diagnostics.Logger.LogEvent(FrontEndHttpProxyEventLogConstants.Tuple_ErrorLoadingSslCert, null, new object[]
							{
								HttpProxyGlobals.ProtocolType.ToString()
							});
						}
						FbaModule.sslCert = x509Certificate;
						FbaModule.loadedSslCert = true;
					}
				}
			}
			if (FbaModule.sslCert == null)
			{
				throw new MissingSslCertificateException();
			}
			return FbaModule.sslCert;
		}

		// Token: 0x0600034C RID: 844 RVA: 0x00011E5C File Offset: 0x0001005C
		private static X509Certificate2 LoadSslCertificate(HttpRequest httpRequest)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug(0L, "[FbaModule::LoadSslCertificate] Loading SSL certificate.");
			}
			string text = httpRequest.ServerVariables["INSTANCE_ID"];
			if (string.IsNullOrEmpty(text))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError(0L, "[FbaModule::LoadSslCertificate] INSTANCE_ID server variable was returned as null or empty!");
				}
				return null;
			}
			int num;
			if (!int.TryParse(text, out num))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string>(0L, "[FbaModule::LoadSslCertificate] Could not parse instance id {0}", text);
				}
				return null;
			}
			byte[] array = null;
			string text2 = null;
			using (ServerManager serverManager = new ServerManager())
			{
				Site site = null;
				foreach (Site site2 in serverManager.Sites)
				{
					if (site2.Id == (long)num)
					{
						site = site2;
						break;
					}
				}
				if (site == null)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError<int>(0L, "[FbaModule::LoadSslCertificate] Could not find site with id {0}", num);
					}
					return null;
				}
				foreach (Binding binding in site.Bindings)
				{
					if (binding.Protocol == "https")
					{
						array = binding.CertificateHash;
						text2 = binding.CertificateStoreName;
						if (array != null && text2 != null)
						{
							break;
						}
					}
				}
				if (array == null || text2 == null)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError(0L, "[FbaModule::LoadSslCertificate] Could not find certificate information in bindings");
					}
					return null;
				}
			}
			X509Certificate2 x509Certificate = null;
			X509Store x509Store = new X509Store(text2, StoreLocation.LocalMachine);
			x509Store.Open(OpenFlags.OpenExistingOnly);
			try
			{
				foreach (X509Certificate2 x509Certificate2 in x509Store.Certificates)
				{
					byte[] certHash = x509Certificate2.GetCertHash();
					if (certHash.Length == array.Length)
					{
						bool flag = true;
						for (int i = 0; i < certHash.Length; i++)
						{
							if (certHash[i] != array[i])
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							x509Certificate = x509Certificate2;
							break;
						}
					}
				}
			}
			finally
			{
				x509Store.Close();
			}
			if (x509Certificate == null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError(0L, "[FbaModule::LoadSslCertificate] Could not find SSL certificate in store.");
				}
				return null;
			}
			return x509Certificate;
		}

		// Token: 0x0600034D RID: 845 RVA: 0x000120DC File Offset: 0x000102DC
		private static List<X509Certificate2> SafeGetCertificates()
		{
			List<X509Certificate2> result;
			try
			{
				result = Utility.GetCertificates().ToList<X509Certificate2>();
			}
			catch (AdfsConfigurationException)
			{
				result = new List<X509Certificate2>();
				ExTraceGlobals.VerboseTracer.TraceDebug(0L, "There is no cert configured in auth config, fallback to use SSL cert");
			}
			return result;
		}

		// Token: 0x0600034E RID: 846 RVA: 0x00012124 File Offset: 0x00010324
		private bool RedirectToFbaLogon(HttpApplication httpApplication, FbaModule.LogonReason reason)
		{
			HttpContext context = httpApplication.Context;
			HttpRequest request = context.Request;
			HttpResponse response = context.Response;
			Utility.DeleteFbaAuthCookies(request, response, false);
			UriBuilder uriBuilder = new UriBuilder();
			uriBuilder.Host = request.Url.Host;
			int port = uriBuilder.Port;
			uriBuilder.Scheme = FbaModule.GetExternalUrlScheme(ref port);
			uriBuilder.Port = port;
			uriBuilder.Path = "/owa/auth/logon.aspx";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("url=");
			if (this.destinationUrl != null)
			{
				stringBuilder.Append(HttpUtility.UrlEncode(new UriBuilder(this.destinationUrl)
				{
					Scheme = uriBuilder.Scheme,
					Port = uriBuilder.Port
				}.Uri.AbsoluteUri.ToString()));
			}
			else
			{
				string text = new UriBuilder(Extensions.GetFullRawUrl(SharedHttpContextWrapper.GetWrapper(context).Request))
				{
					Scheme = uriBuilder.Scheme,
					Port = uriBuilder.Port
				}.Uri.AbsoluteUri;
				string strB = request.Url.Segments[request.Url.Segments.Length - 1];
				if (string.Compare("auth.owa", strB, StringComparison.OrdinalIgnoreCase) == 0)
				{
					int startIndex = text.LastIndexOf("auth.owa") - 1;
					text = text.Remove(startIndex);
				}
				string text2 = HttpUtility.UrlDecode(request.Headers[OwaHttpHeader.ExplicitLogonUser]);
				if (!string.IsNullOrEmpty(text2) && !text.Contains(text2))
				{
					string value = HttpUtility.UrlEncode("/");
					string applicationPath = request.ApplicationPath;
					int num = text.IndexOf(applicationPath, StringComparison.OrdinalIgnoreCase);
					if (num == -1)
					{
						stringBuilder.Append(HttpUtility.UrlEncode(text));
						if (text[text.Length - 1] != '/')
						{
							stringBuilder.Append(value);
						}
						stringBuilder.Append(HttpUtility.UrlEncode(text2));
						stringBuilder.Append(value);
					}
					else
					{
						num += applicationPath.Length;
						if (num < text.Length && text[num] == '/')
						{
							num++;
						}
						stringBuilder.Append(HttpUtility.UrlEncode(text.Substring(0, num)));
						if (text[num - 1] != '/')
						{
							stringBuilder.Append(value);
						}
						stringBuilder.Append(HttpUtility.UrlEncode(text2));
						stringBuilder.Append(value);
						stringBuilder.Append(HttpUtility.UrlEncode(text.Substring(num)));
					}
				}
				else
				{
					int num2 = text.IndexOf('?');
					string text3 = null;
					if (text.ToLowerInvariant().Contains("logoff.owa"))
					{
						if (!LogOnSettings.IsLegacyLogOff)
						{
							uriBuilder.Path = "/owa/" + LogOnSettings.SignOutPageUrl;
						}
						if (num2 >= 0)
						{
							string text4 = text.Substring(num2 + 1).Split(new char[]
							{
								'&'
							}).FirstOrDefault((string x) => x.StartsWith("url=", StringComparison.OrdinalIgnoreCase));
							if (text4 != null)
							{
								text3 = text4.Substring("url=".Length);
							}
						}
					}
					if (text3 == null)
					{
						string str;
						text3 = ((!UrlUtilities.IsCmdWebPart(request) && UrlUtilities.ShouldRedirectQueryParamsAsHashes(new Uri(text), UrlUtilities.ImportantQueryParamNames, ref str)) ? HttpUtility.UrlEncode(str) : HttpUtility.UrlEncode(text));
					}
					stringBuilder.Append(text3);
				}
			}
			stringBuilder.AppendFormat("&reason={0}", (int)reason);
			uriBuilder.Query = stringBuilder.ToString();
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<FbaModule.LogonReason, string>((long)this.GetHashCode(), "RedirectToFbaLogon - Reason: {0}, URL: {1}", reason, uriBuilder.ToString());
			}
			base.PfdTracer.TraceRedirect("FbaAuth", uriBuilder.ToString());
			response.Redirect(uriBuilder.ToString(), false);
			httpApplication.CompleteRequest();
			return true;
		}

		// Token: 0x0600034F RID: 847 RVA: 0x000124F0 File Offset: 0x000106F0
		private void Send440Response(HttpApplication httpApplication, bool isPost)
		{
			HttpRequest request = httpApplication.Context.Request;
			HttpResponse response = httpApplication.Context.Response;
			response.StatusCode = 440;
			response.StatusDescription = "Login Timeout";
			Utility.DeleteFbaAuthCookies(request, response, false);
			response.ContentType = "text/html";
			response.Headers["Connection"] = "close";
			if (isPost)
			{
				response.Output.Write("<HTML><SCRIPT>if (parent.navbar != null) parent.location = self.location;else self.location = self.location;</SCRIPT><BODY>440 Login Timeout</BODY></HTML>");
			}
			else
			{
				response.Output.Write("<HTML><BODY>440 Login Timeout</BODY></HTML>");
			}
			base.PfdTracer.TraceResponse("FbaAuth", response);
			httpApplication.CompleteRequest();
		}

		// Token: 0x06000350 RID: 848 RVA: 0x00012590 File Offset: 0x00010790
		private bool HandleFbaAuthFormPost(HttpApplication httpApplication)
		{
			HttpContext context = httpApplication.Context;
			HttpRequest request = context.Request;
			HttpResponse response = context.Response;
			if (request.GetHttpMethod() != HttpMethod.Post)
			{
				return false;
			}
			string strB = request.Url.Segments[request.Url.Segments.Length - 1];
			if (string.Compare("auth.owa", strB, StringComparison.OrdinalIgnoreCase) != 0 && string.Compare("owaauth.dll", strB, StringComparison.OrdinalIgnoreCase) != 0)
			{
				return false;
			}
			if (string.IsNullOrEmpty(request.ContentType))
			{
				request.ContentType = "application/x-www-form-urlencoded";
			}
			SecureHtmlFormReader secureHtmlFormReader = new SecureHtmlFormReader(request);
			secureHtmlFormReader.AddSensitiveInputName("password");
			SecureNameValueCollection secureNameValueCollection = null;
			try
			{
				if (!secureHtmlFormReader.TryReadSecureFormData(out secureNameValueCollection))
				{
					AspNetHelper.EndResponse(context, HttpStatusCode.BadRequest);
				}
				string text = null;
				string text2 = null;
				SecureString secureString = null;
				string text3 = null;
				secureNameValueCollection.TryGetUnsecureValue("username", out text2);
				secureNameValueCollection.TryGetSecureValue("password", out secureString);
				secureNameValueCollection.TryGetUnsecureValue("destination", out text);
				secureNameValueCollection.TryGetUnsecureValue("flags", out text3);
				if (text == null || text2 == null || secureString == null || text3 == null || !this.CheckPostDestination(text, context.Request))
				{
					AspNetHelper.EndResponse(context, HttpStatusCode.BadRequest);
				}
				this.password = secureString.Copy();
				this.userName = text2;
				this.destinationUrl = text;
				int num;
				if (int.TryParse(text3, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
				{
					this.flags = num;
				}
				else
				{
					this.flags = 0;
				}
				text2 += ":";
				Encoding @default = Encoding.Default;
				using (SecureArray<byte> secureArray = new SecureArray<byte>(@default.GetMaxByteCount(text2.Length + secureString.Length)))
				{
					int num2 = @default.GetBytes(text2, 0, text2.Length, secureArray.ArrayValue, 0);
					using (SecureArray<char> secureArray2 = SecureStringExtensions.ConvertToSecureCharArray(secureString))
					{
						num2 += @default.GetBytes(secureArray2.ArrayValue, 0, secureArray2.Length(), secureArray.ArrayValue, num2);
						this.basicAuthString = "Basic " + Convert.ToBase64String(secureArray.ArrayValue, 0, num2);
						request.Headers["Authorization"] = this.basicAuthString;
					}
				}
			}
			finally
			{
				if (secureNameValueCollection != null)
				{
					secureNameValueCollection.Dispose();
				}
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<Uri>(0L, "HandleFbaAuthFormPost - {0}", request.Url);
			}
			return true;
		}

		// Token: 0x06000351 RID: 849 RVA: 0x00012834 File Offset: 0x00010A34
		private void ParseCadataCookies(HttpApplication httpApplication)
		{
			HttpContext context = httpApplication.Context;
			HttpRequest request = context.Request;
			HttpResponse response = context.Response;
			RequestDetailsLogger current = RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(context);
			string text = null;
			if (request.Cookies["cadata"] != null && request.Cookies["cadata"].Value != null)
			{
				text = request.Cookies["cadata"].Value;
			}
			string text2 = null;
			if (request.Cookies["cadataKey"] != null && request.Cookies["cadataKey"].Value != null)
			{
				text2 = request.Cookies["cadataKey"].Value;
			}
			string text3 = null;
			if (request.Cookies["cadataIV"] != null && request.Cookies["cadataIV"].Value != null)
			{
				text3 = request.Cookies["cadataIV"].Value;
			}
			string text4 = null;
			if (request.Cookies["cadataSig"] != null && request.Cookies["cadataSig"].Value != null)
			{
				text4 = request.Cookies["cadataSig"].Value;
			}
			string text5 = null;
			if (request.Cookies["cadataTTL"] != null && request.Cookies["cadataTTL"].Value != null)
			{
				text5 = request.Cookies["cadataTTL"].Value;
			}
			if (text == null || text2 == null || text3 == null || text4 == null || text5 == null)
			{
				return;
			}
			byte[] array = null;
			byte[] array2 = null;
			PerfCounters.HttpProxyCacheCountersInstance.FbaModuleKeyCacheHitsRateBase.Increment();
			FbaModule.KeyCache.TryGetValue(text2, ref array);
			FbaModule.KeyCache.TryGetValue(text3, ref array2);
			if (array != null && array2 != null)
			{
				PerfCounters.HttpProxyCacheCountersInstance.FbaModuleKeyCacheHitsRate.Increment();
			}
			else
			{
				string text6 = null;
				RSACryptoServiceProvider rsacryptoServiceProvider = null;
				List<X509Certificate2> list = FbaModule.SafeGetCertificates();
				X509Certificate2 sslCertificate = FbaModule.GetSslCertificate(request);
				if (CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).OwaFbaAuthconfigCert.Enabled)
				{
					list.Add(sslCertificate);
				}
				else
				{
					list.Insert(0, sslCertificate);
				}
				foreach (X509Certificate2 x509Certificate in list)
				{
					try
					{
						rsacryptoServiceProvider = (x509Certificate.PrivateKey as RSACryptoServiceProvider);
						if (rsacryptoServiceProvider != null)
						{
							byte[] rgb = Convert.FromBase64String(text4);
							byte[] bytes = rsacryptoServiceProvider.Decrypt(rgb, true);
							if (string.Compare(Encoding.Unicode.GetString(bytes), "Fba Rocks!", StringComparison.Ordinal) == 0)
							{
								text6 = null;
								break;
							}
							text6 = "does not match the SSL certificate on the Cafe web-site on another server in this Cafe array";
						}
						else
						{
							text6 = "does not contain RSACryptoServiceProvider";
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
							{
								ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] Certificate:{0},Name:{1},Thumbprint:{2},PrivateKeyKey.(Exchange/Signature)Algorighm:{3} has no RSACryptoServiceProvider", new object[]
								{
									x509Certificate.Subject,
									x509Certificate.FriendlyName,
									x509Certificate.Thumbprint,
									(x509Certificate.PrivateKey == null) ? "NULL" : (x509Certificate.PrivateKey.KeyExchangeAlgorithm + "/" + x509Certificate.PrivateKey.SignatureAlgorithm)
								});
							}
						}
					}
					catch (CryptographicException ex)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<CryptographicException>((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] Received CryptographicException {0} decrypting cadataSig", ex);
						}
					}
					catch (InvalidOperationException ex2)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<InvalidOperationException>((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] Received InvalidOperationException {0} decrypting cadataSig", ex2);
						}
					}
					catch (FormatException ex3)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<FormatException>((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] Received FormatException {0} decoding cadataSig", ex3);
						}
						httpApplication.Response.AppendToLog("&DecodeError=InvalidCaDataSignature");
						return;
					}
				}
				if (text6 != null)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError<string, string>((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] {0} {1}", "Error in validating Cadata signature. This most likely indicates that the SSL certifcate on the Cafe web-site on this server ", text6);
					}
					return;
				}
				try
				{
					byte[] rgb2 = Convert.FromBase64String(text2);
					byte[] rgb3 = Convert.FromBase64String(text3);
					array = rsacryptoServiceProvider.Decrypt(rgb2, true);
					array2 = rsacryptoServiceProvider.Decrypt(rgb3, true);
				}
				catch (CryptographicException ex4)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<CryptographicException>((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] Received CryptographicException {0} decrypting symKey/symIV", ex4);
					}
					httpApplication.Response.AppendToLog("&CryptoError=PossibleSSLCertrolloverMismatch");
					return;
				}
				catch (FormatException ex5)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<FormatException>((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] Received FormatException {0} decoding symKey/symIV", ex5);
					}
					httpApplication.Response.AppendToLog("&DecodeError=InvalidKeyOrIV");
					return;
				}
				this.cadataKeyString = text2;
				this.cadataIVString = text3;
				this.symKey = array;
				this.symIV = array2;
			}
			using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider())
			{
				aesCryptoServiceProvider.Key = array;
				aesCryptoServiceProvider.IV = array2;
				using (ICryptoTransform cryptoTransform = aesCryptoServiceProvider.CreateDecryptor())
				{
					byte[] array3 = null;
					try
					{
						byte[] array4 = Convert.FromBase64String(text5);
						array3 = cryptoTransform.TransformFinalBlock(array4, 0, array4.Length);
					}
					catch (CryptographicException ex6)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<CryptographicException>((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] Received CryptographicException {0} transforming TTL", ex6);
						}
						httpApplication.Response.AppendToLog("&CryptoError=PossibleSSLCertrolloverMismatch");
						return;
					}
					catch (FormatException ex7)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<FormatException>((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] Received FormatException {0} decoding TTL", ex7);
						}
						httpApplication.Response.AppendToLog("&DecodeError=InvalidTTL");
						return;
					}
					if (array3.Length < 1)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] TTL length was less than 1.");
						}
						return;
					}
					long num = BitConverter.ToInt64(array3, 0);
					if (num < DateTime.MinValue.Ticks || num > DateTime.MaxValue.Ticks)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] TTL value is out of range.");
						}
						return;
					}
					int num2 = (int)array3[8];
					bool flag = (num2 & 4) == 4;
					context.Items["Flags"] = num2;
					ExDateTime exDateTime;
					exDateTime..ctor(ExTimeZone.UtcTimeZone, num);
					ExDateTime utcNow = ExDateTime.UtcNow;
					if (exDateTime < utcNow)
					{
						if (request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
						{
							if (request.QueryString.ToString().StartsWith("oeh=1&", StringComparison.OrdinalIgnoreCase))
							{
								RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(current, "LoginTimeout", "440 - GET/OEH");
								httpApplication.Response.AppendToLog("&LogoffReason=LoginTimeoutOEH");
								this.Send440Response(httpApplication, false);
								return;
							}
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(current, "LoginTimeout", "302 - GET/Timeout");
							httpApplication.Response.AppendToLog("&LogoffReason=LoginTimeoutGet");
							this.RedirectToFbaLogon(httpApplication, FbaModule.LogonReason.Timeout);
							return;
						}
						else
						{
							if (request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
							{
								RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(current, "LoginTimeout", "440 - POST");
								httpApplication.Response.AppendToLog("&LogoffReason=LoginTimeoutPost");
								this.Send440Response(httpApplication, true);
								return;
							}
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(current, "LoginTimeout", "440 - " + request.HttpMethod);
							httpApplication.Response.AppendToLog("&LogoffReason=LoginTimeoutGet" + request.HttpMethod);
							this.Send440Response(httpApplication, false);
							return;
						}
					}
					else
					{
						FbaModule.DetermineKeyIntervalsIfNecessary();
						if (exDateTime.AddTicks(-2L * (flag ? FbaModule.fbaPrivateKeyReissueInterval.Ticks : FbaModule.fbaPublicKeyReissueInterval.Ticks)) < utcNow && Utility.IsOwaUserActivityRequest(request))
						{
							FbaModule.SetCadataTtlCookie(aesCryptoServiceProvider, num2, request, response);
						}
					}
				}
				using (ICryptoTransform cryptoTransform2 = aesCryptoServiceProvider.CreateDecryptor())
				{
					byte[] bytes2 = null;
					try
					{
						byte[] array5 = Convert.FromBase64String(text);
						bytes2 = cryptoTransform2.TransformFinalBlock(array5, 0, array5.Length);
					}
					catch (CryptographicException ex8)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<CryptographicException>((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] Received CryptographicException {0} transforming auth", ex8);
						}
						httpApplication.Response.AppendToLog("&CryptoError=PossibleSSLCertrolloverMismatch");
						return;
					}
					catch (FormatException ex9)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<FormatException>((long)this.GetHashCode(), "[FbaModule::ParseCadataCookies] Received FormatException {0} decoding caData auth", ex9);
						}
						httpApplication.Response.AppendToLog("&DecodeError=InvalidCaDataAuthCookie");
						return;
					}
					string @string = Encoding.Unicode.GetString(bytes2);
					request.Headers["Authorization"] = @string;
				}
			}
		}

		// Token: 0x06000352 RID: 850 RVA: 0x000131D8 File Offset: 0x000113D8
		private bool CheckPostDestination(string destination, HttpRequest request)
		{
			if (string.IsNullOrEmpty(destination))
			{
				return false;
			}
			if (destination.StartsWith("/", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			try
			{
				if (!string.Equals(new Uri(destination).Host, request.Url.Host, StringComparison.OrdinalIgnoreCase))
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[FbaModule::CheckPostRequestDestination] Destination URL {0} does not match the request host, generating 400 response.", destination);
					}
					return false;
				}
			}
			catch (UriFormatException)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[FbaModule::CheckPostRequestDestination] Destination URL {0} is not a valid URL, generating 400 response.", destination);
				}
				return false;
			}
			return true;
		}

		// Token: 0x040001F3 RID: 499
		internal const string LogoffPage = "/logoff.owa";

		// Token: 0x040001F4 RID: 500
		internal const string FlagsHttpContextKeyName = "Flags";

		// Token: 0x040001F5 RID: 501
		internal const byte IsDownlevelFlagValue = 1;

		// Token: 0x040001F6 RID: 502
		internal const byte IsTrustedFlagValue = 4;

		// Token: 0x040001F7 RID: 503
		internal const string PasswordFormName = "password";

		// Token: 0x040001F8 RID: 504
		internal const string DestinationFormName = "destination";

		// Token: 0x040001F9 RID: 505
		internal const string UsernameFormName = "username";

		// Token: 0x040001FA RID: 506
		internal const string FlagsFormName = "flags";

		// Token: 0x040001FB RID: 507
		internal const string BasicAuthHeader = "Authorization";

		// Token: 0x040001FC RID: 508
		private const string AuthPost = "auth.owa";

		// Token: 0x040001FD RID: 509
		private const string LegacyAuthPost = "owaauth.dll";

		// Token: 0x040001FE RID: 510
		private const string LogonPage = "/auth/logon.aspx";

		// Token: 0x040001FF RID: 511
		private const string LogonPath = "/owa";

		// Token: 0x04000200 RID: 512
		private const string E14OwaAuthPost = "/owa/auth.owa";

		// Token: 0x04000201 RID: 513
		private const string HttpGetMethod = "GET";

		// Token: 0x04000202 RID: 514
		private const string HttpPostMethod = "POST";

		// Token: 0x04000203 RID: 515
		private const string OehParameter = "oeh=1&";

		// Token: 0x04000204 RID: 516
		private const string CadataSig = "Fba Rocks!";

		// Token: 0x04000205 RID: 517
		private const string BasicHeaderValue = "Basic ";

		// Token: 0x04000206 RID: 518
		private const string ResponseBody440 = "<HTML><BODY>440 Login Timeout</BODY></HTML>";

		// Token: 0x04000207 RID: 519
		private const string ResponseBody440Post = "<HTML><SCRIPT>if (parent.navbar != null) parent.location = self.location;else self.location = self.location;</SCRIPT><BODY>440 Login Timeout</BODY></HTML>";

		// Token: 0x04000208 RID: 520
		private const string CommaSpace = ", ";

		// Token: 0x04000209 RID: 521
		private const string FbaPrivateCookieTTLValueName = "PrivateTimeout";

		// Token: 0x0400020A RID: 522
		private const string FbaPublicCookieTTLValueName = "PublicTimeout";

		// Token: 0x0400020B RID: 523
		private const string FbaMowaCookieTTLValueName = "MowaTimeout";

		// Token: 0x0400020C RID: 524
		private const int FbaMinimumTimeout = 1;

		// Token: 0x0400020D RID: 525
		private const int FbaMaximumTimeout = 43200;

		// Token: 0x0400020E RID: 526
		private static readonly int DefaultPrivateKeyTimeToLiveInMinutes = 480;

		// Token: 0x0400020F RID: 527
		private static readonly int DefaultPublicKeyTimeToLiveInMinutes = 15;

		// Token: 0x04000210 RID: 528
		private static readonly int MowaKeyTimeToLiveInMinutes = 960;

		// Token: 0x04000211 RID: 529
		private static readonly int TtlReissueDivisor = 2;

		// Token: 0x04000212 RID: 530
		private static readonly IntAppSettingsEntry FbaKeyCacheSizeLimit = new IntAppSettingsEntry(HttpProxySettings.Prefix("FbaKeyCacheSizeLimit"), 25000, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000213 RID: 531
		private static readonly string[] KeyCacheCookieKeys = new string[]
		{
			"cadataKey",
			"cadataIV"
		};

		// Token: 0x04000214 RID: 532
		private static readonly object LockObject = new object();

		// Token: 0x04000215 RID: 533
		private static TimeSpan fbaPrivateKeyTTL = new TimeSpan(0, (FbaModule.TtlReissueDivisor + 1) * (FbaModule.DefaultPrivateKeyTimeToLiveInMinutes / FbaModule.TtlReissueDivisor), 0);

		// Token: 0x04000216 RID: 534
		private static TimeSpan fbaPrivateKeyReissueInterval = new TimeSpan(0, 0, FbaModule.DefaultPrivateKeyTimeToLiveInMinutes * 60 / FbaModule.TtlReissueDivisor);

		// Token: 0x04000217 RID: 535
		private static TimeSpan fbaPublicKeyTTL = new TimeSpan(0, 0, (FbaModule.TtlReissueDivisor + 1) * (FbaModule.DefaultPublicKeyTimeToLiveInMinutes * 60 / FbaModule.TtlReissueDivisor));

		// Token: 0x04000218 RID: 536
		private static TimeSpan fbaPublicKeyReissueInterval = new TimeSpan(0, 0, FbaModule.DefaultPublicKeyTimeToLiveInMinutes * 60 / FbaModule.TtlReissueDivisor);

		// Token: 0x04000219 RID: 537
		private static TimeSpan fbaMowaKeyTTL = new TimeSpan(0, (FbaModule.TtlReissueDivisor + 1) * (FbaModule.MowaKeyTimeToLiveInMinutes / FbaModule.TtlReissueDivisor), 0);

		// Token: 0x0400021A RID: 538
		private static TimeSpan fbaMowaKeyReissueInterval = new TimeSpan(0, 0, FbaModule.MowaKeyTimeToLiveInMinutes * 60 / FbaModule.TtlReissueDivisor);

		// Token: 0x0400021B RID: 539
		private static bool haveDeterminedKeyIntervals = false;

		// Token: 0x0400021C RID: 540
		private static bool loadedSslCert = false;

		// Token: 0x0400021D RID: 541
		private static X509Certificate2 sslCert;

		// Token: 0x0400021E RID: 542
		private string basicAuthString;

		// Token: 0x0400021F RID: 543
		private string destinationUrl;

		// Token: 0x04000220 RID: 544
		private string userName;

		// Token: 0x04000221 RID: 545
		private SecureString password;

		// Token: 0x04000222 RID: 546
		private string cadataKeyString;

		// Token: 0x04000223 RID: 547
		private string cadataIVString;

		// Token: 0x04000224 RID: 548
		private byte[] symKey;

		// Token: 0x04000225 RID: 549
		private byte[] symIV;

		// Token: 0x04000226 RID: 550
		private int flags;

		// Token: 0x020000FE RID: 254
		protected enum LogonReason
		{
			// Token: 0x040004BF RID: 1215
			None,
			// Token: 0x040004C0 RID: 1216
			Logoff,
			// Token: 0x040004C1 RID: 1217
			InvalidCredentials,
			// Token: 0x040004C2 RID: 1218
			Timeout,
			// Token: 0x040004C3 RID: 1219
			ChangePasswordLogoff
		}
	}
}
