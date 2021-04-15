using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.Exchange.Security.Authorization;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200006C RID: 108
	internal class SerializedClientSecurityContext : ISecurityAccessToken
	{
		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x06000381 RID: 897 RVA: 0x00014059 File Offset: 0x00012259
		// (set) Token: 0x06000382 RID: 898 RVA: 0x00014061 File Offset: 0x00012261
		public string UserSid
		{
			get
			{
				return this.userSid;
			}
			set
			{
				this.userSid = value;
			}
		}

		// Token: 0x170000D3 RID: 211
		// (get) Token: 0x06000383 RID: 899 RVA: 0x0001406A File Offset: 0x0001226A
		// (set) Token: 0x06000384 RID: 900 RVA: 0x00014072 File Offset: 0x00012272
		public SidStringAndAttributes[] GroupSids
		{
			get
			{
				return this.groupSids;
			}
			set
			{
				this.groupSids = value;
			}
		}

		// Token: 0x170000D4 RID: 212
		// (get) Token: 0x06000385 RID: 901 RVA: 0x0001407B File Offset: 0x0001227B
		// (set) Token: 0x06000386 RID: 902 RVA: 0x00014083 File Offset: 0x00012283
		public SidStringAndAttributes[] RestrictedGroupSids
		{
			get
			{
				return this.restrictedGroupSids;
			}
			set
			{
				this.restrictedGroupSids = value;
			}
		}

		// Token: 0x170000D5 RID: 213
		// (get) Token: 0x06000387 RID: 903 RVA: 0x0001408C File Offset: 0x0001228C
		// (set) Token: 0x06000388 RID: 904 RVA: 0x00014094 File Offset: 0x00012294
		internal string AuthenticationType
		{
			get
			{
				return this.authenticationType;
			}
			set
			{
				this.authenticationType = value;
			}
		}

		// Token: 0x170000D6 RID: 214
		// (get) Token: 0x06000389 RID: 905 RVA: 0x0001409D File Offset: 0x0001229D
		// (set) Token: 0x0600038A RID: 906 RVA: 0x000140A5 File Offset: 0x000122A5
		internal string LogonName
		{
			get
			{
				return this.logonName;
			}
			set
			{
				this.logonName = value;
			}
		}

		// Token: 0x0600038B RID: 907 RVA: 0x000140B0 File Offset: 0x000122B0
		public static SerializedClientSecurityContext CreateFromClientSecurityContext(ClientSecurityContext clientSecurityContext, string logonName, string authenticationType)
		{
			SerializedClientSecurityContext serializedClientSecurityContext = new SerializedClientSecurityContext();
			clientSecurityContext.SetSecurityAccessToken(serializedClientSecurityContext);
			serializedClientSecurityContext.LogonName = logonName;
			serializedClientSecurityContext.AuthenticationType = authenticationType;
			return serializedClientSecurityContext;
		}

		// Token: 0x0600038C RID: 908 RVA: 0x000140DC File Offset: 0x000122DC
		public void Serialize(XmlTextWriter writer)
		{
			writer.WriteStartElement(SerializedClientSecurityContext.RootElementName);
			writer.WriteAttributeString(SerializedClientSecurityContext.AuthenticationTypeAttributeName, this.authenticationType);
			writer.WriteAttributeString(SerializedClientSecurityContext.LogonNameAttributeName, this.logonName);
			SerializedClientSecurityContext.WriteSid(writer, this.UserSid, 0U, SerializedClientSecurityContext.SidType.User);
			if (this.GroupSids != null)
			{
				for (int i = 0; i < this.GroupSids.Length; i++)
				{
					SerializedClientSecurityContext.WriteSid(writer, this.GroupSids[i].SecurityIdentifier, this.GroupSids[i].Attributes, SerializedClientSecurityContext.SidType.Group);
				}
			}
			if (this.RestrictedGroupSids != null)
			{
				for (int j = 0; j < this.RestrictedGroupSids.Length; j++)
				{
					SerializedClientSecurityContext.WriteSid(writer, this.RestrictedGroupSids[j].SecurityIdentifier, this.RestrictedGroupSids[j].Attributes, SerializedClientSecurityContext.SidType.RestrictedGroup);
				}
			}
			writer.WriteEndElement();
		}

		// Token: 0x0600038D RID: 909 RVA: 0x000141A4 File Offset: 0x000123A4
		internal static SerializedClientSecurityContext Deserialize(Stream input)
		{
			XmlTextReader xmlTextReader = null;
			SerializedClientSecurityContext result;
			try
			{
				xmlTextReader = new XmlTextReader(input);
				xmlTextReader.WhitespaceHandling = WhitespaceHandling.All;
				result = SerializedClientSecurityContext.Deserialize(xmlTextReader);
			}
			finally
			{
				if (xmlTextReader != null)
				{
					xmlTextReader.Dispose();
				}
			}
			return result;
		}

		// Token: 0x0600038E RID: 910 RVA: 0x000141E8 File Offset: 0x000123E8
		internal static SerializedClientSecurityContext Deserialize(XmlTextReader reader)
		{
			SerializedClientSecurityContext serializedClientSecurityContext = new SerializedClientSecurityContext();
			serializedClientSecurityContext.UserSid = null;
			serializedClientSecurityContext.GroupSids = null;
			serializedClientSecurityContext.RestrictedGroupSids = null;
			try
			{
				List<SidStringAndAttributes> list = new List<SidStringAndAttributes>();
				List<SidStringAndAttributes> list2 = new List<SidStringAndAttributes>();
				if (!reader.Read() || XmlNodeType.Element != reader.NodeType || StringComparer.OrdinalIgnoreCase.Compare(reader.Name, SerializedClientSecurityContext.RootElementName) != 0)
				{
					SerializedClientSecurityContext.ThrowParserException(reader, "Missing or invalid root node");
				}
				if (reader.MoveToFirstAttribute())
				{
					do
					{
						if (StringComparer.OrdinalIgnoreCase.Compare(reader.Name, SerializedClientSecurityContext.AuthenticationTypeAttributeName) == 0)
						{
							if (serializedClientSecurityContext.authenticationType != null)
							{
								SerializedClientSecurityContext.ThrowParserException(reader, string.Format("Duplicated attribute {0}", SerializedClientSecurityContext.AuthenticationTypeAttributeName));
							}
							serializedClientSecurityContext.authenticationType = reader.Value;
						}
						else if (StringComparer.OrdinalIgnoreCase.Compare(reader.Name, SerializedClientSecurityContext.LogonNameAttributeName) == 0)
						{
							if (serializedClientSecurityContext.logonName != null)
							{
								SerializedClientSecurityContext.ThrowParserException(reader, string.Format("Duplicated attribute {0}", SerializedClientSecurityContext.LogonNameAttributeName));
							}
							serializedClientSecurityContext.logonName = reader.Value;
						}
						else
						{
							SerializedClientSecurityContext.ThrowParserException(reader, "Found invalid attribute in root element");
						}
					}
					while (reader.MoveToNextAttribute());
				}
				if (serializedClientSecurityContext.authenticationType == null || serializedClientSecurityContext.logonName == null)
				{
					SerializedClientSecurityContext.ThrowParserException(reader, "Auth type or logon name attributes are missing");
				}
				bool flag = false;
				int num = 0;
				while (reader.Read())
				{
					if (XmlNodeType.EndElement == reader.NodeType && StringComparer.OrdinalIgnoreCase.Compare(reader.Name, SerializedClientSecurityContext.RootElementName) == 0)
					{
						flag = true;
						break;
					}
					if (XmlNodeType.Element != reader.NodeType || StringComparer.OrdinalIgnoreCase.Compare(reader.Name, SerializedClientSecurityContext.SidElementName) != 0)
					{
						SerializedClientSecurityContext.ThrowParserException(reader, "Expecting SID node");
					}
					SerializedClientSecurityContext.SidType sidType = SerializedClientSecurityContext.SidType.User;
					uint num2 = 0U;
					if (reader.MoveToFirstAttribute())
					{
						do
						{
							if (StringComparer.OrdinalIgnoreCase.Compare(reader.Name, SerializedClientSecurityContext.SidTypeAttributeName) == 0)
							{
								int num3 = int.Parse(reader.Value);
								if (num3 == 1)
								{
									sidType = SerializedClientSecurityContext.SidType.Group;
								}
								else if (num3 == 2)
								{
									sidType = SerializedClientSecurityContext.SidType.RestrictedGroup;
								}
								else
								{
									SerializedClientSecurityContext.ThrowParserException(reader, "Invalid SID type");
								}
							}
							else if (StringComparer.OrdinalIgnoreCase.Compare(reader.Name, SerializedClientSecurityContext.SidAttributesAttributeName) == 0)
							{
								num2 = uint.Parse(reader.Value);
							}
							else
							{
								SerializedClientSecurityContext.ThrowParserException(reader, "Found invalid attribute in SID element");
							}
						}
						while (reader.MoveToNextAttribute());
					}
					if (sidType == SerializedClientSecurityContext.SidType.User)
					{
						if (num2 != 0U)
						{
							SerializedClientSecurityContext.ThrowParserException(reader, "'Attributes' shouldn't be set in an user SID");
						}
						else if (serializedClientSecurityContext.UserSid != null)
						{
							SerializedClientSecurityContext.ThrowParserException(reader, "There can only be one user SID in the XML document");
						}
					}
					if (!reader.Read() || XmlNodeType.Text != reader.NodeType || string.IsNullOrEmpty(reader.Value))
					{
						SerializedClientSecurityContext.ThrowParserException(reader, "Expecting SID value in SDDL format");
					}
					string value = reader.Value;
					if (sidType == SerializedClientSecurityContext.SidType.User)
					{
						serializedClientSecurityContext.UserSid = value;
					}
					else if (sidType == SerializedClientSecurityContext.SidType.Group)
					{
						SidStringAndAttributes item = new SidStringAndAttributes(value, num2);
						list.Add(item);
					}
					else if (sidType == SerializedClientSecurityContext.SidType.RestrictedGroup)
					{
						SidStringAndAttributes item2 = new SidStringAndAttributes(value, num2);
						list2.Add(item2);
					}
					if (!reader.Read() || XmlNodeType.EndElement != reader.NodeType)
					{
						SerializedClientSecurityContext.ThrowParserException(reader, "Expected end of SID node");
					}
					num++;
					if (num > SerializedClientSecurityContext.MaximumSidsPerContext)
					{
						throw new Exception(string.Format("Too many SID nodes in the request, maximum is {0}", SerializedClientSecurityContext.MaximumSidsPerContext));
					}
				}
				if (serializedClientSecurityContext.UserSid == null)
				{
					SerializedClientSecurityContext.ThrowParserException(reader, "Serialized context should at least contain an user SID");
				}
				if (!flag)
				{
					SerializedClientSecurityContext.ThrowParserException(reader, "Parsing error");
				}
				if (list.Count > 0)
				{
					serializedClientSecurityContext.GroupSids = list.ToArray();
				}
				if (list2.Count > 0)
				{
					serializedClientSecurityContext.RestrictedGroupSids = list2.ToArray();
				}
			}
			catch (XmlException ex)
			{
				SerializedClientSecurityContext.ThrowParserException(reader, string.Format("Parser threw an XML exception: {0}", ex.Message));
			}
			return serializedClientSecurityContext;
		}

		// Token: 0x0600038F RID: 911 RVA: 0x0001456C File Offset: 0x0001276C
		internal string Serialize()
		{
			StringWriter stringWriter = new StringWriter();
			XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
			string result;
			try
			{
				this.Serialize(xmlTextWriter);
				stringWriter.Flush();
				result = stringWriter.ToString();
			}
			finally
			{
				if (xmlTextWriter != null)
				{
					xmlTextWriter.Flush();
					xmlTextWriter.Dispose();
				}
				if (stringWriter != null)
				{
					stringWriter.Dispose();
				}
			}
			return result;
		}

		// Token: 0x06000390 RID: 912 RVA: 0x000145C8 File Offset: 0x000127C8
		private static void ThrowParserException(XmlTextReader reader, string description)
		{
			throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Invalid serialized client context. Line number: {0} Position: {1}.{2}", reader.LineNumber.ToString(CultureInfo.InvariantCulture), reader.LinePosition.ToString(CultureInfo.InvariantCulture), (description != null) ? (" " + description) : string.Empty));
		}

		// Token: 0x06000391 RID: 913 RVA: 0x00014624 File Offset: 0x00012824
		private static void WriteSid(XmlTextWriter writer, string sid, uint attributes, SerializedClientSecurityContext.SidType sidType)
		{
			writer.WriteStartElement(SerializedClientSecurityContext.SidElementName);
			if (attributes != 0U)
			{
				writer.WriteAttributeString(SerializedClientSecurityContext.SidAttributesAttributeName, attributes.ToString());
			}
			if (sidType != SerializedClientSecurityContext.SidType.User)
			{
				string sidTypeAttributeName = SerializedClientSecurityContext.SidTypeAttributeName;
				int num = (int)sidType;
				writer.WriteAttributeString(sidTypeAttributeName, num.ToString());
			}
			writer.WriteString(sid);
			writer.WriteEndElement();
		}

		// Token: 0x0400024A RID: 586
		private static readonly int MaximumSidsPerContext = 3000;

		// Token: 0x0400024B RID: 587
		private static readonly string RootElementName = "r";

		// Token: 0x0400024C RID: 588
		private static readonly string AuthenticationTypeAttributeName = "at";

		// Token: 0x0400024D RID: 589
		private static readonly string LogonNameAttributeName = "ln";

		// Token: 0x0400024E RID: 590
		private static readonly string SidElementName = "s";

		// Token: 0x0400024F RID: 591
		private static readonly string SidTypeAttributeName = "t";

		// Token: 0x04000250 RID: 592
		private static readonly string SidAttributesAttributeName = "a";

		// Token: 0x04000251 RID: 593
		private string userSid;

		// Token: 0x04000252 RID: 594
		private SidStringAndAttributes[] groupSids;

		// Token: 0x04000253 RID: 595
		private SidStringAndAttributes[] restrictedGroupSids;

		// Token: 0x04000254 RID: 596
		private string authenticationType;

		// Token: 0x04000255 RID: 597
		private string logonName;

		// Token: 0x02000103 RID: 259
		private enum SidType
		{
			// Token: 0x040004D2 RID: 1234
			User,
			// Token: 0x040004D3 RID: 1235
			Group,
			// Token: 0x040004D4 RID: 1236
			RestrictedGroup
		}
	}
}
