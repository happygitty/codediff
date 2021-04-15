using System;
using System.Linq;
using System.Text;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000C4 RID: 196
	internal class ProxyDestination
	{
		// Token: 0x06000766 RID: 1894 RVA: 0x0002B288 File Offset: 0x00029488
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

		// Token: 0x06000767 RID: 1895 RVA: 0x0002B2EB File Offset: 0x000294EB
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

		// Token: 0x17000185 RID: 389
		// (get) Token: 0x06000768 RID: 1896 RVA: 0x0002B31F File Offset: 0x0002951F
		internal int Port
		{
			get
			{
				return this.port;
			}
		}

		// Token: 0x17000186 RID: 390
		// (get) Token: 0x06000769 RID: 1897 RVA: 0x0002B327 File Offset: 0x00029527
		internal int Version
		{
			get
			{
				return this.version;
			}
		}

		// Token: 0x17000187 RID: 391
		// (get) Token: 0x0600076A RID: 1898 RVA: 0x0002B32F File Offset: 0x0002952F
		internal bool IsDynamicTarget
		{
			get
			{
				return !this.isFixedDestination && this.version < Server.E15MinVersion && this.version >= Server.E2007MinVersion;
			}
		}

		// Token: 0x0600076B RID: 1899 RVA: 0x0002B358 File Offset: 0x00029558
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

		// Token: 0x0600076C RID: 1900 RVA: 0x0002B46C File Offset: 0x0002966C
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

		// Token: 0x04000404 RID: 1028
		private readonly bool isFixedDestination;

		// Token: 0x04000405 RID: 1029
		private readonly int port;

		// Token: 0x04000406 RID: 1030
		private readonly int version;

		// Token: 0x04000407 RID: 1031
		private string[] serverFqdnList;

		// Token: 0x04000408 RID: 1032
		private string[] inServiceServerFqdnList;
	}
}
