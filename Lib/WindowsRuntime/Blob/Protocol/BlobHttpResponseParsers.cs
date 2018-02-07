// -----------------------------------------------------------------------------------------
// <copyright file="BlobHttpResponseParsers.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;

#if NETCORE
    using System.Net.Http.Headers;
    public
#else
    internal
#endif
        static partial class BlobHttpResponseParsers
    {
        /// <summary>
        /// Gets the blob's properties from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The blob's properties.</returns>
        public static BlobProperties GetProperties(HttpResponseMessage response)
        {
            BlobProperties properties = new BlobProperties();

            if (response.Content != null)
            {
                properties.LastModified = response.Content.Headers.LastModified;
#if NETCORE
                HttpContentHeaders contentHeaders = response.Content.Headers;
                properties.ContentEncoding = HttpWebUtility.GetHeaderValues("Content-Encoding", contentHeaders);
                properties.ContentLanguage = HttpWebUtility.GetHeaderValues("Content-Language", contentHeaders);
                properties.ContentDisposition = HttpWebUtility.GetHeaderValues("Content-Disposition", contentHeaders);
                properties.ContentType = HttpWebUtility.GetHeaderValues("Content-Type", contentHeaders);
#else
                properties.ContentEncoding = HttpWebUtility.CombineHttpHeaderValues(response.Content.Headers.ContentEncoding);
                properties.ContentLanguage = HttpWebUtility.CombineHttpHeaderValues(response.Content.Headers.ContentLanguage);

                if (response.Content.Headers.ContentDisposition != null)
                {
                    properties.ContentDisposition = response.Content.Headers.ContentDisposition.ToString();
                }
                if (response.Content.Headers.ContentType != null)
                {
                    properties.ContentType = response.Content.Headers.ContentType.ToString();
                }
#endif

                if (response.Content.Headers.ContentMD5 != null && response.Content.Headers.ContentRange == null)
                {
                    properties.ContentMD5 = Convert.ToBase64String(response.Content.Headers.ContentMD5);
                }
                else if (!string.IsNullOrEmpty(response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.BlobContentMD5Header)))
                {
                    properties.ContentMD5 = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.BlobContentMD5Header);
                }

                string blobEncryption = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ServerEncrypted);
                properties.IsServerEncrypted = string.Equals(blobEncryption, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);

                string incrementalCopy = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.IncrementalCopyHeader);
                properties.IsIncrementalCopy = string.Equals(incrementalCopy, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);

                // Get the content length. Prioritize range and x-ms over content length for the special cases.
                string contentLengthHeader = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.BlobContentLengthHeader);
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
#if NETCORE
            properties.CacheControl = HttpWebUtility.GetHeaderValues("Cache-Control", response.Headers);
#else
            if (response.Headers.CacheControl != null)
            {
                properties.CacheControl = response.Headers.CacheControl.ToString();
            }
#endif

            if (response.Headers.ETag != null)
            {
                properties.ETag = response.Headers.ETag.ToString();
            }

            // Get blob type
            string blobType = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.BlobType);
            if (!string.IsNullOrEmpty(blobType))
            {
                properties.BlobType = (BlobType)Enum.Parse(typeof(BlobType), blobType, true);
            }

            // Get lease properties
            properties.LeaseStatus = GetLeaseStatus(response);
            properties.LeaseState = GetLeaseState(response);
            properties.LeaseDuration = GetLeaseDuration(response);

            // Get sequence number
            string sequenceNumber = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.BlobSequenceNumber);
            if (!string.IsNullOrEmpty(sequenceNumber))
            {
                properties.PageBlobSequenceNumber = long.Parse(sequenceNumber, CultureInfo.InvariantCulture);
            }

            // Get committed block count
            string comittedBlockCount = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.BlobCommittedBlockCount);
            if (!string.IsNullOrEmpty(comittedBlockCount))
            {
                properties.AppendBlobCommittedBlockCount = int.Parse(comittedBlockCount, CultureInfo.InvariantCulture);
            }

            // Get the tier of the blob
            string premiumBlobTierInferredString = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.AccessTierInferredHeader);
            if (!string.IsNullOrEmpty(premiumBlobTierInferredString))
            {
                properties.BlobTierInferred = Convert.ToBoolean(premiumBlobTierInferredString);
            }
            
            string blobTierString = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.AccessTierHeader);
            StandardBlobTier? standardBlobTier;
            PremiumPageBlobTier? premiumPageBlobTier;
            BlobHttpResponseParsers.GetBlobTier(properties.BlobType, blobTierString, out standardBlobTier, out premiumPageBlobTier);
            properties.StandardBlobTier = standardBlobTier;
            properties.PremiumPageBlobTier = premiumPageBlobTier;

            // Get the rehydration status
            string rehydrationStatusString = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ArchiveStatusHeader);
            if (!string.IsNullOrEmpty(rehydrationStatusString))
            {
                if (Constants.RehydratePendingToHot.Equals(rehydrationStatusString))
                {
                    properties.RehydrationStatus = RehydrationStatus.PendingToHot;
                }
                else if (Constants.RehydratePendingToCool.Equals(rehydrationStatusString))
                {
                    properties.RehydrationStatus = RehydrationStatus.PendingToCool;
                }
                else
                {
                    properties.RehydrationStatus = RehydrationStatus.Unknown;
                }
            }
            
            if ((properties.PremiumPageBlobTier.HasValue || properties.StandardBlobTier.HasValue) && !properties.BlobTierInferred.HasValue)
            {
                properties.BlobTierInferred = false;
            }

            // Get the time the tier of the blob was last modified
            string accessTierChangeTimeString = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.AccessTierChangeTimeHeader);
            if (!string.IsNullOrEmpty(accessTierChangeTimeString))
            {
                properties.BlobTierLastModifiedTime = DateTimeOffset.Parse(accessTierChangeTimeString, CultureInfo.InvariantCulture);
            }

            return properties;
        }

        /// <summary>
        /// Extracts the lease status from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A <see cref="LeaseStatus"/> enumeration from the web response.</returns>
        /// <remarks>If the appropriate header is not present, a status of <see cref="LeaseStatus.Unspecified"/> is returned.</remarks>
        /// <exception cref="System.ArgumentException">The header contains an unrecognized value.</exception>
        public static LeaseStatus GetLeaseStatus(HttpResponseMessage response)
        {
            string leaseStatus = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.LeaseStatus);
            return GetLeaseStatus(leaseStatus);
        }

        /// <summary>
        /// Extracts the lease state from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A <see cref="LeaseState"/> enumeration from the web response.</returns>
        /// <remarks>If the appropriate header is not present, a status of <see cref="LeaseState.Unspecified"/> is returned.</remarks>
        /// <exception cref="System.ArgumentException">The header contains an unrecognized value.</exception>
        public static LeaseState GetLeaseState(HttpResponseMessage response)
        {
            string leaseState = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.LeaseState);
            return GetLeaseState(leaseState);
        }

        /// <summary>
        /// Extracts the lease duration from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A <see cref="LeaseDuration"/> enumeration from the web response.</returns>
        /// <remarks>If the appropriate header is not present, a status of <see cref="LeaseDuration.Unspecified"/> is returned.</remarks>
        /// <exception cref="System.ArgumentException">The header contains an unrecognized value.</exception>
        public static LeaseDuration GetLeaseDuration(HttpResponseMessage response)
        {
            string leaseDuration = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.LeaseDurationHeader);
            return GetLeaseDuration(leaseDuration);
        }

        /// <summary>
        /// Extracts the lease ID header from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The lease ID.</returns>
        public static string GetLeaseId(HttpResponseMessage response)
        {
            return response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.LeaseIdHeader);
        }

        /// <summary>
        /// Extracts the remaining lease time from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The remaining lease time, in seconds.</returns>
        public static int? GetRemainingLeaseTime(HttpResponseMessage response)
        {
            string leaseTime = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.LeaseTimeHeader);
            int remainingLeaseTime;
            if (int.TryParse(leaseTime, out remainingLeaseTime))
            {
                return remainingLeaseTime;
            }
            else
            {
                return null;
            }
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
        /// Extracts a <see cref="CopyState"/> object from the headers of a web response.
        /// </summary>
        /// <param name="response">The HTTP web response.</param>
        /// <returns>A <see cref="CopyState"/> object, or null if the web response does not contain a copy status.</returns>
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

        /// <summary>
        /// Gets the snapshot timestamp from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The snapshot timestamp.</returns>
        public static string GetSnapshotTime(HttpResponseMessage response)
        {
            return response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.SnapshotHeader);
        }
    }
}
