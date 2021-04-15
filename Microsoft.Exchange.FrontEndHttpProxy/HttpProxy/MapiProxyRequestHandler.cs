using System;
using System.Linq;
using System.Net;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.Net.MapiHttp;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200009C RID: 156
	internal class MapiProxyRequestHandler : BEServerCookieProxyRequestHandler<WebServicesService>
	{
		// Token: 0x1700012B RID: 299
		// (get) Token: 0x06000563 RID: 1379 RVA: 0x0001981A File Offset: 0x00017A1A
		protected override ClientAccessType ClientAccessType
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x1700012C RID: 300
		// (get) Token: 0x06000564 RID: 1380 RVA: 0x00003193 File Offset: 0x00001393
		protected override bool ShouldForceUnbufferedClientResponseOutput
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700012D RID: 301
		// (get) Token: 0x06000565 RID: 1381 RVA: 0x00003165 File Offset: 0x00001365
		protected override bool ShouldSendFullActivityScope
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000566 RID: 1382 RVA: 0x0001E298 File Offset: 0x0001C498
		protected override BufferPool GetResponseBufferPool()
		{
			if (MapiProxyRequestHandler.UseCustomNotificationWaitBuffers.Value)
			{
				string text = base.ClientRequest.Headers["X-RequestType"];
				if (!string.IsNullOrEmpty(text) && string.Equals(text, "NotificationWait", StringComparison.OrdinalIgnoreCase))
				{
					return MapiProxyRequestHandler.NotificationWaitBufferPool;
				}
			}
			return base.GetResponseBufferPool();
		}

		// Token: 0x06000567 RID: 1383 RVA: 0x0001E2EC File Offset: 0x0001C4EC
		protected override bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			if (MapiProxyRequestHandler.ProtectedHeaderNames.Contains(headerName, StringComparer.OrdinalIgnoreCase))
			{
				return false;
			}
			bool flag = base.ShouldCopyHeaderToServerRequest(headerName);
			return (!flag && string.Equals(headerName, "client-request-id", StringComparison.OrdinalIgnoreCase)) || flag;
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x0001E329 File Offset: 0x0001C529
		protected override void DoProtocolSpecificBeginRequestLogging()
		{
			this.LogClientRequestInfo();
		}

		// Token: 0x06000569 RID: 1385 RVA: 0x0001E334 File Offset: 0x0001C534
		protected override void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			DatabaseBasedAnchorMailbox databaseBasedAnchorMailbox = base.AnchoredRoutingTarget.AnchorMailbox as DatabaseBasedAnchorMailbox;
			if (databaseBasedAnchorMailbox != null)
			{
				ADObjectId database = databaseBasedAnchorMailbox.GetDatabase();
				if (database != null)
				{
					headers["X-DatabaseGuid"] = database.ObjectGuid.ToString();
				}
			}
			base.AddProtocolSpecificHeadersToServerRequest(headers);
		}

		// Token: 0x0600056A RID: 1386 RVA: 0x0001E388 File Offset: 0x0001C588
		protected override AnchorMailbox ResolveAnchorMailbox()
		{
			string text;
			if (RequestQueryStringParser.TryGetMailboxId(base.ClientRequest.QueryString, ref text))
			{
				base.Logger.Set(3, "MailboxGuidWithDomain");
				return this.GetAnchorMailboxFromMailboxId(text);
			}
			if (RequestQueryStringParser.TryGetSmtpAddress(base.ClientRequest.QueryString, ref text))
			{
				base.Logger.Set(3, "SMTP");
				return this.GetAnchorMailboxFromSmtpAddress(text);
			}
			bool flag = false;
			if (RequestQueryStringParser.TryGetUseMailboxOfAuthenticatedUser(base.ClientRequest.QueryString, ref text) && bool.TryParse(text, out flag) && flag)
			{
				return base.ResolveAnchorMailbox();
			}
			if (string.Compare(base.ClientRequest.RequestType, "GET", true) == 0)
			{
				return base.ResolveAnchorMailbox();
			}
			throw new HttpProxyException(HttpStatusCode.BadRequest, 3003, "No target mailbox specified.");
		}

		// Token: 0x0600056B RID: 1387 RVA: 0x0001E45C File Offset: 0x0001C65C
		private AnchorMailbox GetAnchorMailboxFromMailboxId(string mailboxId)
		{
			Guid guid = Guid.Empty;
			string domain = string.Empty;
			if (!SmtpAddress.IsValidSmtpAddress(mailboxId))
			{
				throw new HttpProxyException(HttpStatusCode.BadRequest, 3003, "Malformed mailbox id.");
			}
			try
			{
				SmtpAddress smtpAddress;
				smtpAddress..ctor(mailboxId);
				guid = new Guid(smtpAddress.Local);
				domain = smtpAddress.Domain;
			}
			catch (FormatException innerException)
			{
				throw new HttpProxyException(HttpStatusCode.BadRequest, 3003, string.Format("Invalid mailboxGuid {0}", guid), innerException);
			}
			return new MailboxGuidAnchorMailbox(guid, domain, this);
		}

		// Token: 0x0600056C RID: 1388 RVA: 0x0001E4EC File Offset: 0x0001C6EC
		private AnchorMailbox GetAnchorMailboxFromSmtpAddress(string smtpAddress)
		{
			if (!SmtpAddress.IsValidSmtpAddress(smtpAddress))
			{
				throw new HttpProxyException(HttpStatusCode.BadRequest, 3003, "Malformed smtp address.");
			}
			return new SmtpAnchorMailbox(smtpAddress, this);
		}

		// Token: 0x0600056D RID: 1389 RVA: 0x0001E514 File Offset: 0x0001C714
		private void LogClientRequestInfo()
		{
			if (string.Compare(base.ClientRequest.RequestType, "POST", true) != 0)
			{
				return;
			}
			string clientRequestInfo = MapiHttpEndpoints.GetClientRequestInfo(base.HttpContext);
			base.ClientResponse.AppendToLog("&ClientRequestInfo=" + clientRequestInfo);
			base.Logger.Set(13, clientRequestInfo);
		}

		// Token: 0x0400036D RID: 877
		private const string HttpVerbGet = "GET";

		// Token: 0x0400036E RID: 878
		private const string HttpVerbPost = "POST";

		// Token: 0x0400036F RID: 879
		private const string XRequestType = "X-RequestType";

		// Token: 0x04000370 RID: 880
		private const string ClientRequestInfoLogParameter = "&ClientRequestInfo=";

		// Token: 0x04000371 RID: 881
		private const string RequestTypeEmsmdbNotificationWait = "NotificationWait";

		// Token: 0x04000372 RID: 882
		private static readonly string[] ProtectedHeaderNames = new string[]
		{
			"X-DatabaseGuid"
		};

		// Token: 0x04000373 RID: 883
		private static readonly BoolAppSettingsEntry UseCustomNotificationWaitBuffers = new BoolAppSettingsEntry(HttpProxySettings.Prefix("UseCustomNotificationWaitBuffers"), true, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000374 RID: 884
		private static readonly IntAppSettingsEntry NotificationWaitBufferSize = new IntAppSettingsEntry(HttpProxySettings.Prefix("NotificationWaitBufferSize"), 256, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000375 RID: 885
		private static readonly IntAppSettingsEntry NotificationWaitBuffersPerProcessor = new IntAppSettingsEntry(HttpProxySettings.Prefix("NotificationWaitBuffersPerProcessor"), 512, ExTraceGlobals.VerboseTracer);

		// Token: 0x04000376 RID: 886
		private static readonly BufferPool NotificationWaitBufferPool = new BufferPool(MapiProxyRequestHandler.NotificationWaitBufferSize.Value, MapiProxyRequestHandler.NotificationWaitBuffersPerProcessor.Value);
	}
}
