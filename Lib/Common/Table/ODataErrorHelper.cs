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
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
#if WINDOWS_RT   || NETCORE
    using System.Net.Http;
#endif

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
#elif WINDOWS_RT
    using Windows.Storage.Streams;
#endif

    /// <summary>
    /// Represents additional functionality for processing extended error information returned by the Windows Azure storage services for Tables.
    /// </summary>
    public static class ODataErrorHelper
    {
        /// <summary>
        /// Gets the error details from the stream.
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

#if WINDOWS_RT || NETCORE
            string actualContentType = response.Content.Headers.ContentType.ToString();
#else
            string actualContentType = response.ContentType;
#endif
            // Some table operations respond with XML - request body too large, for example.
            if (actualContentType.Contains(@"xml"))
            {
                return StorageExtendedErrorInformation.ReadFromStream(inputStream);
            }

            return ReadAndParseExtendedError(new NonCloseableStream(inputStream));
        }

        /// <summary>
        /// Parses the error details from the stream
        /// </summary>
        /// <param name="inputStream">The stream to parse.</param>
        /// <returns>The error details.</returns>
        public static StorageExtendedErrorInformation ReadAndParseExtendedError(Stream inputStream)
        {
            return CommonUtility.RunWithoutSynchronizationContext(() => ReadAndParseExtendedErrorAsync(inputStream, CancellationToken.None).GetAwaiter().GetResult());
        }

        /// <summary>
        /// Parses the error details from the stream.
        /// </summary>
        /// <param name="responseStream">The stream to parse.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the request.</param>
        /// <returns>The error details.</returns>
        public static async Task<StorageExtendedErrorInformation> ReadAndParseExtendedErrorAsync(Stream responseStream, CancellationToken cancellationToken)
        {
            try
            {
                StreamReader streamReader = new StreamReader(responseStream);
                using (JsonReader reader = new JsonTextReader(streamReader))
                {
                    reader.DateParseHandling = DateParseHandling.None;
                    JObject dataSet = await JObject.LoadAsync(reader, cancellationToken).ConfigureAwait(false);

                    Dictionary<string, object> properties = dataSet.ToObject<Dictionary<string, object>>(DefaultSerializer.Instance);

                    StorageExtendedErrorInformation errorInformation = new StorageExtendedErrorInformation();

                    errorInformation.AdditionalDetails = new Dictionary<string, string>();
                    if (properties.ContainsKey(@"odata.error"))
                    {
                        Dictionary<string, object> errorProperties = ((JObject)properties[@"odata.error"]).ToObject<Dictionary<string, object>>(DefaultSerializer.Instance);
                        if (errorProperties.ContainsKey(@"code"))
                        {
#pragma warning disable 618
                            errorInformation.ErrorCode = (string)errorProperties[@"code"];
#pragma warning restore 618
                        }
                        if (errorProperties.ContainsKey(@"message"))
                        {
                            Dictionary<string, object> errorMessageProperties = ((JObject)errorProperties[@"message"]).ToObject<Dictionary<string, object>>(DefaultSerializer.Instance);
                            if (errorMessageProperties.ContainsKey(@"value"))
                            {
                                errorInformation.ErrorMessage = (string)errorMessageProperties[@"value"];
                            }
                        }
                        if (errorProperties.ContainsKey(@"innererror"))
                        {
                            Dictionary<string, object> innerErrorDictionary = ((JObject)errorProperties[@"innererror"]).ToObject<Dictionary<string, object>>(DefaultSerializer.Instance);
                            if (innerErrorDictionary.ContainsKey(@"message"))
                            {
                                errorInformation.AdditionalDetails[Constants.ErrorExceptionMessage] = (string)innerErrorDictionary[@"message"];
                            }

                            if (innerErrorDictionary.ContainsKey(@"type"))
                            {
                                errorInformation.AdditionalDetails[Constants.ErrorException] = (string)innerErrorDictionary[@"type"];
                            }

                            if (innerErrorDictionary.ContainsKey(@"stacktrace"))
                            {
                                errorInformation.AdditionalDetails[Constants.ErrorExceptionStackTrace] = (string)innerErrorDictionary[@"stacktrace"];
                            }
                        }
                    }

                    return errorInformation;
                }
            }
            catch (Exception)
            {
                // Exception cannot be parsed, better to throw the original exception than the error-parsing exception.
                return null;
            }
        }
    }
}
