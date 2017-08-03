// -----------------------------------------------------------------------------------------
// <copyright file="ODataErrorHelper.cs" company="Microsoft">
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
    using Microsoft.Data.OData;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
#if WINDOWS_RT   || NETCORE
    using System.Net.Http;
#endif

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
    using Microsoft.WindowsAzure.Storage.Table.DataServices;
#elif WINDOWS_RT
    using Windows.Storage.Streams;
#endif

    /// <summary>
    /// Represents additional functionality for processing extended error information returned by the Windows Azure storage services for Tables.
    /// The class was created to only load the OData library when required for parsing the ExtendedErrorInformation of Tables.
    /// </summary>
    public static class ODataErrorHelper 
    {
        /// <summary>
        /// Gets the error details from the stream using OData library.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="response">The web response.</param>
        /// <param name="contentType">The response Content-Type.</param>
        /// <returns>The error details.</returns>
#if WINDOWS_RT || NETCORE
        public static StorageExtendedErrorInformation ReadFromStreamUsingODataLib(Stream inputStream, HttpResponseMessage response, string contentType)
#else
        public static StorageExtendedErrorInformation ReadFromStreamUsingODataLib(Stream inputStream,
                HttpWebResponse response, string contentType)
#endif
        {
            CommonUtility.AssertNotNull("inputStream", inputStream);
            CommonUtility.AssertNotNull("response", response);

            if (inputStream.CanSeek && inputStream.Length <= 0)
            {
                return null;
            }

            HttpResponseAdapterMessage responseMessage = new HttpResponseAdapterMessage(response,
                new NonCloseableStream(inputStream), contentType);
            return ReadAndParseExtendedError(responseMessage);
        }

        /// <summary>
        /// Gets the error details from the stream using OData library.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="responseHeaders">The web response headers.</param>
        /// <param name="contentType">The response Content-Type.</param>
        /// <returns>The error details.</returns>
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
        public static StorageExtendedErrorInformation ReadDataServiceResponseFromStream(Stream inputStream,
            IDictionary<string, string> responseHeaders, string contentType)
        {
            CommonUtility.AssertNotNull("inputStream", inputStream);

            if (inputStream.CanSeek && inputStream.Length <= 0)
            {
                return null;
            }

            DataServicesResponseAdapterMessage responseMessage = new DataServicesResponseAdapterMessage(responseHeaders,
                inputStream, contentType);
            return ReadAndParseExtendedError(responseMessage);
        }
#endif

        /// <summary>
        /// Parses the error details from the stream using OData library.
        /// </summary>
        /// <param name="responseMessage">The IODataResponseMessage to parse.</param>
        /// <returns>The error details.</returns>
        public static StorageExtendedErrorInformation ReadAndParseExtendedError(IODataResponseMessage responseMessage)
        {
            StorageExtendedErrorInformation storageExtendedError = null;
            using (ODataMessageReader reader = new ODataMessageReader(responseMessage))
            {
                try
                {
                    ODataError error = reader.ReadError();
                    if (error != null)
                    {
                        storageExtendedError = new StorageExtendedErrorInformation();
                        storageExtendedError.AdditionalDetails = new Dictionary<string, string>();
                        storageExtendedError.ErrorCode = error.ErrorCode;
                        storageExtendedError.ErrorMessage = error.Message;
                        if (error.InnerError != null)
                        {
                            storageExtendedError.AdditionalDetails[Constants.ErrorExceptionMessage] =
                                error.InnerError.Message;
                            storageExtendedError.AdditionalDetails[Constants.ErrorExceptionStackTrace] =
                                error.InnerError.StackTrace;
                        }

#if !(WINDOWS_PHONE && WINDOWS_DESKTOP)
                        if (error.InstanceAnnotations.Count > 0)
                        {
                            foreach (ODataInstanceAnnotation annotation in error.InstanceAnnotations)
                            {
                                storageExtendedError.AdditionalDetails[annotation.Name] =
                                    annotation.Value.GetAnnotation<string>();
                            }
                        }
#endif
                    }
                }
                catch (Exception)
                {
                    // Error cannot be parsed. 
                    return null;
                }
            }

            return storageExtendedError;
        }
    }
}
