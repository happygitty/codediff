using System;
using System.Net;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.HttpProxy.Routing;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200001C RID: 28
	internal class CidAnchorMailbox : UserBasedAnchorMailbox
	{
		// Token: 0x060000F3 RID: 243 RVA: 0x00006154 File Offset: 0x00004354
		public CidAnchorMailbox(string cid, IRequestContext requestContext) : base(AnchorSource.Cid, cid, requestContext)
		{
			base.NotFoundExceptionCreator = delegate()
			{
				string message = string.Format("Cannot find user with cid {0}.", cid);
				return new HttpProxyException(HttpStatusCode.NotFound, 3002, message);
			};
		}

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000F4 RID: 244 RVA: 0x0000618F File Offset: 0x0000438F
		public string Cid
		{
			get
			{
				return (string)base.SourceObject;
			}
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000619C File Offset: 0x0000439C
		protected override IRoutingKey GetRoutingKey()
		{
			return new CidRoutingKey(new CID(this.Cid));
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x000061B0 File Offset: 0x000043B0
		protected override ADRawEntry LoadADRawEntry()
		{
			ADRawEntry ret = null;
			CID cid = null;
			if (CID.TryParse(this.Cid, ref cid))
			{
				IRecipientSession recipientSession = DirectoryExtensions.CreateRecipientSession(ADSessionSettings.FromConsumerOrganization(), null);
				IAggregateSession aggregateSession = recipientSession.GetAggregateSession("d:\\dbs\\sh\\e16df\\0212_214120_0\\cmd\\1g\\sources\\Dev\\Cafe\\src\\HttpProxy\\AnchorMailbox\\CidAnchorMailbox.cs", 73, "LoadADRawEntry");
				aggregateSession.MbxReadMode = 0;
				ret = DirectoryHelper.InvokeAccountForest<ADRawEntry>(base.RequestContext.LatencyTracker, () => aggregateSession.FindADRawEntryByCid(cid, this.PropertySet), base.RequestContext.Logger, recipientSession);
			}
			return base.CheckForNullAndThrowIfApplicable<ADRawEntry>(ret);
		}
	}
}
