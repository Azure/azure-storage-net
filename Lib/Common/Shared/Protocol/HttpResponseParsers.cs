// -----------------------------------------------------------------------------------------
// <copyright file="HttpResponseParsers.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Shared.Protocol
{
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    internal static partial class HttpResponseParsers
    {
        internal static T ProcessExpectedStatusCodeNoException<T>(HttpStatusCode expectedStatusCode, HttpResponseMessage resp, T retVal, StorageCommandBase<T> cmd, Exception ex)
        {
            return ProcessExpectedStatusCodeNoException(expectedStatusCode, resp != null ? resp.StatusCode : HttpStatusCode.Unused, retVal, cmd, ex);
        }

        internal static T ProcessExpectedStatusCodeNoException<T>(HttpStatusCode[] expectedStatusCodes, HttpResponseMessage resp, T retVal, StorageCommandBase<T> cmd, Exception ex)
        {
            return ProcessExpectedStatusCodeNoException(expectedStatusCodes, resp != null ? resp.StatusCode : HttpStatusCode.Unused, retVal, cmd, ex);
        }

        internal static string GetETag(HttpResponseMessage response)
        {
            return response.Headers.ETag != null ? response.Headers.ETag.ToString() : null;
        }

        /// <summary>
        /// Parses the server request encrypted response header.
        /// </summary>
        /// <param name="response">Response to be parsed.</param>
        /// <returns><c>true</c> if write content was encrypted by service or <c>false</c> if not.</returns>
        internal static bool ParseServerRequestEncrypted(HttpResponseMessage response)
        {
            string requestEncrypted = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ServerRequestEncrypted);
            return string.Equals(requestEncrypted, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);
        }

        internal static string ParseEncryptionKeySHA256(HttpResponseMessage response)
        {
            return response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ClientProvidedEncyptionKeyHash);
        }

        internal static bool ParseServiceEncrypted(HttpResponseMessage response)
        {
            string serviceEncrypted = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ServerEncrypted);
            return string.Equals(serviceEncrypted, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);
        }

        internal static string ParseEncryptionScope(HttpResponseMessage response)
        {
            return response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.EncryptionScopeHeader);
        }

        /// <summary>
        /// Gets the metadata or properties.
        /// </summary>
        /// <param name="response">The response from server.</param>
        /// <param name="prefix">The prefix for all the headers.</param>
        /// <returns>A <see cref="IDictionary"/> of the headers with the prefix.</returns>
        private static IDictionary<string, string> GetMetadataOrProperties(HttpResponseMessage response, string prefix)
        {
            IDictionary<string, string> nameValues = prefix == Constants.HeaderConstants.PrefixForStorageMetadata 
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>();
            HttpResponseHeaders headers = response.Headers;
            int prefixLength = prefix.Length;

            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                if (header.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    nameValues[header.Key.Substring(prefixLength)] = string.Join(",", header.Value);
                }
            }

            return nameValues;
        }

                /// <summary>
        /// Gets an ETag from a response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentType(HttpResponseMessage response)
        {
            return response.Content.Headers.ContentType != null ? response.Content.Headers.ContentType.ToString() : null;
        }

        /// <summary>
        /// Gets an ETag from a response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentRange(HttpResponseMessage response)
        {
            return response.Content.Headers.ContentRange != null ? response.Content.Headers.ContentRange.ToString() : null;
        }

        /// <summary>
        /// Gets an ContentMD5 from a response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A ContentMD5 string.</returns>
        internal static string GetContentMD5(HttpResponseMessage response)
        {
            return response.Content.Headers.ContentMD5 != null ? Convert.ToBase64String(response.Content.Headers.ContentMD5) : null;
        }

        /// <summary>
        /// Gets an ContentCRC64 from a response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A ContentCRC64 string.</returns>
        internal static string GetContentCRC64(HttpResponseMessage response)
        {
            return GetHeader(response, Constants.HeaderConstants.ContentCrc64Header);
        }

        /// <summary>
        /// Gets an ETag from a response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentLocation(HttpResponseMessage response)
        {
            return response.Content.Headers.ContentLocation != null ? response.Content.Headers.ContentLocation.ToString() : null;
        }

        /// <summary>
        /// Gets an ETag from a response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentLength(HttpResponseMessage response)
        {
            return response.Content.Headers.ContentLength != null ? response.Content.Headers.ContentLength.ToString() : null;
        }

        /// <summary>
        /// Gets an ETag from a response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentLanguage(HttpResponseMessage response)
        {
            return response.Content.Headers.ContentLanguage != null ? response.Content.Headers.ContentLanguage.ToString() : null;
        }


        /// <summary>
        /// Gets an ETag from a response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentEncoding(HttpResponseMessage response)
        {
            return response.Content.Headers.ContentEncoding != null ? response.Content.Headers.ContentEncoding.ToString() : null;
        }

        /// <summary>
        /// Gets an ETag from a response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static string GetContentDisposition(HttpResponseMessage response)
        {
            return response.Content.Headers.ContentDisposition != null ? response.Content.Headers.ContentDisposition.ToString() : null;
        }

        internal static string GetCacheControl(HttpResponseMessage response)
        {
            return response.Headers.CacheControl != null ? response.Headers.CacheControl.ToString() : null;
        }

        internal static string GetHeader(HttpResponseMessage response, string headerName)
        {
            if (response.Content != null && response.Content.Headers.Contains(headerName))
            {
                return response.Content.Headers.GetValues(headerName).First();
            }
            else if (response.Headers.Contains(headerName))
            {
                return response.Headers.GetValues(headerName).First();
            }
            else
            {
                return null;
            }
        }

        internal static List<string> GetAllHeaders(HttpResponseMessage response)
        {
            return response.Headers.Concat(response.Content.Headers).Select(r => r.Key).ToList();
        }

        /// <summary>
        /// Gets an ETag from a response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A quoted ETag string.</returns>
        internal static DateTimeOffset GetLastModifiedTime(HttpResponseMessage response)
        {
            return response.Content.Headers.LastModified.GetValueOrDefault();
        }
        internal static Stream GetResponseStream(HttpResponseMessage response)
        {
            return response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the user-defined metadata.
        /// </summary>
        /// <param name="response">The response from server.</param>
        /// <returns>A <see cref="System.Collections.IDictionary"/> of the metadata.</returns>
        internal static IDictionary<string, string> GetMetadata(HttpResponseMessage response)
        {
            return GetMetadataOrProperties(response, Constants.HeaderConstants.PrefixForStorageMetadata);
        }
    }
}
