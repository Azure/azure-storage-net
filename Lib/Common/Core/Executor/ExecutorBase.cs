//-----------------------------------------------------------------------
// <copyright file="ExecutorBase.cs" company="Microsoft">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
#if NETCORE
    using System.Net.Http;
#endif

    internal abstract class ExecutorBase
    {
        protected static void ApplyUserHeaders<T>(ExecutionState<T> executionState)
        {
            if (!string.IsNullOrEmpty(executionState.OperationContext.ClientRequestID))
            {
                executionState.Req.Headers.Add(Constants.HeaderConstants.ClientRequestIdHeader, executionState.OperationContext.ClientRequestID);
            }

            if (executionState.OperationContext.UserHeaders != null && executionState.OperationContext.UserHeaders.Count > 0)
            {
                foreach (string key in executionState.OperationContext.UserHeaders.Keys)
                {
                    executionState.Req.Headers.Add(key, executionState.OperationContext.UserHeaders[key]);
                }
            }
        }

        protected static void StartRequestAttempt<T>(ExecutionState<T> executionState)
        {
            // Need to clear this explicitly for retries
            executionState.ExceptionRef = null;

            executionState.Cmd.CurrentResult = new RequestResult() { StartTime = DateTime.Now };

            lock (executionState.OperationContext.RequestResults)
            {
                executionState.OperationContext.RequestResults.Add(executionState.Cmd.CurrentResult);
                executionState.Cmd.RequestResults.Add(executionState.Cmd.CurrentResult);
            }

            RESTCommand<T> cmd = executionState.RestCMD;
            if (cmd != null)
            {
                if (!cmd.StorageUri.ValidateLocationMode(cmd.LocationMode))
                {
                    throw new InvalidOperationException(SR.StorageUriMissingLocation);
                }

                // If the command only allows for a specific location, we should target
                // that location no matter what the retry policy says.
                switch (cmd.CommandLocationMode)
                {
                    case CommandLocationMode.PrimaryOnly:
                        if (cmd.LocationMode == LocationMode.SecondaryOnly)
                        {
                            throw new InvalidOperationException(SR.PrimaryOnlyCommand);
                        }

                        Logger.LogInformational(executionState.OperationContext, SR.PrimaryOnlyCommand);
                        executionState.CurrentLocation = StorageLocation.Primary;
                        cmd.LocationMode = LocationMode.PrimaryOnly;
                        break;

                    case CommandLocationMode.SecondaryOnly:
                        if (cmd.LocationMode == LocationMode.PrimaryOnly)
                        {
                            throw new InvalidOperationException(SR.SecondaryOnlyCommand);
                        }

                        Logger.LogInformational(executionState.OperationContext, SR.SecondaryOnlyCommand);
                        executionState.CurrentLocation = StorageLocation.Secondary;
                        cmd.LocationMode = LocationMode.SecondaryOnly;
                        break;
                }
            }

            executionState.Cmd.CurrentResult.TargetLocation = executionState.CurrentLocation;
        }

        protected static StorageLocation GetNextLocation(StorageLocation lastLocation, LocationMode locationMode)
        {
            switch (locationMode)
            {
                case LocationMode.PrimaryOnly:
                    return StorageLocation.Primary;

                case LocationMode.SecondaryOnly:
                    return StorageLocation.Secondary;

                case LocationMode.PrimaryThenSecondary:
                case LocationMode.SecondaryThenPrimary:
                    return (lastLocation == StorageLocation.Primary) ?
                        StorageLocation.Secondary :
                        StorageLocation.Primary;

                default:
                    CommonUtility.ArgumentOutOfRange("LocationMode", locationMode);
                    return StorageLocation.Primary;
            }
        }

        protected static void FinishRequestAttempt<T>(ExecutionState<T> executionState)
        {
            executionState.Cmd.CurrentResult.EndTime = DateTime.Now;
            executionState.OperationContext.EndTime = DateTime.Now;
            Executor.FireRequestCompleted(executionState);
        }

        protected static void FireSendingRequest<T>(ExecutionState<T> executionState)
        {
            RequestEventArgs args = GenerateRequestEventArgs<T>(executionState);
            executionState.OperationContext.FireSendingRequest(args);
        }

        protected static void FireResponseReceived<T>(ExecutionState<T> executionState)
        {
            RequestEventArgs args = GenerateRequestEventArgs<T>(executionState);
            executionState.OperationContext.FireResponseReceived(args);
        }

        protected static void FireRequestCompleted<T>(ExecutionState<T> executionState)
        {
            RequestEventArgs args = GenerateRequestEventArgs<T>(executionState);
            executionState.OperationContext.FireRequestCompleted(args);
        }

        protected static void FireRetrying<T>(ExecutionState<T> executionState)
        {
            RequestEventArgs args = GenerateRequestEventArgs<T>(executionState);
            executionState.OperationContext.FireRetrying(args);
        }

        private static RequestEventArgs GenerateRequestEventArgs<T>(ExecutionState<T> executionState)
        {
            RequestEventArgs args = new RequestEventArgs(executionState.Cmd.CurrentResult);
#if WINDOWS_RT || NETCORE
            args.RequestUri = executionState.Req.RequestUri;
#else
            args.Request = executionState.Req;
            args.Response = executionState.Resp;
#endif
            return args;
        }

        protected static bool CheckTimeout<T>(ExecutionState<T> executionState, bool throwOnTimeout)
        {
            if (executionState.ReqTimedOut || (executionState.OperationExpiryTime.HasValue && executionState.Cmd.CurrentResult.StartTime.CompareTo(executionState.OperationExpiryTime.Value) > 0))
            {
                executionState.ReqTimedOut = true;
                StorageException storageEx = Exceptions.GenerateTimeoutException(executionState.Cmd.CurrentResult, null);
                executionState.ExceptionRef = storageEx;

                if (throwOnTimeout)
                {
                    throw executionState.ExceptionRef;
                }

                return true;
            }

            return false;
        }

#if WINDOWS_DESKTOP
        protected static bool CheckCancellation<T>(ExecutionState<T> executionState)
        {
            lock (executionState.CancellationLockerObject)
            {
                if (executionState.CancelRequested)
                {
                    executionState.ExceptionRef = Exceptions.GenerateCancellationException(executionState.Cmd.CurrentResult, null);
                }

                return executionState.CancelRequested;
            }
        }
#endif

#if NETCORE
        internal static StorageException TranslateExceptionBasedOnParseError(Exception ex, RequestResult currentResult, HttpResponseMessage response, Func<Stream, HttpResponseMessage, string, StorageExtendedErrorInformation> parseError)
        {
            if (parseError != null)
            {
                return StorageException.TranslateException(
                    ex,
                    currentResult,
                    (stream) => parseError(stream, response, null), response);
            }
            else
            {
               return StorageException.TranslateException(ex, currentResult, null, response);
            }
        }
#else
        internal static StorageException TranslateExceptionBasedOnParseError(Exception ex, RequestResult currentResult, HttpWebResponse response, Func<Stream, HttpWebResponse, string, StorageExtendedErrorInformation> parseError)
        {
            if (parseError != null)
            {
                return StorageException.TranslateException(
                    ex,
                    currentResult,
                    (stream) => parseError(stream, response, null));
            }
            else
            {
                return StorageException.TranslateException(ex, currentResult, null);
            }
        }
#endif

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
        internal static StorageException TranslateDataServiceExceptionBasedOnParseError(Exception ex, RequestResult currentResult, Func<Stream, IDictionary<string, string>, string, StorageExtendedErrorInformation> parseDataServiceError)
        {
            if (parseDataServiceError != null)
            {
                return StorageException.TranslateDataServiceException(
                    ex,
                    currentResult,
                    (stream, headers) => parseDataServiceError(stream, headers, null));
            }
            else
            {
                return StorageException.TranslateDataServiceException(ex, currentResult, null);
            }
        }
#endif
    }
}
