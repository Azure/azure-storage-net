//-----------------------------------------------------------------------
// <copyright file="Executor.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Threading;

    internal class Executor : ExecutorBase
    {
        #region Async

        #region Begin / End
        // Cancellation will be handled by a flag in the return type ( CancellationToken in .net4+ )
        public static ICancellableAsyncResult BeginExecuteAsync<T>(RESTCommand<T> cmd, IRetryPolicy policy, OperationContext operationContext, AsyncCallback callback, object asyncState)
        {
            // Note all code below will reference state, not params directly, this will allow common code with async executor
            ExecutionState<T> executionState = new ExecutionState<T>(cmd, policy, operationContext, callback, asyncState);
            Executor.InitRequest(executionState);
            return executionState;
        }

        public static T EndExecuteAsync<T>(IAsyncResult result)
        {
            CommonUtility.AssertNotNull("result", result);

            using (ExecutionState<T> executionState = (ExecutionState<T>)result)
            {
                executionState.End();

                executionState.RestCMD.SendStream = null;
                executionState.RestCMD.DestinationStream = null;

                if (executionState.ExceptionRef != null)
                {
                    throw executionState.ExceptionRef;
                }

                return executionState.Result;
            }
        }

        #endregion

        #region Steps 0 - 4 Setup Request
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        public static void InitRequest<T>(ExecutionState<T> executionState)
        {
            try
            {
                executionState.Init();

                // 0. Begin Request 
                Executor.StartRequestAttempt(executionState);

                // Steps 1 - 4
                Executor.ProcessStartOfRequest(executionState, SR.TraceStartRequestAsync);

                if (Executor.CheckTimeout<T>(executionState, false))
                {
                    Executor.EndOperation(executionState);
                    return;
                }

                if (Executor.CheckCancellation(executionState))
                {
                    Executor.EndOperation(executionState);
                    return;
                }

                // 5. potentially upload data
                if (executionState.RestCMD.SendStream != null)
                {
                    Executor.BeginGetRequestStream(executionState);
                }
                else
                {
                    Executor.BeginGetResponse(executionState);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(executionState.OperationContext, SR.TraceInitRequestError, ex.Message);

                // Store exception and invoke callback here. All operations in this try would be non-retryable by default
                StorageException storageEx = TranslateExceptionBasedOnParseError(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);

                storageEx.IsRetryable = false;
                executionState.ExceptionRef = storageEx;
                executionState.OnComplete();
            }
        }
        #endregion

        #region Step 5 Get Request Stream & upload
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        private static void BeginGetRequestStream<T>(ExecutionState<T> executionState)
        {
            executionState.CurrentOperation = ExecutorOperation.BeginGetRequestStream;
            Logger.LogInformational(executionState.OperationContext, SR.TracePrepareUpload);

            try
            {
                APMWithTimeout.RunWithTimeout(
                    executionState.Req.BeginGetRequestStream,
                    Executor.EndGetRequestStream<T>,
                    Executor.AbortRequest<T>,
                    executionState,
                    executionState.RemainingTimeout);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(executionState.OperationContext, SR.TracePrepareUploadError, ex.Message);
                executionState.ExceptionRef = ExecutorBase.TranslateExceptionBasedOnParseError(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);
                Executor.EndOperation(executionState);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        private static void EndGetRequestStream<T>(IAsyncResult getRequestStreamResult)
        {
            ExecutionState<T> executionState = (ExecutionState<T>)getRequestStreamResult.AsyncState;
            executionState.CurrentOperation = ExecutorOperation.EndGetRequestStream;

            try
            {
                executionState.UpdateCompletedSynchronously(getRequestStreamResult.CompletedSynchronously);

                executionState.ReqStream = executionState.Req.EndGetRequestStream(getRequestStreamResult);

                executionState.CurrentOperation = ExecutorOperation.BeginUploadRequest;
                Logger.LogInformational(executionState.OperationContext, SR.TraceUpload);
                MultiBufferMemoryStream multiBufferMemoryStream = executionState.RestCMD.SendStream as MultiBufferMemoryStream;
                if (multiBufferMemoryStream != null && !executionState.RestCMD.SendStreamLength.HasValue)
                {
                    multiBufferMemoryStream.BeginFastCopyTo(executionState.ReqStream, executionState.OperationExpiryTime, EndFastCopyTo<T>, executionState);
                }
                else
                {
                    // don't calculate md5 here as we should have already set this for auth purposes
                    executionState.RestCMD.SendStream.WriteToAsync(executionState.ReqStream, executionState.RestCMD.SendStreamLength, null /* maxLength */, false, executionState, null /* streamCopyState */, EndSendStreamCopy);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(executionState.OperationContext, SR.TraceUploadError, ex.Message);
                executionState.ExceptionRef = ExecutorBase.TranslateExceptionBasedOnParseError(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);
                Executor.EndOperation(executionState);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        private static void EndFastCopyTo<T>(IAsyncResult fastCopyToResult)
        {
            ExecutionState<T> executionState = (ExecutionState<T>)fastCopyToResult.AsyncState;

            try
            {
                executionState.UpdateCompletedSynchronously(fastCopyToResult.CompletedSynchronously);

                MultiBufferMemoryStream multiBufferMemoryStream = (MultiBufferMemoryStream)executionState.RestCMD.SendStream;
                multiBufferMemoryStream.EndFastCopyTo(fastCopyToResult);

                Executor.EndSendStreamCopy(executionState);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(executionState.OperationContext, SR.TraceUploadError, ex.Message);
                executionState.ExceptionRef = ExecutorBase.TranslateExceptionBasedOnParseError(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);
                Executor.EndOperation(executionState);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        private static void EndSendStreamCopy<T>(ExecutionState<T> executionState)
        {
            executionState.CurrentOperation = ExecutorOperation.EndUploadRequest;

            Executor.CheckCancellation(executionState);

            if (executionState.ExceptionRef != null)
            {
                try
                {
                    executionState.Req.Abort();
                }
                catch (Exception)
                {
                    // No op
                }

                Executor.EndOperation(executionState);
            }
            else
            {
                try
                {
                    executionState.ReqStream.Flush();
                    executionState.ReqStream.Dispose();
                    executionState.ReqStream = null;
                }
                catch (Exception)
                {
                    // If we could not flush/dispose the request stream properly,
                    // BeginGetResponse will fail with a more meaningful error anyway.
                }

                Executor.BeginGetResponse(executionState);
            }
        }
        #endregion

        #region Steps 6-8 Get Response & Potentially Send
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        private static void BeginGetResponse<T>(ExecutionState<T> executionState)
        {
            executionState.CurrentOperation = ExecutorOperation.BeginGetResponse;
            Logger.LogInformational(executionState.OperationContext, SR.TraceGetResponse);

            try
            {
                APMWithTimeout.RunWithTimeout(
                    executionState.Req.BeginGetResponse,
                    Executor.EndGetResponse<T>,
                    Executor.AbortRequest<T>,
                    executionState,
                    executionState.RemainingTimeout);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(executionState.OperationContext, SR.TraceGetResponseError, ex.Message);
                executionState.ExceptionRef = ExecutorBase.TranslateExceptionBasedOnParseError(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);
                Executor.EndOperation(executionState);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        private static void EndGetResponse<T>(IAsyncResult getResponseResult)
        {
            ExecutionState<T> executionState = (ExecutionState<T>)getResponseResult.AsyncState;
            executionState.CurrentOperation = ExecutorOperation.EndGetResponse;

            try
            {
                executionState.UpdateCompletedSynchronously(getResponseResult.CompletedSynchronously);

                try
                {
                    executionState.Resp = executionState.Req.EndGetResponse(getResponseResult) as HttpWebResponse;
                }
                catch (WebException ex)
                {
                    Logger.LogWarning(executionState.OperationContext, SR.TraceGetResponseError, ex.Message);
                    executionState.Resp = (HttpWebResponse)ex.Response;

                    if (ex.Status == WebExceptionStatus.Timeout || executionState.ReqTimedOut)
                    {
                        throw new TimeoutException();
                    }

                    if (executionState.Resp == null)
                    {
                        throw;
                    }
                    else
                    {
                        // Store this exception for now. It will be parsed/thrown after the stream is read in step 8
                        executionState.ExceptionRef = ex;
                    }
                }

                Logger.LogInformational(executionState.OperationContext, SR.TraceResponse, executionState.Cmd.CurrentResult.HttpStatusCode, executionState.Cmd.CurrentResult.ServiceRequestID, executionState.Cmd.CurrentResult.ContentMd5, executionState.Cmd.CurrentResult.Etag);
                Executor.FireResponseReceived(executionState);

                // 7. Do Response parsing (headers etc, no stream available here)
                if (executionState.RestCMD.PreProcessResponse != null)
                {
                    executionState.CurrentOperation = ExecutorOperation.PreProcess;

                    try
                    {
                        executionState.Result = executionState.RestCMD.PreProcessResponse(executionState.RestCMD, executionState.Resp, executionState.ExceptionRef, executionState.OperationContext);

                        // clear exception
                        executionState.ExceptionRef = null;
                        Logger.LogInformational(executionState.OperationContext, SR.TracePreProcessDone);
                    }
                    catch (Exception ex)
                    {
                        executionState.ExceptionRef = ex;
                    }
                }

                Executor.CheckCancellation(executionState);

                executionState.CurrentOperation = ExecutorOperation.GetResponseStream;
                executionState.RestCMD.ResponseStream = executionState.Resp.GetResponseStream();

                // 8. (Potentially reads stream from server)
                if (executionState.ExceptionRef != null)
                {
                    executionState.CurrentOperation = ExecutorOperation.BeginDownloadResponse;
                    Logger.LogInformational(executionState.OperationContext, SR.TraceDownloadError);

                    executionState.RestCMD.ErrorStream = new MemoryStream();
                    executionState.RestCMD.ResponseStream.WriteToAsync(executionState.RestCMD.ErrorStream, null /* copyLength */, null /* maxLength */, false /* calculateMd5 */, executionState, new StreamDescriptor(), EndResponseStreamCopy);
                }
                else
                {
                    if (!executionState.RestCMD.RetrieveResponseStream)
                    {
                        executionState.RestCMD.DestinationStream = Stream.Null;
                    }

                    if (executionState.RestCMD.DestinationStream != null)
                    {
                        if (executionState.RestCMD.StreamCopyState == null)
                        {
                            executionState.RestCMD.StreamCopyState = new StreamDescriptor();
                        }

                        executionState.CurrentOperation = ExecutorOperation.BeginDownloadResponse;
                        Logger.LogInformational(executionState.OperationContext, SR.TraceDownload);
                        executionState.RestCMD.ResponseStream.WriteToAsync(executionState.RestCMD.DestinationStream, null /* copyLength */, null /* maxLength */, executionState.RestCMD.CalculateMd5ForResponseStream, executionState, executionState.RestCMD.StreamCopyState, EndResponseStreamCopy);
                    }
                    else
                    {
                        // Dont want to copy stream, just want to consume it so end
                        Executor.EndOperation(executionState);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(executionState.OperationContext, SR.TracePreProcessError, ex.Message);
                executionState.ExceptionRef = ExecutorBase.TranslateExceptionBasedOnParseError(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);
                Executor.EndOperation(executionState);
            }
        }

        private static void EndResponseStreamCopy<T>(ExecutionState<T> executionState)
        {
            // At this time, the response/error stream has been read. So try to parse the error if there was en exception
            if (executionState.RestCMD.ErrorStream != null)
            {
                executionState.RestCMD.ErrorStream.Seek(0, SeekOrigin.Begin);
                if (executionState.Cmd.ParseError != null)
                {
                    executionState.ExceptionRef = StorageException.TranslateExceptionWithPreBufferedStream(executionState.ExceptionRef, executionState.Cmd.CurrentResult, stream => executionState.Cmd.ParseError(stream, executionState.Resp, null), executionState.RestCMD.ErrorStream);
                }
                else
                {
                    executionState.ExceptionRef = StorageException.TranslateExceptionWithPreBufferedStream(executionState.ExceptionRef, executionState.Cmd.CurrentResult, null, executionState.RestCMD.ErrorStream);
                }

                // Dispose the stream and end the operation
                try
                {
                    executionState.RestCMD.ErrorStream.Dispose();
                    executionState.RestCMD.ErrorStream = null;
                }
                catch (Exception)
                {
                    // no-op
                }
            }

            // Dispose the stream and end the operation
            try
            {
                if (executionState.RestCMD.ResponseStream != null)
                {
                    executionState.RestCMD.ResponseStream.Dispose();
                    executionState.RestCMD.ResponseStream = null;
                }
            }
            catch (Exception)
            {
                // no-op, because HttpWebResponse.Close should take care of it
            }

            executionState.CurrentOperation = ExecutorOperation.EndDownloadResponse;

            Executor.EndOperation(executionState);
        }
        #endregion

        #region Step 9 - Parse Response
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        private static void EndOperation<T>(ExecutionState<T> executionState)
        {
            Executor.FinishRequestAttempt(executionState);

            try
            {
                // If an operation has been canceled of timed out this should overwrite any exception
                Executor.CheckCancellation(executionState);
                Executor.CheckTimeout(executionState, true);

                // Success
                if (executionState.ExceptionRef == null)
                {
                    // Step 9 - This will not be called if an exception is raised during stream copying
                    Executor.ProcessEndOfRequest(executionState);
                    executionState.OnComplete();
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(executionState.OperationContext, SR.TracePostProcessError, ex.Message);
                executionState.ExceptionRef = ExecutorBase.TranslateExceptionBasedOnParseError(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);
            }
            finally
            {
                try
                {
                    if (executionState.ReqStream != null)
                    {
                        executionState.ReqStream.Dispose();
                        executionState.ReqStream = null;
                    }

                    if (executionState.Resp != null)
                    {
                        executionState.Resp.Close();
                        executionState.Resp = null;
                    }
                }
                catch (Exception)
                {
                    // no op
                }
            }

            // Handle Retry
            try
            {
                StorageException translatedException = TranslateExceptionBasedOnParseError(executionState.ExceptionRef, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);
                executionState.ExceptionRef = translatedException;
                Logger.LogInformational(executionState.OperationContext, SR.TraceRetryCheck, executionState.RetryCount, executionState.Cmd.CurrentResult.HttpStatusCode, translatedException.IsRetryable ? "yes" : "no", translatedException.Message);

                bool shouldRetry = false;
                TimeSpan delay = TimeSpan.Zero;
                if (translatedException.IsRetryable && (executionState.RetryPolicy != null))
                {
                    executionState.CurrentLocation = Executor.GetNextLocation(executionState.CurrentLocation, executionState.RestCMD.LocationMode);
                    Logger.LogInformational(executionState.OperationContext, SR.TraceNextLocation, executionState.CurrentLocation);

                    IExtendedRetryPolicy extendedRetryPolicy = executionState.RetryPolicy as IExtendedRetryPolicy;
                    if (extendedRetryPolicy != null)
                    {
                        RetryContext retryContext = new RetryContext(
                            executionState.RetryCount++,
                            executionState.Cmd.CurrentResult,
                            executionState.CurrentLocation,
                            executionState.RestCMD.LocationMode);

                        RetryInfo retryInfo = extendedRetryPolicy.Evaluate(retryContext, executionState.OperationContext);
                        if (retryInfo != null)
                        {
                            Logger.LogInformational(executionState.OperationContext, SR.TraceRetryInfo, retryInfo.TargetLocation, retryInfo.UpdatedLocationMode);
                            shouldRetry = true;
                            executionState.CurrentLocation = retryInfo.TargetLocation;
                            executionState.RestCMD.LocationMode = retryInfo.UpdatedLocationMode;
                            delay = retryInfo.RetryInterval;
                        }
                    }
                    else
                    {
                        shouldRetry = executionState.RetryPolicy.ShouldRetry(
                            executionState.RetryCount++,
                            executionState.Cmd.CurrentResult.HttpStatusCode,
                            executionState.ExceptionRef,
                            out delay,
                            executionState.OperationContext);
                    }

                    if ((delay < TimeSpan.Zero) || (delay > Constants.MaximumRetryBackoff))
                    {
                        delay = Constants.MaximumRetryBackoff;
                    }
                }

                if (!shouldRetry || (executionState.OperationExpiryTime.HasValue && (DateTime.Now + delay).CompareTo(executionState.OperationExpiryTime.Value) > 0))
                {
                    Logger.LogError(executionState.OperationContext, shouldRetry ? SR.TraceRetryDecisionTimeout : SR.TraceRetryDecisionPolicy, executionState.ExceptionRef.Message);

                    // No Retry
                    executionState.OnComplete();
                }
                else
                {
                    if (executionState.Cmd.RecoveryAction != null)
                    {
                        // I.E. Rewind stream etc.
                        executionState.Cmd.RecoveryAction(executionState.Cmd, executionState.ExceptionRef, executionState.OperationContext);
                    }

                    if (delay > TimeSpan.Zero)
                    {
                        Logger.LogInformational(executionState.OperationContext, SR.TraceRetryDelay, (int)delay.TotalMilliseconds);

                        executionState.UpdateCompletedSynchronously(false);
                        if (executionState.BackoffTimer == null)
                        {
                            executionState.BackoffTimer = new Timer(
                                Executor.RetryRequest<T>,
                                executionState,
                                (int)delay.TotalMilliseconds,
                                Timeout.Infinite);
                        }
                        else
                        {
                            executionState.BackoffTimer.Change((int)delay.TotalMilliseconds, Timeout.Infinite);
                        }

                        executionState.CancelDelegate = () =>
                        {
                            // Disabling the timer here, but there is still a scenario where the user calls cancel after
                            // the timer starts the next retry but before it sets the CancelDelegate back to null. However, even
                            // if that happens, next retry will start and then stop immediately because of the cancelled flag.
                            Timer backoffTimer = executionState.BackoffTimer;
                            if (backoffTimer != null)
                            {
                                executionState.BackoffTimer = null;
                                backoffTimer.Dispose();
                            }

                            Logger.LogWarning(executionState.OperationContext, SR.TraceAbortRetry);
                            Executor.CheckCancellation(executionState);
                            executionState.OnComplete();
                        };
                    }
                    else
                    {
                        // Start next request immediately
                        Executor.RetryRequest<T>(executionState);
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch all ( i.e. users retry policy throws etc.)
                Logger.LogWarning(executionState.OperationContext, SR.TraceRetryError, ex.Message);
                executionState.ExceptionRef = ExecutorBase.TranslateExceptionBasedOnParseError(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);
                executionState.OnComplete();
            }
        }

        private static void RetryRequest<T>(object state)
        {
            ExecutionState<T> executionState = (ExecutionState<T>)state;
            Logger.LogInformational(executionState.OperationContext, SR.TraceRetry);
            Executor.FireRetrying(executionState);
            Executor.InitRequest(executionState);
        }
        #endregion

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        private static void AbortRequest<T>(object state)
        {
            ExecutionState<T> executionState = (ExecutionState<T>)state;
            Logger.LogInformational(executionState.OperationContext, SR.TraceAbort);

            try
            {
                executionState.ReqTimedOut = true;
                executionState.Req.Abort();
            }
            catch (Exception ex)
            {
                Logger.LogError(executionState.OperationContext, SR.TraceAbortError, ex.Message);
            }
        }
        #endregion

        #region Sync
#if SYNC
        public static T ExecuteSync<T>(RESTCommand<T> cmd, IRetryPolicy policy, OperationContext operationContext)
        {
            // Note all code below will reference state, not params directly, this will allow common code with async executor
            using (ExecutionState<T> executionState = new ExecutionState<T>(cmd, policy, operationContext))
            {
                bool shouldRetry = false;
                TimeSpan delay = TimeSpan.Zero;

                do
                {
                    try
                    {
                        executionState.Init();

                        // 0. Begin Request 
                        Executor.StartRequestAttempt(executionState);

                        // Steps 1-4
                        Executor.ProcessStartOfRequest(executionState, SR.TraceStartRequestSync);

                        Executor.CheckTimeout<T>(executionState, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(executionState.OperationContext, SR.TraceInitRequestError, ex.Message);

                        // Store exception and throw here. All operations in this try would be non-retryable by default. At this point, the request is not even made.
                        // Therefore, we will not get extended error info. Hence ParseError doesn't matter here.
                        StorageException storageEx = ExecutorBase.TranslateExceptionBasedOnParseError(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);

                        storageEx.IsRetryable = false;
                        executionState.ExceptionRef = storageEx;
                        throw executionState.ExceptionRef;
                    }

                    // Enter Retryable Section of execution
                    try
                    {
                        // 5. potentially upload data
                        if (cmd.SendStream != null)
                        {
                            executionState.CurrentOperation = ExecutorOperation.BeginGetRequestStream;
                            Logger.LogInformational(executionState.OperationContext, SR.TracePrepareUpload);
                            executionState.Req.Timeout = (int)executionState.RemainingTimeout.TotalMilliseconds;
                            executionState.ReqStream = executionState.Req.GetRequestStream();

                            executionState.CurrentOperation = ExecutorOperation.BeginUploadRequest;
                            Logger.LogInformational(executionState.OperationContext, SR.TraceUpload);
                            MultiBufferMemoryStream multiBufferMemoryStream = cmd.SendStream as MultiBufferMemoryStream;

                            try
                            {
                                if (multiBufferMemoryStream != null && !cmd.SendStreamLength.HasValue)
                                {
                                    multiBufferMemoryStream.FastCopyTo(executionState.ReqStream, executionState.OperationExpiryTime);
                                }
                                else
                                {
                                    // don't calculate md5 here as we should have already set this for auth purposes
                                    executionState.RestCMD.SendStream.WriteToSync(executionState.ReqStream, cmd.SendStreamLength, null /* maxLength */, false, true, executionState, null /* streamCopyState */);
                                }

                                executionState.ReqStream.Flush();
                                executionState.ReqStream.Dispose();
                                executionState.ReqStream = null;
                            }
                            catch (Exception)
                            {
                                executionState.Req.Abort();
                                throw;
                            }
                        }

                        // 6. Get response 
                        try
                        {
                            executionState.CurrentOperation = ExecutorOperation.BeginGetResponse;
                            Logger.LogInformational(executionState.OperationContext, SR.TraceGetResponse);
                            executionState.Req.Timeout = (int)executionState.RemainingTimeout.TotalMilliseconds;
                            executionState.Resp = (HttpWebResponse)executionState.Req.GetResponse();
                            executionState.CurrentOperation = ExecutorOperation.EndGetResponse;
                        }
                        catch (WebException ex)
                        {
                            Logger.LogWarning(executionState.OperationContext, SR.TraceGetResponseError, ex.Message);
                            executionState.Resp = (HttpWebResponse)ex.Response;

                            if (ex.Status == WebExceptionStatus.Timeout || executionState.ReqTimedOut)
                            {
                                throw new TimeoutException();
                            }

                            if (executionState.Resp == null)
                            {
                                throw;
                            }
                            else
                            {
                                executionState.ExceptionRef = ExecutorBase.TranslateExceptionBasedOnParseError(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);
                            }
                        }

                        // Response
                        Logger.LogInformational(executionState.OperationContext, SR.TraceResponse, executionState.Cmd.CurrentResult.HttpStatusCode, executionState.Cmd.CurrentResult.ServiceRequestID, executionState.Cmd.CurrentResult.ContentMd5, executionState.Cmd.CurrentResult.Etag);
                        Executor.FireResponseReceived(executionState);

                        // 7. Do Response parsing (headers etc, no stream available here)
                        if (cmd.PreProcessResponse != null)
                        {
                            executionState.CurrentOperation = ExecutorOperation.PreProcess;
                            executionState.Result = cmd.PreProcessResponse(cmd, executionState.Resp, executionState.ExceptionRef, executionState.OperationContext);

                            // clear exception
                            executionState.ExceptionRef = null;
                            Logger.LogInformational(executionState.OperationContext, SR.TracePreProcessDone);
                        }

                        // 8. (Potentially reads stream from server)
                        executionState.CurrentOperation = ExecutorOperation.GetResponseStream;
                        cmd.ResponseStream = executionState.Resp.GetResponseStream();

                        if (!cmd.RetrieveResponseStream)
                        {
                            cmd.DestinationStream = Stream.Null;
                        }

                        if (cmd.DestinationStream != null)
                        {
                            if (cmd.StreamCopyState == null)
                            {
                                cmd.StreamCopyState = new StreamDescriptor();
                            }

                            try
                            {
                                executionState.CurrentOperation = ExecutorOperation.BeginDownloadResponse;
                                Logger.LogInformational(executionState.OperationContext, SR.TraceDownload);
                                cmd.ResponseStream.WriteToSync(cmd.DestinationStream, null /* copyLength */, null /* maxLength */, cmd.CalculateMd5ForResponseStream, false, executionState, cmd.StreamCopyState);
                            }
                            finally
                            {
                                cmd.ResponseStream.Dispose();
                                cmd.ResponseStream = null;
                            }
                        }

                        // Step 9 - This will not be called if an exception is raised during stream copying
                        Executor.ProcessEndOfRequest(executionState);

                        Executor.FinishRequestAttempt(executionState);
                        return executionState.Result;
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning(executionState.OperationContext, SR.TraceGenericError, e.Message);
                        Executor.FinishRequestAttempt(executionState);

                        StorageException translatedException = ExecutorBase.TranslateExceptionBasedOnParseError(e, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseError);

                        executionState.ExceptionRef = translatedException;
                        Logger.LogInformational(executionState.OperationContext, SR.TraceRetryCheck, executionState.RetryCount, executionState.Cmd.CurrentResult.HttpStatusCode, translatedException.IsRetryable ? "yes" : "no", translatedException.Message);

                        shouldRetry = false;
                        if (translatedException.IsRetryable && (executionState.RetryPolicy != null))
                        {
                            executionState.CurrentLocation = Executor.GetNextLocation(executionState.CurrentLocation, cmd.LocationMode);
                            Logger.LogInformational(executionState.OperationContext, SR.TraceNextLocation, executionState.CurrentLocation);

                            IExtendedRetryPolicy extendedRetryPolicy = executionState.RetryPolicy as IExtendedRetryPolicy;
                            if (extendedRetryPolicy != null)
                            {
                                RetryContext retryContext = new RetryContext(
                                    executionState.RetryCount++,
                                    cmd.CurrentResult,
                                    executionState.CurrentLocation,
                                    cmd.LocationMode);

                                RetryInfo retryInfo = extendedRetryPolicy.Evaluate(retryContext, executionState.OperationContext);
                                if (retryInfo != null)
                                {
                                    Logger.LogInformational(executionState.OperationContext, SR.TraceRetryInfo, retryInfo.TargetLocation, retryInfo.UpdatedLocationMode);
                                    shouldRetry = true;
                                    executionState.CurrentLocation = retryInfo.TargetLocation;
                                    cmd.LocationMode = retryInfo.UpdatedLocationMode;
                                    delay = retryInfo.RetryInterval;
                                }
                            }
                            else
                            {
                                shouldRetry = executionState.RetryPolicy.ShouldRetry(
                                    executionState.RetryCount++,
                                    cmd.CurrentResult.HttpStatusCode,
                                    executionState.ExceptionRef,
                                    out delay,
                                    executionState.OperationContext);
                            }

                            if ((delay < TimeSpan.Zero) || (delay > Constants.MaximumRetryBackoff))
                            {
                                delay = Constants.MaximumRetryBackoff;
                            }
                        }
                    }
                    finally
                    {
                        if (executionState.Resp != null)
                        {
                            executionState.Resp.Close();
                            executionState.Resp = null;
                        }
                    }

                    if (!shouldRetry || (executionState.OperationExpiryTime.HasValue && (DateTime.Now + delay).CompareTo(executionState.OperationExpiryTime.Value) > 0))
                    {
                        Logger.LogError(executionState.OperationContext, shouldRetry ? SR.TraceRetryDecisionTimeout : SR.TraceRetryDecisionPolicy, executionState.ExceptionRef.Message);
                        throw executionState.ExceptionRef;
                    }
                    else
                    {
                        if (executionState.Cmd.RecoveryAction != null)
                        {
                            // I.E. Rewind stream etc.
                            executionState.Cmd.RecoveryAction(executionState.Cmd, executionState.ExceptionRef, executionState.OperationContext);
                        }

                        if (delay > TimeSpan.Zero)
                        {
                            Logger.LogInformational(executionState.OperationContext, SR.TraceRetryDelay, (int)delay.TotalMilliseconds);
                            Thread.Sleep(delay);
                        }

                        Logger.LogInformational(executionState.OperationContext, SR.TraceRetry);
                    }

                    Executor.FireRetrying(executionState);
                }
                while (shouldRetry);
            }

            // should never get here, either return, or throw;
            throw new NotImplementedException(SR.InternalStorageError);
        }
#endif
        #endregion

        #region Common
        private static void ProcessStartOfRequest<T>(ExecutionState<T> executionState, string startLogMessage)
        {
            executionState.CurrentOperation = ExecutorOperation.BeginOperation;

            // 1. Build request
            Uri uri = executionState.RestCMD.StorageUri.GetUri(executionState.CurrentLocation);
            Uri transformedUri = executionState.RestCMD.Credentials.TransformUri(uri);
            Logger.LogInformational(executionState.OperationContext, startLogMessage, transformedUri);
            UriQueryBuilder builder = new UriQueryBuilder(executionState.RestCMD.Builder);

            // Note - The service accepts both api-version and x-ms-version and therefore it is ok to add x-ms-version to all requests.
            executionState.Req = executionState.RestCMD.BuildRequestDelegate(transformedUri, builder, executionState.Cmd.ServerTimeoutInSeconds, true, executionState.OperationContext);
            executionState.CancelDelegate = executionState.Req.Abort;

            // 2. Set Headers
            Executor.ApplyUserHeaders(executionState);
            if (executionState.RestCMD.SetHeaders != null)
            {
                executionState.RestCMD.SetHeaders(executionState.Req, executionState.OperationContext);
            }

            // Set Content-Length and do minor tweaks
            if (executionState.RestCMD.SendStream != null)
            {
                long streamLength = executionState.RestCMD.SendStreamLength ?? executionState.RestCMD.SendStream.Length - executionState.RestCMD.SendStream.Position;
                CommonUtility.ApplyRequestOptimizations(executionState.Req, streamLength);
            }
            else
            {
                CommonUtility.ApplyRequestOptimizations(executionState.Req, -1);
            }

            // Let the user know we are ready to send
            Executor.FireSendingRequest(executionState);

            // 3. Sign Request
            if (executionState.RestCMD.SignRequest != null)
            {
                executionState.RestCMD.SignRequest(executionState.Req, executionState.OperationContext);
            }
#if SYNC
            // 4. Set timeout (this is actually not honored by asynchronous requests)
            executionState.Req.Timeout = (int)executionState.RemainingTimeout.TotalMilliseconds;
#endif
        }

        private static void ProcessEndOfRequest<T>(ExecutionState<T> executionState)
        {
            // 9. Evaluate Response & Parse Results, (Stream potentially available here) 
            if (executionState.RestCMD.PostProcessResponse != null)
            {
                executionState.CurrentOperation = ExecutorOperation.PostProcess;
                Logger.LogInformational(executionState.OperationContext, SR.TracePostProcess);
                executionState.Result = executionState.RestCMD.PostProcessResponse(executionState.RestCMD, executionState.Resp, executionState.OperationContext);
            }

            // 10. If there is a dispose action specified on the command, invoke it.
            if (executionState.RestCMD.DisposeAction != null)
            {
                Logger.LogInformational(executionState.OperationContext, SR.TraceDispose);
                executionState.RestCMD.DisposeAction(executionState.RestCMD);
                executionState.RestCMD.DisposeAction = null;
            }

            executionState.CurrentOperation = ExecutorOperation.EndOperation;
            Logger.LogInformational(executionState.OperationContext, SR.TraceSuccess);
            executionState.CancelDelegate = null;
        }
        #endregion
    }
}