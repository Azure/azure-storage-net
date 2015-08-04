//-----------------------------------------------------------------------
// <copyright file="BlobErrorCodeStrings.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
    /// <summary>
    /// Provides error code strings that are specific to the Blob service.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
 static class BlobErrorCodeStrings
    {
        /// <summary>
        /// Error code that may be returned when the specified append offset is invalid.
        /// </summary>
        public static readonly string InvalidAppendCondition = "AppendPositionConditionNotMet";

        /// <summary>
        /// Error code that may be returned when the specified maximum blob size is invalid.
        /// </summary>
        public static readonly string InvalidMaxBlobSizeCondition = "MaxBlobSizeConditionNotMet";

        /// <summary>
        /// Error code that may be returned when the specified block or blob is invalid.
        /// </summary>
        public static readonly string InvalidBlobOrBlock = "InvalidBlobOrBlock";

        /// <summary>
        /// Error code that may be returned when a block ID is invalid.
        /// </summary>
        public static readonly string InvalidBlockId = "InvalidBlockId";

        /// <summary>
        /// Error code that may be returned when a block list is invalid.
        /// </summary>
        public static readonly string InvalidBlockList = "InvalidBlockList";

        /// <summary>
        /// The specified container was not found.
        /// </summary>
        public static readonly string ContainerNotFound = "ContainerNotFound";

        /// <summary>
        /// Error code that may be returned when a blob with the specified address cannot be found.
        /// </summary>
        public static readonly string BlobNotFound = "BlobNotFound";

        /// <summary>
        /// The specified container already exists.
        /// </summary>
        public static readonly string ContainerAlreadyExists = "ContainerAlreadyExists";

        /// <summary>
        /// The specified container is disabled.
        /// </summary>
        public static readonly string ContainerDisabled = "ContainerDisabled";

        /// <summary>
        /// The specified container is being deleted.
        /// </summary>
        public static readonly string ContainerBeingDeleted = "ContainerBeingDeleted";

        /// <summary>
        /// Error code that may be returned when a client attempts to create a blob that already exists.
        /// </summary>
        public static readonly string BlobAlreadyExists = "BlobAlreadyExists";

        /// <summary>
        /// Error code that may be returned when there is currently no lease on the blob.
        /// </summary>
        public static readonly string LeaseNotPresentWithBlobOperation = "LeaseNotPresentWithBlobOperation";

        /// <summary>
        /// Error code that may be returned when there is currently no lease on the container.
        /// </summary>
        public static readonly string LeaseNotPresentWithContainerOperation = "LeaseNotPresentWithContainerOperation";

        /// <summary>
        /// Error code that may be returned when a lease ID was specified, but the lease has expired.
        /// </summary>
        public static readonly string LeaseLost = "LeaseLost";

        /// <summary>
        /// Error code that may be returned when the lease ID specified did not match the lease ID for the blob.
        /// </summary>
        public static readonly string LeaseIdMismatchWithBlobOperation = "LeaseIdMismatchWithBlobOperation";

        /// <summary>
        /// Error code that may be returned when the lease ID specified did not match the lease ID for the container.
        /// </summary>
        public static readonly string LeaseIdMismatchWithContainerOperation = "LeaseIdMismatchWithContainerOperation";

        /// <summary>
        /// Error code that may be returned when there is currently a lease on the resource and no lease ID was specified in the request.
        /// </summary>
        public static readonly string LeaseIdMissing = "LeaseIdMissing";

        /// <summary>
        /// Error code that may be returned when there is currently no lease on the resource.
        /// </summary>
        public static readonly string LeaseNotPresentWithLeaseOperation = "LeaseNotPresentWithLeaseOperation";

        /// <summary>
        /// Error code that may be returned when the lease ID specified did not match the lease ID.
        /// </summary>
        public static readonly string LeaseIdMismatchWithLeaseOperation = "LeaseIdMismatchWithLeaseOperation";

        /// <summary>
        /// Error code that may be returned when there is already a lease present.
        /// </summary>
        public static readonly string LeaseAlreadyPresent = "LeaseAlreadyPresent";

        /// <summary>
        /// Error code that may be returned when the lease has already been broken and cannot be broken again.
        /// </summary>
        public static readonly string LeaseAlreadyBroken = "LeaseAlreadyBroken";

        /// <summary>
        /// Error code that may be returned when the lease ID matched, but the lease has been broken explicitly and cannot be renewed.
        /// </summary>
        public static readonly string LeaseIsBrokenAndCannotBeRenewed = "LeaseIsBrokenAndCannotBeRenewed";

        /// <summary>
        /// Error code that may be returned when the lease ID matched, but the lease is breaking and cannot be acquired.
        /// </summary>
        public static readonly string LeaseIsBreakingAndCannotBeAcquired = "LeaseIsBreakingAndCannotBeAcquired";

        /// <summary>
        /// Error code that may be returned when the lease ID matched, but the lease is breaking and cannot be changed.
        /// </summary>
        public static readonly string LeaseIsBreakingAndCannotBeChanged = "LeaseIsBreakingAndCannotBeChanged";

        /// <summary>
        /// Error code that may be returned when the destination of a copy operation has a lease of fixed duration.
        /// </summary>
        public static readonly string InfiniteLeaseDurationRequired = "InfiniteLeaseDurationRequired";

        /// <summary>
        /// Error code that may be returned when the operation is not permitted because the blob has snapshots.
        /// </summary>
        public static readonly string SnapshotsPresent = "SnapshotsPresent";

        /// <summary>
        /// Error code that may be returned when the blob type is invalid for this operation.
        /// </summary>
        public static readonly string InvalidBlobType = "InvalidBlobType";

        /// <summary>
        /// Error code that may be returned when the operation on page blobs uses a version prior to 2009-09-19.
        /// </summary>
        public static readonly string InvalidVersionForPageBlobOperation = "InvalidVersionForPageBlobOperation";

        /// <summary>
        /// Error code that may be returned when the page range specified is invalid.
        /// </summary>
        public static readonly string InvalidPageRange = "InvalidPageRange";

        /// <summary>
        /// Error code that may be returned when the sequence number condition specified was not met.
        /// </summary>
        public static readonly string SequenceNumberConditionNotMet = "SequenceNumberConditionNotMet";

        /// <summary>
        /// Error code that may be returned when the sequence number increment cannot be performed because it would result in overflow of the sequence number.
        /// </summary>
        public static readonly string SequenceNumberIncrementTooLarge = "SequenceNumberIncrementTooLarge";

        /// <summary>
        /// Error code that may be returned when the source condition specified using HTTP conditional header(s) is not met.
        /// </summary>
        public static readonly string SourceConditionNotMet = "SourceConditionNotMet";

        /// <summary>
        /// Error code that may be returned when the target condition specified using HTTP conditional header(s) is not met.
        /// </summary>
        public static readonly string TargetConditionNotMet = "TargetConditionNotMet";

        /// <summary>
        /// Error code that may be returned when the copy source account and destination account are not the same.
        /// </summary>
        public static readonly string CopyAcrossAccountsNotSupported = "CopyAcrossAccountsNotSupported";

        /// <summary>
        /// Error code that may be returned when the source of a copy cannot be accessed.
        /// </summary>
        public static readonly string CannotVerifyCopySource = "CannotVerifyCopySource";

        /// <summary>
        /// Error code that may be returned when an attempt to modify the destination of a pending copy is made.
        /// </summary>
        public static readonly string PendingCopyOperation = "PendingCopyOperation";

        /// <summary>
        /// Error code that may be returned when an Abort Copy operation is called when there is no pending copy.
        /// </summary>
        public static readonly string NoPendingCopyOperation = "NoPendingCopyOperation";

        /// <summary>
        /// Error code that may be returned when the copy ID specified in an Abort Copy operation does not match the current pending copy ID.
        /// </summary>
        public static readonly string CopyIdMismatch = "CopyIdMismatch";
    }
}