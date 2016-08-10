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

namespace Microsoft.WindowsAzure.Storage
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.IO;
    using System.Net;
    using System.Text;

#if WINDOWS_DESKTOP 
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Serialization;
#elif WINDOWS_RT || NETCORE
    using System.Runtime.InteropServices;
#endif

#if NETCORE
    using System.Net.Http;
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
            this.IsRetryable = true;
        }

        /// <summary>
        /// Translates the specified exception into a <see cref="StorageException"/>.
        /// </summary>
        /// <param name="ex">The exception to translate.</param>
        /// <param name="reqResult">The request result.</param>
        /// <returns>The storage exception.</returns>
        /// <returns>An exception of type <see cref="StorageException"/>.</returns>
        public static StorageException TranslateException(Exception ex, RequestResult reqResult)
        {
            return TranslateException(ex, reqResult, null);
        }

        /// <summary>
        /// Translates the specified exception into a storage exception.
        /// </summary>
        /// <param name="ex">The exception to translate.</param>
        /// <param name="reqResult">The request result.</param>
        /// <param name="parseError">The delegate used to parse the error to get extended error information.</param>
        /// <returns>The storage exception.</returns>
        public static StorageException TranslateException(Exception ex, RequestResult reqResult, Func<Stream, StorageExtendedErrorInformation> parseError)
        {
            StorageException storageException;

            try
            {
                if ((storageException = CoreTranslate(ex, reqResult, ref parseError)) != null)
                {
                    return storageException;
                }

#if !(NETCORE)
                WebException we = ex as WebException;
                if (we != null)
                {
                    HttpWebResponse response = we.Response as HttpWebResponse;
                    if (response != null)
                    {
                        StorageException.PopulateRequestResult(reqResult, response);
                        reqResult.ExtendedErrorInformation = parseError(response.GetResponseStream());
                    }
                }
#endif
            }
            catch (Exception)
            {
                // if there is an error thrown while parsing the service error, just wrap the service error in a StorageException.
                // no op
            }

            // Just wrap in StorageException
            return new StorageException(reqResult, ex.Message, ex);
        }

#if NETCORE
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
                    reqResult.ExtendedErrorInformation = CommonUtility.RunWithoutSynchronizationContext(() => StorageExtendedErrorInformation.ReadFromStream(response.Content.ReadAsStreamAsync().Result));
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
        /// <returns>The storage exception.</returns>
        internal static StorageException TranslateExceptionWithPreBufferedStream(Exception ex, RequestResult reqResult, Func<Stream, StorageExtendedErrorInformation> parseError, Stream responseStream)
        {
            StorageException storageException;

            try
            {
                if ((storageException = CoreTranslate(ex, reqResult, ref parseError)) != null)
                {
                    return storageException;
                }

#if !(NETCORE)
                WebException we = ex as WebException;
                if (we != null)
                {
                    HttpWebResponse response = we.Response as HttpWebResponse;
                    if (response != null)
                    {
                        PopulateRequestResult(reqResult, response);
                        reqResult.ExtendedErrorInformation = parseError(responseStream);
                    }
                }
#endif
            }
            catch (Exception)
            {
                // if there is an error thrown while parsing the service error, just wrap the service error in a StorageException.
                // no op
            }

            // Just wrap in StorageException
            return new StorageException(reqResult, ex.Message, ex);
        }

#if NETCORE
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
                    reqResult.ExtendedErrorInformation = StorageExtendedErrorInformation.ReadFromStream(responseStream);
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
                parseError = StorageExtendedErrorInformation.ReadFromStream;
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
#if WINDOWS_RT || NETCORE
            else if (ex is OperationCanceledException)
            {
                reqResult.HttpStatusMessage = null;
                reqResult.HttpStatusCode = 306; // unused
                reqResult.ExtendedErrorInformation = null;
                return new StorageException(reqResult, ex.Message, ex);
            }
#elif WINDOWS_DESKTOP && !WINDOWS_PHONE
            else
            {
                // Should never get to this one since we will call TranslateDataServiceException for DataService operations.
                StorageException tableEx = TableUtilities.TranslateDataServiceException(ex, reqResult, null);

                if (tableEx != null)
                {
                    return tableEx;
                }
            }
#endif
            // return null and check in the caller
            return null;
        }

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
        /// <summary>
        /// Translates the specified exception into a storage exception.
        /// </summary>
        /// <param name="ex">The exception to translate.</param>
        /// <param name="reqResult">The request result.</param>
        /// <param name="parseError">The delegate used to parse the error to get extended error information.</param>
        /// <returns>The storage exception.</returns>
        internal static StorageException TranslateDataServiceException(Exception ex, RequestResult reqResult, Func<Stream, IDictionary<string, string>, StorageExtendedErrorInformation> parseError)
        {
            CommonUtility.AssertNotNull("reqResult", reqResult);
            CommonUtility.AssertNotNull("ex", ex);
            CommonUtility.AssertNotNull("parseError", parseError);

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
            else
            {
                StorageException tableEx = TableUtilities.TranslateDataServiceException(ex, reqResult, parseError);

                if (tableEx != null)
                {
                    return tableEx;
                }
            }

            // Just wrap in StorageException
            return new StorageException(reqResult, ex.Message, ex);
        }
#endif

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
        private static void PopulateRequestResult(RequestResult reqResult, HttpWebResponse response)
        {
            reqResult.HttpStatusMessage = response.StatusDescription;
            reqResult.HttpStatusCode = (int)response.StatusCode;
            if (response.Headers != null)
            {
#if WINDOWS_DESKTOP
                reqResult.ServiceRequestID = HttpWebUtility.TryGetHeader(response, Constants.HeaderConstants.RequestIdHeader, null);
                reqResult.ContentMd5 = HttpWebUtility.TryGetHeader(response, "Content-MD5", null);
                string tempDate = HttpWebUtility.TryGetHeader(response, "Date", null);
                reqResult.RequestDate = string.IsNullOrEmpty(tempDate) ? DateTime.Now.ToString("R", CultureInfo.InvariantCulture) : tempDate;
                reqResult.Etag = response.Headers[HttpResponseHeader.ETag];
#endif
            }

#if !WINDOWS_RT
            if (response.ContentLength > 0) 
            {
                reqResult.IngressBytes += response.ContentLength;
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

                if (this.RequestInformation.ExtendedErrorInformation != null)
                {
                    sb.AppendLine("ErrorCode:" + this.RequestInformation.ExtendedErrorInformation.ErrorCode);
                }
            }

            return sb.ToString();
        }
    }
}