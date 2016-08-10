// -----------------------------------------------------------------------------------------
// <copyright file="FileHttpResponseParsers.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Blob.Protocol;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

#if NETCORE
    public
#else
    internal
#endif
        static partial class FileHttpResponseParsers
    {
        /// <summary>
        /// Gets the file's properties from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The file's properties.</returns>
        public static FileProperties GetProperties(HttpResponseMessage response)
        {
            FileProperties properties = new FileProperties();

            if (response.Content != null)
            {
                properties.LastModified = response.Content.Headers.LastModified;

                properties.ContentEncoding = HttpWebUtility.CombineHttpHeaderValues(response.Content.Headers.ContentEncoding);
                properties.ContentLanguage = HttpWebUtility.CombineHttpHeaderValues(response.Content.Headers.ContentLanguage);

                if (response.Content.Headers.ContentDisposition != null)
                {
                    properties.ContentDisposition = response.Content.Headers.ContentDisposition.ToString();
                }

                if (response.Content.Headers.ContentMD5 != null)
                {
                    properties.ContentMD5 = Convert.ToBase64String(response.Content.Headers.ContentMD5);
                }

                if (response.Content.Headers.ContentType != null)
                {
                    properties.ContentType = response.Content.Headers.ContentType.ToString();
                }

                // Get the content length. Prioritize range and x-ms over content length for the special cases.
                string contentLengthHeader = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.FileContentLengthHeader);
                if ((response.Content.Headers.ContentRange != null) &&
                    response.Content.Headers.ContentRange.HasLength)
                {
                    properties.Length = response.Content.Headers.ContentRange.Length.Value;
                }
                else if (!string.IsNullOrEmpty(contentLengthHeader))
                {
                    properties.Length = long.Parse(contentLengthHeader);
                }
                else if (response.Content.Headers.ContentLength.HasValue)
                {
                    properties.Length = response.Content.Headers.ContentLength.Value;
                }
            }

            if (response.Headers.CacheControl != null)
            {
                properties.CacheControl = response.Headers.CacheControl.ToString();
            }

            if (response.Headers.ETag != null)
            {
                properties.ETag = response.Headers.ETag.ToString();
            }

            return properties;
        }

        /// <summary>
        /// Extracts a <see cref="CopyState"/> object from the headers of a web response.
        /// </summary>
        /// <param name="response">The HTTP web response.</param>
        /// <returns>A <see cref="CopyState"/> object, or null if the web response does not contain a copy status.</returns>
        public static CopyState GetCopyAttributes(HttpResponseMessage response)
        {
            return BlobHttpResponseParsers.GetCopyAttributes(response);
        }

        /// <summary>
        /// Gets the user-defined metadata.
        /// </summary>
        /// <param name="response">The response from server.</param>
        /// <returns>A <see cref="IDictionary"/> of the metadata.</returns>
        public static IDictionary<string, string> GetMetadata(HttpResponseMessage response)
        {
            return HttpResponseParsers.GetMetadata(response);
        }
    }
}
