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
		// Token: 0x060004DB RID: 1243 RVA: 0x0001ABF5 File Offset: 0x00018DF5
		internal E4eProxyRequestHandler()
		{
		}

		// Token: 0x1700011A RID: 282
		// (get) Token: 0x060004DC RID: 1244 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool WillAddProtocolSpecificCookiesToClientResponse
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060004DD RID: 1245 RVA: 0x0001AD47 File Offset: 0x00018F47
		internal static bool IsE4ePayloadRequest(HttpRequest request)
		{
			return request.FilePath.EndsWith("store.ashx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004DE RID: 1246 RVA: 0x0001AD5A File Offset: 0x00018F5A
		internal static bool IsE4eRetrieveRequest(HttpRequest request)
		{
			return request.FilePath.EndsWith("Retrieve.ashx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004DF RID: 1247 RVA: 0x0001AD70 File Offset: 0x00018F70
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

		// Token: 0x060004E0 RID: 1248 RVA: 0x0001AFB8 File Offset: 0x000191B8
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

		// Token: 0x060004E1 RID: 1249 RVA: 0x0001B000 File Offset: 0x00019200
		protected override bool ShouldCopyCookieToClientResponse(Cookie cookie)
		{
			return !cookie.Name.Equals("X-E4eBudgetType", StringComparison.OrdinalIgnoreCase) && !cookie.Name.Equals("X-E4eEmailAddress", StringComparison.OrdinalIgnoreCase) && !cookie.Name.Equals("X-E4eBackOffUntilUtc", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004E2 RID: 1250 RVA: 0x0001B040 File Offset: 0x00019240
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

		// Token: 0x060004E3 RID: 1251 RVA: 0x0001B1FC File Offset: 0x000193FC
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

		// Token: 0x060004E4 RID: 1252 RVA: 0x0001B22C File Offset: 0x0001942C
		private static bool IsE4ePostOrRetrievePayloadRequest(HttpRequest request)
		{
			bool flag = request.HttpMethod.Equals(HttpMethod.Post.ToString(), StringComparison.OrdinalIgnoreCase) && E4eProxyRequestHandler.IsE4ePayloadRequest(request);
			bool flag2 = E4eProxyRequestHandler.IsRESTAPIUploadRequset(request);
			bool flag3 = E4eProxyRequestHandler.IsE4eRetrieveRequest(request);
			return flag || flag2 || flag3;
		}

		// Token: 0x060004E5 RID: 1253 RVA: 0x0001B274 File Offset: 0x00019474
		private static bool IsRESTAPIUploadRequset(HttpRequest request)
		{
			UriTemplate uriTemplate = new UriTemplate("mail/upload");
			Uri baseAddress = new Uri(request.Url.GetLeftPart(UriPartial.Authority) + request.FilePath);
			Uri url = request.Url;
			return uriTemplate.Match(baseAddress, url) != null;
		}

		// Token: 0x060004E6 RID: 1254 RVA: 0x0001B2BC File Offset: 0x000194BC
		private static bool IsE4eInvalidStoreRequest(HttpRequest request)
		{
			return request.HttpMethod.Equals(HttpMethod.Get.ToString(), StringComparison.OrdinalIgnoreCase) && E4eProxyRequestHandler.IsE4ePayloadRequest(request);
		}

		// Token: 0x060004E7 RID: 1255 RVA: 0x0001B2F0 File Offset: 0x000194F0
		private static bool IsErrorPageRequest(HttpRequest request)
		{
			return request.HttpMethod.Equals(HttpMethod.Get.ToString(), StringComparison.OrdinalIgnoreCase) && request.FilePath.EndsWith("ErrorPage.aspx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004E8 RID: 1256 RVA: 0x0001B330 File Offset: 0x00019530
		private static bool IsAnonymousErrorPageRequest(HttpRequest request)
		{
			E4eProxyRequestHandler.E4eErrorType e4eErrorType;
			return E4eProxyRequestHandler.IsErrorPageRequest(request) && Enum.TryParse<E4eProxyRequestHandler.E4eErrorType>(request.QueryString["code"], true, out e4eErrorType) && (e4eErrorType == E4eProxyRequestHandler.E4eErrorType.OrgNotExisting || e4eErrorType == E4eProxyRequestHandler.E4eErrorType.InvalidStoreRequest);
		}

		// Token: 0x060004E9 RID: 1257 RVA: 0x0001B370 File Offset: 0x00019570
		private static bool IsAnonymousAppRedirectPageRequest(HttpRequest request)
		{
			return request.HttpMethod.Equals(HttpMethod.Get.ToString(), StringComparison.OrdinalIgnoreCase) && request.FilePath.EndsWith("RedirectToOMEViewer.ashx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004EA RID: 1258 RVA: 0x0001B3B0 File Offset: 0x000195B0
		private static bool IsAnonymousAppFeedbackRequest(HttpRequest request)
		{
			return request.HttpMethod.Equals(HttpMethod.Post.ToString(), StringComparison.OrdinalIgnoreCase) && request.FilePath.EndsWith("SendOMEFeedback.ashx", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x0001B3ED File Offset: 0x000195ED
		private static bool IsRequestBoundToBEServer(HttpRequest request)
		{
			return !E4eProxyRequestHandler.IsErrorPageRequest(request) && !BEResourceRequestHandler.IsResourceRequest(request.Url.LocalPath);
		}

		// Token: 0x060004EC RID: 1260 RVA: 0x0001B40C File Offset: 0x0001960C
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

		// Token: 0x060004ED RID: 1261 RVA: 0x0001B46C File Offset: 0x0001966C
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

		// Token: 0x060004EE RID: 1262 RVA: 0x0001B528 File Offset: 0x00019728
		private static string GetItemIdFromCookie(HttpRequest request)
		{
			HttpCookie httpCookie = request.Cookies["X-OTPItemId"];
			if (httpCookie != null)
			{
				return httpCookie.Value;
			}
			return null;
		}

		// Token: 0x060004EF RID: 1263 RVA: 0x0001B554 File Offset: 0x00019754
		private static string GetItemIdFromCookie(HttpWebResponse serverResponse)
		{
			Cookie cookie = serverResponse.Cookies["X-OTPItemId"];
			if (cookie != null)
			{
				return cookie.Value;
			}
			return null;
		}

		// Token: 0x060004F0 RID: 1264 RVA: 0x0001B580 File Offset: 0x00019780
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

		// Token: 0x060004F1 RID: 1265 RVA: 0x0001B714 File Offset: 0x00019914
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

		// Token: 0x060004F2 RID: 1266 RVA: 0x0001B760 File Offset: 0x00019960
		private string GetServerResponseCookieValue(string cookieName)
		{
			Cookie cookie = base.ServerResponse.Cookies[cookieName];
			if (cookie != null)
			{
				return cookie.Value;
			}
			return string.Empty;
		}

		// Token: 0x060004F3 RID: 1267 RVA: 0x0001B78E File Offset: 0x0001998E
		private bool IsWACRequest()
		{
			return base.ClientRequest.Url.Segments.Length > 4 && string.Equals(base.ClientRequest.Url.Segments[2], "wopi/", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004F4 RID: 1268 RVA: 0x0001B7C8 File Offset: 0x000199C8
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

		// Token: 0x060004F5 RID: 1269 RVA: 0x0001B82C File Offset: 0x00019A2C
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

		// Token: 0x060004F6 RID: 1270 RVA: 0x0001B919 File Offset: 0x00019B19
		private AnchorMailbox GetAnchorMailbox()
		{
			return new SmtpWithDomainFallbackAnchorMailbox(this.routingEmailAddress, this)
			{
				UseServerCookie = true
			};
		}

		// Token: 0x060004F7 RID: 1271 RVA: 0x0001B92E File Offset: 0x00019B2E
		private bool IsBackEndCookieAndHeaderFlightEnabled()
		{
			return CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).E4eBackendCookieHint.Enabled;
		}

		// Token: 0x04000342 RID: 834
		private string senderEmailAddress;

		// Token: 0x04000343 RID: 835
		private string senderOrganization;

		// Token: 0x04000344 RID: 836
		private string routingEmailAddress;

		// Token: 0x0200011E RID: 286
		private enum E4eErrorType
		{
			// Token: 0x04000511 RID: 1297
			GenericError,
			// Token: 0x04000512 RID: 1298
			ConfigError,
			// Token: 0x04000513 RID: 1299
			ThrottlingRestriction,
			// Token: 0x04000514 RID: 1300
			OrgNotExisting,
			// Token: 0x04000515 RID: 1301
			AuthenticationFailure,
			// Token: 0x04000516 RID: 1302
			UploadFailure,
			// Token: 0x04000517 RID: 1303
			ClientFailure,
			// Token: 0x04000518 RID: 1304
			InvalidCredentials,
			// Token: 0x04000519 RID: 1305
			InvalidEmailAddress,
			// Token: 0x0400051A RID: 1306
			InvalidMetadata,
			// Token: 0x0400051B RID: 1307
			InvalidMessage,
			// Token: 0x0400051C RID: 1308
			MessageNotFound,
			// Token: 0x0400051D RID: 1309
			MessageNotAuthorized,
			// Token: 0x0400051E RID: 1310
			TransientFailure,
			// Token: 0x0400051F RID: 1311
			SessionTimeout,
			// Token: 0x04000520 RID: 1312
			ProbeRequest,
			// Token: 0x04000521 RID: 1313
			ClientException,
			// Token: 0x04000522 RID: 1314
			InvalidStoreRequest,
			// Token: 0x04000523 RID: 1315
			OTPSendPerSession,
			// Token: 0x04000524 RID: 1316
			OTPSendAcrossSession,
			// Token: 0x04000525 RID: 1317
			OTPAttemptPerSession,
			// Token: 0x04000526 RID: 1318
			OTPAttemptAcrossSession,
			// Token: 0x04000527 RID: 1319
			OTPDisabled,
			// Token: 0x04000528 RID: 1320
			OTPPasscodeExpired,
			// Token: 0x04000529 RID: 1321
			UnsupportedBrowser,
			// Token: 0x0400052A RID: 1322
			MessageExpired,
			// Token: 0x0400052B RID: 1323
			OTPPasscodeNotCorrect,
			// Token: 0x0400052C RID: 1324
			RESTAPIDisabled,
			// Token: 0x0400052D RID: 1325
			FeatureDisabled,
			// Token: 0x0400052E RID: 1326
			UnknownFailure,
			// Token: 0x0400052F RID: 1327
			InvalidRequest
		}

		// Token: 0x0200011F RID: 287
		private enum E4eErrorSource
		{
			// Token: 0x04000531 RID: 1329
			Store,
			// Token: 0x04000532 RID: 1330
			Auth,
			// Token: 0x04000533 RID: 1331
			Backend,
			// Token: 0x04000534 RID: 1332
			Client,
			// Token: 0x04000535 RID: 1333
			Generic,
			// Token: 0x04000536 RID: 1334
			OTP,
			// Token: 0x04000537 RID: 1335
			Retrieve,
			// Token: 0x04000538 RID: 1336
			ExternalOAuth
		}

		// Token: 0x02000120 RID: 288
		private class E4eConstants
		{
			// Token: 0x04000539 RID: 1337
			public const string ErrorPage = "ErrorPage.aspx";

			// Token: 0x0400053A RID: 1338
			public const string RedirectToAppPage = "RedirectToOMEViewer.ashx";

			// Token: 0x0400053B RID: 1339
			public const string AppFeedbackPage = "SendOMEFeedback.ashx";

			// Token: 0x0400053C RID: 1340
			public const string ErrorCode = "code";

			// Token: 0x0400053D RID: 1341
			public const string PostPayloadFilePath = "store.ashx";

			// Token: 0x0400053E RID: 1342
			public const string RetrievePayloadFilePath = "Retrieve.ashx";

			// Token: 0x0400053F RID: 1343
			public const string RecipientEmailAddress = "RecipientEmailAddress";

			// Token: 0x04000540 RID: 1344
			public const string SenderEmailAddress = "SenderEmailAddress";

			// Token: 0x04000541 RID: 1345
			public const string SenderOrganization = "SenderOrganization";

			// Token: 0x04000542 RID: 1346
			public const string TokenSeparatorForWacRequests = "Authenticator";

			// Token: 0x04000543 RID: 1347
			public const string WacRequestParameter = "wopi/";

			// Token: 0x04000544 RID: 1348
			public const string CfmRecipient = "cfmRecipient";

			// Token: 0x04000545 RID: 1349
			public const string RoutingEmailAddress = "routingEmailAddress";

			// Token: 0x04000546 RID: 1350
			public const string XSenderEmailAddress = "X-SenderEmailAddress";

			// Token: 0x04000547 RID: 1351
			public const string XSenderOrganization = "X-SenderOrganization";

			// Token: 0x04000548 RID: 1352
			public const string XRoutingEmailAddress = "X-RoutingEmailAddress";

			// Token: 0x04000549 RID: 1353
			public const string XBudgetTypeCookieName = "X-E4eBudgetType";

			// Token: 0x0400054A RID: 1354
			public const string XEmailAddressCookieName = "X-E4eEmailAddress";

			// Token: 0x0400054B RID: 1355
			public const string XBackoffUntilUtcCookieName = "X-E4eBackOffUntilUtc";

			// Token: 0x0400054C RID: 1356
			public const string XBackEndCookieName = "X-E4eBackEnd";

			// Token: 0x0400054D RID: 1357
			public const string XBackEndHeaderName = "X-E4ePostToBackEnd";

			// Token: 0x0400054E RID: 1358
			public const string XItemIdCookieName = "X-OTPItemId";

			// Token: 0x0400054F RID: 1359
			public const char BackendCookieValueDelimiter = '~';
		}
	}
}
