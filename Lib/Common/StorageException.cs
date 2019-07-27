// -----------------------------------------------------------------------------------------
// <copyright file="StorageException.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage
{
    using Microsoft.Azure.Storage.Core.Util;
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Net.Http;
#if WINDOWS_DESKTOP 
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Serialization;
#elif WINDOWS_RT || NETCORE
    using System.Runtime.InteropServices;
#endif



    /// <summary>
    /// Represents an exception thrown by the Azure Storage service.
    /// </summary>
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
    [Serializable]
#endif

    public class StorageException : Exception
    {
        /// <summary>
        /// Gets the <see cref="RequestResult"/> object for this <see cref="StorageException"/> object.
        /// </summary>
        /// <value>The <see cref="RequestResult"/> object for this <see cref="StorageException"/> object.</value>
        public RequestResult RequestInformation { get; private set; }

        /// <summary>
        /// Indicates if exception is retryable.
        /// </summary>
        internal bool IsRetryable { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageException"/> class.
        /// </summary>
        public StorageException() : this(null /* res */, null /* message */, null /* inner */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageException"/> class using the specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public StorageException(string message) :
            this(null /* res */, message, null /* inner */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageException"/> class with a specified error message and a reference to the inner exception that generated this exception.
        /// </summary>
        /// <param name="message">The exception error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public StorageException(string message, Exception innerException) :
            this(null /* res */, message, innerException)
        {
        }

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> object that holds serialized object data for the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <remarks>This constructor is called during de-serialization to reconstitute the exception object transmitted over a stream.</remarks>
        protected StorageException(SerializationInfo info, StreamingContext context) :
            base(info, context) 
        {
            if (info != null)
            {
                this.IsRetryable = info.GetBoolean("IsRetryable");
                this.RequestInformation = (RequestResult)info.GetValue("RequestInformation", typeof(RequestResult));
            }
        }

        /// <summary>
        /// Populates a <see cref="System.Runtime.Serialization.SerializationInfo"/> object with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> object to populate with data.</param>
        /// <param name="context">The destination context for this serialization.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info != null)
            {
                info.AddValue("IsRetryable", this.IsRetryable);
                info.AddValue("RequestInformation", this.RequestInformation, typeof(RequestResult));
            }

            base.GetObjectData(info, context);
        }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageException"/> class by using the specified parameters.
        /// </summary>
        /// <param name="res">The request result.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The inner exception.</param>
        public StorageException(RequestResult res, string message, Exception inner)
            : base(message, inner)
        {
            this.RequestInformation = res;
            //HttpClient errors: retrying might lead to hang becuse of the maximum number of connections
            this.IsRetryable = true;
        }

        /// <summary>
        /// Translates the specified exception into a storage exception.
        /// </summary>
        /// <param name="ex">The exception to translate.</param>
        /// <param name="reqResult">The request result.</param>
        /// <param name="parseErrorAsync">The delegate used to parse the error to get extended error information.</param>
        /// <param name="cancellationToken">cancellation token for the async operation</param>
        /// 
        /// <returns>The storage exception.</returns>
        public static async Task<StorageException> TranslateExceptionAsync(Exception ex, RequestResult reqResult, Func<Stream, CancellationToken, Task<StorageExtendedErrorInformation>> parseErrorAsync, CancellationToken cancellationToken, HttpResponseMessage response)
        {
            StorageException storageException;

            try
            {
                if (parseErrorAsync == null)
                {
                    parseErrorAsync = StorageExtendedErrorInformation.ReadFromStreamAsync;
                }

                if ((storageException = CoreTranslateAsync(ex, reqResult, cancellationToken)) != null)
                {
                    return storageException;
                }
                
                if (response != null)
                {
                    StorageException.PopulateRequestResult(reqResult, response);
                    reqResult.ExtendedErrorInformation = await parseErrorAsync(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // if there is an error thrown while parsing the service error, just wrap the service error in a StorageException.
                // The idea is that it's more helpful for the user to get the actual service error than a "parsing failed" error.
            }

            // Just wrap in StorageException
            return new StorageException(reqResult, ex.Message, ex);
        }

#if NETCORE || WINDOWS_RT
        /// <summary>
        /// Translates the specified exception into a storage exception.
        /// </summary>
        /// <param name="ex">The exception to translate.</param>
        /// <param name="reqResult">The request result.</param>
        /// <param name="parseError">The delegate used to parse the error to get extended error information.</param>
        /// <param name="response">HTTP response message</param>
        /// <returns>The storage exception.</returns>
        public static StorageException TranslateException(Exception ex, RequestResult reqResult, Func<Stream, StorageExtendedErrorInformation> parseError, HttpResponseMessage response)
        {
            StorageException storageException;

            try
            {
                if ((storageException = CoreTranslate(ex, reqResult, ref parseError)) != null)
                {
                    return storageException;
                }

                if (response != null)
                {
                    StorageException.PopulateRequestResult(reqResult, response);
                    reqResult.ExtendedErrorInformation = StorageExtendedErrorInformation.ReadFromStreamAsync(response.Content.ReadAsStreamAsync().GetAwaiter().GetResult()).GetAwaiter().GetResult();
                }
            }
            catch (Exception)
            {
                // if there is an error thrown while parsing the service error, just wrap the service error in a StorageException.
                // no op
            }

            // Just wrap in StorageException
            return new StorageException(reqResult, ex.Message, ex);
        }
#endif

        /// <summary>
        /// Translates the specified exception into a storage exception.
        /// </summary>
        /// <param name="ex">The exception to translate.</param>
        /// <param name="reqResult">The request result.</param>
        /// <param name="parseError">The delegate used to parse the error to get extended error information.</param>
        /// <param name="responseStream">The error stream that contains the error information.</param>
        /// <param name="response">HTTP response message</param>
        /// <returns>The storage exception.</returns>
        internal static StorageException TranslateExceptionWithPreBufferedStream(Exception ex, RequestResult reqResult, Func<Stream, StorageExtendedErrorInformation> parseError, Stream responseStream, HttpResponseMessage response)
        {
            StorageException storageException;

            try
            {
                if ((storageException = CoreTranslate(ex, reqResult, ref parseError)) != null)
                {
                    return storageException;
                }

                if (response != null)
                {
                    PopulateRequestResult(reqResult, response);
                    reqResult.ExtendedErrorInformation = StorageExtendedErrorInformation.ReadFromStreamAsync(responseStream).GetAwaiter().GetResult();
                }
            }
            catch (Exception)
            {
                // if there is an error thrown while parsing the service error, just wrap the service error in a StorageException.
                // no op
            }

            // Just wrap in StorageException
            return new StorageException(reqResult, ex.Message, ex);
        }

        /// <summary>
        /// Tries to translate the specified exception into a storage exception.
        /// </summary>
        /// <param name="ex">The exception to translate.</param>
        /// <param name="reqResult">The request result.</param>
        /// <param name="parseError">The delegate used to parse the error to get extended error information.</param>
        /// <returns>The storage exception or <c>null</c>.</returns>
        private static StorageException CoreTranslate(Exception ex, RequestResult reqResult, ref Func<Stream, StorageExtendedErrorInformation> parseError)
        {
            CommonUtility.AssertNotNull("reqResult", reqResult);
            CommonUtility.AssertNotNull("ex", ex);

            if (parseError == null)
            {
                parseError = s => StorageExtendedErrorInformation.ReadFromStreamAsync(s).GetAwaiter().GetResult();
            }

            // Dont re-wrap storage exceptions
            if (ex is StorageException)
            {
                return (StorageException)ex;
            }
            else if (ex is TimeoutException)
            {
                reqResult.HttpStatusMessage = null;
                reqResult.HttpStatusCode = (int)HttpStatusCode.RequestTimeout;
                reqResult.ExtendedErrorInformation = null;
                return new StorageException(reqResult, ex.Message, ex);
            }
            else if (ex is ArgumentException)
            {
                reqResult.HttpStatusMessage = null;
                reqResult.HttpStatusCode = (int)HttpStatusCode.Unused;
                reqResult.ExtendedErrorInformation = null;
                return new StorageException(reqResult, ex.Message, ex) { IsRetryable = false };
            }
            // return null and check in the caller
            return null;
        }

        /// <summary>
        /// Tries to translate the specified exception into a storage exception.
        /// Note: we can probably combine this with the above CoreTranslate, this doesn't need to be async.
        /// </summary>
        /// <param name="ex">The exception to translate.</param>
        /// <param name="reqResult">The request result.</param>
        /// <param name="parseError">The delegate used to parse the error to get extended error information.</param>
        /// <returns>The storage exception or <c>null</c>.</returns>
        private static StorageException CoreTranslateAsync(Exception ex, RequestResult reqResult, CancellationToken token)
        {
            CommonUtility.AssertNotNull("reqResult", reqResult);
            CommonUtility.AssertNotNull("ex", ex);

            // Dont re-wrap storage exceptions
            if (ex is StorageException)
            {
                return (StorageException)ex;
            }
            else if (ex is TimeoutException)
            {
                reqResult.HttpStatusMessage = null;
                reqResult.HttpStatusCode = (int)HttpStatusCode.RequestTimeout;
                reqResult.ExtendedErrorInformation = null;
                return new StorageException(reqResult, ex.Message, ex);
            }
            else if (ex is ArgumentException)
            {
                reqResult.HttpStatusMessage = null;
                reqResult.HttpStatusCode = (int)HttpStatusCode.Unused;
                reqResult.ExtendedErrorInformation = null;
                return new StorageException(reqResult, ex.Message, ex) { IsRetryable = false };
            }
            else if (ex is OperationCanceledException)
            {
                reqResult.HttpStatusMessage = null;
                reqResult.HttpStatusCode = 306; // unused
                reqResult.ExtendedErrorInformation = null;
                return new StorageException(reqResult, ex.Message, ex);
            }
            // return null and check in the caller
            return null;
        }


        /// <summary>
        /// Populate the RequestResult.
        /// </summary>
        /// <param name="reqResult">The request result.</param>
        /// <param name="response">The web response.</param>
#if NETCORE
        private static void PopulateRequestResult(RequestResult reqResult, HttpResponseMessage response)
        {
            reqResult.HttpStatusMessage = response.StatusCode.ToString();
            reqResult.HttpStatusCode = (int)response.StatusCode;
        }
#else
        private static void PopulateRequestResult(RequestResult reqResult, HttpResponseMessage response)
        {
            reqResult.HttpStatusMessage = response.ReasonPhrase;
            reqResult.HttpStatusCode = (int)response.StatusCode;
#if !WINDOWS_RT
            if (response.Headers != null)
            {

                reqResult.ServiceRequestID = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(response.Headers, Constants.HeaderConstants.RequestIdHeader);
                reqResult.RequestDate = response.Headers.Date.HasValue ? response.Headers.Date.Value.UtcDateTime.ToString("R", CultureInfo.InvariantCulture) : null;
                reqResult.Etag = response.Headers.ETag?.ToString();
                reqResult.ErrorCode = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(response.Headers, Constants.HeaderConstants.StorageErrorCodeHeader);
            }

            if (response.Content != null && response.Content.Headers != null)
            {
                reqResult.ContentMd5 = response.Content.Headers.ContentMD5 != null ? Convert.ToBase64String(response.Content.Headers.ContentMD5) : null;
                reqResult.ContentCrc64 = HttpResponseParsers.GetContentCRC64(response);
                reqResult.IngressBytes += response.Content.Headers.ContentLength.HasValue ? (long)response.Content.Headers.ContentLength : 0;
            }
#endif
        }
#endif

        /// <summary>
        /// Represents an exception thrown by the Microsoft Azure storage client library. 
        /// </summary>
        /// <returns>A string that represents the exception.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(base.ToString());

            if (this.RequestInformation != null)
            {
                sb.AppendLine("Request Information");
                sb.AppendLine("RequestID:" + this.RequestInformation.ServiceRequestID);
                sb.AppendLine("RequestDate:" + this.RequestInformation.RequestDate);
                sb.AppendLine("StatusMessage:" + this.RequestInformation.HttpStatusMessage);
                sb.AppendLine("ErrorCode:" + this.RequestInformation.ErrorCode);

                if (this.RequestInformation.ExtendedErrorInformation != null)
                {
                    sb.AppendLine("ErrorMessage:" + this.RequestInformation.ExtendedErrorInformation.ErrorMessage);
                }
            }

            return sb.ToString();
        }
    }
}