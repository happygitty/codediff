using System;
using System.Collections.Generic;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C4 RID: 196
	internal static class RpcHttpPackets
	{
		// Token: 0x06000768 RID: 1896 RVA: 0x0002B6C3 File Offset: 0x000298C3
		public static bool IsConnA3PacketInBuffer(ArraySegment<byte> buffer)
		{
			return RpcHttpPackets.CheckPacketInStream(buffer, 28, RpcHttpRtsFlags.None, 1);
		}

		// Token: 0x06000769 RID: 1897 RVA: 0x0002B6CF File Offset: 0x000298CF
		public static bool IsConnC2PacketInBuffer(ArraySegment<byte> buffer)
		{
			return RpcHttpPackets.CheckPacketInStream(buffer, 44, RpcHttpRtsFlags.None, 3);
		}

		// Token: 0x0600076A RID: 1898 RVA: 0x0002B6DC File Offset: 0x000298DC
		public static bool IsPingPacket(ArraySegment<byte> buffer)
		{
			int num;
			return RpcHttpPackets.CheckPacket(buffer, 20, RpcHttpRtsFlags.Ping, 0, out num);
		}

		// Token: 0x0600076B RID: 1899 RVA: 0x0002B6F8 File Offset: 0x000298F8
		private static bool CheckPacketInStream(ArraySegment<byte> buffer, int unitLength, RpcHttpRtsFlags flags, int numberOfCommands)
		{
			while (buffer.Count > 0)
			{
				int num;
				if (RpcHttpPackets.CheckPacket(buffer, unitLength, flags, numberOfCommands, out num))
				{
					return true;
				}
				if (num == 0)
				{
					break;
				}
				int num2 = buffer.Offset + num;
				int num3 = buffer.Count - num;
				if (num2 > buffer.Array.Length || num3 < 0)
				{
					break;
				}
				buffer = new ArraySegment<byte>(buffer.Array, num2, num3);
			}
			return false;
		}

		// Token: 0x0600076C RID: 1900 RVA: 0x0002B758 File Offset: 0x00029958
		private static bool CheckPacket(ArraySegment<byte> buffer, int unitLength, RpcHttpRtsFlags flags, int numberOfCommands, out int unitLengthFound)
		{
			RpcHttpRtsFlags rpcHttpRtsFlags;
			int num;
			return RpcHttpPackets.TryParseRtsHeader(buffer, out unitLengthFound, out rpcHttpRtsFlags, out num) && (unitLengthFound == unitLength && rpcHttpRtsFlags == flags) && num == numberOfCommands;
		}

		// Token: 0x0600076D RID: 1901 RVA: 0x0002B78C File Offset: 0x0002998C
		private static short ReadInt16(IList<byte> buffer, int offset)
		{
			int num = (int)buffer[offset];
			int num2 = (int)buffer[offset + 1] << 31;
			return (short)(num + num2);
		}

		// Token: 0x0600076E RID: 1902 RVA: 0x0002B7B0 File Offset: 0x000299B0
		private static bool TryParseRtsHeader(IList<byte> buffer, out int unitLength, out RpcHttpRtsFlags flags, out int numberOfCommands)
		{
			unitLength = 0;
			flags = RpcHttpRtsFlags.None;
			numberOfCommands = 0;
			if (buffer.Count < 20)
			{
				return false;
			}
			if (buffer[2] != 20)
			{
				return false;
			}
			unitLength = (int)RpcHttpPackets.ReadInt16(buffer, 8);
			flags = (RpcHttpRtsFlags)RpcHttpPackets.ReadInt16(buffer, 16);
			numberOfCommands = (int)RpcHttpPackets.ReadInt16(buffer, 18);
			return true;
		}

		// Token: 0x0400040D RID: 1037
		private const int ConnA3PacketSize = 28;

		// Token: 0x0400040E RID: 1038
		private const int ConnC2PacketSize = 44;

		// Token: 0x0400040F RID: 1039
		private const int PingPacketSize = 20;
	}
}
