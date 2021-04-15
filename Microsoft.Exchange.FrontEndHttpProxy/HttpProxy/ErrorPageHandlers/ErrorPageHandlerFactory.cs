using System;
using System.Web;

namespace Microsoft.Exchange.HttpProxy.ErrorPageHandlers
{
	// Token: 0x020000C8 RID: 200
	public class ErrorPageHandlerFactory
	{
		// Token: 0x06000774 RID: 1908 RVA: 0x0002B914 File Offset: 0x00029B14
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

		// Token: 0x04000422 RID: 1058
		public const string AppNameQueryParamKey = "app";

		// Token: 0x04000423 RID: 1059
		public const string MailAppName = "mail";

		// Token: 0x04000424 RID: 1060
		public const string MiniAppName = "mini";

		// Token: 0x04000425 RID: 1061
		public const string PeopleAppName = "people";

		// Token: 0x04000426 RID: 1062
		public const string PhotoHubAppName = "photohub";

		// Token: 0x04000427 RID: 1063
		public const string SideBarAppName = "sidebar";

		// Token: 0x04000428 RID: 1064
		public const string CalendarAppName = "calendar";
	}
}
