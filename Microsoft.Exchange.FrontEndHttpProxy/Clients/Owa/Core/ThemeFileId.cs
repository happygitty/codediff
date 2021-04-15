using System;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x02000010 RID: 16
	public enum ThemeFileId
	{
		// Token: 0x04000087 RID: 135
		[ThemeFileInfo]
		None,
		// Token: 0x04000088 RID: 136
		[ThemeFileInfo("owafont.css", ThemeFileInfoFlags.Resource)]
		OwaFontCss,
		// Token: 0x04000089 RID: 137
		[ThemeFileInfo("logon.css", ThemeFileInfoFlags.Resource)]
		LogonCss,
		// Token: 0x0400008A RID: 138
		[ThemeFileInfo("errorFE.css", ThemeFileInfoFlags.Resource)]
		ErrorFECss,
		// Token: 0x0400008B RID: 139
		[ThemeFileInfo("icon_settings.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		OwaSettings,
		// Token: 0x0400008C RID: 140
		[ThemeFileInfo("olk_logo_white.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		OutlookLogoWhite,
		// Token: 0x0400008D RID: 141
		[ThemeFileInfo("olk_logo_white_cropped.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		OutlookLogoWhiteCropped,
		// Token: 0x0400008E RID: 142
		[ThemeFileInfo("olk_logo_white_small.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		OutlookLogoWhiteSmall,
		// Token: 0x0400008F RID: 143
		[ThemeFileInfo("owa_text_blue.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		OwaHeaderTextBlue,
		// Token: 0x04000090 RID: 144
		[ThemeFileInfo("bg_gradient.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		BackgroundGradient,
		// Token: 0x04000091 RID: 145
		[ThemeFileInfo("bg_gradient_login.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		BackgroundGradientLogin,
		// Token: 0x04000092 RID: 146
		[ThemeFileInfo("Sign_in_arrow.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		SignInArrow,
		// Token: 0x04000093 RID: 147
		[ThemeFileInfo("Sign_in_arrow_rtl.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		SignInArrowRtl,
		// Token: 0x04000094 RID: 148
		[ThemeFileInfo("warn.png")]
		Error,
		// Token: 0x04000095 RID: 149
		[ThemeFileInfo("lgntopl.gif", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		LogonTopLeft,
		// Token: 0x04000096 RID: 150
		[ThemeFileInfo("lgntopm.gif", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		LogonTopMiddle,
		// Token: 0x04000097 RID: 151
		[ThemeFileInfo("lgntopr.gif", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		LogonTopRight,
		// Token: 0x04000098 RID: 152
		[ThemeFileInfo("lgnbotl.gif", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		LogonBottomLeft,
		// Token: 0x04000099 RID: 153
		[ThemeFileInfo("lgnbotm.gif", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		LogonBottomMiddle,
		// Token: 0x0400009A RID: 154
		[ThemeFileInfo("lgnbotr.gif", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		LogonBottomRight,
		// Token: 0x0400009B RID: 155
		[ThemeFileInfo("lgnexlogo.gif", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		LogonExchangeLogo,
		// Token: 0x0400009C RID: 156
		[ThemeFileInfo("lgnleft.gif", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		LogonLeft,
		// Token: 0x0400009D RID: 157
		[ThemeFileInfo("lgnright.gif", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		LogonRight,
		// Token: 0x0400009E RID: 158
		[ThemeFileInfo("favicon.ico", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		FavoriteIcon,
		// Token: 0x0400009F RID: 159
		[ThemeFileInfo("favicon_office.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		FaviconOffice,
		// Token: 0x040000A0 RID: 160
		[ThemeFileInfo("icp.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		ICPNum,
		// Token: 0x040000A1 RID: 161
		[ThemeFileInfo("office365_cn.png", ThemeFileInfoFlags.LooseImage | ThemeFileInfoFlags.Resource)]
		Office365CnLogo,
		// Token: 0x040000A2 RID: 162
		[ThemeFileInfo]
		Count
	}
}
