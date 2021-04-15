using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;
using System.Xml;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Authentication;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.Net.WSTrust;
using Microsoft.Exchange.Security.X509CertAuth;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000071 RID: 113
	internal class WsSecurityParser
	{
		// Token: 0x060003CC RID: 972 RVA: 0x00015953 File Offset: 0x00013B53
		public WsSecurityParser(int traceContext)
		{
			this.TraceContext = traceContext;
		}

		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x060003CD RID: 973 RVA: 0x00015962 File Offset: 0x00013B62
		// (set) Token: 0x060003CE RID: 974 RVA: 0x0001596A File Offset: 0x00013B6A
		private int TraceContext { get; set; }

		// Token: 0x060003CF RID: 975 RVA: 0x00015974 File Offset: 0x00013B74
		internal KeyValuePair<string, bool> FindAddressFromWsSecurityRequest(Stream stream)
		{
			bool value = false;
			return new KeyValuePair<string, bool>(this.FindAddressFromWsSecurity(stream, WsSecurityHeaderType.WSSecurityAuth, out value), value);
		}

		// Token: 0x060003D0 RID: 976 RVA: 0x00015994 File Offset: 0x00013B94
		internal string FindAddressFromPartnerAuthRequest(Stream stream)
		{
			bool flag;
			return this.FindAddressFromWsSecurity(stream, WsSecurityHeaderType.PartnerAuth, out flag);
		}

		// Token: 0x060003D1 RID: 977 RVA: 0x000159AC File Offset: 0x00013BAC
		internal string FindAddressFromX509CertAuthRequest(Stream stream)
		{
			bool flag;
			return this.FindAddressFromWsSecurity(stream, WsSecurityHeaderType.X509CertAuth, out flag);
		}

		// Token: 0x060003D2 RID: 978 RVA: 0x000159C4 File Offset: 0x00013BC4
		internal string FindAddressFromWsSecurity(Stream stream, WsSecurityHeaderType headerType, out bool isDelegationToken)
		{
			isDelegationToken = false;
			long position = stream.Position;
			try
			{
				XmlReader xmlReader = XmlReader.Create(stream);
				if (xmlReader.Settings != null && xmlReader.Settings.DtdProcessing != DtdProcessing.Prohibit)
				{
					xmlReader.Settings.DtdProcessing = DtdProcessing.Prohibit;
				}
				XmlElement xmlElement = null;
				XmlElement xmlElement2 = null;
				XmlElement xmlElement3 = null;
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
							if (stream.Position > 73628L)
							{
								throw new QuotaExceededException();
							}
							if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.LocalName == "Header" && (xmlReader.NamespaceURI == "http://schemas.xmlsoap.org/soap/envelope/" || xmlReader.NamespaceURI == "http://www.w3.org/2003/05/soap-envelope"))
							{
								if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
								{
									ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[WsSecurityParser::FindAddressFromWsSecurity]: Context {0}; Hit the end of the SOAP header unexpectedly", this.TraceContext);
								}
								flag = false;
								break;
							}
							if (headerType != WsSecurityHeaderType.PartnerAuth)
							{
								if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "Security" && xmlReader.NamespaceURI == "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")
								{
									while (flag)
									{
										if (!xmlReader.Read())
										{
											break;
										}
										if (stream.Position > 73628L)
										{
											throw new QuotaExceededException();
										}
										if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.LocalName == "Security" && xmlReader.NamespaceURI == "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")
										{
											if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
											{
												ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[WsSecurityParser::FindAddressFromWsSecurity]: Context {0}; Hit the end of the WS-Security header unexpectedly", this.TraceContext);
											}
											flag = false;
											break;
										}
										if (xmlReader.NodeType == XmlNodeType.Element && ((headerType == WsSecurityHeaderType.WSSecurityAuth && xmlReader.LocalName == "EncryptedData" && xmlReader.NamespaceURI == "http://www.w3.org/2001/04/xmlenc#") || (headerType == WsSecurityHeaderType.X509CertAuth && xmlReader.LocalName == "BinarySecurityToken" && xmlReader.NamespaceURI == "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")))
										{
											using (XmlReader xmlReader2 = xmlReader.ReadSubtree())
											{
												XmlDocument xmlDocument = new XmlDocument();
												xmlDocument.Load(xmlReader2);
												xmlElement = xmlDocument.DocumentElement;
											}
											flag = false;
											break;
										}
									}
								}
							}
							else
							{
								if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "ExchangeImpersonation" && xmlReader.NamespaceURI == "http://schemas.microsoft.com/exchange/services/2006/types")
								{
									using (XmlReader xmlReader3 = xmlReader.ReadSubtree())
									{
										XmlDocument xmlDocument2 = new XmlDocument();
										xmlDocument2.Load(xmlReader3);
										xmlElement2 = xmlDocument2.DocumentElement;
									}
									flag = false;
									break;
								}
								if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "Attribute" && xmlReader.NamespaceURI == "urn:oasis:names:tc:SAML:1.0:assertion" && xmlReader.HasAttributes && xmlReader.GetAttribute("AttributeName") == "targettenant")
								{
									using (XmlReader xmlReader4 = xmlReader.ReadSubtree())
									{
										XmlDocument xmlDocument3 = new XmlDocument();
										xmlDocument3.Load(xmlReader4);
										xmlElement3 = xmlDocument3.DocumentElement;
									}
									flag = false;
									break;
								}
							}
						}
					}
				}
				if (headerType == WsSecurityHeaderType.PartnerAuth)
				{
					if (xmlElement2 != null)
					{
						if (xmlElement2.NodeType == XmlNodeType.Element && xmlElement2.FirstChild.NodeType == XmlNodeType.Element && xmlElement2.FirstChild.LocalName == "ConnectingSID")
						{
							XmlNode firstChild = xmlElement2.FirstChild.FirstChild;
							if (firstChild.LocalName == "PrincipalName" || firstChild.LocalName == "PrimarySmtpAddress" || firstChild.LocalName == "SmtpAddress" || firstChild.LocalName == "SID")
							{
								return firstChild.InnerXml;
							}
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
							{
								ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[WsSecurityParser::FindAddressFromWsSecurity]: Context {0}; Unexpected type {1} in ConnectingSID in impersonation header", this.TraceContext, firstChild.Name);
							}
							return null;
						}
					}
					else if (xmlElement3 != null && xmlElement3.NodeType == XmlNodeType.Element && xmlElement3.FirstChild.LocalName == "AttributeValue")
					{
						return xmlElement3.FirstChild.InnerText;
					}
				}
				else if (xmlElement != null)
				{
					ExternalAuthentication current = ExternalAuthentication.GetCurrent();
					if (!current.Enabled)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[WsSecurityParser::FindAddressFromWsSecurity]: ExternalAuthentication is not enabled");
						}
						return null;
					}
					if (headerType == WsSecurityHeaderType.X509CertAuth)
					{
						AuthorizationContext authorizationContext;
						if (current.TokenValidator.AuthorizationContextFromToken(xmlElement, ref authorizationContext).Result == null)
						{
							ReadOnlyCollection<ClaimSet> claimSets = authorizationContext.ClaimSets;
							X509CertUser x509CertUser = null;
							if (!X509CertUser.TryCreateX509CertUser(claimSets, ref x509CertUser))
							{
								if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
								{
									ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[WsSecurityParser::FindAddressFromWsSecurity]: Context {0}; Unable to create the x509certuser", this.TraceContext);
								}
								return null;
							}
							OrganizationId organizationId;
							WindowsIdentity windowsIdentity;
							string text;
							if (!x509CertUser.TryGetWindowsIdentity(ref organizationId, ref windowsIdentity, ref text))
							{
								if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
								{
									ExTraceGlobals.VerboseTracer.TraceDebug<int, X509CertUser, string>((long)this.GetHashCode(), "[WsSecurityParser::FindAddressFromWsSecurity]: Context {0}; unable to find the windows identity for cert user: {1}, reason: {2}", this.TraceContext, x509CertUser, text);
								}
								return null;
							}
							return x509CertUser.UserPrincipalName;
						}
					}
					else
					{
						TokenValidationResults tokenValidationResults = current.TokenValidator.FindEmailAddress(xmlElement, ref isDelegationToken);
						if (!string.IsNullOrEmpty(tokenValidationResults.EmailAddress))
						{
							return tokenValidationResults.EmailAddress;
						}
					}
				}
			}
			catch (XmlException ex)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, XmlException>((long)this.GetHashCode(), "[WsSecurityParser::FindAddressFromWsSecurity]: Context {0}; XmlException {1}", this.TraceContext, ex);
				}
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[WsSecurityParser::FindAddressFromWsSecurity]: Context {0}; No email address found in Ws-Trust token", this.TraceContext);
			}
			return null;
		}

		// Token: 0x0400027B RID: 635
		internal const string SoapHeaderElementName = "Header";

		// Token: 0x0400027C RID: 636
		internal const string SecurityHeaderElementName = "Security";

		// Token: 0x0400027D RID: 637
		internal const string EncryptedDataElementName = "EncryptedData";

		// Token: 0x0400027E RID: 638
		internal const string BinarySecurityTokenElementName = "BinarySecurityToken";

		// Token: 0x0400027F RID: 639
		internal const string ExchangeImpersonationElementName = "ExchangeImpersonation";

		// Token: 0x04000280 RID: 640
		internal const string ActionElementName = "Action";

		// Token: 0x04000281 RID: 641
		internal const string BodyElementName = "Body";

		// Token: 0x04000282 RID: 642
		internal const string GetFederationInformationElementName = "GetFederationInformationRequestMessage";

		// Token: 0x04000283 RID: 643
		internal const string RequestElementName = "Request";

		// Token: 0x04000284 RID: 644
		internal const string DomainElementName = "Domain";

		// Token: 0x04000285 RID: 645
		internal const string ConnectingSIDElementName = "ConnectingSID";

		// Token: 0x04000286 RID: 646
		internal const string PrincipalNameElementName = "PrincipalName";

		// Token: 0x04000287 RID: 647
		internal const string PrimarySmtpAddressElementName = "PrimarySmtpAddress";

		// Token: 0x04000288 RID: 648
		internal const string SmtpAddressElementName = "SmtpAddress";

		// Token: 0x04000289 RID: 649
		internal const string SIDElementName = "SID";

		// Token: 0x0400028A RID: 650
		internal const string SamlAttributeElementName = "Attribute";

		// Token: 0x0400028B RID: 651
		internal const string AttributeNameElementName = "AttributeName";

		// Token: 0x0400028C RID: 652
		internal const string AttributeValueElementName = "AttributeValue";

		// Token: 0x0400028D RID: 653
		internal const string TargetTenantAttributeName = "targettenant";

		// Token: 0x0400028E RID: 654
		internal const string Soap11Namespace = "http://schemas.xmlsoap.org/soap/envelope/";

		// Token: 0x0400028F RID: 655
		internal const string Soap12Namespace = "http://www.w3.org/2003/05/soap-envelope";

		// Token: 0x04000290 RID: 656
		internal const string WSSecurity200401SNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

		// Token: 0x04000291 RID: 657
		internal const string XmlEncryptionNamespace = "http://www.w3.org/2001/04/xmlenc#";

		// Token: 0x04000292 RID: 658
		internal const string AddressingNamespace = "http://www.w3.org/2005/08/addressing";

		// Token: 0x04000293 RID: 659
		internal const string NamespaceBase = "http://schemas.microsoft.com/exchange/services/2006";

		// Token: 0x04000294 RID: 660
		internal const string TypeNamespace = "http://schemas.microsoft.com/exchange/services/2006/types";

		// Token: 0x04000295 RID: 661
		internal const string SamlNamespace = "urn:oasis:names:tc:SAML:1.0:assertion";

		// Token: 0x04000296 RID: 662
		internal const int MaxSizeOfHeaders = 73628;
	}
}
