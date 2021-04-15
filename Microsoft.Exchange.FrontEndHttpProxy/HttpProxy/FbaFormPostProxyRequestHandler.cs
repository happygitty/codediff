using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Security;
using System.Text;
using System.Web;
using System.Web.Configuration;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Common;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.Extensions;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200009A RID: 154
	internal class FbaFormPostProxyRequestHandler : OwaProxyRequestHandler
	{
		// Token: 0x17000129 RID: 297
		// (get) Token: 0x0600054E RID: 1358 RVA: 0x0001D474 File Offset: 0x0001B674
		internal static bool DisableSSORedirects
		{
			get
			{
				if (FbaFormPostProxyRequestHandler.disableSSORedirects == null)
				{
					bool value;
					if (!bool.TryParse(WebConfigurationManager.AppSettings["DisableSSORedirects"], out value))
					{
						value = false;
					}
					FbaFormPostProxyRequestHandler.disableSSORedirects = new bool?(value);
				}
				return FbaFormPostProxyRequestHandler.disableSSORedirects.Value;
			}
		}

		// Token: 0x0600054F RID: 1359 RVA: 0x0001D4BC File Offset: 0x0001B6BC
		public static char[] EncodeForSingleQuotedAttribute(char c)
		{
			char[] result = null;
			if (c == '&')
			{
				result = FbaFormPostProxyRequestHandler.EncodedAmpersand;
			}
			else if (c == '\'')
			{
				result = FbaFormPostProxyRequestHandler.EncodedApostrophe;
			}
			return result;
		}

		// Token: 0x06000550 RID: 1360 RVA: 0x0001D4E4 File Offset: 0x0001B6E4
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			string text = base.HttpContext.Items["destination"] as string;
			Uri uri;
			if (!Uri.TryCreate(text, UriKind.Absolute, out uri))
			{
				throw new HttpException(400, "destination value is not valid");
			}
			string text2 = null;
			bool flag;
			string text3;
			if (FbaFormPostProxyRequestHandler.IsExplicitLogon(HttpRuntime.AppDomainAppVirtualPath, uri.PathAndQuery, uri.OriginalString, out flag, out text2, out text3))
			{
				this.explicitLogonUser = text2;
			}
			AnchorMailbox anchorMailbox;
			if (!string.IsNullOrEmpty(this.explicitLogonUser) && SmtpAddress.IsValidSmtpAddress(this.explicitLogonUser))
			{
				anchorMailbox = new SmtpAnchorMailbox(this.explicitLogonUser, this);
			}
			else
			{
				anchorMailbox = AnchorMailboxFactory.CreateFromCaller(this);
			}
			UserBasedAnchorMailbox userBasedAnchorMailbox = anchorMailbox as UserBasedAnchorMailbox;
			if (userBasedAnchorMailbox != null)
			{
				if (UrlUtilities.IsEacUrl(text))
				{
					userBasedAnchorMailbox.CacheKeyPostfix = "_EAC";
				}
				else
				{
					userBasedAnchorMailbox.MissingDatabaseHandler = new Func<ADRawEntry, ADObjectId>(base.ResolveMailboxDatabase);
				}
			}
			return anchorMailbox;
		}

		// Token: 0x06000551 RID: 1361 RVA: 0x0001D5B8 File Offset: 0x0001B7B8
		protected override BackEndServer GetDownLevelClientAccessServer(AnchorMailbox anchorMailbox, BackEndServer mailboxServer)
		{
			base.LogElapsedTime("E_GetDLCAS");
			Uri uri = null;
			BackEndServer downLevelClientAccessServer = DownLevelServerManager.Instance.GetDownLevelClientAccessServer<HttpService>(anchorMailbox, mailboxServer, this.ClientAccessType, base.Logger, HttpProxyGlobals.ProtocolType == 4 || HttpProxyGlobals.ProtocolType == 5 || HttpProxyGlobals.ProtocolType == 1, out uri);
			base.LogElapsedTime("L_GetDLCAS");
			return downLevelClientAccessServer;
		}

		// Token: 0x06000552 RID: 1362 RVA: 0x0001D612 File Offset: 0x0001B812
		protected override bool ShouldContinueProxy()
		{
			this.HandleFbaFormPost(base.AnchoredRoutingTarget.BackEndServer);
			return true;
		}

		// Token: 0x06000553 RID: 1363 RVA: 0x0001D628 File Offset: 0x0001B828
		private static bool IsExplicitLogon(string appVdir, string requestVirtualPath, string requestRawUrl, out bool endsWithSlash, out string alternateMailboxSmtpAddress, out string updatedRequestUrl)
		{
			bool flag = false;
			alternateMailboxSmtpAddress = string.Empty;
			updatedRequestUrl = appVdir;
			int num = appVdir.Length + 1;
			endsWithSlash = false;
			int length = requestVirtualPath.Length;
			int num2 = length - 1;
			for (int i = num; i < length; i++)
			{
				if (i != num && requestVirtualPath[i] == '@')
				{
					flag = true;
				}
				if (requestVirtualPath[i] == '/')
				{
					endsWithSlash = true;
					num2 = i - 1;
					break;
				}
			}
			if (flag)
			{
				string text = appVdir;
				if (text.Length == 1 && text[0] == '/')
				{
					text = string.Empty;
				}
				if (endsWithSlash)
				{
					updatedRequestUrl = text + requestVirtualPath.Substring(num2 + 1);
				}
				alternateMailboxSmtpAddress = requestVirtualPath.Substring(num, num2 - num + 1);
			}
			return flag;
		}

		// Token: 0x06000554 RID: 1364 RVA: 0x0001D6DD File Offset: 0x0001B8DD
		private static string CheckRedirectUrlForNewline(string destinationUrl)
		{
			if (destinationUrl.IndexOf('\n') >= 0)
			{
				destinationUrl = destinationUrl.Replace("\n", HttpUtility.UrlEncode("\n"));
			}
			return destinationUrl;
		}

		// Token: 0x06000555 RID: 1365 RVA: 0x0001D704 File Offset: 0x0001B904
		private static FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause NeedCrossSiteRedirect(BackEndServer backEndServer, Site mailboxSite, Site currentServerSite, OwaServerVersion mailboxVersion, bool isEcpUrl, out Uri crossSiteRedirectUrl, out bool isSameAuthMethod)
		{
			isSameAuthMethod = false;
			crossSiteRedirectUrl = null;
			OwaServerVersion.CreateFromVersionString(HttpProxyGlobals.ApplicationVersion);
			FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause result = FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.None;
			if (mailboxSite == null)
			{
				return result;
			}
			if (!mailboxSite.Equals(currentServerSite))
			{
				crossSiteRedirectUrl = FbaFormPostProxyRequestHandler.FindRedirectOwaUrlCrossSite(mailboxSite, mailboxVersion.Major, OwaVdirConfiguration.Instance.InternalAuthenticationMethod, OwaVdirConfiguration.Instance.ExternalAuthenticationMethod, backEndServer, out isSameAuthMethod, out result);
				if (isEcpUrl && crossSiteRedirectUrl != null)
				{
					crossSiteRedirectUrl = FbaFormPostProxyRequestHandler.FindRedirectEcpUrlCrossSite(mailboxSite, mailboxVersion.Major, out result);
				}
			}
			return result;
		}

		// Token: 0x06000556 RID: 1366 RVA: 0x0001D780 File Offset: 0x0001B980
		private static Uri FindRedirectOwaUrlCrossSite(Site targetSite, int expectedMajorVersion, AuthenticationMethod internalAutheticationMethod, AuthenticationMethod externalAuthenticationMethod, BackEndServer backEndServer, out bool isSameAuthMethod, out FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause failureCause)
		{
			failureCause = FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.None;
			isSameAuthMethod = false;
			bool isSameAuthExternalService = false;
			OwaService clientExternalService = null;
			ServiceTopology currentServiceTopology = ServiceTopology.GetCurrentServiceTopology("d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectOwaUrlCrossSite", 391);
			string mailboxServerFQDN = backEndServer.Fqdn;
			new List<OwaService>();
			currentServiceTopology.ForEach<OwaService>(delegate(OwaService owaService)
			{
				if (ServiceTopology.IsOnSite(owaService, targetSite, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectOwaUrlCrossSite", 402) && owaService.ClientAccessType == 1 && OwaServerVersion.CreateFromVersionNumber(owaService.ServerVersionNumber).Major == expectedMajorVersion)
				{
					bool flag = false;
					if (owaService.AuthenticationMethod == internalAutheticationMethod || ((internalAutheticationMethod & 4) != null && (owaService.AuthenticationMethod & 4) != null))
					{
						flag = true;
						if (!isSameAuthExternalService)
						{
							clientExternalService = null;
							isSameAuthExternalService = true;
						}
					}
					if (flag || !isSameAuthExternalService)
					{
						if (clientExternalService == null)
						{
							clientExternalService = owaService;
							return;
						}
						if (ServiceTopology.CasMbxServicesFirst(owaService, clientExternalService, mailboxServerFQDN, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectOwaUrlCrossSite", 433) < 0)
						{
							clientExternalService = owaService;
						}
					}
				}
			}, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectOwaUrlCrossSite", 397);
			if (clientExternalService != null)
			{
				isSameAuthMethod = isSameAuthExternalService;
				return clientExternalService.Url;
			}
			failureCause = FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.NoCasFound;
			return null;
		}

		// Token: 0x06000557 RID: 1367 RVA: 0x0001D828 File Offset: 0x0001BA28
		private static Uri FindRedirectEcpUrlCrossSite(Site targetSite, int expectedMajorVersion, out FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause failureCause)
		{
			failureCause = FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.None;
			EcpService clientExternalService = null;
			ServiceTopology.GetCurrentServiceTopology("d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectEcpUrlCrossSite", 471).ForEach<EcpService>(delegate(EcpService ecpService)
			{
				if (ServiceTopology.IsOnSite(ecpService, targetSite, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectEcpUrlCrossSite", 477) && ecpService.ClientAccessType == 1 && OwaServerVersion.CreateFromVersionNumber(ecpService.ServerVersionNumber).Major == expectedMajorVersion)
				{
					clientExternalService = ecpService;
				}
			}, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectEcpUrlCrossSite", 474);
			if (clientExternalService != null)
			{
				return clientExternalService.Url;
			}
			failureCause = FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.NoCasFound;
			return null;
		}

		// Token: 0x06000558 RID: 1368 RVA: 0x0001D8A0 File Offset: 0x0001BAA0
		private static FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause NeedOnSiteLegacyRedirect(BackEndServer backEndServer, Site mailboxSite, Site currentServerSite, OwaServerVersion mailboxVersion, out Uri legacyRedirectUrl, out bool isSameAuthMethod)
		{
			isSameAuthMethod = false;
			legacyRedirectUrl = null;
			OwaServerVersion owaServerVersion = OwaServerVersion.CreateFromVersionString(HttpProxyGlobals.ApplicationVersion);
			if (mailboxSite == null)
			{
				mailboxSite = currentServerSite;
			}
			FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause result = FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.None;
			if (mailboxSite.Equals(currentServerSite) && owaServerVersion.Major > mailboxVersion.Major && mailboxVersion.Major == (int)ExchangeObjectVersion.Exchange2007.ExchangeBuild.Major)
			{
				legacyRedirectUrl = FbaFormPostProxyRequestHandler.FindRedirectOwaUrlOnSiteForMismatchVersion(mailboxSite, mailboxVersion.Major, OwaVdirConfiguration.Instance.InternalAuthenticationMethod, OwaVdirConfiguration.Instance.ExternalAuthenticationMethod, backEndServer, out isSameAuthMethod, out result);
			}
			return result;
		}

		// Token: 0x06000559 RID: 1369 RVA: 0x0001D920 File Offset: 0x0001BB20
		private static Uri FindRedirectOwaUrlOnSiteForMismatchVersion(Site targetSite, int expectedMajorVersion, AuthenticationMethod internalAutheticationMethod, AuthenticationMethod externalAuthenticationMethod, BackEndServer backEndServer, out bool isSameAuthMethod, out FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause failureCause)
		{
			failureCause = FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.None;
			isSameAuthMethod = true;
			bool isSameAuthInternalService = false;
			bool isSameAuthExternalService = false;
			OwaService clientInternalService = null;
			OwaService clientExternalService = null;
			string mailboxServerFQDN = backEndServer.Fqdn;
			ServiceTopology currentServiceTopology = ServiceTopology.GetCurrentServiceTopology("d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectOwaUrlOnSiteForMismatchVersion", 578);
			new List<OwaService>();
			currentServiceTopology.ForEach<OwaService>(delegate(OwaService owaService)
			{
				if (ServiceTopology.IsOnSite(owaService, targetSite, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectOwaUrlOnSiteForMismatchVersion", 587))
				{
					if (owaService.ClientAccessType == 1)
					{
						if (OwaServerVersion.CreateFromVersionNumber(owaService.ServerVersionNumber).Major == expectedMajorVersion)
						{
							bool flag = false;
							if (owaService.AuthenticationMethod == internalAutheticationMethod || ((internalAutheticationMethod & 4) != null && (owaService.AuthenticationMethod & 4) != null))
							{
								flag = true;
								if (!isSameAuthExternalService)
								{
									clientExternalService = null;
									isSameAuthExternalService = true;
								}
							}
							if (flag || !isSameAuthExternalService)
							{
								if (clientExternalService == null)
								{
									clientExternalService = owaService;
									return;
								}
								if (ServiceTopology.CasMbxServicesFirst(owaService, clientExternalService, mailboxServerFQDN, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectOwaUrlOnSiteForMismatchVersion", 618) < 0)
								{
									clientExternalService = owaService;
									return;
								}
							}
						}
					}
					else if (owaService.ClientAccessType == null && OwaServerVersion.CreateFromVersionNumber(owaService.ServerVersionNumber).Major == expectedMajorVersion && clientExternalService == null)
					{
						bool flag = false;
						if (owaService.AuthenticationMethod == internalAutheticationMethod || ((internalAutheticationMethod & 4) != null && (owaService.AuthenticationMethod & 4) != null))
						{
							flag = true;
							if (!isSameAuthInternalService)
							{
								clientInternalService = null;
								isSameAuthInternalService = true;
							}
						}
						if (flag || !isSameAuthInternalService)
						{
							if (clientInternalService == null)
							{
								clientInternalService = owaService;
								return;
							}
							if (ServiceTopology.CasMbxServicesFirst(owaService, clientInternalService, mailboxServerFQDN, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectOwaUrlOnSiteForMismatchVersion", 657) > 0)
							{
								clientInternalService = owaService;
							}
						}
					}
				}
			}, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "FindRedirectOwaUrlOnSiteForMismatchVersion", 582);
			if (clientExternalService != null)
			{
				isSameAuthMethod = isSameAuthExternalService;
				return clientExternalService.Url;
			}
			if (clientInternalService != null)
			{
				isSameAuthMethod = isSameAuthInternalService;
				return clientInternalService.Url;
			}
			failureCause = FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.NoCasFound;
			return null;
		}

		// Token: 0x0600055A RID: 1370 RVA: 0x0001D9F2 File Offset: 0x0001BBF2
		private static bool IsOwaUrl(Uri requestUrl, OwaUrl owaUrl, bool exactMatch)
		{
			return FbaFormPostProxyRequestHandler.IsOwaUrl(requestUrl, owaUrl, exactMatch, true);
		}

		// Token: 0x0600055B RID: 1371 RVA: 0x0001DA00 File Offset: 0x0001BC00
		private static bool IsOwaUrl(Uri requestUrl, OwaUrl owaUrl, bool exactMatch, bool useLocal)
		{
			int length = owaUrl.ImplicitUrl.Length;
			string text = useLocal ? requestUrl.LocalPath : requestUrl.PathAndQuery;
			bool flag = string.Compare(text, 0, owaUrl.ImplicitUrl, 0, length, StringComparison.OrdinalIgnoreCase) == 0;
			if (exactMatch)
			{
				flag = (flag && length == text.Length);
			}
			return flag;
		}

		// Token: 0x0600055C RID: 1372 RVA: 0x0001DA53 File Offset: 0x0001BC53
		private static string GetNoScriptHtml()
		{
			return string.Format(LocalizedStrings.GetHtmlEncoded(719849305), "<a href=\"https://go.microsoft.com/fwlink/?linkid=2009667&clcid=0x409\" > ", "</a>");
		}

		// Token: 0x0600055D RID: 1373 RVA: 0x0001DA70 File Offset: 0x0001BC70
		private void HandleFbaFormPost(BackEndServer backEndServer)
		{
			HttpContext httpContext = base.HttpContext;
			HttpResponse response = httpContext.Response;
			Uri uri = null;
			string text = httpContext.Items["destination"] as string;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = true;
			string fqdn = backEndServer.Fqdn;
			int version = backEndServer.Version;
			OwaServerVersion owaServerVersion = null;
			bool flag4 = false;
			Site site = ServiceTopology.GetCurrentServiceTopology("d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "HandleFbaFormPost", 761).GetSite(fqdn, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\RequestHandlers\\FbaFormPostProxyRequestHandler.cs", "HandleFbaFormPost", 762);
			if (site != null && !site.Equals(HttpProxyGlobals.LocalSite.Member))
			{
				flag3 = false;
			}
			if (!FbaFormPostProxyRequestHandler.DisableSSORedirects)
			{
				owaServerVersion = OwaServerVersion.CreateFromVersionNumber(version);
				if (UrlUtilities.IsEcpUrl(text) && owaServerVersion.Major < (int)ExchangeObjectVersion.Exchange2010.ExchangeBuild.Major)
				{
					flag = false;
					flag2 = false;
				}
				else if (!flag3 && !UserAgentParser.IsMonitoringRequest(base.ClientRequest.UserAgent))
				{
					if (owaServerVersion.Major >= (int)ExchangeObjectVersion.Exchange2007.ExchangeBuild.Major)
					{
						FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause legacyRedirectFailureCause = FbaFormPostProxyRequestHandler.NeedCrossSiteRedirect(backEndServer, site, HttpProxyGlobals.LocalSite.Member, owaServerVersion, UrlUtilities.IsEcpUrl(text), out uri, out flag4);
						string authority = base.ClientRequest.Url.Authority;
						string b = (uri == null) ? string.Empty : uri.Authority;
						flag2 = (legacyRedirectFailureCause != FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.NoCasFound && !string.Equals(authority, b, StringComparison.OrdinalIgnoreCase) && (legacyRedirectFailureCause != FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.None || uri != null));
						if (uri == null && owaServerVersion.Major == (int)ExchangeObjectVersion.Exchange2007.ExchangeBuild.Major)
						{
							flag = (FbaFormPostProxyRequestHandler.NeedOnSiteLegacyRedirect(backEndServer, null, HttpProxyGlobals.LocalSite.Member, owaServerVersion, out uri, out flag4) != FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.None || uri != null);
						}
					}
				}
				else
				{
					flag = (FbaFormPostProxyRequestHandler.NeedOnSiteLegacyRedirect(backEndServer, site, HttpProxyGlobals.LocalSite.Member, owaServerVersion, out uri, out flag4) != FbaFormPostProxyRequestHandler.LegacyRedirectFailureCause.None || uri != null);
				}
			}
			if (flag2 || flag)
			{
				if (uri != null)
				{
					string authority2 = base.ClientRequest.Url.Authority;
					string authority3 = uri.Authority;
					if (string.Compare(authority2, authority3, StringComparison.OrdinalIgnoreCase) == 0)
					{
						throw new HttpException(403, "Redirect loop detected");
					}
				}
				using (SecureNameValueCollection secureNameValueCollection = new SecureNameValueCollection())
				{
					int num = (int)base.HttpContext.Items["flags"];
					secureNameValueCollection.AddUnsecureNameValue("destination", base.HttpContext.Items["destination"] as string);
					secureNameValueCollection.AddUnsecureNameValue("username", base.HttpContext.Items["username"] as string);
					secureNameValueCollection.AddUnsecureNameValue("flags", num.ToString(CultureInfo.InvariantCulture));
					using (SecureString secureString = base.HttpContext.Items["password"] as SecureString)
					{
						secureNameValueCollection.AddSecureNameValue("password", secureString);
						if (!flag)
						{
							if (flag2)
							{
								if (uri == null)
								{
									throw new HttpException(302, AspNetHelper.GetCafeErrorPageRedirectUrl(httpContext, new NameValueCollection
									{
										{
											"CafeError",
											ErrorFE.FEErrorCodes.NoLegacyCAS.ToString()
										}
									}));
								}
								Uri uri2 = uri;
								if (this.explicitLogonUser != null)
								{
									uri2 = UrlUtilities.AppendSmtpAddressToUrl(uri, this.explicitLogonUser);
								}
								if (flag4)
								{
									if (uri.Scheme == Uri.UriSchemeHttps)
									{
										if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
										{
											ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "FbaFormPostProxyRequestHandler - SSO redirecting to {0}", uri.ToString());
										}
										this.RedirectUsingSSOFBA(secureNameValueCollection, uri, response, owaServerVersion.Major);
										response.End();
										return;
									}
									throw new HttpException(302, AspNetHelper.GetCafeErrorPageRedirectUrl(httpContext, new NameValueCollection
									{
										{
											"CafeError",
											ErrorFE.FEErrorCodes.NoFbaSSL.ToString()
										}
									}));
								}
								else
								{
									if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
									{
										ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "FbaFormPostProxyRequestHandler - redirecting to {0}", uri2.ToString());
									}
									base.PfdTracer.TraceRedirect("FbaAuth", uri2.ToString());
									response.Redirect(FbaFormPostProxyRequestHandler.CheckRedirectUrlForNewline(uri2.ToString()));
								}
							}
							return;
						}
						if (uri == null)
						{
							throw new HttpException(302, AspNetHelper.GetCafeErrorPageRedirectUrl(httpContext, new NameValueCollection
							{
								{
									"CafeError",
									ErrorFE.FEErrorCodes.NoLegacyCAS.ToString()
								}
							}));
						}
						if (!flag4)
						{
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
							{
								ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "FbaFormPostProxyRequestHandler - redirecting to {0}", uri.ToString());
							}
							base.PfdTracer.TraceRedirect("FbaAuth", uri.ToString());
							response.Redirect(FbaFormPostProxyRequestHandler.CheckRedirectUrlForNewline(uri.ToString()));
							return;
						}
						if (uri.Scheme == Uri.UriSchemeHttps)
						{
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
							{
								ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "FbaFormPostProxyRequestHandler - SSO redirecting to {0}", uri.ToString());
							}
							this.RedirectUsingSSOFBA(secureNameValueCollection, uri, response, owaServerVersion.Major);
							response.End();
							return;
						}
						throw new HttpException(302, AspNetHelper.GetCafeErrorPageRedirectUrl(httpContext, new NameValueCollection
						{
							{
								"CafeError",
								ErrorFE.FEErrorCodes.NoFbaSSL.ToString()
							}
						}));
					}
				}
			}
			try
			{
				FbaModule.SetCadataCookies(base.HttpApplication);
			}
			catch (MissingSslCertificateException)
			{
				throw new HttpException(302, AspNetHelper.GetCafeErrorPageRedirectUrl(httpContext, new NameValueCollection
				{
					{
						"CafeError",
						ErrorFE.FEErrorCodes.NoFbaSSL.ToString()
					}
				}));
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "FbaFormPostProxyRequestHandler - redirecting to {0}", text);
			}
			base.PfdTracer.TraceRedirect("FbaAuth", text);
			response.Redirect(FbaFormPostProxyRequestHandler.CheckRedirectUrlForNewline(text), false);
		}

		// Token: 0x0600055E RID: 1374 RVA: 0x0001E0C4 File Offset: 0x0001C2C4
		private void RedirectUsingSSOFBA(SecureNameValueCollection collection, Uri redirectUrl, HttpResponse response, int majorCasVersion)
		{
			response.StatusCode = 200;
			response.Status = "200 - OK";
			response.BufferOutput = false;
			response.CacheControl = "no-cache";
			response.Cache.SetNoStore();
			HttpCookie httpCookie = new HttpCookie("PBack");
			httpCookie.Value = "1";
			response.Cookies.Add(httpCookie);
			response.Headers.Add("X-OWA-FEError", ErrorFE.FEErrorCodes.CasRedirect.ToString());
			using (SecureHttpBuffer secureHttpBuffer = new SecureHttpBuffer(1000, response))
			{
				this.CreateHtmlForSsoFba(secureHttpBuffer, collection, redirectUrl, majorCasVersion);
				secureHttpBuffer.Flush();
				response.End();
			}
		}

		// Token: 0x0600055F RID: 1375 RVA: 0x0001E184 File Offset: 0x0001C384
		private void CreateHtmlForSsoFba(SecureHttpBuffer buffer, SecureNameValueCollection collection, Uri redirectUrl, int majorCasVersion)
		{
			string noScriptHtml = FbaFormPostProxyRequestHandler.GetNoScriptHtml();
			buffer.CopyAtCurrentPosition("<html><noscript>");
			buffer.CopyAtCurrentPosition(noScriptHtml.ToString());
			buffer.CopyAtCurrentPosition("</noscript><head><title>Continue</title><script type='text/javascript'>function OnBack(){}function DoSubmit(){var subt=false;if(!subt){subt=true;document.logonForm.submit();}}</script></head><body onload='javascript:DoSubmit();'>");
			this.CreateFormHtmlForSsoFba(buffer, collection, redirectUrl, majorCasVersion);
			buffer.CopyAtCurrentPosition("</body></html>");
		}

		// Token: 0x06000560 RID: 1376 RVA: 0x0001E1D0 File Offset: 0x0001C3D0
		private void CreateFormHtmlForSsoFba(SecureHttpBuffer buffer, SecureNameValueCollection collection, Uri redirectUrl, int majorCasVersion)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(redirectUrl.Scheme);
			stringBuilder.Append(Uri.SchemeDelimiter);
			stringBuilder.Append(redirectUrl.Authority);
			stringBuilder.Append(OwaUrl.AuthDll.ImplicitUrl);
			buffer.CopyAtCurrentPosition("<form name='logonForm' id='logonForm' action='");
			buffer.CopyAtCurrentPosition(stringBuilder.ToString());
			buffer.CopyAtCurrentPosition("' method='post' target='_top'>");
			this.CreateInputHtmlCollection(collection, buffer, redirectUrl, majorCasVersion);
			buffer.CopyAtCurrentPosition("</form>");
		}

		// Token: 0x06000561 RID: 1377 RVA: 0x0001E254 File Offset: 0x0001C454
		private void CreateInputHtmlCollection(SecureNameValueCollection collection, SecureHttpBuffer buffer, Uri redirectUrl, int majorCasVersion)
		{
			foreach (string text in collection)
			{
				buffer.CopyAtCurrentPosition("<input type='hidden' name='");
				buffer.CopyAtCurrentPosition(text);
				buffer.CopyAtCurrentPosition("' value='");
				if (text == "password")
				{
					SecureString secureString;
					collection.TryGetSecureValue(text, out secureString);
					using (SecureArray<char> secureArray = SecureStringExtensions.TransformToSecureCharArray(secureString, new CharTransformDelegate(FbaFormPostProxyRequestHandler.EncodeForSingleQuotedAttribute)))
					{
						buffer.CopyAtCurrentPosition(secureArray);
						goto IL_14D;
					}
					goto IL_74;
				}
				goto IL_74;
				IL_14D:
				buffer.CopyAtCurrentPosition("'>");
				continue;
				IL_74:
				string text2;
				if (!(text == "destination"))
				{
					collection.TryGetUnsecureValue(text, out text2);
					buffer.CopyAtCurrentPosition(EncodingUtilities.HtmlEncode(text2));
					goto IL_14D;
				}
				collection.TryGetUnsecureValue(text, out text2);
				Uri uri;
				if (!Uri.TryCreate(text2, UriKind.Absolute, out uri))
				{
					throw new HttpException(400, "destination value is not valid");
				}
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(redirectUrl.Scheme);
				stringBuilder.Append(Uri.SchemeDelimiter);
				stringBuilder.Append(redirectUrl.Authority);
				if (FbaFormPostProxyRequestHandler.IsOwaUrl(uri, OwaUrl.AuthPost, true))
				{
					stringBuilder.Append(OwaUrl.ApplicationRoot.ImplicitUrl);
				}
				else if (string.IsNullOrEmpty(this.explicitLogonUser))
				{
					stringBuilder.Append(redirectUrl.PathAndQuery);
				}
				else
				{
					stringBuilder.Append(uri.PathAndQuery);
				}
				buffer.CopyAtCurrentPosition(stringBuilder.ToString());
				goto IL_14D;
			}
		}

		// Token: 0x0400036B RID: 875
		private const string PostBackFFCookieName = "PBack";

		// Token: 0x0400036C RID: 876
		private const string DisableSSORedirectsAppSetting = "DisableSSORedirects";

		// Token: 0x0400036D RID: 877
		private static readonly char[] EncodedAmpersand = EncodingUtilities.HtmlEncode("&").ToCharArray();

		// Token: 0x0400036E RID: 878
		private static readonly char[] EncodedApostrophe = EncodingUtilities.HtmlEncode("'").ToCharArray();

		// Token: 0x0400036F RID: 879
		private static bool? disableSSORedirects = null;

		// Token: 0x04000370 RID: 880
		private string explicitLogonUser;

		// Token: 0x02000121 RID: 289
		private enum LegacyRedirectFailureCause
		{
			// Token: 0x04000551 RID: 1361
			None,
			// Token: 0x04000552 RID: 1362
			NoCasFound
		}
	}
}
