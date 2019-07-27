//-----------------------------------------------------------------------
// <copyright file="Exceptions.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Core.Util
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Storage.Shared.Protocol;

    using System.IO;
    using System.Globalization;

    internal class Exceptions
    {
#if NETCORE || WINDOWS_RT
        internal async static Task<StorageException> PopulateStorageExceptionFromHttpResponseMessage(HttpResponseMessage response, RequestResult currentResult, Func<Stream, HttpResponseMessage, string, StorageExtendedErrorInformation> parseError)
        {
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    currentResult.HttpStatusMessage = response.ReasonPhrase;
                    currentResult.HttpStatusCode = (int)response.StatusCode;
                    currentResult.ServiceRequestID = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(response.Headers, Constants.HeaderConstants.RequestIdHeader);

                    string tempDate = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(response.Headers, Constants.HeaderConstants.Date);
                    currentResult.RequestDate = string.IsNullOrEmpty(tempDate) ? DateTime.Now.ToString("R", CultureInfo.InvariantCulture) : tempDate;

                    if (response.Headers.ETag != null)
                    {
                        currentResult.Etag = response.Headers.ETag.ToString();
                    }

                    if (response.Content != null && response.Content.Headers.ContentMD5 != null)
                    {
                        currentResult.ContentMd5 = Convert.ToBase64String(response.Content.Headers.ContentMD5);
                    }

                    if (response.Content != null && response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ContentCrc64Header) != null)
                    {
                        currentResult.ContentCrc64 = HttpResponseParsers.GetContentCRC64(response);
                    }

                    currentResult.ErrorCode = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(response.Headers, Constants.HeaderConstants.StorageErrorCodeHeader);

                }
                catch (Exception)
                {
                    // no op
                }

                try
                {
                    Stream errStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    if (parseError != null)
                    {
                        currentResult.ExtendedErrorInformation = parseError(errStream, response, response.Content.Headers.ContentType.ToString());
                    }
                    else
                    {
                        currentResult.ExtendedErrorInformation = await StorageExtendedErrorInformation.ReadFromStreamAsync(errStream.AsInputStream()).ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                    // no op
                }

                return new StorageException(currentResult, response.ReasonPhrase, null);
            }
            else
            {
                return null;
            }
        }
#else
        internal async static Task<StorageException> PopulateStorageExceptionFromHttpResponseMessage(HttpResponseMessage response, RequestResult currentResult, CancellationToken token, Func<Stream, HttpResponseMessage, string, CancellationToken, Task<StorageExtendedErrorInformation>> parseError)
        {
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    currentResult.HttpStatusMessage = response.ReasonPhrase;
                    currentResult.HttpStatusCode = (int)response.StatusCode;
                    currentResult.ServiceRequestID = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(response.Headers, Constants.HeaderConstants.RequestIdHeader);
                    
                    string tempDate = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(response.Headers, Constants.HeaderConstants.Date);
                    currentResult.RequestDate = string.IsNullOrEmpty(tempDate) ? DateTime.Now.ToString("R", CultureInfo.InvariantCulture) : tempDate;
                    
                    if (response.Headers.ETag != null)
                    {
                        currentResult.Etag = response.Headers.ETag.ToString();
                    }

                    if (response.Content != null && response.Content.Headers.ContentMD5 != null)
                    {
                        currentResult.ContentMd5 = Convert.ToBase64String(response.Content.Headers.ContentMD5);
                    }

                    if (response.Content != null && HttpResponseParsers.GetContentCRC64(response) != null)
                    {
                        currentResult.ContentCrc64 = HttpResponseParsers.GetContentCRC64(response);
                    }

                    currentResult.ErrorCode = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(response.Headers, Constants.HeaderConstants.StorageErrorCodeHeader);
                    
                }
                catch (Exception)
                {
                    // no op
                }

                try
                {
                    Stream errStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    if (parseError != null)
                    {
                        currentResult.ExtendedErrorInformation = await parseError(errStream, response, response.Content.Headers.ContentType.ToString(), token).ConfigureAwait(false);
                    }
                    else
                    {
                        currentResult.ExtendedErrorInformation = await StorageExtendedErrorInformation.ReadFromStreamAsync(errStream, token).ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                    // no op
                }

                return new StorageException(currentResult, response.ReasonPhrase, null);
            }
            else
            {
                return null;
            }
        }
#endif

        internal static StorageException GenerateTimeoutException(RequestResult res, Exception inner)
        {
            if (res != null)
            {
                res.HttpStatusCode = 408; // RequestTimeout
            }

            TimeoutException timeoutEx = new TimeoutException(SR.TimeoutExceptionMessage, inner);
            return new StorageException(res, timeoutEx.Message, timeoutEx)
            {
                IsRetryable = false
            };
        }

#if !(NETCORE || WINDOWS_RT)
        internal static StorageException GenerateCancellationException(RequestResult res, Exception inner)
        {
            if (res != null)
            {
                res.HttpStatusCode = 306;
                res.HttpStatusMessage = "Unused";
            }

            OperationCanceledException cancelEx = new OperationCanceledException(SR.OperationCanceled, inner);
            return new StorageException(res, cancelEx.Message, cancelEx) { IsRetryable = false };
        }
#endif
    }
}
