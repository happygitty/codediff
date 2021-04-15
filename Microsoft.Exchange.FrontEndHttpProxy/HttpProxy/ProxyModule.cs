using System;
using System.Security.Principal;
using System.Web;
using Microsoft.Exchange.Clients.Common;
using Microsoft.Exchange.Common;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.Diagnostics.Components.Security;
using Microsoft.Exchange.Diagnostics.WorkloadManagement;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net;
using Microsoft.Exchange.Net.Protocols;
using Microsoft.Exchange.Net.Wopi;
using Microsoft.Exchange.Security.Authentication;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x020000AD RID: 173
	public class ProxyModule : IHttpModule
	{
		// Token: 0x17000142 RID: 322
		// (get) Token: 0x060005EC RID: 1516 RVA: 0x00021106 File Offset: 0x0001F306
		// (set) Token: 0x060005ED RID: 1517 RVA: 0x0002110E File Offset: 0x0001F30E
		internal PfdTracer PfdTracer { get; set; }

		// Token: 0x060005EE RID: 1518 RVA: 0x00021117 File Offset: 0x0001F317
		public void Init(HttpApplication application)
		{
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				LatencyTracker latencyTracker = new LatencyTracker();
				latencyTracker.StartTracking(LatencyTrackerKey.ProxyModuleInitLatency, false);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<ProtocolType>((long)this.GetHashCode(), "[ProxyModule::Init]: Init called.  Protocol type: {0}", HttpProxyGlobals.ProtocolType);
				}
				if (application == null)
				{
					string text = "[ProxyModule::Init]: ProxyModule.Init called with null HttpApplication context.";
					if (ExTraceGlobals.BriefTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.BriefTracer.TraceError((long)this.GetHashCode(), text);
					}
					throw new ArgumentNullException("application", text);
				}
				this.PfdTracer = new PfdTracer(0, this.GetHashCode());
				application.BeginRequest += this.OnBeginRequest;
				application.AuthenticateRequest += this.OnAuthenticateRequest;
				application.PostAuthorizeRequest += this.OnPostAuthorizeRequest;
				application.PreSendRequestHeaders += this.OnPreSendRequestHeaders;
				application.EndRequest += this.OnEndRequest;
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<ProtocolType, long>((long)this.GetHashCode(), "[ProxyModule::Init]: Protocol type: {0}, InitLatency {1}", HttpProxyGlobals.ProtocolType, latencyTracker.GetCurrentLatency(LatencyTrackerKey.ProxyModuleInitLatency));
				}
			});
		}

		// Token: 0x060005EF RID: 1519 RVA: 0x00008C7B File Offset: 0x00006E7B
		public void Dispose()
		{
		}

		// Token: 0x060005F0 RID: 1520 RVA: 0x0002113C File Offset: 0x0001F33C
		protected virtual void OnBeginRequestInternal(HttpApplication httpApplication)
		{
			if (HttpProxyGlobals.OnlyProxySecureConnections && !httpApplication.Request.IsSecureConnection)
			{
				AspNetHelper.TerminateRequestWithSslRequiredResponse(httpApplication);
			}
		}

		// Token: 0x060005F1 RID: 1521 RVA: 0x00021158 File Offset: 0x0001F358
		protected virtual void OnAuthenticateInternal(HttpApplication httpApplication)
		{
			HttpContext context = httpApplication.Context;
			bool flag = this.AllowAnonymousRequest(context.Request);
			if (flag)
			{
				context.User = new WindowsPrincipal(WindowsIdentity.GetAnonymous());
			}
			if (flag)
			{
				context.SkipAuthorization = true;
			}
		}

		// Token: 0x060005F2 RID: 1522 RVA: 0x00021194 File Offset: 0x0001F394
		protected virtual void OnPostAuthorizeInternal(HttpApplication httpApplication)
		{
			HttpContext context = httpApplication.Context;
			if (NativeProxyHelper.CanNativeProxyHandleRequest(SharedHttpContextWrapper.GetWrapper(context)))
			{
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(context), "ProxyRequestHandler", "NativeHttpProxy");
				return;
			}
			IHttpHandler httpHandler;
			if (context.Request.IsAuthenticated)
			{
				httpHandler = this.SelectHandlerForAuthenticatedRequest(context);
			}
			else
			{
				httpHandler = this.SelectHandlerForUnauthenticatedRequest(context);
			}
			if (httpHandler != null)
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<Type, object>((long)this.GetHashCode(), "[ProxyModule::OnPostAuthorizeInternal]: The selected HttpHandler is {0}; Context {1};", httpHandler.GetType(), context.Items[Constants.TraceContextKey]);
				}
				PerfCounters.HttpProxyCountersInstance.TotalRequests.Increment();
				if (httpHandler is ProxyRequestHandler)
				{
					((ProxyRequestHandler)httpHandler).Run(context);
				}
				else
				{
					context.RemapHandler(httpHandler);
				}
				long currentLatency = LatencyTracker.FromHttpContext(context).GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency);
				if (currentLatency > 100L)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(context), "RemapHandler", currentLatency);
				}
			}
		}

		// Token: 0x060005F3 RID: 1523 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void OnEndRequestInternal(HttpApplication httpApplication)
		{
		}

		// Token: 0x060005F4 RID: 1524 RVA: 0x00021284 File Offset: 0x0001F484
		protected virtual bool AllowAnonymousRequest(HttpRequest httpRequest)
		{
			if (HttpProxyGlobals.ProtocolType == 14)
			{
				return false;
			}
			if (HttpProxyGlobals.ProtocolType == 21)
			{
				return true;
			}
			if (HttpProxyGlobals.ProtocolType == 22)
			{
				return true;
			}
			UriBuilder uriBuilder = new UriBuilder(httpRequest.Url);
			string text = null;
			if (UrlUtilities.TryGetExplicitLogonUser(httpRequest, ref text))
			{
				uriBuilder.Path = UrlUtilities.GetPathWithExplictLogonHint(httpRequest.Url, text);
			}
			return WopiRequestPathHandler.IsWopiRequest(httpRequest.HttpMethod, httpRequest.Url, AuthCommon.IsFrontEnd) || AnonymousCalendarProxyRequestHandler.IsAnonymousCalendarRequest(httpRequest) || OwaExtensibilityProxyRequestHandler.IsOwaExtensibilityRequest(httpRequest) || UrlUtilities.IsOwaDownloadRequest(uriBuilder.Uri) || OwaCobrandingRedirProxyRequestHandler.IsCobrandingRedirRequest(httpRequest) || E4eProxyRequestHandler.IsE4ePayloadRequest(httpRequest) || httpRequest.IsWsSecurityRequest();
		}

		// Token: 0x060005F5 RID: 1525 RVA: 0x0002132C File Offset: 0x0001F52C
		private static void FinalizeRequestLatencies(HttpContext httpContext, RequestDetailsLogger requestDetailsLogger, IActivityScope activityScope, LatencyTracker tracker, int traceContext)
		{
			if (tracker == null)
			{
				return;
			}
			if (requestDetailsLogger == null)
			{
				throw new ArgumentNullException("requestDetailsLogger");
			}
			if (activityScope == null)
			{
				throw new ArgumentNullException("activityScope");
			}
			if (httpContext == null)
			{
				throw new ArgumentNullException("httpContext");
			}
			HttpContextBase wrapper = SharedHttpContextWrapper.GetWrapper(httpContext);
			long num = tracker.GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency);
			if (num >= 0L)
			{
				long num2 = 0L;
				long.TryParse(activityScope.GetProperty(6), out num2);
				long num3 = 0L;
				bool flag = requestDetailsLogger.TryGetLatency(37, ref num3);
				long num4 = requestDetailsLogger.GetLatency(34, 0L) + requestDetailsLogger.GetLatency(36, 0L) + num3 + requestDetailsLogger.GetLatency(39, 0L) + requestDetailsLogger.GetLatency(40, 0L);
				long num5 = num - num4;
				if (!NativeProxyHelper.WasProxiedByNativeProxyHandler(wrapper))
				{
					PerfCounters.UpdateMovingAveragePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingAverageCasLatency, num5);
				}
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(requestDetailsLogger, 43, num5);
				long num6 = num5 - num2;
				if (flag)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(requestDetailsLogger, 42, Math.Max(num6, 0L));
					string property = activityScope.GetProperty(13);
					if (PerfCounters.RoutingLatenciesEnabled && !string.IsNullOrEmpty(property))
					{
						string empty = string.Empty;
						Utilities.TryExtractForestFqdnFromServerFqdn(property, ref empty);
						PercentilePerfCounters.UpdateRoutingLatencyPerfCounter(empty, (double)num6);
						PerfCounters.GetHttpProxyPerForestCountersInstance(empty).TotalProxyWithLatencyRequests.Increment();
					}
				}
				long val = num6 - requestDetailsLogger.GetLatency(35, 0L) - requestDetailsLogger.GetLatency(38, 0L);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(requestDetailsLogger, 41, Math.Max(val, 0L));
				long currentLatency = tracker.GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency);
				long num7 = currentLatency - num;
				num = currentLatency;
				if (num7 > 5L)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(requestDetailsLogger, "TotalRequestTimeDelta", num7);
				}
			}
			RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(requestDetailsLogger, 16, num);
		}

		// Token: 0x060005F6 RID: 1526 RVA: 0x000214F0 File Offset: 0x0001F6F0
		private static void InspectNativeProxyFatalError(HttpResponse httpResponse, RequestDetailsLogger requestLogger)
		{
			if (!HttpProxySettings.DiagnosticsEnabled.Value)
			{
				string text = httpResponse.Headers[NativeProxyHelper.NativeProxyStatusHeaders.ProxyErrorHResult];
				if (!string.IsNullOrEmpty(text))
				{
					string arg = httpResponse.Headers[NativeProxyHelper.NativeProxyStatusHeaders.ProxyErrorLabel];
					string arg2 = httpResponse.Headers[NativeProxyHelper.NativeProxyStatusHeaders.ProxyErrorMessage];
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericError(requestLogger, "ProxyError", string.Format("[{0}] [{1}] {2}", text, arg, arg2));
				}
			}
		}

		// Token: 0x060005F7 RID: 1527 RVA: 0x0002155C File Offset: 0x0001F75C
		private IHttpHandler SelectHandlerForAuthenticatedRequest(HttpContext httpContext)
		{
			IHttpHandler result;
			try
			{
				IHttpHandler httpHandler;
				if (HttpProxyGlobals.ProtocolType == 14)
				{
					httpHandler = new MapiProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 1)
				{
					if (SiteMailboxCreatingProxyRequestHandler.IsSiteMailboxCreatingProxyRequest(httpContext.Request))
					{
						httpHandler = new SiteMailboxCreatingProxyRequestHandler();
					}
					else if (EDiscoveryExportToolProxyRequestHandler.IsEDiscoveryExportToolProxyRequest(httpContext.Request))
					{
						httpHandler = new EDiscoveryExportToolProxyRequestHandler();
					}
					else if (BEResourceRequestHandler.CanHandle(httpContext.Request))
					{
						httpHandler = new BEResourceRequestHandler();
					}
					else
					{
						httpHandler = new EcpProxyRequestHandler();
					}
				}
				else if (HttpProxyGlobals.ProtocolType == 9)
				{
					httpHandler = new AutodiscoverProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 2)
				{
					if (EwsUserPhotoProxyRequestHandler.IsUserPhotoRequest(httpContext.Request))
					{
						httpHandler = new EwsUserPhotoProxyRequestHandler();
					}
					else if (MrsProxyRequestHandler.IsMrsRequest(httpContext.Request))
					{
						httpHandler = new MrsProxyRequestHandler();
					}
					else if (MessageTrackingRequestHandler.IsMessageTrackingRequest(httpContext.Request))
					{
						httpHandler = new MessageTrackingRequestHandler();
					}
					else
					{
						httpHandler = new EwsProxyRequestHandler();
					}
				}
				else if (HttpProxyGlobals.ProtocolType == 27)
				{
					httpHandler = new RestProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 8)
				{
					if (RpcHttpRequestHandler.CanHandleRequest(httpContext.Request))
					{
						httpHandler = new RpcHttpRequestHandler();
					}
					else
					{
						httpHandler = new RpcHttpProxyRequestHandler();
					}
				}
				else if (HttpProxyGlobals.ProtocolType == null)
				{
					httpHandler = new EasProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 3)
				{
					httpHandler = new OabProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 6 || HttpProxyGlobals.ProtocolType == 7)
				{
					httpHandler = new RemotePowerShellProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 10)
				{
					httpHandler = new ReportingWebServiceProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 11)
				{
					httpHandler = new PswsProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 12)
				{
					httpHandler = new XRopProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 4)
				{
					string absolutePath = httpContext.Request.Url.AbsolutePath;
					if (OWAUserPhotoProxyRequestHandler.IsUserPhotoRequest(httpContext.Request))
					{
						httpHandler = new OWAUserPhotoProxyRequestHandler();
					}
					else if (RequestPathParser.IsOwaEwsJsonRequest(absolutePath))
					{
						httpHandler = new EwsJsonProxyRequestHandler();
					}
					else if (RequestPathParser.IsOwaOeh2Request(absolutePath))
					{
						httpHandler = new OwaOeh2ProxyRequestHandler();
					}
					else if (RequestPathParser.IsOwaSpeechRecoRequest(absolutePath))
					{
						httpHandler = new SpeechRecoProxyRequestHandler();
					}
					else if (RequestPathParser.IsOwaLanguagePostRequest(absolutePath))
					{
						httpHandler = new OwaLanguagePostProxyRequestHandler();
					}
					else if (RequestPathParser.IsOwaE14ProxyRequest(absolutePath, httpContext.Request.RawUrl))
					{
						httpHandler = new EwsProxyRequestHandler(true);
					}
					else if (AuthenticatedWopiRequestPathHandler.IsAuthenticatedWopiRequest(httpContext.Request, AuthCommon.IsFrontEnd))
					{
						httpHandler = new AuthenticatedWopiProxyRequestHandler();
					}
					else
					{
						httpHandler = new OwaProxyRequestHandler();
					}
				}
				else if (HttpProxyGlobals.ProtocolType == 13)
				{
					httpHandler = new PushNotificationsProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 16)
				{
					httpHandler = new OutlookServiceProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 17)
				{
					httpHandler = new SnackyServiceProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 18)
				{
					httpHandler = new MicroServiceProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 15)
				{
					httpHandler = new E4eProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 20)
				{
					httpHandler = new O365SuiteServiceProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 21)
				{
					httpHandler = new MailboxDeliveryProxyRequestHandler();
				}
				else if (HttpProxyGlobals.ProtocolType == 22)
				{
					httpHandler = new ComplianceServiceProxyRequestHandler();
				}
				else
				{
					if (HttpProxyGlobals.ProtocolType != 24)
					{
						throw new InvalidOperationException("Unknown protocol type " + HttpProxyGlobals.ProtocolType);
					}
					httpHandler = new LogExportProxyHandler();
				}
				result = httpHandler;
			}
			finally
			{
				long currentLatency = LatencyTracker.FromHttpContext(httpContext).GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency);
				if (currentLatency > 100L)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpContext), "SelectHandler", currentLatency);
				}
			}
			return result;
		}

		// Token: 0x060005F8 RID: 1528 RVA: 0x000218D4 File Offset: 0x0001FAD4
		private IHttpHandler SelectHandlerForUnauthenticatedRequest(HttpContext httpContext)
		{
			IHttpHandler result;
			try
			{
				if (HttpProxySettings.NeedHandleAsAuthenticatedRequest(httpContext.Request.Headers, httpContext.Request.Cookies, httpContext.SkipAuthorization))
				{
					result = this.SelectHandlerForAuthenticatedRequest(httpContext);
				}
				else
				{
					UriBuilder uriBuilder = new UriBuilder(httpContext.Request.Url);
					string text = null;
					if (UrlUtilities.TryGetExplicitLogonUser(httpContext.Request, ref text))
					{
						uriBuilder.Path = UrlUtilities.GetPathWithExplictLogonHint(httpContext.Request.Url, text);
					}
					IHttpHandler httpHandler = null;
					if (HttpProxyGlobals.ProtocolType == 9)
					{
						httpHandler = new AutodiscoverProxyRequestHandler();
					}
					else if (HttpProxyGlobals.ProtocolType == 2)
					{
						if (RequestPathParser.IsEwsUnauthenticatedRequestProxyHandlerAllowed(httpContext.Request))
						{
							httpHandler = new EwsProxyRequestHandler();
						}
					}
					else if (HttpProxyGlobals.ProtocolType == 27)
					{
						if (RequestPathParser.IsRestUnauthenticatedRequestProxyHandlerAllowed(httpContext.Request))
						{
							httpHandler = new RestProxyRequestHandler();
						}
					}
					else if (HttpProxyGlobals.ProtocolType == 1)
					{
						if (EDiscoveryExportToolProxyRequestHandler.IsEDiscoveryExportToolProxyRequest(httpContext.Request))
						{
							httpHandler = new EDiscoveryExportToolProxyRequestHandler();
						}
						else if (BEResourceRequestHandler.CanHandle(httpContext.Request))
						{
							httpHandler = new BEResourceRequestHandler();
						}
						else if (EcpProxyRequestHandler.IsCrossForestDelegatedRequest(httpContext.Request))
						{
							httpHandler = new EcpProxyRequestHandler
							{
								IsCrossForestDelegated = true
							};
						}
						else if (!httpContext.Request.Path.StartsWith("/ecp/auth/", StringComparison.OrdinalIgnoreCase) && !httpContext.Request.Path.Equals("/ecp/ping.ecp", StringComparison.OrdinalIgnoreCase))
						{
							httpHandler = new Return401RequestHandler();
						}
					}
					else if (HttpProxyGlobals.ProtocolType == 8)
					{
						httpHandler = new RpcHttpRequestHandler();
					}
					else if (HttpProxyGlobals.ProtocolType == 12)
					{
						httpHandler = new XRopProxyRequestHandler();
					}
					else if (HttpProxyGlobals.ProtocolType == 15)
					{
						httpHandler = new E4eProxyRequestHandler();
					}
					else if (AnonymousCalendarProxyRequestHandler.IsAnonymousCalendarRequest(httpContext.Request))
					{
						httpHandler = new AnonymousCalendarProxyRequestHandler();
					}
					else if (HttpProxyGlobals.ProtocolType == 4 && WopiRequestPathHandler.IsWopiRequest(httpContext.Request.HttpMethod, httpContext.Request.Url, AuthCommon.IsFrontEnd))
					{
						httpHandler = new WopiProxyRequestHandler();
					}
					else if (OwaExtensibilityProxyRequestHandler.IsOwaExtensibilityRequest(httpContext.Request))
					{
						httpHandler = new OwaExtensibilityProxyRequestHandler();
					}
					else if (UrlUtilities.IsOwaDownloadRequest(uriBuilder.Uri))
					{
						httpHandler = new OwaDownloadProxyRequestHandler();
					}
					else if (OwaCobrandingRedirProxyRequestHandler.IsCobrandingRedirRequest(httpContext.Request))
					{
						httpHandler = new OwaCobrandingRedirProxyRequestHandler();
					}
					else if (HttpProxyGlobals.ProtocolType == 4 && OwaResourceProxyRequestHandler.CanHandle(httpContext.Request))
					{
						httpHandler = new OwaResourceProxyRequestHandler();
					}
					else if (HttpProxyGlobals.ProtocolType == 21)
					{
						httpHandler = new MailboxDeliveryProxyRequestHandler();
					}
					else if (HttpProxyGlobals.ProtocolType == 22)
					{
						httpHandler = new ComplianceServiceProxyRequestHandler();
					}
					result = httpHandler;
				}
			}
			finally
			{
				long currentLatency = LatencyTracker.FromHttpContext(httpContext).GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency);
				if (currentLatency > 100L)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpContext), "SelectHandler", currentLatency);
				}
			}
			return result;
		}

		// Token: 0x060005F9 RID: 1529 RVA: 0x00021B94 File Offset: 0x0001FD94
		private void OnAuthenticateRequest(object sender, EventArgs e)
		{
			HttpApplication httpApplication = (HttpApplication)sender;
			HttpContext httpContext = httpApplication.Context;
			CheckpointTracker.GetOrCreate(httpContext.Items).Add(FrontEndHttpProxyCheckpoints.ProxyModuleAuthenticateRequest);
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				LatencyTracker.FromHttpContext(httpContext).StartTracking(LatencyTrackerKey.AuthenticationLatency, false);
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string, Uri, object>((long)this.GetHashCode(), "[ProxyModule::OnAuthenticateRequest]: Method {0}; Url {1}; Context {2};", httpContext.Request.HttpMethod, httpContext.Request.Url, httpContext.Items[Constants.TraceContextKey]);
				}
				this.OnAuthenticateInternal(httpApplication);
				long currentLatency = LatencyTracker.FromHttpContext(httpContext).GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency);
				if (currentLatency > 100L)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpContext), "OnAuthenticate", currentLatency);
				}
			}, new Diagnostics.LastChanceExceptionHandler(RequestDetailsLogger.LastChanceExceptionHandler));
		}

		// Token: 0x060005FA RID: 1530 RVA: 0x00021C00 File Offset: 0x0001FE00
		private void OnBeginRequest(object sender, EventArgs e)
		{
			HttpApplication httpApplication = (HttpApplication)sender;
			HttpContext httpContext = httpApplication.Context;
			CheckpointTracker.GetOrCreate(httpContext.Items).Add(FrontEndHttpProxyCheckpoints.ProxyModuleBeginRequest);
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				LatencyTracker latencyTracker = new LatencyTracker();
				latencyTracker.StartTracking(LatencyTrackerKey.ProxyModuleLatency, false);
				AspNetHelper.AddTimestampHeaderIfNecessary(httpContext.Request.Headers, "X-FrontEnd-Begin");
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<string, Uri, int>((long)this.GetHashCode(), "[ProxyModule::OnBeginRequest]: Method {0}; Url {1}; Context {2};", httpContext.Request.HttpMethod, httpContext.Request.Url, httpContext.GetHashCode());
				}
				if (HealthCheckResponder.Instance.IsHealthCheckRequest(httpContext))
				{
					HealthCheckResponder.Instance.CheckHealthStateAndRespond(httpContext);
					return;
				}
				RequestDetailsLogger requestDetailsLogger = RequestDetailsLoggerBase<RequestDetailsLogger>.InitializeRequestLogger();
				requestDetailsLogger.LogCurrentTime("BeginRequest");
				httpContext.Items[Constants.TraceContextKey] = httpContext.GetHashCode();
				httpContext.Items[Constants.LatencyTrackerContextKeyName] = latencyTracker;
				requestDetailsLogger.ActivityScope.UpdateFromMessage(httpContext.Request);
				requestDetailsLogger.ActivityScope.SerializeTo(httpContext.Response);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SetCurrent(httpContext, requestDetailsLogger);
				httpContext.Items[typeof(ActivityScope)] = requestDetailsLogger.ActivityScope;
				httpContext.Items[Constants.RequestIdHttpContextKeyName] = requestDetailsLogger.ActivityScope.ActivityId;
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(requestDetailsLogger, 6, HttpProxyGlobals.ProtocolType);
				requestDetailsLogger.SafeLogUriData(httpContext.Request.Url);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(requestDetailsLogger, 6, httpContext.Request.HttpMethod);
				string requestCorrelationId = AspNetHelper.GetRequestCorrelationId(httpContext);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(requestDetailsLogger, "CorrelationID", requestCorrelationId);
				httpContext.Response.AppendToLog(Constants.CorrelationIdKeyForIISLogs + requestCorrelationId + ";");
				UrlUtilities.SaveOriginalRequestHostSchemePortToContext(SharedHttpContextWrapper.GetWrapper(httpContext));
				try
				{
					this.OnBeginRequestInternal(httpApplication);
				}
				catch (Exception ex)
				{
					RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericError(requestDetailsLogger, "OnBeginRequestInternal", ex.ToString());
					requestDetailsLogger.AsyncCommit(false, false);
					throw;
				}
			}, new Diagnostics.LastChanceExceptionHandler(RequestDetailsLogger.LastChanceExceptionHandler));
		}

		// Token: 0x060005FB RID: 1531 RVA: 0x00021C6C File Offset: 0x0001FE6C
		private void OnPostAuthorizeRequest(object sender, EventArgs e)
		{
			HttpApplication httpApplication = (HttpApplication)sender;
			HttpContext httpContext = httpApplication.Context;
			CheckpointTracker.GetOrCreate(httpContext.Items).Add(FrontEndHttpProxyCheckpoints.ProxyModulePostAuthorizeRequest);
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[ProxyModule::OnPostAuthorizeRequest]: Method {0}; Url {1}; Username {2}; Context {3};", new object[]
					{
						httpContext.Request.HttpMethod,
						httpContext.Request.Url,
						(httpContext.User == null) ? string.Empty : IIdentityExtensions.GetSafeName(httpContext.User.Identity, true),
						httpContext.GetTraceContext()
					});
				}
				this.OnPostAuthorizeInternal(httpApplication);
				LatencyTracker latencyTracker = LatencyTracker.FromHttpContext(httpContext);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpContext), 6, latencyTracker.GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency));
				long currentLatency = latencyTracker.GetCurrentLatency(LatencyTrackerKey.AuthenticationLatency);
				PerfCounters.UpdateMovingAveragePerformanceCounter(PerfCounters.HttpProxyCountersInstance.MovingAverageAuthenticationLatency, currentLatency);
				latencyTracker.StartTracking(LatencyTrackerKey.ModuleToHandlerSwitchingLatency, false);
			}, new Diagnostics.LastChanceExceptionHandler(RequestDetailsLogger.LastChanceExceptionHandler));
		}

		// Token: 0x060005FC RID: 1532 RVA: 0x00021CD8 File Offset: 0x0001FED8
		private void SetResponseHeaders(RequestDetailsLogger logger, HttpContext httpContext)
		{
			if (logger != null && !logger.IsDisposed && logger.ShouldSendDebugResponseHeaders())
			{
				ServiceCommonMetadataPublisher.PublishMetadata();
				if (httpContext != null)
				{
					logger.PushDebugInfoToResponseHeaders(httpContext);
				}
			}
		}

		// Token: 0x060005FD RID: 1533 RVA: 0x00021CFC File Offset: 0x0001FEFC
		private void OnPreSendRequestHeaders(object sender, EventArgs e)
		{
			HttpApplication httpApplication = (HttpApplication)sender;
			HttpContext httpContext = httpApplication.Context;
			CheckpointTracker.GetOrCreate(httpContext.Items).Add(FrontEndHttpProxyCheckpoints.ProxyModulePreSendRequestHeaders);
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				if (httpContext != null && httpContext.Response != null && httpContext.Response.Headers != null)
				{
					AspNetHelper.AddTimestampHeaderIfNecessary(httpContext.Response.Headers, "X-FrontEnd-End");
					RequestDetailsLogger current = RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpContext);
					if (current != null && !current.IsDisposed)
					{
						this.SetResponseHeaders(current, httpContext);
					}
					if (Extensions.IsProbeRequest(Extensions.GetHttpRequestBase(httpContext.Request)) && !RequestFailureContext.IsSetInResponse(SharedHttpContextWrapper.GetWrapper(httpContext).Response))
					{
						RequestFailureContext requestFailureContext = null;
						if (httpContext.Items.Contains(RequestFailureContext.HttpContextKeyName))
						{
							requestFailureContext = (RequestFailureContext)httpContext.Items[RequestFailureContext.HttpContextKeyName];
						}
						else if (httpContext.Response.StatusCode >= 400 && httpContext.Response.StatusCode < 600)
						{
							LiveIdAuthResult? liveIdAuthResult = null;
							LiveIdAuthResult value;
							if (httpContext.Items.Contains("LiveIdBasicAuthResult") && Enum.TryParse<LiveIdAuthResult>((string)httpContext.Items["LiveIdBasicAuthResult"], true, out value))
							{
								liveIdAuthResult = new LiveIdAuthResult?(value);
							}
							requestFailureContext = new RequestFailureContext(1, httpContext.Response.StatusCode, httpContext.Response.StatusDescription, string.Empty, null, null, liveIdAuthResult);
						}
						if (requestFailureContext != null)
						{
							requestFailureContext.UpdateResponse(SharedHttpContextWrapper.GetWrapper(httpContext).Response);
						}
					}
					ProxyRequestHandler proxyRequestHandler = httpContext.CurrentHandler as ProxyRequestHandler;
					if (proxyRequestHandler != null)
					{
						proxyRequestHandler.ResponseHeadersSent = true;
					}
				}
			}, new Diagnostics.LastChanceExceptionHandler(RequestDetailsLogger.LastChanceExceptionHandler));
		}

		// Token: 0x060005FE RID: 1534 RVA: 0x00021D60 File Offset: 0x0001FF60
		private void OnEndRequest(object sender, EventArgs e)
		{
			HttpApplication httpApplication = (HttpApplication)sender;
			HttpContext httpContext = httpApplication.Context;
			CheckpointTracker.GetOrCreate(httpContext.Items).Add(FrontEndHttpProxyCheckpoints.ProxyModuleEndRequest);
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				if (!HostHeaderValidator.HasValidHostHeaderStatusDescription(new HttpContextWrapper(httpContext).Response) || httpContext.Items["AutodiscoverRedirectModule"] != null)
				{
					return;
				}
				LatencyTracker latencyTracker = LatencyTracker.FromHttpContext(httpContext);
				RequestDetailsLogger current = RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpContext);
				RequestDetailsLoggerBase<RequestDetailsLogger>.SafeAppendGenericInfo(RequestDetailsLoggerBase<RequestDetailsLogger>.GetCurrent(httpContext), GuardedSharedCacheExecution.Default.Key, GuardedSharedCacheExecution.Default.Guard.GetCurrentValue());
				int traceContext = httpContext.GetTraceContext();
				if (httpContext.Response != null && current != null)
				{
					httpContext.Response.AppendToLog(Constants.RequestIdKeyForIISLogs + current.ActivityId.ToString() + ";");
				}
				if (HealthCheckResponder.Instance.IsHealthCheckRequest(httpContext))
				{
					return;
				}
				if (httpContext.Response.StatusCode == 404 && httpContext.Response.SubStatusCode == 13)
				{
					httpContext.Response.StatusCode = 507;
				}
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[ProxyModule::OnEndRequest]: Method {0}; Url {1}; Username {2}; Context {3};", new object[]
					{
						httpContext.Request.HttpMethod,
						httpContext.Request.Url,
						(httpContext.User == null) ? string.Empty : IIdentityExtensions.GetSafeName(httpContext.User.Identity, true),
						traceContext
					});
				}
				if (latencyTracker != null)
				{
					long currentLatency = latencyTracker.GetCurrentLatency(LatencyTrackerKey.HandlerToModuleSwitchingLatency);
					if (currentLatency >= 0L)
					{
						RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(current, 5, currentLatency);
					}
				}
				ProxyRequestHandler proxyRequestHandler = httpContext.CurrentHandler as ProxyRequestHandler;
				if (proxyRequestHandler != null && !proxyRequestHandler.IsDisposed)
				{
					current.AppendGenericInfo("DisposeProxyRequestHandler", "ProxyModule::OnEndRequest");
					proxyRequestHandler.Dispose();
				}
				ProxyModule.InspectNativeProxyFatalError(httpContext.Response, current);
				string text = httpContext.Items["AnonymousRequestFilterModule"] as string;
				if (!string.IsNullOrEmpty(text))
				{
					current.AppendGenericInfo("AnonymousRequestFilterModule", text);
				}
				try
				{
					this.OnEndRequestInternal(httpApplication);
				}
				finally
				{
					if (current != null && !current.IsDisposed)
					{
						IActivityScope activityScope = current.ActivityScope;
						if (activityScope != null)
						{
							if (!string.IsNullOrEmpty(activityScope.TenantId))
							{
								httpContext.Items["AuthenticatedUserOrganization"] = activityScope.TenantId;
							}
							ProxyModule.FinalizeRequestLatencies(httpContext, current, activityScope, latencyTracker, traceContext);
						}
						current.LogCurrentTime("EndRequest");
						RequestDetailsLoggerBase<RequestDetailsLogger>.SafeSetLogger(current, 0, DateTime.UtcNow);
						current.AsyncCommit(false, NativeProxyHelper.WasProxiedByNativeProxyHandler(SharedHttpContextWrapper.GetWrapper(httpContext)));
					}
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug(0L, "[ProxyModule::OnEndRequest]: Method {0}; Url {1}; OnEndRequestLatency {2}; Context {3};", new object[]
						{
							httpContext.Request.HttpMethod,
							httpContext.Request.Url,
							(latencyTracker != null) ? latencyTracker.GetCurrentLatency(LatencyTrackerKey.ProxyModuleLatency).ToString() : "Unknown",
							traceContext
						});
					}
				}
			}, new Diagnostics.LastChanceExceptionHandler(RequestDetailsLogger.LastChanceExceptionHandler));
		}
	}
}
