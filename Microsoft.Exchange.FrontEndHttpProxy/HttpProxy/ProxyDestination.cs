using System;
using System.Linq;
using System.Text;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C3 RID: 195
	internal class ProxyDestination
	{
		// Token: 0x06000761 RID: 1889 RVA: 0x0002B470 File Offset: 0x00029670
		internal ProxyDestination(int version, int portToUse, string[] allDestinations, string[] destinationsInService)
		{
			if (allDestinations == null)
			{
				throw new ArgumentNullException("allDestinations can't be null!");
			}
			if (destinationsInService == null)
			{
				throw new ArgumentNullException("destinationsInServices can't be null!");
			}
			if (allDestinations.Length == 0)
			{
				throw new ArgumentException("allDestinations must have at least one server!");
			}
			this.version = version;
			this.port = portToUse;
			this.serverFqdnList = allDestinations;
			this.inServiceServerFqdnList = destinationsInService;
			this.isFixedDestination = false;
		}

		// Token: 0x06000762 RID: 1890 RVA: 0x0002B4D3 File Offset: 0x000296D3
		internal ProxyDestination(int version, int portToUse, string fqdn)
		{
			this.version = version;
			this.port = portToUse;
			this.serverFqdnList = new string[]
			{
				fqdn
			};
			this.inServiceServerFqdnList = null;
			this.isFixedDestination = true;
		}

		// Token: 0x17000183 RID: 387
		// (get) Token: 0x06000763 RID: 1891 RVA: 0x0002B507 File Offset: 0x00029707
		internal int Port
		{
			get
			{
				return this.port;
			}
		}

		// Token: 0x17000184 RID: 388
		// (get) Token: 0x06000764 RID: 1892 RVA: 0x0002B50F File Offset: 0x0002970F
		internal int Version
		{
			get
			{
				return this.version;
			}
		}

		// Token: 0x17000185 RID: 389
		// (get) Token: 0x06000765 RID: 1893 RVA: 0x0002B517 File Offset: 0x00029717
		internal bool IsDynamicTarget
		{
			get
			{
				return !this.isFixedDestination && this.version < Server.E15MinVersion && this.version >= Server.E2007MinVersion;
			}
		}

		// Token: 0x06000766 RID: 1894 RVA: 0x0002B540 File Offset: 0x00029740
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("Is fixed = {0}; Port = {1}; Servers = (", this.isFixedDestination, this.Port);
			for (int i = 0; i < this.serverFqdnList.Length - 1; i++)
			{
				stringBuilder.AppendFormat("{0},", this.serverFqdnList[i]);
			}
			if (this.serverFqdnList.Length != 0)
			{
				stringBuilder.AppendFormat("{0}); Servers in service = (", this.serverFqdnList[this.serverFqdnList.Length - 1]);
			}
			else
			{
				stringBuilder.Append("); Servers in service = (");
			}
			if (this.inServiceServerFqdnList == null || this.inServiceServerFqdnList.Length == 0)
			{
				stringBuilder.Append(")");
			}
			else if (this.inServiceServerFqdnList != null)
			{
				for (int j = 0; j < this.inServiceServerFqdnList.Length - 1; j++)
				{
					stringBuilder.AppendFormat("{0},", this.inServiceServerFqdnList[j]);
				}
				stringBuilder.AppendFormat("{0})", this.inServiceServerFqdnList[this.inServiceServerFqdnList.Length - 1]);
			}
			else
			{
				stringBuilder.Append(")");
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000767 RID: 1895 RVA: 0x0002B654 File Offset: 0x00029854
		internal string GetHostName(int key)
		{
			if (this.isFixedDestination)
			{
				return this.serverFqdnList[0];
			}
			string text = null;
			checked
			{
				if (this.serverFqdnList.Length != 0)
				{
					text = this.serverFqdnList[(int)((IntPtr)(unchecked((ulong)key % (ulong)((long)this.serverFqdnList.Length))))];
					if (!this.inServiceServerFqdnList.Contains(text))
					{
						text = null;
						if (this.inServiceServerFqdnList.Length != 0)
						{
							text = this.inServiceServerFqdnList[(int)((IntPtr)(unchecked((ulong)key % (ulong)((long)this.inServiceServerFqdnList.Length))))];
						}
					}
				}
				return text;
			}
		}

		// Token: 0x04000408 RID: 1032
		private readonly bool isFixedDestination;

		// Token: 0x04000409 RID: 1033
		private readonly int port;

		// Token: 0x0400040A RID: 1034
		private readonly int version;

		// Token: 0x0400040B RID: 1035
		private string[] serverFqdnList;

		// Token: 0x0400040C RID: 1036
		private string[] inServiceServerFqdnList;
	}
}
