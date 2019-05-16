// -----------------------------------------------------------------------------------------
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
// -----------------------------------------------------------------------------------------

namespace Microsoft.Azure.Storage.Core.Executor
{
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.RetryPolicies;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Executor : ExecutorBase
    {
        #region IAsyncAction
        public static Task ExecuteAsyncNullReturn(RESTCommand<NullType> cmd, IRetryPolicy policy, OperationContext operationContext)
        {
            return ExecuteAsyncNullReturn(cmd, policy, operationContext, CancellationToken.None);
        }

        public static Task ExecuteAsyncNullReturn(RESTCommand<NullType> cmd, IRetryPolicy policy, OperationContext operationContext, CancellationToken token)
        {
            return ExecuteAsyncInternal(cmd, policy, operationContext, token);
        }
        #endregion

        #region IAsyncOperation
        public static Task<T> ExecuteAsync<T>(RESTCommand<T> cmd, IRetryPolicy policy, OperationContext operationContext)
        {
            return ExecuteAsync(cmd, policy, operationContext, CancellationToken.None);
        }

        public static Task<T> ExecuteAsync<T>(RESTCommand<T> cmd, IRetryPolicy policy, OperationContext operationContext, CancellationToken token)
        {
            return ExecuteAsyncInternal(cmd, policy, operationContext, token);
        }
        #endregion

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        private async static Task<T> ExecuteAsyncInternal<T>(RESTCommand<T> cmd, IRetryPolicy policy, OperationContext operationContext, CancellationToken token)
        {
            // Note all code below will reference state, not params directly, this will allow common code with multiple executors (APM, Sync, Async)
            using (ExecutionState<T> executionState = new ExecutionState<T>(cmd, policy, operationContext))
            using (CancellationTokenSource timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                bool shouldRetry = false;
                TimeSpan delay = TimeSpan.Zero;

                // Note - The service accepts both api-version and x-ms-version and therefore it is ok to add x-ms-version to all requests.
                
                // Create a new client
                HttpClient client = cmd.HttpClient ?? HttpClientFactory.Instance;

                do
                {
                    try
                    {
                        executionState.Init();

                        // 0. Begin Request 
                        Executor.StartRequestAttempt(executionState);

                        // 1. Build request and content
                        executionState.CurrentOperation = ExecutorOperation.BeginOperation;

                        // Content is re-created every retry, as HttpClient disposes it after a successful request
                        HttpContent content = cmd.BuildContent != null ? cmd.BuildContent(cmd, executionState.OperationContext) : null;

                        // This is so the old auth header etc is cleared out, the content is where serialization occurs which is the major perf hit
                        Uri uri = cmd.StorageUri.GetUri(executionState.CurrentLocation);
                        Uri transformedUri = cmd.Credentials.TransformUri(uri);
                        Logger.LogInformational(executionState.OperationContext, SR.TraceStartRequestAsync, transformedUri);
                        UriQueryBuilder builder = new UriQueryBuilder(executionState.RestCMD.Builder);
                        executionState.Req = cmd.BuildRequest(cmd, transformedUri, builder, content, cmd.ServerTimeoutInSeconds, executionState.OperationContext);

                        // 2. Set Headers
                        Executor.ApplyUserHeaders(executionState);

                        // Let the user know we are ready to send
                        Executor.FireSendingRequest(executionState);

                        // 3. Sign Request is not needed, as HttpClient will call us

                        // 4. Set timeout
                        if (executionState.OperationExpiryTime.HasValue)
                        {
                            // set the token to cancel after timing out, if the higher token hasn't already been cancelled
                            timeoutTokenSource.CancelAfter(executionState.RemainingTimeout);
                        }
                        else
                        {
                            // effectively prevent timeout
                            timeoutTokenSource.CancelAfter(int.MaxValue);
                        }

                        Executor.CheckTimeout<T>(executionState, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(executionState.OperationContext, SR.TraceInitRequestError, ex.Message);

                        // Store exception and throw here. All operations in this try would be non-retryable by default
                        StorageException storageEx = await StorageException.TranslateExceptionAsync(ex, executionState.Cmd.CurrentResult, null, timeoutTokenSource.Token, executionState.Resp).ConfigureAwait(false);
                        storageEx.IsRetryable = false;
                        executionState.ExceptionRef = storageEx;

                        throw executionState.ExceptionRef;
                    }

                    // Enter Retryable Section of execution
                    try
                    {
                        // Send Request 
                        executionState.CurrentOperation = ExecutorOperation.BeginGetResponse;
                        Logger.LogInformational(executionState.OperationContext, SR.TraceGetResponse);
                        executionState.Resp = await client.SendAsync(executionState.Req, HttpCompletionOption.ResponseHeadersRead, timeoutTokenSource.Token).ConfigureAwait(false);
                        executionState.CurrentOperation = ExecutorOperation.EndGetResponse;

                        // Check that echoed client ID matches the one we sent
                        var clientRequestId = HttpRequestParsers.GetHeader(executionState.Req, Constants.HeaderConstants.ClientRequestIdHeader);
                        var echoedClientRequestId = HttpResponseParsers.GetHeader(executionState.Resp, Constants.HeaderConstants.ClientRequestIdHeader);

                        if (echoedClientRequestId != null && echoedClientRequestId != clientRequestId)
                        {
                            var requestId = HttpResponseParsers.GetHeader(executionState.Resp, Constants.HeaderConstants.RequestIdHeader);
                            var storageEx = new StorageException($"Echoed client request ID: {echoedClientRequestId} does not match sent client request ID: {clientRequestId}.  Service request ID: {requestId}")
                            {
                                IsRetryable = false
                            };
                            executionState.ExceptionRef = storageEx;
                            throw storageEx;
                        }

                        // Since HttpClient wont throw for non success, manually check and populate an exception
                        if (!executionState.Resp.IsSuccessStatusCode)
                        {
                            // At this point, don't try to read the stream to parse the error
                            executionState.ExceptionRef = await Exceptions.PopulateStorageExceptionFromHttpResponseMessage(executionState.Resp, executionState.Cmd.CurrentResult, executionState.Cmd.ParseError).ConfigureAwait(false);
                        }

                        Logger.LogInformational(executionState.OperationContext, SR.TraceResponse, executionState.Cmd.CurrentResult.HttpStatusCode, executionState.Cmd.CurrentResult.ServiceRequestID, executionState.Cmd.CurrentResult.ContentMd5, executionState.Cmd.CurrentResult.Etag);
                        Executor.FireResponseReceived(executionState);

                        // 7. Do Response parsing (headers etc, no stream available here)
                        if (cmd.PreProcessResponse != null)
                        {
                            executionState.CurrentOperation = ExecutorOperation.PreProcess;

                            try
                            {
                                executionState.Result = cmd.PreProcessResponse(cmd, executionState.Resp, executionState.ExceptionRef, executionState.OperationContext);

                                // clear exception
                                executionState.ExceptionRef = null;
                            }
                            catch (Exception ex)
                            {
                                executionState.ExceptionRef = ex;
                            }

                            Logger.LogInformational(executionState.OperationContext, SR.TracePreProcessDone);
                        }

                        // 8. (Potentially reads stream from server)
                        executionState.CurrentOperation = ExecutorOperation.GetResponseStream;
                        cmd.ResponseStream = await executionState.Resp.Content.ReadAsStreamAsync().ConfigureAwait(false);

                        // The stream is now available in ResponseStream. Use the stream to parse out the response or error
                        if (executionState.ExceptionRef != null)
                        {
                            executionState.CurrentOperation = ExecutorOperation.BeginDownloadResponse;
                            Logger.LogInformational(executionState.OperationContext, SR.TraceDownloadError);

                            try
                            {
                                cmd.ErrorStream = new MemoryStream();
                                await cmd.ResponseStream.WriteToAsync(cmd.ErrorStream, default(IBufferManager), null /* copyLength */, null /* maxLength */, false, executionState, new StreamDescriptor(), timeoutTokenSource.Token).ConfigureAwait(false);

                                cmd.ErrorStream.Seek(0, SeekOrigin.Begin);
                                executionState.ExceptionRef = StorageException.TranslateExceptionWithPreBufferedStream(executionState.ExceptionRef, executionState.Cmd.CurrentResult, stream => executionState.Cmd.ParseError(stream, executionState.Resp, null), cmd.ErrorStream, executionState.Resp);
                                throw executionState.ExceptionRef;
                            }
                            finally
                            {
                                cmd.ResponseStream.Dispose();
                                cmd.ResponseStream = null;

                                cmd.ErrorStream.Dispose();
                                cmd.ErrorStream = null;
                            }
                        }
                        else
                        {
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
                                    await cmd.ResponseStream.WriteToAsync(cmd.DestinationStream, default(IBufferManager), null /* copyLength */, null /* maxLength */, cmd.CalculateMd5ForResponseStream, executionState, cmd.StreamCopyState, timeoutTokenSource.Token).ConfigureAwait(false);
                                }
                                finally
                                {
                                    cmd.ResponseStream.Dispose();
                                    cmd.ResponseStream = null;
                                }
                            }
                        }

                        // 9. Evaluate Response & Parse Results, (Stream potentially available here) 
                        if (cmd.PostProcessResponseAsync != null)
                        {
                            executionState.CurrentOperation = ExecutorOperation.PostProcess;
                            Logger.LogInformational(executionState.OperationContext, SR.TracePostProcess);
                            executionState.Result = await cmd.PostProcessResponseAsync(cmd, executionState.Resp, executionState.OperationContext, timeoutTokenSource.Token).ConfigureAwait(false);
                        }

                        executionState.CurrentOperation = ExecutorOperation.EndOperation;
                        Logger.LogInformational(executionState.OperationContext, SR.TraceSuccess);
                        Executor.FinishRequestAttempt(executionState);

                        return executionState.Result;
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning(executionState.OperationContext, SR.TraceGenericError, e.Message);
                        Executor.FinishRequestAttempt(executionState);

                        if (e is TaskCanceledException && (executionState.OperationExpiryTime.HasValue && DateTime.Now.CompareTo(executionState.OperationExpiryTime.Value) > 0))
                        {
                            e = new TimeoutException(SR.TimeoutExceptionMessage, e);
                        }
#if NETCORE || WINDOWS_RT
                        StorageException translatedException = StorageException.TranslateException(e, executionState.Cmd.CurrentResult, stream => executionState.Cmd.ParseError(stream, executionState.Resp, null), executionState.Resp);
#else
                        StorageException translatedException = StorageException.TranslateException(e, executionState.Cmd.CurrentResult, stream => executionState.Cmd.ParseError(stream, executionState.Resp, null));
#endif
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
                            executionState.Resp.Dispose();
                            executionState.Resp = null;
                        }
                    }

                    // potentially backoff
                    if (!shouldRetry || (executionState.OperationExpiryTime.HasValue && (DateTime.Now + delay).CompareTo(executionState.OperationExpiryTime.Value) > 0))
                    {
                        Logger.LogError(executionState.OperationContext, shouldRetry ? SR.TraceRetryDecisionTimeout : SR.TraceRetryDecisionPolicy, executionState.ExceptionRef.Message);
                        throw executionState.ExceptionRef;
                    }
                    else
                    {
                        if (cmd.RecoveryAction != null)
                        {
                            // I.E. Rewind stream etc.
                            cmd.RecoveryAction(cmd, executionState.Cmd.CurrentResult.Exception, executionState.OperationContext);
                        }

                        if (delay > TimeSpan.Zero)
                        {
                            await Task.Delay(delay, token).ConfigureAwait(false);
                        }

                        Logger.LogInformational(executionState.OperationContext, SR.TraceRetry);
                    }

                    Executor.FireRetrying(executionState);
                }
                while (shouldRetry);

                // should never get here
                throw new NotImplementedException(SR.InternalStorageError);
            }
        }
    }
}
