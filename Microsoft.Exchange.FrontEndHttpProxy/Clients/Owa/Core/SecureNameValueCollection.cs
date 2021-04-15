using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using Microsoft.Exchange.Diagnostics;

namespace Microsoft.Exchange.Clients.Owa.Core
{
	// Token: 0x0200000E RID: 14
	public class SecureNameValueCollection : DisposeTrackableBase, IEnumerable<string>, IEnumerable
	{
		// Token: 0x06000076 RID: 118 RVA: 0x000042EC File Offset: 0x000024EC
		public SecureNameValueCollection()
		{
			this.names = new List<string>();
			this.unsecuredValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			this.secureValues = new Dictionary<string, SecureString>(StringComparer.OrdinalIgnoreCase);
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00004320 File Offset: 0x00002520
		public void AddUnsecureNameValue(string name, string value)
		{
			if (name == null || value == null)
			{
				throw new ArgumentException("name or value is null");
			}
			if (this.secureValues.ContainsKey(name))
			{
				throw new InvalidOperationException("Name was already added as secure pair. Name value:" + name);
			}
			if (this.unsecuredValues.ContainsKey(name))
			{
				throw new ArgumentException("Name is already in the collection. Name:" + name);
			}
			this.names.Add(name);
			this.unsecuredValues.Add(name, value);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00004398 File Offset: 0x00002598
		public void AddSecureNameValue(string name, SecureString value)
		{
			if (name == null || value == null)
			{
				throw new ArgumentException("name or value is null");
			}
			if (this.unsecuredValues.ContainsKey(name))
			{
				throw new InvalidOperationException("Name was already added unsecure pair. Name value:" + name);
			}
			if (this.secureValues.ContainsKey(name))
			{
				throw new ArgumentException("Name is already in the collection. Name:" + name);
			}
			this.names.Add(name);
			this.secureValues.Add(name, value);
		}

		// Token: 0x06000079 RID: 121 RVA: 0x0000440D File Offset: 0x0000260D
		public IEnumerator<string> GetEnumerator()
		{
			return this.names.GetEnumerator();
		}

		// Token: 0x0600007A RID: 122 RVA: 0x0000441F File Offset: 0x0000261F
		public bool TryGetSecureValue(string name, out SecureString value)
		{
			value = null;
			return this.secureValues.TryGetValue(name, out value);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00004436 File Offset: 0x00002636
		public bool TryGetUnsecureValue(string name, out string value)
		{
			value = null;
			return this.unsecuredValues.TryGetValue(name, out value);
		}

		// Token: 0x0600007C RID: 124 RVA: 0x0000444D File Offset: 0x0000264D
		public bool ContainsUnsecureValue(string name)
		{
			return this.unsecuredValues.ContainsKey(name);
		}

		// Token: 0x0600007D RID: 125 RVA: 0x0000445B File Offset: 0x0000265B
		public bool ContainsSecureValue(string name)
		{
			return this.secureValues.ContainsKey(name);
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00004469 File Offset: 0x00002669
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00004474 File Offset: 0x00002674
		protected override void InternalDispose(bool isDisposing)
		{
			if (isDisposing && !this.isDisposed)
			{
				foreach (SecureString secureString in this.secureValues.Values)
				{
					secureString.Dispose();
				}
				this.unsecuredValues = null;
				this.secureValues = null;
				this.isDisposed = true;
			}
		}

		// Token: 0x06000080 RID: 128 RVA: 0x000044EC File Offset: 0x000026EC
		protected override DisposeTracker InternalGetDisposeTracker()
		{
			return DisposeTracker.Get<SecureNameValueCollection>(this);
		}

		// Token: 0x0400007D RID: 125
		private List<string> names;

		// Token: 0x0400007E RID: 126
		private Dictionary<string, string> unsecuredValues;

		// Token: 0x0400007F RID: 127
		private Dictionary<string, SecureString> secureValues;

		// Token: 0x04000080 RID: 128
		private bool isDisposed;
	}
}
