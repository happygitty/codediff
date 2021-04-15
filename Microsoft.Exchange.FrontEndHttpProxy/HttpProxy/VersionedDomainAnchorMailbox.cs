using System;
using System.Text.RegularExpressions;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000032 RID: 50
	internal class VersionedDomainAnchorMailbox : DomainAnchorMailbox
	{
		// Token: 0x0600019A RID: 410 RVA: 0x000085F0 File Offset: 0x000067F0
		public VersionedDomainAnchorMailbox(string domain, int version, IRequestContext requestContext) : base(AnchorSource.DomainAndVersion, domain + "~" + version.ToString(), requestContext)
		{
			this.domain = domain;
			this.Version = version;
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x0600019B RID: 411 RVA: 0x0000861A File Offset: 0x0000681A
		public override string Domain
		{
			get
			{
				return this.domain;
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x0600019C RID: 412 RVA: 0x00008622 File Offset: 0x00006822
		// (set) Token: 0x0600019D RID: 413 RVA: 0x0000862A File Offset: 0x0000682A
		public int Version { get; private set; }

		// Token: 0x0600019E RID: 414 RVA: 0x00008634 File Offset: 0x00006834
		public static AnchorMailbox GetAnchorMailbox(string domain, string versionString, IRequestContext requestContext)
		{
			ServerVersion serverVersion = VersionedDomainAnchorMailbox.ParseServerVersion(versionString);
			if (serverVersion == null)
			{
				return new DomainAnchorMailbox(domain, requestContext);
			}
			return new VersionedDomainAnchorMailbox(domain, serverVersion.Major, requestContext);
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00008668 File Offset: 0x00006868
		protected override ADRawEntry LoadADRawEntry()
		{
			if (this.Version >= 15)
			{
				return base.LoadADRawEntry();
			}
			IRecipientSession session = base.GetDomainRecipientSession();
			ADRawEntry ret = DirectoryHelper.InvokeAccountForest<ADUser>(base.RequestContext.LatencyTracker, () => HttpProxyBackEndHelper.GetE14EDiscoveryMailbox(session), base.RequestContext.Logger, session);
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(ret);
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x000086D0 File Offset: 0x000068D0
		private static ServerVersion ParseServerVersion(string versionString)
		{
			ServerVersion result = null;
			if (!string.IsNullOrEmpty(versionString))
			{
				Match match = Constants.ExchClientVerRegex.Match(versionString);
				ServerVersion serverVersion;
				if (match.Success && RegexUtilities.TryGetServerVersionFromRegexMatch(match, ref serverVersion) && serverVersion.Major >= 14)
				{
					result = serverVersion;
				}
			}
			return result;
		}

		// Token: 0x04000108 RID: 264
		private readonly string domain;
	}
}
