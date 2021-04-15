using System;
using System.Net;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Data.Common;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.Security.Authentication;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000054 RID: 84
	internal abstract class DatacenterRedirectStrategy
	{
		// Token: 0x060002B0 RID: 688 RVA: 0x0000D773 File Offset: 0x0000B973
		public DatacenterRedirectStrategy(IRequestContext requestContext)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			this.RequestContext = requestContext;
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060002B1 RID: 689 RVA: 0x0000D790 File Offset: 0x0000B990
		// (set) Token: 0x060002B2 RID: 690 RVA: 0x0000D798 File Offset: 0x0000B998
		public IRequestContext RequestContext { get; private set; }

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060002B3 RID: 691 RVA: 0x0000D7A1 File Offset: 0x0000B9A1
		public int TraceContext
		{
			get
			{
				return this.RequestContext.TraceContext;
			}
		}

		// Token: 0x060002B4 RID: 692 RVA: 0x0000D7B0 File Offset: 0x0000B9B0
		public static void CheckLiveIdBasicPartialAuthResult(HttpContext httpContext)
		{
			if (httpContext == null)
			{
				throw new ArgumentNullException("httpContext");
			}
			IPrincipal user = httpContext.User;
			if (user != null && user.Identity != null && user.GetType().Equals(typeof(GenericPrincipal)) && user.Identity.GetType().Equals(typeof(GenericIdentity)) && string.Equals(user.Identity.AuthenticationType, "LiveIdBasic", StringComparison.OrdinalIgnoreCase))
			{
				throw new HttpException(403, string.Format("Unable to resolve identity: {0}", IIdentityExtensions.GetSafeName(user.Identity, true)));
			}
		}

		// Token: 0x060002B5 RID: 693 RVA: 0x0000D84C File Offset: 0x0000BA4C
		public void RedirectMailbox(AnchorMailbox anchorMailbox)
		{
			if (anchorMailbox == null)
			{
				throw new ArgumentNullException("anchorMailbox");
			}
			if (!(anchorMailbox is UserBasedAnchorMailbox))
			{
				throw new ArgumentException("The AnchorMailbox object needs to be user based.");
			}
			string userAddress = this.ResolveUserAddress(anchorMailbox);
			this.RedirectAddress(userAddress);
		}

		// Token: 0x060002B6 RID: 694 RVA: 0x0000D889 File Offset: 0x0000BA89
		protected virtual Uri GetRedirectUrl(string redirectServer)
		{
			return new UriBuilder(this.RequestContext.HttpContext.Request.Url)
			{
				Host = redirectServer
			}.Uri;
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x0000D8B4 File Offset: 0x0000BAB4
		private void RedirectAddress(string userAddress)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[DatacenterRedirectStrategy::RedirectAddress]: Context {0}. Will use address {2} to look up MSERV.", this.TraceContext, userAddress);
			}
			string text = this.InvokeMserv(userAddress);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, string, string>((long)this.GetHashCode(), "[DatacenterRedirectStrategy::RedirectAddress]: Context {0}. Will redirect user {1} to server {2}", this.TraceContext, userAddress, text);
			}
			Uri redirectUrl = this.GetRedirectUrl(text);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, string, Uri>((long)this.GetHashCode(), "[DatacenterRedirectStrategy::RedirectAddress]: Context {0}. Will redirect user {1} to URL {2}", this.TraceContext, userAddress, redirectUrl);
			}
			throw new HttpException(302, redirectUrl.AbsoluteUri);
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x0000D964 File Offset: 0x0000BB64
		private string InvokeMserv(string userAddress)
		{
			int currentSitePartnerId = HttpProxyGlobals.LocalSite.Member.PartnerId;
			string text = null;
			long num = 0L;
			Exception ex = null;
			try
			{
				text = LatencyTracker.GetLatency<string>(() => EdgeSyncMservConnector.GetRedirectServer(DatacenterRedirectStrategy.PodRedirectTemplate.Value, userAddress, currentSitePartnerId, DatacenterRedirectStrategy.PodSiteStartRange.Value, DatacenterRedirectStrategy.PodSiteEndRange.Value, false, true), out num);
			}
			catch (MServTransientException ex)
			{
			}
			catch (MServPermanentException ex)
			{
			}
			catch (InvalidOperationException ex)
			{
			}
			catch (LocalizedException ex)
			{
			}
			finally
			{
				this.RequestContext.Logger.AppendGenericInfo("MservLatency", num);
			}
			string message = string.Format("Failed to look up MSERV for address {0}.", userAddress);
			if (ex != null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<int, string, Exception>((long)this.GetHashCode(), "[DatacenterRedirectStrategy::InvokeMserv]: Context {0}. Failed to look up MSERV for address {1}. Error: {2}", this.TraceContext, userAddress, ex);
				}
				throw new HttpProxyException(HttpStatusCode.InternalServerError, 2002, message, ex);
			}
			if (string.IsNullOrEmpty(text))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<int, string>((long)this.GetHashCode(), "[DatacenterRedirectStrategy::InvokeMserv]: Context {0}. MSERV did not return redirect server for address {1}.", this.TraceContext, userAddress);
				}
				throw new HttpProxyException(HttpStatusCode.InternalServerError, 2002, message);
			}
			return text;
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x0000DABC File Offset: 0x0000BCBC
		private string ResolveUserAddress(AnchorMailbox anchorMailbox)
		{
			SidAnchorMailbox sidAnchorMailbox = anchorMailbox as SidAnchorMailbox;
			if (sidAnchorMailbox != null && !string.IsNullOrEmpty(sidAnchorMailbox.SmtpOrLiveId))
			{
				return sidAnchorMailbox.SmtpOrLiveId;
			}
			SmtpAnchorMailbox smtpAnchorMailbox = anchorMailbox as SmtpAnchorMailbox;
			if (smtpAnchorMailbox != null)
			{
				return smtpAnchorMailbox.Smtp;
			}
			UserBasedAnchorMailbox userBasedAnchorMailbox = (UserBasedAnchorMailbox)anchorMailbox;
			return string.Format("anyone@{0}", userBasedAnchorMailbox.GetDomainName());
		}

		// Token: 0x040001A0 RID: 416
		protected static readonly StringAppSettingsEntry PodRedirectTemplate = new StringAppSettingsEntry("PodRedirectTemplate", "pod{0}.outlook.com", ExTraceGlobals.VerboseTracer);

		// Token: 0x040001A1 RID: 417
		protected static readonly IntAppSettingsEntry PodSiteStartRange = new IntAppSettingsEntry("PodSiteStartRange", 5000, ExTraceGlobals.VerboseTracer);

		// Token: 0x040001A2 RID: 418
		protected static readonly IntAppSettingsEntry PodSiteEndRange = new IntAppSettingsEntry("PodSiteEndRange", 5009, ExTraceGlobals.VerboseTracer);
	}
}
