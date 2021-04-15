using System;
using System.Web;
using Microsoft.Exchange.Clients.Owa.Core;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000067 RID: 103
	public class RedirSuiteServiceProxy : OwaPage
	{
		// Token: 0x0600036D RID: 877 RVA: 0x00013630 File Offset: 0x00011830
		protected override void OnLoad(EventArgs e)
		{
			string value = base.Request.Headers["Host"];
			string text = base.Request.QueryString["suiteServiceReturnUrl"];
			if (!string.IsNullOrEmpty(text))
			{
				string script = string.Format("window.top.location = 'https://{0}/owa/InitSuiteServiceProxy.aspx?{1}={2}'", HttpUtility.JavaScriptStringEncode(value), "suiteServiceReturnUrl", HttpUtility.JavaScriptStringEncode(HttpUtility.UrlEncode(text)));
				base.ClientScript.RegisterClientScriptBlock(base.GetType(), "Redir", script, true);
			}
		}
	}
}
