using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.ExchangeSystem;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authentication;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200008C RID: 140
	internal abstract class BEServerCookieProxyRequestHandler<ServiceType> : ProxyRequestHandler where ServiceType : HttpService
	{
		// Token: 0x17000112 RID: 274
		// (get) Token: 0x060004AA RID: 1194 RVA: 0x00019F2F File Offset: 0x0001812F
		// (set) Token: 0x060004AB RID: 1195 RVA: 0x00019F37 File Offset: 0x00018137
		internal string Domain { get; set; }

		// Token: 0x17000113 RID: 275
		// (get) Token: 0x060004AC RID: 1196 RVA: 0x00019F40 File Offset: 0x00018140
		// (set) Token: 0x060004AD RID: 1197 RVA: 0x00019F48 File Offset: 0x00018148
		protected bool IsWsSecurityRequest { get; set; }

		// Token: 0x17000114 RID: 276
		// (get) Token: 0x060004AE RID: 1198 RVA: 0x00019F51 File Offset: 0x00018151
		// (set) Token: 0x060004AF RID: 1199 RVA: 0x00019F59 File Offset: 0x00018159
		protected bool IsDomainBasedRequest { get; set; }

		// Token: 0x17000115 RID: 277
		// (get) Token: 0x060004B0 RID: 1200
		protected abstract ClientAccessType ClientAccessType { get; }

		// Token: 0x17000116 RID: 278
		// (get) Token: 0x060004B1 RID: 1201 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool WillAddProtocolSpecificCookiesToClientResponse
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000117 RID: 279
		// (get) Token: 0x060004B2 RID: 1202 RVA: 0x00019F62 File Offset: 0x00018162
		protected virtual int MaxBackEndCookieEntries
		{
			get
			{
				return 5;
			}
		}

		// Token: 0x17000118 RID: 280
		// (get) Token: 0x060004B3 RID: 1203 RVA: 0x00019F65 File Offset: 0x00018165
		protected virtual string[] BackEndCookieNames
		{
			get
			{
				return BEServerCookieProxyRequestHandler<ServiceType>.ClientSupportedBackEndCookieNames;
			}
		}

		// Token: 0x060004B4 RID: 1204 RVA: 0x00019F6C File Offset: 0x0001816C
		protected override bool ShouldBackendRequestBeAnonymous()
		{
			return this.IsWsSecurityRequest;
		}

		// Token: 0x060004B5 RID: 1205 RVA: 0x00019F74 File Offset: 0x00018174
		protected override BackEndServer GetDownLevelClientAccessServer(AnchorMailbox anchorMailbox, BackEndServer mailboxServer)
		{
			if (mailboxServer.Version < Server.E14MinVersion)
			{
				return this.GetE12TargetServer(mailboxServer);
			}
			Uri uri = null;
			BackEndServer downLevelClientAccessServer = DownLevelServerManager.Instance.GetDownLevelClientAccessServer<ServiceType>(anchorMailbox, mailboxServer, this.ClientAccessType, base.Logger, HttpProxyGlobals.ProtocolType == 4 || HttpProxyGlobals.ProtocolType == 5 || HttpProxyGlobals.ProtocolType == 1, out uri);
			if (uri != null)
			{
				Uri uri2 = this.UpdateExternalRedirectUrl(uri);
				if (Uri.Compare(uri2, base.ClientRequest.Url, UriComponents.Host, UriFormat.Unescaped, StringComparison.Ordinal) != 0)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::GetDownLevelClientAccessServer]: Stop processing and redirect to {0}.", uri2.ToString());
					}
					throw new HttpException(302, uri2.ToString());
				}
			}
			return downLevelClientAccessServer;
		}

		// Token: 0x060004B6 RID: 1206 RVA: 0x0001A02E File Offset: 0x0001822E
		protected override void ResetForRetryOnError()
		{
			this.haveSetBackEndCookie = false;
			this.removeBackEndCookieEntry = false;
			base.ResetForRetryOnError();
		}

		// Token: 0x060004B7 RID: 1207 RVA: 0x0001A044 File Offset: 0x00018244
		protected virtual BackEndServer GetE12TargetServer(BackEndServer mailboxServer)
		{
			return MailboxServerCache.Instance.GetRandomE15Server(this);
		}

		// Token: 0x060004B8 RID: 1208 RVA: 0x0001A051 File Offset: 0x00018251
		protected virtual Uri UpdateExternalRedirectUrl(Uri originalRedirectUrl)
		{
			return originalRedirectUrl;
		}

		// Token: 0x060004B9 RID: 1209 RVA: 0x00003193 File Offset: 0x00001393
		protected virtual bool ShouldExcludeFromExplicitLogonParsing()
		{
			return true;
		}

		// Token: 0x060004BA RID: 1210 RVA: 0x00003193 File Offset: 0x00001393
		protected virtual bool IsValidExplicitLogonNode(string node, bool nodeIsLast)
		{
			return true;
		}

		// Token: 0x060004BB RID: 1211 RVA: 0x0001A054 File Offset: 0x00018254
		protected override bool ShouldCopyCookieToServerRequest(HttpCookie cookie)
		{
			return !FbaModule.IsCadataCookie(cookie.Name) && (base.AuthBehavior.AuthState == AuthState.BackEndFullAuth || (!string.Equals(cookie.Name, Constants.LiveIdRPSAuth, StringComparison.OrdinalIgnoreCase) && !string.Equals(cookie.Name, Constants.LiveIdRPSSecAuth, StringComparison.OrdinalIgnoreCase) && !string.Equals(cookie.Name, Constants.LiveIdRPSTAuth, StringComparison.OrdinalIgnoreCase))) && !this.BackEndCookieNames.Any((string cookieName) => string.Equals(cookie.Name, cookieName, StringComparison.OrdinalIgnoreCase)) && !string.Equals(cookie.Name, Constants.RPSBackEndServerCookieName, StringComparison.OrdinalIgnoreCase) && base.ShouldCopyCookieToServerRequest(cookie);
		}

		// Token: 0x060004BC RID: 1212 RVA: 0x0001A11B File Offset: 0x0001831B
		protected override void CopySupplementalCookiesToClientResponse()
		{
			this.SetBackEndCookie();
			base.CopySupplementalCookiesToClientResponse();
		}

		// Token: 0x060004BD RID: 1213 RVA: 0x0001A12C File Offset: 0x0001832C
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			AnchorMailbox anchorMailbox = null;
			if (!base.HasPreemptivelyCheckedForRoutingHint)
			{
				anchorMailbox = base.CreateAnchorMailboxFromRoutingHint();
			}
			if (anchorMailbox != null)
			{
				return anchorMailbox;
			}
			anchorMailbox = this.TryGetAnchorMailboxFromWsSecurityRequest();
			if (anchorMailbox != null)
			{
				return anchorMailbox;
			}
			anchorMailbox = this.TryGetAnchorMailboxFromDomainBasedRequest();
			if (anchorMailbox != null)
			{
				return anchorMailbox;
			}
			return AnchorMailboxFactory.CreateFromCaller(this);
		}

		// Token: 0x060004BE RID: 1214 RVA: 0x0001A170 File Offset: 0x00018370
		protected override AnchoredRoutingTarget TryFastTargetCalculationByAnchorMailbox(AnchorMailbox anchorMailbox)
		{
			if (this.backEndCookie == null || !base.IsRetryOnErrorEnabled)
			{
				this.FetchBackEndServerCookie();
			}
			PerfCounters.HttpProxyCacheCountersInstance.CookieUseRateBase.Increment();
			PerfCounters.IncrementMovingPercentagePerformanceCounterBase(PerfCounters.HttpProxyCacheCountersInstance.MovingPercentageCookieUseRate);
			if (this.backEndCookie != null)
			{
				BackEndServer backEndServer = anchorMailbox.AcceptBackEndCookie(this.backEndCookie);
				if (backEndServer != null)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailbox, BackEndServer>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::TryFastTargetCalculationByAnchorMailbox]: Back end server {1} resolved from anchor mailbox {0}", anchorMailbox, backEndServer);
					}
					base.Logger.AppendString(3, "-ServerCookie");
					return new AnchoredRoutingTarget(anchorMailbox, backEndServer);
				}
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailbox>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::TryFastTargetCalculationByAnchorMailbox]: No cookie associated with anchor mailbox {0}", anchorMailbox);
			}
			return base.TryFastTargetCalculationByAnchorMailbox(anchorMailbox);
		}

		// Token: 0x060004BF RID: 1215 RVA: 0x0001A238 File Offset: 0x00018438
		protected virtual string TryGetExplicitLogonNode(ExplicitLogonNode node)
		{
			if (this.ShouldExcludeFromExplicitLogonParsing())
			{
				return null;
			}
			string text = null;
			string text2;
			bool nodeIsLast;
			if (ExplicitLogonParser.TryGetExplicitLogonNode(base.ClientRequest.ApplicationPath, base.ClientRequest.FilePath, node, ref text2, ref nodeIsLast) && this.IsValidExplicitLogonNode(text2, nodeIsLast))
			{
				text = text2;
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[OwaEcpProxyRequestHandler::TryGetExplicitLogonNode]: Context {0}; candidate explicit logon node: {1}", base.TraceContext, text);
				}
			}
			return text;
		}

		// Token: 0x060004C0 RID: 1216 RVA: 0x0001A2AC File Offset: 0x000184AC
		protected AnchorMailbox TryGetAnchorMailboxFromWsSecurityRequest()
		{
			if (!this.IsWsSecurityRequest)
			{
				return null;
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::TryGetAnchorMailboxFromWsSecurityRequest]: Context {0}; WSSecurity request.", base.TraceContext);
			}
			WsSecurityParser @object = new WsSecurityParser(base.TraceContext);
			bool flag = false;
			string address;
			if (base.ClientRequest.IsPartnerAuthRequest())
			{
				address = base.ParseClientRequest<string>(new Func<Stream, string>(@object.FindAddressFromPartnerAuthRequest), 73628);
			}
			else if (base.ClientRequest.IsX509CertAuthRequest())
			{
				address = base.ParseClientRequest<string>(new Func<Stream, string>(@object.FindAddressFromX509CertAuthRequest), 73628);
			}
			else
			{
				KeyValuePair<string, bool> keyValuePair = base.ParseClientRequest<KeyValuePair<string, bool>>(new Func<Stream, KeyValuePair<string, bool>>(@object.FindAddressFromWsSecurityRequest), 73628);
				flag = keyValuePair.Value;
				address = keyValuePair.Key;
			}
			if (flag)
			{
				base.Logger.Set(3, "WSSecurityRequest-DelegationToken-Random");
				return new AnonymousAnchorMailbox(this);
			}
			return AnchorMailboxFactory.CreateFromSamlTokenAddress(address, this);
		}

		// Token: 0x060004C1 RID: 1217 RVA: 0x0001A398 File Offset: 0x00018598
		protected AnchorMailbox TryGetAnchorMailboxFromDomainBasedRequest()
		{
			if (!this.IsDomainBasedRequest)
			{
				return null;
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::ResolveAnchorMailbox]: Context {0}; Domain-based request with domain {1}.", base.TraceContext, this.Domain);
			}
			if (!string.IsNullOrEmpty(this.Domain) && SmtpAddress.IsValidDomain(this.Domain))
			{
				base.Logger.Set(3, "DomainBaseRequest-Domain");
				return new DomainAnchorMailbox(this.Domain, this);
			}
			base.Logger.Set(3, "DomainBaseRequest-Random");
			return new AnonymousAnchorMailbox(this);
		}

		// Token: 0x060004C2 RID: 1218 RVA: 0x0001A43C File Offset: 0x0001863C
		protected ServerVersionAnchorMailbox<ServiceType> GetServerVersionAnchorMailbox(string serverVersionString)
		{
			ServerVersion serverVersion = new ServerVersion(LocalServerCache.LocalServer.VersionNumber);
			if (!string.IsNullOrEmpty(serverVersionString))
			{
				Match match = Constants.ExchClientVerRegex.Match(serverVersionString);
				ServerVersion serverVersion2;
				if (match.Success && RegexUtilities.TryGetServerVersionFromRegexMatch(match, ref serverVersion2) && serverVersion2.Major >= 14)
				{
					serverVersion = serverVersion2;
				}
			}
			int num = (serverVersion.Build > 0) ? (serverVersion.Build - 1) : serverVersion.Build;
			serverVersion = new ServerVersion(serverVersion.Major, serverVersion.Minor, num, serverVersion.Revision);
			return new ServerVersionAnchorMailbox<ServiceType>(serverVersion, this.ClientAccessType, false, this);
		}

		// Token: 0x060004C3 RID: 1219 RVA: 0x0001A4CC File Offset: 0x000186CC
		protected override void UpdateOrInvalidateAnchorMailboxCache(Guid mdbGuid, string resourceForest)
		{
			this.removeBackEndCookieEntry = true;
			this.SetBackEndCookie();
			base.UpdateOrInvalidateAnchorMailboxCache(mdbGuid, resourceForest);
		}

		// Token: 0x060004C4 RID: 1220 RVA: 0x0001A4E4 File Offset: 0x000186E4
		protected override void OnDatabaseNotFound(AnchorMailbox anchorMailbox)
		{
			foreach (string text in this.BackEndCookieNames)
			{
				Utility.DeleteCookie(base.ClientRequest, base.ClientResponse, text, this.GetCookiePath(), false, false);
				Utility.DeleteCookie(base.ClientRequest, base.ClientResponse, text, null, true, false);
			}
			base.OnDatabaseNotFound(anchorMailbox);
		}

		// Token: 0x060004C5 RID: 1221 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void SetUseServerCookieFlag(AnchorMailbox anchorMailbox)
		{
		}

		// Token: 0x060004C6 RID: 1222 RVA: 0x0001A540 File Offset: 0x00018740
		private void FetchBackEndServerCookie()
		{
			foreach (string text in this.BackEndCookieNames)
			{
				if (this.ShouldProcessBackEndCookie(text))
				{
					HttpCookie httpCookie = base.ClientRequest.Cookies[text];
					if (httpCookie != null && httpCookie.Values != null)
					{
						this.backEndCookie = httpCookie;
						if (!ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							break;
						}
						StringBuilder stringBuilder = new StringBuilder();
						foreach (string text2 in httpCookie.Values.AllKeys)
						{
							stringBuilder.AppendFormat("{0}:{1};", text2, httpCookie.Values[text2]);
						}
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::FetchBackEndServerCookie]: Context {0}; Recieving cookie {1}", base.TraceContext, stringBuilder.ToString());
							return;
						}
						break;
					}
				}
			}
		}

		// Token: 0x060004C7 RID: 1223 RVA: 0x0001A630 File Offset: 0x00018830
		private void SanitizeCookie(HttpCookie backEndCookie)
		{
			if (backEndCookie == null)
			{
				return;
			}
			if (this.removeBackEndCookieEntry && base.AnchoredRoutingTarget != null)
			{
				string text = base.AnchoredRoutingTarget.AnchorMailbox.ToCookieKey();
				backEndCookie.Values.Remove(text);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::SanitizeCookie]: Context {0}; Removed cookie entry with key {1}.", base.TraceContext, text);
				}
			}
			ExDateTime exDateTime = ExDateTime.UtcNow.AddYears(-30);
			int num = 0;
			for (int i = backEndCookie.Values.Count - 1; i >= 0; i--)
			{
				bool flag = true;
				BackEndCookieEntryBase backEndCookieEntryBase = null;
				if (num < this.MaxBackEndCookieEntries && BackEndCookieEntryParser.TryParse(backEndCookie.Values[i], out backEndCookieEntryBase))
				{
					flag = backEndCookieEntryBase.Expired;
					if (!flag && this.removeBackEndCookieEntry && base.AnchoredRoutingTarget != null && backEndCookieEntryBase.ShouldInvalidate(base.AnchoredRoutingTarget.BackEndServer))
					{
						flag = true;
					}
				}
				if (flag)
				{
					string key = backEndCookie.Values.GetKey(i);
					backEndCookie.Values.Remove(key);
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::SanitizeCookie]: Context {0}; Removed cookie entry with key {1}.", base.TraceContext, key);
					}
				}
				else
				{
					num++;
					if (backEndCookieEntryBase.ExpiryTime > exDateTime)
					{
						exDateTime = backEndCookieEntryBase.ExpiryTime;
					}
				}
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::SanitizeCookie]: Context {0}; {1}", base.TraceContext, (num == 0) ? "Marking current cookie as expired." : "Extending cookie expiration.");
			}
			backEndCookie.Expires = exDateTime.UniversalTime;
		}

		// Token: 0x060004C8 RID: 1224 RVA: 0x0001A7D4 File Offset: 0x000189D4
		private void SetBackEndCookie()
		{
			if (this.haveSetBackEndCookie)
			{
				return;
			}
			foreach (string text in this.BackEndCookieNames)
			{
				if (this.ShouldProcessBackEndCookie(text))
				{
					HttpCookie httpCookie = base.ClientRequest.Cookies[text];
					if (httpCookie == null)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::SetBackEndCookie]: Context {0}; Client request does not include back end cookie.", base.TraceContext);
						}
						httpCookie = new HttpCookie(text);
					}
					httpCookie.HttpOnly = true;
					httpCookie.Secure = base.ClientRequest.IsSecureConnection;
					httpCookie.Path = this.GetCookiePath();
					if (base.AnchoredRoutingTarget != null)
					{
						string text2 = base.AnchoredRoutingTarget.AnchorMailbox.ToCookieKey();
						this.SetUseServerCookieFlag(base.AnchoredRoutingTarget.AnchorMailbox);
						BackEndCookieEntryBase backEndCookieEntryBase = base.AnchoredRoutingTarget.AnchorMailbox.BuildCookieEntryForTarget(base.AnchoredRoutingTarget.BackEndServer, base.ProxyToDownLevel, this.ShouldBackEndCookieHaveResourceForest(text), HttpProxySettings.NoMailboxFallbackRoutingEnabled.Value);
						if (backEndCookieEntryBase != null)
						{
							httpCookie.Values[text2] = backEndCookieEntryBase.ToObscureString();
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
							{
								ExTraceGlobals.VerboseTracer.TraceDebug<int, string, BackEndCookieEntryBase>((long)this.GetHashCode(), "[BEServerCookieProxyRequestHandler::SetBackEndCookie]: Context {0}; Setting cookie entry {1}={2}.", base.TraceContext, text2, backEndCookieEntryBase);
							}
						}
					}
					this.SanitizeCookie(httpCookie);
					base.ClientResponse.Cookies.Add(httpCookie);
					this.haveSetBackEndCookie = true;
				}
			}
		}

		// Token: 0x060004C9 RID: 1225 RVA: 0x0001A944 File Offset: 0x00018B44
		private string GetCookiePath()
		{
			if (base.ClientRequest.ApplicationPath.Length < base.ClientRequest.Url.AbsolutePath.Length)
			{
				return base.ClientRequest.Url.AbsolutePath.Remove(base.ClientRequest.ApplicationPath.Length);
			}
			return base.ClientRequest.Url.AbsolutePath;
		}

		// Token: 0x060004CA RID: 1226 RVA: 0x0001A9B0 File Offset: 0x00018BB0
		private bool ShouldProcessBackEndCookie(string backEndCookieName)
		{
			return this.BackEndCookieNames.Length <= 1 || (((HttpProxySettings.SupportBackEndCookie.Value & ProxyRequestHandler.SupportBackEndCookie.V1) != (ProxyRequestHandler.SupportBackEndCookie)0 || !string.Equals(backEndCookieName, "X-BackEndCookie", StringComparison.OrdinalIgnoreCase)) && ((HttpProxySettings.SupportBackEndCookie.Value & ProxyRequestHandler.SupportBackEndCookie.V2) != (ProxyRequestHandler.SupportBackEndCookie)0 || !string.Equals(backEndCookieName, "X-BackEndCookie2", StringComparison.OrdinalIgnoreCase)));
		}

		// Token: 0x060004CB RID: 1227 RVA: 0x0001AA07 File Offset: 0x00018C07
		private bool ShouldBackEndCookieHaveResourceForest(string backEndCookieName)
		{
			if ((HttpProxySettings.SupportBackEndCookie.Value & ProxyRequestHandler.SupportBackEndCookie.V2) != (ProxyRequestHandler.SupportBackEndCookie)0)
			{
				if (this.BackEndCookieNames.Length <= 1)
				{
					return true;
				}
				if (string.Equals(backEndCookieName, "X-BackEndCookie2", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x04000336 RID: 822
		private const string BackEndCookie2Name = "X-BackEndCookie2";

		// Token: 0x04000337 RID: 823
		private static readonly string[] ClientSupportedBackEndCookieNames = new string[]
		{
			"X-BackEndCookie2",
			"X-BackEndCookie"
		};

		// Token: 0x04000338 RID: 824
		private bool haveSetBackEndCookie;

		// Token: 0x04000339 RID: 825
		private bool removeBackEndCookieEntry;

		// Token: 0x0400033A RID: 826
		private HttpCookie backEndCookie;
	}
}
