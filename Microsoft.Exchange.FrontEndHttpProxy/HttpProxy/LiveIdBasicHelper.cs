using System;
using System.Security.Principal;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.Security.Authorization;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200007E RID: 126
	internal static class LiveIdBasicHelper
	{
		// Token: 0x06000436 RID: 1078 RVA: 0x00018368 File Offset: 0x00016568
		public static IIdentity GetCallerIdentity(IRequestContext requestContext)
		{
			ADRawEntry callerAdEntry = LiveIdBasicHelper.GetCallerAdEntry(requestContext);
			SecurityIdentifier securityIdentifier = callerAdEntry[ADMailboxRecipientSchema.Sid] as SecurityIdentifier;
			OrganizationId organizationId = (OrganizationId)callerAdEntry[ADObjectSchema.OrganizationId];
			return new GenericSidIdentity(securityIdentifier.ToString(), "LiveIdBasic", securityIdentifier, organizationId.PartitionId.ToString());
		}

		// Token: 0x06000437 RID: 1079 RVA: 0x000183B8 File Offset: 0x000165B8
		private static ADRawEntry GetCallerAdEntry(IRequestContext requestContext)
		{
			if (!requestContext.HttpContext.Items.Contains(Constants.CallerADRawEntryKeyName))
			{
				CommonAccessToken commonAccessToken = requestContext.HttpContext.Items["Item-CommonAccessToken"] as CommonAccessToken;
				if (commonAccessToken == null)
				{
					throw new InvalidOperationException("CAT token not present - cannot lookup LiveIdBasic user's AD entry.");
				}
				ADRawEntry value = null;
				LatencyTracker latencyTracker = (LatencyTracker)requestContext.HttpContext.Items[Constants.LatencyTrackerContextKeyName];
				LiveIdBasicTokenAccessor accessor = LiveIdBasicTokenAccessor.Attach(commonAccessToken);
				if (accessor.TokenType == 2)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<string, string>(0L, "[Extensions::GetFullCallerIdentity] Calling AD to convert PUID {0} for LiveIdMemberName {1} to SID to construct GenericSidIdentity.", accessor.Puid, accessor.LiveIdMemberName);
					}
					ITenantRecipientSession session = DirectoryHelper.GetTenantRecipientSessionFromSmtpOrLiveId(accessor.LiveIdMemberName, requestContext.Logger, latencyTracker, false);
					value = DirectoryHelper.InvokeAccountForest<ADRawEntry>(latencyTracker, () => session.FindUniqueEntryByNetID(accessor.Puid, null, UserBasedAnchorMailbox.ADRawEntryPropertySet, "d:\\dbs\\sh\\e16dt\\0404_133553_0\\cmd\\j\\sources\\Dev\\Cafe\\src\\HttpProxy\\Misc\\LiveIdBasicHelper.cs", 92, "GetCallerAdEntry"), requestContext.Logger, session);
				}
				requestContext.HttpContext.Items[Constants.CallerADRawEntryKeyName] = value;
			}
			return (ADRawEntry)requestContext.HttpContext.Items[Constants.CallerADRawEntryKeyName];
		}
	}
}
