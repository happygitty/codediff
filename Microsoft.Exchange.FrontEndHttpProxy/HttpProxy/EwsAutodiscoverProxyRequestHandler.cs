using System;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000093 RID: 147
	internal abstract class EwsAutodiscoverProxyRequestHandler : BEServerCookieProxyRequestHandler<WebServicesService>
	{
		// Token: 0x17000121 RID: 289
		// (get) Token: 0x06000518 RID: 1304 RVA: 0x0001C7A9 File Offset: 0x0001A9A9
		// (set) Token: 0x06000519 RID: 1305 RVA: 0x0001C7B1 File Offset: 0x0001A9B1
		protected bool PreferAnchorMailboxHeader
		{
			get
			{
				return this.preferAnchorMailboxHeader;
			}
			set
			{
				this.preferAnchorMailboxHeader = value;
			}
		}

		// Token: 0x17000122 RID: 290
		// (get) Token: 0x0600051A RID: 1306 RVA: 0x0001C7BA File Offset: 0x0001A9BA
		// (set) Token: 0x0600051B RID: 1307 RVA: 0x0001C7C2 File Offset: 0x0001A9C2
		protected bool SkipTargetBackEndCalculation
		{
			get
			{
				return this.skipTargetBackEndCalculation;
			}
			set
			{
				this.skipTargetBackEndCalculation = value;
			}
		}

		// Token: 0x0600051C RID: 1308 RVA: 0x0001C7CC File Offset: 0x0001A9CC
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			if (this.skipTargetBackEndCalculation)
			{
				base.Logger.Set(3, "OrgRelationship-Anonymous");
				return new AnonymousAnchorMailbox(this);
			}
			if (base.UseRoutingHintForAnchorMailbox)
			{
				string text;
				if (RequestPathParser.IsAutodiscoverV2PreviewRequest(base.ClientRequest.Url.AbsolutePath))
				{
					text = base.ClientRequest.Params["Email"];
				}
				else if (RequestPathParser.IsAutodiscoverV2Version1Request(base.ClientRequest.Url.AbsolutePath))
				{
					int num = base.ClientRequest.Url.AbsolutePath.LastIndexOf('/');
					text = base.ClientRequest.Url.AbsolutePath.Substring(num + 1);
				}
				else
				{
					text = this.TryGetExplicitLogonNode(0);
				}
				string text2;
				if (ExplicitLogonParser.TryGetNormalizedExplicitLogonAddress(text, ref text2) && SmtpAddress.IsValidSmtpAddress(text2))
				{
					this.isExplicitLogonRequest = true;
					this.explicitLogonAddress = text;
					if (HttpProxySettings.NoMailboxFallbackRoutingEnabled.Value)
					{
						base.IsAnchorMailboxFromRoutingHint = true;
					}
					bool failOnDomainNotFound = !RequestPathParser.IsAutodiscoverV2Request(base.ClientRequest.Url.AbsolutePath);
					if (this.preferAnchorMailboxHeader)
					{
						string text3 = base.ClientRequest.Headers[Constants.AnchorMailboxHeaderName];
						if (!string.IsNullOrEmpty(text3) && !StringComparer.OrdinalIgnoreCase.Equals(text3, text2) && SmtpAddress.IsValidSmtpAddress(text3))
						{
							return AnchorMailboxFactory.ParseAnchorMailboxFromSmtp(this, text3, "AnchorMailboxHeader", failOnDomainNotFound);
						}
					}
					return AnchorMailboxFactory.ParseAnchorMailboxFromSmtp(this, text2, "ExplicitLogon", failOnDomainNotFound);
				}
			}
			return base.ResolveAnchorMailbox();
		}

		// Token: 0x0600051D RID: 1309 RVA: 0x00003165 File Offset: 0x00001365
		protected override bool ShouldExcludeFromExplicitLogonParsing()
		{
			return false;
		}

		// Token: 0x0600051E RID: 1310 RVA: 0x0001C940 File Offset: 0x0001AB40
		protected override bool IsValidExplicitLogonNode(string node, bool nodeIsLast)
		{
			if (nodeIsLast)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[AutodiscoverProxyRequestHandler::IsValidExplicitLogonNode]: Context {0}; rejected explicit logon node: {1}", base.TraceContext, node);
				}
				return false;
			}
			return true;
		}

		// Token: 0x0600051F RID: 1311 RVA: 0x0001C974 File Offset: 0x0001AB74
		protected override UriBuilder GetClientUrlForProxy()
		{
			string absoluteUri = base.ClientRequest.Url.AbsoluteUri;
			string uri = absoluteUri;
			if (this.isExplicitLogonRequest && !RequestPathParser.IsAutodiscoverV2Request(base.ClientRequest.Url.AbsoluteUri))
			{
				uri = UrlHelper.RemoveExplicitLogonFromUrlAbsoluteUri(absoluteUri, this.explicitLogonAddress);
			}
			return new UriBuilder(uri);
		}

		// Token: 0x04000356 RID: 854
		private bool preferAnchorMailboxHeader;

		// Token: 0x04000357 RID: 855
		private bool skipTargetBackEndCalculation;

		// Token: 0x04000358 RID: 856
		private bool isExplicitLogonRequest;

		// Token: 0x04000359 RID: 857
		private string explicitLogonAddress;
	}
}
