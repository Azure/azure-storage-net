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

namespace Microsoft.Azure.Storage.File.Protocol
{
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Headers;

    public static partial class FileHttpResponseParsers
    {
        /// <summary>
        /// Gets the file's properties from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The file's properties.</returns>
        public static FileProperties GetProperties(HttpResponseMessage response)
        {
            CommonUtility.AssertNotNull("response", response);
            FileProperties properties = new FileProperties();

            if (response.Content != null)
            {
                properties.LastModified = response.Content.Headers.LastModified;
                HttpContentHeaders contentHeaders = response.Content.Headers;
                properties.ContentEncoding = HttpWebUtility.GetHeaderValues("Content-Encoding", contentHeaders);
                properties.ContentLanguage = HttpWebUtility.GetHeaderValues("Content-Language", contentHeaders);
                properties.ContentDisposition = HttpWebUtility.GetHeaderValues("Content-Disposition", contentHeaders);
                properties.ContentType = HttpWebUtility.GetHeaderValues("Content-Type", contentHeaders);

                if (response.Content.Headers.ContentMD5 != null && response.Content.Headers.ContentRange == null)
                {
                    properties.ContentChecksum.MD5 = Convert.ToBase64String(response.Content.Headers.ContentMD5);
                }
                else if (!string.IsNullOrEmpty(response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.FileContentMD5Header)))
                {
                    properties.ContentChecksum.MD5 = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.FileContentMD5Header);
                }

                if (!string.IsNullOrEmpty(response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.FileContentCRC64Header)))
                {
                    properties.ContentChecksum.CRC64 = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.FileContentCRC64Header);
                }

                string fileEncryption = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ServerEncrypted);
                properties.IsServerEncrypted = string.Equals(fileEncryption, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);

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

            properties.CacheControl = HttpWebUtility.GetHeaderValues("Cache-Control", response.Headers);

            if (response.Headers.ETag != null)
            {
                properties.ETag = response.Headers.ETag.ToString();
            }

            return properties;
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
        public static CopyState GetCopyAttributes(HttpResponseMessage response)
        {
            string copyStatusString = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.CopyStatusHeader);
            if (!string.IsNullOrEmpty(copyStatusString))
            {
                return GetCopyAttributes(
                    copyStatusString,
                    response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.CopyIdHeader),
                    response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.CopySourceHeader),
                    response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.CopyProgressHeader),
                    response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.CopyCompletionTimeHeader),
                    response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.CopyDescriptionHeader),
                    response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.CopyDestinationSnapshotHeader));
            }
            else
            {
                return null;
            }
        }
    }
}
