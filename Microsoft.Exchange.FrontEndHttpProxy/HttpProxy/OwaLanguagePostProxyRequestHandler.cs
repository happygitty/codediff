using System;
using System.IO;
using System.Net;
using System.Xml;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A7 RID: 167
	internal class OwaLanguagePostProxyRequestHandler : OwaProxyRequestHandler
	{
		// Token: 0x060005BC RID: 1468 RVA: 0x0001FD38 File Offset: 0x0001DF38
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			if (base.ProxyToDownLevel)
			{
				using (StringWriter stringWriter = new StringWriter())
				{
					using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
					{
						this.GetSerializedClientSecurityContext().Serialize(xmlTextWriter);
						stringWriter.Flush();
						headers["X-OwaLanguageProxySerializedSecurityContext"] = stringWriter.ToString();
					}
				}
			}
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x060005BD RID: 1469 RVA: 0x0001FDB8 File Offset: 0x0001DFB8
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return !string.Equals(headerName, "X-OwaLanguageProxySerializedSecurityContext", StringComparison.OrdinalIgnoreCase) && base.ShouldCopyHeaderToServerRequest(headerName);
		}

		// Token: 0x0400038D RID: 909
		private const string LanguageProxySerializedSecurityContextHeaderName = "X-OwaLanguageProxySerializedSecurityContext";
	}
}
