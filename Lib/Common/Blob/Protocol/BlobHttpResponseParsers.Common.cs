﻿//-----------------------------------------------------------------------
// <copyright file="BlobHttpResponseParsers.Common.cs" company="Microsoft">
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
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Globalization;
    using System.IO;

#if WINDOWS_RT
    internal
#else
    public
#endif
        static partial class BlobHttpResponseParsers
    {
        /// <summary>
        /// Reads service properties from a stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the service properties.</param>
        /// <returns>The service properties stored in the stream.</returns>
        public static ServiceProperties ReadServiceProperties(Stream inputStream)
        {
            return HttpResponseParsers.ReadServiceProperties(inputStream);
        }

        /// <summary>
        /// Reads service stats from a stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the service stats.</param>
        /// <returns>The service stats stored in the stream.</returns>
        public static ServiceStats ReadServiceStats(Stream inputStream)
        {
            return HttpResponseParsers.ReadServiceStats(inputStream);
        }

        /// <summary>
        /// Gets a <see cref="LeaseStatus"/> from a string.
        /// </summary>
        /// <param name="leaseStatus">The lease status string.</param>
        /// <returns>A <see cref="LeaseStatus"/> enumeration.</returns>
        /// <remarks>If a null or empty string is supplied, a status of <see cref="LeaseStatus.Unspecified"/> is returned.</remarks>
        /// <exception cref="System.ArgumentException">The string contains an unrecognized value.</exception>
        internal static LeaseStatus GetLeaseStatus(string leaseStatus)
        {
            if (!string.IsNullOrEmpty(leaseStatus))
            {
                switch (leaseStatus)
                {
                    case Constants.LockedValue:
                        return LeaseStatus.Locked;

                    case Constants.UnlockedValue:
                        return LeaseStatus.Unlocked;

                    default:
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidLeaseStatus, leaseStatus), "leaseStatus");
                }
            }

            return LeaseStatus.Unspecified;
        }

        /// <summary>
        /// Gets a <see cref="LeaseState"/> from a string.
        /// </summary>
        /// <param name="leaseState">The lease state string.</param>
        /// <returns>A <see cref="LeaseState"/> enumeration.</returns>
        /// <remarks>If a null or empty string is supplied, a status of <see cref="LeaseState.Unspecified"/> is returned.</remarks>
        /// <exception cref="System.ArgumentException">The string contains an unrecognized value.</exception>
        internal static LeaseState GetLeaseState(string leaseState)
        {
            if (!string.IsNullOrEmpty(leaseState))
            {
                switch (leaseState)
                {
                    case Constants.LeaseAvailableValue:
                        return LeaseState.Available;

                    case Constants.LeasedValue:
                        return LeaseState.Leased;

                    case Constants.LeaseExpiredValue:
                        return LeaseState.Expired;

                    case Constants.LeaseBreakingValue:
                        return LeaseState.Breaking;

                    case Constants.LeaseBrokenValue:
                        return LeaseState.Broken;

                    default:
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidLeaseState, leaseState), "leaseState");
                }
            }

            return LeaseState.Unspecified;
        }

        /// <summary>
        /// Gets a <see cref="LeaseDuration"/> from a string.
        /// </summary>
        /// <param name="leaseDuration">The lease duration string.</param>
        /// <returns>A <see cref="LeaseDuration"/> enumeration.</returns>
        /// <remarks>If a null or empty string is supplied, a status of <see cref="LeaseDuration.Unspecified"/> is returned.</remarks>
        /// <exception cref="System.ArgumentException">The string contains an unrecognized value.</exception>
        internal static LeaseDuration GetLeaseDuration(string leaseDuration)
        {
            if (!string.IsNullOrEmpty(leaseDuration))
            {
                switch (leaseDuration)
                {
                    case Constants.LeaseFixedValue:
                        return LeaseDuration.Fixed;

                    case Constants.LeaseInfiniteValue:
                        return LeaseDuration.Infinite;

                    default:
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidLeaseDuration, leaseDuration), "leaseDuration");
                }
            }

            return LeaseDuration.Unspecified;
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
        /// Determines if a blob is listed as server-side encypted.
        /// </summary>
        /// <param name="encryptionHeader">String giving the status of server encryption.</param>
        /// <returns><c>true</c> if blob encrypted or <c>false</c> if not.</returns>
        public static bool GetServerEncrypted(string encryptionHeader)
        {
            return CheckIfTrue(encryptionHeader);
        }

        /// <summary>
        /// Determines if a blob in an incremental copy.
        /// </summary>
        /// <param name="incrementalCopyHeader">String giving the incremental copy status of the blob</param>
        /// <returns><c>true</c> if blob is an incremental copy or <c>false</c> if not.</returns>
        public static bool GetIncrementalCopyStatus(string incrementalCopyHeader)
        {
            return CheckIfTrue(incrementalCopyHeader);
        }


        /// <summary>
        /// Determines if a blob has been deleted.
        /// </summary>
        /// <param name="deletedHeader">String giving the deletion status of the blob</param>
        /// <returns><c>true</c> if blob has been deleted or <c>false</c> if not.</returns>
        public static bool GetDeletionStatus(string deletedHeader)
        {
            return CheckIfTrue(deletedHeader);
        }

        /// <summary>
        /// Determines if the header is equal to the value true.
        /// </summary>
        /// <param name="header">The header to check</param>
        /// <returns><c>true</c> if header equals true or <c>false</c> if not.</returns>
        private static bool CheckIfTrue(string header)
        {
            return string.Equals(header, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines the tier of the blob.
        /// </summary>
        /// <param name="blobType">A <see cref="BlobType" /> indicating the type of blob.</param>
        /// <param name="blobTierString">The blob tier as a string</param>
        /// <param name="standardBlobTier">A nullable <see cref="StandardBlobTier"/>. This value will be populated if the blob type is unspecified or is a block blob.</param>
        /// <param name="premiumPageBlobTier">A nullable <see cref="PremiumPageBlobTier"/>. This value will be populated if the blob type is unspecified or is a page blob.</param>
        internal static void GetBlobTier(BlobType blobType, string blobTierString, out StandardBlobTier? standardBlobTier, out PremiumPageBlobTier? premiumPageBlobTier)
        {
            standardBlobTier = null;
            premiumPageBlobTier = null;

            if (blobType.Equals(BlobType.BlockBlob))
            {
                StandardBlobTier standardBlobTierFromResponse;
                if (Enum.TryParse(blobTierString, true, out standardBlobTierFromResponse))
                {
                    standardBlobTier = standardBlobTierFromResponse;
                }
                else
                {
                    standardBlobTier = StandardBlobTier.Unknown;
                }
            }
            else if (blobType.Equals(BlobType.PageBlob))
            {
                PremiumPageBlobTier pageBlobTierFromResponse;
                if (Enum.TryParse(blobTierString, true, out pageBlobTierFromResponse))
                {
                    premiumPageBlobTier = pageBlobTierFromResponse;
                }
                else
                {
                    premiumPageBlobTier = PremiumPageBlobTier.Unknown;
                }
            }
            else if (blobType.Equals(BlobType.Unspecified))
            {
                StandardBlobTier standardBlobTierFromResponse;
                PremiumPageBlobTier pageBlobTierFromResponse;
                if (Enum.TryParse(blobTierString, true, out standardBlobTierFromResponse))
                {
                    standardBlobTier = standardBlobTierFromResponse;
                }
                else if (Enum.TryParse(blobTierString, true, out pageBlobTierFromResponse))
                {
                    premiumPageBlobTier = pageBlobTierFromResponse;
                }
                else
                {
                    standardBlobTier = StandardBlobTier.Unknown;
                    premiumPageBlobTier = PremiumPageBlobTier.Unknown;
                }
            }
        }

        /// <summary>
        /// Determines the rehydration status of the blob.
        /// </summary>
        /// <param name="rehydrationStatus">The rehydration status as a string.</param>
        /// <returns>A <see cref="RehydrationStatus"/> representing the rehydration status of the blob.</returns>
        internal static RehydrationStatus? GetRehydrationStatus(string rehydrationStatus)
        {
            if (!string.IsNullOrEmpty(rehydrationStatus))
            {
                if (Constants.RehydratePendingToHot.Equals(rehydrationStatus))
                {
                    return RehydrationStatus.PendingToHot;
                }
                else if (Constants.RehydratePendingToCool.Equals(rehydrationStatus))
                {
                    return RehydrationStatus.PendingToCool;
                }

                return RehydrationStatus.Unknown;
            }

            return null;
        }
    }
}
