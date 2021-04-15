using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Clients.Security;
using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.ApplicationLogic.Cafe;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Exchange.Data.Directory.SystemConfiguration;
using Microsoft.Exchange.Data.ServerLocator;
using Microsoft.Exchange.Data.Storage;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.HttpProxy.EventLogs;
using Microsoft.Exchange.HttpProxy.Routing;
using Microsoft.Exchange.HttpProxy.Routing.RoutingDestinations;
using Microsoft.Exchange.HttpProxy.Routing.RoutingEntries;
using Microsoft.Exchange.HttpProxy.Routing.RoutingKeys;
using Microsoft.Exchange.HttpProxy.Routing.Serialization;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.Net.MonitoringWebClient;
using Microsoft.Exchange.Net.Protocols;
using Microsoft.Exchange.Security.Authentication;
using Microsoft.Exchange.Security.Authorization;
using Microsoft.Exchange.Security.OAuth;
using Microsoft.Exchange.VariantConfiguration;
using Microsoft.Exchange.VariantConfiguration.Cafe;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000AE RID: 174
	internal abstract class ProxyRequestHandler : IHttpAsyncHandler, IHttpHandler, IAsyncResult, IDisposeTrackable, IDisposable, IRequestContext
	{
		// Token: 0x060005FD RID: 1533 RVA: 0x00021C44 File Offset: 0x0001FE44
		internal ProxyRequestHandler()
		{
			this.disposeTracker = this.GetDisposeTracker();
			this.TraceContext = 0;
			this.State = ProxyRequestHandler.ProxyState.None;
			this.ServerAsyncState = new AsyncStateHolder(this);
			this.UseRoutingHintForAnchorMailbox = true;
			this.HasPreemptivelyCheckedForRoutingHint = false;
		}

		// Token: 0x17000143 RID: 323
		// (get) Token: 0x060005FE RID: 1534 RVA: 0x00003165 File Offset: 0x00001365
		public bool IsReusable
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000144 RID: 324
		// (get) Token: 0x060005FF RID: 1535 RVA: 0x00021C98 File Offset: 0x0001FE98
		public object AsyncState
		{
			get
			{
				object obj = this.LockObject;
				object result;
				lock (obj)
				{
					result = this.asyncState;
				}
				return result;
			}
		}

		// Token: 0x17000145 RID: 325
		// (get) Token: 0x06000600 RID: 1536 RVA: 0x00021CDC File Offset: 0x0001FEDC
		public bool IsCompleted
		{
			get
			{
				object obj = this.LockObject;
				bool result;
				lock (obj)
				{
					result = (this.State == ProxyRequestHandler.ProxyState.Completed || this.State == ProxyRequestHandler.ProxyState.CleanedUp);
				}
				return result;
			}
		}

		// Token: 0x17000146 RID: 326
		// (get) Token: 0x06000601 RID: 1537 RVA: 0x00003165 File Offset: 0x00001365
		public bool CompletedSynchronously
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000147 RID: 327
		// (get) Token: 0x06000602 RID: 1538 RVA: 0x00021D30 File Offset: 0x0001FF30
		public WaitHandle AsyncWaitHandle
		{
			get
			{
				object obj = this.LockObject;
				WaitHandle result;
				lock (obj)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::AsyncWaitHandle::get]: Context {0}", this.TraceContext);
					}
					if (this.completedWaitHandle == null)
					{
						this.completedWaitHandle = new ManualResetEvent(false);
						if (this.IsCompleted && !this.completedWaitHandle.Set())
						{
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
							{
								ExTraceGlobals.VerboseTracer.TraceError<int>((long)this.GetHashCode(), "[ProxyRequestHandler::AsyncWaitHandle::get]: Failed to set the WaitHandle. This condition can lead to possible deadlock. Context {0}", this.TraceContext);
							}
							throw new InvalidOperationException("Unable to set wait handle.");
						}
					}
					result = this.completedWaitHandle;
				}
				return result;
			}
		}

		// Token: 0x17000148 RID: 328
		// (get) Token: 0x06000603 RID: 1539 RVA: 0x00021DF8 File Offset: 0x0001FFF8
		// (set) Token: 0x06000604 RID: 1540 RVA: 0x00021E00 File Offset: 0x00020000
		public IAuthBehavior AuthBehavior { get; private set; }

		// Token: 0x17000149 RID: 329
		// (get) Token: 0x06000605 RID: 1541 RVA: 0x00021E09 File Offset: 0x00020009
		// (set) Token: 0x06000606 RID: 1542 RVA: 0x00021E11 File Offset: 0x00020011
		public HttpContext HttpContext { get; private set; }

		// Token: 0x1700014A RID: 330
		// (get) Token: 0x06000607 RID: 1543 RVA: 0x00021E1A File Offset: 0x0002001A
		// (set) Token: 0x06000608 RID: 1544 RVA: 0x00021E22 File Offset: 0x00020022
		public LatencyTracker LatencyTracker { get; private set; }

		// Token: 0x1700014B RID: 331
		// (get) Token: 0x06000609 RID: 1545 RVA: 0x00021E2B File Offset: 0x0002002B
		public Guid ActivityId
		{
			get
			{
				if (this.Logger != null)
				{
					return this.Logger.ActivityId;
				}
				return Guid.Empty;
			}
		}

		// Token: 0x1700014C RID: 332
		// (get) Token: 0x0600060A RID: 1546 RVA: 0x00021E46 File Offset: 0x00020046
		// (set) Token: 0x0600060B RID: 1547 RVA: 0x00021E4E File Offset: 0x0002004E
		public int TraceContext { get; private set; }

		// Token: 0x1700014D RID: 333
		// (get) Token: 0x0600060C RID: 1548 RVA: 0x00021E57 File Offset: 0x00020057
		// (set) Token: 0x0600060D RID: 1549 RVA: 0x00021E5F File Offset: 0x0002005F
		public RequestDetailsLogger Logger { get; private set; }

		// Token: 0x1700014E RID: 334
		// (get) Token: 0x0600060E RID: 1550 RVA: 0x00021E68 File Offset: 0x00020068
		public bool IsDisposed
		{
			get
			{
				return this.disposed;
			}
		}

		// Token: 0x1700014F RID: 335
		// (get) Token: 0x0600060F RID: 1551 RVA: 0x00021E70 File Offset: 0x00020070
		// (set) Token: 0x06000610 RID: 1552 RVA: 0x00021E78 File Offset: 0x00020078
		internal HttpApplication HttpApplication { get; private set; }

		// Token: 0x17000150 RID: 336
		// (get) Token: 0x06000611 RID: 1553 RVA: 0x00021E81 File Offset: 0x00020081
		// (set) Token: 0x06000612 RID: 1554 RVA: 0x00021E89 File Offset: 0x00020089
		internal HttpRequest ClientRequest { get; private set; }

		// Token: 0x17000151 RID: 337
		// (get) Token: 0x06000613 RID: 1555 RVA: 0x00021E92 File Offset: 0x00020092
		// (set) Token: 0x06000614 RID: 1556 RVA: 0x00021E9A File Offset: 0x0002009A
		internal Stream ClientRequestStream { get; set; }

		// Token: 0x17000152 RID: 338
		// (get) Token: 0x06000615 RID: 1557 RVA: 0x00021EA3 File Offset: 0x000200A3
		// (set) Token: 0x06000616 RID: 1558 RVA: 0x00021EAB File Offset: 0x000200AB
		internal HttpResponse ClientResponse { get; private set; }

		// Token: 0x17000153 RID: 339
		// (get) Token: 0x06000617 RID: 1559 RVA: 0x00021EB4 File Offset: 0x000200B4
		// (set) Token: 0x06000618 RID: 1560 RVA: 0x00021EBC File Offset: 0x000200BC
		internal HttpWebRequest ServerRequest { get; private set; }

		// Token: 0x17000154 RID: 340
		// (get) Token: 0x06000619 RID: 1561 RVA: 0x00021EC5 File Offset: 0x000200C5
		// (set) Token: 0x0600061A RID: 1562 RVA: 0x00021ECD File Offset: 0x000200CD
		internal Stream ServerRequestStream { get; private set; }

		// Token: 0x17000155 RID: 341
		// (get) Token: 0x0600061B RID: 1563 RVA: 0x00021ED6 File Offset: 0x000200D6
		// (set) Token: 0x0600061C RID: 1564 RVA: 0x00021EDE File Offset: 0x000200DE
		internal HttpWebResponse ServerResponse { get; private set; }

		// Token: 0x17000156 RID: 342
		// (get) Token: 0x0600061D RID: 1565 RVA: 0x00021EE7 File Offset: 0x000200E7
		// (set) Token: 0x0600061E RID: 1566 RVA: 0x00021EEF File Offset: 0x000200EF
		internal Stream ServerResponseStream { get; private set; }

		// Token: 0x17000157 RID: 343
		// (get) Token: 0x0600061F RID: 1567 RVA: 0x00021EF8 File Offset: 0x000200F8
		// (set) Token: 0x06000620 RID: 1568 RVA: 0x00021F00 File Offset: 0x00020100
		internal AsyncStateHolder ServerAsyncState { get; private set; }

		// Token: 0x17000158 RID: 344
		// (get) Token: 0x06000621 RID: 1569 RVA: 0x00021F09 File Offset: 0x00020109
		internal object LockObject
		{
			get
			{
				return this.lockObject;
			}
		}

		// Token: 0x17000159 RID: 345
		// (get) Token: 0x06000622 RID: 1570 RVA: 0x00021F11 File Offset: 0x00020111
		// (set) Token: 0x06000623 RID: 1571 RVA: 0x00021F19 File Offset: 0x00020119
		internal bool ResponseHeadersSent { get; set; }

		// Token: 0x1700015A RID: 346
		// (get) Token: 0x06000624 RID: 1572 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool ImplementsOutOfBandProxyLogon
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700015B RID: 347
		// (get) Token: 0x06000625 RID: 1573 RVA: 0x00021F22 File Offset: 0x00020122
		protected virtual HttpStatusCode StatusCodeSignifyingOutOfBandProxyLogonNeeded
		{
			get
			{
				throw new NotImplementedException("Should not be called - out-of-band proxy logon unsupported.");
			}
		}

		// Token: 0x1700015C RID: 348
		// (get) Token: 0x06000626 RID: 1574 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool WillContentBeChangedDuringStreaming
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700015D RID: 349
		// (get) Token: 0x06000627 RID: 1575 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool WillAddProtocolSpecificCookiesToServerRequest
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700015E RID: 350
		// (get) Token: 0x06000628 RID: 1576 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool WillAddProtocolSpecificCookiesToClientResponse
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700015F RID: 351
		// (get) Token: 0x06000629 RID: 1577 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool ShouldForceUnbufferedClientResponseOutput
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000160 RID: 352
		// (get) Token: 0x0600062A RID: 1578 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool ProxyKerberosAuthentication
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000161 RID: 353
		// (get) Token: 0x0600062B RID: 1579 RVA: 0x00003193 File Offset: 0x00001393
		protected virtual bool ShouldSendFullActivityScope
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000162 RID: 354
		// (get) Token: 0x0600062C RID: 1580 RVA: 0x00021F2E File Offset: 0x0002012E
		// (set) Token: 0x0600062D RID: 1581 RVA: 0x00021F36 File Offset: 0x00020136
		protected ProxyRequestHandler.ProxyState State { get; set; }

		// Token: 0x17000163 RID: 355
		// (get) Token: 0x0600062E RID: 1582 RVA: 0x00021F3F File Offset: 0x0002013F
		// (set) Token: 0x0600062F RID: 1583 RVA: 0x00021F47 File Offset: 0x00020147
		protected PfdTracer PfdTracer { get; set; }

		// Token: 0x17000164 RID: 356
		// (get) Token: 0x06000630 RID: 1584 RVA: 0x00021F50 File Offset: 0x00020150
		// (set) Token: 0x06000631 RID: 1585 RVA: 0x00021F58 File Offset: 0x00020158
		private protected bool UseRoutingHintForAnchorMailbox { protected get; private set; }

		// Token: 0x17000165 RID: 357
		// (get) Token: 0x06000632 RID: 1586 RVA: 0x00021F61 File Offset: 0x00020161
		// (set) Token: 0x06000633 RID: 1587 RVA: 0x00021F69 File Offset: 0x00020169
		protected bool HasPreemptivelyCheckedForRoutingHint { get; set; }

		// Token: 0x17000166 RID: 358
		// (get) Token: 0x06000634 RID: 1588 RVA: 0x00021F72 File Offset: 0x00020172
		// (set) Token: 0x06000635 RID: 1589 RVA: 0x00021F7A File Offset: 0x0002017A
		protected bool IsAnchorMailboxFromRoutingHint { get; set; }

		// Token: 0x17000167 RID: 359
		// (get) Token: 0x06000636 RID: 1590 RVA: 0x00021F83 File Offset: 0x00020183
		protected bool IsRetryOnErrorEnabled
		{
			get
			{
				return HttpProxySettings.MaxRetryOnError.Value > 0;
			}
		}

		// Token: 0x17000168 RID: 360
		// (get) Token: 0x06000637 RID: 1591 RVA: 0x00021F92 File Offset: 0x00020192
		protected bool IsRetryOnConnectivityErrorEnabled
		{
			get
			{
				return this.IsRetryOnErrorEnabled && HttpProxySettings.RetryOnConnectivityErrorEnabled.Value;
			}
		}

		// Token: 0x17000169 RID: 361
		// (get) Token: 0x06000638 RID: 1592 RVA: 0x00021FA8 File Offset: 0x000201A8
		protected bool IsRetryingOnError
		{
			get
			{
				return this.retryOnErrorCounter > 0;
			}
		}

		// Token: 0x1700016A RID: 362
		// (get) Token: 0x06000639 RID: 1593 RVA: 0x00021FB3 File Offset: 0x000201B3
		protected bool ShouldRetryOnError
		{
			get
			{
				return this.retryOnErrorCounter < HttpProxySettings.MaxRetryOnError.Value;
			}
		}

		// Token: 0x1700016B RID: 363
		// (get) Token: 0x0600063A RID: 1594 RVA: 0x00021FC8 File Offset: 0x000201C8
		private bool IsInRoutingState
		{
			get
			{
				object obj = this.LockObject;
				bool result;
				lock (obj)
				{
					result = (this.State == ProxyRequestHandler.ProxyState.None || this.State == ProxyRequestHandler.ProxyState.Initializing || this.State == ProxyRequestHandler.ProxyState.CalculateBackEnd || this.State == ProxyRequestHandler.ProxyState.CalculateBackEndSecondRound || this.State == ProxyRequestHandler.ProxyState.PrepareServerRequest || this.State == ProxyRequestHandler.ProxyState.ProxyRequestData || this.State == ProxyRequestHandler.ProxyState.WaitForServerResponse || this.State == ProxyRequestHandler.ProxyState.WaitForProxyLogonRequestStream || this.State == ProxyRequestHandler.ProxyState.WaitForProxyLogonResponse);
				}
				return result;
			}
		}

		// Token: 0x1700016C RID: 364
		// (get) Token: 0x0600063B RID: 1595 RVA: 0x0002205C File Offset: 0x0002025C
		private bool IsInRetryableState
		{
			get
			{
				object obj = this.LockObject;
				bool result;
				lock (obj)
				{
					result = (this.State == ProxyRequestHandler.ProxyState.None || this.State == ProxyRequestHandler.ProxyState.Initializing || this.State == ProxyRequestHandler.ProxyState.CalculateBackEnd || this.State == ProxyRequestHandler.ProxyState.CalculateBackEndSecondRound || this.State == ProxyRequestHandler.ProxyState.PrepareServerRequest || this.State == ProxyRequestHandler.ProxyState.ProxyRequestData || this.State == ProxyRequestHandler.ProxyState.WaitForServerResponse);
				}
				return result;
			}
		}

		// Token: 0x1700016D RID: 365
		// (get) Token: 0x0600063C RID: 1596 RVA: 0x000220DC File Offset: 0x000202DC
		private bool IsInPostRoutingState
		{
			get
			{
				object obj = this.LockObject;
				bool result;
				lock (obj)
				{
					result = (this.State == ProxyRequestHandler.ProxyState.ProxyResponseData || this.State == ProxyRequestHandler.ProxyState.Completed || this.State == ProxyRequestHandler.ProxyState.CleanedUp);
				}
				return result;
			}
		}

		// Token: 0x0600063D RID: 1597 RVA: 0x00022138 File Offset: 0x00020338
		public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
		{
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 9, this.LatencyTracker.GetCurrentLatency(LatencyTrackerKey.ModuleToHandlerSwitchingLatency));
			this.LogElapsedTime("E_BPR");
			object obj = this.LockObject;
			lock (obj)
			{
				this.LatencyTracker.StartTracking(LatencyTrackerKey.RequestHandlerLatency, false);
				AspNetHelper.AddTimestampHeaderIfNecessary(context.Request.Headers, "X-FrontEnd-Handler-Begin");
				this.asyncCallback = cb;
				this.asyncState = extraData;
				this.PfdTracer = new PfdTracer(this.TraceContext, this.GetHashCode());
				this.PfdTracer.TraceRequest("ClientRequest", this.ClientRequest);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, string, Uri>((long)this.GetHashCode(), "[ProxyRequestHandler::BeginProcessRequest]: Called for Context {0}; method {1}; url {2}", this.TraceContext, this.ClientRequest.HttpMethod, this.ClientRequest.Url);
				}
				this.DoProtocolSpecificBeginRequestLogging();
				ThreadPool.QueueUserWorkItem(new WaitCallback(this.BeginCalculateTargetBackEnd));
				this.State = ProxyRequestHandler.ProxyState.CalculateBackEnd;
				this.LogElapsedTime("L_BPR");
			}
			return this;
		}

		// Token: 0x0600063E RID: 1598 RVA: 0x00022264 File Offset: 0x00020464
		public void EndProcessRequest(IAsyncResult result)
		{
			try
			{
				this.LogElapsedTime("E_EPR");
				object obj = this.LockObject;
				lock (obj)
				{
					Exception ex = this.asyncException;
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int, string>((long)this.GetHashCode(), "[ProxyRequestHandler::EndProcessRequest]: Called for Context {0}; status code {1}", this.TraceContext, (ex != null) ? ex.ToString() : this.ClientResponse.StatusCode.ToString(CultureInfo.InvariantCulture));
					}
					bool isClientConnected = this.ClientResponse.IsClientConnected;
					this.Dispose();
					if (ex != null)
					{
						PerfCounters.UpdateMovingPercentagePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingPercentageMailboxServerFailure);
						if (isClientConnected)
						{
							Diagnostics.ReportException(ex, FrontEndHttpProxyEventLogConstants.Tuple_InternalServerError, null, "Exception from EndProcessRequest event: {0}");
							throw new AggregateException(new Exception[]
							{
								ex
							});
						}
						if (!AspNetHelper.IsExceptionExpectedWhenDisconnected(ex))
						{
							this.InspectDisconnectException(ex);
						}
					}
				}
			}
			finally
			{
				long currentLatency = this.LatencyTracker.GetCurrentLatency(LatencyTrackerKey.HandlerCompletionLatency);
				if (currentLatency >= 0L)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 11, currentLatency);
				}
				long currentLatency2 = this.LatencyTracker.GetCurrentLatency(LatencyTrackerKey.RequestHandlerLatency);
				if (currentLatency2 >= 0L)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 12, currentLatency2);
				}
				this.LogElapsedTime("L_EPR");
				this.LatencyTracker.StartTracking(LatencyTrackerKey.HandlerToModuleSwitchingLatency, false);
			}
		}

		// Token: 0x0600063F RID: 1599 RVA: 0x000223D0 File Offset: 0x000205D0
		public void ProcessRequest(HttpContext context)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
			{
				ExTraceGlobals.VerboseTracer.TraceError((long)this.GetHashCode(), "[ProxyRequestHandler::ProcessRequest]: ProcessRequest() should never be called!");
			}
			throw new NotSupportedException();
		}

		// Token: 0x06000640 RID: 1600 RVA: 0x000223FA File Offset: 0x000205FA
		public DisposeTracker GetDisposeTracker()
		{
			return DisposeTracker.Get<ProxyRequestHandler>(this);
		}

		// Token: 0x06000641 RID: 1601 RVA: 0x00022402 File Offset: 0x00020602
		public void SuppressDisposeTracker()
		{
			if (this.disposeTracker != null)
			{
				this.disposeTracker.Suppress();
				this.disposeTracker = null;
			}
		}

		// Token: 0x06000642 RID: 1602 RVA: 0x0002241E File Offset: 0x0002061E
		public void Dispose()
		{
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				object obj = this.LockObject;
				lock (obj)
				{
					if (!this.disposed)
					{
						long num = 0L;
						LatencyTracker.GetLatency(delegate()
						{
							this.Cleanup();
						}, out num);
						if (num > 50L)
						{
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "HandlerCleanupLatency", num);
						}
						this.disposed = true;
						GC.SuppressFinalize(this);
					}
				}
			}, new Diagnostics.LastChanceExceptionHandler(RequestDetailsLogger.LastChanceExceptionHandler));
		}

		// Token: 0x06000643 RID: 1603 RVA: 0x0002243D File Offset: 0x0002063D
		internal void Run(HttpContext context)
		{
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				try
				{
					this.LatencyTracker = LatencyTracker.FromHttpContext(context);
					this.Logger = RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(context);
					this.LogElapsedTime("E_Run");
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "ProxyState-Run", this.State);
					this.HttpApplication = context.ApplicationInstance;
					this.HttpContext = context;
					this.ClientRequest = context.Request;
					this.ClientResponse = context.Response;
					this.TraceContext = (int)context.Items[Constants.TraceContextKey];
					this.ResponseHeadersSent = false;
					this.AuthBehavior = DefaultAuthBehavior.CreateAuthBehavior(this.HttpContext);
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int, string, Uri>((long)this.GetHashCode(), "[ProxyRequestHandler::Run]: Called for Context {0}; method {1}; url {2}", this.TraceContext, this.ClientRequest.HttpMethod, this.ClientRequest.Url);
					}
					this.State = ProxyRequestHandler.ProxyState.Initializing;
					try
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<int, string, Uri>((long)this.GetHashCode(), "[ProxyRequestHandler::Run]: Calling OnInitializingHandler for Context {0}; method {1}; url {2}", this.TraceContext, this.ClientRequest.HttpMethod, this.ClientRequest.Url);
						}
						this.OnInitializingHandler();
					}
					catch (HttpException ex)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<HttpException>(0L, "[ProxyRequestHandler::Run] HttpException thrown during handler initialization: {0}", ex);
						}
						string text = ex.ToString();
						if (this.Logger != null)
						{
							this.Logger.AppendGenericError("ProxyRequestHandler_Run_Exception", text);
						}
						this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, ex.GetHttpCode());
						this.ClientResponse.SuppressContent = true;
						this.HttpContext.ApplicationInstance.CompleteRequest();
						this.SetRequestFailureContext(1, ex.GetHttpCode(), ex.Message, text, null, null);
						this.Dispose();
						return;
					}
					PerfCounters.HttpProxyCountersInstance.TotalProxyRequests.Increment();
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int, string, Uri>((long)this.GetHashCode(), "[ProxyRequestHandler::Run]: Remapping handler for Context {0}; method {1}; url {2}", this.TraceContext, this.ClientRequest.HttpMethod, this.ClientRequest.Url);
					}
					context.RemapHandler(this);
				}
				finally
				{
					this.LogElapsedTime("L_Run");
				}
			}, new Diagnostics.LastChanceExceptionHandler(RequestDetailsLogger.LastChanceExceptionHandler));
		}

		// Token: 0x06000644 RID: 1604 RVA: 0x00022470 File Offset: 0x00020670
		internal void CallThreadEntranceMethod(Action method)
		{
			try
			{
				this.HttpContext.SetActivityScopeOnCurrentThread(this.Logger);
				Diagnostics.SendWatsonReportOnUnhandledException(method);
			}
			catch (Exception ex)
			{
				object obj = this.lockObject;
				lock (obj)
				{
					if (this.State != ProxyRequestHandler.ProxyState.CleanedUp && this.State != ProxyRequestHandler.ProxyState.Completed)
					{
						this.CompleteWithError(ex, "CallThreadEntranceMethod");
					}
				}
			}
		}

		// Token: 0x06000645 RID: 1605 RVA: 0x000224F4 File Offset: 0x000206F4
		protected void CompleteForRedirect(string redirectUrl)
		{
			this.LogElapsedTime("E_CompRedir");
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
			{
				ExTraceGlobals.VerboseTracer.TraceError<int, string>((long)this.GetHashCode(), "[ProxyRequestHandler::CompleteForRedirect]: Context {0}; redirectUrl {1}", this.TraceContext, redirectUrl);
			}
			this.Logger.AppendGenericError("Redirected", redirectUrl);
			this.Complete();
			this.LogElapsedTime("L_CompRedir");
		}

		// Token: 0x06000646 RID: 1606 RVA: 0x00022558 File Offset: 0x00020758
		protected virtual void Cleanup()
		{
			this.LogElapsedTime("E_Cleanup");
			if (this.State != ProxyRequestHandler.ProxyState.CleanedUp)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::Cleanup]: Context {0}", this.TraceContext);
				}
				if (this.ServerRequest != null)
				{
					try
					{
						this.ServerRequest.Abort();
						this.ServerRequest = null;
					}
					catch (Exception)
					{
					}
				}
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ServerAsyncState);
				this.ServerAsyncState = null;
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.completedWaitHandle);
				this.completedWaitHandle = null;
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.bufferedRegionStream);
				this.bufferedRegionStream = null;
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ClientRequestStream);
				this.ClientRequestStream = null;
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ServerRequestStream);
				this.ServerRequestStream = null;
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ServerResponseStream);
				this.ServerResponseStream = null;
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ServerResponse);
				this.ServerResponse = null;
				this.CleanUpRequestStreamsAndBuffer();
				this.CleanUpResponseStreamsAndBuffer();
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.mailboxServerLocator);
				this.mailboxServerLocator = null;
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.authenticationContext);
				this.authenticationContext = null;
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.disposeTracker);
				this.disposeTracker = null;
				this.State = ProxyRequestHandler.ProxyState.CleanedUp;
			}
			this.LogElapsedTime("L_Cleanup");
		}

		// Token: 0x06000647 RID: 1607 RVA: 0x000226A8 File Offset: 0x000208A8
		protected virtual void OnInitializingHandler()
		{
			if (!LiveIdBasicAuthModule.SyncADBackendOnly && this.AuthBehavior.AuthState == AuthState.FrontEndFullAuth)
			{
				DatacenterRedirectStrategy.CheckLiveIdBasicPartialAuthResult(this.HttpContext);
			}
		}

		// Token: 0x06000648 RID: 1608 RVA: 0x000226CC File Offset: 0x000208CC
		protected virtual void DoProtocolSpecificBeginRequestLogging()
		{
			if ((this.ClientRequest.Url.LocalPath.IndexOf("owa/service.svc") > 0 || this.ClientRequest.Url.LocalPath.IndexOf("owa/integrated/service.svc") > 0) && this.ClientRequest.QueryString["Action"] != null)
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 21, this.ClientRequest.QueryString["Action"]);
			}
		}

		// Token: 0x06000649 RID: 1609 RVA: 0x00022751 File Offset: 0x00020951
		protected virtual void StartOutOfBandProxyLogon(object extraData)
		{
			throw new NotImplementedException("Should never start an out-of-band proxy logon - not supported!");
		}

		// Token: 0x0600064A RID: 1610 RVA: 0x00003193 File Offset: 0x00001393
		protected virtual bool ShouldCopyCookieToClientResponse(Cookie cookie)
		{
			return true;
		}

		// Token: 0x0600064B RID: 1611 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void CopySupplementalCookiesToClientResponse()
		{
		}

		// Token: 0x0600064C RID: 1612 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void ClearBackEndOverrideCookie()
		{
		}

		// Token: 0x0600064D RID: 1613 RVA: 0x00022760 File Offset: 0x00020960
		protected void CopyServerCookieToClientResponse(Cookie serverCookie)
		{
			HttpCookie httpCookie = new HttpCookie(serverCookie.Name, serverCookie.Value);
			httpCookie.Path = serverCookie.Path;
			if (!string.IsNullOrEmpty(this.ClientRequest.UserAgent) && new UserAgent(this.ClientRequest.UserAgent, this.ClientRequest.Cookies).DoesSupportSameSiteNone() && string.Equals(httpCookie.Name, "msExchEcpCanary", StringComparison.OrdinalIgnoreCase))
			{
				HttpCookie httpCookie2 = httpCookie;
				httpCookie2.Path += ";SameSite=None";
			}
			httpCookie.Expires = serverCookie.Expires;
			httpCookie.HttpOnly = serverCookie.HttpOnly;
			httpCookie.Secure = serverCookie.Secure;
			if (HttpProxySettings.AddHostHeaderInServerRequestEnabled.Value && !string.IsNullOrEmpty(serverCookie.Domain))
			{
				httpCookie.Domain = serverCookie.Domain;
			}
			this.ClientResponse.Cookies.Add(httpCookie);
		}

		// Token: 0x0600064E RID: 1614 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void SetProtocolSpecificServerRequestParameters(HttpWebRequest serverRequest)
		{
		}

		// Token: 0x0600064F RID: 1615 RVA: 0x00022844 File Offset: 0x00020A44
		protected virtual void AddProtocolSpecificHeadersToServerRequest(WebHeaderCollection headers)
		{
			this.LogElapsedTime("E_AddHeaders");
			string fullRawUrl = Extensions.GetFullRawUrl(SharedHttpContextWrapper.GetWrapper(this.HttpContext).Request);
			try
			{
				headers[Constants.MsExchProxyUri] = fullRawUrl;
			}
			catch (ArgumentException)
			{
				headers[Constants.MsExchProxyUri] = Uri.EscapeUriString(fullRawUrl);
			}
			headers[Constants.XIsFromCafe] = Constants.IsFromCafeHeaderValue;
			headers[Constants.XSourceCafeServer] = HttpProxyGlobals.LocalMachineFqdn.Member;
			if (this.AuthBehavior.AuthState != AuthState.BackEndFullAuth)
			{
				if (this.ClientRequest.IsAuthenticated)
				{
					CommonAccessToken commonAccessToken = AspNetHelper.FixupCommonAccessToken(this.HttpContext, this.AnchoredRoutingTarget.BackEndServer.Version);
					if (commonAccessToken == null)
					{
						commonAccessToken = (this.HttpContext.Items["Item-CommonAccessToken"] as CommonAccessToken);
					}
					if (commonAccessToken != null)
					{
						headers["X-CommonAccessToken"] = commonAccessToken.Serialize(new int?(HttpProxySettings.CompressTokenMinimumSize.Value));
					}
				}
				else if (this.ShouldBackendRequestBeAnonymous())
				{
					headers["X-CommonAccessToken"] = new CommonAccessToken(9).Serialize();
				}
			}
			string value;
			if (HttpContextItemParser.TryGetLiveIdMemberName(this.HttpContext.Items, ref value))
			{
				headers[Constants.WLIDMemberNameHeaderName] = value;
				headers[Constants.LiveIdMemberName] = value;
			}
			string value2 = this.HttpContext.Items[Constants.MissingDirectoryUserObjectKey] as string;
			if (!string.IsNullOrEmpty(value2))
			{
				headers[Constants.MissingDirectoryUserObjectHeader] = value2;
			}
			string value3;
			if (HttpContextItemParser.TryGetOrganizationContext(this.HttpContext.Items, ref value3))
			{
				headers[Constants.OrganizationContextHeader] = value3;
			}
			if (!Utilities.IsPartnerHostedOnly && !CafeConfiguration.GetSnapshot(MachineSettingsContext.Local, null, null).NoVDirLocationHint.Enabled && HttpProxyGlobals.VdirObject.Member != null)
			{
				string value4 = HttpProxyGlobals.VdirObject.Member.Id.ObjectGuid.ToString();
				headers[Constants.VDirObjectID] = value4;
			}
			if (!this.IsBackendServerCacheValidationEnabled && !this.ShouldRetryOnError)
			{
				this.RemoveNotNeededHttpContextContent();
			}
			this.LogElapsedTime("L_AddHeaders");
		}

		// Token: 0x06000650 RID: 1616 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void DoProtocolSpecificBeginProcess()
		{
		}

		// Token: 0x06000651 RID: 1617 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool ShouldBackendRequestBeAnonymous()
		{
			return false;
		}

		// Token: 0x06000652 RID: 1618 RVA: 0x00022A64 File Offset: 0x00020C64
		protected virtual bool ShouldBlockCurrentOAuthRequest()
		{
			return this.ProxyToDownLevel;
		}

		// Token: 0x06000653 RID: 1619 RVA: 0x00022A6C File Offset: 0x00020C6C
		protected void PrepareServerRequest(HttpWebRequest serverRequest)
		{
			this.LogElapsedTime("E_PrepSvrReq");
			if (this.ClientRequest.IsAuthenticated)
			{
				OAuthIdentity oauthIdentity = this.HttpContext.User.Identity as OAuthIdentity;
				if (oauthIdentity != null)
				{
					if (this.ShouldBlockCurrentOAuthRequest())
					{
						throw new HttpException(403, "Cannot proxy OAuth request to down level server.", InvalidOAuthTokenException.OAuthRequestProxyToDownLevelException.Value);
					}
					if (!oauthIdentity.IsAppOnly)
					{
						RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "ActAsUserVerified", oauthIdentity.ActAsUser.IsUserVerified);
					}
				}
			}
			serverRequest.ServicePoint.Expect100Continue = false;
			serverRequest.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint(this.BindIPEndPointCallback);
			if (this.ProxyKerberosAuthentication)
			{
				serverRequest.ConnectionGroupName = this.ClientRequest.UserHostAddress + ":" + GccUtils.GetClientPort(SharedHttpContextWrapper.GetWrapper(this.HttpContext));
			}
			else if (this.AuthBehavior.AuthState == AuthState.BackEndFullAuth || this.ShouldBackendRequestBeAnonymous() || (HttpProxySettings.TestBackEndSupportEnabled.Value && !string.IsNullOrEmpty(this.ClientRequest.Headers[Constants.TestBackEndUrlRequestHeaderKey])))
			{
				serverRequest.ConnectionGroupName = "Unauthenticated";
			}
			else
			{
				serverRequest.ConnectionGroupName = Constants.KerberosPackageValue;
				long num = 0L;
				LatencyTracker.GetLatency(delegate()
				{
					serverRequest.Headers[Constants.AuthorizationHeader] = KerberosUtilities.GenerateKerberosAuthHeader(serverRequest.Address.Host, this.TraceContext, ref this.authenticationContext, ref this.kerberosChallenge);
				}, out num);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 10, num);
			}
			serverRequest.AutomaticDecompression = DecompressionMethods.None;
			string text = this.ClientRequest.Headers[Constants.AcceptEncodingHeaderName];
			if (!string.IsNullOrEmpty(text))
			{
				if (-1 != text.IndexOf(Constants.GzipHeaderValue, StringComparison.OrdinalIgnoreCase))
				{
					serverRequest.Headers[Constants.AcceptEncodingHeaderName] = Constants.GzipHeaderValue;
				}
				else if (-1 != text.IndexOf(Constants.DeflateHeaderValue, StringComparison.OrdinalIgnoreCase))
				{
					serverRequest.Headers[Constants.AcceptEncodingHeaderName] = Constants.DeflateHeaderValue;
				}
			}
			serverRequest.AllowAutoRedirect = false;
			serverRequest.SendChunked = false;
			serverRequest.ServerCertificateValidationCallback = ProxyApplication.RemoteCertificateValidationCallback;
			CertificateValidationManager.SetComponentId(serverRequest, Constants.CertificateValidationComponentId);
			if (HttpProxyRegistry.AreGccStoredSecretKeysValid.Member)
			{
				this.CopyOrCreateNewXGccProxyInfoHeader(serverRequest);
			}
			if (HttpProxySettings.CafeV1RUMEnabled.Value)
			{
				this.AddRoutingEntryHeaderToRequest(serverRequest);
			}
			this.CopyHeadersToServerRequest(serverRequest);
			this.CopyCookiesToServerRequest(serverRequest);
			this.SetProtocolSpecificServerRequestParameters(serverRequest);
			this.AddProtocolSpecificHeadersToServerRequest(serverRequest.Headers);
			TimeSpan timeout;
			if (this.ShouldSetRequestTimeout(out timeout))
			{
				this.SetupRequestTimeout(serverRequest, timeout);
			}
			this.LogElapsedTime("L_PrepSvrReq");
		}

		// Token: 0x06000654 RID: 1620 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool TryHandleProtocolSpecificResponseErrors(WebException e)
		{
			return false;
		}

		// Token: 0x06000655 RID: 1621 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool TryHandleProtocolSpecificRequestErrors(Exception e)
		{
			return false;
		}

		// Token: 0x06000656 RID: 1622 RVA: 0x00022D3A File Offset: 0x00020F3A
		protected void BeginProxyRequest(object extraData)
		{
			this.CallThreadEntranceMethod(delegate
			{
				this.LogElapsedTime("E_BegProxyReq");
				try
				{
					object obj = this.LockObject;
					lock (obj)
					{
						PerfCounters.IncrementMovingPercentagePerformanceCounterBase(PerfCounters.HttpProxyCountersInstance.MovingPercentageMailboxServerFailure);
						try
						{
							FrontEndProxyServerSettingsProvider instance = FrontEndProxyServerSettingsProvider.Instance;
							if (this.AnchoredRoutingTarget != null && this.AnchoredRoutingTarget.BackEndServer != null && !instance.IsBackEndProxyAllowed(this.AnchoredRoutingTarget.BackEndServer.Fqdn, this.AnchoredRoutingTarget.AnchorMailbox.GetTenantContext().OrganizationId))
							{
								throw new HttpException(503, string.Format("Cross forest proxy to {0} is blocked.", this.AnchoredRoutingTarget.BackEndServer.Fqdn));
							}
							Uri uri = this.GetTargetBackEndServerUrl();
							if (!this.ProxyKerberosAuthentication && !string.Equals(uri.Host, this.AnchoredRoutingTarget.BackEndServer.Fqdn, StringComparison.OrdinalIgnoreCase))
							{
								throw new HttpException(503, "Service Unavailable");
							}
							bool flag2 = false;
							if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
							{
								ExTraceGlobals.FaultInjectionTracer.TraceTest<bool>(44828U, ref flag2);
							}
							if (flag2)
							{
								throw new HttpException(500, "RequestFailureContextTests");
							}
							bool flag3 = false;
							if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
							{
								ExTraceGlobals.FaultInjectionTracer.TraceTest<bool>(3448122685U, ref flag3);
							}
							if (flag3)
							{
								throw new WebException("RequestFailureContextTests", WebExceptionStatus.Success);
							}
							if (HttpProxySettings.TestBackEndSupportEnabled.Value)
							{
								string testBackEndUrl = this.ClientRequest.GetTestBackEndUrl();
								if (!string.IsNullOrEmpty(testBackEndUrl))
								{
									uri = new Uri(testBackEndUrl);
								}
							}
							if (this.AnchoredRoutingTarget != null)
							{
								RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 14, Utilities.FormatServerVersion(this.AnchoredRoutingTarget.BackEndServer.Version));
								this.PfdTracer.TraceProxyTarget(this.AnchoredRoutingTarget);
							}
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 7, "Proxy");
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 13, uri.Host);
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 49, "CafeV1");
							this.Logger.SetRoutingType(uri);
							string absoluteUri = this.ClientRequest.Url.AbsoluteUri;
							if (string.Compare(absoluteUri, uri.AbsoluteUri, StringComparison.InvariantCultureIgnoreCase) == 0 && absoluteUri.IndexOf("mrsproxy", StringComparison.InvariantCultureIgnoreCase) == -1 && absoluteUri.IndexOf("mailboxreplicationservice", StringComparison.InvariantCultureIgnoreCase) == -1)
							{
								throw new HttpException(403, "Redirect loop detected");
							}
							this.ClientResponse.Headers["X-CalculatedBETarget"] = uri.Host;
							if (Extensions.IsProxyTestProbeRequest(Extensions.GetHttpRequestBase(this.ClientRequest)))
							{
								this.CompleteForLocalProbe();
							}
							else
							{
								this.ServerRequest = this.CreateServerRequest(uri);
								PerfCounters.IncrementMovingPercentagePerformanceCounterBase(PerfCounters.HttpProxyCountersInstance.MovingPercentageNewProxyConnectionCreation);
								if (this.retryOnErrorCounter == 0)
								{
									PerfCounters.HttpProxyCountersInstance.RoutingRetryRateBase.Increment();
								}
								if (this.HttpContext.IsWebSocketRequest)
								{
									if (!this.ProtocolSupportsWebSocket())
									{
										this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, 501, Constants.NotImplementedStatusDescription);
										throw new HttpException(501, Constants.NotImplementedStatusDescription);
									}
									this.Logger.LogCurrentTime("ProxyWebSocketData");
									this.State = ProxyRequestHandler.ProxyState.ProxyWebSocketData;
									this.ProcessWebSocketRequest(this.HttpContext);
								}
								else if (this.ClientRequest.HasBody())
								{
									this.LatencyTracker.StartTracking(LatencyTrackerKey.BackendRequestInitLatency, this.IsRetryingOnError);
									this.ServerRequest.BeginGetRequestStream(new AsyncCallback(ProxyRequestHandler.RequestStreamReadyCallback), this.ServerAsyncState);
									this.Logger.LogCurrentTime("BeginGetRequestStream");
									this.State = ProxyRequestHandler.ProxyState.ProxyRequestData;
								}
								else
								{
									if (this.ClientRequest.ContentLength > 0)
									{
										this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, 501, Constants.NotImplementedStatusDescription);
										throw new HttpException(501, Constants.NotImplementedStatusDescription);
									}
									this.BeginGetServerResponse();
								}
							}
						}
						catch (InvalidOAuthTokenException ex)
						{
							HttpException ex2 = new HttpException((ex.ErrorCategory == 2000007) ? 500 : 401, string.Empty, ex);
							this.CompleteWithError(ex2, "BeginProxyRequest");
						}
						catch (WebException ex3)
						{
							this.CompleteWithError(ex3, "BeginProxyRequest");
						}
						catch (HttpProxyException ex4)
						{
							this.CompleteWithError(ex4, "BeginProxyRequest");
						}
						catch (HttpException ex5)
						{
							this.CompleteWithError(ex5, "BeginProxyRequest");
						}
						catch (IOException ex6)
						{
							this.CompleteWithError(ex6, "BeginProxyRequest");
						}
						catch (SocketException ex7)
						{
							this.CompleteWithError(ex7, "BeginProxyRequest");
						}
					}
				}
				finally
				{
					this.LogElapsedTime("L_BegProxyReq");
				}
			});
		}

		// Token: 0x06000657 RID: 1623 RVA: 0x00022D50 File Offset: 0x00020F50
		protected HttpWebRequest CreateServerRequest(Uri targetUrl)
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(targetUrl);
			if (!HttpProxySettings.UseDefaultWebProxy.Value)
			{
				httpWebRequest.Proxy = NullWebProxy.Instance;
			}
			httpWebRequest.ServicePoint.ConnectionLimit = HttpProxySettings.ServicePointConnectionLimit.Value;
			httpWebRequest.Method = this.ClientRequest.HttpMethod;
			httpWebRequest.Headers["X-FE-ClientIP"] = ClientEndpointResolver.GetClientIP(SharedHttpContextWrapper.GetWrapper(this.HttpContext));
			httpWebRequest.Headers["X-Forwarded-For"] = ClientEndpointResolver.GetClientProxyChainIPs(SharedHttpContextWrapper.GetWrapper(this.HttpContext));
			httpWebRequest.Headers["X-Forwarded-Port"] = ClientEndpointResolver.GetClientPort(SharedHttpContextWrapper.GetWrapper(this.HttpContext));
			httpWebRequest.Headers["X-MS-EdgeIP"] = Utilities.GetEdgeServerIpAsProxyHeader(SharedHttpContextWrapper.GetWrapper(this.HttpContext).Request);
			this.PrepareServerRequest(httpWebRequest);
			this.PfdTracer.TraceRequest("ProxyRequest", httpWebRequest);
			this.PfdTracer.TraceHeaders("ProxyRequest", this.ClientRequest.Headers, httpWebRequest.Headers);
			this.PfdTracer.TraceCookies("ProxyRequest", this.ClientRequest.Cookies, httpWebRequest.CookieContainer);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, Uri>((long)this.GetHashCode(), "[ProxyRequestHandler::CreateServerRequest]: Context {0}; Target address {1}", this.TraceContext, httpWebRequest.Address);
			}
			return httpWebRequest;
		}

		// Token: 0x06000658 RID: 1624 RVA: 0x00003193 File Offset: 0x00001393
		protected virtual bool ShouldCopyCookieToServerRequest(HttpCookie cookie)
		{
			return true;
		}

		// Token: 0x06000659 RID: 1625 RVA: 0x00022EB8 File Offset: 0x000210B8
		protected virtual bool ShouldCopyHeaderToServerRequest(string headerName)
		{
			return !string.Equals(headerName, "X-CommonAccessToken", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, Constants.XIsFromCafe, StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, Constants.XSourceCafeServer, StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, Constants.MsExchProxyUri, StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "X-MSExchangeActivityCtx", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "return-client-request-id", StringComparison.OrdinalIgnoreCase) && !string.Equals(headerName, "X-Forwarded-For", StringComparison.OrdinalIgnoreCase) && (!headerName.StartsWith(Constants.XBackendHeaderPrefix, StringComparison.OrdinalIgnoreCase) || Extensions.IsProbeRequest(Extensions.GetHttpRequestBase(this.ClientRequest)));
		}

		// Token: 0x0600065A RID: 1626 RVA: 0x00022F4A File Offset: 0x0002114A
		protected virtual void AddProtocolSpecificCookiesToServerRequest(CookieContainer cookieContainer)
		{
			throw new InvalidOperationException();
		}

		// Token: 0x0600065B RID: 1627 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void HandleLogoffRequest()
		{
		}

		// Token: 0x0600065C RID: 1628 RVA: 0x00022F51 File Offset: 0x00021151
		protected virtual void ResetForRetryOnError()
		{
			this.haveStartedOutOfBandProxyLogon = false;
			this.haveReceivedAuthChallenge = false;
			this.UseRoutingHintForAnchorMailbox = true;
			this.IsAnchorMailboxFromRoutingHint = false;
			this.HasPreemptivelyCheckedForRoutingHint = false;
			this.AuthBehavior.ResetState();
		}

		// Token: 0x0600065D RID: 1629 RVA: 0x00022F84 File Offset: 0x00021184
		protected TResult ParseClientRequest<TResult>(Func<Stream, TResult> parseMethod, int bufferSize)
		{
			this.LogElapsedTime("E_ParseReq");
			object obj = this.LockObject;
			TResult result;
			lock (obj)
			{
				if (this.bufferedRegionStream == null || !this.IsRetryOnErrorEnabled)
				{
					BufferPool bufferPool = null;
					this.bufferedRegionStream = new BufferedRegionStream(this.ClientRequest.GetBufferlessInputStream());
					if (bufferSize < 512)
					{
						this.bufferedRegionStream.SetBufferedRegion(bufferSize, (int size) => new byte[size], delegate(byte[] buffer)
						{
						});
					}
					else
					{
						this.bufferedRegionStream.SetBufferedRegion(bufferSize, delegate(int size)
						{
							bufferPool = this.GetBufferPool(bufferSize);
							return bufferPool.Acquire();
						}, delegate(byte[] memory)
						{
							bufferPool.Release(memory);
						});
					}
				}
				else
				{
					try
					{
						this.bufferedRegionStream.Position = 0L;
					}
					catch (InvalidOperationException ex)
					{
						this.Logger.AppendGenericError("ParseClientRequest", ex.ToString());
						throw new HttpProxyException(HttpStatusCode.InternalServerError, 5001, "Cannot replay request as bufferedRegionStream cannot be reset.");
					}
				}
				this.ClientRequestStream = this.bufferedRegionStream;
				TResult tresult;
				try
				{
					tresult = parseMethod(this.ClientRequestStream);
				}
				catch (QuotaExceededException)
				{
					throw new HttpProxyException(HttpStatusCode.InternalServerError, 5001, "Cannot parse request as bufferedRegionStream max size is exceeded.");
				}
				catch (CryptographicException)
				{
					throw new HttpProxyException(HttpStatusCode.Unauthorized, 4002, "Cannot decrypt token");
				}
				catch (FormatException)
				{
					throw new HttpProxyException(HttpStatusCode.Unauthorized, 4002, "Cannot parse token from request headers");
				}
				finally
				{
					this.ClientRequestStream.Position = 0L;
					this.LogElapsedTime("L_ParseReq");
				}
				result = tresult;
			}
			return result;
		}

		// Token: 0x0600065E RID: 1630 RVA: 0x000231F8 File Offset: 0x000213F8
		protected void Complete()
		{
			this.LogElapsedTime("E_Complete");
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "ProxyState-Complete", this.State);
			this.LatencyTracker.StartTracking(LatencyTrackerKey.HandlerCompletionLatency, false);
			if (this.ClientResponse != null)
			{
				this.PfdTracer.TraceResponse("ClientResponse", this.ClientResponse);
			}
			try
			{
				this.HttpApplication.CompleteRequest();
			}
			finally
			{
				this.FinalizeRequestHandlerLatencies();
			}
			this.State = ProxyRequestHandler.ProxyState.Completed;
			this.HttpContext.Items[Constants.RequestCompletedHttpContextKeyName] = true;
			ThreadPool.QueueUserWorkItem(new WaitCallback(this.MakeCallback));
			this.LogElapsedTime("L_Complete");
		}

		// Token: 0x0600065F RID: 1631 RVA: 0x000232BC File Offset: 0x000214BC
		protected bool IsAuthenticationChallengeFromBackend(WebException exception)
		{
			return exception.Status == WebExceptionStatus.ProtocolError && exception.Response != null && ((HttpWebResponse)exception.Response).StatusCode == HttpStatusCode.Unauthorized && !this.ShouldBackendRequestBeAnonymous() && !this.haveReceivedAuthChallenge;
		}

		// Token: 0x06000660 RID: 1632 RVA: 0x000232F9 File Offset: 0x000214F9
		protected bool TryFindKerberosChallenge(string authenticationHeader, out bool foundNegotiatePackageName)
		{
			if (KerberosUtilities.TryFindKerberosChallenge(authenticationHeader, this.TraceContext, out this.kerberosChallenge, out foundNegotiatePackageName))
			{
				this.haveReceivedAuthChallenge = true;
				return true;
			}
			return false;
		}

		// Token: 0x06000661 RID: 1633 RVA: 0x0002331A File Offset: 0x0002151A
		protected void LogElapsedTime(string latencyName)
		{
			if (HttpProxySettings.DetailedLatencyTracingEnabled.Value && this.LatencyTracker != null)
			{
				this.LatencyTracker.LogElapsedTime(this.Logger, latencyName);
			}
		}

		// Token: 0x06000662 RID: 1634 RVA: 0x00023342 File Offset: 0x00021542
		protected AnchorMailbox CreateAnchorMailboxFromRoutingHint()
		{
			if (!this.UseRoutingHintForAnchorMailbox)
			{
				return null;
			}
			AnchorMailbox anchorMailbox = AnchorMailboxFactory.TryCreateFromRoutingHint(this, !this.IsRetryingOnError);
			if (anchorMailbox != null)
			{
				this.IsAnchorMailboxFromRoutingHint = true;
			}
			return anchorMailbox;
		}

		// Token: 0x06000663 RID: 1635 RVA: 0x00023368 File Offset: 0x00021568
		protected void ThrowWebExceptionForRetryOnErrorTest(WebResponse webResponse, params int[] checkShouldInvalidateVal)
		{
			if (this.ShouldRetryOnError)
			{
				int num = -1;
				if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
				{
					ExTraceGlobals.FaultInjectionTracer.TraceTest<int>(2256940349U, ref num);
				}
				if (num != -1 && (checkShouldInvalidateVal == null || checkShouldInvalidateVal.Length == 0 || Array.IndexOf<int>(checkShouldInvalidateVal, num) >= 0))
				{
					webResponse.Headers[Constants.BEServerExceptionHeaderName] = Constants.IllegalCrossServerConnectionExceptionType;
					if (this.retryOnErrorCounter == 1)
					{
						webResponse.Headers["X-DBMountedOnServer"] = string.Format("{0}~{1}~{2}", default(Guid), ComputerInformation.DnsFullyQualifiedDomainName, Server.E15MinVersion);
					}
					string empty = string.Empty;
					if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
					{
						ExTraceGlobals.FaultInjectionTracer.TraceTest<string>(3330682173U, ref empty);
					}
					if (!string.IsNullOrEmpty(empty) && !empty.StartsWith("BEAuth"))
					{
						bool flag = false;
						if (string.Equals(empty, "Unauthorized", StringComparison.OrdinalIgnoreCase))
						{
							flag = true;
							HttpWebResponse obj = (HttpWebResponse)webResponse;
							typeof(HttpWebResponse).GetField("m_StatusCode", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, HttpStatusCode.Unauthorized);
							typeof(DefaultAuthBehavior).GetProperty("AuthState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty).SetValue(this.AuthBehavior, AuthState.BackEndFullAuth);
						}
						WebExceptionStatus status;
						if (string.Equals(empty, "Retryable", StringComparison.OrdinalIgnoreCase))
						{
							status = WebExceptionStatus.KeepAliveFailure;
						}
						else if (string.Equals(empty, "NonRetryable", StringComparison.OrdinalIgnoreCase))
						{
							status = WebExceptionStatus.Timeout;
						}
						else if (flag)
						{
							status = WebExceptionStatus.ProtocolError;
						}
						else
						{
							status = WebExceptionStatus.UnknownError;
						}
						throw new WebException(string.Format("Fault injection at ThrowWebExceptionForRetryOnErrorTest. retryOnErrorCounter:{0}, shouldInvalidateVal:{1}, throwWebException:{2}", this.retryOnErrorCounter, num, empty), null, status, webResponse);
					}
				}
			}
		}

		// Token: 0x06000664 RID: 1636 RVA: 0x00023514 File Offset: 0x00021714
		private static void ResponseReadyCallback(IAsyncResult result)
		{
			ProxyRequestHandler proxyRequestHandler = AsyncStateHolder.Unwrap<ProxyRequestHandler>(result);
			if (result.CompletedSynchronously)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(proxyRequestHandler.OnResponseReady), result);
				return;
			}
			proxyRequestHandler.OnResponseReady(result);
		}

		// Token: 0x06000665 RID: 1637 RVA: 0x0002354C File Offset: 0x0002174C
		private static void RequestStreamReadyCallback(IAsyncResult result)
		{
			ProxyRequestHandler proxyRequestHandler = AsyncStateHolder.Unwrap<ProxyRequestHandler>(result);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)proxyRequestHandler.GetHashCode(), "[ProxyRequestHandler::RequestStreamReadyCallback]: Context {0}", proxyRequestHandler.TraceContext);
			}
			if (result.CompletedSynchronously)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(proxyRequestHandler.OnRequestStreamReady), result);
				return;
			}
			proxyRequestHandler.OnRequestStreamReady(result);
		}

		// Token: 0x06000666 RID: 1638 RVA: 0x000235AC File Offset: 0x000217AC
		private static void DisposeIfNotNullAndCatchExceptions(IDisposable objectToDispose)
		{
			if (objectToDispose == null)
			{
				return;
			}
			try
			{
				objectToDispose.Dispose();
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x06000667 RID: 1639 RVA: 0x000235DC File Offset: 0x000217DC
		private IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
		{
			this.Logger.AppendGenericInfo(Constants.NewTcpConnectionLogKey, remoteEndPoint.Address + '&' + retryCount);
			PerfCounters.UpdateMovingPercentagePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingPercentageNewProxyConnectionCreation);
			if (retryCount > HttpProxySettings.BindIpEndpointCallbackMaxRetryCount.Value)
			{
				this.Logger.AppendGenericError("ConnectError", "BindIPEndPointCallback called too many times");
				return null;
			}
			return new IPEndPoint((remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6) ? IPAddress.IPv6Any : IPAddress.Any, 0);
		}

		// Token: 0x06000668 RID: 1640 RVA: 0x00023660 File Offset: 0x00021860
		private BufferPool GetBufferPool(int bufferSize)
		{
			BufferPoolCollection.BufferSize bufferSize2;
			if (!BufferPoolCollection.AutoCleanupCollection.TryMatchBufferSize(bufferSize, ref bufferSize2))
			{
				throw new InvalidOperationException("Could not get buffer size for BufferedRegionStream buffer.");
			}
			return BufferPoolCollection.AutoCleanupCollection.Acquire(bufferSize2);
		}

		// Token: 0x06000669 RID: 1641 RVA: 0x00023694 File Offset: 0x00021894
		private void FinalizeRequestHandlerLatencies()
		{
			this.UpdateRoutingFailurePerfCounter(false);
			if (this.retryOnErrorCounter > 0)
			{
				PerfCounters.HttpProxyCountersInstance.RoutingRetryRate.Increment();
				PerfCounters.HttpProxyCountersInstance.RoutingRetryFailureRateBase.Increment();
				if (this.retryOnErrorCounter >= HttpProxySettings.MaxRetryOnError.Value)
				{
					PerfCounters.HttpProxyCountersInstance.RoutingRetryFailureRate.Increment();
				}
			}
			long currentLatency = this.LatencyTracker.GetCurrentLatency(LatencyTrackerKey.BackendResponseInitLatency);
			if (currentLatency >= 0L)
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 15, currentLatency);
			}
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 26, this.LatencyTracker.GlsLatencyBreakup);
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 27, this.LatencyTracker.TotalGlsLatency);
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 28, this.LatencyTracker.AccountForestLatencyBreakup);
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 29, this.LatencyTracker.TotalAccountForestDirectoryLatency);
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 30, this.LatencyTracker.ResourceForestLatencyBreakup);
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 31, this.LatencyTracker.TotalResourceForestDirectoryLatency);
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 1, this.LatencyTracker.AdLatency);
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 32, this.LatencyTracker.SharedCacheLatencyBreakup);
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 33, this.LatencyTracker.TotalSharedCacheLatency);
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, long, string>((long)this.GetHashCode(), "[ProxyRequestHandler::Complete]: Context {0}; ElapsedTime {1}; StatusCode {2}", this.TraceContext, this.LatencyTracker.GetCurrentLatency(LatencyTrackerKey.RequestHandlerLatency), (this.asyncException != null) ? this.asyncException.ToString() : this.ClientResponse.StatusCode.ToString(CultureInfo.InvariantCulture));
			}
		}

		// Token: 0x0600066A RID: 1642 RVA: 0x00023880 File Offset: 0x00021A80
		private bool ShouldSetRequestTimeout(out TimeSpan timeout)
		{
			timeout = TimeSpan.Zero;
			string text = this.ClientRequest.Headers[Constants.FrontEndToBackEndTimeout];
			if (string.IsNullOrEmpty(text))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::ShouldSetRequestTimeout]: Timeout header not passed in the client request.", this.TraceContext);
				}
				return false;
			}
			if (!Extensions.IsProbeRequest(Extensions.GetHttpRequestBase(this.ClientRequest)))
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string, int>((long)this.GetHashCode(), "[ProxyRequestHandler::ShouldSetRequestTimeout]: Not a monitoring request. Timeout won't be set. UserAgent: {0}", this.ClientRequest.UserAgent, this.TraceContext);
				}
				return false;
			}
			int num = -1;
			if (!int.TryParse(text, out num) || num < 0 || num > 300)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string, string, int>((long)this.GetHashCode(), "[ProxyRequestHandler::ShouldSetRequestTimeout]: Invalid value used on the {0} header. Value: {1}", Constants.FrontEndToBackEndTimeout, text, this.TraceContext);
				}
				return false;
			}
			timeout = TimeSpan.FromSeconds((double)num);
			return true;
		}

		// Token: 0x0600066B RID: 1643 RVA: 0x00023980 File Offset: 0x00021B80
		private void SetupRequestTimeout(HttpWebRequest serverRequest, TimeSpan timeout)
		{
			this.requestState = new RequestState(new TimerCallback(this.RequestTimeoutCallback), serverRequest, (int)timeout.TotalMilliseconds);
		}

		// Token: 0x0600066C RID: 1644 RVA: 0x000239A4 File Offset: 0x00021BA4
		private void RequestTimeoutCallback(object asyncObj)
		{
			HttpWebRequest httpWebRequest = asyncObj as HttpWebRequest;
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, int>((long)this.GetHashCode(), "[ProxyRequestHandler::RequestTimeoutCallback]: Request timed out. Request state: {0}", this.requestState.State, this.TraceContext);
			}
			if (this.requestState.TryTransitionFromExecutingToTimedOut())
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::RequestTimeoutCallback]: Calling request.Abort()", this.TraceContext);
				}
				httpWebRequest.Abort();
			}
		}

		// Token: 0x0600066D RID: 1645 RVA: 0x00023A28 File Offset: 0x00021C28
		private bool ShouldCopyProxyResponseHeader(string headerName)
		{
			return !WebHeaderCollection.IsRestricted(headerName) && !ProxyRequestHandler.RestrictedHeaders.Contains(headerName);
		}

		// Token: 0x0600066E RID: 1646 RVA: 0x00023A44 File Offset: 0x00021C44
		private void CopyOrCreateNewXGccProxyInfoHeader(HttpWebRequest toRequest)
		{
			this.LogElapsedTime("E_XGcc");
			try
			{
				string value;
				if ((GccUtils.TryGetGccProxyInfo(SharedHttpContextWrapper.GetWrapper(this.HttpContext), ref value) || GccUtils.TryCreateGccProxyInfo(SharedHttpContextWrapper.GetWrapper(this.HttpContext), ref value)) && !string.IsNullOrEmpty(value))
				{
					toRequest.Headers.Add("X-GCC-PROXYINFO", value);
				}
			}
			finally
			{
				this.LogElapsedTime("L_XGcc");
			}
		}

		// Token: 0x0600066F RID: 1647 RVA: 0x00023ABC File Offset: 0x00021CBC
		private void SetException(Exception e)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, Exception>((long)this.GetHashCode(), "[ProxyRequestHandler::SetException]: Context {0}; Exception {1}", this.TraceContext, e);
			}
			if (this.asyncException == null)
			{
				this.asyncException = e;
			}
		}

		// Token: 0x06000670 RID: 1648 RVA: 0x00023AF7 File Offset: 0x00021CF7
		private void MakeCallback(object extraData)
		{
			this.asyncCallback(this);
		}

		// Token: 0x06000671 RID: 1649 RVA: 0x00023B08 File Offset: 0x00021D08
		private void OnResponseReady(object extraData)
		{
			this.CallThreadEntranceMethod(delegate
			{
				this.LogElapsedTime("E_OnRespReady");
				try
				{
					this.LatencyTracker.LogElapsedTimeAsLatency(this.Logger, LatencyTrackerKey.BackendProcessingLatency, 37);
					IAsyncResult asyncResult = extraData as IAsyncResult;
					object obj = this.LockObject;
					lock (obj)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::OnResponseReady]: Context {0}", this.TraceContext);
						}
						if (this.requestState != null)
						{
							this.requestState.TryTransitionFromExecutingToFinished();
						}
						this.ServerRequest.Headers.Clear();
						try
						{
							this.Logger.LogCurrentTime("OnResponseReady");
							GuardedProxyExecution.Default.Decrement(this.AnchoredRoutingTarget.BackEndServer);
							WebResponse webResponse = this.ServerRequest.EndGetResponse(asyncResult);
							this.ThrowWebExceptionForRetryOnErrorTest(webResponse, Array.Empty<int>());
							this.Logger.LogCurrentTime("EndGetResponse");
							if (this.IsRetryOnErrorEnabled)
							{
								ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ServerResponse);
								this.ServerResponse = null;
							}
							this.ServerResponse = (HttpWebResponse)webResponse;
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 25, (long)this.ServerResponse.StatusCode);
							RequestDetailsLogger.PublishBackendDataToLog(this.Logger, this.ServerResponse);
						}
						catch (WebException ex)
						{
							this.Logger.LogCurrentTime("EndGetResponse");
							HttpStatusCode httpStatusCode = HttpStatusCode.OK;
							if (ex.Response != null)
							{
								httpStatusCode = ((HttpWebResponse)ex.Response).StatusCode;
								RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 25, (long)httpStatusCode);
							}
							else
							{
								RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 25, ex.Status);
							}
							if (this.IsAuthenticationChallengeFromBackend(ex))
							{
								bool flag2 = false;
								string text = ex.Response.Headers[Constants.AuthenticationHeader];
								if (this.TryFindKerberosChallenge(text, out flag2))
								{
									if (this.AuthBehavior.AuthState != AuthState.BackEndFullAuth)
									{
										ThreadPool.QueueUserWorkItem(new WaitCallback(this.BeginProxyRequest));
										this.State = ProxyRequestHandler.ProxyState.PrepareServerRequest;
										return;
									}
								}
								else if ((HttpProxyGlobals.ProtocolType == 4 || HttpProxyGlobals.ProtocolType == 5 || HttpProxyGlobals.ProtocolType == 1) && this.ProxyToDownLevel && !flag2)
								{
									NameValueCollection nameValueCollection = new NameValueCollection();
									nameValueCollection.Add("CafeError", ErrorFE.FEErrorCodes.CAS14WithNoWIA.ToString());
									throw new HttpException(302, AspNetHelper.GetCafeErrorPageRedirectUrl(this.HttpContext, nameValueCollection));
								}
								if (!string.IsNullOrEmpty(text) && text.Contains("Bearer"))
								{
									string text2 = ex.Response.Headers[MSDiagnosticsHeader.HeaderNameFromBackend];
									if (!string.IsNullOrEmpty(text2))
									{
										this.ClientResponse.AppendHeader(Constants.AuthenticationHeader, ConfigProvider.Instance.Configuration.ChallengeResponseString);
										this.HandleMSDiagnosticsHeader(text2);
									}
								}
							}
							if (this.ImplementsOutOfBandProxyLogon && !this.haveStartedOutOfBandProxyLogon && ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null && httpStatusCode == this.StatusCodeSignifyingOutOfBandProxyLogonNeeded)
							{
								this.haveStartedOutOfBandProxyLogon = true;
								this.StartOutOfBandProxyLogon(null);
								return;
							}
							if (this.TryHandleProtocolSpecificResponseErrors(ex))
							{
								return;
							}
							this.CompleteWithError(ex, "HandleResponseError");
							return;
						}
						this.ProcessResponse(null);
					}
				}
				finally
				{
					this.LogElapsedTime("L_OnRespReady");
				}
			});
		}

		// Token: 0x06000672 RID: 1650 RVA: 0x00023B3C File Offset: 0x00021D3C
		private void OnRequestStreamReady(object extraData)
		{
			this.CallThreadEntranceMethod(delegate
			{
				this.LogElapsedTime("E_ReqStreamReady");
				try
				{
					IAsyncResult asyncResult = extraData as IAsyncResult;
					object obj = this.LockObject;
					lock (obj)
					{
						try
						{
							this.Logger.LogCurrentTime("OnRequestStreamReady");
							if (this.IsRetryOnErrorEnabled)
							{
								ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ServerRequestStream);
								this.ServerRequestStream = null;
							}
							this.ServerRequestStream = this.ServerRequest.EndGetRequestStream(asyncResult);
							this.LatencyTracker.LogElapsedTimeAsLatency(this.Logger, LatencyTrackerKey.BackendRequestInitLatency, 35);
							if (this.ClientRequestStream == null)
							{
								this.ClientRequestStream = this.ClientRequest.GetBufferlessInputStream();
							}
							this.BeginRequestStreaming();
						}
						catch (WebException ex)
						{
							if (ex.Response != null)
							{
								RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 25, (long)((HttpWebResponse)ex.Response).StatusCode);
							}
							else
							{
								RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 25, ex.Status);
							}
							this.CompleteWithError(ex, "OnRequestStreamReady");
						}
						catch (HttpException ex2)
						{
							this.CompleteWithError(ex2, "OnRequestStreamReady");
						}
						catch (HttpProxyException ex3)
						{
							this.CompleteWithError(ex3, "OnRequestStreamReady");
						}
						catch (IOException ex4)
						{
							this.CompleteWithError(ex4, "OnRequestStreamReady");
						}
					}
				}
				finally
				{
					this.LogElapsedTime("L_ReqStreamReady");
				}
			});
		}

		// Token: 0x06000673 RID: 1651 RVA: 0x00023B70 File Offset: 0x00021D70
		private void BeginGetServerResponse()
		{
			this.LogElapsedTime("E_BegGetSvrResp");
			try
			{
				GuardedProxyExecution.Default.Increment(this.AnchoredRoutingTarget.BackEndServer, Extensions.GetGenericInfoLogDelegate(this.Logger));
				this.LatencyTracker.StartTracking(LatencyTrackerKey.BackendResponseInitLatency, this.IsRetryingOnError);
				this.ServerRequest.BeginGetResponse(new AsyncCallback(ProxyRequestHandler.ResponseReadyCallback), this.ServerAsyncState);
				this.Logger.LogCurrentTime("BeginGetResponse");
				this.State = ProxyRequestHandler.ProxyState.WaitForServerResponse;
			}
			catch
			{
				GuardedProxyExecution.Default.Decrement(this.AnchoredRoutingTarget.BackEndServer);
				throw;
			}
			finally
			{
				this.LogElapsedTime("L_BegGetSvrResp");
				this.LatencyTracker.LogElapsedTimeAsLatency(this.Logger, LatencyTrackerKey.BackendResponseInitLatency, 38);
				this.LatencyTracker.StartTracking(LatencyTrackerKey.BackendProcessingLatency, this.IsRetryingOnError);
			}
		}

		// Token: 0x06000674 RID: 1652 RVA: 0x00023C5C File Offset: 0x00021E5C
		private void CopyHeadersToServerRequest(HttpWebRequest destination)
		{
			this.LogElapsedTime("E_CopyHeadersSvrReq");
			NameValueCollection headers = this.ClientRequest.Headers;
			foreach (object obj in headers)
			{
				string text = (string)obj;
				try
				{
					string text2 = text.ToUpperInvariant();
					uint num = <PrivateImplementationDetails>.ComputeStringHash(text2);
					if (num <= 1030842853U)
					{
						if (num <= 453382463U)
						{
							if (num <= 169929350U)
							{
								if (num != 40228184U)
								{
									if (num == 169929350U)
									{
										if (text2 == "PROXY-CONNECTION")
										{
											continue;
										}
									}
								}
								else if (text2 == "EXPECT")
								{
									continue;
								}
							}
							else if (num != 247469449U)
							{
								if (num == 453382463U)
								{
									if (text2 == "COOKIE")
									{
										continue;
									}
								}
							}
							else if (text2 == "IF-MODIFIED-SINCE")
							{
								HttpWebHelper.SetIfModifiedSince(destination, headers[text]);
								continue;
							}
						}
						else if (num <= 635591758U)
						{
							if (num != 547231721U)
							{
								if (num == 635591758U)
								{
									if (text2 == "USER-AGENT")
									{
										destination.UserAgent = headers[text];
										continue;
									}
								}
							}
							else if (text2 == "ACCEPT")
							{
								destination.Accept = headers[text];
								continue;
							}
						}
						else if (num != 707182617U)
						{
							if (num == 1030842853U)
							{
								if (text2 == "X-EXCOMPID")
								{
									continue;
								}
							}
						}
						else if (text2 == "CONNECTION")
						{
							HttpWebHelper.SetConnectionHeader(destination, headers[text]);
							continue;
						}
					}
					else if (num <= 2982521275U)
					{
						if (num <= 2630822013U)
						{
							if (num != 1696719282U)
							{
								if (num == 2630822013U)
								{
									if (text2 == "CONTENT-LENGTH")
									{
										if (!this.WillContentBeChangedDuringStreaming)
										{
											destination.ContentLength = long.Parse(headers[text], CultureInfo.InvariantCulture);
											continue;
										}
										continue;
									}
								}
							}
							else if (text2 == "RANGE")
							{
								HttpWebHelper.SetRange(destination, headers[text]);
								continue;
							}
						}
						else if (num != 2976646412U)
						{
							if (num == 2982521275U)
							{
								if (text2 == "PROXY-AUTHORIZATION")
								{
									continue;
								}
							}
						}
						else if (text2 == "TRANSFER-ENCODING")
						{
							string text3 = headers[text];
							if (text3 != null && this.ClientRequest.CanHaveBody() && text3.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) >= 0)
							{
								destination.SendChunked = true;
								continue;
							}
							continue;
						}
					}
					else if (num <= 3161469017U)
					{
						if (num != 3118819654U)
						{
							if (num == 3161469017U)
							{
								if (text2 == "ACCEPT-ENCODING")
								{
									continue;
								}
							}
						}
						else if (text2 == "REFERER")
						{
							destination.Referer = headers[text];
							continue;
						}
					}
					else if (num != 3945365109U)
					{
						if (num != 3991944751U)
						{
							if (num == 4194732382U)
							{
								if (text2 == "AUTHORIZATION")
								{
									if (this.AuthBehavior.AuthState == AuthState.BackEndFullAuth || this.ProxyKerberosAuthentication)
									{
										destination.Headers.Add(text, headers[text]);
										continue;
									}
									continue;
								}
							}
						}
						else if (text2 == "HOST")
						{
							if (HttpProxySettings.AddHostHeaderInServerRequestEnabled.Value)
							{
								destination.Host = headers[text];
								continue;
							}
							continue;
						}
					}
					else if (text2 == "CONTENT-TYPE")
					{
						destination.ContentType = headers[text];
						continue;
					}
					if (!WebHeaderCollection.IsRestricted(text) && this.ShouldCopyHeaderToServerRequest(text))
					{
						destination.Headers.Add(text, headers[text]);
					}
				}
				catch (ArgumentException innerException)
				{
					throw new HttpException(400, "Invalid HTTP header: " + text, innerException);
				}
			}
			if (this.ShouldSendFullActivityScope)
			{
				this.Logger.ActivityScope.SerializeTo(destination);
			}
			else
			{
				this.Logger.ActivityScope.SerializeMinimalTo(destination);
			}
			this.LogElapsedTime("L_CopyHeadersSvrReq");
		}

		// Token: 0x06000675 RID: 1653 RVA: 0x00024130 File Offset: 0x00022330
		private void CopyCookiesToClientResponse()
		{
			this.LogElapsedTime("E_CopyCookiesClientResp");
			foreach (object obj in this.ServerResponse.Cookies)
			{
				Cookie cookie = (Cookie)obj;
				if (cookie.Name.Equals("CopyLiveIdAuthCookieFromBE", StringComparison.OrdinalIgnoreCase))
				{
					LiveIdAuthenticationModule.ProcessFrontEndLiveIdAuthCookie(this.HttpContext, cookie.Value);
				}
				else if (this.ShouldCopyCookieToClientResponse(cookie))
				{
					this.CopyServerCookieToClientResponse(cookie);
				}
			}
			if (this.WillAddProtocolSpecificCookiesToClientResponse)
			{
				this.CopySupplementalCookiesToClientResponse();
			}
			this.LogElapsedTime("L_CopyCookiesClientResp");
		}

		// Token: 0x06000676 RID: 1654 RVA: 0x000241E4 File Offset: 0x000223E4
		private void SetResponseStatusIfHeadersUnsent(HttpResponse response, int status)
		{
			if (!this.ResponseHeadersSent)
			{
				response.StatusCode = status;
			}
		}

		// Token: 0x06000677 RID: 1655 RVA: 0x000241F5 File Offset: 0x000223F5
		private void SetResponseStatusIfHeadersUnsent(HttpResponse response, int status, string description)
		{
			if (!this.ResponseHeadersSent)
			{
				response.StatusCode = status;
				response.StatusDescription = description;
			}
		}

		// Token: 0x06000678 RID: 1656 RVA: 0x0002420D File Offset: 0x0002240D
		private void SetResponseHeaderIfHeadersUnsent(HttpResponse response, string name, string value)
		{
			if (!this.ResponseHeadersSent)
			{
				response.Headers[name] = value;
			}
		}

		// Token: 0x06000679 RID: 1657 RVA: 0x00024224 File Offset: 0x00022424
		private void CopyHeadersToClientResponse()
		{
			this.LogElapsedTime("E_CopyHeadersClientResp");
			foreach (object obj in this.ServerResponse.Headers)
			{
				string text = (string)obj;
				string a = text.ToUpperInvariant();
				if (!(a == "CONTENT-LENGTH"))
				{
					if (!(a == "CONTENT-TYPE"))
					{
						if (!(a == "CACHE-CONTROL"))
						{
							if (!(a == "X-FROMBACKEND-CLIENTCONNECTION"))
							{
								if (!(a == "X-MS-DIAGNOSTICS-FROM-BACKEND"))
								{
									if (!(a == "WWW-AUTHENTICATE"))
									{
										if (this.ShouldCopyProxyResponseHeader(text))
										{
											this.ClientResponse.Headers.Add(text, this.ServerResponse.Headers[text]);
										}
									}
									else if (this.ProxyKerberosAuthentication)
									{
										foreach (string text2 in this.ServerResponse.Headers[text].Split(new char[]
										{
											','
										}))
										{
											if (text2.TrimStart(Array.Empty<char>()).StartsWith(Constants.KerberosPackageValue, StringComparison.OrdinalIgnoreCase))
											{
												this.ClientResponse.Headers.Add(text, text2.Trim());
												break;
											}
										}
									}
									else if (this.AuthBehavior.ShouldCopyAuthenticationHeaderToClientResponse)
									{
										this.ClientResponse.Headers.Add(text, this.ServerResponse.Headers[text]);
									}
								}
								else
								{
									string diagnostics = this.ServerResponse.Headers[MSDiagnosticsHeader.HeaderNameFromBackend];
									this.HandleMSDiagnosticsHeader(diagnostics);
								}
							}
							else
							{
								this.ClientResponse.Headers.Add(HttpRequestHeader.Connection.ToString(), this.ServerResponse.Headers[text]);
							}
						}
						else
						{
							AspNetHelper.SetCacheability(this.ClientResponse, this.ServerResponse.Headers[text]);
						}
					}
					else
					{
						this.ClientResponse.ContentType = this.ServerResponse.ContentType;
					}
				}
				else if (this.ClientRequest.GetHttpMethod() == HttpMethod.Head)
				{
					this.ClientResponse.Headers[text] = this.ServerResponse.Headers[text];
				}
			}
			this.LogElapsedTime("L_CopyHeadersClientResp");
		}

		// Token: 0x0600067A RID: 1658 RVA: 0x000244B0 File Offset: 0x000226B0
		private void CopyCookiesToServerRequest(HttpWebRequest serverRequest)
		{
			this.LogElapsedTime("E_CopyCookiesSvrReq");
			if (serverRequest.CookieContainer == null)
			{
				serverRequest.CookieContainer = new CookieContainer();
			}
			serverRequest.CookieContainer.PerDomainCapacity = int.MaxValue;
			for (int i = 0; i < this.ClientRequest.Cookies.Count; i++)
			{
				HttpCookie httpCookie = this.ClientRequest.Cookies[i];
				if (this.ShouldCopyCookieToServerRequest(httpCookie))
				{
					try
					{
						Cookie cookie = new Cookie();
						cookie.Name = httpCookie.Name;
						cookie.Value = httpCookie.Value;
						cookie.Path = httpCookie.Path;
						cookie.Expires = httpCookie.Expires;
						cookie.HttpOnly = httpCookie.HttpOnly;
						cookie.Secure = httpCookie.Secure;
						if (HttpProxySettings.AddHostHeaderInServerRequestEnabled.Value)
						{
							cookie.Domain = (string.IsNullOrEmpty(httpCookie.Domain) ? serverRequest.Host : httpCookie.Domain);
						}
						else
						{
							cookie.Domain = serverRequest.Address.Host;
						}
						serverRequest.CookieContainer.Add(cookie);
					}
					catch (CookieException)
					{
					}
				}
			}
			if (this.WillAddProtocolSpecificCookiesToServerRequest)
			{
				this.AddProtocolSpecificCookiesToServerRequest(serverRequest.CookieContainer);
			}
			this.LogElapsedTime("L_CopyCookiesSvrReq");
		}

		// Token: 0x0600067B RID: 1659 RVA: 0x000245FC File Offset: 0x000227FC
		private void ProcessResponse(WebException exception)
		{
			this.LogElapsedTime("E_ProcResp");
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int, ProxyRequestHandler.ProxyState>((long)this.GetHashCode(), "[ProxyRequestHandler::ProcessResponse]: Context {0}; State {1}", this.TraceContext, this.State);
			}
			if (this.ServerResponse == null)
			{
				if (this.ShouldRetryOnError)
				{
					this.RemoveNotNeededHttpContextContent();
					ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ClientRequestStream);
					this.ClientRequestStream = null;
				}
				this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, 503, "Unable to reach destination");
				this.Complete();
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::ProcessResponse]: Context {0}; NULL ServerResponse. Returning 503", this.TraceContext);
					return;
				}
			}
			else
			{
				this.PfdTracer.TraceResponse("ProxyResponse", this.ServerResponse);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, HttpStatusCode>((long)this.GetHashCode(), "[ProxyRequestHandler::ProcessResponse]: Context {0}; ServerResponse.StatusCode {1}", this.TraceContext, this.ServerResponse.StatusCode);
				}
				bool flag = this.ProcessRoutingUpdateModuleResponse(this.ServerResponse);
				if (this.HandleRoutingError(this.ServerResponse, !flag))
				{
					if (this.RecalculateTargetBackend())
					{
						return;
					}
				}
				else if (this.ShouldRetryOnError)
				{
					this.RemoveNotNeededHttpContextContent();
					ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ClientRequestStream);
					this.ClientRequestStream = null;
				}
				int statusCode = (int)this.ServerResponse.StatusCode;
				this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, statusCode, Utilities.GetTruncatedString(this.ServerResponse.StatusDescription, 512));
				if (exception != null)
				{
					this.SetRequestFailureContext(2, statusCode, statusCode.ToString(), exception.ToString(), null, new WebExceptionStatus?(exception.Status));
				}
				this.CopyHeadersToClientResponse();
				this.CopyCookiesToClientResponse();
				this.HandleLogoffRequest();
				this.ClientResponse.ContentType = this.ServerResponse.ContentType;
				this.PfdTracer.TraceHeaders("ProxyResponse", this.ServerResponse.Headers, this.ClientResponse.Headers);
				this.PfdTracer.TraceCookies("ProxyResponse", this.ServerResponse.Cookies, this.ClientResponse.Cookies);
				this.CleanUpRequestStreamsAndBuffer();
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.authenticationContext);
				this.authenticationContext = null;
				this.kerberosChallenge = null;
				this.BeginResponseStreaming();
				this.LogElapsedTime("L_ProcResp");
			}
		}

		// Token: 0x0600067C RID: 1660 RVA: 0x00024844 File Offset: 0x00022A44
		private void LogForRetry(string key, bool logLatency, params HttpProxyMetadata[] preservedMetadataEntries)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string format = "{0}:{1} ";
			if (logLatency)
			{
				stringBuilder.AppendFormat(format, "TotalRequestTime", this.LatencyTracker.GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency));
				stringBuilder.AppendFormat(format, "Delay", this.delayOnRetryOnError);
				stringBuilder.AppendFormat(format, "State", this.State);
			}
			foreach (HttpProxyMetadata httpProxyMetadata in preservedMetadataEntries)
			{
				stringBuilder.AppendFormat(format, httpProxyMetadata, this.Logger.Get(httpProxyMetadata) ?? string.Empty);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, httpProxyMetadata, string.Empty);
			}
			this.Logger.AppendGenericError(key, stringBuilder.ToString());
		}

		// Token: 0x0600067D RID: 1661 RVA: 0x00024918 File Offset: 0x00022B18
		private void RemoveNotNeededHttpContextContent()
		{
			if (this.ClientRequest.IsAuthenticated)
			{
				if (!this.ProxyToDownLevel)
				{
					IIdentity identity = this.HttpContext.User.Identity;
					WindowsIdentity windowsIdentity = identity as WindowsIdentity;
					if (windowsIdentity != null)
					{
						this.HttpContext.User = new GenericPrincipal(new GenericIdentity(IIdentityExtensions.GetSafeName(identity, true), identity.AuthenticationType), null);
						windowsIdentity.Dispose();
					}
				}
				this.HttpContext.Items["Item-CommonAccessToken"] = null;
			}
		}

		// Token: 0x0600067E RID: 1662 RVA: 0x00024994 File Offset: 0x00022B94
		private void HandleMSDiagnosticsHeader(string diagnostics)
		{
			if (string.IsNullOrEmpty(diagnostics))
			{
				return;
			}
			if (this.HttpContext == null || this.HttpContext.User == null)
			{
				return;
			}
			if (this.HttpContext.User.Identity is OAuthIdentity)
			{
				OAuthErrors oauthErrors;
				string text;
				if (MSDiagnosticsHeader.TryParseHeaderFromBackend(diagnostics, ref oauthErrors, ref text))
				{
					OAuthErrorCategory errorCategory = OAuthErrorsUtil.GetErrorCategory(oauthErrors);
					this.HttpContext.Items["OAuthError"] = text;
					this.HttpContext.Items["OAuthErrorCategory"] = errorCategory + "-BE";
					string str = this.HttpContext.Items["OAuthExtraInfo"] as string;
					this.HttpContext.Items["OAuthExtraInfo"] = str + string.Format("ErrorCode:{0}", oauthErrors);
					MSDiagnosticsHeader.AppendToResponse(errorCategory, text, this.ClientResponse);
					return;
				}
				this.Logger.AppendAuthError("OAuth", diagnostics);
			}
		}

		// Token: 0x0600067F RID: 1663 RVA: 0x00024A90 File Offset: 0x00022C90
		private void AlterAuthBehaviorStateForBEAuthTest()
		{
			string empty = string.Empty;
			if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
			{
				ExTraceGlobals.FaultInjectionTracer.TraceTest<string>(3330682173U, ref empty);
			}
			if (!string.IsNullOrEmpty(empty) && empty.StartsWith("BEAuth"))
			{
				PropertyInfo property = typeof(DefaultAuthBehavior).GetProperty("AuthState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
				if (empty.Contains("FrontEndContinueAuth-FrontEndContinueAuth"))
				{
					property.SetValue(this.AuthBehavior, AuthState.FrontEndContinueAuth);
					return;
				}
				if (empty.Contains("BackEndFullAuth-BackEndFullAuth"))
				{
					property.SetValue(this.AuthBehavior, AuthState.BackEndFullAuth);
					return;
				}
				if (empty.Contains("FrontEndContinueAuth-BackEndFullAuth"))
				{
					if (!this.IsRetryingOnError)
					{
						property.SetValue(this.AuthBehavior, AuthState.FrontEndContinueAuth);
						return;
					}
					property.SetValue(this.AuthBehavior, AuthState.BackEndFullAuth);
					return;
				}
				else if (empty.Contains("BackEndFullAuth-FrontEndContinueAuth"))
				{
					if (!this.IsRetryingOnError)
					{
						property.SetValue(this.AuthBehavior, AuthState.BackEndFullAuth);
						return;
					}
					property.SetValue(this.AuthBehavior, AuthState.FrontEndContinueAuth);
				}
			}
		}

		// Token: 0x06000680 RID: 1664 RVA: 0x00024BAC File Offset: 0x00022DAC
		private void AddRoutingEntryHeaderToRequest(HttpWebRequest serverRequest)
		{
			string text = string.Empty;
			IRoutingEntry routingEntry = this.AnchoredRoutingTarget.AnchorMailbox.GetRoutingEntry();
			if (routingEntry != null)
			{
				text = RoutingEntryHeaderSerializer.Serialize(routingEntry);
			}
			if (this.databaseToServerRoutingEntry != null)
			{
				if (!string.IsNullOrEmpty(text))
				{
					text += ",";
				}
				text += RoutingEntryHeaderSerializer.Serialize(this.databaseToServerRoutingEntry);
			}
			if (!string.IsNullOrEmpty(text))
			{
				serverRequest.Headers["X-LegacyRoutingEntry"] = text;
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "RoutingEntry", text);
			}
		}

		// Token: 0x06000681 RID: 1665 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void ExposeExceptionToClientResponse(Exception ex)
		{
		}

		// Token: 0x06000682 RID: 1666 RVA: 0x00003193 File Offset: 0x00001393
		protected virtual bool ShouldLogClientDisconnectError(Exception ex)
		{
			return true;
		}

		// Token: 0x06000683 RID: 1667 RVA: 0x00024C38 File Offset: 0x00022E38
		protected void CompleteWithError(Exception ex, string label)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
			{
				ExTraceGlobals.VerboseTracer.TraceError<int, string, string>((long)this.GetHashCode(), "[ProxyRequestHandler::CompleteWithError]: Context {0}; Label {1}; Exception: {2}", this.TraceContext, label, ex.ToString());
			}
			this.ExposeExceptionToClientResponse(ex);
			if (ex is MaxConcurrencyReachedException)
			{
				ex = new HttpProxyException(HttpStatusCode.ServiceUnavailable, ConcurrencyExceptionErrorCodeMapping.GetSubErrorCode(ex as MaxConcurrencyReachedException), ((MaxConcurrencyReachedException)ex).Message, ex);
			}
			if (ex is WebException)
			{
				if (!this.HandleWebException(ex as WebException))
				{
					return;
				}
			}
			else if (ex is HttpProxyException)
			{
				if (!this.HandleHttpProxyException(ex as HttpProxyException))
				{
					return;
				}
			}
			else if (ex is HttpException)
			{
				if (!this.HandleHttpException(ex as HttpException))
				{
					return;
				}
			}
			else if (this.IsPossibleException(ex))
			{
				this.Logger.AppendGenericError("PossibleException", ex.GetType().Name);
				this.HttpContext.Server.ClearError();
				this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, 500);
				this.LogErrorCode(Constants.InternalServerErrorStatusCode);
			}
			else
			{
				this.SetException(ex);
				this.UpdateRoutingFailurePerfCounter(true);
				this.Logger.AppendGenericError("UnexpectedException", ex.ToString());
			}
			this.Complete();
		}

		// Token: 0x06000684 RID: 1668 RVA: 0x00024D70 File Offset: 0x00022F70
		protected virtual void LogWebException(WebException exception)
		{
			this.Logger.AppendGenericError("WebExceptionStatus", exception.Status.ToString());
			HttpWebResponse httpWebResponse = (HttpWebResponse)exception.Response;
			int num = 0;
			if (httpWebResponse != null)
			{
				num = (int)httpWebResponse.StatusCode;
				this.Logger.AppendGenericError("ResponseStatusCode", num.ToString());
			}
			if (num != 500)
			{
				this.Logger.AppendGenericError("WebException", exception.ToString());
			}
		}

		// Token: 0x06000685 RID: 1669 RVA: 0x00024DF0 File Offset: 0x00022FF0
		protected bool HandleWebExceptionConnectivityError(WebException exception)
		{
			HttpWebHelper.ConnectivityError connectivityError = HttpWebHelper.CheckConnectivityError(exception);
			HttpWebResponse response = (HttpWebResponse)exception.Response;
			if (this.IsRetryOnConnectivityErrorEnabled && this.IsInRetryableState && connectivityError == HttpWebHelper.ConnectivityError.Retryable)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::HandleWebExceptionConnectivityError]: Context {0}; retryable connectivity exception thrown; will invalidate cache and set delay.", this.TraceContext);
				}
				this.InvalidateBackEndServerCacheSetDelay(response, false, true);
				if (this.RecalculateTargetBackend())
				{
					return true;
				}
			}
			else if (connectivityError != HttpWebHelper.ConnectivityError.None)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::HandleWebExceptionConnectivityError]: Context {0}; nonretryable connectivity exception thrown; will invalidate cache.", this.TraceContext);
				}
				this.InvalidateBackEndServerCache(response, true);
			}
			return false;
		}

		// Token: 0x06000686 RID: 1670 RVA: 0x00024E96 File Offset: 0x00023096
		private void LogErrorCode(string errorCode)
		{
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 9, errorCode);
		}

		// Token: 0x06000687 RID: 1671 RVA: 0x00024EAC File Offset: 0x000230AC
		private bool HandleWebException(WebException exception)
		{
			this.LogWebException(exception);
			if (this.HandleWebExceptionConnectivityError(exception))
			{
				return false;
			}
			string errorCode = exception.Status.ToString();
			if (exception.Response != null)
			{
				HttpWebResponse httpWebResponse = (HttpWebResponse)exception.Response;
				int statusCode = (int)httpWebResponse.StatusCode;
				if (statusCode != 401 || this.ProxyKerberosAuthentication || this.AuthBehavior.ShouldCopyAuthenticationHeaderToClientResponse)
				{
					this.HttpContext.Server.ClearError();
					ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ServerResponse);
					this.ServerResponse = null;
					this.ServerResponse = (HttpWebResponse)exception.Response;
					this.ProcessResponse(exception);
					return false;
				}
				this.haveReceivedAuthChallenge = false;
				bool flag = false;
				if (this.IsAuthenticationChallengeFromBackend(exception) && this.TryFindKerberosChallenge(exception.Response.Headers[Constants.AuthenticationHeader], out flag))
				{
					this.HttpContext.Server.ClearError();
					this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, 503, Utilities.GetTruncatedString("Failed authentication on backend server: " + httpWebResponse.StatusDescription, 512));
					this.SetRequestFailureContext(2, statusCode, statusCode.ToString(), exception.ToString(), null, null);
					errorCode = Constants.ServerKerberosAuthenticationFailureErrorCode;
				}
				else
				{
					this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, statusCode, Utilities.GetTruncatedString(httpWebResponse.StatusDescription, 512));
					this.SetRequestFailureContext(2, statusCode, statusCode.ToString(), exception.ToString(), null, null);
				}
			}
			else if (exception.Status == WebExceptionStatus.Timeout || exception.Status == WebExceptionStatus.RequestCanceled)
			{
				this.HttpContext.Server.ClearError();
				this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, 504);
				this.SetRequestFailureContext(2, -1, exception.Status.ToString(), exception.Message, new HttpProxySubErrorCode?(2014), new WebExceptionStatus?(exception.Status));
			}
			else
			{
				this.HttpContext.Server.ClearError();
				this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, 503);
				this.SetRequestFailureContext(2, -1, exception.Status.ToString(), exception.Message, null, new WebExceptionStatus?(exception.Status));
				if (HttpProxySettings.ClearBackEndOverrideCookieEnabled.Value && exception.Status == WebExceptionStatus.ConnectFailure)
				{
					this.ClearBackEndOverrideCookie();
				}
			}
			this.LogErrorCode(errorCode);
			return true;
		}

		// Token: 0x06000688 RID: 1672 RVA: 0x00025130 File Offset: 0x00023330
		private bool HandleHttpProxyException(HttpProxyException exception)
		{
			this.HttpContext.Server.ClearError();
			string text = exception.ErrorCode.ToString();
			if (exception.StatusCode != HttpStatusCode.Unauthorized)
			{
				this.Logger.AppendGenericError("HttpProxyException", exception.ToString());
			}
			if (exception.StatusCode == HttpStatusCode.InternalServerError && !(exception.InnerException is NonUniqueRecipientException))
			{
				this.UpdateRoutingFailurePerfCounter(true);
			}
			this.SetResponseHeaderIfHeadersUnsent(this.ClientResponse, Constants.CafeErrorCodeHeaderName, text);
			this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, (int)exception.StatusCode);
			this.SetRequestFailureContext(1, (int)exception.StatusCode, exception.ErrorCode.ToString(), exception.Message, new HttpProxySubErrorCode?(exception.ErrorCode), null);
			this.LogErrorCode(text);
			return true;
		}

		// Token: 0x06000689 RID: 1673 RVA: 0x00025210 File Offset: 0x00023410
		private bool HandleHttpException(HttpException exception)
		{
			string errorCode = string.Empty;
			InvalidOAuthTokenException ex = exception.InnerException as InvalidOAuthTokenException;
			if (ex != null)
			{
				errorCode = Constants.InvalidOAuthTokenErrorCode;
				this.HandleOAuthException(ex);
			}
			if (exception.GetHttpCode() == 302)
			{
				this.ClientResponse.Redirect(exception.Message, false);
				this.CompleteForRedirect(exception.Message);
				return false;
			}
			if (this.ClientResponse != null && !this.ClientResponse.IsClientConnected && AspNetHelper.IsExceptionExpectedWhenDisconnected(exception))
			{
				if (this.ShouldLogClientDisconnectError(exception))
				{
					this.Logger.AppendGenericError("HttpException", "ClientDisconnect");
					errorCode = Constants.ClientDisconnectErrorCode;
				}
			}
			else
			{
				this.HttpContext.Server.ClearError();
				int httpCode = exception.GetHttpCode();
				string text = exception.Message;
				if (httpCode == 500)
				{
					text = exception.ToString();
				}
				this.Logger.AppendGenericError("HttpException", text);
				HttpStatusCode httpStatusCode = (HttpStatusCode)httpCode;
				errorCode = httpStatusCode.ToString();
				this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, httpCode);
				try
				{
					this.ClientResponse.Write(exception.Message);
					this.SetRequestFailureContext(1, exception.GetHttpCode(), exception.Message, exception.ToString(), null, null);
				}
				catch
				{
				}
			}
			this.LogErrorCode(errorCode);
			return true;
		}

		// Token: 0x0600068A RID: 1674 RVA: 0x0002536C File Offset: 0x0002356C
		private void HandleOAuthException(InvalidOAuthTokenException exception)
		{
			MSDiagnosticsHeader.AppendChallengeAndDiagnosticsHeadersToResponse(this.ClientResponse, exception.ErrorCategory, exception.Message);
			this.HttpContext.Items["OAuthError"] = exception.ToString();
			this.HttpContext.Items["OAuthErrorCategory"] = exception.ErrorCategory.ToString();
			string str = this.HttpContext.Items["OAuthExtraInfo"] as string;
			this.HttpContext.Items["OAuthExtraInfo"] = str + string.Format("ErrorCode:{0}", exception.ErrorCode);
		}

		// Token: 0x0600068B RID: 1675 RVA: 0x0002541F File Offset: 0x0002361F
		private bool IsPossibleException(Exception exception)
		{
			return exception is SocketException || exception is IOException || exception is ProtocolViolationException;
		}

		// Token: 0x0600068C RID: 1676 RVA: 0x0002543C File Offset: 0x0002363C
		private void UpdateRoutingFailurePerfCounter(bool wasFailure)
		{
			string empty = string.Empty;
			if (!PerfCounters.RoutingErrorsEnabled)
			{
				return;
			}
			if (this.AnchoredRoutingTarget != null && this.AnchoredRoutingTarget.BackEndServer != null && !string.IsNullOrEmpty(this.AnchoredRoutingTarget.BackEndServer.Fqdn))
			{
				Utilities.TryExtractForestFqdnFromServerFqdn(this.AnchoredRoutingTarget.BackEndServer.Fqdn, ref empty);
			}
			if (wasFailure)
			{
				if (this.IsInRoutingState)
				{
					PerfCounters.UpdateMovingPercentagePerformanceCounter(PerfCounters.GetHttpProxyPerForestCountersInstance(empty).MovingPercentageRoutingFailure);
					PerfCounters.GetHttpProxyPerForestCountersInstance(empty).TotalFailedRequests.Increment();
					return;
				}
				if (!this.IsInPostRoutingState)
				{
					throw new NotImplementedException("No implementation for ProxyState");
				}
			}
			else
			{
				PerfCounters.IncrementMovingPercentagePerformanceCounterBase(PerfCounters.GetHttpProxyPerForestCountersInstance(empty).MovingPercentageRoutingFailure);
				PerfCounters.IncrementMovingPercentagePerformanceCounterBase(PerfCounters.GetHttpProxyPerForestCountersInstance(string.Empty).MovingPercentageRoutingFailure);
			}
		}

		// Token: 0x0600068D RID: 1677 RVA: 0x00025500 File Offset: 0x00023700
		private void SetRequestFailureContext(RequestFailureContext.RequestFailurePoint failurePoint, int statusCode, string error, string details, HttpProxySubErrorCode? httpProxySubErrorCode = null, WebExceptionStatus? webExceptionStatus = null)
		{
			RequestFailureContext value = new RequestFailureContext(failurePoint, statusCode, error, details, httpProxySubErrorCode, webExceptionStatus, null);
			this.HttpContext.Items[RequestFailureContext.HttpContextKeyName] = value;
		}

		// Token: 0x0600068E RID: 1678 RVA: 0x0002553C File Offset: 0x0002373C
		private void InspectDisconnectException(Exception exception)
		{
			HttpException ex = exception as HttpException;
			if (ex != null)
			{
				int errorCode = ex.ErrorCode;
				int num = 0;
				if (ex.InnerException != null && ex.InnerException is COMException)
				{
					num = ((COMException)ex.InnerException).ErrorCode;
				}
				throw new HttpException(string.Format("Unexpected HttpException with error code {0} - {1}, details {2}", errorCode, num, ex.ToString()), exception);
			}
			throw exception;
		}

		// Token: 0x1700016E RID: 366
		// (get) Token: 0x0600068F RID: 1679 RVA: 0x00003193 File Offset: 0x00001393
		protected virtual bool UseBackEndCacheForDownLevelServer
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700016F RID: 367
		// (get) Token: 0x06000690 RID: 1680 RVA: 0x000255A6 File Offset: 0x000237A6
		protected DatacenterRedirectStrategy DatacenterRedirectStrategy
		{
			get
			{
				if (this.datacenterRedirectStrategy == null)
				{
					this.datacenterRedirectStrategy = this.CreateDatacenterRedirectStrategy();
				}
				return this.datacenterRedirectStrategy;
			}
		}

		// Token: 0x17000170 RID: 368
		// (get) Token: 0x06000691 RID: 1681 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool IsBackendServerCacheValidationEnabled
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000171 RID: 369
		// (get) Token: 0x06000692 RID: 1682 RVA: 0x000255C2 File Offset: 0x000237C2
		// (set) Token: 0x06000693 RID: 1683 RVA: 0x000255CA File Offset: 0x000237CA
		protected AnchoredRoutingTarget AnchoredRoutingTarget { get; set; }

		// Token: 0x17000172 RID: 370
		// (get) Token: 0x06000694 RID: 1684 RVA: 0x000255D3 File Offset: 0x000237D3
		// (set) Token: 0x06000695 RID: 1685 RVA: 0x000255DB File Offset: 0x000237DB
		protected bool ProxyToDownLevel { get; set; }

		// Token: 0x06000696 RID: 1686 RVA: 0x000255E4 File Offset: 0x000237E4
		internal bool TryGetSpecificHeaderFromResponse(HttpWebResponse response, string functionName, string headerName, string expectedHeaderValue, out string headerValue)
		{
			headerValue = null;
			if (response != null)
			{
				headerValue = response.Headers[headerName];
				if (!string.IsNullOrEmpty(headerValue) && (expectedHeaderValue == null || headerValue.Equals(expectedHeaderValue, StringComparison.OrdinalIgnoreCase)))
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[{3}]: Context {0}; found {1} in {2} header.", new object[]
						{
							this.TraceContext,
							headerValue,
							headerName,
							functionName
						});
					}
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "RoutingError", headerName + "(" + headerValue + ")");
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000697 RID: 1687 RVA: 0x0001B794 File Offset: 0x00019994
		protected virtual DatacenterRedirectStrategy CreateDatacenterRedirectStrategy()
		{
			return new DefaultRedirectStrategy(this);
		}

		// Token: 0x06000698 RID: 1688 RVA: 0x00025690 File Offset: 0x00023890
		protected virtual AnchorMailbox ResolveAnchorMailbox()
		{
			this.LogElapsedTime("E_BaseResAnchMbx");
			AnchorMailbox anchorMailbox = null;
			if (!this.HasPreemptivelyCheckedForRoutingHint)
			{
				anchorMailbox = this.CreateAnchorMailboxFromRoutingHint();
			}
			if (anchorMailbox == null)
			{
				anchorMailbox = AnchorMailboxFactory.CreateFromCaller(this);
			}
			this.LogElapsedTime("L_BaseResAnchMbx");
			return anchorMailbox;
		}

		// Token: 0x06000699 RID: 1689 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool ShouldRecalculateProxyTarget()
		{
			return false;
		}

		// Token: 0x0600069A RID: 1690 RVA: 0x000256D0 File Offset: 0x000238D0
		protected virtual AnchoredRoutingTarget TryFastTargetCalculationByAnchorMailbox(AnchorMailbox anchorMailbox)
		{
			this.LogElapsedTime("E_TryFastTargetCalc");
			BackEndServer backEndServer = anchorMailbox.TryDirectBackEndCalculation();
			if (backEndServer != null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<AnchoredRoutingTarget>((long)this.GetHashCode(), "[ProxyRequestHandler::TryFastTargetCalculationByAnchorMailbox]: Resolved target {0} directly from anchor mailbox.", this.AnchoredRoutingTarget);
				}
				return new AnchoredRoutingTarget(anchorMailbox, backEndServer);
			}
			this.LogElapsedTime("L_TryFastTargetCalc");
			return null;
		}

		// Token: 0x0600069B RID: 1691 RVA: 0x00025730 File Offset: 0x00023930
		protected virtual BackEndServer GetDownLevelClientAccessServer(AnchorMailbox anchorMailbox, BackEndServer mailboxServer)
		{
			this.LogElapsedTime("E_GetDLCAS");
			Uri uri = null;
			BackEndServer downLevelClientAccessServer = DownLevelServerManager.Instance.GetDownLevelClientAccessServer<WebServicesService>(anchorMailbox, mailboxServer, 2, this.Logger, false, out uri);
			this.LogElapsedTime("L_GetDLCAS");
			return downLevelClientAccessServer;
		}

		// Token: 0x0600069C RID: 1692 RVA: 0x0000500A File Offset: 0x0000320A
		protected virtual AnchoredRoutingTarget TryDirectTargetCalculation()
		{
			return null;
		}

		// Token: 0x0600069D RID: 1693 RVA: 0x0002576C File Offset: 0x0002396C
		protected virtual bool HandleBackEndCalculationException(Exception exception, AnchorMailbox anchorMailbox, string label)
		{
			this.LogElapsedTime("E_HandleBECalcEx");
			bool result;
			try
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<int, AnchorMailbox, Exception>((long)this.GetHashCode(), "[ProxyRequestHandler::HandleBackEndCalculationException]: Context {0}. Handling backend calculation exception for anchor mailbox {1}. Exception: {2}", this.TraceContext, anchorMailbox, exception);
				}
				if (exception is HttpException || exception is HttpProxyException)
				{
					this.CompleteWithError(exception, label);
					result = true;
				}
				else if (exception is ADTransientException || exception is MServTransientException || exception is DataValidationException || exception is DataSourceOperationException || exception is NonUniqueRecipientException)
				{
					Exception ex = new HttpProxyException(HttpStatusCode.InternalServerError, 2001, exception.Message, exception);
					this.CompleteWithError(ex, label);
					result = true;
				}
				else
				{
					if (exception is RemoteForestDownLevelServerException)
					{
						RemoteForestDownLevelServerException ex2 = (RemoteForestDownLevelServerException)exception;
						try
						{
							this.Logger.AppendGenericError("RemoteForestDownLevelServerException", string.Format("{0}@{1}", ex2.DatabaseId, ex2.ResourceForest));
							this.DatacenterRedirectStrategy.RedirectMailbox(anchorMailbox);
						}
						catch (HttpException ex3)
						{
							this.CompleteWithError(ex3, label);
							return true;
						}
						catch (HttpProxyException ex4)
						{
							this.CompleteWithError(ex4, label);
							return true;
						}
					}
					if (exception is ServerLocatorClientException || exception is ServerLocatorClientTransientException || exception is MailboxServerLocatorException || exception is AmServerTransientException || exception is AmServerException)
					{
						PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorFailedCalls.Increment();
						PerfCounters.UpdateMovingPercentagePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingPercentageMailboxServerLocatorFailedCalls);
						Exception ex = new HttpProxyException(HttpStatusCode.InternalServerError, 2004, exception.Message, exception);
						this.CompleteWithError(ex, label);
						result = true;
					}
					else if (exception is DatabaseNotFoundException)
					{
						this.OnDatabaseNotFound(anchorMailbox);
						Exception ex;
						if (anchorMailbox is DatabaseNameAnchorMailbox)
						{
							ex = new HttpProxyException(HttpStatusCode.NotFound, 3004, exception.Message, exception);
						}
						else if (anchorMailbox is DatabaseGuidAnchorMailbox)
						{
							ex = new HttpProxyException(HttpStatusCode.NotFound, 3005, exception.Message, exception);
						}
						else
						{
							ex = new HttpProxyException(HttpStatusCode.InternalServerError, 3005, exception.Message, exception);
						}
						this.CompleteWithError(ex, label);
						result = true;
					}
					else if (exception is DagDecomException)
					{
						this.OnDatabaseNotFound(anchorMailbox);
						Exception ex = new HttpProxyException(HttpStatusCode.InternalServerError, 2006, exception.Message, exception);
						this.CompleteWithError(ex, label);
						result = true;
					}
					else if (exception is ServerNotFoundException)
					{
						Exception ex;
						if (anchorMailbox is ServerInfoAnchorMailbox)
						{
							ex = new HttpProxyException(HttpStatusCode.NotFound, 3007, exception.Message, exception);
						}
						else if (anchorMailbox != null && anchorMailbox.AnchorSource == AnchorSource.ServerVersion)
						{
							ex = new HttpProxyException(HttpStatusCode.NotFound, 3008, exception.Message, exception);
						}
						else
						{
							ex = new HttpProxyException(HttpStatusCode.InternalServerError, 3007, exception.Message, exception);
						}
						this.CompleteWithError(ex, label);
						result = true;
					}
					else if (exception is ObjectNotFoundException)
					{
						Exception ex = new HttpProxyException(HttpStatusCode.InternalServerError, 3007, exception.Message, exception);
						this.CompleteWithError(ex, label);
						result = true;
					}
					else if (exception is ServiceDiscoveryPermanentException || exception is ServiceDiscoveryTransientException)
					{
						Exception ex = new HttpProxyException(HttpStatusCode.InternalServerError, 2003, exception.Message, exception);
						this.CompleteWithError(ex, label);
						result = true;
					}
					else if (exception is CrossResourceForestProxyNotAllowedException)
					{
						this.CompleteWithError((HttpProxyException)exception, label);
						result = true;
					}
					else
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
						{
							ExTraceGlobals.VerboseTracer.TraceError<int, AnchorMailbox, Exception>((long)this.GetHashCode(), "[ProxyRequestHandler::HandleBackEndCalculationException]: Context {0}. BackEnd calculation exception unhandled for anchor mailbox {1}. Exception: {2}", this.TraceContext, anchorMailbox, exception);
						}
						result = false;
					}
				}
			}
			finally
			{
				this.LogElapsedTime("L_HandleBECalcEx");
			}
			return result;
		}

		// Token: 0x0600069E RID: 1694 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void RedirectIfNeeded(BackEndServer mailbox)
		{
		}

		// Token: 0x0600069F RID: 1695 RVA: 0x00025B14 File Offset: 0x00023D14
		protected virtual void OnDatabaseNotFound(AnchorMailbox anchorMailbox)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::OnDatabaseNotFound]: Context {0}; invalidating cache.", this.TraceContext);
			}
			if (anchorMailbox == null)
			{
				return;
			}
			ADObjectId adobjectId = null;
			DatabaseBasedAnchorMailbox databaseBasedAnchorMailbox = anchorMailbox as DatabaseBasedAnchorMailbox;
			if (databaseBasedAnchorMailbox != null)
			{
				try
				{
					adobjectId = databaseBasedAnchorMailbox.GetDatabase();
				}
				catch (DatabaseNotFoundException)
				{
				}
			}
			if (adobjectId != null)
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "OnDatabaseNotFound", adobjectId.ObjectGuid);
				MailboxServerCache.Instance.Remove(adobjectId.ObjectGuid, this);
			}
			anchorMailbox.InvalidateCache();
		}

		// Token: 0x060006A0 RID: 1696 RVA: 0x00025BAC File Offset: 0x00023DAC
		protected virtual void UpdateOrInvalidateAnchorMailboxCache(Guid mdbGuid, string resourceForest)
		{
			this.InvalidateAnchorMailboxCache(mdbGuid, resourceForest);
		}

		// Token: 0x060006A1 RID: 1697 RVA: 0x00025BB6 File Offset: 0x00023DB6
		protected void InvalidateAnchorMailboxCache(Guid mdbGuid, string resourceForest)
		{
			this.AnchoredRoutingTarget.AnchorMailbox.InvalidateCache();
		}

		// Token: 0x060006A2 RID: 1698 RVA: 0x00025BC8 File Offset: 0x00023DC8
		protected virtual bool IsRoutingError(HttpWebResponse response)
		{
			string text;
			return this.TryGetSpecificHeaderFromResponse(response, "ProxyRequestHandler::IsRoutingError", Constants.BEServerExceptionHeaderName, Constants.IllegalCrossServerConnectionExceptionType, out text) || this.TryGetSpecificHeaderFromResponse(response, "ProxyRequestHandler::IsRoutingError", Constants.BEServerRoutingErrorHeaderName, null, out text) || (this.IsRumRoutingError(response) && response.StatusCode == HttpStatusCode.ServiceUnavailable);
		}

		// Token: 0x060006A3 RID: 1699 RVA: 0x00025C21 File Offset: 0x00023E21
		protected virtual AnchoredRoutingTarget DoProtocolSpecificRoutingTargetOverride(AnchoredRoutingTarget routingTarget)
		{
			if (routingTarget == null)
			{
				throw new ArgumentNullException("routingTarget");
			}
			this.RedirectIfNeeded(routingTarget.BackEndServer);
			return null;
		}

		// Token: 0x060006A4 RID: 1700 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool ShouldContinueProxy()
		{
			return false;
		}

		// Token: 0x060006A5 RID: 1701 RVA: 0x00025C40 File Offset: 0x00023E40
		protected virtual Uri GetTargetBackEndServerUrl()
		{
			this.LogElapsedTime("E_TargetBEUrl");
			Uri result;
			try
			{
				UrlAnchorMailbox urlAnchorMailbox = this.AnchoredRoutingTarget.AnchorMailbox as UrlAnchorMailbox;
				if (urlAnchorMailbox != null)
				{
					result = urlAnchorMailbox.Url;
				}
				else
				{
					UriBuilder clientUrlForProxy = this.GetClientUrlForProxy();
					clientUrlForProxy.Scheme = Uri.UriSchemeHttps;
					clientUrlForProxy.Host = this.AnchoredRoutingTarget.BackEndServer.Fqdn;
					clientUrlForProxy.Port = 444;
					if (this.AnchoredRoutingTarget.BackEndServer.Version < Server.E15MinVersion)
					{
						this.ProxyToDownLevel = true;
						RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "ProxyToDownLevel", true);
						clientUrlForProxy.Port = 443;
					}
					result = clientUrlForProxy.Uri;
				}
			}
			finally
			{
				this.LogElapsedTime("L_TargetBEUrl");
			}
			return result;
		}

		// Token: 0x060006A6 RID: 1702 RVA: 0x0001BAAF File Offset: 0x00019CAF
		protected virtual UriBuilder GetClientUrlForProxy()
		{
			return new UriBuilder(this.ClientRequest.Url);
		}

		// Token: 0x060006A7 RID: 1703 RVA: 0x00025D10 File Offset: 0x00023F10
		protected virtual MailboxServerLocator CreateMailboxServerLocator(Guid databaseGuid, string domainName, string resourceForest)
		{
			return MailboxServerLocator.Create(databaseGuid, domainName, resourceForest, !this.IsRetryingOnError, GuardedSlsExecution.MailboxServerLocatorCallbacks, Extensions.GetGenericInfoLogDelegate(this.Logger));
		}

		// Token: 0x060006A8 RID: 1704 RVA: 0x00025D33 File Offset: 0x00023F33
		protected virtual void BeginValidateBackendServerCache()
		{
			throw new NotImplementedException("Backend server cache validation not implemented for this handler");
		}

		// Token: 0x060006A9 RID: 1705 RVA: 0x00025D40 File Offset: 0x00023F40
		protected void BeginProxyRequestOrRecalculate()
		{
			object obj = this.LockObject;
			lock (obj)
			{
				if (this.State != ProxyRequestHandler.ProxyState.CalculateBackEndSecondRound && this.ShouldRecalculateProxyTarget())
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::BeginProxyRequestOrRecalculate]: context {0}, protocal require 2nd round calculation. Start 2nd round BeginCalculateTargetBackEnd again.", this.TraceContext);
					}
					ThreadPool.QueueUserWorkItem(new WaitCallback(this.BeginCalculateTargetBackEnd));
					this.State = ProxyRequestHandler.ProxyState.CalculateBackEndSecondRound;
				}
				else if (this.ShouldContinueProxy())
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::BeginProxyRequestOrRecalculate]: ShouldProxy == false.  No need to process futher.  Context {0}.", this.TraceContext);
					}
					this.Complete();
				}
				else
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int, AnchoredRoutingTarget>((long)this.GetHashCode(), "[ProxyRequestHandler::BeginProxyRequestOrRecalculate]: Enqueue BeginProxyRequest. Context {0}, final AnchoredRoutingTarget {1}.", this.TraceContext, this.AnchoredRoutingTarget);
					}
					ThreadPool.QueueUserWorkItem(new WaitCallback(this.BeginProxyRequest));
					this.State = ProxyRequestHandler.ProxyState.PrepareServerRequest;
				}
			}
		}

		// Token: 0x060006AA RID: 1706 RVA: 0x00025E54 File Offset: 0x00024054
		protected bool HandleRoutingError(HttpWebResponse response, bool invalidateAnchorMailboxCache)
		{
			if (this.IsRoutingError(response))
			{
				this.InvalidateBackEndServerCacheSetDelay(response, false, invalidateAnchorMailboxCache);
				return true;
			}
			if (this.IsRumRoutingError(response))
			{
				this.InvalidateBackEndServerCache(response, invalidateAnchorMailboxCache);
			}
			return false;
		}

		// Token: 0x060006AB RID: 1707 RVA: 0x00025E80 File Offset: 0x00024080
		protected bool ProcessRoutingUpdateModuleResponse(HttpWebResponse response)
		{
			try
			{
				DatabaseGuidRoutingDestination databaseGuidRoutingDestination = null;
				string text;
				if (this.ShouldUpdateAnchorMailboxCache(response, out text, out databaseGuidRoutingDestination))
				{
					this.UpdateOrInvalidateAnchorMailboxCache(databaseGuidRoutingDestination.DatabaseGuid, databaseGuidRoutingDestination.ResourceForest);
					this.AnchoredRoutingTarget.AnchorMailbox.UpdateCache(new AnchorMailboxCacheEntry
					{
						Database = new ADObjectId(databaseGuidRoutingDestination.DatabaseGuid, databaseGuidRoutingDestination.ResourceForest),
						DomainName = databaseGuidRoutingDestination.DomainName
					});
					PerfCounters.HttpProxyCacheCountersInstance.RouteRefresherSuccessfulAnchorMailboxCacheUpdates.Increment();
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "AnchorMailboxRoutingEntryUpdate", text);
					this.Logger.AddDatabaseGuid(Constants.CafeUpdateDBGuidPrefix, databaseGuidRoutingDestination.DatabaseGuid);
					return true;
				}
			}
			catch (ArgumentException ex)
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericError(this.Logger, "HandleRUMResponseError", ex.Message);
			}
			return false;
		}

		// Token: 0x060006AC RID: 1708 RVA: 0x00025F54 File Offset: 0x00024154
		protected bool RecalculateTargetBackend()
		{
			if (this.ShouldRetryOnError)
			{
				this.retryOnErrorCounter++;
				this.LogForRetry(string.Format("WillRetryOnError({0}/{1})-LastTryData", this.retryOnErrorCounter, HttpProxySettings.MaxRetryOnError.Value), true, RequestDetailsLogger.PreservedHttpProxyMetadata);
				this.ResetForRetryOnError();
				object obj = this.LockObject;
				lock (obj)
				{
					if (this.delayOnRetryOnError > 0)
					{
						Task.Delay(this.delayOnRetryOnError).ContinueWith(new Action<Task>(this.BeginCalculateTargetBackEnd));
					}
					else
					{
						ThreadPool.QueueUserWorkItem(new WaitCallback(this.BeginCalculateTargetBackEnd));
					}
					this.State = ProxyRequestHandler.ProxyState.CalculateBackEnd;
				}
				return true;
			}
			return false;
		}

		// Token: 0x060006AD RID: 1709 RVA: 0x00026024 File Offset: 0x00024224
		protected void BeginContinueOnAuthenticate(object extraData)
		{
			this.CallThreadEntranceMethod(delegate
			{
				this.LogElapsedTime("E_BeginContinueOnAuthenticate");
				try
				{
					this.AuthBehavior.ContinueOnAuthenticate(this.HttpApplication, new AsyncCallback(this.ContinueOnAuthenticateCallBack));
				}
				catch (Exception ex)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError<Exception, int, ProxyRequestHandler.ProxyState>((long)this.GetHashCode(), "[ProxyRequestHandler::BeginContinueOnAuthenticate]: An error occurred while trying to authenticate: {0}; Context {1}; State {2}", ex, this.TraceContext, this.State);
					}
					throw;
				}
				finally
				{
					this.LogElapsedTime("L_BeginContinueOnAuthenticate");
				}
			});
		}

		// Token: 0x060006AE RID: 1710 RVA: 0x00026038 File Offset: 0x00024238
		private static void MailboxServerLocatorCompletedCallback(IAsyncResult result)
		{
			MailboxServerLocatorAsyncState mailboxServerLocatorAsyncState = (MailboxServerLocatorAsyncState)result.AsyncState;
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)mailboxServerLocatorAsyncState.ProxyRequestHandler.GetHashCode(), "[ProxyRequestHandler::MailboxServerLocatorCompletedCallback]: Context {0}", mailboxServerLocatorAsyncState.ProxyRequestHandler.TraceContext);
			}
			if (result.CompletedSynchronously)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(mailboxServerLocatorAsyncState.ProxyRequestHandler.OnCalculateTargetBackEndCompleted), new TargetCalculationCallbackBeacon(mailboxServerLocatorAsyncState, result));
				return;
			}
			mailboxServerLocatorAsyncState.ProxyRequestHandler.OnCalculateTargetBackEndCompleted(new TargetCalculationCallbackBeacon(mailboxServerLocatorAsyncState, result));
		}

		// Token: 0x060006AF RID: 1711 RVA: 0x000260C0 File Offset: 0x000242C0
		private bool IsRumRoutingError(HttpWebResponse response)
		{
			string text;
			return this.TryGetSpecificHeaderFromResponse(response, "ProxyRequestHandler::IsRumRoutingError", "X-RoutingEntryUpdate", null, out text) && (!string.IsNullOrEmpty(text) && text.Contains("DatabaseGuid")) && text.Contains(Constants.RumCouldNotFindDatabaseSerializedString);
		}

		// Token: 0x060006B0 RID: 1712 RVA: 0x00026108 File Offset: 0x00024308
		private bool ShouldUpdateAnchorMailboxCache(HttpWebResponse response, out string routingUpdateHeaderValue, out DatabaseGuidRoutingDestination updatedRoutingDestination)
		{
			updatedRoutingDestination = null;
			if (this.TryGetSpecificHeaderFromResponse(response, "[ProxyRequestHandler::ShouldUpdateAnchorMailboxCache]", "X-RoutingEntryUpdate", null, out routingUpdateHeaderValue) && !string.IsNullOrEmpty(routingUpdateHeaderValue))
			{
				updatedRoutingDestination = (RoutingEntryHeaderSerializer.Deserialize(routingUpdateHeaderValue).Destination as DatabaseGuidRoutingDestination);
				if (updatedRoutingDestination != null)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060006B1 RID: 1713 RVA: 0x00026158 File Offset: 0x00024358
		private bool InvalidateBackEndServerCache(HttpWebResponse response, bool invalidateAnchorMailboxCache)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<int>((long)this.GetHashCode(), "[ProxyRequestHandler::InvalidatingBackEndServerCache]: Context {0}; invalidating cache.", this.TraceContext);
			}
			ADObjectId adobjectId = null;
			DatabaseBasedAnchorMailbox databaseBasedAnchorMailbox = this.AnchoredRoutingTarget.AnchorMailbox as DatabaseBasedAnchorMailbox;
			if (databaseBasedAnchorMailbox != null)
			{
				adobjectId = databaseBasedAnchorMailbox.GetDatabase();
			}
			Guid guid = (adobjectId != null) ? adobjectId.ObjectGuid : Guid.Empty;
			string resourceForest = null;
			string text;
			if (this.IsRetryOnErrorEnabled && this.TryGetSpecificHeaderFromResponse(response, "InvalidateBackEndServerCache", "X-DBMountedOnServer", null, out text))
			{
				Fqdn fqdn;
				int num;
				if (Utilities.TryParseDBMountedOnServerHeader(text, ref guid, ref fqdn, ref num))
				{
					bool flag = adobjectId != null && !adobjectId.ObjectGuid.Equals(guid);
					if (string.Equals(this.AnchoredRoutingTarget.BackEndServer.Fqdn, fqdn.ToString(), StringComparison.InvariantCultureIgnoreCase))
					{
						if (flag)
						{
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "MdbGuidMismatch-Error", string.Format("{0}~{1}", adobjectId.ObjectGuid, guid));
						}
						return false;
					}
					if (flag)
					{
						RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "MdbGuidMismatch", string.Format("{0}~{1}", adobjectId.ObjectGuid, guid));
					}
					Utilities.TryExtractForestFqdnFromServerFqdn(fqdn, ref resourceForest);
				}
				else
				{
					this.Logger.AppendGenericError("InvalidDBMountedServerHeader", text);
				}
			}
			if (adobjectId != null)
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "InvalidatingBackEndServerCache", adobjectId.ObjectGuid);
				MailboxServerCache.Instance.Remove(adobjectId.ObjectGuid, this);
			}
			if (invalidateAnchorMailboxCache)
			{
				this.UpdateOrInvalidateAnchorMailboxCache(guid, resourceForest);
			}
			return true;
		}

		// Token: 0x060006B2 RID: 1714 RVA: 0x000262EC File Offset: 0x000244EC
		private void InvalidateBackEndServerCacheSetDelay(HttpWebResponse response, bool alwaysDelay, bool invalidateAnchorMailboxCache = true)
		{
			bool flag = this.InvalidateBackEndServerCache(response, invalidateAnchorMailboxCache);
			this.delayOnRetryOnError = 0;
			if (alwaysDelay || !flag || this.IsRetryingOnError)
			{
				this.delayOnRetryOnError = HttpProxySettings.DelayOnRetryOnError.Value;
			}
			if (!this.ShouldRetryOnError)
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 9, "RoutingError");
			}
		}

		// Token: 0x060006B3 RID: 1715 RVA: 0x00026346 File Offset: 0x00024546
		private void BeginCalculateTargetBackEnd(object extraData)
		{
			this.CallThreadEntranceMethod(delegate
			{
				this.LogElapsedTime("E_BeginCalcTargetBE");
				try
				{
					object obj = this.LockObject;
					lock (obj)
					{
						this.LatencyTracker.StartTracking((this.State == ProxyRequestHandler.ProxyState.CalculateBackEnd) ? LatencyTrackerKey.CalculateTargetBackEndLatency : LatencyTrackerKey.CalculateTargetBackEndSecondRoundLatency, this.IsRetryingOnError);
						AnchorMailbox anchorMailbox = null;
						try
						{
							this.DoProtocolSpecificBeginProcess();
							this.InternalBeginCalculateTargetBackEnd(out anchorMailbox);
							if (anchorMailbox != null)
							{
								this.Logger.ActivityScope.SetProperty(5, anchorMailbox.GetOrganizationNameForLogging());
							}
						}
						catch (Exception exception)
						{
							if (!this.HandleBackEndCalculationException(exception, anchorMailbox, "BeginCalculateTargetBackEnd"))
							{
								ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.mailboxServerLocator);
								this.mailboxServerLocator = null;
								throw;
							}
						}
					}
				}
				finally
				{
					this.LogElapsedTime("L_BeginCalcTargetBE");
				}
			});
		}

		// Token: 0x060006B4 RID: 1716 RVA: 0x0002635C File Offset: 0x0002455C
		private void InternalBeginCalculateTargetBackEnd(out AnchorMailbox anchorMailbox)
		{
			this.LogElapsedTime("E_IntBeginCalcTargetBE");
			try
			{
				anchorMailbox = null;
				this.AnchoredRoutingTarget = this.TryDirectTargetCalculation();
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<AnchoredRoutingTarget>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalBeginCalculateTargetBackEnd]: TryDirectTargetCalculation returns {0}", this.AnchoredRoutingTarget);
				}
				if (this.AnchoredRoutingTarget != null)
				{
					ThreadPool.QueueUserWorkItem(new WaitCallback(this.OnCalculateTargetBackEndCompleted), new TargetCalculationCallbackBeacon(this.AnchoredRoutingTarget));
					anchorMailbox = this.AnchoredRoutingTarget.AnchorMailbox;
				}
				else
				{
					anchorMailbox = this.ResolveAnchorMailbox();
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailbox>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalBeginCalculateTargetBackEnd]: ResolveAnchorMailbox returns {0}", anchorMailbox);
					}
					string text = anchorMailbox.ToString();
					if (anchorMailbox.OriginalAnchorMailbox != null)
					{
						text = string.Format("{0}-{1}", anchorMailbox.OriginalAnchorMailbox.ToString(), text);
					}
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 1, text);
					PerfCounters.HttpProxyCacheCountersInstance.OverallCacheEffectivenessRateBase.Increment();
					NegativeAnchorMailboxCacheEntry negativeCacheEntry = anchorMailbox.GetNegativeCacheEntry();
					if (negativeCacheEntry != null)
					{
						throw new HttpProxyException(negativeCacheEntry.ErrorCode, negativeCacheEntry.SubErrorCode, "NegativeCache:" + negativeCacheEntry.SourceObject);
					}
					this.AnchoredRoutingTarget = this.TryFastTargetCalculationByAnchorMailbox(anchorMailbox);
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<AnchoredRoutingTarget>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalBeginCalculateTargetBackEnd]: TryFastTargetCalculationByAnchorMailbox returns {0}", this.AnchoredRoutingTarget);
					}
					if (this.AnchoredRoutingTarget != null)
					{
						ThreadPool.QueueUserWorkItem(new WaitCallback(this.OnCalculateTargetBackEndCompleted), new TargetCalculationCallbackBeacon(this.AnchoredRoutingTarget));
						if (anchorMailbox.CacheEntryCacheHit)
						{
							PerfCounters.HttpProxyCacheCountersInstance.OverallCacheEffectivenessRate.Increment();
						}
					}
					else
					{
						DatabaseBasedAnchorMailbox databaseBasedAnchorMailbox = (DatabaseBasedAnchorMailbox)anchorMailbox;
						ADObjectId adobjectId = databaseBasedAnchorMailbox.GetDatabase();
						if (adobjectId != null)
						{
							this.Logger.AddDatabaseGuid(Constants.CafeRoutingDBGuidPrefix, adobjectId.ObjectGuid);
						}
						FrontEndProxyServerSettingsProvider instance = FrontEndProxyServerSettingsProvider.Instance;
						if (adobjectId != null && !instance.IsBackEndProxyAllowed(adobjectId.PartitionFQDN, anchorMailbox.GetTenantContext().OrganizationId))
						{
							throw new CrossResourceForestProxyNotAllowedException(adobjectId.PartitionFQDN);
						}
						if (adobjectId != null && HttpProxySettings.NoMailboxFallbackRoutingEnabled.Value && databaseBasedAnchorMailbox.IsOrganizationMailboxDatabase)
						{
							adobjectId = null;
							if (this.fallbackAnchorMailbox == null)
							{
								this.fallbackAnchorMailbox = databaseBasedAnchorMailbox;
							}
						}
						if (adobjectId == null && this.UseRoutingHintForAnchorMailbox && this.IsAnchorMailboxFromRoutingHint)
						{
							this.UseRoutingHintForAnchorMailbox = false;
							this.IsAnchorMailboxFromRoutingHint = false;
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
							{
								ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailbox>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalBeginCalculateTargetBackEnd]: Current anchor mailbox {0} cannot be associated with a database. Will calculate again without using routing hint.", anchorMailbox);
							}
							this.LogForRetry("RetryInternalBeginCalculateTargetBackEnd", false, new HttpProxyMetadata[]
							{
								1,
								3
							});
							this.InternalBeginCalculateTargetBackEnd(out anchorMailbox);
						}
						else
						{
							if (adobjectId == null && this.fallbackAnchorMailbox != null)
							{
								ADObjectId database = this.fallbackAnchorMailbox.GetDatabase();
								this.Logger.AddDatabaseGuid(Constants.CafeRoutingDBGuidPrefix, database.ObjectGuid);
								if (!HttpProxySettings.NoMailboxFallbackRoutingRandomBackEndEnabled.Value || !Utilities.IsLocalForest(database.PartitionFQDN))
								{
									RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "Fallback", database.ObjectGuid);
									adobjectId = database;
									anchorMailbox = this.fallbackAnchorMailbox;
								}
							}
							if (adobjectId == null && this.AuthBehavior.AuthState != AuthState.FrontEndFullAuth && this.AuthBehavior.ShouldDoFullAuthOnUnresolvedAnchorMailbox && !this.AuthBehavior.IsFullyAuthenticated() && !AnchorMailbox.AllowMissingTenant.Value)
							{
								RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "ContinueAuth", "Database-Null");
								ThreadPool.QueueUserWorkItem(new WaitCallback(this.BeginContinueOnAuthenticate));
							}
							else
							{
								if (adobjectId == null && HttpProxySettings.LocalForestDatabaseEnabled.Value)
								{
									adobjectId = LocalForestDatabaseProvider.Instance.GetRandomDatabase();
									RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "RandomDB", (adobjectId == null) ? "<null>" : adobjectId.ObjectGuid.ToString());
									if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
									{
										ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailbox, ADObjectId>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalBeginCalculateTargetBackEnd]: Current anchor mailbox {0} cannot be associated with a database. Will use random database to route {1}", anchorMailbox, adobjectId);
									}
								}
								if (adobjectId == null)
								{
									BackEndServer randomE15Server = MailboxServerCache.Instance.GetRandomE15Server(this);
									RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "RandomBE", randomE15Server);
									this.AnchoredRoutingTarget = new AnchoredRoutingTarget(anchorMailbox, randomE15Server);
									if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
									{
										ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailbox, AnchoredRoutingTarget>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalBeginCalculateTargetBackEnd]: Current anchor mailbox {0} cannot be associated with a database. Will use random routing target {1}", anchorMailbox, this.AnchoredRoutingTarget);
									}
									ThreadPool.QueueUserWorkItem(new WaitCallback(this.OnCalculateTargetBackEndCompleted), new TargetCalculationCallbackBeacon(this.AnchoredRoutingTarget));
									if (anchorMailbox.CacheEntryCacheHit)
									{
										PerfCounters.HttpProxyCacheCountersInstance.OverallCacheEffectivenessRate.Increment();
									}
								}
								else
								{
									if (this.IsRetryOnErrorEnabled)
									{
										bool flag = false;
										if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
										{
											ExTraceGlobals.FaultInjectionTracer.TraceTest<bool>(2379006271U, ref flag);
										}
										if (flag && this.retryOnErrorCounter == 0)
										{
											MailboxServerCache.Instance.Remove(adobjectId.ObjectGuid, this);
										}
									}
									BackEndServer backEndServer = null;
									MailboxServerCacheEntry mailboxServerCacheEntry = null;
									if (MailboxServerCache.Instance.TryGet(adobjectId.ObjectGuid, this, out mailboxServerCacheEntry))
									{
										backEndServer = mailboxServerCacheEntry.BackEndServer;
										this.SetDatabaseToServerRoutingEntry(adobjectId.ObjectGuid, adobjectId.PartitionFQDN, mailboxServerCacheEntry.BackEndServer, mailboxServerCacheEntry.FailoverSequenceNumber);
										if (anchorMailbox.CacheEntryCacheHit)
										{
											PerfCounters.HttpProxyCacheCountersInstance.OverallCacheEffectivenessRate.Increment();
										}
										if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
										{
											ExTraceGlobals.VerboseTracer.TraceDebug<BackEndServer>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalBeginCalculateTargetBackEnd]: Found back end server {0} in cache.", backEndServer);
										}
									}
									if (backEndServer != null)
									{
										ThreadPool.QueueUserWorkItem(new WaitCallback(this.OnCalculateTargetBackEndCompleted), new TargetCalculationCallbackBeacon(anchorMailbox, backEndServer));
									}
									else
									{
										if (this.IsRetryOnErrorEnabled)
										{
											bool flag2 = false;
											if (ExTraceGlobals.FaultInjectionTracer.IsTraceEnabled(9))
											{
												ExTraceGlobals.FaultInjectionTracer.TraceTest<bool>(2379006271U, ref flag2);
											}
											if (flag2)
											{
												ThreadPool.QueueUserWorkItem(new WaitCallback(this.OnCalculateTargetBackEndCompleted), new TargetCalculationCallbackBeacon(anchorMailbox, MailboxServerCache.Instance.GetRandomE15Server(this)));
												return;
											}
										}
										string text2 = null;
										if (anchorMailbox is UserBasedAnchorMailbox)
										{
											text2 = ((UserBasedAnchorMailbox)anchorMailbox).GetDomainName();
										}
										PerfCounters.IncrementMovingPercentagePerformanceCounterBase(PerfCounters.HttpProxyCountersInstance.MovingPercentageMailboxServerLocatorFailedCalls);
										if (this.IsRetryOnErrorEnabled)
										{
											ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.mailboxServerLocator);
											this.mailboxServerLocator = null;
										}
										this.mailboxServerLocator = this.CreateMailboxServerLocator(adobjectId.ObjectGuid, text2, adobjectId.PartitionFQDN);
										if (HttpProxySettings.TestBackEndSupportEnabled.Value && !string.IsNullOrEmpty(this.ClientRequest.GetTestBackEndUrl()))
										{
											this.mailboxServerLocator.SkipServerLocatorQuery = true;
										}
										MailboxServerLocatorAsyncState mailboxServerLocatorAsyncState = new MailboxServerLocatorAsyncState
										{
											AnchorMailbox = anchorMailbox,
											Locator = this.mailboxServerLocator,
											ProxyRequestHandler = this
										};
										RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "ServerLocatorCall", string.Concat(new object[]
										{
											MailboxServerLocator.UseResourceForest.Value ? "RF" : "DM",
											":",
											adobjectId.ObjectGuid,
											"~",
											text2,
											"~",
											adobjectId.PartitionFQDN
										}));
										if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
										{
											ExTraceGlobals.VerboseTracer.TraceDebug<ADObjectId>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalBeginCalculateTargetBackEnd]: Begin resolving backend server for database {0}", adobjectId);
										}
										PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorCalls.Increment();
										this.mailboxServerLocator.BeginGetServer(new AsyncCallback(ProxyRequestHandler.MailboxServerLocatorCompletedCallback), mailboxServerLocatorAsyncState);
									}
								}
							}
						}
					}
				}
			}
			finally
			{
				this.LogElapsedTime("L_IntBeginCalcTargetBE");
			}
		}

		// Token: 0x060006B5 RID: 1717 RVA: 0x00026A9C File Offset: 0x00024C9C
		private void OnCalculateTargetBackEndCompleted(object extraData)
		{
			this.CallThreadEntranceMethod(delegate
			{
				this.LogElapsedTime("E_OnCalcTargetBEComp");
				try
				{
					object obj = this.LockObject;
					lock (obj)
					{
						ProxyRequestHandler.ProxyState state = this.State;
						TargetCalculationCallbackBeacon targetCalculationCallbackBeacon = (TargetCalculationCallbackBeacon)extraData;
						try
						{
							this.InternalOnCalculateTargetBackEndCompleted(targetCalculationCallbackBeacon);
						}
						catch (Exception exception)
						{
							if (!this.HandleBackEndCalculationException(exception, targetCalculationCallbackBeacon.AnchorMailbox, "OnCalculateTargetBackEndCompleted"))
							{
								throw;
							}
						}
						finally
						{
							LatencyTrackerKey trackingKey;
							if (state == ProxyRequestHandler.ProxyState.CalculateBackEnd)
							{
								trackingKey = LatencyTrackerKey.CalculateTargetBackEndLatency;
							}
							else
							{
								trackingKey = LatencyTrackerKey.CalculateTargetBackEndSecondRoundLatency;
							}
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 8, this.LatencyTracker.GetCurrentLatency(trackingKey));
						}
					}
				}
				finally
				{
					this.LogElapsedTime("L_OnCalcTargetBEComp");
				}
			});
		}

		// Token: 0x060006B6 RID: 1718 RVA: 0x00026AD0 File Offset: 0x00024CD0
		private void InternalOnCalculateTargetBackEndCompleted(TargetCalculationCallbackBeacon beacon)
		{
			this.LogElapsedTime("E_IntOnCalcTargetBEComp");
			if (beacon.State == TargetCalculationCallbackState.TargetResolved)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<AnchoredRoutingTarget>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalOnCalculateTargetBackEndCompleted]: Routing target already resolved as {0}", beacon.AnchoredRoutingTarget);
				}
			}
			else
			{
				BackEndServer server = null;
				if (beacon.State == TargetCalculationCallbackState.LocatorCallback)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailbox>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalOnCalculateTargetBackEndCompleted]: Called by MailboxServerLocator async callback with anchor mailbox {0}.", beacon.AnchorMailbox);
					}
					server = this.ProcessMailboxServerLocatorCallBack(beacon.MailboxServerLocatorAsyncResult, beacon.MailboxServerLocatorAsyncState);
				}
				else
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<AnchorMailbox, BackEndServer>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalOnCalculateTargetBackEndCompleted]: Called by self callback with anchor mailbox {0} and mailbox server {1}.", beacon.AnchorMailbox, beacon.MailboxServer);
					}
					server = beacon.MailboxServer;
				}
				if (server.Version < Server.E15MinVersion)
				{
					long num = 0L;
					BackEndServer latency = LatencyTracker.GetLatency<BackEndServer>(() => this.GetDownLevelClientAccessServer(beacon.AnchorMailbox, server), out num);
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "ClientAccessServer", latency.Fqdn);
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "ResolveCasLatency", num);
					server = latency;
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<string, int>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalOnCalculateTargetBackEndCompleted]: Resolved down level Client Access server {0} with version {1}", server.Fqdn, server.Version);
					}
				}
				this.AnchoredRoutingTarget = new AnchoredRoutingTarget(beacon.AnchorMailbox, server);
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<AnchoredRoutingTarget>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalOnCalculateTargetBackEndCompleted]: Will use target {0} for proxy.", this.AnchoredRoutingTarget);
			}
			AnchoredRoutingTarget anchoredRoutingTarget = this.DoProtocolSpecificRoutingTargetOverride(this.AnchoredRoutingTarget);
			if (anchoredRoutingTarget != null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<AnchoredRoutingTarget, AnchoredRoutingTarget>((long)this.GetHashCode(), "[ProxyRequestHandler::InternalOnCalculateTargetBackEndCompleted]: Routing target overridden from {0} to {1}.", this.AnchoredRoutingTarget, anchoredRoutingTarget);
				}
				this.AnchoredRoutingTarget = anchoredRoutingTarget;
			}
			this.AuthBehavior.SetState(this.AnchoredRoutingTarget.BackEndServer.Version);
			this.AlterAuthBehaviorStateForBEAuthTest();
			string text = string.Format("BEVersion-{0}", this.AnchoredRoutingTarget.BackEndServer.Version);
			if (this.AuthBehavior.AuthState == AuthState.FrontEndContinueAuth && !this.AuthBehavior.IsFullyAuthenticated())
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "ContinueAuth", text);
				ThreadPool.QueueUserWorkItem(new WaitCallback(this.BeginContinueOnAuthenticate));
			}
			else
			{
				if (this.AuthBehavior.IsFullyAuthenticated())
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "FEAuth", text);
				}
				else
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "BEAuth", text);
				}
				this.BeginValidateBackendServerCacheOrProxyOrRecalculate();
			}
			this.LogElapsedTime("L_IntOnCalcTargetBEComp");
		}

		// Token: 0x060006B7 RID: 1719 RVA: 0x00026E0B File Offset: 0x0002500B
		private void BeginValidateBackendServerCacheOrProxyOrRecalculate()
		{
			if (this.IsBackendServerCacheValidationEnabled)
			{
				this.BeginValidateBackendServerCache();
				return;
			}
			this.BeginProxyRequestOrRecalculate();
		}

		// Token: 0x060006B8 RID: 1720 RVA: 0x00026E24 File Offset: 0x00025024
		private void ContinueOnAuthenticateCallBack(object extraData)
		{
			if (!this.AuthBehavior.IsFullyAuthenticated())
			{
				this.AuthBehavior.SetFailureStatus();
				this.Complete();
				return;
			}
			if (this.AnchoredRoutingTarget == null && this.AuthBehavior.ShouldDoFullAuthOnUnresolvedAnchorMailbox)
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(this.Logger, "AnchoredRoutingTarget-Null", "BeginCalculateTargetBackEnd");
				ThreadPool.QueueUserWorkItem(new WaitCallback(this.BeginCalculateTargetBackEnd));
				return;
			}
			this.BeginValidateBackendServerCacheOrProxyOrRecalculate();
		}

		// Token: 0x060006B9 RID: 1721 RVA: 0x00026E94 File Offset: 0x00025094
		private BackEndServer ProcessMailboxServerLocatorCallBack(IAsyncResult asyncResult, MailboxServerLocatorAsyncState asyncState)
		{
			this.LogElapsedTime("E_ProcSvrLocCB");
			AnchorMailbox anchorMailbox = asyncState.AnchorMailbox;
			BackEndServer backEndServer = null;
			long failoverSequenceNumber = 0L;
			try
			{
				backEndServer = MailboxServerCache.Instance.ServerLocatorEndGetServer(asyncState.Locator, asyncResult, this);
				MailboxServerCacheEntry mailboxServerCacheEntry;
				if (MailboxServerCache.Instance.TryGet(asyncState.Locator.DatabaseGuid, out mailboxServerCacheEntry))
				{
					failoverSequenceNumber = mailboxServerCacheEntry.FailoverSequenceNumber;
				}
			}
			finally
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 20, string.Join(";", asyncState.Locator.LocatorServiceHosts));
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 19, asyncState.Locator.Latency);
				this.LatencyTracker.HandleGlsLatency(asyncState.Locator.GlsLatencies);
				this.LatencyTracker.HandleResourceLatency(asyncState.Locator.DirectoryLatencies);
			}
			PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorLatency.RawValue = asyncState.Locator.Latency;
			PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorAverageLatency.IncrementBy(asyncState.Locator.Latency);
			PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorAverageLatencyBase.Increment();
			PerfCounters.UpdateMovingAveragePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingAverageMailboxServerLocatorLatency, asyncState.Locator.Latency);
			PerfCounters.IncrementMovingPercentagePerformanceCounterBase(PerfCounters.HttpProxyCountersInstance.MovingPercentageMailboxServerLocatorRetriedCalls);
			if (asyncState.Locator.LocatorServiceHosts.Length > 1)
			{
				PerfCounters.HttpProxyCountersInstance.MailboxServerLocatorRetriedCalls.Increment();
				PerfCounters.UpdateMovingPercentagePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingPercentageMailboxServerLocatorRetriedCalls);
			}
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string, int>((long)this.GetHashCode(), "[ProxyRequestHandler::ProcessMailboxServerLocatorCallBack]: MailboxServerLocator returns server {0} with version {1}.", backEndServer.Fqdn, backEndServer.Version);
			}
			this.SetDatabaseToServerRoutingEntry(asyncState.Locator.DatabaseGuid, asyncState.Locator.ResourceForestFqdn, backEndServer, failoverSequenceNumber);
			this.LogElapsedTime("L_ProcSvrLocCB");
			return backEndServer;
		}

		// Token: 0x060006BA RID: 1722 RVA: 0x00027064 File Offset: 0x00025264
		private void SetDatabaseToServerRoutingEntry(Guid databaseGuid, string resourceForest, BackEndServer server, long failoverSequenceNumber)
		{
			if (server != null && !string.IsNullOrEmpty(resourceForest))
			{
				DatabaseGuidRoutingKey databaseGuidRoutingKey = new DatabaseGuidRoutingKey(databaseGuid, resourceForest, resourceForest);
				ServerRoutingDestination serverRoutingDestination = new ServerRoutingDestination(server.Fqdn, new int?(server.Version));
				this.databaseToServerRoutingEntry = new SuccessfulDatabaseGuidRoutingEntry(databaseGuidRoutingKey, serverRoutingDestination, failoverSequenceNumber);
			}
		}

		// Token: 0x17000173 RID: 371
		// (get) Token: 0x060006BB RID: 1723 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool UseSmartBufferSizing
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000174 RID: 372
		// (get) Token: 0x060006BC RID: 1724 RVA: 0x000270AB File Offset: 0x000252AB
		private bool IsSmartBufferSizingEnabled
		{
			get
			{
				return this.UseSmartBufferSizing && HttpProxySettings.UseSmartBufferSizing.Value;
			}
		}

		// Token: 0x060006BD RID: 1725 RVA: 0x000270C1 File Offset: 0x000252C1
		protected virtual StreamProxy BuildRequestStreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, byte[] buffer)
		{
			return new StreamProxy(streamProxyType, source, target, buffer, this);
		}

		// Token: 0x060006BE RID: 1726 RVA: 0x000270CE File Offset: 0x000252CE
		protected virtual StreamProxy BuildRequestStreamProxySmartSizing(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target)
		{
			return new StreamProxy(streamProxyType, source, target, HttpProxySettings.RequestBufferSize.Value, HttpProxySettings.MinimumRequestBufferSize.Value, this);
		}

		// Token: 0x060006BF RID: 1727 RVA: 0x000270C1 File Offset: 0x000252C1
		protected virtual StreamProxy BuildResponseStreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, byte[] buffer)
		{
			return new StreamProxy(streamProxyType, source, target, buffer, this);
		}

		// Token: 0x060006C0 RID: 1728 RVA: 0x000270ED File Offset: 0x000252ED
		protected virtual StreamProxy BuildResponseStreamProxySmartSizing(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target)
		{
			return new StreamProxy(streamProxyType, source, target, HttpProxySettings.ResponseBufferSize.Value, HttpProxySettings.MinimumResponseBufferSize.Value, this);
		}

		// Token: 0x060006C1 RID: 1729 RVA: 0x0002710C File Offset: 0x0002530C
		protected virtual BufferPool GetRequestBufferPool()
		{
			return BufferPoolCollection.AutoCleanupCollection.Acquire(HttpProxySettings.RequestBufferSize.Value);
		}

		// Token: 0x060006C2 RID: 1730 RVA: 0x00027122 File Offset: 0x00025322
		protected virtual BufferPool GetResponseBufferPool()
		{
			return BufferPoolCollection.AutoCleanupCollection.Acquire(HttpProxySettings.ResponseBufferSize.Value);
		}

		// Token: 0x060006C3 RID: 1731 RVA: 0x00027138 File Offset: 0x00025338
		private void BeginRequestStreaming()
		{
			this.LogElapsedTime("E_BegReqStrm");
			try
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, ProxyRequestHandler.ProxyState>((long)this.GetHashCode(), "[ProxyRequestHandler::BeginRequestStreaming]: Context {0}; State {1}", this.TraceContext, this.State);
				}
				if (this.requestStreamProxy == null)
				{
					this.InitializeRequestBufferPool();
					if (this.IsSmartBufferSizingEnabled)
					{
						this.requestStreamProxy = this.BuildRequestStreamProxySmartSizing(StreamProxy.StreamProxyType.Request, this.ClientRequestStream, this.ServerRequestStream);
					}
					else
					{
						this.requestStreamProxy = this.BuildRequestStreamProxy(StreamProxy.StreamProxyType.Request, this.ClientRequestStream, this.ServerRequestStream, this.requestStreamBufferPool.AcquireBuffer());
					}
				}
				else
				{
					if (this.requestStreamProxy.NumberOfReadsCompleted > 1L)
					{
						this.Logger.AppendGenericError("CannotReplay-NumReads/TotalBytes", this.requestStreamProxy.NumberOfReadsCompleted.ToString() + "/" + this.requestStreamProxy.TotalBytesProxied.ToString());
						throw new HttpProxyException(HttpStatusCode.InternalServerError, 5001, "Cannot replay request where the number of initial reads wasn't 1.");
					}
					this.requestStreamProxy.SetTargetStreamForBufferedSend(this.ServerRequestStream);
				}
				try
				{
					this.requestStreamProxy.BeginProcess(new AsyncCallback(this.RequestStreamProxyCompleted), this);
				}
				catch (StreamProxyException exception)
				{
					this.HandleStreamProxyError(exception, this.requestStreamProxy);
				}
			}
			finally
			{
				this.LogElapsedTime("L_BegReqStrm");
			}
		}

		// Token: 0x060006C4 RID: 1732 RVA: 0x000272BC File Offset: 0x000254BC
		private void BeginResponseStreaming()
		{
			this.LogElapsedTime("E_BegRespStrm");
			try
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, ProxyRequestHandler.ProxyState>((long)this.GetHashCode(), "[ProxyRequestHandler::BeginResponseStreaming]: Context {0}; State {1}", this.TraceContext, this.State);
				}
				this.InitializeResponseBufferPool();
				if (this.IsSmartBufferSizingEnabled)
				{
					this.responseStreamProxy = this.BuildResponseStreamProxySmartSizing(StreamProxy.StreamProxyType.Response, this.ServerResponse.GetResponseStream(), this.ClientResponse.OutputStream);
				}
				else
				{
					this.responseStreamProxy = this.BuildResponseStreamProxy(StreamProxy.StreamProxyType.Response, this.ServerResponse.GetResponseStream(), this.ClientResponse.OutputStream, this.responseStreamBufferPool.AcquireBuffer());
				}
				this.responseStreamProxy.AuxTargetStream = this.captureResponseStream;
				this.State = ProxyRequestHandler.ProxyState.ProxyResponseData;
				try
				{
					this.responseStreamProxy.BeginProcess(new AsyncCallback(this.ResponseStreamProxyCompleted), this);
				}
				catch (StreamProxyException exception)
				{
					this.HandleStreamProxyError(exception, this.responseStreamProxy);
				}
			}
			finally
			{
				this.LogElapsedTime("L_BegRespStrm");
			}
		}

		// Token: 0x060006C5 RID: 1733 RVA: 0x000273D0 File Offset: 0x000255D0
		private void InitializeRequestBufferPool()
		{
			this.LogElapsedTime("E_InitReqBufPool");
			try
			{
				if (!this.requestBufferInitialized)
				{
					if (!this.IsSmartBufferSizingEnabled)
					{
						if (this.ClientRequest.IsRequestChunked() || (long)this.ClientRequest.ContentLength >= HttpProxySettings.RequestBufferBoundary.Member)
						{
							this.requestStreamBufferPool = new ProxyRequestHandler.BufferPoolWithBuffer(this.GetRequestBufferPool());
						}
						else
						{
							BufferPoolCollection.BufferSize bufferSize;
							if (!BufferPoolCollection.AutoCleanupCollection.TryMatchBufferSize(this.ClientRequest.ContentLength, ref bufferSize))
							{
								throw new InvalidOperationException("Failed to get buffer size for request stream buffer.");
							}
							this.requestStreamBufferPool = new ProxyRequestHandler.BufferPoolWithBuffer(bufferSize);
						}
					}
					this.requestBufferInitialized = true;
				}
			}
			finally
			{
				this.LogElapsedTime("L_InitReqBufPool");
			}
		}

		// Token: 0x060006C6 RID: 1734 RVA: 0x0002748C File Offset: 0x0002568C
		private void InitializeResponseBufferPool()
		{
			this.LogElapsedTime("E_InitRespBufPool");
			try
			{
				if (!this.responseBufferInitialized)
				{
					bool flag = false;
					if (this.ShouldForceUnbufferedClientResponseOutput || this.ServerResponse.IsChunkedResponse())
					{
						this.ClientResponse.BufferOutput = false;
						flag = true;
					}
					if (!this.IsSmartBufferSizingEnabled)
					{
						if (flag)
						{
							this.responseStreamBufferPool = new ProxyRequestHandler.BufferPoolWithBuffer(this.GetResponseBufferPool());
						}
						else if (this.ServerResponse.ContentLength >= HttpProxySettings.ResponseBufferBoundary.Member)
						{
							this.responseStreamBufferPool = new ProxyRequestHandler.BufferPoolWithBuffer(HttpProxySettings.ResponseBufferSize.Value);
						}
						else
						{
							BufferPoolCollection.BufferSize bufferSize;
							if (!BufferPoolCollection.AutoCleanupCollection.TryMatchBufferSize((int)this.ServerResponse.ContentLength, ref bufferSize))
							{
								throw new InvalidOperationException("Could not get buffer size for response stream buffer.");
							}
							this.responseStreamBufferPool = new ProxyRequestHandler.BufferPoolWithBuffer(bufferSize);
						}
					}
					this.responseBufferInitialized = true;
				}
			}
			finally
			{
				this.LogElapsedTime("L_InitRespBufPool");
			}
		}

		// Token: 0x060006C7 RID: 1735 RVA: 0x00027578 File Offset: 0x00025778
		[Conditional("DEBUG")]
		private void InitializeCaptureResponseStream()
		{
			if (!string.IsNullOrEmpty(HttpProxySettings.CaptureResponsesLocation.Value) && string.IsNullOrEmpty(this.ClientRequest.GetTestBackEndUrl()))
			{
				string text = this.ClientRequest.Headers[Constants.CaptureResponseIdHeaderKey];
				if (!string.IsNullOrEmpty(text))
				{
					string path = Path.Combine(HttpProxySettings.CaptureResponsesLocation.Value, text + ".header");
					object multiRequestLockObject = ProxyRequestHandler.MultiRequestLockObject;
					lock (multiRequestLockObject)
					{
						if (!File.Exists(path))
						{
							using (StreamWriter streamWriter = new StreamWriter(path))
							{
								for (int i = 0; i < this.ServerResponse.Headers.Count; i++)
								{
									streamWriter.Write(this.ServerResponse.Headers.Keys[i]);
									streamWriter.Write(": ");
									streamWriter.Write(this.ServerResponse.Headers[i]);
									streamWriter.Write(Environment.NewLine);
								}
								streamWriter.Flush();
							}
							string path2 = Path.Combine(HttpProxySettings.CaptureResponsesLocation.Value, text + ".txt");
							this.captureResponseStream = File.OpenWrite(path2);
						}
					}
				}
			}
		}

		// Token: 0x060006C8 RID: 1736 RVA: 0x000276E4 File Offset: 0x000258E4
		private void RequestStreamProxyCompleted(IAsyncResult result)
		{
			this.CallThreadEntranceMethod(delegate
			{
				this.LogElapsedTime("E_ReqStrmProxyComp");
				try
				{
					object obj = this.lockObject;
					lock (obj)
					{
						try
						{
							this.requestStreamProxy.EndProcess(result);
						}
						catch (StreamProxyException exception)
						{
							if (!this.HandleStreamProxyError(exception, this.requestStreamProxy))
							{
								return;
							}
						}
						finally
						{
							long totalBytesProxied = this.requestStreamProxy.TotalBytesProxied;
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
							{
								ExTraceGlobals.VerboseTracer.TraceDebug<int, long>((long)this.GetHashCode(), "[ProxyRequestHandler::RequestStreamProxyCompleted]: Context {0}; Bytes copied {1}", this.TraceContext, totalBytesProxied);
							}
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 2, totalBytesProxied);
						}
						try
						{
							Stream serverRequestStream = this.ServerRequestStream;
							this.ServerRequestStream = null;
							serverRequestStream.Flush();
							serverRequestStream.Dispose();
							if (!this.ShouldRetryOnError)
							{
								ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ClientRequestStream);
								this.ClientRequestStream = null;
							}
							this.BeginGetServerResponse();
						}
						catch (WebException ex)
						{
							this.CompleteWithError(ex, "RequestStreamProxyCompleted");
						}
						catch (HttpException ex2)
						{
							this.CompleteWithError(ex2, "RequestStreamProxyCompleted");
						}
						catch (HttpProxyException ex3)
						{
							this.CompleteWithError(ex3, "RequestStreamProxyCompleted");
						}
						catch (IOException ex4)
						{
							this.CompleteWithError(ex4, "RequestStreamProxyCompleted");
						}
					}
				}
				finally
				{
					this.LogElapsedTime("L_ReqStrmProxyComp");
				}
			});
		}

		// Token: 0x060006C9 RID: 1737 RVA: 0x00027718 File Offset: 0x00025918
		private void ResponseStreamProxyCompleted(IAsyncResult result)
		{
			this.CallThreadEntranceMethod(delegate
			{
				this.LogElapsedTime("E_RespStrmProxyComp");
				try
				{
					object obj = this.lockObject;
					lock (obj)
					{
						try
						{
							this.responseStreamProxy.EndProcess(result);
						}
						catch (StreamProxyException exception)
						{
							if (!this.HandleStreamProxyError(exception, this.responseStreamProxy))
							{
								return;
							}
						}
						finally
						{
							long totalBytesProxied = this.responseStreamProxy.TotalBytesProxied;
							if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
							{
								ExTraceGlobals.VerboseTracer.TraceDebug<int, long>((long)this.GetHashCode(), "[ProxyRequestHandler::ResponseStreamProxyCompleted]: Context {0}; Bytes copied {1}", this.TraceContext, totalBytesProxied);
							}
							RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(this.Logger, 3, totalBytesProxied);
						}
						this.Complete();
					}
				}
				finally
				{
					this.LogElapsedTime("L_RespStrmProxyComp");
				}
			});
		}

		// Token: 0x060006CA RID: 1738 RVA: 0x0002774C File Offset: 0x0002594C
		private bool HandleStreamProxyError(StreamProxyException exception, StreamProxy streamProxy)
		{
			this.LogElapsedTime("E_HandleSPErr");
			bool result;
			try
			{
				Exception innerException = exception.InnerException;
				string text = string.Format("StreamProxy-{0}-{1}", streamProxy.ProxyType, streamProxy.StreamState);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, string, Exception>((long)this.GetHashCode(), "[ProxyRequestHandler::HandleStreamProxyError]: Context {0}; Handling StreamProxy error at label {1}. Exception: {2}", this.TraceContext, text, innerException);
				}
				if (streamProxy.ProxyType == StreamProxy.StreamProxyType.Request && this.TryHandleProtocolSpecificRequestErrors(exception.InnerException))
				{
					result = true;
				}
				else
				{
					this.Logger.AppendGenericError("StreamProxy", text);
					this.CompleteWithError(innerException, text);
					result = false;
				}
			}
			finally
			{
				this.LogElapsedTime("L_HandleSPErr");
			}
			return result;
		}

		// Token: 0x060006CB RID: 1739 RVA: 0x0002780C File Offset: 0x00025A0C
		private void CleanUpRequestStreamsAndBuffer()
		{
			this.LogElapsedTime("E_CleanUpReqBuf");
			if (this.requestStreamBufferPool != null)
			{
				this.requestStreamBufferPool.Release();
				this.requestStreamBufferPool = null;
			}
			ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.requestStreamProxy);
			this.requestStreamProxy = null;
			ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.bufferedRegionStream);
			this.bufferedRegionStream = null;
			ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.ClientRequestStream);
			this.ClientRequestStream = null;
			this.LogElapsedTime("L_CleanUpReqBuf");
		}

		// Token: 0x060006CC RID: 1740 RVA: 0x00027880 File Offset: 0x00025A80
		private void CleanUpResponseStreamsAndBuffer()
		{
			this.LogElapsedTime("E_CleanUpRespBuf");
			if (this.responseStreamBufferPool != null)
			{
				this.responseStreamBufferPool.Release();
				this.responseStreamBufferPool = null;
			}
			ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.responseStreamProxy);
			this.responseStreamProxy = null;
			ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.captureResponseStream);
			this.captureResponseStream = null;
			this.LogElapsedTime("L_CleanUpRespBuf");
		}

		// Token: 0x060006CD RID: 1741 RVA: 0x000278E4 File Offset: 0x00025AE4
		internal void CompleteForLocalProbe()
		{
			int value = HttpProxySettings.DelayProbeResponseSeconds.Value;
			if (value > 0)
			{
				Thread.Sleep(TimeSpan.FromSeconds((double)value));
			}
			this.SetResponseStatusIfHeadersUnsent(this.ClientResponse, 200);
			this.Complete();
		}

		// Token: 0x060006CE RID: 1742 RVA: 0x00003165 File Offset: 0x00001365
		protected virtual bool ProtocolSupportsWebSocket()
		{
			return false;
		}

		// Token: 0x060006CF RID: 1743 RVA: 0x00027923 File Offset: 0x00025B23
		private void ProcessWebSocketRequest(HttpContext context)
		{
			this.serverRequestHeaders = this.ServerRequest.Headers;
			context.AcceptWebSocketRequest(new Func<AspNetWebSocketContext, Task>(this.HandleWebSocket));
			this.Complete();
		}

		// Token: 0x060006D0 RID: 1744 RVA: 0x00027950 File Offset: 0x00025B50
		private async Task ConnectToBackend(Uri uri, CancellationToken cancellationToken)
		{
			if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
			{
				ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[ProxyRequestHandler::ConnectToBackend]: Connecting to {0} via WebSocket protocol.", uri.ToString());
			}
			this.backendSocket = new ClientWebSocket();
			this.backendSocket.Options.UseDefaultCredentials = true;
			for (int i = 0; i < this.serverRequestHeaders.Count; i++)
			{
				string key = this.serverRequestHeaders.GetKey(i);
				string[] values = this.serverRequestHeaders.GetValues(i);
				if (values != null)
				{
					foreach (string headerValue in values)
					{
						if (!ProxyRequestHandler.FrontEndHeaders.Contains(key))
						{
							this.backendSocket.Options.SetRequestHeader(key, headerValue);
						}
					}
				}
			}
			try
			{
				await this.backendSocket.ConnectAsync(uri, cancellationToken);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[ProxyRequestHandler::ConnectToBackend]: Successfully connected to {0} via WebSocket protocol.", uri.ToString());
				}
			}
			catch (Exception)
			{
				if (this.timeoutCancellationTokenSource.IsCancellationRequested)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[ProxyRequestHandler::ConnectToBackend]: Timed out trying to connect to {0} via WebSocket protocol.", uri.ToString());
					}
					this.CleanupWebSocket(true);
				}
				else
				{
					if (!this.disposeCancellationTokenSource.IsCancellationRequested)
					{
						throw;
					}
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError((long)this.GetHashCode(), "[ProxyRequestHandler::ConnectToBackend]: Proxy request handler has been disposed.");
					}
					this.CleanupWebSocket(false);
				}
			}
		}

		// Token: 0x060006D1 RID: 1745 RVA: 0x000279A8 File Offset: 0x00025BA8
		private async Task HandleWebSocket(WebSocketContext webSocketContext)
		{
			try
			{
				this.disposeCancellationTokenSource = new CancellationTokenSource();
				this.timeoutCancellationTokenSource = new CancellationTokenSource();
				await this.HandleWebSocketInternal((AspNetWebSocketContext)webSocketContext);
			}
			catch (Exception source)
			{
				ExceptionDispatchInfo exceptionDispatchInfo = ExceptionDispatchInfo.Capture(source);
				Diagnostics.SendWatsonReportOnUnhandledException(delegate()
				{
					exceptionDispatchInfo.Throw();
				});
			}
			finally
			{
				Diagnostics.SendWatsonReportOnUnhandledException(delegate()
				{
					this.CleanupWebSocket(false);
				});
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.disposeCancellationTokenSource);
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.timeoutCancellationTokenSource);
				ProxyRequestHandler.DisposeIfNotNullAndCatchExceptions(this.cancellationTokenSource);
			}
		}

		// Token: 0x060006D2 RID: 1746 RVA: 0x000279F8 File Offset: 0x00025BF8
		private async Task HandleWebSocketInternal(AspNetWebSocketContext webSocketContext)
		{
			this.clientSocket = webSocketContext.WebSocket;
			this.timeoutCancellationTokenSource.CancelAfter(ProxyRequestHandler.Timeout);
			this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.timeoutCancellationTokenSource.Token, this.disposeCancellationTokenSource.Token);
			Uri targetBackEndServerUrl = this.GetTargetBackEndServerUrl();
			await this.ConnectToBackend(new UriBuilder(targetBackEndServerUrl)
			{
				Scheme = ((StringComparer.OrdinalIgnoreCase.Compare(targetBackEndServerUrl.Scheme, "https") == 0) ? "wss" : "ws")
			}.Uri, this.cancellationTokenSource.Token);
			this.bufferPool = this.GetBufferPool(ProxyRequestHandler.MaxMessageSize.Value);
			this.clientBuffer = this.bufferPool.Acquire();
			this.backendBuffer = this.bufferPool.Acquire();
			await Task.WhenAll(new List<Task>
			{
				this.ProxyWebSocketData(this.clientSocket, this.backendSocket, this.clientBuffer, true, this.cancellationTokenSource.Token).ContinueWith<Task>(async delegate(Task _)
				{
					if (this.backendSocket.State == WebSocketState.Open)
					{
						await this.backendSocket.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, string.Empty, this.cancellationTokenSource.Token);
					}
				}),
				this.ProxyWebSocketData(this.backendSocket, this.clientSocket, this.backendBuffer, false, this.cancellationTokenSource.Token).ContinueWith<Task>(async delegate(Task _)
				{
					if (this.clientSocket.State == WebSocketState.Open)
					{
						await this.clientSocket.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, string.Empty, this.cancellationTokenSource.Token);
					}
				})
			});
		}

		// Token: 0x060006D3 RID: 1747 RVA: 0x00027A48 File Offset: 0x00025C48
		private async Task ProxyWebSocketData(WebSocket receiveSocket, WebSocket sendSocket, byte[] receiveBuffer, bool fromClientToBackend, CancellationToken cancellationToken)
		{
			ProxyRequestHandler.<>c__DisplayClass334_0 CS$<>8__locals1 = new ProxyRequestHandler.<>c__DisplayClass334_0();
			CS$<>8__locals1.receiveSocket = receiveSocket;
			CS$<>8__locals1.receiveBuffer = receiveBuffer;
			CS$<>8__locals1.cancellationToken = cancellationToken;
			while (CS$<>8__locals1.receiveSocket.State == WebSocketState.Open && sendSocket.State == WebSocketState.Open)
			{
				ProxyRequestHandler.<>c__DisplayClass334_1 CS$<>8__locals2 = new ProxyRequestHandler.<>c__DisplayClass334_1();
				CS$<>8__locals2.CS$<>8__locals1 = CS$<>8__locals1;
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string>((long)this.GetHashCode(), "[ProxyRequestHandler::ProxyWebSocketData]: Receiving WebSocket data from the {0}.", fromClientToBackend ? "client" : "backend");
				}
				Func<Task<WebSocketReceiveResult>> func;
				if ((func = CS$<>8__locals2.CS$<>8__locals1.<>9__0) == null)
				{
					ProxyRequestHandler.<>c__DisplayClass334_0 CS$<>8__locals3 = CS$<>8__locals2.CS$<>8__locals1;
					Func<Task<WebSocketReceiveResult>> func2 = async () => await CS$<>8__locals2.CS$<>8__locals1.receiveSocket.ReceiveAsync(new ArraySegment<byte>(CS$<>8__locals2.CS$<>8__locals1.receiveBuffer), CS$<>8__locals2.CS$<>8__locals1.cancellationToken);
					CS$<>8__locals3.<>9__0 = func2;
					func = func2;
				}
				WebSocketReceiveResult webSocketReceiveResult = await this.ReceiveAsyncWithTryCatch(func);
				WebSocketReceiveResult result = webSocketReceiveResult;
				if (result != null)
				{
					if (result.MessageType != WebSocketMessageType.Close)
					{
						CS$<>8__locals2.count = result.Count;
						while (!result.EndOfMessage)
						{
							if (CS$<>8__locals2.count >= ProxyRequestHandler.MaxMessageSize.Value)
							{
								string statusDescription = string.Format("Maximum message size: {0} bytes.", ProxyRequestHandler.MaxMessageSize.Value);
								await Task.WhenAll(new Task[]
								{
									CS$<>8__locals2.CS$<>8__locals1.receiveSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, statusDescription, CS$<>8__locals2.CS$<>8__locals1.cancellationToken),
									sendSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, statusDescription, CS$<>8__locals2.CS$<>8__locals1.cancellationToken)
								});
								return;
							}
							webSocketReceiveResult = await this.ReceiveAsyncWithTryCatch(async () => await CS$<>8__locals2.CS$<>8__locals1.receiveSocket.ReceiveAsync(new ArraySegment<byte>(CS$<>8__locals2.CS$<>8__locals1.receiveBuffer, CS$<>8__locals2.count, ProxyRequestHandler.MaxMessageSize.Value - CS$<>8__locals2.count), CS$<>8__locals2.CS$<>8__locals1.cancellationToken));
							result = webSocketReceiveResult;
							if (result == null)
							{
								return;
							}
							CS$<>8__locals2.count += result.Count;
						}
						if (result.MessageType == WebSocketMessageType.Text)
						{
							await this.SendWebSocketMessage(CS$<>8__locals2.CS$<>8__locals1.receiveBuffer, CS$<>8__locals2.count, sendSocket, WebSocketMessageType.Text, CS$<>8__locals2.CS$<>8__locals1.cancellationToken);
						}
						else
						{
							if (result.MessageType != WebSocketMessageType.Binary)
							{
								throw new NotImplementedException(string.Format("Unexpected WebSocket message type: {0}.", result.MessageType));
							}
							await this.SendWebSocketMessage(CS$<>8__locals2.CS$<>8__locals1.receiveBuffer, CS$<>8__locals2.count, sendSocket, WebSocketMessageType.Binary, CS$<>8__locals2.CS$<>8__locals1.cancellationToken);
						}
						CS$<>8__locals2 = null;
						result = null;
						continue;
					}
					await Task.WhenAll(new Task[]
					{
						CS$<>8__locals2.CS$<>8__locals1.receiveSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CS$<>8__locals2.CS$<>8__locals1.cancellationToken),
						sendSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CS$<>8__locals2.CS$<>8__locals1.cancellationToken)
					});
				}
				return;
			}
		}

		// Token: 0x060006D4 RID: 1748 RVA: 0x00027AB8 File Offset: 0x00025CB8
		private async Task SendWebSocketMessage(byte[] receiveBuffer, int bytesCount, WebSocket sendSocket, WebSocketMessageType webSocketMessageType, CancellationToken cancellationToken)
		{
			ArraySegment<byte> outputBuffer = new ArraySegment<byte>(receiveBuffer, 0, bytesCount);
			await this.SendAsyncWithTryCatch(async delegate
			{
				await sendSocket.SendAsync(outputBuffer, webSocketMessageType, true, cancellationToken);
			});
		}

		// Token: 0x060006D5 RID: 1749 RVA: 0x00027B28 File Offset: 0x00025D28
		private async Task<WebSocketReceiveResult> ReceiveAsyncWithTryCatch(Func<Task<WebSocketReceiveResult>> func)
		{
			try
			{
				return await func();
			}
			catch (InvalidOperationException ex)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[ProxyRequestHandler::ProxyWebSocketData]: ReceiveAsync failed: {0}.", ex.Message);
				}
			}
			catch (WebSocketException ex2)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[ProxyRequestHandler::ProxyWebSocketData]: ReceiveAsync failed: {0}.", ex2.Message);
				}
			}
			catch (Exception)
			{
				if (this.timeoutCancellationTokenSource.IsCancellationRequested)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError((long)this.GetHashCode(), "[ProxyRequestHandler::ProxyWebSocketData]: Timed out trying to read data.");
					}
					this.CleanupWebSocket(true);
				}
				else
				{
					if (!this.disposeCancellationTokenSource.IsCancellationRequested)
					{
						throw;
					}
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError((long)this.GetHashCode(), "[ProxyRequestHandler::ProxyWebSocketData]: Proxy request handler has been disposed.");
					}
					this.CleanupWebSocket(false);
				}
			}
			return null;
		}

		// Token: 0x060006D6 RID: 1750 RVA: 0x00027B78 File Offset: 0x00025D78
		private async Task SendAsyncWithTryCatch(Func<Task> func)
		{
			try
			{
				await func();
			}
			catch (WebSocketException ex)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError<string>((long)this.GetHashCode(), "[ProxyRequestHandler::ProxyWebSocketData]: SendAsync failed: {0}.", ex.Message);
				}
			}
			catch (Exception)
			{
				if (this.timeoutCancellationTokenSource.IsCancellationRequested)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError((long)this.GetHashCode(), "[ProxyRequestHandler::ProxyWebSocketData]: Timed out trying to send data.");
					}
					this.CleanupWebSocket(true);
				}
				else
				{
					if (!this.disposeCancellationTokenSource.IsCancellationRequested)
					{
						throw;
					}
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError((long)this.GetHashCode(), "[ProxyRequestHandler::ProxyWebSocketData]: Proxy request handler has been disposed.");
					}
					this.CleanupWebSocket(false);
				}
			}
		}

		// Token: 0x060006D7 RID: 1751 RVA: 0x00027BC8 File Offset: 0x00025DC8
		private void CleanupWebSocket(bool timedOut)
		{
			try
			{
				if (this.backendSocket != null && this.backendSocket.State != WebSocketState.Closed)
				{
					this.backendSocket.CloseAsync(timedOut ? WebSocketCloseStatus.EndpointUnavailable : WebSocketCloseStatus.NormalClosure, string.Empty, this.timeoutCancellationTokenSource.Token).Wait((int)ProxyRequestHandler.Timeout.TotalMilliseconds, this.timeoutCancellationTokenSource.Token);
				}
				if (this.clientSocket != null && this.clientSocket.State != WebSocketState.Closed)
				{
					this.clientSocket.CloseAsync(timedOut ? WebSocketCloseStatus.EndpointUnavailable : WebSocketCloseStatus.NormalClosure, string.Empty, this.timeoutCancellationTokenSource.Token).Wait((int)ProxyRequestHandler.Timeout.TotalMilliseconds, this.timeoutCancellationTokenSource.Token);
				}
			}
			catch (Exception)
			{
				if (this.timeoutCancellationTokenSource.IsCancellationRequested && ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
				{
					ExTraceGlobals.VerboseTracer.TraceError((long)this.GetHashCode(), "[ProxyRequestHandler::CleanupWebSocket]: Timed out trying to close WebSocket connections.");
				}
			}
			if (this.disposeCancellationTokenSource != null)
			{
				this.disposeCancellationTokenSource.Cancel();
			}
			if (this.clientBuffer != null)
			{
				this.bufferPool.Release(this.clientBuffer);
				this.clientBuffer = null;
			}
			if (this.backendBuffer != null)
			{
				this.bufferPool.Release(this.backendBuffer);
				this.backendBuffer = null;
			}
			if (this.backendSocket != null)
			{
				this.backendSocket.Dispose();
				this.backendSocket = null;
			}
		}

		// Token: 0x0400039D RID: 925
		public const string CanaryKey = "msExchEcpCanary";

		// Token: 0x0400039E RID: 926
		private static readonly object MultiRequestLockObject = new object();

		// Token: 0x0400039F RID: 927
		private static readonly HashSet<string> RestrictedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"set-cookie",
			"server",
			"x-powered-by",
			"x-aspnet-version",
			"www-authenticate",
			"persistent-auth",
			Constants.BEServerExceptionHeaderName,
			Constants.BEServerRoutingErrorHeaderName,
			"X-MSExchangeActivityCtx",
			"request-id",
			"client-request-id",
			Constants.BEResourcePath,
			"X-FromBackend-ServerAffinity",
			"X-DBMountedOnServer",
			"X-RoutingEntryUpdate"
		};

		// Token: 0x040003A0 RID: 928
		private readonly object lockObject = new object();

		// Token: 0x040003A1 RID: 929
		private AsyncCallback asyncCallback;

		// Token: 0x040003A2 RID: 930
		private object asyncState;

		// Token: 0x040003A3 RID: 931
		private ManualResetEvent completedWaitHandle;

		// Token: 0x040003A4 RID: 932
		private Exception asyncException;

		// Token: 0x040003A5 RID: 933
		private DisposeTracker disposeTracker;

		// Token: 0x040003A6 RID: 934
		private bool haveStartedOutOfBandProxyLogon;

		// Token: 0x040003A7 RID: 935
		private bool haveReceivedAuthChallenge;

		// Token: 0x040003A8 RID: 936
		private int retryOnErrorCounter;

		// Token: 0x040003A9 RID: 937
		private BufferedRegionStream bufferedRegionStream;

		// Token: 0x040003AA RID: 938
		private AuthenticationContext authenticationContext;

		// Token: 0x040003AB RID: 939
		private string kerberosChallenge;

		// Token: 0x040003AC RID: 940
		private int delayOnRetryOnError;

		// Token: 0x040003AD RID: 941
		private RequestState requestState;

		// Token: 0x040003AE RID: 942
		private bool disposed;

		// Token: 0x040003C3 RID: 963
		private const int MaxStatusDescriptionLength = 512;

		// Token: 0x040003C4 RID: 964
		protected const int E12E14TargetPort = 443;

		// Token: 0x040003C5 RID: 965
		protected const int TargetPort = 444;

		// Token: 0x040003C6 RID: 966
		private const string RoutingErrorLogString = "RoutingError";

		// Token: 0x040003C7 RID: 967
		private MailboxServerLocator mailboxServerLocator;

		// Token: 0x040003C8 RID: 968
		private DatacenterRedirectStrategy datacenterRedirectStrategy;

		// Token: 0x040003C9 RID: 969
		private DatabaseBasedAnchorMailbox fallbackAnchorMailbox;

		// Token: 0x040003CA RID: 970
		private SuccessfulDatabaseGuidRoutingEntry databaseToServerRoutingEntry;

		// Token: 0x040003CD RID: 973
		private StreamProxy requestStreamProxy;

		// Token: 0x040003CE RID: 974
		private ProxyRequestHandler.BufferPoolWithBuffer requestStreamBufferPool;

		// Token: 0x040003CF RID: 975
		private bool requestBufferInitialized;

		// Token: 0x040003D0 RID: 976
		private StreamProxy responseStreamProxy;

		// Token: 0x040003D1 RID: 977
		private ProxyRequestHandler.BufferPoolWithBuffer responseStreamBufferPool;

		// Token: 0x040003D2 RID: 978
		private bool responseBufferInitialized;

		// Token: 0x040003D3 RID: 979
		private FileStream captureResponseStream;

		// Token: 0x040003D4 RID: 980
		private static readonly HashSet<string> FrontEndHeaders = new HashSet<string>
		{
			"Connection",
			"Upgrade",
			"Sec-WebSocket-Key",
			"Sec-WebSocket-Version"
		};

		// Token: 0x040003D5 RID: 981
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds((double)new IntAppSettingsEntry("HttpProxy.WebSocketTimeout", 60, ExTraceGlobals.VerboseTracer).Value);

		// Token: 0x040003D6 RID: 982
		private static readonly IntAppSettingsEntry MaxMessageSize = new IntAppSettingsEntry("HttpProxy.MaxWebSocketMessageSize", 16384, ExTraceGlobals.VerboseTracer);

		// Token: 0x040003D7 RID: 983
		private WebSocket clientSocket;

		// Token: 0x040003D8 RID: 984
		private ClientWebSocket backendSocket;

		// Token: 0x040003D9 RID: 985
		private BufferPool bufferPool;

		// Token: 0x040003DA RID: 986
		private byte[] clientBuffer;

		// Token: 0x040003DB RID: 987
		private byte[] backendBuffer;

		// Token: 0x040003DC RID: 988
		private CancellationTokenSource timeoutCancellationTokenSource;

		// Token: 0x040003DD RID: 989
		private CancellationTokenSource disposeCancellationTokenSource;

		// Token: 0x040003DE RID: 990
		private CancellationTokenSource cancellationTokenSource;

		// Token: 0x040003DF RID: 991
		private WebHeaderCollection serverRequestHeaders;

		// Token: 0x02000135 RID: 309
		public enum SupportBackEndCookie
		{
			// Token: 0x04000584 RID: 1412
			V1 = 1,
			// Token: 0x04000585 RID: 1413
			V2,
			// Token: 0x04000586 RID: 1414
			All
		}

		// Token: 0x02000136 RID: 310
		protected enum ProxyState
		{
			// Token: 0x04000588 RID: 1416
			None,
			// Token: 0x04000589 RID: 1417
			Initializing,
			// Token: 0x0400058A RID: 1418
			CalculateBackEnd,
			// Token: 0x0400058B RID: 1419
			CalculateBackEndSecondRound,
			// Token: 0x0400058C RID: 1420
			PrepareServerRequest,
			// Token: 0x0400058D RID: 1421
			ProxyRequestData,
			// Token: 0x0400058E RID: 1422
			WaitForServerResponse,
			// Token: 0x0400058F RID: 1423
			ProxyResponseData,
			// Token: 0x04000590 RID: 1424
			Completed,
			// Token: 0x04000591 RID: 1425
			CleanedUp,
			// Token: 0x04000592 RID: 1426
			WaitForProxyLogonRequestStream,
			// Token: 0x04000593 RID: 1427
			WaitForProxyLogonResponse,
			// Token: 0x04000594 RID: 1428
			ProxyWebSocketData
		}

		// Token: 0x02000137 RID: 311
		public class BufferPoolWithBuffer
		{
			// Token: 0x06000890 RID: 2192 RVA: 0x0002EACC File Offset: 0x0002CCCC
			public BufferPoolWithBuffer(BufferPoolCollection.BufferSize bufferSize)
			{
				this.bufferPool = BufferPoolCollection.AutoCleanupCollection.Acquire(bufferSize);
			}

			// Token: 0x06000891 RID: 2193 RVA: 0x0002EAE5 File Offset: 0x0002CCE5
			public BufferPoolWithBuffer(BufferPool bufferPool)
			{
				this.bufferPool = bufferPool;
			}

			// Token: 0x06000892 RID: 2194 RVA: 0x0002EAF4 File Offset: 0x0002CCF4
			public byte[] AcquireBuffer()
			{
				if (this.buffer == null)
				{
					this.buffer = this.bufferPool.Acquire();
				}
				return this.buffer;
			}

			// Token: 0x06000893 RID: 2195 RVA: 0x0002EB18 File Offset: 0x0002CD18
			public void Release()
			{
				if (this.buffer == null)
				{
					return;
				}
				try
				{
					this.bufferPool.Release(this.buffer);
					this.buffer = null;
					this.bufferPool = null;
				}
				catch (Exception)
				{
				}
			}

			// Token: 0x04000595 RID: 1429
			private BufferPool bufferPool;

			// Token: 0x04000596 RID: 1430
			private byte[] buffer;
		}
	}
}
