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

namespace Microsoft.Azure.Storage.File.Protocol
{
#if ALL_SERVICES
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Azure.Storage.Blob.Protocol;
#endif
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
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

            // For range gets, only look at 'x-ms-content-md5' for overall MD5
            if (response.Headers[HttpResponseHeader.ContentRange] != null)
            {
                properties.ContentMD5 = response.Headers[Constants.HeaderConstants.FileContentMD5Header];
            }
            else
            {
                properties.ContentMD5 = response.Headers[HttpResponseHeader.ContentMd5];
            }

            properties.ContentType = response.Headers[HttpResponseHeader.ContentType];
            properties.CacheControl = response.Headers[HttpResponseHeader.CacheControl];

            string fileEncryption = response.Headers[Constants.HeaderConstants.ServerEncrypted];
            properties.IsServerEncrypted = string.Equals(fileEncryption, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);

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
#if ALL_SERVICES
        /// <summary>
        /// Extracts a <see cref="CopyState"/> object from the headers of a web response.
        /// </summary>
        /// <param name="response">The HTTP web response.</param>
        /// <returns>A <see cref="CopyState"/> object, or <c>null</c> if the web response does not include copy state.</returns>
        public static CopyState GetCopyAttributes(HttpWebResponse response)
        {
            return BlobHttpResponseParsers.GetCopyAttributes(response);
        }
#else
        /// <summary>
        /// Builds a <see cref="CopyState"/> object from the given strings containing formatted copy information.
        /// </summary>
        /// <param name="copyStatusString">The copy status, as a string.</param>
        /// <param name="copyId">The copy ID.</param>
        /// <param name="copySourceString">The source URI of the copy, as a string.</param>
        /// <param name="copyProgressString">A string formatted as progressBytes/TotalBytes.</param>
        /// <param name="copyCompletionTimeString">The copy completion time, as a string, or <c>null</c>.</param>
        /// <param name="copyStatusDescription">The copy status description, if any.</param>
        /// <param name="copyDestinationSnapshotTimeString">The incremental destination snapshot time for the latest incremental copy</param>
        /// <returns>A <see cref="CopyState"/> object populated from the given strings.</returns>
        internal static CopyState GetCopyAttributes(
            string copyStatusString,
            string copyId,
            string copySourceString,
            string copyProgressString,
            string copyCompletionTimeString,
            string copyStatusDescription,
            string copyDestinationSnapshotTimeString)
        {
            CopyState copyAttributes = new CopyState
            {
                CopyId = copyId,
                StatusDescription = copyStatusDescription
            };

            switch (copyStatusString)
            {
                case Constants.CopySuccessValue:
                    copyAttributes.Status = CopyStatus.Success;
                    break;

                case Constants.CopyPendingValue:
                    copyAttributes.Status = CopyStatus.Pending;
                    break;

                case Constants.CopyAbortedValue:
                    copyAttributes.Status = CopyStatus.Aborted;
                    break;

                case Constants.CopyFailedValue:
                    copyAttributes.Status = CopyStatus.Failed;
                    break;

                default:
                    copyAttributes.Status = CopyStatus.Invalid;
                    break;
            }

            if (!string.IsNullOrEmpty(copyProgressString))
            {
                string[] progressSequence = copyProgressString.Split('/');
                copyAttributes.BytesCopied = long.Parse(progressSequence[0], CultureInfo.InvariantCulture);
                copyAttributes.TotalBytes = long.Parse(progressSequence[1], CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(copySourceString))
            {
                copyAttributes.Source = new Uri(copySourceString);
            }

            if (!string.IsNullOrEmpty(copyCompletionTimeString))
            {
                copyAttributes.CompletionTime = copyCompletionTimeString.ToUTCTime();
            }

            if (!string.IsNullOrEmpty(copyDestinationSnapshotTimeString))
            {
                copyAttributes.DestinationSnapshotTime = copyDestinationSnapshotTimeString.ToUTCTime();
            }

            return copyAttributes;
        }

        /// <summary>
        /// Extracts a <see cref="CopyState"/> object from the headers of a web response.
        /// </summary>
        /// <param name="response">The HTTP web response.</param>
        /// <returns>A <see cref="CopyState"/> object, or <c>null</c> if the web response does not include copy state.</returns>
        public static CopyState GetCopyAttributes(HttpWebResponse response)
        {
            CommonUtility.AssertNotNull("response", response);

            string copyStatusString = response.Headers[Constants.HeaderConstants.CopyStatusHeader];
            if (!string.IsNullOrEmpty(copyStatusString))
            {
                return GetCopyAttributes(
                    copyStatusString,
                    response.Headers[Constants.HeaderConstants.CopyIdHeader],
                    response.Headers[Constants.HeaderConstants.CopySourceHeader],
                    response.Headers[Constants.HeaderConstants.CopyProgressHeader],
                    response.Headers[Constants.HeaderConstants.CopyCompletionTimeHeader],
                    response.Headers[Constants.HeaderConstants.CopyDescriptionHeader],
                    response.Headers[Constants.HeaderConstants.CopyDestinationSnapshotHeader]);
            }
            else
            {
                return null;
            }
        }
#endif
    }
}
