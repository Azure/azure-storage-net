﻿//-----------------------------------------------------------------------
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
// <summary>
//    Contains code for the CloudStorageAccount class.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.Azure.Storage.Blob.Protocol
{
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;

    /// <summary>
    /// Provides a set of methods for parsing a response containing blob data from the Blob service.
    /// </summary>
    public static partial class BlobHttpResponseParsers
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
        /// Gets the blob's properties from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The blob's properties.</returns>
        public static BlobProperties GetProperties(HttpWebResponse response)
        {
            CommonUtility.AssertNotNull("response", response);

            BlobProperties properties = new BlobProperties();
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

            // For range gets, only look at 'x-ms-blob-content-md5' for overall MD5
            if (response.Headers[HttpResponseHeader.ContentRange] != null)
            {
                properties.ContentMD5 = response.Headers[Constants.HeaderConstants.BlobContentMD5Header];
            }
            else
            {
                properties.ContentMD5 = response.Headers[HttpResponseHeader.ContentMd5];
            }

            properties.ContentType = response.Headers[HttpResponseHeader.ContentType];
            properties.CacheControl = response.Headers[HttpResponseHeader.CacheControl];

            string blobEncryption = response.Headers[Constants.HeaderConstants.ServerEncrypted];
            properties.IsServerEncrypted = string.Equals(blobEncryption, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);

            string incrementalCopy = response.Headers[Constants.HeaderConstants.IncrementalCopyHeader];
            properties.IsIncrementalCopy = string.Equals(incrementalCopy, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);

            // Get blob type
            string blobType = response.Headers[Constants.HeaderConstants.BlobType];
            if (!string.IsNullOrEmpty(blobType))
            {
                properties.BlobType = (BlobType)Enum.Parse(typeof(BlobType), blobType, true);
            }

            // Get lease properties
            properties.LeaseStatus = GetLeaseStatus(response);
            properties.LeaseState = GetLeaseState(response);
            properties.LeaseDuration = GetLeaseDuration(response);

            // Get the content length. Prioritize range and x-ms over content length for the special cases.
            string rangeHeader = response.Headers[HttpResponseHeader.ContentRange];
            string contentLengthHeader = response.Headers[Constants.HeaderConstants.ContentLengthHeader];
            string blobContentLengthHeader = response.Headers[Constants.HeaderConstants.BlobContentLengthHeader];
            if (!string.IsNullOrEmpty(rangeHeader))
            {
                properties.Length = long.Parse(rangeHeader.Split('/')[1], CultureInfo.InvariantCulture);
            }
            else if (!string.IsNullOrEmpty(blobContentLengthHeader))
            {
                properties.Length = long.Parse(blobContentLengthHeader, CultureInfo.InvariantCulture);
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

            // Get sequence number
            string sequenceNumber = response.Headers[Constants.HeaderConstants.BlobSequenceNumber];
            if (!string.IsNullOrEmpty(sequenceNumber))
            {
                properties.PageBlobSequenceNumber = long.Parse(sequenceNumber, CultureInfo.InvariantCulture);
            }

            // Get committed block count
            string comittedBlockCount = response.Headers[Constants.HeaderConstants.BlobCommittedBlockCount];
            if (!string.IsNullOrEmpty(comittedBlockCount))
            {
                properties.AppendBlobCommittedBlockCount = int.Parse(comittedBlockCount, CultureInfo.InvariantCulture);
            }
            
            // Get the tier of the blob
            string premiumPageBlobTierInferredString = response.Headers[Constants.HeaderConstants.AccessTierInferredHeader];
            if (!string.IsNullOrEmpty(premiumPageBlobTierInferredString))
            {
                properties.BlobTierInferred = Convert.ToBoolean(premiumPageBlobTierInferredString);
            }
            
            string blobTierString = response.Headers[Constants.HeaderConstants.AccessTierHeader];

            StandardBlobTier? standardBlobTier;
            PremiumPageBlobTier? premiumPageBlobTier;
            BlobHttpResponseParsers.GetBlobTier(properties.BlobType, blobTierString, out standardBlobTier, out premiumPageBlobTier);
            properties.StandardBlobTier = standardBlobTier;
            properties.PremiumPageBlobTier = premiumPageBlobTier;
            
            if ((properties.PremiumPageBlobTier.HasValue || properties.StandardBlobTier.HasValue) && !properties.BlobTierInferred.HasValue)
            {
                properties.BlobTierInferred = false;
            }

            // Get the rehydration status
            string rehydrationStatusString = response.Headers[Constants.HeaderConstants.ArchiveStatusHeader];
            properties.RehydrationStatus = BlobHttpResponseParsers.GetRehydrationStatus(rehydrationStatusString);

            // Get the time the tier of the blob was last modified
            string accessTierChangeTimeString = response.Headers[Constants.HeaderConstants.AccessTierChangeTimeHeader];
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
        public static LeaseStatus GetLeaseStatus(HttpWebResponse response)
        {
            CommonUtility.AssertNotNull("response", response);

            string leaseStatus = response.Headers[Constants.HeaderConstants.LeaseStatus];
            return GetLeaseStatus(leaseStatus);
        }

        /// <summary>
        /// Extracts the lease state from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A <see cref="LeaseState"/> enumeration from the web response.</returns>
        /// <remarks>If the appropriate header is not present, a status of <see cref="LeaseState.Unspecified"/> is returned.</remarks>
        /// <exception cref="System.ArgumentException">The header contains an unrecognized value.</exception>
        public static LeaseState GetLeaseState(HttpWebResponse response)
        {
            CommonUtility.AssertNotNull("response", response);

            string leaseState = response.Headers[Constants.HeaderConstants.LeaseState];
            return GetLeaseState(leaseState);
        }

        /// <summary>
        /// Extracts the lease duration from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A <see cref="LeaseDuration"/> enumeration from the web response.</returns>
        /// <remarks>If the appropriate header is not present, a status of <see cref="LeaseDuration.Unspecified"/> is returned.</remarks>
        /// <exception cref="System.ArgumentException">The header contains an unrecognized value.</exception>
        public static LeaseDuration GetLeaseDuration(HttpWebResponse response)
        {
            CommonUtility.AssertNotNull("response", response);

            string leaseDuration = response.Headers[Constants.HeaderConstants.LeaseDurationHeader];
            return GetLeaseDuration(leaseDuration);
        }

        /// <summary>
        /// Extracts the lease ID header from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The lease ID.</returns>
        public static string GetLeaseId(HttpWebResponse response)
        {
            CommonUtility.AssertNotNull("response", response);

            return response.Headers[Constants.HeaderConstants.LeaseIdHeader];
        }

        /// <summary>
        /// Extracts the remaining lease time from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The remaining lease time, in seconds.</returns>
        public static int? GetRemainingLeaseTime(HttpWebResponse response)
        {
            CommonUtility.AssertNotNull("response", response);

            int remainingLeaseTime;
            if (int.TryParse(response.Headers[Constants.HeaderConstants.LeaseTimeHeader], out remainingLeaseTime))
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

        /// <summary>
        /// Gets the snapshot timestamp from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The snapshot timestamp.</returns>
        public static string GetSnapshotTime(HttpWebResponse response)
        {
            CommonUtility.AssertNotNull("response", response);

            return response.Headers[Constants.HeaderConstants.SnapshotHeader];
        }
    }
}
