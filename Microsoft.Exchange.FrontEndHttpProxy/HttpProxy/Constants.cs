using System;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Exchange.HttpProxy.Common;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000075 RID: 117
	internal static class Constants
	{
		// Token: 0x040002A5 RID: 677
		public static readonly string MsExchProxyUri = Constants.MsExchProxyUri;

		// Token: 0x040002A6 RID: 678
		public static readonly string XIsFromCafe = Constants.XIsFromCafe;

		// Token: 0x040002A7 RID: 679
		public static readonly string XSourceCafeServer = Constants.XSourceCafeServer;

		// Token: 0x040002A8 RID: 680
		public static readonly string XBackendHeaderPrefix = Constants.XBackendHeaderPrefix;

		// Token: 0x040002A9 RID: 681
		public static readonly string XRequestId = "X-RequestId";

		// Token: 0x040002AA RID: 682
		public static readonly string TargetDatabaseHeaderName = "X-TargetDatabase";

		// Token: 0x040002AB RID: 683
		public static readonly string ClientVersionHeaderName = "X-ExchClientVersion";

		// Token: 0x040002AC RID: 684
		public static readonly string BEServerExceptionHeaderName = "X-BEServerException";

		// Token: 0x040002AD RID: 685
		public static readonly string IllegalCrossServerConnectionExceptionType = "Microsoft.Exchange.Data.Storage.IllegalCrossServerConnectionException";

		// Token: 0x040002AE RID: 686
		public static readonly string RumCouldNotFindDatabaseSerializedString = "Error:Could%20not%20find%20database%3A%20";

		// Token: 0x040002AF RID: 687
		public const string DbGuidSerializationString = "DatabaseGuid";

		// Token: 0x040002B0 RID: 688
		public static readonly string BEServerRoutingErrorHeaderName = "X-BEServerRoutingError";

		// Token: 0x040002B1 RID: 689
		public static readonly string WLIDMemberNameHeaderName = Constants.WLIDMemberNameHeaderName;

		// Token: 0x040002B2 RID: 690
		public static readonly string WLIDOrganizationContextHeaderName = Constants.WLIDOrganizationContextHeaderName;

		// Token: 0x040002B3 RID: 691
		public static readonly string LiveIdEnvironment = "RPSEnv";

		// Token: 0x040002B4 RID: 692
		public static readonly string LiveIdPuid = "RPSPUID";

		// Token: 0x040002B5 RID: 693
		public static readonly string OrgIdPuid = "RPSOrgIdPUID";

		// Token: 0x040002B6 RID: 694
		public static readonly string LiveIdMemberName = Constants.LiveIdMemberName;

		// Token: 0x040002B7 RID: 695
		public static readonly string AcceptEncodingHeaderName = "Accept-Encoding";

		// Token: 0x040002B8 RID: 696
		public static readonly string TestBackEndUrlRequestHeaderKey = "TestBackEndUrl";

		// Token: 0x040002B9 RID: 697
		public static readonly string CafeErrorCodeHeaderName = "X-CasErrorCode";

		// Token: 0x040002BA RID: 698
		public static readonly string CaptureResponseIdHeaderKey = "CaptureResponseId";

		// Token: 0x040002BB RID: 699
		public static readonly string ProbeHeaderName = Constants.ProbeHeaderName;

		// Token: 0x040002BC RID: 700
		public static readonly string LocalProbeHeaderValue = "X-MS-ClientAccess-LocalProbe";

		// Token: 0x040002BD RID: 701
		public static readonly string AuthorizationHeader = "Authorization";

		// Token: 0x040002BE RID: 702
		public static readonly string AuthenticationHeader = "WWW-Authenticate";

		// Token: 0x040002BF RID: 703
		public static readonly string FrontEndToBackEndTimeout = "X-FeToBeTimeout";

		// Token: 0x040002C0 RID: 704
		public static readonly string BEResourcePath = "X-BEResourcePath";

		// Token: 0x040002C1 RID: 705
		public static readonly string VDirObjectID = "X-vDirObjectId";

		// Token: 0x040002C2 RID: 706
		public static readonly string MissingDirectoryUserObjectHeader = "X-MissingDirectoryUserObjectHint";

		// Token: 0x040002C3 RID: 707
		public static readonly string OrganizationContextHeader = "X-OrganizationContext";

		// Token: 0x040002C4 RID: 708
		public static readonly string RequestCompletedHttpContextKeyName = "RequestCompleted";

		// Token: 0x040002C5 RID: 709
		public static readonly string LatencyTrackerContextKeyName = "LatencyTracker";

		// Token: 0x040002C6 RID: 710
		public static readonly string TraceContextKey = "TraceContext";

		// Token: 0x040002C7 RID: 711
		public static readonly string RequestIdHttpContextKeyName = "LogRequestId";

		// Token: 0x040002C8 RID: 712
		public static readonly string CallerADRawEntryKeyName = "CallerADRawEntry";

		// Token: 0x040002C9 RID: 713
		public static readonly string MissingDirectoryUserObjectKey = "MissingDirectoryUserObject";

		// Token: 0x040002CA RID: 714
		public static readonly string WLIDMemberName = Constants.WLIDMemberName;

		// Token: 0x040002CB RID: 715
		public static readonly string GzipHeaderValue = "gzip";

		// Token: 0x040002CC RID: 716
		public static readonly string DeflateHeaderValue = "deflate";

		// Token: 0x040002CD RID: 717
		public static readonly string IsFromCafeHeaderValue = Constants.IsFromCafeHeaderValue;

		// Token: 0x040002CE RID: 718
		public static readonly string SpnPrefixForHttp = "HTTP/";

		// Token: 0x040002CF RID: 719
		public static readonly string NegotiatePackageValue = "Negotiate";

		// Token: 0x040002D0 RID: 720
		public static readonly string NtlmPackageValue = "NTLM";

		// Token: 0x040002D1 RID: 721
		public static readonly string KerberosPackageValue = "Kerberos";

		// Token: 0x040002D2 RID: 722
		public static readonly string PrefixForKerbAuthBlob = "Negotiate ";

		// Token: 0x040002D3 RID: 723
		public static readonly string RPSBackEndServerCookieName = "MS-WSMAN";

		// Token: 0x040002D4 RID: 724
		public static readonly string LiveIdRPSAuth = "RPSAuth";

		// Token: 0x040002D5 RID: 725
		public static readonly string LiveIdRPSSecAuth = "RPSSecAuth";

		// Token: 0x040002D6 RID: 726
		public static readonly string LiveIdRPSTAuth = "RPSTAuth";

		// Token: 0x040002D7 RID: 727
		public static readonly string BEResource = "X-BEResource";

		// Token: 0x040002D8 RID: 728
		public static readonly string AnonResource = "X-AnonResource";

		// Token: 0x040002D9 RID: 729
		public static readonly string AnonResourceBackend = "X-AnonResource-Backend";

		// Token: 0x040002DA RID: 730
		public static readonly string BackEndOverrideCookieName = Constants.BackEndOverrideCookieName;

		// Token: 0x040002DB RID: 731
		public static readonly string PreferServerAffinityHeader = Constants.PreferServerAffinityHeader;

		// Token: 0x040002DC RID: 732
		public static readonly Regex SidRegex = new Regex("(?<sid>S-1-5-\\d{2}-\\d{9,}-\\d{9,}-\\d{9,}-\\d{4,})@(?<domain>[\\S.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x040002DD RID: 733
		public static readonly Regex SidOnlyRegex = new Regex("(?<sid>S-1-5-\\d{2}-\\d{9,}-\\d{9,}-\\d{9,}-\\d{4,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x040002DE RID: 734
		public static readonly Regex ExchClientVerRegex = new Regex("(?<major>\\d{2})\\.(?<minor>\\d{1,})\\.(?<build>\\d{1,})\\.(?<revision>\\d{1,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x040002DF RID: 735
		public static readonly Regex NoLeadingZeroRegex = new Regex("0*([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x040002E0 RID: 736
		public static readonly Regex NoRevisionNumberRegex = new Regex("^([0-9]+\\.[0-9]+\\.[0-9]+)(\\.[0-9]+)*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x040002E1 RID: 737
		public static readonly string CertificateValidationComponentId = "ClientAccessFrontEnd";

		// Token: 0x040002E2 RID: 738
		public static readonly string NotImplementedStatusDescription = "Not implemented";

		// Token: 0x040002E3 RID: 739
		public static readonly string OMAPath = "/oma";

		// Token: 0x040002E4 RID: 740
		public static readonly string RequestIdKeyForIISLogs = "&cafeReqId=";

		// Token: 0x040002E5 RID: 741
		public static readonly string CorrelationIdKeyForIISLogs = "&CorrelationID=";

		// Token: 0x040002E6 RID: 742
		public static readonly string ISO8601DateTimeMsPattern = "yyyy-MM-ddTHH:mm:ss.fff";

		// Token: 0x040002E7 RID: 743
		public static readonly string HealthCheckPage = "HealthCheck.htm";

		// Token: 0x040002E8 RID: 744
		public static readonly string HealthCheckPageResponse = "200 OK";

		// Token: 0x040002E9 RID: 745
		public static readonly string OutlookDomain = "outlook.com";

		// Token: 0x040002EA RID: 746
		public static readonly string Office365Domain = "outlook.office365.com";

		// Token: 0x040002EB RID: 747
		public static readonly string ServerKerberosAuthenticationFailureErrorCode = 2016.ToString();

		// Token: 0x040002EC RID: 748
		public static readonly string InvalidOAuthTokenErrorCode = 4004.ToString();

		// Token: 0x040002ED RID: 749
		public static readonly string ClientDisconnectErrorCode = 4003.ToString();

		// Token: 0x040002EE RID: 750
		public static readonly string InternalServerErrorStatusCode = HttpStatusCode.InternalServerError.ToString();
	}
}
