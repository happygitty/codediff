using System;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Exchange.Clients.Owa.Core;
using Microsoft.Exchange.Common;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Extensions;
using Microsoft.Exchange.Security.Authentication;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000062 RID: 98
	public class ExpiredPassword : OwaPage
	{
		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x0600032E RID: 814 RVA: 0x00010DA4 File Offset: 0x0000EFA4
		protected ExpiredPassword.ExpiredPasswordReason Reason
		{
			get
			{
				return this.reason;
			}
		}

		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x0600032F RID: 815 RVA: 0x00010DAC File Offset: 0x0000EFAC
		protected string Destination
		{
			get
			{
				string text = base.Request.Form["url"];
				if (string.IsNullOrEmpty(text))
				{
					return "../";
				}
				return text;
			}
		}

		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x06000330 RID: 816 RVA: 0x00010DE0 File Offset: 0x0000EFE0
		protected string UserNameLabel
		{
			get
			{
				switch (OwaVdirConfiguration.Instance.LogonFormat)
				{
				case 1:
					return LocalizedStrings.GetHtmlEncoded(1677919363);
				case 2:
					return LocalizedStrings.GetHtmlEncoded(537815319);
				}
				return LocalizedStrings.GetHtmlEncoded(78658498);
			}
		}

		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x06000331 RID: 817 RVA: 0x00010E2C File Offset: 0x0000F02C
		protected bool PasswordChanged
		{
			get
			{
				return this.passwordChanged;
			}
		}

		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x06000332 RID: 818 RVA: 0x00003193 File Offset: 0x00001393
		protected bool ShouldClearAuthenticationCache
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170000BA RID: 186
		// (get) Token: 0x06000333 RID: 819 RVA: 0x00003165 File Offset: 0x00001365
		protected override bool UseStrictMode
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000334 RID: 820
		[DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern uint NetUserChangePassword(string domainname, string username, IntPtr oldpassword, IntPtr newpassword);

		// Token: 0x06000335 RID: 821 RVA: 0x00010E34 File Offset: 0x0000F034
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			this.reason = ExpiredPassword.ExpiredPasswordReason.None;
			this.passwordChanged = false;
			this.ChangePassword();
			if (this.passwordChanged)
			{
				Utility.DeleteFbaAuthCookies(base.Request, base.Response, false);
			}
		}

		// Token: 0x06000336 RID: 822 RVA: 0x00010E6C File Offset: 0x0000F06C
		private static ExpiredPassword.ChangePasswordResult ChangePasswordNUCP(string logonName, SecureString oldPassword, SecureString newPassword)
		{
			if (logonName == null || oldPassword == null || newPassword == null)
			{
				throw new ArgumentNullException();
			}
			string text = string.Empty;
			string text2 = string.Empty;
			switch (OwaVdirConfiguration.Instance.LogonFormat)
			{
			case 0:
				ExpiredPassword.GetDomainUser(logonName, ref text, ref text2);
				break;
			case 1:
				text = NativeHelpers.GetDomainName();
				text2 = logonName;
				break;
			case 2:
				if (logonName.IndexOf("\\") == -1)
				{
					text2 = logonName;
					text = NativeHelpers.GetDomainName();
				}
				else
				{
					ExpiredPassword.GetDomainUser(logonName, ref text, ref text2);
				}
				break;
			}
			if (text == string.Empty || text2 == string.Empty)
			{
				return ExpiredPassword.ChangePasswordResult.OtherError;
			}
			IntPtr intPtr = IntPtr.Zero;
			IntPtr intPtr2 = IntPtr.Zero;
			try
			{
				intPtr = Marshal.SecureStringToGlobalAllocUnicode(oldPassword);
				intPtr2 = Marshal.SecureStringToGlobalAllocUnicode(newPassword);
				uint num = ExpiredPassword.NetUserChangePassword(text, text2, intPtr, intPtr2);
				if (num != 0U)
				{
					if (num == 5U)
					{
						return ExpiredPassword.ChangePasswordResult.LockedOut;
					}
					if (num == 86U)
					{
						return ExpiredPassword.ChangePasswordResult.InvalidCredentials;
					}
					if (num != 2245U)
					{
						return ExpiredPassword.ChangePasswordResult.OtherError;
					}
					return ExpiredPassword.ChangePasswordResult.BadNewPassword;
				}
			}
			finally
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.ZeroFreeGlobalAllocUnicode(intPtr);
				}
				if (intPtr2 != IntPtr.Zero)
				{
					Marshal.ZeroFreeGlobalAllocUnicode(intPtr2);
				}
			}
			return ExpiredPassword.ChangePasswordResult.Success;
		}

		// Token: 0x06000337 RID: 823 RVA: 0x00010F9C File Offset: 0x0000F19C
		private static void GetDomainUser(string logonName, ref string domain, ref string user)
		{
			string[] array = logonName.Split(new char[]
			{
				'\\'
			});
			if (array.Length == 2)
			{
				domain = array[0];
				user = array[1];
			}
		}

		// Token: 0x06000338 RID: 824 RVA: 0x00010FCC File Offset: 0x0000F1CC
		private static bool SecureStringEquals(SecureString secureStringA, SecureString secureStringB)
		{
			if (secureStringA == null || secureStringB == null || secureStringA.Length != secureStringB.Length)
			{
				return false;
			}
			using (SecureArray<char> secureArray = SecureStringExtensions.ConvertToSecureCharArray(secureStringA))
			{
				using (SecureArray<char> secureArray2 = SecureStringExtensions.ConvertToSecureCharArray(secureStringB))
				{
					for (int i = 0; i < secureStringA.Length; i++)
					{
						if (secureArray.ArrayValue[i] != secureArray2.ArrayValue[i])
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		// Token: 0x06000339 RID: 825 RVA: 0x0001105C File Offset: 0x0000F25C
		private void ChangePassword()
		{
			SecureHtmlFormReader secureHtmlFormReader = new SecureHtmlFormReader(base.Request);
			secureHtmlFormReader.AddSensitiveInputName("oldPwd");
			secureHtmlFormReader.AddSensitiveInputName("newPwd1");
			secureHtmlFormReader.AddSensitiveInputName("newPwd2");
			SecureNameValueCollection secureNameValueCollection = null;
			try
			{
				if (secureHtmlFormReader.TryReadSecureFormData(out secureNameValueCollection))
				{
					string text = null;
					SecureString secureString = null;
					SecureString secureString2 = null;
					SecureString secureString3 = null;
					try
					{
						secureNameValueCollection.TryGetUnsecureValue("username", out text);
						secureNameValueCollection.TryGetSecureValue("oldPwd", out secureString);
						secureNameValueCollection.TryGetSecureValue("newPwd1", out secureString2);
						secureNameValueCollection.TryGetSecureValue("newPwd2", out secureString3);
						if (text != null && secureString != null && secureString2 != null && secureString3 != null)
						{
							if (!ExpiredPassword.SecureStringEquals(secureString2, secureString3))
							{
								this.reason = ExpiredPassword.ExpiredPasswordReason.PasswordConflict;
							}
							else
							{
								switch (ExpiredPassword.ChangePasswordNUCP(text, secureString, secureString2))
								{
								case ExpiredPassword.ChangePasswordResult.Success:
									this.reason = ExpiredPassword.ExpiredPasswordReason.None;
									this.passwordChanged = true;
									break;
								case ExpiredPassword.ChangePasswordResult.InvalidCredentials:
									this.reason = ExpiredPassword.ExpiredPasswordReason.InvalidCredentials;
									break;
								case ExpiredPassword.ChangePasswordResult.LockedOut:
									this.reason = ExpiredPassword.ExpiredPasswordReason.LockedOut;
									break;
								case ExpiredPassword.ChangePasswordResult.BadNewPassword:
									this.reason = ExpiredPassword.ExpiredPasswordReason.InvalidNewPassword;
									break;
								case ExpiredPassword.ChangePasswordResult.OtherError:
									this.reason = ExpiredPassword.ExpiredPasswordReason.InvalidCredentials;
									break;
								}
							}
						}
					}
					finally
					{
						secureString.Dispose();
						secureString2.Dispose();
						secureString3.Dispose();
					}
				}
			}
			finally
			{
				if (secureNameValueCollection != null)
				{
					secureNameValueCollection.Dispose();
				}
			}
		}

		// Token: 0x040001E6 RID: 486
		private const string DestinationParameter = "url";

		// Token: 0x040001E7 RID: 487
		private const string DefaultDestination = "../";

		// Token: 0x040001E8 RID: 488
		private const string UsernameParameter = "username";

		// Token: 0x040001E9 RID: 489
		private const string OldPasswordParameter = "oldPwd";

		// Token: 0x040001EA RID: 490
		private const string NewPassword1Parameter = "newPwd1";

		// Token: 0x040001EB RID: 491
		private const string NewPassword2Parameter = "newPwd2";

		// Token: 0x040001EC RID: 492
		private const int NetUserChangePasswordSuccess = 0;

		// Token: 0x040001ED RID: 493
		private const int NetUserChangePasswordAccessDenied = 5;

		// Token: 0x040001EE RID: 494
		private const int NetUserChangePasswordInvalidOldPassword = 86;

		// Token: 0x040001EF RID: 495
		private const int NetUserChangePasswordDoesNotMeetPolicyRequirement = 2245;

		// Token: 0x040001F0 RID: 496
		private ExpiredPassword.ExpiredPasswordReason reason;

		// Token: 0x040001F1 RID: 497
		private bool passwordChanged;

		// Token: 0x020000FD RID: 253
		protected enum ChangePasswordResult
		{
			// Token: 0x040004AF RID: 1199
			Success,
			// Token: 0x040004B0 RID: 1200
			InvalidCredentials,
			// Token: 0x040004B1 RID: 1201
			LockedOut,
			// Token: 0x040004B2 RID: 1202
			BadNewPassword,
			// Token: 0x040004B3 RID: 1203
			OtherError
		}

		// Token: 0x020000FE RID: 254
		protected enum ExpiredPasswordReason
		{
			// Token: 0x040004B5 RID: 1205
			None,
			// Token: 0x040004B6 RID: 1206
			InvalidCredentials,
			// Token: 0x040004B7 RID: 1207
			InvalidNewPassword,
			// Token: 0x040004B8 RID: 1208
			PasswordConflict,
			// Token: 0x040004B9 RID: 1209
			LockedOut
		}
	}
}
