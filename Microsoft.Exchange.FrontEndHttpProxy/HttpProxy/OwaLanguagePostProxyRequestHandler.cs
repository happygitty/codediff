using System;
using System.IO;
using System.Net;
using System.Xml;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000A7 RID: 167
	internal class OwaLanguagePostProxyRequestHandler : OwaProxyRequestHandler
	{
		// Token: 0x060005B9 RID: 1465 RVA: 0x0001FB94 File Offset: 0x0001DD94
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

		// Token: 0x060005BA RID: 1466 RVA: 0x0001FC14 File Offset: 0x0001DE14
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return !string.Equals(headerName, "X-OwaLanguageProxySerializedSecurityContext", StringComparison.OrdinalIgnoreCase) && base.ShouldCopyHeaderToServerRequest(headerName);
		}

		// Token: 0x04000389 RID: 905
		private const string LanguageProxySerializedSecurityContextHeaderName = "X-OwaLanguageProxySerializedSecurityContext";
	}
}
