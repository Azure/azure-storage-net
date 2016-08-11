//-----------------------------------------------------------------------
// <copyright file="ExecutionState.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Core.Executor
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Globalization;
    using System.IO;

#if WINDOWS_RT || NETCORE
    using System.Net.Http;
#else
    using System.Net;
    using System.Threading;
#endif

    // This class encapsulates a StorageCommand and stores state about its execution.
    // Note conceptually there is some overlap between ExecutionState and operationContext, however the 
    // operationContext is the user visible object and the ExecutionState is an internal object used to coordinate execution.
#if WINDOWS_RT || NETCORE
    internal class ExecutionState<T> : IDisposable
#else
    // If we are exposing APM then derive this class from the StorageCommandAsyncResult
    internal class ExecutionState<T> : StorageCommandAsyncResult
#endif
    {
        public ExecutionState(StorageCommandBase<T> cmd, IRetryPolicy policy, OperationContext operationContext)
        {
            this.Cmd = cmd;
            this.RetryPolicy = policy != null ? policy.CreateInstance() : new NoRetry();
            this.OperationContext = operationContext ?? new OperationContext();
            this.InitializeLocation();

#if WINDOWS_RT || NETCORE
            if (this.OperationContext.StartTime == DateTimeOffset.MinValue)
            {
                this.OperationContext.StartTime = DateTimeOffset.Now;
            }
#else
            if (this.OperationContext.StartTime == DateTime.MinValue)
            {
                this.OperationContext.StartTime = DateTime.Now;
            }
#endif
        }

#if WINDOWS_DESKTOP 
        public ExecutionState(StorageCommandBase<T> cmd, IRetryPolicy policy, OperationContext operationContext, AsyncCallback callback, object asyncState)
            : base(callback, asyncState)
        {
            this.Cmd = cmd;
            this.RetryPolicy = policy != null ? policy.CreateInstance() : new NoRetry();
            this.OperationContext = operationContext ?? new OperationContext();
            this.InitializeLocation();

            if (this.OperationContext.StartTime == DateTime.MinValue)
            {
                this.OperationContext.StartTime = DateTime.Now;
            }
        }
#endif

        internal void Init()
        {
            this.Req = null;
            this.resp = null;

#if !(WINDOWS_RT || NETCORE)
            this.ReqTimedOut = false;
            this.CancelDelegate = null;
#endif
        }

#if WINDOWS_RT || NETCORE
        public void Dispose()
        {
            this.CheckDisposeSendStream();
            this.CheckDisposeAction();
        }
#else
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Timer backoffTimer = this.BackoffTimer;
                if (backoffTimer != null)
                {
                    this.BackoffTimer = null;
                    backoffTimer.Dispose();
                }

                this.CheckDisposeSendStream();
                this.CheckDisposeAction();
            }

            base.Dispose(disposing);
        }

        internal Timer BackoffTimer { get; set; }
#endif

        internal OperationContext OperationContext { get; private set; }

        internal DateTime? OperationExpiryTime
        {
            get { return this.Cmd.OperationExpiryTime; }
        }

        internal IRetryPolicy RetryPolicy { get; private set; }

        internal StorageCommandBase<T> Cmd { get; private set; }

        internal StorageLocation CurrentLocation { get; set; }

        internal RESTCommand<T> RestCMD
        {
            get
            {
                return this.Cmd as RESTCommand<T>;
            }
        }

        internal ExecutorOperation CurrentOperation { get; set; }

        internal TimeSpan RemainingTimeout
        {
            get
            {
                if (!this.OperationExpiryTime.HasValue || this.OperationExpiryTime.Value.Equals(DateTime.MaxValue))
                {
                    // User did not specify a timeout, so we will set the request timeout to avoid
                    // waiting for the response infinitely
                    return Constants.DefaultClientSideTimeout;
                }
                else
                {
                    TimeSpan potentialTimeout = this.OperationExpiryTime.Value - DateTime.Now;

                    if (potentialTimeout <= TimeSpan.Zero)
                    {
                        throw Exceptions.GenerateTimeoutException(this.Cmd.CurrentResult, null);
                    }

                    return potentialTimeout;
                }
            }
        }

        internal int RetryCount { get; set; }

        internal Stream ReqStream
        {
            get
            {
                return this.reqStream;
            }

            set
            {
                this.reqStream =
#if WINDOWS_RT || NETCORE
                    value;
#else
                    value == null ? null : value.WrapWithByteCountingStream(this.Cmd.CurrentResult);
#endif
            }
        }

        private Stream reqStream;

        private volatile Exception exceptionRef = null;

        internal Exception ExceptionRef
        {
            get
            {
                return this.exceptionRef;
            }

            set
            {
                this.exceptionRef = value;
                if (this.Cmd != null && this.Cmd.CurrentResult != null)
                {
                    this.Cmd.CurrentResult.Exception = value;
                }
            }
        }

        internal T Result { get; set; }

        private object timeoutLockerObj = new object();
        private bool reqTimedOut = false;

        internal bool ReqTimedOut
        {
            get
            {
                lock (this.timeoutLockerObj)
                {
                    return this.reqTimedOut;
                }
            }

            set
            {
                lock (this.timeoutLockerObj)
                {
                    this.reqTimedOut = value;
                }
            }
        }

        private void CheckDisposeSendStream()
        {
            RESTCommand<T> cmd = this.RestCMD;

            if ((cmd != null) && (cmd.StreamToDispose != null))
            {
                cmd.StreamToDispose.Dispose();
                cmd.StreamToDispose = null;
            }
        }

        private void CheckDisposeAction()
        {
            RESTCommand<T> cmd = this.RestCMD;
            if (cmd != null && cmd.DisposeAction != null)
            {
                Logger.LogInformational(this.OperationContext, SR.TraceDispose);

                try
                {
                    cmd.DisposeAction(cmd);
                }
                catch (Exception ex)
                {
                    // Ignore the error thrown by the dispose action in the error case so the original error that caused it can be exposed
                    // to the user. Just log this here for debugging service.
                    Logger.LogWarning(this.OperationContext, SR.TraceDisposeError, ex.Message);
                }
            }
        }

#if WINDOWS_RT || NETCORE
        internal StorageRequestMessage Req { get; set; }

        private HttpResponseMessage resp = null;

        internal HttpResponseMessage Resp
        {
            get
            {
                return this.resp;
            }

            set
            {
                this.resp = value;

                if (value != null)
                {
                    this.Cmd.CurrentResult.ServiceRequestID = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(this.resp.Headers, Constants.HeaderConstants.RequestIdHeader);
                    this.Cmd.CurrentResult.ContentMd5 = this.resp.Content.Headers.ContentMD5 != null ? Convert.ToBase64String(this.resp.Content.Headers.ContentMD5) : null;
                    this.Cmd.CurrentResult.Etag = this.resp.Headers.ETag != null ? this.resp.Headers.ETag.ToString() : null;
                    this.Cmd.CurrentResult.RequestDate = this.resp.Headers.Date.HasValue ? this.resp.Headers.Date.Value.UtcDateTime.ToString("R", CultureInfo.InvariantCulture) : null;
                    this.Cmd.CurrentResult.HttpStatusMessage = this.resp.ReasonPhrase;
                    this.Cmd.CurrentResult.HttpStatusCode = (int)this.resp.StatusCode;
                }
            }
        }
#else
        internal HttpWebRequest Req { get; set; }

        private HttpWebResponse resp = null;

        internal HttpWebResponse Resp
        {
            get
            {
                return this.resp;
            }

            set
            {
                this.resp = value;

                if (this.resp != null)
                {
                    if (value.Headers != null)
                    {
#if WINDOWS_DESKTOP 
                        this.Cmd.CurrentResult.ServiceRequestID = HttpWebUtility.TryGetHeader(this.resp, Constants.HeaderConstants.RequestIdHeader, null);
                        this.Cmd.CurrentResult.ContentMd5 = HttpWebUtility.TryGetHeader(this.resp, "Content-MD5", null);
                        string tempDate = HttpWebUtility.TryGetHeader(this.resp, "Date", null);
                        this.Cmd.CurrentResult.RequestDate = string.IsNullOrEmpty(tempDate) ? DateTime.Now.ToString("R", CultureInfo.InvariantCulture) : tempDate;
                        this.Cmd.CurrentResult.Etag = this.resp.Headers[HttpResponseHeader.ETag];
#endif
                    }

                    this.Cmd.CurrentResult.HttpStatusMessage = this.resp.StatusDescription;
                    this.Cmd.CurrentResult.HttpStatusCode = (int)this.resp.StatusCode;
                }
            }
        }
#endif

        private void InitializeLocation()
        {
            RESTCommand<T> cmd = this.RestCMD;
            if (cmd != null)
            {
                switch (cmd.LocationMode)
                {
                    case LocationMode.PrimaryOnly:
                    case LocationMode.PrimaryThenSecondary:
                        this.CurrentLocation = StorageLocation.Primary;
                        break;

                    case LocationMode.SecondaryOnly:
                    case LocationMode.SecondaryThenPrimary:
                        this.CurrentLocation = StorageLocation.Secondary;
                        break;

                    default:
                        CommonUtility.ArgumentOutOfRange("LocationMode", cmd.LocationMode);
                        break;
                }

                Logger.LogInformational(this.OperationContext, SR.TraceInitLocation, this.CurrentLocation, cmd.LocationMode);
            }
            else
            {
                this.CurrentLocation = StorageLocation.Primary;
            }
        }
    }
}
