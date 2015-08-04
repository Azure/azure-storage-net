//-----------------------------------------------------------------------
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
// <summary>
//    Contains code for the CloudStorageAccount class.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Blob.Protocol;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;

    /// <summary>
    /// Provides methods for parsing responses to operations on files in the File service.
    /// </summary>
    public static partial class FileHttpResponseParsers
    {
        /// <summary>
        /// Gets the request ID from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A unique value associated with the request.</returns>
        public static string GetRequestId(HttpWebResponse response)
        {
            return Response.GetRequestId(response);
        }

        /// <summary>
        /// Gets the file's properties from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The file's properties.</returns>
        public static FileProperties GetProperties(HttpWebResponse response)
        {
            CommonUtility.AssertNotNull("response", response);

            FileProperties properties = new FileProperties();
            properties.ETag = HttpResponseParsers.GetETag(response);

#if WINDOWS_PHONE 
            properties.LastModified = HttpResponseParsers.GetLastModified(response);
            properties.ContentLanguage = response.Headers[Constants.HeaderConstants.ContentLanguageHeader];
#else
            properties.LastModified = response.LastModified.ToUniversalTime();
            properties.ContentLanguage = response.Headers[HttpResponseHeader.ContentLanguage];
#endif

            properties.ContentDisposition = response.Headers[Constants.HeaderConstants.ContentDispositionResponseHeader];
            properties.ContentEncoding = response.Headers[HttpResponseHeader.ContentEncoding];
            properties.ContentMD5 = response.Headers[HttpResponseHeader.ContentMd5];
            properties.ContentType = response.Headers[HttpResponseHeader.ContentType];
            properties.CacheControl = response.Headers[HttpResponseHeader.CacheControl];

            // Get the content length. Prioritize range and x-ms over content length for the special cases.
            string rangeHeader = response.Headers[HttpResponseHeader.ContentRange];
            string contentLengthHeader = response.Headers[Constants.HeaderConstants.ContentLengthHeader];
            string fileContentLengthHeader = response.Headers[Constants.HeaderConstants.FileContentLengthHeader];
            if (!string.IsNullOrEmpty(rangeHeader))
            {
                properties.Length = long.Parse(rangeHeader.Split('/')[1], CultureInfo.InvariantCulture);
            }
            else if (!string.IsNullOrEmpty(fileContentLengthHeader))
            {
                properties.Length = long.Parse(fileContentLengthHeader, CultureInfo.InvariantCulture);
            }
            else if (!string.IsNullOrEmpty(contentLengthHeader))
            {
                // On Windows Phone, ContentLength property is not always same as Content-Length header,
                // so we try to parse the header first.
                properties.Length = long.Parse(contentLengthHeader, CultureInfo.InvariantCulture);
            }
            else
            {
                properties.Length = response.ContentLength;
            }

            return properties;
        }

        /// <summary>
        /// Gets the user-defined metadata.
        /// </summary>
        /// <param name="response">The response from server.</param>
        /// <returns>A <see cref="System.Collections.IDictionary"/> of the metadata.</returns>
        public static IDictionary<string, string> GetMetadata(HttpWebResponse response)
        {
            return HttpResponseParsers.GetMetadata(response);
        }

        /// <summary>
        /// Extracts a <see cref="CopyState"/> object from the headers of a web response.
        /// </summary>
        /// <param name="response">The HTTP web response.</param>
        /// <returns>A <see cref="CopyState"/> object, or <c>null</c> if the web response does not include copy state.</returns>
        public static CopyState GetCopyAttributes(HttpWebResponse response)
        {
            return BlobHttpResponseParsers.GetCopyAttributes(response);
        }
    }
}
