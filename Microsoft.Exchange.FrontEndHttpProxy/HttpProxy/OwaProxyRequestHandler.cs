using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.Security.OAuth;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A9 RID: 169
	internal class OwaProxyRequestHandler : OwaEcpProxyRequestHandler<OwaService>
	{
		// Token: 0x1700013C RID: 316
		// (get) Token: 0x060005BF RID: 1471 RVA: 0x0001FC2D File Offset: 0x0001DE2D
		protected override string ProxyLogonUri
		{
			get
			{
				return "proxyLogon.owa";
			}
		}

		// Token: 0x1700013D RID: 317
		// (get) Token: 0x060005C0 RID: 1472 RVA: 0x00003165 File Offset: 0x00001365
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x1700013E RID: 318
		// (get) Token: 0x060005C1 RID: 1473 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool WillAddProtocolSpecificCookiesToClientResponse
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700013F RID: 319
		// (get) Token: 0x060005C2 RID: 1474 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool ShouldForceUnbufferedClientResponseOutput
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060005C3 RID: 1475 RVA: 0x0001FC34 File Offset: 0x0001DE34
		internal static void AddProxyUriHeader(HttpRequest request, WebHeaderCollection headers)
		{
			headers["X-OWA-ProxyUri"] = new UriBuilder
			{
				Scheme = request.Url.Scheme,
				Port = request.Url.Port,
				Host = request.Url.Host,
				Path = request.ApplicationPath
			}.Uri.ToString();
		}

		// Token: 0x060005C4 RID: 1476 RVA: 0x0001FC9C File Offset: 0x0001DE9C
		internal static bool IsRoutingErrorFromOWA(ProxyRequestHandler requestHandler, HttpWebResponse response)
		{
			string text;
			return requestHandler.TryGetSpecificHeaderFromResponse(response, "OwaProxyRequestHandler::IsRoutingErrorFromOWA", "X-RetriableError", "1", out text) || requestHandler.TryGetSpecificHeaderFromResponse(response, "OwaProxyRequestHandler::IsRoutingErrorFromOWA", "X-OWA-Error", Constants.IllegalCrossServerConnectionExceptionType, out text) || requestHandler.TryGetSpecificHeaderFromResponse(response, "OwaProxyRequestHandler::IsRoutingErrorFromOWA", "X-OWA-Error", UrlUtilities.RetriableErrorHeaderValue, out text) || requestHandler.TryGetSpecificHeaderFromResponse(response, "OwaProxyRequestHandler::IsRoutingErrorFromOWA", "X-OWA-Error", "Microsoft.Exchange.Data.Storage.DatabaseNotFoundException", out text);
		}

		// Token: 0x060005C5 RID: 1477 RVA: 0x0001FD1C File Offset: 0x0001DF1C
		protected override void SetProtocolSpecificServerRequestParameters(HttpWebRequest serverRequest)
		{
			base.SetProtocolSpecificServerRequestParameters(serverRequest);
			object obj = base.HttpContext.Items["Flags"];
			if (obj != null)
			{
				int num = (int)obj;
				if ((num & 4) != 4)
				{
					serverRequest.Headers["X-LogonType"] = "Public";
				}
				if ((num & 1) == 1 && !base.ClientRequest.Url.AbsolutePath.StartsWith("/owa/attachment.ashx", StringComparison.OrdinalIgnoreCase) && !base.ClientRequest.Url.AbsolutePath.StartsWith("/owa/integrated/attachment.ashx", StringComparison.OrdinalIgnoreCase))
				{
					serverRequest.UserAgent = "Mozilla/5.0 (Windows NT; owaauth)";
				}
			}
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x0001FDB4 File Offset: 0x0001DFB4
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			IIdentity identity = base.HttpContext.User.Identity;
			CompositeIdentity compositeIdentity = base.HttpContext.User.Identity as CompositeIdentity;
			if (compositeIdentity != null)
			{
				identity = compositeIdentity.PrimaryIdentity;
			}
			if (!base.ProxyToDownLevel || identity is OAuthIdentity || identity is OAuthPreAuthIdentity || identity is MSAIdentity)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[OwaProxyRequestHandler::AddProtocolSpecificHeadersToServerRequest]: Skip adding downlevel proxy headers.");
				}
			}
			else
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[OwaProxyRequestHandler::AddProtocolSpecificHeadersToServerRequest]: User identity type is {0}.", identity.GetType().FullName);
				}
				headers["X-OWA-ProxySid"] = IIdentityExtensions.GetSecurityIdentifier(identity).ToString();
				OwaProxyRequestHandler.AddProxyUriHeader(base.ClientRequest, headers);
				headers["X-OWA-ProxyVersion"] = HttpProxyGlobals.ApplicationVersion;
			}
			if (UrlUtilities.IsCmdWebPart(base.ClientRequest) && !OwaProxyRequestHandler.IsOwa15Url(base.ClientRequest))
			{
				headers["X-OWA-ProxyWebPart"] = "1";
			}
			headers["RPSPUID"] = (string)base.HttpContext.Items["RPSPUID"];
			headers["RPSOrgIdPUID"] = (string)base.HttpContext.Items["RPSOrgIdPUID"];
			headers["logonLatency"] = (string)base.HttpContext.Items["logonLatency"];
			if (base.IsExplicitSignOn)
			{
				headers[OwaHttpHeader.ExplicitLogonUser] = HttpUtility.UrlDecode(base.ExplicitSignOnAddress);
			}
			string clientRequestIdValue = DiagnosticIdentifiers.GetClientRequestIdValue();
			base.HttpContext.Response.AppendToLog(string.Format("&{0}={1}", "ClientRequestId", clientRequestIdValue));
			headers["X-ClientRequestId"] = clientRequestIdValue;
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x0001FF88 File Offset: 0x0001E188
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return !string.Equals(headerName, "X-OWA-ProxyUri", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "X-OWA-ProxyVersion", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "X-OWA-ProxySid", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "X-LogonType", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "X-OWA-ProxyWebPart", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "RPSPUID", StringComparison.OrdinalIgnoreCase) && base.ShouldCopyHeaderToServerRequest(headerName);
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x0001FFF2 File Offset: 0x0001E1F2
		protected override void CopySupplementalCookiesToClientResponse()
		{
			if (HttpProxySettings.DFPOWAVdirProxyEnabled.Value)
			{
				this.SetDFPOwaVdirCookie();
			}
			base.CopySupplementalCookiesToClientResponse();
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x0002000C File Offset: 0x0001E20C
		protected override Uri GetTargetBackEndServerUrl()
		{
			Uri targetBackEndServerUrl = base.GetTargetBackEndServerUrl();
			if (HttpProxySettings.DFPOWAVdirProxyEnabled.Value)
			{
				string text = base.ClientRequest.QueryString[Constants.DFPOWAVdirParam];
				HttpCookie httpCookie = base.ClientRequest.Cookies["X-DFPOWA-Vdir"];
				if (!base.ClientRequest.Url.AbsolutePath.EndsWith("/logoff.owa", StringComparison.OrdinalIgnoreCase))
				{
					string text2 = string.Empty;
					if (httpCookie != null && !string.IsNullOrEmpty(httpCookie.Value))
					{
						text2 = httpCookie.Value;
					}
					if (!string.IsNullOrEmpty(text))
					{
						text = text.Trim();
						if (OwaProxyRequestHandler.DFPOWAValidVdirValues.Contains(text, StringComparer.OrdinalIgnoreCase))
						{
							text2 = text;
						}
					}
					if (!string.IsNullOrEmpty(text2))
					{
						return UrlUtilities.FixDFPOWAVdirUrlForBackEnd(targetBackEndServerUrl, text2);
					}
				}
			}
			return UrlUtilities.FixIntegratedAuthUrlForBackEnd(targetBackEndServerUrl);
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x000200D0 File Offset: 0x0001E2D0
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			if (base.AuthBehavior.IsFullyAuthenticated())
			{
				return this.LegacyResolveAnchorMailbox();
			}
			base.HasPreemptivelyCheckedForRoutingHint = true;
			string liveIdMemberName;
			AnchorMailbox anchorMailbox;
			if (!RequestHeaderParser.TryGetAnchorMailboxUpn(base.ClientRequest.Headers, ref liveIdMemberName))
			{
				anchorMailbox = base.CreateAnchorMailboxFromRoutingHint();
			}
			else
			{
				base.Logger.SafeSet(3, "OwaEcpUpn");
				anchorMailbox = new LiveIdMemberNameAnchorMailbox(liveIdMemberName, null, this);
			}
			string text;
			RequestHeaderParser.TryGetExplicitLogonSmtp(base.ClientRequest.Headers, ref text);
			if (anchorMailbox == null)
			{
				if (base.UseRoutingHintForAnchorMailbox)
				{
					if (!string.IsNullOrEmpty(text) && SmtpAddress.IsValidSmtpAddress(text))
					{
						base.IsExplicitSignOn = true;
						base.ExplicitSignOnAddress = text;
						base.Logger.Set(3, "ExplicitLogon-SMTP-Header");
						anchorMailbox = new SmtpAnchorMailbox(text, this);
					}
					else
					{
						text = this.TryGetExplicitLogonNode(0);
						if (!string.IsNullOrEmpty(text))
						{
							if (SmtpAddress.IsValidSmtpAddress(text))
							{
								base.IsExplicitSignOn = true;
								base.ExplicitSignOnAddress = text;
								base.Logger.Set(3, "ExplicitLogon-SMTP");
								anchorMailbox = new SmtpAnchorMailbox(text, this);
							}
							else if ((Utilities.IsPartnerHostedOnly || CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).ExplicitDomain.Enabled) && SmtpAddress.IsValidDomain(text))
							{
								text = this.TryGetExplicitLogonNode(1);
								if (text != null)
								{
									base.IsExplicitSignOn = true;
									base.ExplicitSignOnAddress = text;
									base.Logger.Set(3, "ExplicitLogon-SMTP");
									anchorMailbox = new SmtpAnchorMailbox(text, this);
								}
							}
						}
					}
				}
				if (anchorMailbox == null)
				{
					anchorMailbox = base.ResolveAnchorMailbox();
				}
				else
				{
					base.IsAnchorMailboxFromRoutingHint = true;
					this.originalAnchorMailboxFromExplicitLogon = anchorMailbox;
				}
			}
			else if (!string.IsNullOrWhiteSpace(text))
			{
				if (!string.Equals(anchorMailbox.SourceObject.ToString(), text, StringComparison.InvariantCultureIgnoreCase))
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(base.Logger, "ExplicitLogonMismatch", string.Format("{0}~{1}", anchorMailbox.SourceObject, text));
				}
				this.originalAnchorMailboxFromExplicitLogon = anchorMailbox;
			}
			UserBasedAnchorMailbox userBasedAnchorMailbox = anchorMailbox as UserBasedAnchorMailbox;
			if (userBasedAnchorMailbox != null)
			{
				userBasedAnchorMailbox.MissingDatabaseHandler = new Func<ADRawEntry, ADObjectId>(this.ResolveMailboxDatabase);
			}
			return anchorMailbox;
		}

		// Token: 0x060005CB RID: 1483 RVA: 0x000202C4 File Offset: 0x0001E4C4
		protected ADObjectId ResolveMailboxDatabase(ADRawEntry activeDirectoryRawEntry)
		{
			if (activeDirectoryRawEntry == null)
			{
				throw new ArgumentNullException("activeDirectoryRawEntry");
			}
			SmtpProxyAddress smtpProxyAddress = (SmtpProxyAddress)activeDirectoryRawEntry[ADRecipientSchema.ExternalEmailAddress];
			if (smtpProxyAddress != null)
			{
				OrganizationId organizationId = (OrganizationId)activeDirectoryRawEntry[ADObjectSchema.OrganizationId];
				OrganizationIdCacheValue organizationIdCacheValue = OrganizationIdCache.Singleton.Get(organizationId);
				SmtpAddress smtpAddress = (SmtpAddress)smtpProxyAddress;
				if (!smtpAddress.IsValidAddress)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[OwaProxyRequestHandler::ResolveMailboxDatabase]: ExternalEmailAddress configured is invalid.");
					}
				}
				else
				{
					OrganizationRelationship organizationRelationship = organizationIdCacheValue.GetOrganizationRelationship(((SmtpAddress)smtpProxyAddress).Domain);
					if (organizationRelationship != null && organizationRelationship.TargetOwaURL != null)
					{
						string absoluteUri = organizationRelationship.TargetOwaURL.AbsoluteUri;
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[OwaProxyRequestHandler::ResolveMailboxDatabase]: Stop processing and redirect to {0}.", absoluteUri);
						}
						base.Logger.AppendGenericInfo("ExternalRedir", absoluteUri);
						throw new HttpException(302, this.GetCrossPremiseRedirectUrl(smtpAddress.Domain, organizationId.ToExternalDirectoryOrganizationId(), smtpProxyAddress.SmtpAddress));
					}
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[OwaProxyRequestHandler::ResolveMailboxDatabase]: Unable to find OrganizationRelationShip or its TargetOwaUrl is not configured.");
					}
					base.Logger.AppendGenericInfo("ExternalRedir", "Org-Relationship or targetOwaUrl not found.");
				}
			}
			return null;
		}

		// Token: 0x060005CC RID: 1484 RVA: 0x00020420 File Offset: 0x0001E620
		protected override void ResetForRetryOnError()
		{
			this.originalAnchorMailboxFromExplicitLogon = null;
			base.ResetForRetryOnError();
		}

		// Token: 0x060005CD RID: 1485 RVA: 0x00020430 File Offset: 0x0001E630
		protected override void UpdateOrInvalidateAnchorMailboxCache(Guid mdbGuid, string resourceForest)
		{
			if (this.originalAnchorMailboxFromExplicitLogon != null && this.originalAnchorMailboxFromExplicitLogon != base.AnchoredRoutingTarget.AnchorMailbox && !string.IsNullOrEmpty(resourceForest))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailbox, Guid>((long)this.GetHashCode(), "[OwaProxyRequestHandler::UpdateOrInvalidateAnchorMailboxCache]: Updating anchor mailbox cache for original anchor mailbox {0}, mapping to Mailbox Database {1}.", this.originalAnchorMailboxFromExplicitLogon, mdbGuid);
				}
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(base.Logger, "UpdateAnchorMbxCache", string.Format("{0}~{1}~{2}", this.originalAnchorMailboxFromExplicitLogon, mdbGuid, resourceForest));
				this.originalAnchorMailboxFromExplicitLogon.UpdateCache(new AnchorMailboxCacheEntry
				{
					Database = new ADObjectId(mdbGuid, resourceForest)
				});
				return;
			}
			base.UpdateOrInvalidateAnchorMailboxCache(mdbGuid, resourceForest);
		}

		// Token: 0x060005CE RID: 1486 RVA: 0x000204DC File Offset: 0x0001E6DC
		protected override void HandleLogoffRequest()
		{
			if (base.ClientRequest != null && base.ClientResponse != null && base.ClientRequest.Url.AbsolutePath.EndsWith("/logoff.owa", StringComparison.OrdinalIgnoreCase))
			{
				if (!Utilities.IsPartnerHostedOnly && !CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).NoFormBasedAuthentication.Enabled)
				{
					FbaModule.InvalidateKeyCache(base.ClientRequest);
				}
				Utility.DeleteFbaAuthCookies(base.ClientRequest, base.ClientResponse, false);
			}
		}

		// Token: 0x060005CF RID: 1487 RVA: 0x00020554 File Offset: 0x0001E754
		protected override BackEndServer GetE12TargetServer(BackEndServer mailboxServer)
		{
			if (!Utilities.IsPartnerHostedOnly && !CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).NoCrossForestServerLocate.Enabled)
			{
				Uri e12ExternalUrl = HttpProxyBackEndHelper.GetE12ExternalUrl<OwaService>(mailboxServer);
				if (e12ExternalUrl != null)
				{
					throw new HttpException(302, e12ExternalUrl.ToString());
				}
			}
			return base.GetE12TargetServer(mailboxServer);
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x00019686 File Offset: 0x00017886
		protected override bool IsRoutingError(HttpWebResponse response)
		{
			return OwaProxyRequestHandler.IsRoutingErrorFromOWA(this, response) || base.IsRoutingError(response);
		}

		// Token: 0x060005D1 RID: 1489 RVA: 0x000205A8 File Offset: 0x0001E7A8
		private static bool IsOwa15Url(HttpRequest request)
		{
			if (string.IsNullOrEmpty(request.Url.Query))
			{
				return true;
			}
			foreach (string name in OwaProxyRequestHandler.Owa15ParameterNames)
			{
				if (!string.IsNullOrEmpty(request.QueryString[name]))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060005D2 RID: 1490 RVA: 0x000205F8 File Offset: 0x0001E7F8
		private string GetCrossPremiseRedirectUrl(string domainName, string externalDirectoryOrgId, string externalEmailAddress)
		{
			NameValueCollection nameValueCollection = new NameValueCollection();
			string value = UrlUtilities.IsConsumerRequestForO365(base.HttpContext) ? OwaProxyRequestHandler.SilentRedirection : OwaProxyRequestHandler.ManualRedirection;
			nameValueCollection.Add("redirectType", value);
			nameValueCollection.Add("extDomain", domainName);
			nameValueCollection.Add("extDirOrgId", externalDirectoryOrgId);
			if (CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).AddExternalEmailAddressToRedirectURL.Enabled)
			{
				nameValueCollection.Add("extEmail", externalEmailAddress);
			}
			return AspNetHelper.GetCafeErrorPageRedirectUrl(base.HttpContext, nameValueCollection);
		}

		// Token: 0x060005D3 RID: 1491 RVA: 0x0002067C File Offset: 0x0001E87C
		private void SetDFPOwaVdirCookie()
		{
			if (HttpProxySettings.DFPOWAVdirProxyEnabled.Value)
			{
				string text = base.ClientRequest.QueryString[Constants.DFPOWAVdirParam];
				HttpCookie httpCookie = base.ClientRequest.Cookies["X-DFPOWA-Vdir"];
				if (!string.IsNullOrEmpty(text))
				{
					text = text.Trim();
					if (!OwaProxyRequestHandler.DFPOWAValidVdirValues.Contains(text, StringComparer.OrdinalIgnoreCase))
					{
						return;
					}
					bool flag = httpCookie != null && !string.Equals(text, httpCookie.Value);
					if (httpCookie == null || flag)
					{
						HttpCookie httpCookie2 = new HttpCookie("X-DFPOWA-Vdir", text);
						httpCookie2.HttpOnly = false;
						httpCookie2.Secure = base.ClientRequest.IsSecureConnection;
						base.ClientResponse.Cookies.Add(httpCookie2);
					}
				}
			}
		}

		// Token: 0x060005D4 RID: 1492 RVA: 0x0002073C File Offset: 0x0001E93C
		private AnchorMailbox LegacyResolveAnchorMailbox()
		{
			AnchorMailbox anchorMailbox = null;
			if (base.UseRoutingHintForAnchorMailbox)
			{
				string text;
				if (RequestHeaderParser.TryGetExplicitLogonSmtp(base.ClientRequest.Headers, ref text))
				{
					base.IsExplicitSignOn = true;
					base.ExplicitSignOnAddress = text;
					base.Logger.Set(3, "ExplicitLogon-SMTP-Header");
					anchorMailbox = new SmtpAnchorMailbox(text, this);
				}
				else
				{
					text = this.TryGetExplicitLogonNode(0);
					if (!string.IsNullOrEmpty(text))
					{
						if (SmtpAddress.IsValidSmtpAddress(text))
						{
							base.IsExplicitSignOn = true;
							base.ExplicitSignOnAddress = text;
							base.Logger.Set(3, "ExplicitLogon-SMTP");
							anchorMailbox = new SmtpAnchorMailbox(text, this);
						}
						else if ((Utilities.IsPartnerHostedOnly || CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).ExplicitDomain.Enabled) && SmtpAddress.IsValidDomain(text))
						{
							string domain = text;
							text = this.TryGetExplicitLogonNode(1);
							if (text == null)
							{
								base.Logger.Set(3, "ExplicitLogon-Domain");
								anchorMailbox = new DomainAnchorMailbox(domain, this);
							}
							else if (SmtpAddress.IsValidSmtpAddress(text))
							{
								base.IsExplicitSignOn = true;
								base.ExplicitSignOnAddress = text;
								base.Logger.Set(3, "ExplicitLogon-SMTP");
								anchorMailbox = new SmtpAnchorMailbox(text, this);
							}
						}
					}
				}
			}
			if (anchorMailbox == null)
			{
				anchorMailbox = base.ResolveAnchorMailbox();
			}
			else
			{
				base.IsAnchorMailboxFromRoutingHint = true;
				this.originalAnchorMailboxFromExplicitLogon = anchorMailbox;
			}
			UserBasedAnchorMailbox userBasedAnchorMailbox = anchorMailbox as UserBasedAnchorMailbox;
			if (userBasedAnchorMailbox != null)
			{
				userBasedAnchorMailbox.MissingDatabaseHandler = new Func<ADRawEntry, ADObjectId>(this.ResolveMailboxDatabase);
			}
			return anchorMailbox;
		}

		// Token: 0x0400038A RID: 906
		public const string XOwaErrorHeaderName = "X-OWA-Error";

		// Token: 0x0400038B RID: 907
		public const string DatabaseNotFoundException = "Microsoft.Exchange.Data.Storage.DatabaseNotFoundException";

		// Token: 0x0400038C RID: 908
		public static readonly string[] DFPOWAValidVdirValues = new string[]
		{
			"OWA",
			"DFPOWA",
			"DFPOWA1",
			"DFPOWA2",
			"DFPOWA3",
			"DFPOWA4",
			"DFPOWA5"
		};

		// Token: 0x0400038D RID: 909
		private const string OwaLogonTypeHeader = "X-LogonType";

		// Token: 0x0400038E RID: 910
		private const string OwaLogonTypeHeaderPublicValue = "Public";

		// Token: 0x0400038F RID: 911
		private const string OwaProxyLogonUri = "proxyLogon.owa";

		// Token: 0x04000390 RID: 912
		private const string AttachmentUrl = "/owa/attachment.ashx";

		// Token: 0x04000391 RID: 913
		private const string IntegratedAttachmentUrl = "/owa/integrated/attachment.ashx";

		// Token: 0x04000392 RID: 914
		private const string DownlevelUserAgent = "Mozilla/5.0 (Windows NT; owaauth)";

		// Token: 0x04000393 RID: 915
		private const string LiveIdPuid = "RPSPUID";

		// Token: 0x04000394 RID: 916
		private const string OrgIdPuid = "RPSOrgIdPUID";

		// Token: 0x04000395 RID: 917
		private const string LogonLatencyName = "logonLatency";

		// Token: 0x04000396 RID: 918
		private static readonly string ManualRedirection = 1.ToString();

		// Token: 0x04000397 RID: 919
		private static readonly string SilentRedirection = 0.ToString();

		// Token: 0x04000398 RID: 920
		private static readonly string[] Owa15ParameterNames = new string[]
		{
			Constants.DFPOWAVdirParam,
			"owa15",
			"appcache",
			"diag",
			"layout",
			"offline",
			"prefetch",
			"server",
			"sync",
			"tracelevel",
			"viewmodel",
			"wa"
		};

		// Token: 0x04000399 RID: 921
		private AnchorMailbox originalAnchorMailboxFromExplicitLogon;
	}
}
