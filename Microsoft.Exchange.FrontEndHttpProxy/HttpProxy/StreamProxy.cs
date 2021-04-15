using System;
using System.IO;
using System.Threading;
using System.Web;
using Microsoft.Exchange.Diagnostics;
using Microsoft.Exchange.Diagnostics.Components.HttpProxy;
using Microsoft.Exchange.HttpProxy.Common;
using Microsoft.Exchange.Net;

namespace Microsoft.Exchange.HttpProxy
{
	// Token: 0x0200006E RID: 110
	internal class StreamProxy : IDisposeTrackable, IDisposable
	{
		// Token: 0x060003A2 RID: 930 RVA: 0x00014983 File Offset: 0x00012B83
		public StreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, byte[] buffer, IRequestContext requestContext) : this(streamProxyType, source, target, requestContext)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			this.buffer = buffer;
		}

		// Token: 0x060003A3 RID: 931 RVA: 0x000149A8 File Offset: 0x00012BA8
		public StreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, BufferPoolCollection.BufferSize maxBufferPoolSize, BufferPoolCollection.BufferSize minBufferPoolSize, IRequestContext requestContext) : this(streamProxyType, source, target, requestContext)
		{
			this.maxBufferPoolSize = maxBufferPoolSize;
			this.minBufferPoolSize = minBufferPoolSize;
			this.currentBufferPoolSize = minBufferPoolSize;
			this.currentBufferPool = BufferPoolCollection.AutoCleanupCollection.Acquire(this.currentBufferPoolSize);
			this.buffer = this.currentBufferPool.Acquire();
			this.previousBufferSize = this.buffer.Length;
		}

		// Token: 0x060003A4 RID: 932 RVA: 0x00014A10 File Offset: 0x00012C10
		private StreamProxy(StreamProxy.StreamProxyType streamProxyType, Stream source, Stream target, IRequestContext requestContext)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}
			if (requestContext == null)
			{
				throw new ArgumentException("requestContext");
			}
			this.disposeTracker = this.GetDisposeTracker();
			this.isDisposed = false;
			this.proxyType = streamProxyType;
			this.sourceStream = source;
			this.targetStream = target;
			this.requestContext = requestContext;
		}

		// Token: 0x170000DA RID: 218
		// (get) Token: 0x060003A5 RID: 933 RVA: 0x00014A89 File Offset: 0x00012C89
		public StreamProxy.StreamProxyType ProxyType
		{
			get
			{
				this.CheckDispose();
				return this.proxyType;
			}
		}

		// Token: 0x170000DB RID: 219
		// (get) Token: 0x060003A6 RID: 934 RVA: 0x00014A97 File Offset: 0x00012C97
		public StreamProxy.StreamProxyState StreamState
		{
			get
			{
				this.CheckDispose();
				return this.streamState;
			}
		}

		// Token: 0x170000DC RID: 220
		// (get) Token: 0x060003A7 RID: 935 RVA: 0x00014AA5 File Offset: 0x00012CA5
		public Stream SourceStream
		{
			get
			{
				this.CheckDispose();
				return this.sourceStream;
			}
		}

		// Token: 0x170000DD RID: 221
		// (get) Token: 0x060003A8 RID: 936 RVA: 0x00014AB3 File Offset: 0x00012CB3
		public Stream TargetStream
		{
			get
			{
				this.CheckDispose();
				return this.targetStream;
			}
		}

		// Token: 0x170000DE RID: 222
		// (get) Token: 0x060003A9 RID: 937 RVA: 0x00014AC1 File Offset: 0x00012CC1
		public IRequestContext RequestContext
		{
			get
			{
				this.CheckDispose();
				return this.requestContext;
			}
		}

		// Token: 0x170000DF RID: 223
		// (get) Token: 0x060003AA RID: 938 RVA: 0x00014ACF File Offset: 0x00012CCF
		// (set) Token: 0x060003AB RID: 939 RVA: 0x00014ADD File Offset: 0x00012CDD
		public Stream AuxTargetStream
		{
			get
			{
				this.CheckDispose();
				return this.auxTargetStream;
			}
			set
			{
				this.CheckDispose();
				this.auxTargetStream = value;
			}
		}

		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x060003AC RID: 940 RVA: 0x00014AEC File Offset: 0x00012CEC
		public long TotalBytesProxied
		{
			get
			{
				this.CheckDispose();
				return this.totalBytesProxied;
			}
		}

		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x060003AD RID: 941 RVA: 0x00014AFA File Offset: 0x00012CFA
		public long NumberOfReadsCompleted
		{
			get
			{
				this.CheckDispose();
				return this.numberOfReadsCompleted;
			}
		}

		// Token: 0x060003AE RID: 942 RVA: 0x00014B08 File Offset: 0x00012D08
		public IAsyncResult BeginProcess(AsyncCallback asyncCallback, object asyncState)
		{
			this.CheckDispose();
			this.LogElapsedTime("E_BegProc");
			if (asyncCallback == null)
			{
				throw new ArgumentNullException("asyncCallback");
			}
			IAsyncResult result;
			try
			{
				object obj = this.lockObject;
				lock (obj)
				{
					if (this.lazyAsyncResult != null)
					{
						throw new InvalidOperationException("BeginProcess() cannot be called more than once.");
					}
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int, StreamProxy.StreamProxyType>((long)this.GetHashCode(), "[StreamProxy::BeginProcess] Context: {0}, Type :{1}.", this.requestContext.TraceContext, this.proxyType);
					}
					this.lazyAsyncResult = new LazyAsyncResult(this, asyncState, asyncCallback);
					this.asyncException = null;
					this.streamState = StreamProxy.StreamProxyState.None;
					this.asyncStateHolder = new AsyncStateHolder(this);
					try
					{
						if (this.sourceStream != null)
						{
							this.BeginRead();
						}
						else
						{
							this.BeginSend((int)this.totalBytesProxied);
						}
					}
					catch (Exception innerException)
					{
						throw new StreamProxyException(innerException);
					}
					result = this.lazyAsyncResult;
				}
			}
			finally
			{
				this.LogElapsedTime("L_BegProc");
			}
			return result;
		}

		// Token: 0x060003AF RID: 943 RVA: 0x00014C24 File Offset: 0x00012E24
		public void EndProcess(IAsyncResult asyncResult)
		{
			this.CheckDispose();
			this.LogElapsedTime("E_EndProc");
			try
			{
				if (asyncResult == null)
				{
					throw new ArgumentNullException("asyncResult");
				}
				object obj = this.lockObject;
				lock (obj)
				{
					if (this.lazyAsyncResult == null)
					{
						throw new InvalidOperationException("BeginProcess() was not called.");
					}
					if (asyncResult != this.lazyAsyncResult)
					{
						throw new InvalidOperationException("The wrong asyncResult is passed.");
					}
				}
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, StreamProxy.StreamProxyType>((long)this.GetHashCode(), "[StreamProxy::EndProcess] Context: {0}, Type :{1}. ", this.requestContext.TraceContext, this.proxyType);
				}
				this.lazyAsyncResult.InternalWaitForCompletion();
				this.lazyAsyncResult = null;
				this.asyncStateHolder.Dispose();
				this.asyncStateHolder = null;
				if (this.asyncException != null)
				{
					throw new StreamProxyException(this.asyncException);
				}
			}
			finally
			{
				this.LogElapsedTime("L_EndProc");
			}
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x00014D2C File Offset: 0x00012F2C
		public void SetTargetStreamForBufferedSend(Stream newTargetStream)
		{
			this.CheckDispose();
			this.LogElapsedTime("E_SetTargetStream");
			object obj = this.lockObject;
			lock (obj)
			{
				this.sourceStream = null;
				this.targetStream = newTargetStream;
				this.OnTargetStreamUpdate();
			}
			this.LogElapsedTime("L_SetTargetStream");
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x00014D98 File Offset: 0x00012F98
		public DisposeTracker GetDisposeTracker()
		{
			return DisposeTracker.Get<StreamProxy>(this);
		}

		// Token: 0x060003B2 RID: 946 RVA: 0x00014DA0 File Offset: 0x00012FA0
		public void SuppressDisposeTracker()
		{
			if (this.disposeTracker != null)
			{
				this.disposeTracker.Suppress();
				this.disposeTracker = null;
			}
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x00014DBC File Offset: 0x00012FBC
		public void Dispose()
		{
			if (!this.isDisposed)
			{
				this.ReleaseBuffer();
				if (this.disposeTracker != null)
				{
					this.disposeTracker.Dispose();
					this.disposeTracker = null;
				}
				GC.SuppressFinalize(this);
				this.isDisposed = true;
			}
		}

		// Token: 0x060003B4 RID: 948 RVA: 0x0000500A File Offset: 0x0000320A
		protected virtual byte[] GetUpdatedBufferToSend(ArraySegment<byte> buffer)
		{
			return null;
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x00008C7B File Offset: 0x00006E7B
		protected virtual void OnTargetStreamUpdate()
		{
		}

		// Token: 0x060003B6 RID: 950 RVA: 0x00014DF4 File Offset: 0x00012FF4
		private static void ReadCompleteCallback(IAsyncResult asyncResult)
		{
			StreamProxy streamProxy = AsyncStateHolder.Unwrap<StreamProxy>(asyncResult);
			if (asyncResult.CompletedSynchronously)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(streamProxy.OnReadComplete), asyncResult);
				return;
			}
			streamProxy.OnReadComplete(asyncResult);
		}

		// Token: 0x060003B7 RID: 951 RVA: 0x00014E2C File Offset: 0x0001302C
		private static void WriteCompleteCallback(IAsyncResult asyncResult)
		{
			StreamProxy streamProxy = AsyncStateHolder.Unwrap<StreamProxy>(asyncResult);
			if (asyncResult.CompletedSynchronously)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(streamProxy.OnWriteComplete), asyncResult);
				return;
			}
			streamProxy.OnWriteComplete(asyncResult);
		}

		// Token: 0x060003B8 RID: 952 RVA: 0x00014E64 File Offset: 0x00013064
		private void BeginRead()
		{
			this.LogElapsedTime("E_BeginRead");
			try
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, StreamProxy.StreamProxyType>((long)this.GetHashCode(), "[StreamProxy::BeginRead] Context: {0}, Type :{1}. ", this.requestContext.TraceContext, this.proxyType);
				}
				this.requestContext.LatencyTracker.StartTracking(LatencyTrackerKey.StreamingLatency, true);
				this.sourceStream.BeginRead(this.buffer, 0, this.buffer.Length, new AsyncCallback(StreamProxy.ReadCompleteCallback), this.asyncStateHolder);
				this.streamState = StreamProxy.StreamProxyState.ExpectReadCallback;
			}
			finally
			{
				this.LogElapsedTime("L_BeginRead");
			}
		}

		// Token: 0x060003B9 RID: 953 RVA: 0x00014F18 File Offset: 0x00013118
		private void BeginSend(int bytesToSend)
		{
			this.LogElapsedTime("E_BeginSend");
			try
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, StreamProxy.StreamProxyType>((long)this.GetHashCode(), "[StreamProxy::BeginSend] Context: {0}, Type :{1}. ", this.requestContext.TraceContext, this.proxyType);
				}
				if (bytesToSend != this.numberOfBytesInBuffer)
				{
					throw new InvalidOperationException(string.Format("Invalid SendBuffer - {0} bytes in buffer, {1} bytes to be sent", this.numberOfBytesInBuffer, bytesToSend));
				}
				byte[] updatedBufferToSend = this.GetUpdatedBufferToSend(new ArraySegment<byte>(this.buffer, 0, bytesToSend));
				if (updatedBufferToSend != null)
				{
					bytesToSend = updatedBufferToSend.Length;
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
					{
						ExTraceGlobals.VerboseTracer.TraceDebug<int, StreamProxy.StreamProxyType, int>((long)this.GetHashCode(), "[StreamProxy::BeginSend] Context: {0}, Type :{1}. GetUpdatedBufferToSend() returns new buffer with size {2}.", this.requestContext.TraceContext, this.proxyType, bytesToSend);
					}
				}
				this.requestContext.LatencyTracker.StartTracking(LatencyTrackerKey.StreamingLatency, true);
				this.BeginWrite(updatedBufferToSend ?? this.buffer, bytesToSend);
			}
			finally
			{
				this.LogElapsedTime("L_BeginSend");
			}
		}

		// Token: 0x060003BA RID: 954 RVA: 0x00015024 File Offset: 0x00013224
		private void BeginWrite(byte[] buffer, int count)
		{
			this.LogElapsedTime("E_BegWrite");
			try
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug((long)this.GetHashCode(), "[StreamProxy::BeginWrite] Context: {0}, Type :{1}. Writing buffer with size {2} and count {3}.", new object[]
					{
						this.requestContext.TraceContext,
						this.proxyType,
						buffer.Length,
						count
					});
				}
				if (this.AuxTargetStream != null)
				{
					this.AuxTargetStream.Write(buffer, 0, count);
				}
				this.targetStream.BeginWrite(buffer, 0, count, new AsyncCallback(StreamProxy.WriteCompleteCallback), this.asyncStateHolder);
				this.streamState = StreamProxy.StreamProxyState.ExpectWriteCallback;
			}
			catch (NotSupportedException ex)
			{
				throw new HttpException(507, ex.ToString());
			}
			finally
			{
				this.LogElapsedTime("L_BegWrite");
			}
		}

		// Token: 0x060003BB RID: 955 RVA: 0x00015114 File Offset: 0x00013314
		private void LogElapsedTime(string latencyName)
		{
			if (HttpProxySettings.DetailedLatencyTracingEnabled.Value && this.requestContext != null && this.requestContext.LatencyTracker != null)
			{
				this.requestContext.LatencyTracker.LogElapsedTime(this.requestContext.Logger, latencyName + "_" + this.proxyType.ToString());
			}
		}

		// Token: 0x060003BC RID: 956 RVA: 0x0001517C File Offset: 0x0001337C
		private void OnReadComplete(object asyncState)
		{
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				IAsyncResult asyncResult = (IAsyncResult)asyncState;
				this.LogElapsedTime("E_OnReadComp");
				try
				{
					object obj = this.lockObject;
					lock (obj)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<int, StreamProxy.StreamProxyType>((long)this.GetHashCode(), "[StreamProxy::OnReadComplete] Context: {0}, Type :{1}. ", this.requestContext.TraceContext, this.proxyType);
						}
						int num = this.sourceStream.EndRead(asyncResult);
						this.streamState = StreamProxy.StreamProxyState.None;
						if (num > 0)
						{
							StreamProxy.StreamProxyType streamProxyType = this.proxyType;
							if (streamProxyType != StreamProxy.StreamProxyType.Request)
							{
								if (streamProxyType == StreamProxy.StreamProxyType.Response)
								{
									PerfCounters.HttpProxyCountersInstance.TotalBytesOut.IncrementBy((long)num);
								}
							}
							else
							{
								PerfCounters.HttpProxyCountersInstance.TotalBytesIn.IncrementBy((long)num);
							}
							this.requestContext.LatencyTracker.LogElapsedTimeAsLatency(this.requestContext.Logger, LatencyTrackerKey.StreamingLatency, this.GetReadProtocolLogKey());
							this.numberOfBytesInBuffer = num;
							this.numberOfReadsCompleted += 1L;
							this.totalBytesProxied += (long)num;
							this.BeginSend(num);
						}
						else
						{
							this.Complete(null);
						}
					}
				}
				catch (Exception ex)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError<int, StreamProxy.StreamProxyType, Exception>((long)this.GetHashCode(), "[StreamProxy::OnReadComplete] Context: {0}, Type :{1}. Error occured thrown when processing read. Exception: {2}", this.requestContext.TraceContext, this.proxyType, ex);
					}
					this.Complete(ex);
				}
				finally
				{
					this.LogElapsedTime("L_OnReadComp");
				}
			}, new Diagnostics.LastChanceExceptionHandler(RequestDetailsLogger.LastChanceExceptionHandler));
		}

		// Token: 0x060003BD RID: 957 RVA: 0x000151AD File Offset: 0x000133AD
		private void OnWriteComplete(object asyncState)
		{
			Diagnostics.SendWatsonReportOnUnhandledException(delegate()
			{
				IAsyncResult asyncResult = (IAsyncResult)asyncState;
				this.LogElapsedTime("E_OnWriteComp");
				try
				{
					object obj = this.lockObject;
					lock (obj)
					{
						if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
						{
							ExTraceGlobals.VerboseTracer.TraceDebug<int, StreamProxy.StreamProxyType>((long)this.GetHashCode(), "[StreamProxy::OnWriteComplete] Context: {0}, Type :{1}. ", this.requestContext.TraceContext, this.proxyType);
						}
						this.targetStream.EndWrite(asyncResult);
						this.streamState = StreamProxy.StreamProxyState.None;
						this.requestContext.LatencyTracker.LogElapsedTimeAsLatency(this.requestContext.Logger, LatencyTrackerKey.StreamingLatency, this.GetWriteProtocolLogKey());
						if (this.sourceStream != null)
						{
							this.AdjustBuffer();
							this.BeginRead();
						}
						else
						{
							this.Complete(null);
						}
					}
				}
				catch (Exception ex)
				{
					if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(3))
					{
						ExTraceGlobals.VerboseTracer.TraceError<int, StreamProxy.StreamProxyType, Exception>((long)this.GetHashCode(), "[StreamProxy::OnWriteComplete] Context: {0}, Type :{1}. Error occured thrown when processing write. Exception: {2}", this.requestContext.TraceContext, this.proxyType, ex);
					}
					this.Complete(ex);
				}
				finally
				{
					this.LogElapsedTime("L_OnWriteComp");
				}
			}, new Diagnostics.LastChanceExceptionHandler(RequestDetailsLogger.LastChanceExceptionHandler));
		}

		// Token: 0x060003BE RID: 958 RVA: 0x000151E0 File Offset: 0x000133E0
		private void Complete(Exception exception)
		{
			this.LogElapsedTime("E_SPComplete");
			try
			{
				if (ExTraceGlobals.VerboseTracer.IsTraceEnabled(1))
				{
					ExTraceGlobals.VerboseTracer.TraceDebug<int, StreamProxy.StreamProxyType, Exception>((long)this.GetHashCode(), "[StreamProxy::Complete] Context: {0}, Type :{1}. Complete with exception: {2}", this.requestContext.TraceContext, this.proxyType, exception);
				}
				this.asyncException = exception;
				this.lazyAsyncResult.InvokeCallback();
			}
			finally
			{
				this.LogElapsedTime("L_SPComplete");
			}
		}

		// Token: 0x060003BF RID: 959 RVA: 0x00015260 File Offset: 0x00013460
		private HttpProxyMetadata GetReadProtocolLogKey()
		{
			if (this.proxyType == StreamProxy.StreamProxyType.Request)
			{
				return 34;
			}
			return 39;
		}

		// Token: 0x060003C0 RID: 960 RVA: 0x0001526F File Offset: 0x0001346F
		private HttpProxyMetadata GetWriteProtocolLogKey()
		{
			if (this.proxyType == StreamProxy.StreamProxyType.Request)
			{
				return 36;
			}
			return 40;
		}

		// Token: 0x060003C1 RID: 961 RVA: 0x00015280 File Offset: 0x00013480
		private void ReleaseBuffer()
		{
			if (this.buffer != null && this.currentBufferPool != null)
			{
				try
				{
					this.currentBufferPool.Release(this.buffer);
				}
				finally
				{
					this.buffer = null;
				}
			}
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x000152C8 File Offset: 0x000134C8
		private void AdjustBuffer()
		{
			if (this.currentBufferPool == null)
			{
				return;
			}
			if (this.numberOfBytesInBuffer >= this.buffer.Length)
			{
				if (this.currentBufferPoolSize < this.maxBufferPoolSize)
				{
					this.previousBufferSize = this.buffer.Length;
					this.ReleaseBuffer();
					this.currentBufferPoolSize++;
					this.currentBufferPool = BufferPoolCollection.AutoCleanupCollection.Acquire(this.currentBufferPoolSize);
					this.buffer = this.currentBufferPool.Acquire();
					return;
				}
			}
			else if (this.currentBufferPoolSize > this.minBufferPoolSize)
			{
				if (this.numberOfBytesInBuffer == this.previousBufferSize)
				{
					this.ReleaseBuffer();
					this.currentBufferPoolSize--;
					this.currentBufferPool = BufferPoolCollection.AutoCleanupCollection.Acquire(this.currentBufferPoolSize);
					this.buffer = this.currentBufferPool.Acquire();
					this.maxBufferPoolSize = this.currentBufferPoolSize;
					this.minBufferPoolSize = this.currentBufferPoolSize;
					return;
				}
				if (this.numberOfBytesInBuffer > this.previousBufferSize)
				{
					this.previousBufferSize = this.buffer.Length;
				}
			}
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x000153D5 File Offset: 0x000135D5
		private void CheckDispose()
		{
			if (!this.isDisposed)
			{
				return;
			}
			throw new ObjectDisposedException("StreamProxy");
		}

		// Token: 0x0400025B RID: 603
		private readonly object lockObject = new object();

		// Token: 0x0400025C RID: 604
		private readonly StreamProxy.StreamProxyType proxyType;

		// Token: 0x0400025D RID: 605
		private readonly IRequestContext requestContext;

		// Token: 0x0400025E RID: 606
		private Stream sourceStream;

		// Token: 0x0400025F RID: 607
		private Stream targetStream;

		// Token: 0x04000260 RID: 608
		private StreamProxy.StreamProxyState streamState;

		// Token: 0x04000261 RID: 609
		private Stream auxTargetStream;

		// Token: 0x04000262 RID: 610
		private long totalBytesProxied;

		// Token: 0x04000263 RID: 611
		private long numberOfReadsCompleted;

		// Token: 0x04000264 RID: 612
		private int numberOfBytesInBuffer;

		// Token: 0x04000265 RID: 613
		private LazyAsyncResult lazyAsyncResult;

		// Token: 0x04000266 RID: 614
		private AsyncStateHolder asyncStateHolder;

		// Token: 0x04000267 RID: 615
		private Exception asyncException;

		// Token: 0x04000268 RID: 616
		private byte[] buffer;

		// Token: 0x04000269 RID: 617
		private BufferPoolCollection.BufferSize maxBufferPoolSize;

		// Token: 0x0400026A RID: 618
		private BufferPoolCollection.BufferSize minBufferPoolSize;

		// Token: 0x0400026B RID: 619
		private BufferPoolCollection.BufferSize currentBufferPoolSize;

		// Token: 0x0400026C RID: 620
		private int previousBufferSize;

		// Token: 0x0400026D RID: 621
		private BufferPool currentBufferPool;

		// Token: 0x0400026E RID: 622
		private DisposeTracker disposeTracker;

		// Token: 0x0400026F RID: 623
		private bool isDisposed;

		// Token: 0x02000104 RID: 260
		internal enum StreamProxyType
		{
			// Token: 0x040004D6 RID: 1238
			Request,
			// Token: 0x040004D7 RID: 1239
			Response
		}

		// Token: 0x02000105 RID: 261
		internal enum StreamProxyState
		{
			// Token: 0x040004D9 RID: 1241
			None,
			// Token: 0x040004DA RID: 1242
			ExpectReadCallback,
			// Token: 0x040004DB RID: 1243
			ExpectWriteCallback
		}
	}
}
