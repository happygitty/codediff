using System;
using System.Collections.Generic;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C5 RID: 197
	internal static class RpcHttpPackets
	{
		// Token: 0x0600076D RID: 1901 RVA: 0x0002B4DB File Offset: 0x000296DB
		public static bool IsConnA3PacketInBuffer(ArraySegment<byte> buffer)
		{
			return RpcHttpPackets.CheckPacketInStream(buffer, 28, RpcHttpRtsFlags.None, 1);
		}

		// Token: 0x0600076E RID: 1902 RVA: 0x0002B4E7 File Offset: 0x000296E7
		public static bool IsConnC2PacketInBuffer(ArraySegment<byte> buffer)
		{
			return RpcHttpPackets.CheckPacketInStream(buffer, 44, RpcHttpRtsFlags.None, 3);
		}

		// Token: 0x0600076F RID: 1903 RVA: 0x0002B4F4 File Offset: 0x000296F4
		public static bool IsPingPacket(ArraySegment<byte> buffer)
		{
			int num;
			return RpcHttpPackets.CheckPacket(buffer, 20, RpcHttpRtsFlags.Ping, 0, out num);
		}

		// Token: 0x06000770 RID: 1904 RVA: 0x0002B510 File Offset: 0x00029710
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

		// Token: 0x06000771 RID: 1905 RVA: 0x0002B570 File Offset: 0x00029770
		private static bool CheckPacket(ArraySegment<byte> buffer, int unitLength, RpcHttpRtsFlags flags, int numberOfCommands, out int unitLengthFound)
		{
			RpcHttpRtsFlags rpcHttpRtsFlags;
			int num;
			return RpcHttpPackets.TryParseRtsHeader(buffer, out unitLengthFound, out rpcHttpRtsFlags, out num) && (unitLengthFound == unitLength && rpcHttpRtsFlags == flags) && num == numberOfCommands;
		}

		// Token: 0x06000772 RID: 1906 RVA: 0x0002B5A4 File Offset: 0x000297A4
		private static short ReadInt16(IList<byte> buffer, int offset)
		{
			int num = (int)buffer[offset];
			int num2 = (int)buffer[offset + 1] << 31;
			return (short)(num + num2);
		}

		// Token: 0x06000773 RID: 1907 RVA: 0x0002B5C8 File Offset: 0x000297C8
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

		// Token: 0x04000409 RID: 1033
		private const int ConnA3PacketSize = 28;

		// Token: 0x0400040A RID: 1034
		private const int ConnC2PacketSize = 44;

		// Token: 0x0400040B RID: 1035
		private const int PingPacketSize = 20;
	}
}
