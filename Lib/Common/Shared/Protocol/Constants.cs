// -----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// Contains storage constants.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
 static class Constants
    {
        /// <summary>
        /// Constant for the max value of ParallelOperationThreadCount for Block Blobs.
        /// </summary>
        public const int MaxParallelOperationThreadCount = 64;

        /// <summary>
        /// Maximum number of shared access policy identifiers supported by server.
        /// </summary>
        public const int MaxSharedAccessPolicyIdentifiers = 5;

        /// <summary>
        /// Default Write Block Size used by Blob stream.
        /// </summary>
        public const int DefaultWriteBlockSizeBytes = (int)(4 * Constants.MB);

        /// <summary>
        /// Default read buffer size used by the SubStream class for Large Block Blob uploads.
        /// </summary>
        public const int DefaultSubStreamBufferSize = (int)(4 * Constants.MB);

        /// <summary>
        /// Default range size when downloading a blob in parallel.
        /// </summary>
        public const long DefaultParallelDownloadRangeSizeBytes = 16 * Constants.MB;

        /// <summary>
        /// The maximum size of a blob before it must be separated into blocks.
        /// </summary>
        public const long MaxSingleUploadBlobSize = 256 * MB;

        /// <summary>
        /// The maximum size of a single block for Block Blobs.
        /// </summary>
        public const int MaxBlockSize = (int)(100 * Constants.MB);

        /// <summary>
        /// The maximum size of a single block for Append Blobs.
        /// </summary>
        public const int MaxAppendBlockSize = (int)(4 * Constants.MB);

        /// <summary>
        /// The maximum allowed time between write calls to the stream for parallel download streams.
        /// </summary>
        public const int MaxIdleTimeMs = 120000;

        /// <summary>
        /// The maximum size of a range get operation that returns content MD5.
        /// </summary>
        public const int MaxRangeGetContentMD5Size = (int)(4 * Constants.MB);

        /// <summary>
        /// The maximum number of blocks.
        /// </summary>
        public const long MaxBlockNumber = 50000;

        /// <summary>
        /// The maximum size of a blob with blocks.
        /// </summary>
        public const long MaxBlobSize = MaxBlockNumber * MaxBlockSize;

        /// <summary>
        /// The minimum size of a block for the large block upload strategy to be employed.
        /// </summary>
        public const int MinLargeBlockSize = (int)(4 * Constants.MB) + 1;

        /// <summary>
        /// Constant for the max value of MaximumExecutionTime.
        /// </summary>
        public static readonly TimeSpan MaxMaximumExecutionTime = TimeSpan.FromDays(24.0);

        /// <summary>
        /// Default client side timeout for all service clients.
        /// </summary>
        public static readonly TimeSpan DefaultClientSideTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum Retry Policy back-off
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Backoff", Justification = "Reviewed")]
        public static readonly TimeSpan MaximumRetryBackoff = TimeSpan.FromHours(1);

        /// <summary>
        /// Maximum allowed timeout for any request.
        /// </summary>
        public static readonly TimeSpan MaximumAllowedTimeout = TimeSpan.FromSeconds(int.MaxValue);

        /// <summary>
        /// Maximum allowed value for Delete Retention Days.
        /// </summary>
        internal static readonly int MaximumAllowedRetentionDays = 365;

        /// <summary>
        /// Default size of buffer for unknown sized requests.
        /// </summary>
        internal const int DefaultBufferSize = (int)(64 * KB);

        /// <summary>
        /// Common name to be used for all loggers.
        /// </summary>
        internal const string LogSourceName = "Microsoft.WindowsAzure.Storage";

        /// <summary>
        /// The size of a page in a PageBlob.
        /// </summary>
        public const int PageSize = 512;

        /// <summary>
        /// A constant representing a kilo-byte (Non-SI version).
        /// </summary>
        public const long KB = 1024;

        /// <summary>
        /// A constant representing a megabyte (Non-SI version).
        /// </summary>
        public const long MB = 1024 * KB;

        /// <summary>
        /// A constant representing a gigabyte (Non-SI version).
        /// </summary>
        public const long GB = 1024 * MB;

        /// <summary>
        /// XML element for committed blocks.
        /// </summary>
        public const string CommittedBlocksElement = "CommittedBlocks";

        /// <summary>
        /// XML element for uncommitted blocks.
        /// </summary>
        public const string UncommittedBlocksElement = "UncommittedBlocks";

        /// <summary>
        /// XML element for blocks.
        /// </summary>
        public const string BlockElement = "Block";

        /// <summary>
        /// XML element for names.
        /// </summary>
        public const string NameElement = "Name";

        /// <summary>
        /// XML element for sizes.
        /// </summary>
        public const string SizeElement = "Size";

        /// <summary>
        /// XML element for block lists.
        /// </summary>
        public const string BlockListElement = "BlockList";

        /// <summary>
        /// XML element for queue message lists.
        /// </summary>
        public const string MessagesElement = "QueueMessagesList";

        /// <summary>
        /// XML element for queue messages.
        /// </summary>
        public const string MessageElement = "QueueMessage";

        /// <summary>
        /// XML element for message IDs.
        /// </summary>
        public const string MessageIdElement = "MessageId";

        /// <summary>
        /// XML element for insertion times.
        /// </summary>
        public const string InsertionTimeElement = "InsertionTime";

        /// <summary>
        /// XML element for expiration times.
        /// </summary>
        public const string ExpirationTimeElement = "ExpirationTime";

        /// <summary>
        /// XML element for pop receipts.
        /// </summary>
        public const string PopReceiptElement = "PopReceipt";

        /// <summary>
        /// XML element for the time next visible fields.
        /// </summary>
        public const string TimeNextVisibleElement = "TimeNextVisible";

        /// <summary>
        /// XML element for message texts.
        /// </summary>
        public const string MessageTextElement = "MessageText";

        /// <summary>
        /// XML element for dequeue counts.
        /// </summary>
        public const string DequeueCountElement = "DequeueCount";

        /// <summary>
        /// XML element for page ranges.
        /// </summary>
        public const string PageRangeElement = "PageRange";

        /// <summary>
        /// XML element for clear ranges.
        /// </summary>
        public const string ClearRangeElement = "ClearRange";

        /// <summary>
        /// XML element for page list elements.
        /// </summary>
        public const string PageListElement = "PageList";

        /// <summary>
        /// XML element for page range start elements.
        /// </summary>
        public const string StartElement = "Start";

        /// <summary>
        /// XML element for page range end elements.
        /// </summary>
        public const string EndElement = "End";

        /// <summary>
        /// XML element for delimiters.
        /// </summary>
        public const string DelimiterElement = "Delimiter";

        /// <summary>
        /// XML element for blob prefixes.
        /// </summary>
        public const string BlobPrefixElement = "BlobPrefix";

        /// <summary>
        /// XML element for content type fields.
        /// </summary>
        public const string CacheControlElement = "Cache-Control";

        /// <summary>
        /// XML element for content type fields.
        /// </summary>
        public const string ContentTypeElement = "Content-Type";

        /// <summary>
        /// XML element for content encoding fields.
        /// </summary>
        public const string ContentEncodingElement = "Content-Encoding";

        /// <summary>
        /// XML element for content language fields.
        /// </summary>
        public const string ContentLanguageElement = "Content-Language";

        /// <summary>
        /// XML element for content length fields.
        /// </summary>
        public const string ContentLengthElement = "Content-Length";

        /// <summary>
        /// XML element for content MD5 fields.
        /// </summary>
        public const string ContentMD5Element = "Content-MD5";

        /// <summary>
        /// XML element for enumeration results.
        /// </summary>
        public const string EnumerationResultsElement = "EnumerationResults";

        /// <summary>
        /// XML element for service endpoint.
        /// </summary>
        public const string ServiceEndpointElement = "ServiceEndpoint";

        /// <summary>
        /// XML element for container name.
        /// </summary>
        public const string ContainerNameElement = "ContainerName";

        /// <summary>
        /// XML element for share name.
        /// </summary>
        public const string ShareNameElement = "ShareName";

        /// <summary>
        /// XML element for directory path.
        /// </summary>
        public const string DirectoryPathElement = "DirectoryPath";

        /// <summary>
        /// XML element for blobs.
        /// </summary>
        public const string BlobsElement = "Blobs";

        /// <summary>
        /// XML element for prefixes.
        /// </summary>
        public const string PrefixElement = "Prefix";

        /// <summary>
        /// XML element for maximum results.
        /// </summary>
        public const string MaxResultsElement = "MaxResults";

        /// <summary>
        /// XML element for markers.
        /// </summary>
        public const string MarkerElement = "Marker";

        /// <summary>
        /// XML element for the next marker.
        /// </summary>
        public const string NextMarkerElement = "NextMarker";

        /// <summary>
        /// XML element for the ETag.
        /// </summary>
        public const string EtagElement = "Etag";

        /// <summary>
        /// XML element for the creation date.
        /// </summary>
        public const string CreationTimeElement = "Creation-Time";

        /// <summary>
        /// XML element for the last modified date.
        /// </summary>
        public const string LastModifiedElement = "Last-Modified";

        /// <summary>
        /// XML element for the server encryption status.
        /// </summary>
        public const string ServerEncryptionElement = "ServerEncrypted";

        /// <summary>
        /// XML element for the Url.
        /// </summary>
        public const string UrlElement = "Url";

        /// <summary>
        /// XML element for blobs.
        /// </summary>
        public const string BlobElement = "Blob";

        /// <summary>
        /// XML element for copy ID.
        /// </summary>
        public const string CopyIdElement = "CopyId";

        /// <summary>
        /// XML element for copy status.
        /// </summary>
        public const string CopyStatusElement = "CopyStatus";

        /// <summary>
        /// XML element for copy source.
        /// </summary>
        public const string CopySourceElement = "CopySource";

        /// <summary>
        /// XML element for copy progress.
        /// </summary>
        public const string CopyProgressElement = "CopyProgress";

        /// <summary>
        /// XML element for copy completion time.
        /// </summary>
        public const string CopyCompletionTimeElement = "CopyCompletionTime";

        /// <summary>
        /// XML element for copy status description.
        /// </summary>
        public const string CopyStatusDescriptionElement = "CopyStatusDescription";

        /// <summary>
        /// XML element for incremental copy.
        /// </summary>
        public const string IncrementalCopy = "IncrementalCopy";

        /// <summary>
        /// XML element for destination snapshot time.
        /// </summary>
        public const string CopyDestinationSnapshotElement = "CopyDestinationSnapshot";

        /// <summary>
        /// XML element for deleted flag indicating the retention policy on the blob.
        /// </summary>
        public const string DeletedElement = "Deleted";

        /// <summary>
        /// XML element for the time the retained blob was deleted.
        /// </summary>
        public const string DeletedTimeElement = "DeletedTime";

        /// <summary>
        /// XML element for the remaining days before the retained blob will be permenantly deleted.
        /// </summary>
        public const string RemainingRetentionDaysElement = "RemainingRetentionDays";

        /// <summary>
        /// Constant signaling a page blob.
        /// </summary>
        public const string PageBlobValue = "PageBlob";

        /// <summary>
        /// Constant signaling a block blob.
        /// </summary>
        public const string BlockBlobValue = "BlockBlob";

        /// <summary>
        /// Constant signaling an append blob.
        /// </summary>
        public const string AppendBlobValue = "AppendBlob";

        /// <summary>
        /// Constant signaling the blob is locked.
        /// </summary>
        public const string LockedValue = "locked";

        /// <summary>
        /// Constant signaling the blob is unlocked.
        /// </summary>
        public const string UnlockedValue = "unlocked";

        /// <summary>
        /// Constant signaling the resource is available for leasing.
        /// </summary>
        public const string LeaseAvailableValue = "available";

        /// <summary>
        /// Constant signaling the resource is leased.
        /// </summary>
        public const string LeasedValue = "leased";

        /// <summary>
        /// Constant signaling the resource's lease has expired.
        /// </summary>
        public const string LeaseExpiredValue = "expired";

        /// <summary>
        /// Constant signaling the resource's lease is breaking.
        /// </summary>
        public const string LeaseBreakingValue = "breaking";

        /// <summary>
        /// Constant signaling the resource's lease is broken.
        /// </summary>
        public const string LeaseBrokenValue = "broken";

        /// <summary>
        /// Constant signaling the resource's lease is infinite.
        /// </summary>
        public const string LeaseInfiniteValue = "infinite";

        /// <summary>
        /// Constant signaling the resource's lease is fixed (finite).
        /// </summary>
        public const string LeaseFixedValue = "fixed";
        
        /// <summary>
        /// Constant for the minimum period of time that a lease can be broken in. 
        /// </summary>
        public const int MinimumBreakLeasePeriod = 0;

        /// <summary>
        /// Constant for the maximum period of time that a lease can be broken in.
        /// </summary>
        public const int MaximumBreakLeasePeriod = 60;

        /// <summary>
        /// Constant for the minimum duration of a lease.
        /// </summary>
        public const int MinimumLeaseDuration = 15;

        /// <summary>
        /// Constant for the maximum non-infinite duration of a lease.
        /// </summary>
        public const int MaximumLeaseDuration = 60;

        /// <summary>
        /// Constant for a pending copy.
        /// </summary>
        public const string CopyPendingValue = "pending";

        /// <summary>
        /// Constant for a successful copy.
        /// </summary>
        public const string CopySuccessValue = "success";

        /// <summary>
        /// Constant for an aborted copy.
        /// </summary>
        public const string CopyAbortedValue = "aborted";

        /// <summary>
        /// Constant for a failed copy.
        /// </summary>
        public const string CopyFailedValue = "failed";

        /// <summary>
        /// Constant for rehydrating an archived blob to hot storage.
        /// </summary>
        public const string RehydratePendingToHot = "rehydrate-pending-to-hot";

        /// <summary>
        /// Constant for rehydrating an archived blob to cool storage.
        /// </summary>
        public const string RehydratePendingToCool = "rehydrate-pending-to-cool";

        /// <summary>
        /// Constant for unavailable geo-replication status.
        /// </summary>
        public const string GeoUnavailableValue = "unavailable";

        /// <summary>
        /// Constant for live geo-replication status.
        /// </summary>
        public const string GeoLiveValue = "live";

        /// <summary>
        /// Constant for bootstrap geo-replication status.
        /// </summary>
        public const string GeoBootstrapValue = "bootstrap";

        /// <summary>
        /// Constant for the blob tier.
        /// </summary>
        public const string AccessTierElement = "AccessTier";

        /// <summary>
        /// Constant for the access tier being inferred.
        /// </summary>
        public const string AccessTierInferred = "AccessTierInferred";

        /// <summary>
        /// Constant for the access tier change time.
        /// </summary>
        public const string AccessTierChangeTimeElement = "AccessTierChangeTime";

        /// <summary>
        /// Constant for the archive status.
        /// </summary>
        public const string ArchiveStatusElement = "ArchiveStatus";

        /// <summary>
        /// XML element for blob types.
        /// </summary>
        public const string BlobTypeElement = "BlobType";

        /// <summary>
        /// XML element for immutability policy.
        /// </summary>
        public const string HasImmutabilityPolicyElement = "HasImmutabilityPolicy";

        /// <summary>
        /// XML element for legal hold.
        /// </summary>
        public const string HasLegalHoldElement = "HasLegalHold";

        /// <summary>
        /// XML element for the lease status.
        /// </summary>
        public const string LeaseStatusElement = "LeaseStatus";

        /// <summary>
        /// XML element for the lease status.
        /// </summary>
        public const string LeaseStateElement = "LeaseState";

        /// <summary>
        /// XML element for the lease status.
        /// </summary>
        public const string LeaseDurationElement = "LeaseDuration";

        /// <summary>
        /// XML element for the public access value.
        /// </summary>
        public const string PublicAccessElement = "PublicAccess";

        /// <summary>
        /// XML element for snapshots.
        /// </summary>
        public const string SnapshotElement = "Snapshot";

        /// <summary>
        /// XML element for containers.
        /// </summary>
        public const string ContainersElement = "Containers";

        /// <summary>
        /// XML element for a container.
        /// </summary>
        public const string ContainerElement = "Container";

        /// <summary>
        /// XML element for shares.
        /// </summary>
        public const string SharesElement = "Shares";

        /// <summary>
        /// XML element for a share.
        /// </summary>
        public const string ShareElement = "Share";

        /// <summary>
        /// XML element for Share Quota.
        /// </summary>
        public const string QuotaElement = "Quota";

        /// <summary>
        /// XML element for file ranges.
        /// </summary>
        public const string FileRangeElement = "Range";

        /// <summary>
        /// XML element for file list elements.
        /// </summary>
        public const string FileRangeListElement = "Ranges";

        /// <summary>
        /// XML element for files.
        /// </summary>
        public const string EntriesElement = "Entries";

        /// <summary>
        /// XML element for files.
        /// </summary>
        public const string FileElement = "File";

        /// <summary>
        /// XML element for directory.
        /// </summary>
        public const string FileDirectoryElement = "Directory";

        /// <summary>
        /// XML element for queues.
        /// </summary>
        public const string QueuesElement = "Queues";

        /// <summary>
        /// Version 2 of the XML element for the queue name.
        /// </summary>
        public const string QueueNameElement = "Name";

        /// <summary>
        /// XML element for the queue.
        /// </summary>
        public const string QueueElement = "Queue";

        /// <summary>
        /// XML element for properties.
        /// </summary>
        public const string PropertiesElement = "Properties";

        /// <summary>
        /// XML element for the metadata.
        /// </summary>
        public const string MetadataElement = "Metadata";

        /// <summary>
        /// XML element for an invalid metadata name.
        /// </summary>
        public const string InvalidMetadataName = "x-ms-invalid-name";

        /// <summary>
        /// XML element for maximum results.
        /// </summary>
        public const string MaxResults = "MaxResults";

        /// <summary>
        /// XML element for committed blocks.
        /// </summary>
        public const string CommittedElement = "Committed";

        /// <summary>
        /// XML element for uncommitted blocks.
        /// </summary>
        public const string UncommittedElement = "Uncommitted";

        /// <summary>
        /// XML element for the latest.
        /// </summary>
        public const string LatestElement = "Latest";

        /// <summary>
        /// XML element for signed identifiers.
        /// </summary>
        public const string SignedIdentifiers = "SignedIdentifiers";

        /// <summary>
        /// XML element for a signed identifier.
        /// </summary>
        public const string SignedIdentifier = "SignedIdentifier";

        /// <summary>
        /// XML element for access policies.
        /// </summary>
        public const string AccessPolicy = "AccessPolicy";

        /// <summary>
        /// XML attribute for IDs.
        /// </summary>
        public const string Id = "Id";

        /// <summary>
        /// XML element for the start time of an access policy.
        /// </summary>
        public const string Start = "Start";

        /// <summary>
        /// XML element for the end of an access policy.
        /// </summary>
        public const string Expiry = "Expiry";

        /// <summary>
        /// XML element for the permissions of an access policy.
        /// </summary>
        public const string Permission = "Permission";

        /// <summary>
        /// The URI path component to access the messages in a queue.
        /// </summary>
        public const string Messages = "messages";

        /// <summary>
        /// XML element for exception details.
        /// </summary>
        internal const string ErrorException = "exceptiondetails";

        /// <summary>
        /// XML root element for errors.
        /// </summary>
        public const string ErrorRootElement = "Error";

        /// <summary>
        /// XML element for error codes.
        /// </summary>
        public const string ErrorCode = "Code";

        /// <summary>
        /// XML element for error codes returned by the preview tenants.
        /// </summary>
        internal const string ErrorCodePreview = "code";

        /// <summary>
        /// XML element for error messages.
        /// </summary>
        public const string ErrorMessage = "Message";

        /// <summary>
        /// XML element for error messages.
        /// </summary>
        internal const string ErrorMessagePreview = "message";

        /// <summary>
        /// XML element for exception messages.
        /// </summary>
        public const string ErrorExceptionMessage = "ExceptionMessage";

        /// <summary>
        /// XML element for stack traces.
        /// </summary>
        public const string ErrorExceptionStackTrace = "StackTrace";

        /// <summary>
        /// Namespace of the entity container.
        /// </summary>
        internal const string EdmEntityTypeNamespaceName = "AzureTableStorage";

        /// <summary>
        /// Name of the entity container.
        /// </summary>
        internal const string EdmEntityTypeName = "DefaultContainer";

        /// <summary>
        /// Name of the entity set.
        /// </summary>
        internal const string EntitySetName = "Tables";

        /// <summary>
        /// Namespace name for primitive types.
        /// </summary>
        internal const string Edm = "Edm.";

        /// <summary>
        /// Default namespace name for Tables. 
        /// </summary>
        internal const string DefaultNamespaceName = "account";

        /// <summary>
        /// Default name for Tables. 
        /// </summary>
        internal const string DefaultTableName = "TableName";

        /// <summary>
        /// Header value to set Accept to XML.
        /// </summary>
        internal const string XMLAcceptHeaderValue = "application/xml";

        /// <summary>
        /// Header value to set Accept to JsonLight.
        /// </summary>
        internal const string JsonLightAcceptHeaderValue = "application/json;odata=minimalmetadata";

        /// <summary>
        /// Header value to set Accept to JsonFullMetadata.
        /// </summary>
        internal const string JsonFullMetadataAcceptHeaderValue = "application/json;odata=fullmetadata";

        /// <summary>
        /// Header value to set Accept to JsonNoMetadata.
        /// </summary>
        internal const string JsonNoMetadataAcceptHeaderValue = "application/json;odata=nometadata";

        /// <summary>
        /// Header value argument to set JSON no metadata.
        /// </summary>
        internal const string NoMetadata = "odata=nometadata";

        /// <summary>
        /// Header value to set Content-Type to JSON.
        /// </summary>
        internal const string JsonContentTypeHeaderValue = "application/json";

        /// <summary>
        /// The prefix used in all ETags.
        /// </summary>
        internal const string ETagPrefix = "\"datetime'";

        internal const string OdataTypeString = "@odata.type";

        internal const string EdmBinary = @"Edm.Binary";
        internal const string EdmBoolean = @"Emd.Boolean";
        internal const string EdmDateTime = @"Edm.DateTime";
        internal const string EdmDouble = @"Edm.Double";
        internal const string EdmGuid = @"Edm.Guid";
        internal const string EdmInt32 = @"Edm.Int32";
        internal const string EdmInt64 = @"Edm.Int64";
        internal const string EdmString = @"Edm.String";

        internal const string BatchBoundaryMarker = @"multipart/mixed; boundary=batch_";
        internal const string ChangesetBoundaryMarker = @"Content-Type: multipart/mixed; boundary=changeset_";

        internal const string BatchSeparator = @"--batch_";
        internal const string ChangesetSeparator = @"--changeset_";

        internal const string ContentTypeApplicationHttp = @"Content-Type: application/http";
        internal const string ContentTransferEncodingBinary = @"Content-Transfer-Encoding: binary";

        internal const string ContentTypeApplicationJson = HeaderConstants.PayloadContentTypeHeader + ": " + JsonContentTypeHeaderValue;

        internal const string HTTP1_1 = "HTTP/1.1";

        /// <summary>
        /// Constants for HTTP headers.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Reviewed.")]
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        public static class HeaderConstants
        {
            static HeaderConstants()
            {
#if WINDOWS_PHONE && WINDOWS_DESKTOP
                UserAgentComment = string.Format(CultureInfo.InvariantCulture, "(.NET CLR {0}; Windows Phone {1})", Environment.Version, Environment.OSVersion.Version);
#elif WINDOWS_PHONE && WINDOWS_RT
                UserAgentComment = "(Windows Runtime Phone)";
#elif WINDOWS_RT
                UserAgentComment = "(Windows Runtime)";
#elif NETCORE
                UserAgentComment = "(.NET Core)";
#else
                UserAgentComment = string.Format(CultureInfo.InvariantCulture, "(.NET CLR {0}; {1} {2})", Environment.Version, Environment.OSVersion.Platform, Environment.OSVersion.Version);
#endif

                UserAgent = UserAgentProductName + "/" + UserAgentProductVersion + " " + UserAgentComment;
            }

            /// <summary>
            /// Specifies the value to use for UserAgent header.
            /// </summary>
            public static readonly string UserAgent;

            /// <summary>
            /// Specifies the comment to use for UserAgent header.
            /// </summary>
            public static readonly string UserAgentComment;

            /// <summary>
            /// Specifies the value to use for UserAgent header.
            /// </summary>
            public const string UserAgentProductName = "Azure-Storage";

            /// <summary>
            /// Specifies the value to use for UserAgent header.
            /// </summary>
            public const string UserAgentProductVersion = "9.3.1";

            /// <summary>
            /// Master Microsoft Azure Storage header prefix.
            /// </summary>
            public const string PrefixForStorageHeader = "x-ms-";

            /// <summary>
            /// True Header.
            /// </summary>
            public const string TrueHeader = "true";

            /// <summary>
            /// False Header.
            /// </summary>
            public const string FalseHeader = "false";

            /// <summary>
            /// Header prefix for properties.
            /// </summary>
            public const string PrefixForStorageProperties = "x-ms-prop-";

            /// <summary>
            /// Header prefix for metadata.
            /// </summary>
            public const string PrefixForStorageMetadata = "x-ms-meta-";

            /// <summary>
            /// Header that specifies content disposition.
            /// </summary>
            public const string ContentDispositionResponseHeader = "Content-Disposition";

            /// <summary>
            /// Header that specifies content length.
            /// </summary>
            public const string ContentLengthHeader = "Content-Length";

            /// <summary>
            /// Header that specifies content language.
            /// </summary>
            public const string ContentLanguageHeader = "Content-Language";

            /// <summary>
            /// Header that specifies the creation time value for the resource.
            /// </summary>
            public const string CreationTimeHeader = PrefixForStorageHeader + "creation-time";

            /// <summary>
            /// Header that specifies the ETag value for the resource.
            /// </summary>
            public const string EtagHeader = "ETag";

            /// <summary>
            /// Header that specifies the immutability policy value for the resource.
            /// </summary>
            public const string HasImmutabilityPolicyHeader = PrefixForStorageHeader + "has-immutability-policy";

            /// <summary>
            /// Header that specifies the legal hold value for the resource.
            /// </summary>
            public const string HasLegalHoldHeader = PrefixForStorageHeader + "has-legal-hold";

            /// <summary>
            /// Header that specifies if a resourse is fully encrypted server-side.
            /// </summary>
            public const string ServerEncrypted = PrefixForStorageHeader + "server-encrypted";

            /// <summary>
            /// Header that acknowledges the data used for write operation is encrypted server-side.
            /// </summary>
            public const string ServerRequestEncrypted = PrefixForStorageHeader + "request-server-encrypted";

            /// <summary>
            /// Header for data ranges.
            /// </summary>
            public const string RangeHeader = PrefixForStorageHeader + "range";

            /// <summary>
            /// Header for range content MD5.
            /// </summary>
            public const string RangeContentMD5Header = PrefixForStorageHeader + "range-get-content-md5";

            /// <summary>
            /// Header for storage version.
            /// </summary>
            public const string StorageVersionHeader = PrefixForStorageHeader + "version";

            /// <summary>
            /// Header for copy source.
            /// </summary>
            public const string CopySourceHeader = PrefixForStorageHeader + "copy-source";

            /// <summary>
            /// Header for copy sync.
            /// </summary>
            public const string RequiresSyncHeader = PrefixForStorageHeader + "requires-sync";

            /// <summary>
            /// Header for source ranges.
            /// </summary>
            public const string SourceRangeHeader = PrefixForStorageHeader + "source-range";

            /// <summary>
            /// Header for the If-Match condition.
            /// </summary>
            public const string SourceIfMatchHeader = PrefixForStorageHeader + "source-if-match";

            /// <summary>
            /// Header for the If-Modified-Since condition.
            /// </summary>
            public const string SourceIfModifiedSinceHeader = PrefixForStorageHeader + "source-if-modified-since";

            /// <summary>
            /// Header for the If-None-Match condition.
            /// </summary>
            public const string SourceIfNoneMatchHeader = PrefixForStorageHeader + "source-if-none-match";

            /// <summary>
            /// Header for the If-Unmodified-Since condition.
            /// </summary>
            public const string SourceIfUnmodifiedSinceHeader = PrefixForStorageHeader + "source-if-unmodified-since";

            /// <summary>
            /// Header for the file type.
            /// </summary>
            public const string FileType = PrefixForStorageHeader + "type";

            /// <summary>
            /// Header that specifies file caching control.
            /// </summary>
            public const string FileCacheControlHeader = PrefixForStorageHeader + "cache-control";

            /// <summary>
            /// Request header that specifies the file content disposition.
            /// </summary>
            public const string FileContentDispositionRequestHeader = PrefixForStorageHeader + "content-disposition";

            /// <summary>
            /// Header that specifies file content encoding.
            /// </summary>
            public const string FileContentEncodingHeader = PrefixForStorageHeader + "content-encoding";

            /// <summary>
            /// Header that specifies file content language.
            /// </summary>
            public const string FileContentLanguageHeader = PrefixForStorageHeader + "content-language";

            /// <summary>
            /// Header that specifies file content MD5.
            /// </summary>
            public const string FileContentMD5Header = PrefixForStorageHeader + "content-md5";

            /// <summary>
            /// Header that specifies source content MD5.
            /// </summary>
            public const string SourceContentMD5Header = PrefixForStorageHeader + "source-content-md5";

            /// <summary>
            /// Header that specifies file content type.
            /// </summary>
            public const string FileContentTypeHeader = PrefixForStorageHeader + "content-type";

            /// <summary>
            /// Header that specifies file content length.
            /// </summary>
            public const string FileContentLengthHeader = PrefixForStorageHeader + "content-length";

            /// <summary>
            /// Header that specifies the file write mode.
            /// </summary>
            public const string FileRangeWrite = PrefixForStorageHeader + "write";

            /// <summary>
            /// Header for the blob type.
            /// </summary>
            public const string BlobType = PrefixForStorageHeader + "blob-type";

            /// <summary>
            /// Header for snapshots.
            /// </summary>
            public const string SnapshotHeader = PrefixForStorageHeader + "snapshot";

            /// <summary>
            /// Header to delete snapshots.
            /// </summary>
            public const string DeleteSnapshotHeader = PrefixForStorageHeader + "delete-snapshots";

            /// <summary>
            /// Header for the blob tier.
            /// </summary>
            public const string AccessTierHeader = PrefixForStorageHeader + "access-tier";

            /// <summary>
            /// Header for the archive status.
            /// </summary>
            public const string ArchiveStatusHeader = PrefixForStorageHeader + "archive-status";

            /// <summary>
            /// Header for the blob tier inferred.
            /// </summary>
            public const string AccessTierInferredHeader = PrefixForStorageHeader + "access-tier-inferred";

            /// <summary>
            /// Header for the last time the tier was modified.
            /// </summary>
            public const string AccessTierChangeTimeHeader = PrefixForStorageHeader + "access-tier-change-time";

            /// <summary>
            /// Header that specifies blob caching control.
            /// </summary>
            public const string BlobCacheControlHeader = PrefixForStorageHeader + "blob-cache-control";

            /// <summary>
            /// Request header that specifies the blob content disposition.
            /// </summary>
            public const string BlobContentDispositionRequestHeader = PrefixForStorageHeader + "blob-content-disposition";

            /// <summary>
            /// Header that specifies blob content encoding.
            /// </summary>
            public const string BlobContentEncodingHeader = PrefixForStorageHeader + "blob-content-encoding";

            /// <summary>
            /// Header that specifies blob content language.
            /// </summary>
            public const string BlobContentLanguageHeader = PrefixForStorageHeader + "blob-content-language";

            /// <summary>
            /// Header that specifies blob content MD5.
            /// </summary>
            public const string BlobContentMD5Header = PrefixForStorageHeader + "blob-content-md5";

            /// <summary>
            /// Header that specifies blob content type.
            /// </summary>
            public const string BlobContentTypeHeader = PrefixForStorageHeader + "blob-content-type";

            /// <summary>
            /// Header that specifies blob content length.
            /// </summary>
            public const string BlobContentLengthHeader = PrefixForStorageHeader + "blob-content-length";

            /// <summary>
            /// Header that specifies blob sequence number.
            /// </summary>
            public const string BlobSequenceNumber = PrefixForStorageHeader + "blob-sequence-number";

            /// <summary>
            /// Header that specifies sequence number action.
            /// </summary>
            public const string SequenceNumberAction = PrefixForStorageHeader + "sequence-number-action";

            /// <summary>
            /// Header that specifies committed block count.
            /// </summary>
            public const string BlobCommittedBlockCount = PrefixForStorageHeader + "blob-committed-block-count";

            /// <summary>
            /// Header that specifies the blob append offset.
            /// </summary>
            public const string BlobAppendOffset = PrefixForStorageHeader + "blob-append-offset";

            /// <summary>
            /// Header for the If-Sequence-Number-LE condition.
            /// </summary>
            public const string IfSequenceNumberLEHeader = PrefixForStorageHeader + "if-sequence-number-le";

            /// <summary>
            /// Header for the If-Sequence-Number-LT condition.
            /// </summary>
            public const string IfSequenceNumberLTHeader = PrefixForStorageHeader + "if-sequence-number-lt";

            /// <summary>
            /// Header for the If-Sequence-Number-EQ condition.
            /// </summary>
            public const string IfSequenceNumberEqHeader = PrefixForStorageHeader + "if-sequence-number-eq";

            /// <summary>
            /// Header for the blob-condition-maxsize condition.
            /// </summary>
            public const string IfMaxSizeLessThanOrEqualHeader = PrefixForStorageHeader + "blob-condition-maxsize";

            /// <summary>
            /// Header for the blob-condition-appendpos condition.
            /// </summary>
            public const string IfAppendPositionEqualHeader = PrefixForStorageHeader + "blob-condition-appendpos";

            /// <summary>
            /// Header that specifies lease ID.
            /// </summary>
            public const string LeaseIdHeader = PrefixForStorageHeader + "lease-id";

            /// <summary>
            /// Header that specifies lease status.
            /// </summary>
            public const string LeaseStatus = PrefixForStorageHeader + "lease-status";

            /// <summary>
            /// Header that specifies lease status.
            /// </summary>
            public const string LeaseState = PrefixForStorageHeader + "lease-state";

            /// <summary>
            /// Header that specifies page write mode.
            /// </summary>
            public const string PageWrite = PrefixForStorageHeader + "page-write";

            /// <summary>
            /// Header that specifies approximate message count of a queue.
            /// </summary>
            public const string ApproximateMessagesCount = PrefixForStorageHeader + "approximate-messages-count";

            /// <summary>
            /// Header that specifies the date.
            /// </summary>
            public const string Date = PrefixForStorageHeader + "date";

            /// <summary>
            /// Header indicating the request ID.
            /// </summary>
            public const string RequestIdHeader = PrefixForStorageHeader + "request-id";

            /// <summary>
            /// Header indicating the client request ID.
            /// </summary>
            public const string ClientRequestIdHeader = PrefixForStorageHeader + "client-request-id";

            /// <summary>
            /// Header that specifies public access to blobs.
            /// </summary>
            public const string BlobPublicAccess = PrefixForStorageHeader + "blob-public-access";

            /// <summary>
            /// Format string for specifying ranges.
            /// </summary>
            public const string RangeHeaderFormat = "bytes={0}-{1}";

            /// <summary>
            /// Current storage version header value.
            /// Every time this version changes, assembly version needs to be updated as well.
            /// </summary>
            public const string TargetStorageVersion = "2018-03-28";

            /// <summary>
            /// Specifies the file type.
            /// </summary>
            public const string File = "File";

            /// <summary>
            /// Specifies the page blob type.
            /// </summary>
            public const string PageBlob = "PageBlob";

            /// <summary>
            /// Specifies the block blob type.
            /// </summary>
            public const string BlockBlob = "BlockBlob";

            /// <summary>
            /// Specifies the append blob type.
            /// </summary>
            public const string AppendBlob = "AppendBlob";

            /// <summary>
            /// Specifies only snapshots are to be included.
            /// </summary>
            public const string SnapshotsOnlyValue = "only";

            /// <summary>
            /// Specifies snapshots are to be included.
            /// </summary>
            public const string IncludeSnapshotsValue = "include";

            /// <summary>
            /// Header that specifies the pop receipt for a message.
            /// </summary>
            public const string PopReceipt = PrefixForStorageHeader + "popreceipt";

            /// <summary>
            /// Header that specifies the next visible time for a message.
            /// </summary>
            public const string NextVisibleTime = PrefixForStorageHeader + "time-next-visible";

            /// <summary>
            /// Header that specifies whether to peek-only.
            /// </summary>
            public const string PeekOnly = "peekonly";

            /// <summary>
            /// Header that specifies whether data in the container may be accessed publicly and what level of access is to be allowed.
            /// </summary>
            public const string ContainerPublicAccessType = PrefixForStorageHeader + "blob-public-access";

            /// <summary>
            /// Header that specifies the lease action to perform.
            /// </summary>
            public const string LeaseActionHeader = PrefixForStorageHeader + "lease-action";

            /// <summary>
            /// Header that specifies the proposed lease ID for a leasing operation.
            /// </summary>
            public const string ProposedLeaseIdHeader = PrefixForStorageHeader + "proposed-lease-id";

            /// <summary>
            /// Header that specifies the duration of a lease.
            /// </summary>
            public const string LeaseDurationHeader = PrefixForStorageHeader + "lease-duration";

            /// <summary>
            /// Header that specifies the break period of a lease.
            /// </summary>
            public const string LeaseBreakPeriodHeader = PrefixForStorageHeader + "lease-break-period";

            /// <summary>
            /// Header that specifies the remaining lease time.
            /// </summary>
            public const string LeaseTimeHeader = PrefixForStorageHeader + "lease-time";

            /// <summary>
            /// Header that specifies the key name for explicit keys.
            /// </summary>
            public const string KeyNameHeader = PrefixForStorageHeader + "key-name";

            /// <summary>
            /// Header that specifies the copy ID.
            /// </summary>
            public const string CopyIdHeader = PrefixForStorageHeader + "copy-id";

            /// <summary>
            /// Header that specifies the conclusion time of the last attempted blob copy operation 
            /// where this blob was the destination blob.
            /// </summary>
            public const string CopyCompletionTimeHeader = PrefixForStorageHeader + "copy-completion-time";

            /// <summary>
            /// Header that specifies the copy status.
            /// </summary>
            public const string CopyStatusHeader = PrefixForStorageHeader + "copy-status";

            /// <summary>
            /// Header that specifies the copy progress.
            /// </summary>
            public const string CopyProgressHeader = PrefixForStorageHeader + "copy-progress";

            /// <summary>
            /// Header that specifies a copy error message.
            /// </summary>
            public const string CopyDescriptionHeader = PrefixForStorageHeader + "copy-status-description";

            /// <summary>
            /// Header that specifies the copy action.
            /// </summary>
            public const string CopyActionHeader = PrefixForStorageHeader + "copy-action";

            /// <summary>
            /// Header that specifies the copy type.
            /// </summary>
            public const string CopyTypeHeader = PrefixForStorageHeader + "copy-type";

            /// <summary>
            /// The value of the copy action header that signifies an abort operation.
            /// </summary>
            public const string CopyActionAbort = "abort";

            /// <summary>
            /// Header that specifies an incremental copy.
            /// </summary>
            public const string IncrementalCopyHeader = PrefixForStorageHeader + "incremental-copy";

            /// <summary>
            /// Header that specifies the snapshot time of the last successful incremental copy snapshot.
            /// </summary>
            public const string CopyDestinationSnapshotHeader = PrefixForStorageHeader + "copy-destination-snapshot";

            /// <summary>
            /// Header that specifies the share size, in gigabytes.
            /// </summary>
            public const string ShareSize = PrefixForStorageHeader + "share-size";

            /// <summary>
            /// Header that specifies the share quota, in gigabytes.
            /// </summary>
            public const string ShareQuota = PrefixForStorageHeader + "share-quota";

            /// <summary>
            /// The name of the SKU name header element.
            /// </summary>
            internal const string SkuNameName = PrefixForStorageHeader + "sku-name";

            /// <summary>
            /// The name of the account kind header element.
            /// </summary>
            internal const string AccountKindName = PrefixForStorageHeader + "account-kind";

            /// <summary>
            /// Header that specifies the Accept type for the response payload.
            /// </summary>
            internal const string PayloadAcceptHeader = "Accept";

            /// <summary>
            /// Header that specifies the Content type for the request payload.
            /// </summary>
            internal const string PayloadContentTypeHeader = "Content-Type";

            /// <summary>
            /// OData Related
            /// </summary>
            internal const string AcceptCharset = "Accept-Charset";

            internal const string AcceptCharsetValue = "UTF-8";

            internal const string MaxDataServiceVersion = "MaxDataServiceVersion";
            internal const string MaxDataServiceVersionValue = "3.0;NetFx";

            internal const string DataServiceVersion = "DataServiceVersion";
            internal const string DataServiceVersionValue = "3.0;";

            internal const string PostTunnelling = "X-HTTP-Method";

            internal const string IfMatch = "If-Match";

            internal const string Prefer = "Prefer";

            internal const string PreferReturnContent = "return-content";
            internal const string PreferReturnNoContent = "return-no-content";

            /// <summary>
            /// Header that specifies the storage error code string in a failed response.
            /// </summary>
            internal const string StorageErrorCodeHeader = "x-ms-error-code";
        }

        /// <summary>
        /// Constants for query strings.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Reviewed.")]
        public static class QueryConstants
        {
            /// <summary>
            /// Query component for snapshot time.
            /// </summary>
            public const string Snapshot = "snapshot";

            /// <summary>
            /// Query component for share snapshot time.
            /// </summary>
            public const string ShareSnapshot = "sharesnapshot";

            /// <summary>
            /// Query component for the signed SAS start time.
            /// </summary>
            public const string SignedStart = "st";

            /// <summary>
            /// Query component for the signed SAS expiry time.
            /// </summary>
            public const string SignedExpiry = "se";

            /// <summary>
            /// Query component for the signed SAS resource.
            /// </summary>
            public const string SignedResource = "sr";

            /// <summary>
            /// Query component for the signed SAS resource types.
            /// </summary>
            public const string SignedResourceTypes = "srt";

            /// <summary>
            /// Query component for the signed SAS service.
            /// </summary>
            public const string SignedServices = "ss";

            /// <summary>
            /// Query component for the signed SAS protocol.
            /// </summary>
            public const string SignedProtocols = "spr";

            /// <summary>
            /// Query component for the signed SAS IP.
            /// </summary>
            public const string SignedIP = "sip";

            /// <summary>
            /// Query component for the SAS table name.
            /// </summary>
            public const string SasTableName = "tn";

            /// <summary>
            /// Query component for the signed SAS permissions.
            /// </summary>
            public const string SignedPermissions = "sp";

            /// <summary>
            /// Query component for the SAS start partition key.
            /// </summary>
            public const string StartPartitionKey = "spk";

            /// <summary>
            /// Query component for the SAS start row key.
            /// </summary>
            public const string StartRowKey = "srk";

            /// <summary>
            /// Query component for the SAS end partition key.
            /// </summary>
            public const string EndPartitionKey = "epk";

            /// <summary>
            /// Query component for the SAS end row key.
            /// </summary>
            public const string EndRowKey = "erk";

            /// <summary>
            /// Query component for the signed SAS identifier.
            /// </summary>
            public const string SignedIdentifier = "si";

            /// <summary>
            /// Query component for the signing SAS key.
            /// </summary>
            public const string SignedKey = "sk";

            /// <summary>
            /// Query component for the signed SAS version.
            /// </summary>
            public const string SignedVersion = "sv";

            /// <summary>
            /// Query component for SAS signature.
            /// </summary>
            public const string Signature = "sig";

            /// <summary>
            /// Query component for SAS cache control.
            /// </summary>
            public const string CacheControl = "rscc";

            /// <summary>
            /// Query component for SAS content type.
            /// </summary>
            public const string ContentType = "rsct";

            /// <summary>
            /// Query component for SAS content encoding.
            /// </summary>
            public const string ContentEncoding = "rsce";

            /// <summary>
            /// Query component for SAS content language.
            /// </summary>
            public const string ContentLanguage = "rscl";

            /// <summary>
            /// Query component for SAS content disposition.
            /// </summary>
            public const string ContentDisposition = "rscd";

            /// <summary>
            /// Query component for SAS API version.
            /// </summary>
            public const string ApiVersion = "api-version";

            /// <summary>
            /// Query component for message time-to-live.
            /// </summary>
            public const string MessageTimeToLive = "messagettl";

            /// <summary>
            /// Query component for message visibility timeout.
            /// </summary>
            public const string VisibilityTimeout = "visibilitytimeout";

            /// <summary>
            /// Query component for the number of messages.
            /// </summary>
            [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Num", Justification = "Reviewed : Num is allowed in an identifier name.")]
            public const string NumOfMessages = "numofmessages";

            /// <summary>
            /// Query component for message pop receipt.
            /// </summary>
            public const string PopReceipt = "popreceipt";

            /// <summary>
            /// Query component for resource type.
            /// </summary>
            public const string ResourceType = "restype";

            /// <summary>
            /// Query component for the operation (component) to access.
            /// </summary>
            public const string Component = "comp";

            /// <summary>
            /// Query component for the copy ID.
            /// </summary>
            public const string CopyId = "copyid";
            
        }

        /// <summary>
        /// Constants for Result Continuations
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Reviewed.")]
        public static class ContinuationConstants
        {
            /// <summary>
            /// Top Element for Continuation Tokens
            /// </summary>
            public const string ContinuationTopElement = "ContinuationToken";

            /// <summary>
            /// XML element for the next marker.
            /// </summary>
            public const string NextMarkerElement = "NextMarker";

            /// <summary>
            /// XML element for the next partition key.
            /// </summary>
            public const string NextPartitionKeyElement = "NextPartitionKey";

            /// <summary>
            /// XML element for the next row key.
            /// </summary>
            public const string NextRowKeyElement = "NextRowKey";

            /// <summary>
            /// XML element for the next table name.
            /// </summary>
            public const string NextTableNameElement = "NextTableName";

            /// <summary>
            /// XML element for the target location.
            /// </summary>
            public const string TargetLocationElement = "TargetLocation";

            /// <summary>
            /// XML element for the token version.
            /// </summary>
            public const string VersionElement = "Version";

            /// <summary>
            /// Stores the current token version value.
            /// </summary>
            public const string CurrentVersion = "2.0";

            /// <summary>
            /// XML element for the token type.
            /// </summary>
            public const string TypeElement = "Type";

            /// <summary>
            /// Specifies the blob continuation token type.
            /// </summary>
            public const string BlobType = "Blob";

            /// <summary>
            /// Specifies the queue continuation token type.
            /// </summary>
            public const string QueueType = "Queue";

            /// <summary>
            /// Specifies the table continuation token type.
            /// </summary>
            public const string TableType = "Table";

            /// <summary>
            /// Specifies the file continuation token type.
            /// </summary>
            public const string FileType = "File";
        }

        /// <summary>
        /// Constants for version strings
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Reviewed.")]
        public static class VersionConstants
        {
            /// <summary>
            /// Constant for the 2013-08-15 version.
            /// </summary>
            public const string August2013 = "2013-08-15";

            /// <summary>
            /// Constant for the 2012-02-12 version.
            /// </summary>
            public const string February2012 = "2012-02-12";
        }

        /// <summary>
        /// Constants for analytics client
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Reviewed.")]
        public static class AnalyticsConstants
        {
            /// <summary>
            /// Constant for the logs container.
            /// </summary>
            public const string LogsContainer = "$logs";

            /// <summary>
            /// Constant for the blob capacity metrics table.
            /// </summary>
            public const string MetricsCapacityBlob = "$MetricsCapacityBlob";

            /// <summary>
            /// Constant for the blob service primary location hourly metrics table.
            /// </summary>
            public const string MetricsHourPrimaryTransactionsBlob = "$MetricsHourPrimaryTransactionsBlob";

            /// <summary>
            /// Constant for the table service primary location hourly metrics table.
            /// </summary>
            public const string MetricsHourPrimaryTransactionsTable = "$MetricsHourPrimaryTransactionsTable";

            /// <summary>
            /// Constant for the queue service primary location hourly metrics table.
            /// </summary>
            public const string MetricsHourPrimaryTransactionsQueue = "$MetricsHourPrimaryTransactionsQueue";

            /// <summary>
            /// Constant for the file service primary location hourly metrics table.
            /// </summary>
            public const string MetricsHourPrimaryTransactionsFile = "$MetricsHourPrimaryTransactionsFile";

            /// <summary>
            /// Constant for the blob service primary location minute metrics table.
            /// </summary>
            public const string MetricsMinutePrimaryTransactionsBlob = "$MetricsMinutePrimaryTransactionsBlob";

            /// <summary>
            /// Constant for the table service primary location minute metrics table.
            /// </summary>
            public const string MetricsMinutePrimaryTransactionsTable = "$MetricsMinutePrimaryTransactionsTable";

            /// <summary>
            /// Constant for the queue service primary location minute metrics table.
            /// </summary>
            public const string MetricsMinutePrimaryTransactionsQueue = "$MetricsMinutePrimaryTransactionsQueue";

            /// <summary>
            /// Constant for the file service primary location minute metrics table.
            /// </summary>
            public const string MetricsMinutePrimaryTransactionsFile = "$MetricsMinutePrimaryTransactionsFile";

            /// <summary>
            /// Constant for the blob service secondary location hourly metrics table.
            /// </summary>
            public const string MetricsHourSecondaryTransactionsBlob = "$MetricsHourSecondaryTransactionsBlob";

            /// <summary>
            /// Constant for the table service secondary location hourly metrics table.
            /// </summary>
            public const string MetricsHourSecondaryTransactionsTable = "$MetricsHourSecondaryTransactionsTable";

            /// <summary>
            /// Constant for the queue service secondary location hourly metrics table.
            /// </summary>
            public const string MetricsHourSecondaryTransactionsQueue = "$MetricsHourSecondaryTransactionsQueue";

            /// <summary>
            /// Constant for the file service secondary location hourly metrics table.
            /// </summary>
            public const string MetricsHourSecondaryTransactionsFile = "$MetricsHourSecondaryTransactionsFile";

            /// <summary>
            /// Constant for the blob service secondary location minute metrics table.
            /// </summary>
            public const string MetricsMinuteSecondaryTransactionsBlob = "$MetricsMinuteSecondaryTransactionsBlob";

            /// <summary>
            /// Constant for the table service secondary location minute metrics table.
            /// </summary>
            public const string MetricsMinuteSecondaryTransactionsTable = "$MetricsMinuteSecondaryTransactionsTable";

            /// <summary>
            /// Constant for the queue service secondary location minute metrics table.
            /// </summary>
            public const string MetricsMinuteSecondaryTransactionsQueue = "$MetricsMinuteSecondaryTransactionsQueue";

            /// <summary>
            /// Constant for the file service secondary location minute metrics table.
            /// </summary>
            public const string MetricsMinuteSecondaryTransactionsFile = "$MetricsMinuteSecondaryTransactionsFile";

            /// <summary>
            /// Constant for default logging version.
            /// </summary>
            public const string LoggingVersionV1 = "1.0";

            /// <summary>
            /// Constant for default metrics version.
            /// </summary>
            public const string MetricsVersionV1 = "1.0";
        }

        /// <summary>
        /// Constants for client encryption.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Reviewed.")]
        public static class EncryptionConstants
        {
            /// <summary>
            /// Constant for the encryption protocol.
            /// </summary>
            internal const string EncryptionProtocolV1 = "1.0";

            /// <summary>
            /// Encryption metadata key for key wrapping IV.
            /// </summary>
            internal const string KeyWrappingIV = "KeyWrappingIV";

            /// <summary>
            /// Metadata header to store encryption materials.
            /// </summary>
            public const string BlobEncryptionData = "encryptiondata";

            /// <summary>
            /// Property name to store the encryption metadata.
            /// </summary>
            public const string TableEncryptionKeyDetails = "_ClientEncryptionMetadata1";

            /// <summary>
            /// Additional property name to store the encryption metadata.
            /// </summary>
            public const string TableEncryptionPropertyDetails = "_ClientEncryptionMetadata2";

            /// <summary>
            /// Key for the encryption agent
            /// </summary>
            public const string AgentMetadataKey = "EncryptionLibrary";

            /// <summary>
            /// Value for the encryption agent
            /// </summary>
            public const string AgentMetadataValue = ".NET " + Constants.HeaderConstants.UserAgentProductVersion;
        }
    }
}