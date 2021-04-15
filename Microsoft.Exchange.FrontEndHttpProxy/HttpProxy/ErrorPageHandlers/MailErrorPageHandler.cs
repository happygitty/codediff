﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using System.Web.Security.AntiXss;
using Microsoft.Exchange.Clients;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Clients.Security;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.ClientsCommon;

namespace Microsoft.Exchange.HttpProxy.ErrorPageHandlers
{
	// Token: 0x020000CA RID: 202
	public class MailErrorPageHandler : IErrorPageHandler
	{
		// Token: 0x06000781 RID: 1921 RVA: 0x0002BAA0 File Offset: 0x00029CA0
		public MailErrorPageHandler(HttpRequest request)
		{
			this.errorPageRequest = request;
			this.genericErrorInfo = new GenericErrorInfo(request);
			if (!Enum.TryParse<ErrorMode>(this.errorPageRequest.QueryString["em"], out this.errorMode))
			{
				this.errorMode = 0;
			}
			this.InitializeDiagnosticInfomation();
		}

		// Token: 0x17000193 RID: 403
		// (get) Token: 0x06000782 RID: 1922 RVA: 0x0002BB14 File Offset: 0x00029D14
		public string AriaDiagnosticObjectJsonString
		{
			get
			{
				if (!this.ShouldSendSignal())
				{
					return string.Empty;
				}
				StringBuilder stringBuilder = new StringBuilder();
				StringBuilder stringBuilder2 = new StringBuilder();
				foreach (KeyValuePair<string, MailErrorPageHandler.VerifiableString> keyValuePair in this.diagnosticsInfo)
				{
					string @string = keyValuePair.Value.String;
					if (@string != null)
					{
						if (MailErrorPageHandler.AriaExtractedKeys.ContainsKey(keyValuePair.Key))
						{
							stringBuilder.Append(string.Format("{0}: {{ value: '{1}' }},", MailErrorPageHandler.AriaExtractedKeys[keyValuePair.Key], @string));
						}
						else
						{
							stringBuilder2.Append(string.Format("&{0}={1}", keyValuePair.Key, @string));
						}
					}
				}
				stringBuilder.Append(string.Format("MiscData: {{ value: '{0}' }},", stringBuilder2.ToString()));
				return string.Format("{{ name: 'startuperrorpage', properties: {{ {0} }} }}", stringBuilder);
			}
		}

		// Token: 0x17000194 RID: 404
		// (get) Token: 0x06000783 RID: 1923 RVA: 0x0002BC04 File Offset: 0x00029E04
		public string ServerDiagnosticObjectJsonString
		{
			get
			{
				if (!this.ShouldSendSignal())
				{
					return string.Empty;
				}
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("?bret=fail");
				foreach (KeyValuePair<string, MailErrorPageHandler.VerifiableString> keyValuePair in this.diagnosticsInfo)
				{
					string @string = keyValuePair.Value.String;
					if (@string != null)
					{
						stringBuilder.Append(string.Format("&{0}={1}", keyValuePair.Key, @string));
					}
				}
				return string.Format("{{url: '{0}', qp: '{1}' }}", "/mail/bootr.ashx", stringBuilder);
			}
		}

		// Token: 0x17000195 RID: 405
		// (get) Token: 0x06000784 RID: 1924 RVA: 0x0002BCAC File Offset: 0x00029EAC
		public string DiagnosticInformation
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (KeyValuePair<string, MailErrorPageHandler.VerifiableString> keyValuePair in this.diagnosticsInfo)
				{
					string @string = keyValuePair.Value.String;
					if (@string != null)
					{
						if (ValidationUtilities.IsValueValid(@string, keyValuePair.Value.Flags))
						{
							stringBuilder.Append(string.Format("{0}: {1}\n", keyValuePair.Key, @string));
						}
						else
						{
							stringBuilder.Append(string.Format("{0}: {1}\n", keyValuePair.Key, ValidationUtilities.EncodeUnsafeValue(@string, this.unsafeValueEncoding)));
						}
					}
				}
				return stringBuilder.ToString();
			}
		}

		// Token: 0x17000196 RID: 406
		// (get) Token: 0x06000785 RID: 1925 RVA: 0x0002BD68 File Offset: 0x00029F68
		public string ErrorHeader
		{
			get
			{
				string result = string.Empty;
				if (this.errorMode != 1)
				{
					if (this.genericErrorInfo.HttpCode == "404")
					{
						result = LocalizedStrings.GetHtmlEncoded(-392503097);
					}
					else if (this.genericErrorInfo.HttpCode == "500")
					{
						result = LocalizedStrings.GetHtmlEncoded(629133816);
					}
				}
				return result;
			}
		}

		// Token: 0x17000197 RID: 407
		// (get) Token: 0x06000786 RID: 1926 RVA: 0x0002BDCC File Offset: 0x00029FCC
		public string ErrorSubHeader
		{
			get
			{
				string result = string.Empty;
				if (this.errorMode == 1)
				{
					result = LocalizedStrings.GetHtmlEncoded(-146632527);
				}
				else if (this.errorMode == 2)
				{
					result = LocalizedStrings.GetHtmlEncoded(-1935911806);
				}
				else if (this.errorMode == 3)
				{
					result = LocalizedStrings.GetHtmlEncoded(425733410);
				}
				else if (this.errorMode == 4)
				{
					result = LocalizedStrings.GetHtmlEncoded(-432125413);
				}
				else if (this.genericErrorInfo.HttpCode == "404")
				{
					result = LocalizedStrings.GetHtmlEncoded(1252002283);
				}
				else if (this.genericErrorInfo.HttpCode == "503")
				{
					result = LocalizedStrings.GetHtmlEncoded(1252002321);
				}
				else
				{
					result = LocalizedStrings.GetHtmlEncoded(1252002318);
				}
				return result;
			}
		}

		// Token: 0x17000198 RID: 408
		// (get) Token: 0x06000787 RID: 1927 RVA: 0x0002BE90 File Offset: 0x0002A090
		public string ErrorDetails
		{
			get
			{
				string result = string.Empty;
				Strings.IDs ds;
				if (Enum.TryParse<Strings.IDs>(this.errorPageRequest.QueryString["msg"], out ds))
				{
					string text = MailErrorPageHandler.SafeErrorMessagesNoHtmlEncoding.Contains(ds) ? Strings.GetLocalizedString(ds) : LocalizedStrings.GetHtmlEncoded(ds);
					List<string> list = ErrorInformation.ParseMessageParameters(text, this.errorPageRequest);
					if (list != null && list.Count > 0)
					{
						for (int i = 0; i < list.Count; i++)
						{
							list[i] = EncodingUtilities.HtmlEncode(list[i]);
						}
						if (MailErrorPageHandler.MessagesToRenderLogoutLinks.Contains(ds) || MailErrorPageHandler.MessagesToRenderLoginLinks.Contains(ds))
						{
							this.AddSafeLinkToMessageParametersList(ds, this.errorPageRequest, ref list);
						}
						result = string.Format(text, list.ToArray());
					}
					else if (MailErrorPageHandler.MessagesToRenderLogoutLinks.Contains(ds) || MailErrorPageHandler.MessagesToRenderLoginLinks.Contains(ds))
					{
						list = new List<string>();
						this.AddSafeLinkToMessageParametersList(ds, this.errorPageRequest, ref list);
						if (list.Count > 0)
						{
							result = string.Format(text, list.ToArray());
						}
					}
					else
					{
						result = text;
					}
				}
				else if (this.genericErrorInfo.HttpCode == "404")
				{
					result = LocalizedStrings.GetHtmlEncoded(236137810);
				}
				else
				{
					result = LocalizedStrings.GetHtmlEncoded(236137783);
				}
				return result;
			}
		}

		// Token: 0x17000199 RID: 409
		// (get) Token: 0x06000788 RID: 1928 RVA: 0x0002BFD6 File Offset: 0x0002A1D6
		public string ErrorTitle
		{
			get
			{
				if (this.errorMode != 1)
				{
					return LocalizedStrings.GetHtmlEncoded(933672694);
				}
				return LocalizedStrings.GetHtmlEncoded(-594631022);
			}
		}

		// Token: 0x1700019A RID: 410
		// (get) Token: 0x06000789 RID: 1929 RVA: 0x0002BFF6 File Offset: 0x0002A1F6
		public string RefreshButtonText
		{
			get
			{
				if (this.errorMode != 1)
				{
					return LocalizedStrings.GetHtmlEncoded(1939504838);
				}
				return LocalizedStrings.GetHtmlEncoded(867248262);
			}
		}

		// Token: 0x1700019B RID: 411
		// (get) Token: 0x0600078A RID: 1930 RVA: 0x0002C016 File Offset: 0x0002A216
		public bool ShowRefreshButton
		{
			get
			{
				return this.errorMode != 3;
			}
		}

		// Token: 0x1700019C RID: 412
		// (get) Token: 0x0600078B RID: 1931 RVA: 0x0002C024 File Offset: 0x0002A224
		public string ReturnUri
		{
			get
			{
				if (string.IsNullOrEmpty(this.genericErrorInfo.ReturnUri))
				{
					return "/";
				}
				return this.genericErrorInfo.ReturnUri;
			}
		}

		// Token: 0x0600078C RID: 1932 RVA: 0x0002C04C File Offset: 0x0002A24C
		private void InitializeDiagnosticInfomation()
		{
			this.diagnosticsInfo = new Dictionary<string, MailErrorPageHandler.VerifiableString>();
			if (this.errorPageRequest.Cookies["ClientId"] != null)
			{
				this.diagnosticsInfo["cId"] = new MailErrorPageHandler.VerifiableString(AntiXssEncoder.HtmlEncode(this.errorPageRequest.Cookies["ClientId"].Value, false), 1);
			}
			foreach (KeyValuePair<string, ValidationUtilities.ValidationType> keyValuePair in MailErrorPageHandler.DiagnosticsKeys)
			{
				if (this.errorPageRequest.QueryString[keyValuePair.Key] != null)
				{
					this.diagnosticsInfo[keyValuePair.Key] = new MailErrorPageHandler.VerifiableString(AntiXssEncoder.HtmlEncode(this.errorPageRequest.QueryString[keyValuePair.Key], false), keyValuePair.Value);
				}
			}
			long fileTime;
			if (long.TryParse(this.errorPageRequest.QueryString["ts"], out fileTime))
			{
				this.diagnosticsInfo["ts"] = new MailErrorPageHandler.VerifiableString(DateTime.FromFileTimeUtc(fileTime).ToString(), 1);
				return;
			}
			this.diagnosticsInfo["ts"] = new MailErrorPageHandler.VerifiableString(DateTime.UtcNow.ToString(), 1);
		}

		// Token: 0x0600078D RID: 1933 RVA: 0x0002C1AC File Offset: 0x0002A3AC
		private void AddSafeLinkToMessageParametersList(Strings.IDs messageId, HttpRequest request, ref List<string> messageParameters)
		{
			string item = string.Empty;
			string text = string.Empty;
			if (MailErrorPageHandler.MessagesToRenderLogoutLinks.Contains(messageId))
			{
				item = this.GetLocalizedLiveIdSignoutLinkMessage(request);
				messageParameters.Insert(0, item);
				return;
			}
			if (MailErrorPageHandler.MessagesToRenderLoginLinks.Contains(messageId))
			{
				string dnsSafeHost = request.Url.DnsSafeHost;
				if (messageParameters != null && messageParameters.Count > 0)
				{
					text = messageParameters[0];
				}
				item = Utilities.GetAccessURLFromHostnameAndRealm(dnsSafeHost, text, false);
				messageParameters.Insert(0, item);
				messageParameters.Remove(dnsSafeHost);
			}
		}

		// Token: 0x0600078E RID: 1934 RVA: 0x0002C230 File Offset: 0x0002A430
		private string GetLocalizedLiveIdSignoutLinkMessage(HttpRequest request)
		{
			string explicitUrl = OwaUrl.Logoff.GetExplicitUrl(request);
			return "<BR><BR>" + string.Format(CultureInfo.InvariantCulture, Strings.LogonErrorLogoutUrlText, explicitUrl);
		}

		// Token: 0x0600078F RID: 1935 RVA: 0x0002C264 File Offset: 0x0002A464
		private bool ShouldSendSignal()
		{
			return this.diagnosticsInfo.ContainsKey("et") && this.diagnosticsInfo.ContainsKey("esrc") && this.diagnosticsInfo["et"].String == "ServerError" && (this.diagnosticsInfo["esrc"].String == "MasterPage" || this.diagnosticsInfo["esrc"].String == "AppPool");
		}

		// Token: 0x06000790 RID: 1936 RVA: 0x0002C2FC File Offset: 0x0002A4FC
		// Note: this type is marked as 'beforefieldinit'.
		static MailErrorPageHandler()
		{
			Strings.IDs[] array = new Strings.IDs[7];
			RuntimeHelpers.InitializeArray(array, fieldof(<PrivateImplementationDetails>.C3181359425DC88A4FB24C2EC5B370A94026499D).FieldHandle);
			MailErrorPageHandler.SafeErrorMessagesNoHtmlEncoding = array;
			Strings.IDs[] array2 = new Strings.IDs[8];
			RuntimeHelpers.InitializeArray(array2, fieldof(<PrivateImplementationDetails>.53A281C428804F4D524AF2453611A61FA1D7DF9B).FieldHandle);
			MailErrorPageHandler.MessagesToRenderLogoutLinks = array2;
			MailErrorPageHandler.MessagesToRenderLoginLinks = new Strings.IDs[]
			{
				1317300008
			};
			MailErrorPageHandler.AriaExtractedKeys = new Dictionary<string, string>
			{
				{
					"app",
					"AppName"
				},
				{
					"st",
					"StatusCode"
				},
				{
					"cId",
					"ClientId"
				},
				{
					"te",
					"ThroughEdge"
				},
				{
					"refurl",
					"RefUrl"
				},
				{
					"ebe",
					"ErrorBEServer"
				},
				{
					"efe",
					"ErrorFEServer"
				},
				{
					"et",
					"ErrorType"
				},
				{
					"esrc",
					"ErrorSource"
				},
				{
					"err",
					"Error"
				},
				{
					"estack",
					"ExtraErrorInfo"
				},
				{
					"reqid",
					"RequestId"
				}
			};
			MailErrorPageHandler.DiagnosticsKeys = new Dictionary<string, ValidationUtilities.ValidationType>
			{
				{
					"app",
					0
				},
				{
					"st",
					8
				},
				{
					"reqid",
					10
				},
				{
					"cver",
					24
				},
				{
					"wsver",
					0
				},
				{
					"te",
					8
				},
				{
					"refurl",
					0
				},
				{
					"efe",
					108
				},
				{
					"ebe",
					108
				},
				{
					"fost",
					124
				},
				{
					"et",
					0
				},
				{
					"esrc",
					0
				},
				{
					"err",
					0
				},
				{
					"estack",
					0
				}
			};
		}

		// Token: 0x0400042B RID: 1067
		private const string ErrorMessageQueryKey = "msg";

		// Token: 0x0400042C RID: 1068
		private const string ErrorTypeQueryKey = "et";

		// Token: 0x0400042D RID: 1069
		private const string ErrorSourceQueryKey = "esrc";

		// Token: 0x0400042E RID: 1070
		private const string ErrorQueryKey = "err";

		// Token: 0x0400042F RID: 1071
		private const string ClientVersionQueryKey = "cver";

		// Token: 0x04000430 RID: 1072
		private const string WebServiceVersionQueryKey = "wsver";

		// Token: 0x04000431 RID: 1073
		private const string TimeStampQueryKey = "ts";

		// Token: 0x04000432 RID: 1074
		private const string RequestIdQueryKey = "reqid";

		// Token: 0x04000433 RID: 1075
		private const string ErrorFEServerQueryKey = "efe";

		// Token: 0x04000434 RID: 1076
		private const string ErrorBEServerQueryKey = "ebe";

		// Token: 0x04000435 RID: 1077
		private const string ErrorForestQueryKey = "fost";

		// Token: 0x04000436 RID: 1078
		private const string ErrorStackQueryKey = "estack";

		// Token: 0x04000437 RID: 1079
		private const string ThroughEdgeQueryKey = "te";

		// Token: 0x04000438 RID: 1080
		private const string RefUrlQueryKey = "refurl";

		// Token: 0x04000439 RID: 1081
		private const string ErrorModeQueryKey = "em";

		// Token: 0x0400043A RID: 1082
		private const string ServerSignalUri = "/mail/bootr.ashx";

		// Token: 0x0400043B RID: 1083
		private const string ErrorTypeServerError = "ServerError";

		// Token: 0x0400043C RID: 1084
		private const string ErrorSourceMasterPage = "MasterPage";

		// Token: 0x0400043D RID: 1085
		private const string ErrorSourceAppPool = "AppPool";

		// Token: 0x0400043E RID: 1086
		private const string ClienIdKey = "cId";

		// Token: 0x0400043F RID: 1087
		private static readonly Strings.IDs[] SafeErrorMessagesNoHtmlEncoding;

		// Token: 0x04000440 RID: 1088
		private static readonly Strings.IDs[] MessagesToRenderLogoutLinks;

		// Token: 0x04000441 RID: 1089
		private static readonly Strings.IDs[] MessagesToRenderLoginLinks;

		// Token: 0x04000442 RID: 1090
		private static readonly Dictionary<string, string> AriaExtractedKeys;

		// Token: 0x04000443 RID: 1091
		private static readonly Dictionary<string, ValidationUtilities.ValidationType> DiagnosticsKeys;

		// Token: 0x04000444 RID: 1092
		private HttpRequest errorPageRequest;

		// Token: 0x04000445 RID: 1093
		private GenericErrorInfo genericErrorInfo;

		// Token: 0x04000446 RID: 1094
		private ErrorMode errorMode;

		// Token: 0x04000447 RID: 1095
		private Dictionary<string, MailErrorPageHandler.VerifiableString> diagnosticsInfo;

		// Token: 0x04000448 RID: 1096
		private UnsafeValueEncodingType unsafeValueEncoding = ClientsCommonConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).ErrorPageSettings.FrownyUnsafeValueEncoding;

		// Token: 0x02000158 RID: 344
		private class VerifiableString
		{
			// Token: 0x060008E1 RID: 2273 RVA: 0x00031658 File Offset: 0x0002F858
			public VerifiableString(string str, ValidationUtilities.ValidationType flags)
			{
				this.String = str;
				this.Flags = flags;
			}

			// Token: 0x170001B3 RID: 435
			// (get) Token: 0x060008E2 RID: 2274 RVA: 0x0003166E File Offset: 0x0002F86E
			// (set) Token: 0x060008E3 RID: 2275 RVA: 0x00031676 File Offset: 0x0002F876
			public string String { get; set; }

			// Token: 0x170001B4 RID: 436
			// (get) Token: 0x060008E4 RID: 2276 RVA: 0x0003167F File Offset: 0x0002F87F
			// (set) Token: 0x060008E5 RID: 2277 RVA: 0x00031687 File Offset: 0x0002F887
			public ValidationUtilities.ValidationType Flags { get; set; }
		}
	}
}
