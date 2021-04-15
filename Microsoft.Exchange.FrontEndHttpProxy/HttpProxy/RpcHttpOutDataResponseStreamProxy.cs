using System;
using System.IO;
using System.Net;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.ExchangeSystem;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200006A RID: 106
	internal class RpcHttpOutDataResponseStreamProxy : StreamProxy
	{
		// Token: 0x0600037A RID: 890 RVA: 0x00013D14 File Offset: 0x00011F14
		internal RpcHttpOutDataResponseStreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, byte[] buffer, IRequestContext requestContext) : base(streamProxyType, source, target, buffer, requestContext)
		{
			this.connectTimeout = RpcHttpOutDataResponseStreamProxy.RpcHttpOutConnectingTimeoutInSeconds.Value;
			this.isConnecting = (this.connectTimeout != TimeSpan.Zero);
		}

		// Token: 0x0600037B RID: 891 RVA: 0x00013D54 File Offset: 0x00011F54
		internal RpcHttpOutDataResponseStreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, BufferPoolCollection.BufferSize maxBufferSize, BufferPoolCollection.BufferSize minBufferSize, IRequestContext requestContext) : base(streamProxyType, source, target, maxBufferSize, minBufferSize, requestContext)
		{
			this.connectTimeout = RpcHttpOutDataResponseStreamProxy.RpcHttpOutConnectingTimeoutInSeconds.Value;
			this.isConnecting = (this.connectTimeout != TimeSpan.Zero);
		}

		// Token: 0x0600037C RID: 892 RVA: 0x00013DA4 File Offset: 0x00011FA4
		protected override byte[] GetUpdatedBufferToSend(ArraySegment<byte> buffer)
		{
			if (!this.isConnecting)
			{
				return null;
			}
			if (RpcHttpPackets.IsConnA3PacketInBuffer(buffer))
			{
				this.endTime = new ExDateTime?(ExDateTime.Now + this.connectTimeout);
			}
			if (RpcHttpPackets.IsConnC2PacketInBuffer(buffer))
			{
				this.isConnecting = false;
				this.endTime = null;
			}
			if (RpcHttpPackets.IsPingPacket(buffer) && this.endTime != null && ExDateTime.Now >= this.endTime.Value)
			{
				throw new HttpProxyException(HttpStatusCode.InternalServerError, 2013, "Outbound proxy connection timed out");
			}
			return null;
		}

		// Token: 0x04000246 RID: 582
		private static readonly TimeSpanAppSettingsEntry RpcHttpOutConnectingTimeoutInSeconds = new TimeSpanAppSettingsEntry(HttpProxySettings.Prefix("RpcHttpOutConnectingTimeoutInSeconds"), 0, TimeSpan.FromSeconds(0.0), ExTraceGlobals.VerboseTracer);

		// Token: 0x04000247 RID: 583
		private readonly TimeSpan connectTimeout = TimeSpan.Zero;

		// Token: 0x04000248 RID: 584
		private bool isConnecting;

		// Token: 0x04000249 RID: 585
		private ExDateTime? endTime;
	}
}
