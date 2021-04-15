using System;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Web;
using System.Xml;
using Microsoft.Exchange.Data.ConfigurationSettings;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200008B RID: 139
	internal class AutodiscoverProxyRequestHandler : EwsAutodiscoverProxyRequestHandler
	{
		// Token: 0x17000111 RID: 273
		// (get) Token: 0x0600049F RID: 1183 RVA: 0x0001981A File Offset: 0x00017A1A
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x060004A0 RID: 1184 RVA: 0x00019820 File Offset: 0x00017A20
		protected override void OnInitializingHandler()
		{
			base.OnInitializingHandler();
			if (!base.ClientRequest.IsAuthenticated)
			{
				base.IsWsSecurityRequest = base.ClientRequest.IsAnyWsSecurityRequest();
				if (base.IsWsSecurityRequest && !AutodiscoverEwsWebConfiguration.WsSecurityEndpointEnabled)
				{
					throw new HttpException(404, "WS-Security endpoint is not supported");
				}
			}
			if (base.ClientRequest.Url.ToString().EndsWith("autodiscover.xml", StringComparison.OrdinalIgnoreCase) || RequestPathParser.IsAutodiscoverV2Request(base.ClientRequest.Url.AbsolutePath))
			{
				base.PreferAnchorMailboxHeader = true;
			}
		}

		// Token: 0x060004A1 RID: 1185 RVA: 0x000198AC File Offset: 0x00017AAC
		protected override void DoProtocolSpecificBeginProcess()
		{
			if (!base.ClientRequest.IsAuthenticated)
			{
				try
				{
					if (this.IsSimpleSoapRequest())
					{
						base.ParseClientRequest<bool>(new Func<Stream, bool>(this.ParseRequest), 81820);
					}
				}
				catch (FormatException innerException)
				{
					throw new HttpException(400, "FormatException parsing Autodiscover request", innerException);
				}
				catch (XmlException innerException2)
				{
					throw new HttpException(400, "XmlException parsing Autodiscover request", innerException2);
				}
				if (!base.IsWsSecurityRequest && !base.IsDomainBasedRequest && !RequestPathParser.IsOAuthMetadataRequest(base.ClientRequest.Url.AbsolutePath) && !RequestPathParser.IsAutodiscoverV2Request(base.ClientRequest.Url.AbsolutePath))
				{
					throw new HttpProxyException(HttpStatusCode.Unauthorized, 4001, "Unauthenticated AutoDiscover request.");
				}
			}
		}

		// Token: 0x060004A2 RID: 1186 RVA: 0x00019980 File Offset: 0x00017B80
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			if (base.ClientRequest.IsAuthenticated && base.ProxyToDownLevel)
			{
				headers["X-AutodiscoverProxySecurityContext"] = this.GetSerializedAccessTokenString();
			}
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x060004A3 RID: 1187 RVA: 0x000199B0 File Offset: 0x00017BB0
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			if (AutodiscoverProxyRequestHandler.LoadBalancedPartnerRouting.Value && base.ClientRequest.Url.AbsolutePath.ToLower().Contains("/wssecurity/x509cert"))
			{
				string text = base.ClientRequest.Headers[Constants.AnchorMailboxHeaderName];
				string text2 = null;
				if (string.IsNullOrEmpty(text))
				{
					AnchorMailbox anchorMailbox = base.TryGetAnchorMailboxFromWsSecurityRequest();
					if (anchorMailbox != null)
					{
						SmtpAnchorMailbox smtpAnchorMailbox = anchorMailbox as SmtpAnchorMailbox;
						if (smtpAnchorMailbox != null)
						{
							text2 = smtpAnchorMailbox.Smtp;
						}
						else
						{
							DomainAnchorMailbox domainAnchorMailbox = anchorMailbox as DomainAnchorMailbox;
							if (domainAnchorMailbox != null)
							{
								text2 = domainAnchorMailbox.Domain;
							}
						}
					}
				}
				else
				{
					text2 = text;
				}
				if (!string.IsNullOrEmpty(text2) && text2.EndsWith(AutodiscoverProxyRequestHandler.BlackBerryTenantName.Value, StringComparison.OrdinalIgnoreCase))
				{
					base.Logger.SafeSet(3, "PartnerX509Request");
					return new TargetForestAnchorMailbox(this, AutodiscoverProxyRequestHandler.BlackBerryTenantName.Value, false);
				}
			}
			return base.ResolveAnchorMailbox();
		}

		// Token: 0x060004A4 RID: 1188 RVA: 0x00019A8E File Offset: 0x00017C8E
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return !string.Equals(headerName, "X-AutodiscoverProxySecurityContext", StringComparison.OrdinalIgnoreCase) && base.ShouldCopyHeaderToServerRequest(headerName);
		}

		// Token: 0x060004A5 RID: 1189 RVA: 0x00019AA7 File Offset: 0x00017CA7
		protected override Uri UpdateExternalRedirectUrl(Uri originalRedirectUrl)
		{
			return new UriBuilder(base.ClientRequest.Url)
			{
				Host = originalRedirectUrl.Host,
				Port = originalRedirectUrl.Port
			}.Uri;
		}

		// Token: 0x060004A6 RID: 1190 RVA: 0x00019AD6 File Offset: 0x00017CD6
		private bool IsSimpleSoapRequest()
		{
			return base.ClientRequest.Url.LocalPath.EndsWith(".svc", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004A7 RID: 1191 RVA: 0x00019AF4 File Offset: 0x00017CF4
		private bool ParseRequest(Stream stream)
		{
			long position = stream.Position;
			XmlReader xmlReader = XmlReader.Create(stream);
			if (xmlReader.Settings != null && xmlReader.Settings.DtdProcessing != DtdProcessing.Prohibit)
			{
				xmlReader.Settings.DtdProcessing = DtdProcessing.Prohibit;
			}
			XmlElement xmlElement = null;
			xmlReader.MoveToContent();
			bool flag = true;
			while (flag && xmlReader.Read())
			{
				if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "Header")
				{
					if (!(xmlReader.NamespaceURI == "http://schemas.xmlsoap.org/soap/envelope/"))
					{
						if (!(xmlReader.NamespaceURI == "http://www.w3.org/2003/05/soap-envelope"))
						{
							continue;
						}
					}
					while (flag && xmlReader.Read())
					{
						if (stream.Position > 81820L)
						{
							throw new QuotaExceededException();
						}
						if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.LocalName == "Header" && (xmlReader.NamespaceURI == "http://schemas.xmlsoap.org/soap/envelope/" || xmlReader.NamespaceURI == "http://www.w3.org/2003/05/soap-envelope"))
						{
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
							{
								ExTraceGlobals.VerboseTracer.TraceDebug(0L, "[AutodiscoverProxyModule::ParseRequest]: Hit the end of the SOAP header unexpectedly");
							}
							flag = false;
							break;
						}
						if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "Action" && xmlReader.NamespaceURI == "http://www.w3.org/2005/08/addressing")
						{
							using (XmlReader xmlReader2 = xmlReader.ReadSubtree())
							{
								XmlDocument xmlDocument = new XmlDocument();
								xmlDocument.Load(xmlReader2);
								XmlElement documentElement = xmlDocument.DocumentElement;
								if (documentElement == null)
								{
									flag = false;
									break;
								}
								if (!documentElement.InnerText.Trim().EndsWith("/GetFederationInformation", StringComparison.OrdinalIgnoreCase))
								{
									if (documentElement.InnerText.Trim().EndsWith("/GetOrganizationRelationshipSettings", StringComparison.OrdinalIgnoreCase))
									{
										base.SkipTargetBackEndCalculation = true;
									}
									flag = false;
									break;
								}
							}
							base.IsDomainBasedRequest = true;
							while (flag && xmlReader.Read())
							{
								if (stream.Position > 81820L)
								{
									throw new QuotaExceededException();
								}
								if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "Body")
								{
									if (!(xmlReader.NamespaceURI == "http://schemas.xmlsoap.org/soap/envelope/"))
									{
										if (!(xmlReader.NamespaceURI == "http://www.w3.org/2003/05/soap-envelope"))
										{
											continue;
										}
									}
									while (flag && xmlReader.Read())
									{
										if (stream.Position > 81820L)
										{
											throw new QuotaExceededException();
										}
										if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.LocalName == "Body" && (xmlReader.NamespaceURI == "http://schemas.xmlsoap.org/soap/envelope/" || xmlReader.NamespaceURI == "http://www.w3.org/2003/05/soap-envelope"))
										{
											if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
											{
												ExTraceGlobals.VerboseTracer.TraceDebug(0L, "[AutodiscoverProxyModule::ParseRequest]: Hit the end of the SOAP body unexpectedly");
											}
											flag = false;
											break;
										}
										if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "GetFederationInformationRequestMessage")
										{
											using (XmlReader xmlReader3 = xmlReader.ReadSubtree())
											{
												XmlDocument xmlDocument2 = new XmlDocument();
												xmlDocument2.Load(xmlReader3);
												xmlElement = xmlDocument2.DocumentElement;
											}
											flag = false;
											break;
										}
									}
								}
							}
						}
					}
				}
			}
			if (xmlElement != null && xmlElement.ChildNodes != null)
			{
				foreach (object obj in xmlElement.ChildNodes)
				{
					XmlNode xmlNode = (XmlNode)obj;
					if (xmlNode.LocalName == "Request" && xmlNode.FirstChild != null && xmlNode.FirstChild.LocalName == "Domain")
					{
						base.Domain = xmlNode.FirstChild.InnerText;
					}
				}
			}
			return true;
		}

		// Token: 0x0400032E RID: 814
		internal const string AutodiscoverProxySecurityContext = "X-AutodiscoverProxySecurityContext";

		// Token: 0x0400032F RID: 815
		private const string GetFederationInformationAction = "/GetFederationInformation";

		// Token: 0x04000330 RID: 816
		private const string GetOrganizationRelationshipSettingsAction = "/GetOrganizationRelationshipSettings";

		// Token: 0x04000331 RID: 817
		private const string X509CertUrlSuffix = "/wssecurity/x509cert";

		// Token: 0x04000332 RID: 818
		private const string SoapRequestEnd = ".svc";

		// Token: 0x04000333 RID: 819
		private const int MaxSizeOfDomainBasedRequest = 81820;

		// Token: 0x04000334 RID: 820
		private static readonly StringAppSettingsEntry BlackBerryTenantName = new StringAppSettingsEntry(HttpProxySettings.Prefix("BlackBerryTenantName"), "service.businesscloud.blackberry.com", ExTraceGlobals.VerboseTracer);

		// Token: 0x04000335 RID: 821
		private static readonly FlightableBoolAppSettingsEntry LoadBalancedPartnerRouting = new FlightableBoolAppSettingsEntry(HttpProxySettings.Prefix("LoadBalancedPartnerRouting"), () => CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).LoadBalancedPartnerRouting.Enabled);
	}
}
