using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.Security.Authorization;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200006D RID: 109
	[Serializable]
	internal sealed class SerializedSecurityAccessToken : ISecurityAccessToken
	{
		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x06000394 RID: 916 RVA: 0x0001468F File Offset: 0x0001288F
		// (set) Token: 0x06000395 RID: 917 RVA: 0x00014697 File Offset: 0x00012897
		public SidStringAndAttributes[] GroupSids
		{
			get
			{
				return this.groupSids;
			}
			set
			{
				this.groupSids = value;
			}
		}

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x06000396 RID: 918 RVA: 0x000146A0 File Offset: 0x000128A0
		// (set) Token: 0x06000397 RID: 919 RVA: 0x000146A8 File Offset: 0x000128A8
		public SidStringAndAttributes[] RestrictedGroupSids
		{
			get
			{
				return this.restrictedGroupSids;
			}
			set
			{
				this.restrictedGroupSids = value;
			}
		}

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x06000398 RID: 920 RVA: 0x000146B1 File Offset: 0x000128B1
		// (set) Token: 0x06000399 RID: 921 RVA: 0x000146B9 File Offset: 0x000128B9
		public string UserSid
		{
			get
			{
				return this.userSid;
			}
			set
			{
				this.userSid = value;
			}
		}

		// Token: 0x0600039A RID: 922 RVA: 0x000146C4 File Offset: 0x000128C4
		public byte[] GetBytes()
		{
			byte[] array = new byte[this.GetByteCountToSerializeToken()];
			int num = 0;
			SerializedSecurityAccessToken.serializedSecurityAccessTokenCookie.CopyTo(array, num);
			num += SerializedSecurityAccessToken.serializedSecurityAccessTokenCookie.Length;
			array[num++] = 1;
			SerializedSecurityAccessToken.SerializeStringToByteArray(this.UserSid, array, ref num);
			SerializedSecurityAccessToken.SerializeSidArrayToByteArray(this.GroupSids, array, ref num);
			SerializedSecurityAccessToken.SerializeSidArrayToByteArray(this.RestrictedGroupSids, array, ref num);
			return array;
		}

		// Token: 0x0600039B RID: 923 RVA: 0x00014728 File Offset: 0x00012928
		public byte[] GetSecurityContextBytes()
		{
			int num = (this.GroupSids == null) ? 0 : this.GroupSids.Length;
			int num2 = (this.RestrictedGroupSids == null) ? 0 : this.RestrictedGroupSids.Length;
			if (num + num2 > 3000)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<int>(0L, "[SerializedSecurityAccessToken::GetSecurityContextBytes] Token contained more than {0} group sids.", 3000);
				}
				throw new InvalidOperationException();
			}
			byte[] bytes = this.GetBytes();
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
				{
					gzipStream.Write(bytes, 0, bytes.Length);
				}
				memoryStream.Flush();
				result = memoryStream.ToArray();
			}
			return result;
		}

		// Token: 0x0600039C RID: 924 RVA: 0x000147F4 File Offset: 0x000129F4
		private static int GetByteCountToSerializeSidArray(SidStringAndAttributes[] sidArray)
		{
			int num = 0;
			num += 4;
			if (sidArray == null)
			{
				return num;
			}
			foreach (SidStringAndAttributes sidStringAndAttributes in sidArray)
			{
				num += 4;
				num += Encoding.UTF8.GetByteCount(sidStringAndAttributes.SecurityIdentifier);
				num += 4;
			}
			return num;
		}

		// Token: 0x0600039D RID: 925 RVA: 0x0001483C File Offset: 0x00012A3C
		private static void SerializeStringToByteArray(string stringToSerialize, byte[] byteArray, ref int byteIndex)
		{
			int index = byteIndex;
			byteIndex += 4;
			int bytes = Encoding.UTF8.GetBytes(stringToSerialize, 0, stringToSerialize.Length, byteArray, byteIndex);
			byteIndex += bytes;
			BitConverter.GetBytes(bytes).CopyTo(byteArray, index);
		}

		// Token: 0x0600039E RID: 926 RVA: 0x0001487C File Offset: 0x00012A7C
		private static void SerializeSidArrayToByteArray(SidStringAndAttributes[] sidArray, byte[] byteArray, ref int byteIndex)
		{
			if (sidArray == null || sidArray.Length == 0)
			{
				for (int i = 0; i < 4; i++)
				{
					int j = byteIndex;
					byteIndex = j + 1;
					byteArray[j] = 0;
				}
				return;
			}
			BitConverter.GetBytes(sidArray.Length).CopyTo(byteArray, byteIndex);
			byteIndex += 4;
			foreach (SidStringAndAttributes sidStringAndAttributes in sidArray)
			{
				SerializedSecurityAccessToken.SerializeStringToByteArray(sidStringAndAttributes.SecurityIdentifier, byteArray, ref byteIndex);
				BitConverter.GetBytes(sidStringAndAttributes.Attributes).CopyTo(byteArray, byteIndex);
				byteIndex += 4;
			}
		}

		// Token: 0x0600039F RID: 927 RVA: 0x000148F7 File Offset: 0x00012AF7
		private int GetByteCountToSerializeToken()
		{
			return 0 + SerializedSecurityAccessToken.serializedSecurityAccessTokenCookie.Length + 1 + 4 + Encoding.UTF8.GetByteCount(this.UserSid) + SerializedSecurityAccessToken.GetByteCountToSerializeSidArray(this.GroupSids) + SerializedSecurityAccessToken.GetByteCountToSerializeSidArray(this.RestrictedGroupSids);
		}

		// Token: 0x04000255 RID: 597
		private const int SerializedSecurityAccessTokenVersion = 1;

		// Token: 0x04000256 RID: 598
		private static byte[] serializedSecurityAccessTokenCookie = new byte[]
		{
			83,
			83,
			65,
			84
		};

		// Token: 0x04000257 RID: 599
		private string userSid;

		// Token: 0x04000258 RID: 600
		private SidStringAndAttributes[] groupSids;

		// Token: 0x04000259 RID: 601
		private SidStringAndAttributes[] restrictedGroupSids;
	}
}
