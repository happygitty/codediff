using System;
using System.Web;

namespace Microsoft.Exchange.HttpProxy.ErrorPageHandlers
{
	// Token: 0x020000C9 RID: 201
	public class ErrorPageHandlerFactory
	{
		// Token: 0x06000779 RID: 1913 RVA: 0x0002B72C File Offset: 0x0002992C
		public static IErrorPageHandler CreateErrorPageHandler(HttpRequest request)
		{
			string a = (request.QueryString["app"] == null) ? string.Empty : request.QueryString["app"].ToLower();
			IErrorPageHandler result;
			if (a == "mail" || a == "mini" || a == "people" || a == "photohub" || a == "sidebar" || a == "calendar")
			{
				result = new MailErrorPageHandler(request);
			}
			else
			{
				result = new DefaultErrorPageHandler(request);
			}
			return result;
		}

		// Token: 0x0400041E RID: 1054
		public const string AppNameQueryParamKey = "app";

		// Token: 0x0400041F RID: 1055
		public const string MailAppName = "mail";

		// Token: 0x04000420 RID: 1056
		public const string MiniAppName = "mini";

		// Token: 0x04000421 RID: 1057
		public const string PeopleAppName = "people";

		// Token: 0x04000422 RID: 1058
		public const string PhotoHubAppName = "photohub";

		// Token: 0x04000423 RID: 1059
		public const string SideBarAppName = "sidebar";

		// Token: 0x04000424 RID: 1060
		public const string CalendarAppName = "calendar";
	}
}
