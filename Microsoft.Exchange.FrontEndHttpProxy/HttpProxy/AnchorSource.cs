using System;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000017 RID: 23
	internal enum AnchorSource
	{
		// Token: 0x040000BB RID: 187
		Smtp,
		// Token: 0x040000BC RID: 188
		Sid,
		// Token: 0x040000BD RID: 189
		Domain,
		// Token: 0x040000BE RID: 190
		DomainAndVersion,
		// Token: 0x040000BF RID: 191
		OrganizationId,
		// Token: 0x040000C0 RID: 192
		MailboxGuid,
		// Token: 0x040000C1 RID: 193
		DatabaseName,
		// Token: 0x040000C2 RID: 194
		DatabaseGuid,
		// Token: 0x040000C3 RID: 195
		UserADRawEntry,
		// Token: 0x040000C4 RID: 196
		ServerInfo,
		// Token: 0x040000C5 RID: 197
		ServerVersion,
		// Token: 0x040000C6 RID: 198
		Url,
		// Token: 0x040000C7 RID: 199
		Anonymous,
		// Token: 0x040000C8 RID: 200
		GenericAnchorHint,
		// Token: 0x040000C9 RID: 201
		Cid,
		// Token: 0x040000CA RID: 202
		Puid,
		// Token: 0x040000CB RID: 203
		ExternalDirectoryObjectId,
		// Token: 0x040000CC RID: 204
		OAuthActAsUser,
		// Token: 0x040000CD RID: 205
		LiveIdMemberName
	}
}
