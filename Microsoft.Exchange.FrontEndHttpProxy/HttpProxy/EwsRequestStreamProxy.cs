using System;
using System.IO;
using System.Text;
using System.Web;
using Microsoft.Exchange.Security.Authorization;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000069 RID: 105
	internal class EwsRequestStreamProxy : StreamProxy
	{
		// Token: 0x06000376 RID: 886 RVA: 0x00013971 File Offset: 0x00011B71
		public EwsRequestStreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, byte[] buffer, IRequestContext requestContext, bool shouldInsertSecurityContext, bool shouldInsertFreeBusyDefaultSecurityContext, string requestVersionToAdd) : base(streamProxyType, source, target, buffer, requestContext)
		{
			this.shouldInsertSecurityContext = shouldInsertSecurityContext;
			this.shouldInsertFreeBusyDefaultSecurityContext = shouldInsertFreeBusyDefaultSecurityContext;
			this.requestVersionToAdd = requestVersionToAdd;
		}

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x06000377 RID: 887 RVA: 0x00013998 File Offset: 0x00011B98
		public static byte[] FreeBusyPermissionDefaultSecurityAccessToken
		{
			get
			{
				if (EwsRequestStreamProxy.freeBusyPermissionDefaultSecurityContextBytes == null)
				{
					SerializedSecurityAccessToken serializedSecurityAccessToken = new SerializedSecurityAccessToken();
					try
					{
						using (ClientSecurityContext clientSecurityContext = ClientSecurityContext.FreeBusyPermissionDefaultClientSecurityContext.Clone())
						{
							clientSecurityContext.SetSecurityAccessToken(serializedSecurityAccessToken);
						}
					}
					catch (AuthzException ex)
					{
						throw new HttpException(401, ex.Message);
					}
					EwsRequestStreamProxy.freeBusyPermissionDefaultSecurityContextBytes = serializedSecurityAccessToken.GetSecurityContextBytes();
				}
				return EwsRequestStreamProxy.freeBusyPermissionDefaultSecurityContextBytes;
			}
		}

		// Token: 0x06000378 RID: 888 RVA: 0x00013A10 File Offset: 0x00011C10
		protected override byte[] GetUpdatedBufferToSend(ArraySegment<byte> buffer)
		{
			if (this.haveAddedEwsProxyHeader)
			{
				return null;
			}
			if ((long)buffer.Count + base.TotalBytesProxied < (long)"<Body".Length)
			{
				return null;
			}
			string @string = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
			int num = @string.IndexOf("<Header", StringComparison.OrdinalIgnoreCase);
			if (num == -1)
			{
				num = @string.IndexOf(":Header", StringComparison.OrdinalIgnoreCase);
				if (num != -1)
				{
					num = @string.LastIndexOf('<', num - 1);
				}
			}
			int num2 = @string.IndexOf("<Body", StringComparison.OrdinalIgnoreCase);
			if (num2 == -1)
			{
				num2 = @string.IndexOf(":Body", StringComparison.OrdinalIgnoreCase);
				if (num2 != -1)
				{
					num2 = @string.LastIndexOf('<', num2 - 1);
				}
			}
			byte[] array = new byte[0];
			if (this.shouldInsertSecurityContext)
			{
				byte[] inArray = this.shouldInsertFreeBusyDefaultSecurityContext ? EwsRequestStreamProxy.FreeBusyPermissionDefaultSecurityAccessToken : base.RequestContext.CreateSerializedSecurityAccessToken();
				string s = "<ProxySecurityContext xmlns='http://schemas.microsoft.com/exchange/services/2006/types'>" + Convert.ToBase64String(inArray) + "</ProxySecurityContext>";
				array = Encoding.UTF8.GetBytes(s);
			}
			byte[] array2;
			if (num != -1 && this.shouldInsertSecurityContext)
			{
				int num3 = @string.IndexOf('>', num) + 1;
				if (num3 == 0)
				{
					throw new HttpException(400, "Bad open element of SOAP header.");
				}
				array2 = new byte[buffer.Count + array.Length];
				Array.Copy(buffer.Array, buffer.Offset, array2, 0, num3);
				Array.Copy(array, 0, array2, num3, array.Length);
				Array.Copy(buffer.Array, buffer.Offset + num3, array2, num3 + array.Length, buffer.Count - num3);
			}
			else
			{
				if (num2 == -1)
				{
					throw new HttpException(400, "Cannot find the appropriate SOAP header or body.");
				}
				string text = "<Header xmlns='http://schemas.xmlsoap.org/soap/envelope/'>";
				if (this.requestVersionToAdd != null)
				{
					text += "<RequestServerVersion xmlns='http://schemas.microsoft.com/exchange/services/2006/types' Version='";
					text += this.requestVersionToAdd;
					text += "' />";
				}
				byte[] bytes = Encoding.UTF8.GetBytes(text);
				byte[] bytes2 = Encoding.UTF8.GetBytes("</Header>");
				array2 = new byte[buffer.Count + bytes.Length + array.Length + bytes2.Length];
				Array.Copy(buffer.Array, buffer.Offset, array2, 0, num2);
				Array.Copy(bytes, 0, array2, num2, bytes.Length);
				if (this.shouldInsertSecurityContext)
				{
					Array.Copy(array, 0, array2, num2 + bytes.Length, array.Length);
				}
				Array.Copy(bytes2, 0, array2, num2 + bytes.Length + array.Length, bytes2.Length);
				Array.Copy(buffer.Array, buffer.Offset + num2, array2, num2 + bytes.Length + array.Length + bytes2.Length, buffer.Count - num2);
			}
			this.haveAddedEwsProxyHeader = true;
			return array2;
		}

		// Token: 0x06000379 RID: 889 RVA: 0x00013CC9 File Offset: 0x00011EC9
		protected override void OnTargetStreamUpdate()
		{
			base.OnTargetStreamUpdate();
			this.haveAddedEwsProxyHeader = false;
		}

		// Token: 0x04000236 RID: 566
		private const string BeginOfSoapHeaderTagNoNamespace = "<Header";

		// Token: 0x04000237 RID: 567
		private const string BeginOfSoapHeaderTagWithNamespace = ":Header";

		// Token: 0x04000238 RID: 568
		private const string BeginOfSoapBodyTagNoNamespace = "<Body";

		// Token: 0x04000239 RID: 569
		private const string BeginOfSoapBodyTagWithNamespace = ":Body";

		// Token: 0x0400023A RID: 570
		private const string SoapHeaderBegin = "<Header xmlns='http://schemas.xmlsoap.org/soap/envelope/'>";

		// Token: 0x0400023B RID: 571
		private const string SoapHeaderEnd = "</Header>";

		// Token: 0x0400023C RID: 572
		private const string RequestServerVersionHeaderStart = "<RequestServerVersion xmlns='http://schemas.microsoft.com/exchange/services/2006/types' Version='";

		// Token: 0x0400023D RID: 573
		private const string RequestServerVersionHeaderEnd = "' />";

		// Token: 0x0400023E RID: 574
		private const string ProxySecurityContextHeaderStart = "<ProxySecurityContext xmlns='http://schemas.microsoft.com/exchange/services/2006/types'>";

		// Token: 0x0400023F RID: 575
		private const string ProxySecurityContextHeaderEnd = "</ProxySecurityContext>";

		// Token: 0x04000240 RID: 576
		private static byte[] freeBusyPermissionDefaultSecurityContextBytes;

		// Token: 0x04000241 RID: 577
		private readonly bool shouldInsertSecurityContext;

		// Token: 0x04000242 RID: 578
		private readonly bool shouldInsertFreeBusyDefaultSecurityContext;

		// Token: 0x04000243 RID: 579
		private readonly string requestVersionToAdd;

		// Token: 0x04000244 RID: 580
		private bool haveAddedEwsProxyHeader;
	}
}
