using System;
using System.Web;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.HttpProxy.Routing;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x02000018 RID: 24
	internal abstract class AnchorMailbox
	{
		// Token: 0x060000BB RID: 187 RVA: 0x00004F08 File Offset: 0x00003108
		protected AnchorMailbox(AnchorSource anchorSource, object sourceObject, IRequestContext requestContext)
		{
			if (sourceObject == null)
			{
				throw new ArgumentNullException("sourceObject");
			}
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			this.AnchorSource = anchorSource;
			this.SourceObject = sourceObject;
			this.RequestContext = requestContext;
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, AnchorSource, object>((long)this.GetHashCode(), "[AnchorMailbox::ctor]: context {0}; AnchorSource {1}; SourceObject {2}", this.RequestContext.TraceContext, anchorSource, sourceObject);
			}
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060000BC RID: 188 RVA: 0x00004F7C File Offset: 0x0000317C
		// (set) Token: 0x060000BD RID: 189 RVA: 0x00004F84 File Offset: 0x00003184
		public AnchorSource AnchorSource { get; private set; }

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000BE RID: 190 RVA: 0x00004F8D File Offset: 0x0000318D
		// (set) Token: 0x060000BF RID: 191 RVA: 0x00004F95 File Offset: 0x00003195
		public object SourceObject { get; private set; }

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000C0 RID: 192 RVA: 0x00004F9E File Offset: 0x0000319E
		// (set) Token: 0x060000C1 RID: 193 RVA: 0x00004FA6 File Offset: 0x000031A6
		public IRequestContext RequestContext { get; private set; }

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000C2 RID: 194 RVA: 0x00004FAF File Offset: 0x000031AF
		// (set) Token: 0x060000C3 RID: 195 RVA: 0x00004FB7 File Offset: 0x000031B7
		public Func<Exception> NotFoundExceptionCreator { get; set; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000C4 RID: 196 RVA: 0x00004FC0 File Offset: 0x000031C0
		// (set) Token: 0x060000C5 RID: 197 RVA: 0x00004FC8 File Offset: 0x000031C8
		public AnchorMailbox OriginalAnchorMailbox { get; set; }

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000C6 RID: 198 RVA: 0x00004FD1 File Offset: 0x000031D1
		// (set) Token: 0x060000C7 RID: 199 RVA: 0x00004FD9 File Offset: 0x000031D9
		public bool CacheEntryCacheHit { get; private set; }

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000C8 RID: 200 RVA: 0x00004FE2 File Offset: 0x000031E2
		// (set) Token: 0x060000C9 RID: 201 RVA: 0x00004FEA File Offset: 0x000031EA
		private protected BackEndCookieEntryBase IncomingCookieEntry { protected get; private set; }

		// Token: 0x060000CA RID: 202 RVA: 0x00004FF3 File Offset: 0x000031F3
		public virtual string GetOrganizationNameForLogging()
		{
			if (this.loadedCachedEntry != null)
			{
				return this.loadedCachedEntry.DomainName;
			}
			return null;
		}

		// Token: 0x060000CB RID: 203 RVA: 0x0000500A File Offset: 0x0000320A
		public virtual BackEndServer TryDirectBackEndCalculation()
		{
			return null;
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00005010 File Offset: 0x00003210
		public virtual BackEndServer AcceptBackEndCookie(HttpCookie backEndCookie)
		{
			if (backEndCookie == null)
			{
				throw new ArgumentNullException("backEndCookie");
			}
			string name = this.ToCookieKey();
			string text = backEndCookie.Values[name];
			BackEndServer backEndServer = null;
			if (!string.IsNullOrEmpty(text))
			{
				try
				{
					BackEndCookieEntryBase backEndCookieEntryBase;
					string text2;
					if (!BackEndCookieEntryParser.TryParse(text, out backEndCookieEntryBase, out text2))
					{
						throw new InvalidBackEndCookieException();
					}
					if (backEndCookieEntryBase.Expired)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<BackEndCookieEntryBase>((long)this.GetHashCode(), "[AnchorMailbox::ProcessBackEndCookie]: Back end cookie entry {0} has expired.", backEndCookieEntryBase);
						}
						this.RequestContext.Logger.SafeSet(4, string.Format("Expired~{0}", text2));
						throw new InvalidBackEndCookieException();
					}
					this.RequestContext.Logger.SafeSet(4, text2);
					this.IncomingCookieEntry = backEndCookieEntryBase;
					this.CacheEntryCacheHit = true;
					PerfCounters.HttpProxyCacheCountersInstance.CookieUseRate.Increment();
					PerfCounters.UpdateMovingPercentagePerformanceCounter(PerfCounters.HttpProxyCacheCountersInstance.MovingPercentageCookieUseRate);
					backEndServer = this.TryGetBackEndFromCookie(this.IncomingCookieEntry);
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<BackEndServer, BackEndCookieEntryBase>((long)this.GetHashCode(), "[AnchorMailbox::ProcessBackEndCookie]: Back end server {0} resolved from cookie {1}.", backEndServer, this.IncomingCookieEntry);
					}
				}
				catch (InvalidBackEndCookieException)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[AnchorMailbox::ProcessBackEndCookie]: Invalid back end cookie entry.");
					}
					backEndCookie.Values.Remove(name);
				}
			}
			return backEndServer;
		}

		// Token: 0x060000CD RID: 205 RVA: 0x00005174 File Offset: 0x00003374
		public virtual BackEndCookieEntryBase BuildCookieEntryForTarget(BackEndServer routingTarget, bool proxyToDownLevel, bool useResourceForest, bool organizationAware)
		{
			if (routingTarget == null)
			{
				throw new ArgumentNullException("routingTarget");
			}
			return new BackEndServerCookieEntry(routingTarget.Fqdn, routingTarget.Version);
		}

		// Token: 0x060000CE RID: 206 RVA: 0x00005195 File Offset: 0x00003395
		public virtual string ToCookieKey()
		{
			if (this.OriginalAnchorMailbox != null)
			{
				return this.OriginalAnchorMailbox.ToCookieKey();
			}
			return this.SourceObject.ToString().Replace(" ", "_").Replace("=", "+");
		}

		// Token: 0x060000CF RID: 207 RVA: 0x0000500A File Offset: 0x0000320A
		public virtual IRoutingEntry GetRoutingEntry()
		{
			return null;
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x000051D4 File Offset: 0x000033D4
		public virtual ITenantContext GetTenantContext()
		{
			return new ExternalDirectoryOrganizationIdTenantContext(Guid.Empty);
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x000051E0 File Offset: 0x000033E0
		public override string ToString()
		{
			return string.Format("{0}~{1}", this.AnchorSource, this.SourceObject);
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00005200 File Offset: 0x00003400
		public void UpdateCache(AnchorMailboxCacheEntry cacheEntry)
		{
			string text = this.ToCacheKey();
			if (text != null && cacheEntry != null)
			{
				if (cacheEntry.Database != null)
				{
					AnchorMailboxCache.Instance.Add(text, cacheEntry, DateTime.UtcNow, this.RequestContext);
				}
				if (HttpProxySettings.NegativeAnchorMailboxCacheEnabled.Value)
				{
					NegativeAnchorMailboxCache.Instance.Remove(text);
				}
			}
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x00005250 File Offset: 0x00003450
		public void InvalidateCache()
		{
			string key = this.ToCacheKey();
			AnchorMailboxCache.Instance.Remove(key, this.RequestContext);
			this.loadedCachedEntry = null;
			if (HttpProxySettings.NegativeAnchorMailboxCacheEnabled.Value)
			{
				NegativeAnchorMailboxCache.Instance.Remove(key);
			}
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00005294 File Offset: 0x00003494
		public void UpdateNegativeCache(NegativeAnchorMailboxCacheEntry cacheEntry)
		{
			if (!HttpProxySettings.NegativeAnchorMailboxCacheEnabled.Value)
			{
				return;
			}
			string key = this.ToCacheKey();
			NegativeAnchorMailboxCache.Instance.Add(key, cacheEntry);
			AnchorMailboxCache.Instance.Remove(key, this.RequestContext);
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x000052D4 File Offset: 0x000034D4
		public NegativeAnchorMailboxCacheEntry GetNegativeCacheEntry()
		{
			if (!HttpProxySettings.NegativeAnchorMailboxCacheEnabled.Value)
			{
				return null;
			}
			string key = this.ToCacheKey();
			NegativeAnchorMailboxCacheEntry result;
			if (NegativeAnchorMailboxCache.Instance.TryGet(key, out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x00005308 File Offset: 0x00003508
		protected virtual BackEndServer TryGetBackEndFromCookie(BackEndCookieEntryBase cookieEntry)
		{
			BackEndServerCookieEntry backEndServerCookieEntry = cookieEntry as BackEndServerCookieEntry;
			if (backEndServerCookieEntry != null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[AnchorMailbox::TryGetBackEndFromCookie]: BackEndServerCookier {0}", backEndServerCookieEntry.ToString());
				}
				return new BackEndServer(backEndServerCookieEntry.Fqdn, backEndServerCookieEntry.Version);
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[AnchorMailbox::TryGetBackEndFromCookie]: No BackEndServerCookie");
			}
			return null;
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x00005380 File Offset: 0x00003580
		protected AnchorMailboxCacheEntry GetCacheEntry()
		{
			if (this.loadedCachedEntry == null)
			{
				this.loadedCachedEntry = this.LoadCacheEntryFromIncomingCookie();
				string key = this.ToCacheKey();
				if (this.loadedCachedEntry == null)
				{
					if (AnchorMailboxCache.Instance.TryGet(key, this.RequestContext, out this.loadedCachedEntry) && this.loadedCachedEntry != null && this.loadedCachedEntry.Database != null)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailboxCacheEntry, AnchorMailbox>((long)this.GetHashCode(), "[AnchorMailbox::GetCacheEntry]: Using cached entry {0} for anchor mailbox {1}.", this.loadedCachedEntry, this);
						}
						this.CacheEntryCacheHit = true;
					}
					else
					{
						this.loadedCachedEntry = this.RefreshCacheEntry();
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailboxCacheEntry, AnchorMailbox>((long)this.GetHashCode(), "[AnchorMailbox::GetCacheEntry]: RefreshCacheEntry() returns {0} for anchor mailbox {1}.", this.loadedCachedEntry, this);
						}
						if (this.ShouldAddEntryToAnchorMailboxCache(this.loadedCachedEntry))
						{
							this.UpdateCache(this.loadedCachedEntry);
						}
						else if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailboxCacheEntry, AnchorMailbox>((long)this.GetHashCode(), "[AnchorMailbox::GetCacheEntry]: Will not add cache entry {0} for anchor mailbox {1}.", this.loadedCachedEntry, this);
						}
					}
				}
				else
				{
					this.CacheEntryCacheHit = true;
				}
			}
			return this.loadedCachedEntry;
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x00003193 File Offset: 0x00001393
		protected virtual bool ShouldAddEntryToAnchorMailboxCache(AnchorMailboxCacheEntry cacheEntry)
		{
			return true;
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000500A File Offset: 0x0000320A
		protected virtual AnchorMailboxCacheEntry LoadCacheEntryFromIncomingCookie()
		{
			return null;
		}

		// Token: 0x060000DA RID: 218 RVA: 0x0000500A File Offset: 0x0000320A
		protected virtual AnchorMailboxCacheEntry RefreshCacheEntry()
		{
			return null;
		}

		// Token: 0x060000DB RID: 219 RVA: 0x000054A5 File Offset: 0x000036A5
		protected virtual string ToCacheKey()
		{
			return this.ToString().Replace(" ", "_");
		}

		// Token: 0x060000DC RID: 220 RVA: 0x000054BC File Offset: 0x000036BC
		protected T CheckForNullAndThrowIfApplicable<T>(T ret)
		{
			if (ret == null && this.NotFoundExceptionCreator != null)
			{
				throw this.NotFoundExceptionCreator();
			}
			return ret;
		}

		// Token: 0x060000DD RID: 221 RVA: 0x000054DB File Offset: 0x000036DB
		protected Guid GetExternalDirectoryOrganizationGuidFromADRawEntry(ADRawEntry activeDirectoryRawEntry)
		{
			if (activeDirectoryRawEntry != null && activeDirectoryRawEntry[ADObjectSchema.OrganizationId] != null)
			{
				return ((OrganizationId)activeDirectoryRawEntry[ADObjectSchema.OrganizationId]).SafeToExternalDirectoryOrganizationIdGuid();
			}
			return Guid.Empty;
		}

		// Token: 0x040000CE RID: 206
		public static readonly BoolAppSettingsEntry AllowMissingTenant = new BoolAppSettingsEntry("AnchorMailbox.AllowMissingTenant", false, ExTraceGlobals.VerboseTracer);

		// Token: 0x040000CF RID: 207
		private AnchorMailboxCacheEntry loadedCachedEntry;
	}
}
