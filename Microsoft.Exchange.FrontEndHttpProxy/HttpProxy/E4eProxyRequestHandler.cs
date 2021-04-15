using System;
using System.Net;
using System.Web;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200008F RID: 143
	internal class E4eProxyRequestHandler : ProxyRequestHandler
	{
		// Token: 0x060004D7 RID: 1239 RVA: 0x0001AA35 File Offset: 0x00018C35
		internal E4eProxyRequestHandler()
		{
		}

		// Token: 0x1700011A RID: 282
		// (get) Token: 0x060004D8 RID: 1240 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool WillAddProtocolSpecificCookiesToClientResponse
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060004D9 RID: 1241 RVA: 0x0001AB87 File Offset: 0x00018D87
		internal static bool IsE4ePayloadRequest(HttpRequest request)
		{
			return request.FilePath.EndsWith("store.ashx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004DA RID: 1242 RVA: 0x0001AB9A File Offset: 0x00018D9A
		internal static bool IsE4eRetrieveRequest(HttpRequest request)
		{
			return request.FilePath.EndsWith("Retrieve.ashx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004DB RID: 1243 RVA: 0x0001ABB0 File Offset: 0x00018DB0
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			if (E4eProxyRequestHandler.IsAnonymousErrorPageRequest(base.ClientRequest) || E4eProxyRequestHandler.IsAnonymousAppRedirectPageRequest(base.ClientRequest) || E4eProxyRequestHandler.IsAnonymousAppFeedbackRequest(base.ClientRequest))
			{
				return new AnonymousAnchorMailbox(this);
			}
			if (E4eProxyRequestHandler.IsE4eInvalidStoreRequest(base.ClientRequest))
			{
				this.ThrowRedirectException(E4eProxyRequestHandler.GetErrorUrl(E4eProxyRequestHandler.E4eErrorType.InvalidStoreRequest));
			}
			bool flag = E4eProxyRequestHandler.IsE4ePostOrRetrievePayloadRequest(base.ClientRequest);
			bool flag2 = this.IsWACRequest();
			this.GetSenderInfo(flag, flag2);
			string text = this.routingEmailAddress;
			if (E4eProxyRequestHandler.IsRESTAPIUploadRequset(base.ClientRequest) && string.IsNullOrEmpty(text))
			{
				return new AnonymousAnchorMailbox(this);
			}
			if (string.IsNullOrEmpty(text) || !SmtpAddress.IsValidSmtpAddress(text))
			{
				if (BEResourceRequestHandler.IsResourceRequest(base.ClientRequest.Url.LocalPath))
				{
					return new AnonymousAnchorMailbox(this);
				}
				string text2 = string.Format("The routing email address is not valid. Email={0}", text);
				base.Logger.AppendGenericError("Invalid routing email address", text2);
				throw new HttpProxyException(HttpStatusCode.NotFound, 3001, text2);
			}
			else
			{
				if (flag)
				{
					string recipientEmailAddress = base.ClientRequest.QueryString["RecipientEmailAddress"];
					if (E4eBackoffListCache.Instance.ShouldBackOff(this.senderEmailAddress, recipientEmailAddress))
					{
						PerfCounters.HttpProxyCountersInstance.RejectedConnectionCount.Increment();
						this.ThrowRedirectException(E4eProxyRequestHandler.GetErrorUrl(E4eProxyRequestHandler.E4eErrorType.ThrottlingRestriction));
					}
					else
					{
						PerfCounters.HttpProxyCountersInstance.AcceptedConnectionCount.Increment();
					}
				}
				if (!this.IsBackEndCookieAndHeaderFlightEnabled())
				{
					return this.GetAnchorMailbox();
				}
				if (flag)
				{
					string text3 = base.ClientRequest.Headers["X-E4ePostToBackEnd"];
					if (!string.IsNullOrEmpty(text3))
					{
						return new ServerInfoAnchorMailbox(text3, this);
					}
					return this.GetAnchorMailbox();
				}
				else if (flag2)
				{
					string text4;
					string text5;
					if (!this.GetRoutingInformationForWac(out text4, out text5))
					{
						base.Logger.AppendGenericError("E4EWacRequest", "Invalid routing information for request coming from WAC server, url:" + base.ClientRequest.Url);
						return this.GetAnchorMailbox();
					}
					AnchorMailbox result = new ServerInfoAnchorMailbox(text4, this);
					base.Logger.AppendGenericInfo("E4eBEServerCookieHint", string.Format("Using BE server cookie hint. Cookie value [{0}]", text4));
					return result;
				}
				else
				{
					if (!E4eProxyRequestHandler.IsRequestBoundToBEServer(base.ClientRequest))
					{
						return this.GetAnchorMailbox();
					}
					string backendFqdnFromE4eCookie = E4eProxyRequestHandler.GetBackendFqdnFromE4eCookie(base.ClientRequest, base.Logger);
					if (string.IsNullOrEmpty(backendFqdnFromE4eCookie))
					{
						return this.GetAnchorMailbox();
					}
					AnchorMailbox result2 = new ServerInfoAnchorMailbox(backendFqdnFromE4eCookie, this);
					base.Logger.AppendGenericInfo("E4eBEServerCookieHint", string.Format("Using BE server cookie hint. Cookie value [{0}]", backendFqdnFromE4eCookie));
					return result2;
				}
			}
		}

		// Token: 0x060004DC RID: 1244 RVA: 0x0001ADF8 File Offset: 0x00018FF8
		protected override bool HandleBackEndCalculationException(Exception exception, AnchorMailbox anchorMailbox, string label)
		{
			HttpProxyException ex = exception as HttpProxyException;
			if (ex != null && ex.ErrorCode == 3009)
			{
				HttpException exception2 = new HttpException(302, E4eProxyRequestHandler.GetErrorUrl(E4eProxyRequestHandler.E4eErrorType.OrgNotExisting));
				return base.HandleBackEndCalculationException(exception2, anchorMailbox, label);
			}
			return base.HandleBackEndCalculationException(exception, anchorMailbox, label);
		}

		// Token: 0x060004DD RID: 1245 RVA: 0x0001AE40 File Offset: 0x00019040
		protected override bool ShouldCopyCookieToClientResponse(Cookie cookie)
		{
			return !cookie.Name.Equals("X-E4eBudgetType", StringComparison.OrdinalIgnoreCase) && !cookie.Name.Equals("X-E4eEmailAddress", StringComparison.OrdinalIgnoreCase) && !cookie.Name.Equals("X-E4eBackOffUntilUtc", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004DE RID: 1246 RVA: 0x0001AE80 File Offset: 0x00019080
		protected override void CopySupplementalCookiesToClientResponse()
		{
			this.UpdateBackoffCache();
			if (!string.IsNullOrEmpty(this.senderEmailAddress))
			{
				HttpCookie httpCookie = new HttpCookie("X-SenderEmailAddress", this.senderEmailAddress);
				httpCookie.HttpOnly = true;
				httpCookie.Secure = base.ClientRequest.IsSecureConnection;
				base.ClientResponse.Cookies.Add(httpCookie);
			}
			if (!string.IsNullOrEmpty(this.senderOrganization))
			{
				HttpCookie httpCookie2 = new HttpCookie("X-SenderOrganization", this.senderOrganization);
				httpCookie2.HttpOnly = true;
				httpCookie2.Secure = base.ClientRequest.IsSecureConnection;
				base.ClientResponse.Cookies.Add(httpCookie2);
			}
			if (!string.IsNullOrWhiteSpace(this.routingEmailAddress))
			{
				HttpCookie httpCookie3 = new HttpCookie("X-RoutingEmailAddress", this.routingEmailAddress);
				httpCookie3.HttpOnly = true;
				httpCookie3.Secure = base.ClientRequest.IsSecureConnection;
				base.ClientResponse.Cookies.Add(httpCookie3);
				HttpCookie httpCookie4 = new HttpCookie("DefaultAnchorMailbox", this.routingEmailAddress);
				httpCookie4.HttpOnly = true;
				httpCookie4.Secure = base.ClientRequest.IsSecureConnection;
				base.ClientResponse.Cookies.Add(httpCookie4);
			}
			if (base.AnchoredRoutingTarget != null && E4eProxyRequestHandler.IsE4ePostOrRetrievePayloadRequest(base.ClientRequest) && this.IsBackEndCookieAndHeaderFlightEnabled())
			{
				string itemIdFromCookie = E4eProxyRequestHandler.GetItemIdFromCookie(base.ServerResponse);
				if (itemIdFromCookie != null)
				{
					string fqdn = base.AnchoredRoutingTarget.BackEndServer.Fqdn;
					string value = string.Format("{0}{1}{2}", itemIdFromCookie, '~', fqdn);
					HttpCookie httpCookie5 = new HttpCookie("X-E4eBackEnd", value);
					httpCookie5.HttpOnly = true;
					httpCookie5.Secure = base.ClientRequest.IsSecureConnection;
					base.ClientResponse.Cookies.Add(httpCookie5);
				}
			}
			base.CopySupplementalCookiesToClientResponse();
		}

		// Token: 0x060004DF RID: 1247 RVA: 0x0001B03C File Offset: 0x0001923C
		private static bool IsServerMailboxReachable(ServerInfoAnchorMailbox mailbox)
		{
			try
			{
				mailbox.TryDirectBackEndCalculation();
			}
			catch (ServerNotFoundException)
			{
				return false;
			}
			return true;
		}

		// Token: 0x060004E0 RID: 1248 RVA: 0x0001B06C File Offset: 0x0001926C
		private static bool IsE4ePostOrRetrievePayloadRequest(HttpRequest request)
		{
			bool flag = request.HttpMethod.Equals(HttpMethod.Post.ToString(), StringComparison.OrdinalIgnoreCase) && E4eProxyRequestHandler.IsE4ePayloadRequest(request);
			bool flag2 = E4eProxyRequestHandler.IsRESTAPIUploadRequset(request);
			bool flag3 = E4eProxyRequestHandler.IsE4eRetrieveRequest(request);
			return flag || flag2 || flag3;
		}

		// Token: 0x060004E1 RID: 1249 RVA: 0x0001B0B4 File Offset: 0x000192B4
		private static bool IsRESTAPIUploadRequset(HttpRequest request)
		{
			UriTemplate uriTemplate = new UriTemplate("mail/upload");
			Uri baseAddress = new Uri(request.Url.GetLeftPart(UriPartial.Authority) + request.FilePath);
			Uri url = request.Url;
			return uriTemplate.Match(baseAddress, url) != null;
		}

		// Token: 0x060004E2 RID: 1250 RVA: 0x0001B0FC File Offset: 0x000192FC
		private static bool IsE4eInvalidStoreRequest(HttpRequest request)
		{
			return request.HttpMethod.Equals(HttpMethod.Get.ToString(), StringComparison.OrdinalIgnoreCase) && E4eProxyRequestHandler.IsE4ePayloadRequest(request);
		}

		// Token: 0x060004E3 RID: 1251 RVA: 0x0001B130 File Offset: 0x00019330
		private static bool IsErrorPageRequest(HttpRequest request)
		{
			return request.HttpMethod.Equals(HttpMethod.Get.ToString(), StringComparison.OrdinalIgnoreCase) && request.FilePath.EndsWith("ErrorPage.aspx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004E4 RID: 1252 RVA: 0x0001B170 File Offset: 0x00019370
		private static bool IsAnonymousErrorPageRequest(HttpRequest request)
		{
			E4eProxyRequestHandler.E4eErrorType e4eErrorType;
			return E4eProxyRequestHandler.IsErrorPageRequest(request) && Enum.TryParse<E4eProxyRequestHandler.E4eErrorType>(request.QueryString["code"], true, out e4eErrorType) && (e4eErrorType == E4eProxyRequestHandler.E4eErrorType.OrgNotExisting || e4eErrorType == E4eProxyRequestHandler.E4eErrorType.InvalidStoreRequest);
		}

		// Token: 0x060004E5 RID: 1253 RVA: 0x0001B1B0 File Offset: 0x000193B0
		private static bool IsAnonymousAppRedirectPageRequest(HttpRequest request)
		{
			return request.HttpMethod.Equals(HttpMethod.Get.ToString(), StringComparison.OrdinalIgnoreCase) && request.FilePath.EndsWith("RedirectToOMEViewer.ashx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004E6 RID: 1254 RVA: 0x0001B1F0 File Offset: 0x000193F0
		private static bool IsAnonymousAppFeedbackRequest(HttpRequest request)
		{
			return request.HttpMethod.Equals(HttpMethod.Post.ToString(), StringComparison.OrdinalIgnoreCase) && request.FilePath.EndsWith("SendOMEFeedback.ashx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004E7 RID: 1255 RVA: 0x0001B22D File Offset: 0x0001942D
		private static bool IsRequestBoundToBEServer(HttpRequest request)
		{
			return !E4eProxyRequestHandler.IsErrorPageRequest(request) && !BEResourceRequestHandler.IsResourceRequest(request.Url.LocalPath);
		}

		// Token: 0x060004E8 RID: 1256 RVA: 0x0001B24C File Offset: 0x0001944C
		private static string GetErrorUrl(E4eProxyRequestHandler.E4eErrorType type)
		{
			string text = string.Format("/Encryption/ErrorPage.aspx?src={0}&code={1}", 0, (int)type);
			try
			{
				string member = HttpProxyGlobals.LocalMachineFqdn.Member;
				if (!string.IsNullOrEmpty(member))
				{
					text = text + "&fe=" + HttpUtility.UrlEncode(member);
				}
			}
			catch (Exception)
			{
			}
			return text;
		}

		// Token: 0x060004E9 RID: 1257 RVA: 0x0001B2AC File Offset: 0x000194AC
		private static string GetBackendFqdnFromE4eCookie(HttpRequest request, RequestDetailsLogger logger)
		{
			HttpCookie httpCookie = request.Cookies["X-E4eBackEnd"];
			if (httpCookie == null)
			{
				return null;
			}
			string value = httpCookie.Value;
			if (string.IsNullOrEmpty(value))
			{
				logger.AppendGenericError("E4eBeCookieNullOrEmpty", "The E4E backend cookie hint was found but is null or empty.");
				return null;
			}
			string[] array = httpCookie.Value.Split(new char[]
			{
				'~'
			});
			if (array.Length != 2)
			{
				logger.AppendGenericError("E4eBeCookieBadValue", string.Format("The E4E backend cookie hint was found but does not have expected value. Actual value [{0}]", value));
				return null;
			}
			string value2 = array[0];
			string result = array[1];
			string itemIdFromCookie = E4eProxyRequestHandler.GetItemIdFromCookie(request);
			if (string.IsNullOrEmpty(itemIdFromCookie) || !itemIdFromCookie.Equals(value2, StringComparison.OrdinalIgnoreCase))
			{
				logger.AppendGenericInfo("E4eBeCookieStale", string.Format("The E4E backend cookie hint was found but does not match current item id. Cookie value [{0}], current item ID [{1}]", value, itemIdFromCookie));
				return null;
			}
			return result;
		}

		// Token: 0x060004EA RID: 1258 RVA: 0x0001B368 File Offset: 0x00019568
		private static string GetItemIdFromCookie(HttpRequest request)
		{
			HttpCookie httpCookie = request.Cookies["X-OTPItemId"];
			if (httpCookie != null)
			{
				return httpCookie.Value;
			}
			return null;
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x0001B394 File Offset: 0x00019594
		private static string GetItemIdFromCookie(HttpWebResponse serverResponse)
		{
			Cookie cookie = serverResponse.Cookies["X-OTPItemId"];
			if (cookie != null)
			{
				return cookie.Value;
			}
			return null;
		}

		// Token: 0x060004EC RID: 1260 RVA: 0x0001B3C0 File Offset: 0x000195C0
		private void GetSenderInfo(bool isE4ePostOrRetrievePayloadRequest, bool isWacRequest)
		{
			if (isE4ePostOrRetrievePayloadRequest)
			{
				this.senderEmailAddress = base.ClientRequest.QueryString["SenderEmailAddress"];
				this.senderOrganization = base.ClientRequest.QueryString["SenderOrganization"];
				string value = base.ClientRequest.QueryString["cfmRecipient"];
				if (!string.IsNullOrEmpty(value))
				{
					this.routingEmailAddress = value;
				}
				else
				{
					this.routingEmailAddress = base.ClientRequest.QueryString["routingEmailAddress"];
				}
				if (string.IsNullOrEmpty(this.routingEmailAddress))
				{
					this.routingEmailAddress = this.senderEmailAddress;
				}
				base.Logger.Set(3, "SMTP-EmailAddressFromUrlQuery");
				return;
			}
			if (!isWacRequest)
			{
				HttpCookie httpCookie = base.ClientRequest.Cookies["X-SenderEmailAddress"];
				this.senderEmailAddress = ((httpCookie == null) ? null : httpCookie.Value);
				HttpCookie httpCookie2 = base.ClientRequest.Cookies["X-SenderOrganization"];
				this.senderOrganization = ((httpCookie2 == null) ? null : httpCookie2.Value);
				HttpCookie httpCookie3 = base.ClientRequest.Cookies["X-RoutingEmailAddress"];
				this.routingEmailAddress = ((httpCookie3 == null) ? this.senderEmailAddress : httpCookie3.Value);
				base.Logger.Set(3, "SMTP-EmailAddressFromCookie");
				return;
			}
			string text;
			string text2;
			if (!this.GetRoutingInformationForWac(out text, out text2))
			{
				base.Logger.AppendGenericError("E4EWacRequest", "Invalid routing information for request coming from WAC server, url:" + base.ClientRequest.Url);
				return;
			}
			this.routingEmailAddress = text2;
		}

		// Token: 0x060004ED RID: 1261 RVA: 0x0001B554 File Offset: 0x00019754
		private void UpdateBackoffCache()
		{
			if (E4eProxyRequestHandler.IsE4ePostOrRetrievePayloadRequest(base.ClientRequest))
			{
				string serverResponseCookieValue = this.GetServerResponseCookieValue("X-E4eBudgetType");
				string serverResponseCookieValue2 = this.GetServerResponseCookieValue("X-E4eEmailAddress");
				string serverResponseCookieValue3 = this.GetServerResponseCookieValue("X-E4eBackOffUntilUtc");
				E4eBackoffListCache.Instance.UpdateCache(serverResponseCookieValue, serverResponseCookieValue2, serverResponseCookieValue3);
			}
		}

		// Token: 0x060004EE RID: 1262 RVA: 0x0001B5A0 File Offset: 0x000197A0
		private string GetServerResponseCookieValue(string cookieName)
		{
			Cookie cookie = base.ServerResponse.Cookies[cookieName];
			if (cookie != null)
			{
				return cookie.Value;
			}
			return string.Empty;
		}

		// Token: 0x060004EF RID: 1263 RVA: 0x0001B5CE File Offset: 0x000197CE
		private bool IsWACRequest()
		{
			return base.ClientRequest.Url.Segments.Length > 4 && string.Equals(base.ClientRequest.Url.Segments[2], "wopi/", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004F0 RID: 1264 RVA: 0x0001B608 File Offset: 0x00019808
		private bool GetRoutingInformationForWac(out string targetBEMachine, out string routingEmailAddress)
		{
			targetBEMachine = string.Empty;
			routingEmailAddress = string.Empty;
			string text = base.ClientRequest.QueryString["access_token"];
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			string[] array = text.Split(new string[]
			{
				"Authenticator"
			}, StringSplitOptions.None);
			if (array.Length != 4)
			{
				return false;
			}
			routingEmailAddress = array[0];
			targetBEMachine = array[1];
			return true;
		}

		// Token: 0x060004F1 RID: 1265 RVA: 0x0001B66C File Offset: 0x0001986C
		private void ThrowRedirectException(string redirectUrl)
		{
			if (!string.IsNullOrEmpty(this.senderEmailAddress))
			{
				HttpCookie httpCookie = new HttpCookie("X-SenderEmailAddress", this.senderEmailAddress);
				httpCookie.HttpOnly = true;
				httpCookie.Secure = base.ClientRequest.IsSecureConnection;
				base.ClientResponse.Cookies.Add(httpCookie);
			}
			if (!string.IsNullOrEmpty(this.senderOrganization))
			{
				HttpCookie httpCookie2 = new HttpCookie("X-SenderOrganization", this.senderOrganization);
				httpCookie2.HttpOnly = true;
				httpCookie2.Secure = base.ClientRequest.IsSecureConnection;
				base.ClientResponse.Cookies.Add(httpCookie2);
			}
			if (!string.IsNullOrEmpty(this.routingEmailAddress))
			{
				HttpCookie httpCookie3 = new HttpCookie("X-RoutingEmailAddress", this.routingEmailAddress);
				httpCookie3.HttpOnly = true;
				httpCookie3.Secure = base.ClientRequest.IsSecureConnection;
				base.ClientResponse.Cookies.Add(httpCookie3);
			}
			throw new HttpException(302, redirectUrl);
		}

		// Token: 0x060004F2 RID: 1266 RVA: 0x0001B759 File Offset: 0x00019959
		private AnchorMailbox GetAnchorMailbox()
		{
			return new SmtpWithDomainFallbackAnchorMailbox(this.routingEmailAddress, this)
			{
				UseServerCookie = true
			};
		}

		// Token: 0x060004F3 RID: 1267 RVA: 0x0001B76E File Offset: 0x0001996E
		private bool IsBackEndCookieAndHeaderFlightEnabled()
		{
			return CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).E4eBackendCookieHint.Enabled;
		}

		// Token: 0x0400033E RID: 830
		private string senderEmailAddress;

		// Token: 0x0400033F RID: 831
		private string senderOrganization;

		// Token: 0x04000340 RID: 832
		private string routingEmailAddress;

		// Token: 0x0200011F RID: 287
		private enum E4eErrorType
		{
			// Token: 0x0400050D RID: 1293
			GenericError,
			// Token: 0x0400050E RID: 1294
			ConfigError,
			// Token: 0x0400050F RID: 1295
			ThrottlingRestriction,
			// Token: 0x04000510 RID: 1296
			OrgNotExisting,
			// Token: 0x04000511 RID: 1297
			AuthenticationFailure,
			// Token: 0x04000512 RID: 1298
			UploadFailure,
			// Token: 0x04000513 RID: 1299
			ClientFailure,
			// Token: 0x04000514 RID: 1300
			InvalidCredentials,
			// Token: 0x04000515 RID: 1301
			InvalidEmailAddress,
			// Token: 0x04000516 RID: 1302
			InvalidMetadata,
			// Token: 0x04000517 RID: 1303
			InvalidMessage,
			// Token: 0x04000518 RID: 1304
			MessageNotFound,
			// Token: 0x04000519 RID: 1305
			MessageNotAuthorized,
			// Token: 0x0400051A RID: 1306
			TransientFailure,
			// Token: 0x0400051B RID: 1307
			SessionTimeout,
			// Token: 0x0400051C RID: 1308
			ProbeRequest,
			// Token: 0x0400051D RID: 1309
			ClientException,
			// Token: 0x0400051E RID: 1310
			InvalidStoreRequest,
			// Token: 0x0400051F RID: 1311
			OTPSendPerSession,
			// Token: 0x04000520 RID: 1312
			OTPSendAcrossSession,
			// Token: 0x04000521 RID: 1313
			OTPAttemptPerSession,
			// Token: 0x04000522 RID: 1314
			OTPAttemptAcrossSession,
			// Token: 0x04000523 RID: 1315
			OTPDisabled,
			// Token: 0x04000524 RID: 1316
			OTPPasscodeExpired,
			// Token: 0x04000525 RID: 1317
			UnsupportedBrowser,
			// Token: 0x04000526 RID: 1318
			MessageExpired,
			// Token: 0x04000527 RID: 1319
			OTPPasscodeNotCorrect,
			// Token: 0x04000528 RID: 1320
			RESTAPIDisabled,
			// Token: 0x04000529 RID: 1321
			FeatureDisabled,
			// Token: 0x0400052A RID: 1322
			UnknownFailure,
			// Token: 0x0400052B RID: 1323
			InvalidRequest
		}

		// Token: 0x02000120 RID: 288
		private enum E4eErrorSource
		{
			// Token: 0x0400052D RID: 1325
			Store,
			// Token: 0x0400052E RID: 1326
			Auth,
			// Token: 0x0400052F RID: 1327
			Backend,
			// Token: 0x04000530 RID: 1328
			Client,
			// Token: 0x04000531 RID: 1329
			Generic,
			// Token: 0x04000532 RID: 1330
			OTP,
			// Token: 0x04000533 RID: 1331
			Retrieve,
			// Token: 0x04000534 RID: 1332
			ExternalOAuth
		}

		// Token: 0x02000121 RID: 289
		private class E4eConstants
		{
			// Token: 0x04000535 RID: 1333
			public const string ErrorPage = "ErrorPage.aspx";

			// Token: 0x04000536 RID: 1334
			public const string RedirectToAppPage = "RedirectToOMEViewer.ashx";

			// Token: 0x04000537 RID: 1335
			public const string AppFeedbackPage = "SendOMEFeedback.ashx";

			// Token: 0x04000538 RID: 1336
			public const string ErrorCode = "code";

			// Token: 0x04000539 RID: 1337
			public const string PostPayloadFilePath = "store.ashx";

			// Token: 0x0400053A RID: 1338
			public const string RetrievePayloadFilePath = "Retrieve.ashx";

			// Token: 0x0400053B RID: 1339
			public const string RecipientEmailAddress = "RecipientEmailAddress";

			// Token: 0x0400053C RID: 1340
			public const string SenderEmailAddress = "SenderEmailAddress";

			// Token: 0x0400053D RID: 1341
			public const string SenderOrganization = "SenderOrganization";

			// Token: 0x0400053E RID: 1342
			public const string TokenSeparatorForWacRequests = "Authenticator";

			// Token: 0x0400053F RID: 1343
			public const string WacRequestParameter = "wopi/";

			// Token: 0x04000540 RID: 1344
			public const string CfmRecipient = "cfmRecipient";

			// Token: 0x04000541 RID: 1345
			public const string RoutingEmailAddress = "routingEmailAddress";

			// Token: 0x04000542 RID: 1346
			public const string XSenderEmailAddress = "X-SenderEmailAddress";

			// Token: 0x04000543 RID: 1347
			public const string XSenderOrganization = "X-SenderOrganization";

			// Token: 0x04000544 RID: 1348
			public const string XRoutingEmailAddress = "X-RoutingEmailAddress";

			// Token: 0x04000545 RID: 1349
			public const string XBudgetTypeCookieName = "X-E4eBudgetType";

			// Token: 0x04000546 RID: 1350
			public const string XEmailAddressCookieName = "X-E4eEmailAddress";

			// Token: 0x04000547 RID: 1351
			public const string XBackoffUntilUtcCookieName = "X-E4eBackOffUntilUtc";

			// Token: 0x04000548 RID: 1352
			public const string XBackEndCookieName = "X-E4eBackEnd";

			// Token: 0x04000549 RID: 1353
			public const string XBackEndHeaderName = "X-E4ePostToBackEnd";

			// Token: 0x0400054A RID: 1354
			public const string XItemIdCookieName = "X-OTPItemId";

			// Token: 0x0400054B RID: 1355
			public const char BackendCookieValueDelimiter = '~';
		}
	}
}
