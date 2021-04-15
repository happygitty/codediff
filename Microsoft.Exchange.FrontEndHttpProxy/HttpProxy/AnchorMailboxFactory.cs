using System;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.Security.Authorization;
using Microsoft.Exchange.Security.OAuth;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200001A RID: 26
	internal static class AnchorMailboxFactory
	{
		// Token: 0x060000E5 RID: 229 RVA: 0x000055DC File Offset: 0x000037DC
		public static AnchorMailbox CreateFromCaller(IRequestContext requestContext)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			CommonAccessToken commonAccessToken = requestContext.HttpContext.Items["Item-CommonAccessToken"] as CommonAccessToken;
			if (commonAccessToken != null)
			{
				AnchorMailbox anchorMailbox = AnchorMailboxFactory.TryCreateFromCommonAccessToken(commonAccessToken, requestContext);
				if (anchorMailbox != null)
				{
					return anchorMailbox;
				}
			}
			if (requestContext.HttpContext.User == null || requestContext.HttpContext.User.Identity == null)
			{
				requestContext.Logger.SafeSet(3, "UnauthenticatedRequest-RandomBackEnd");
				return new AnonymousAnchorMailbox(requestContext);
			}
			if (HttpProxySettings.IdentityIndependentAuthBehaviorEnabled.Value && requestContext.AuthBehavior.AuthState != AuthState.FrontEndFullAuth)
			{
				AnchorMailbox anchorMailbox2 = requestContext.AuthBehavior.CreateAuthModuleSpecificAnchorMailbox(requestContext);
				if (anchorMailbox2 != null)
				{
					return anchorMailbox2;
				}
			}
			WindowsIdentity windowsIdentity = requestContext.HttpContext.User.Identity as WindowsIdentity;
			if (windowsIdentity != null && windowsIdentity.User == null)
			{
				requestContext.Logger.SafeSet(3, "AnonymousRequest-RandomBackEnd");
				return new AnonymousAnchorMailbox(requestContext);
			}
			return AnchorMailboxFactory.CreateFromLogonIdentity(requestContext);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x000056D4 File Offset: 0x000038D4
		public static AnchorMailbox TryCreateFromRoutingHint(IRequestContext requestContext, bool tryTargetServerRoutingHint)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			string fqdn;
			if (tryTargetServerRoutingHint && RequestHeaderParser.TryGetTargetServer(requestContext.HttpContext.Request.Headers, ref fqdn))
			{
				requestContext.Logger.Set(3, "TargetServerHeader");
				return new ServerInfoAnchorMailbox(fqdn, requestContext);
			}
			string text;
			if (!RequestHeaderParser.TryGetAnchorMailbox(requestContext.HttpContext.Request.Headers, ref text))
			{
				return null;
			}
			Match match = RegexUtilities.TryMatch(Constants.SidRegex, text);
			if (match != null && match.Success)
			{
				string text2 = RegexUtilities.ParseIdentifier(match, "${sid}");
				if (!string.IsNullOrEmpty(text2))
				{
					SecurityIdentifier securityIdentifier = null;
					try
					{
						securityIdentifier = new SecurityIdentifier(text2);
					}
					catch (ArgumentException ex)
					{
						requestContext.Logger.AppendGenericError("Ignored Exception", ex.ToString());
					}
					catch (SystemException ex2)
					{
						requestContext.Logger.AppendGenericError("Ignored Exception", ex2.ToString());
					}
					if (securityIdentifier != null)
					{
						requestContext.Logger.SafeSet(3, "AnchorMailboxHeader-SID");
						return new SidAnchorMailbox(securityIdentifier, requestContext);
					}
				}
			}
			Guid mailboxGuid;
			string text3;
			if (RequestHeaderParser.TryGetMailboxGuid(text, ref mailboxGuid, ref text3))
			{
				string value = string.Format("AnchorMailboxHeader-MailboxGuid{0}", string.IsNullOrEmpty(text3) ? string.Empty : "WithDomain");
				requestContext.Logger.SafeSet(3, value);
				MailboxGuidAnchorMailbox mailboxGuidAnchorMailbox = new MailboxGuidAnchorMailbox(mailboxGuid, text3, requestContext);
				if (!string.IsNullOrEmpty(text3))
				{
					mailboxGuidAnchorMailbox.FallbackSmtp = text;
				}
				return mailboxGuidAnchorMailbox;
			}
			if (PuidAnchorMailbox.IsEnabled)
			{
				NetID netID;
				Guid tenantGuid;
				if (RequestHeaderParser.TryGetNetIdAndTenantGuid(text, ref netID, ref tenantGuid))
				{
					requestContext.Logger.Set(3, "AnchorMailboxHeader-PuidAndTenantGuid");
					return new PuidAnchorMailbox(netID.ToString(), tenantGuid, requestContext, text);
				}
				string text4;
				if (RequestHeaderParser.TryGetNetIdAndDomain(text, ref netID, ref text4))
				{
					if (string.IsNullOrEmpty(text4))
					{
						requestContext.Logger.Set(3, "AnchorMailboxHeader-Puid");
						return new PuidAnchorMailbox(netID.ToString(), requestContext, text);
					}
					requestContext.Logger.Set(3, "AnchorMailboxHeader-PuidAndDomain");
					return new PuidAnchorMailbox(netID.ToString(), text4, requestContext, text);
				}
			}
			if (SmtpAddress.IsValidSmtpAddress(text))
			{
				requestContext.Logger.Set(3, "AnchorMailboxHeader-SMTP");
				return new SmtpAnchorMailbox(text, requestContext);
			}
			return null;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00005928 File Offset: 0x00003B28
		public static bool TryCreateFromMailboxGuid(IRequestContext requestContext, string anchorMailboxAddress, out AnchorMailbox anchorMailbox)
		{
			anchorMailbox = null;
			Guid mailboxGuid;
			string domain;
			if (RequestHeaderParser.TryGetMailboxGuid(anchorMailboxAddress, ref mailboxGuid, ref domain))
			{
				requestContext.Logger.SafeSet(3, "URL-MailboxGuidWithDomain");
				anchorMailbox = new MailboxGuidAnchorMailbox(mailboxGuid, domain, requestContext);
				return true;
			}
			return false;
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00005968 File Offset: 0x00003B68
		public static ArchiveSupportedAnchorMailbox ParseAnchorMailboxFromSmtp(IRequestContext requestContext, string smtpAddress, string source, bool failOnDomainNotFound)
		{
			Guid empty = Guid.Empty;
			string empty2 = string.Empty;
			if (RequestHeaderParser.TryGetMailboxGuid(smtpAddress, ref empty, ref empty2))
			{
				string value = string.Format("{0}-MailboxGuid{1}", source, string.IsNullOrEmpty(empty2) ? string.Empty : "WithDomainAndSmtpFallback");
				requestContext.Logger.SafeSet(3, value);
				MailboxGuidAnchorMailbox mailboxGuidAnchorMailbox = new MailboxGuidAnchorMailbox(empty, empty2, requestContext);
				if (!string.IsNullOrEmpty(empty2))
				{
					mailboxGuidAnchorMailbox.FallbackSmtp = smtpAddress;
				}
				return mailboxGuidAnchorMailbox;
			}
			requestContext.Logger.SafeSet(3, string.Format("{0}-SMTP", source));
			return new SmtpAnchorMailbox(smtpAddress, requestContext)
			{
				FailOnDomainNotFound = failOnDomainNotFound
			};
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00005A04 File Offset: 0x00003C04
		public static AnchorMailbox CreateFromSamlTokenAddress(string address, IRequestContext requestContext)
		{
			if (string.IsNullOrEmpty(address))
			{
				string text = string.Format("Cannot authenticate user address claim {0}", address);
				requestContext.Logger.AppendGenericError("Invalid Wssecurity address claim.", text);
				throw new HttpProxyException(HttpStatusCode.Unauthorized, 4002, text);
			}
			if (SmtpAddress.IsValidSmtpAddress(address))
			{
				requestContext.Logger.Set(3, "WSSecurityRequest-SMTP");
				return new SmtpAnchorMailbox(address, requestContext)
				{
					FailOnDomainNotFound = false
				};
			}
			Match match = RegexUtilities.TryMatch(Constants.SidOnlyRegex, address);
			if (match != null && match.Success)
			{
				SecurityIdentifier securityIdentifier = null;
				try
				{
					securityIdentifier = new SecurityIdentifier(address);
				}
				catch (ArgumentException ex)
				{
					requestContext.Logger.AppendGenericError("Ignored Exception", ex.ToString());
				}
				catch (SystemException ex2)
				{
					requestContext.Logger.AppendGenericError("Ignored Exception", ex2.ToString());
				}
				if (securityIdentifier != null)
				{
					requestContext.Logger.Set(3, "WSSecurityRequest-SID");
					return new SidAnchorMailbox(securityIdentifier, requestContext);
				}
			}
			if (SmtpAddress.IsValidDomain(address))
			{
				requestContext.Logger.Set(3, "WSSecurityRequest-Domain");
				return new DomainAnchorMailbox(address, requestContext);
			}
			throw new HttpProxyException(HttpStatusCode.Unauthorized, 4002, string.Format("Cannot authenticate user address claim {0}", address));
		}

		// Token: 0x060000EA RID: 234 RVA: 0x00005B50 File Offset: 0x00003D50
		private static AnchorMailbox TryCreateFromCommonAccessToken(CommonAccessToken cat, IRequestContext requestContext)
		{
			AccessTokenType accessTokenType = (AccessTokenType)Enum.Parse(typeof(AccessTokenType), cat.TokenType, true);
			if (accessTokenType == 5)
			{
				requestContext.Logger.SafeSet(3, "CommonAccessToken-CompositeIdentity");
				cat = CommonAccessToken.Deserialize(cat.ExtensionData["PrimaryIdentityToken"]);
				accessTokenType = (AccessTokenType)Enum.Parse(typeof(AccessTokenType), cat.TokenType, true);
			}
			switch (accessTokenType)
			{
			case 0:
				requestContext.Logger.SafeSet(3, "CommonAccessToken-Windows");
				return new SidAnchorMailbox(cat.WindowsAccessToken.UserSid, requestContext);
			case 1:
			{
				LiveIdFbaTokenAccessor liveIdFbaTokenAccessor = LiveIdFbaTokenAccessor.Attach(cat);
				requestContext.Logger.SafeSet(3, "CommonAccessToken-LiveId");
				return new SidAnchorMailbox(liveIdFbaTokenAccessor.UserSid, requestContext)
				{
					OrganizationId = liveIdFbaTokenAccessor.OrganizationId,
					SmtpOrLiveId = liveIdFbaTokenAccessor.LiveIdMemberName
				};
			}
			case 2:
			{
				LiveIdBasicTokenAccessor liveIdBasicTokenAccessor = LiveIdBasicTokenAccessor.Attach(cat);
				requestContext.Logger.SafeSet(3, "CommonAccessToken-LiveIdBasic");
				if (liveIdBasicTokenAccessor.UserSid != null)
				{
					return new SidAnchorMailbox(liveIdBasicTokenAccessor.UserSid, requestContext)
					{
						OrganizationId = liveIdBasicTokenAccessor.OrganizationId,
						SmtpOrLiveId = liveIdBasicTokenAccessor.LiveIdMemberName
					};
				}
				if (SmtpAddress.IsValidSmtpAddress(liveIdBasicTokenAccessor.LiveIdMemberName))
				{
					string domain = SmtpAddress.Parse(liveIdBasicTokenAccessor.LiveIdMemberName).Domain;
					return new PuidAnchorMailbox(liveIdBasicTokenAccessor.Puid, domain, requestContext);
				}
				return null;
			}
			case 3:
			{
				string sid = cat.ExtensionData["UserSid"];
				string text;
				cat.ExtensionData.TryGetValue("OrganizationName", out text);
				string smtpOrLiveId;
				cat.ExtensionData.TryGetValue("MemberName", out smtpOrLiveId);
				if (!string.IsNullOrEmpty(text) && requestContext.Logger != null)
				{
					requestContext.Logger.ActivityScope.SetProperty(5, text);
				}
				requestContext.Logger.SafeSet(3, "CommonAccessToken-LiveIdNego2");
				return new SidAnchorMailbox(sid, requestContext)
				{
					SmtpOrLiveId = smtpOrLiveId
				};
			}
			case 4:
				return null;
			case 6:
				return null;
			case 7:
			{
				ADRawEntry httpContextADRawEntry = AuthCommon.GetHttpContextADRawEntry(requestContext.HttpContext);
				if (httpContextADRawEntry != null)
				{
					requestContext.Logger.SafeSet(3, "CommonAccessToken-CertificateSid");
					return new UserADRawEntryAnchorMailbox(httpContextADRawEntry, requestContext);
				}
				CertificateSidTokenAccessor certificateSidTokenAccessor = CertificateSidTokenAccessor.Attach(cat);
				requestContext.Logger.SafeSet(3, "CommonAccessToken-CertificateSid");
				return new SidAnchorMailbox(certificateSidTokenAccessor.UserSid, requestContext)
				{
					PartitionId = certificateSidTokenAccessor.PartitionId
				};
			}
			case 8:
				return null;
			}
			return null;
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00005DD4 File Offset: 0x00003FD4
		private static AnchorMailbox CreateFromLogonIdentity(IRequestContext requestContext)
		{
			HttpContext httpContext = requestContext.HttpContext;
			IPrincipal user = httpContext.User;
			IIdentity identity = httpContext.User.Identity;
			string text;
			HttpContextItemParser.TryGetLiveIdMemberName(httpContext.Items, ref text);
			OAuthIdentity oauthIdentity = identity as OAuthIdentity;
			if (oauthIdentity != null)
			{
				string externalDirectoryObjectId;
				if (RequestHeaderParser.TryGetExternalDirectoryObjectId(httpContext.Request.Headers, ref externalDirectoryObjectId))
				{
					requestContext.Logger.SafeSet(3, "OAuthIdentity-ExternalDirectoryObjectId");
					return new ExternalDirectoryObjectIdAnchorMailbox(externalDirectoryObjectId, oauthIdentity.OrganizationId, requestContext);
				}
				if (oauthIdentity.ActAsUser != null)
				{
					requestContext.Logger.SafeSet(3, "OAuthIdentity-ActAsUser");
					return new OAuthActAsUserAnchorMailbox(oauthIdentity.ActAsUser, requestContext);
				}
				requestContext.Logger.SafeSet(3, "OAuthIdentity-AppOrganization");
				return new OrganizationAnchorMailbox(oauthIdentity.OrganizationId, requestContext);
			}
			else
			{
				GenericSidIdentity genericSidIdentity = identity as GenericSidIdentity;
				if (genericSidIdentity != null)
				{
					requestContext.Logger.SafeSet(3, "GenericSidIdentity");
					return new SidAnchorMailbox(genericSidIdentity.Sid, requestContext)
					{
						PartitionId = genericSidIdentity.PartitionId,
						SmtpOrLiveId = text
					};
				}
				DelegatedPrincipal delegatedPrincipal = user as DelegatedPrincipal;
				if (delegatedPrincipal != null && delegatedPrincipal.DelegatedOrganization != null && string.IsNullOrEmpty(text))
				{
					requestContext.Logger.SafeSet(3, "DelegatedPrincipal-DelegatedOrganization");
					return new DomainAnchorMailbox(delegatedPrincipal.DelegatedOrganization, requestContext);
				}
				WindowsIdentity windowsIdentity = identity as WindowsIdentity;
				if (windowsIdentity != null)
				{
					if (string.IsNullOrEmpty(text))
					{
						requestContext.Logger.SafeSet(3, "WindowsIdentity");
					}
					else
					{
						requestContext.Logger.SafeSet(3, "WindowsIdentity-LiveIdMemberName");
					}
					return new SidAnchorMailbox(windowsIdentity.User, requestContext)
					{
						SmtpOrLiveId = text
					};
				}
				SecurityIdentifier securityIdentifier = null;
				if (IIdentityExtensions.TryGetSecurityIdentifier(identity, ref securityIdentifier) && !securityIdentifier.Equals(AuthCommon.MemberNameNullSid))
				{
					if (string.IsNullOrEmpty(text))
					{
						requestContext.Logger.SafeSet(3, "SID");
					}
					else
					{
						requestContext.Logger.SafeSet(3, "SID-LiveIdMemberName");
					}
					return new SidAnchorMailbox(securityIdentifier, requestContext)
					{
						SmtpOrLiveId = text
					};
				}
				if (!HttpProxySettings.IdentityIndependentAuthBehaviorEnabled.Value && requestContext.AuthBehavior.AuthState != AuthState.FrontEndFullAuth)
				{
					AnchorMailbox anchorMailbox = requestContext.AuthBehavior.CreateAuthModuleSpecificAnchorMailbox(requestContext);
					if (anchorMailbox != null)
					{
						return anchorMailbox;
					}
				}
				if (!string.IsNullOrEmpty(text) && SmtpAddress.IsValidSmtpAddress(text))
				{
					requestContext.Logger.SafeSet(3, "Smtp-LiveIdMemberName");
					return new SmtpAnchorMailbox(text, requestContext);
				}
				throw new InvalidOperationException(string.Format("Unknown idenity {0} with type {1}.", IIdentityExtensions.GetSafeName(identity, true), identity.ToString()));
			}
		}
	}
}
