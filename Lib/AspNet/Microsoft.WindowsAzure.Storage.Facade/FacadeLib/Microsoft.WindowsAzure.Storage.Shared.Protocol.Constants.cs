using System;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
internal static class Constants
{
    public static readonly TimeSpan MaxMaximumExecutionTime = TimeSpan.FromDays(24.0);
    public static readonly TimeSpan DefaultClientSideTimeout = TimeSpan.FromMinutes(5.0);
    public static readonly TimeSpan MaximumRetryBackoff = TimeSpan.FromHours(1.0);
    public static readonly TimeSpan MaximumAllowedTimeout = TimeSpan.FromSeconds((double) int.MaxValue);
    public const int MaxParallelOperationThreadCount = 64;
    public const int MaxSharedAccessPolicyIdentifiers = 5;
    public const int DefaultWriteBlockSizeBytes = 4194304;
    public const int DefaultSubStreamBufferSize = 4194304;
    public const long DefaultParallelDownloadRangeSizeBytes = 16777216;
    public const long MaxSingleUploadBlobSize = 268435456;
    public const int MaxBlockSize = 104857600;
    public const int MaxAppendBlockSize = 4194304;
    public const int MaxIdleTimeMs = 120000;
    public const int MaxRangeGetContentMD5Size = 4194304;
    public const long MaxBlockNumber = 50000;
    public const long MaxBlobSize = 5242880000000;
    public const int MinLargeBlockSize = 4194305;
    internal const int DefaultBufferSize = 65536;
    internal const string LogSourceName = "Microsoft.WindowsAzure.Storage";
    public const int PageSize = 512;
    public const long KB = 1024;
    public const long MB = 1048576;
    public const long GB = 1073741824;
    public const string CommittedBlocksElement = "CommittedBlocks";
    public const string UncommittedBlocksElement = "UncommittedBlocks";
    public const string BlockElement = "Block";
    public const string NameElement = "Name";
    public const string SizeElement = "Size";
    public const string BlockListElement = "BlockList";
    public const string MessagesElement = "QueueMessagesList";
    public const string MessageElement = "QueueMessage";
    public const string MessageIdElement = "MessageId";
    public const string InsertionTimeElement = "InsertionTime";
    public const string ExpirationTimeElement = "ExpirationTime";
    public const string PopReceiptElement = "PopReceipt";
    public const string TimeNextVisibleElement = "TimeNextVisible";
    public const string MessageTextElement = "MessageText";
    public const string DequeueCountElement = "DequeueCount";
    public const string PageRangeElement = "PageRange";
    public const string ClearRangeElement = "ClearRange";
    public const string PageListElement = "PageList";
    public const string StartElement = "Start";
    public const string EndElement = "End";
    public const string DelimiterElement = "Delimiter";
    public const string BlobPrefixElement = "BlobPrefix";
    public const string CacheControlElement = "Cache-Control";
    public const string ContentTypeElement = "Content-Type";
    public const string ContentEncodingElement = "Content-Encoding";
    public const string ContentLanguageElement = "Content-Language";
    public const string ContentLengthElement = "Content-Length";
    public const string ContentMD5Element = "Content-MD5";
    public const string EnumerationResultsElement = "EnumerationResults";
    public const string ServiceEndpointElement = "ServiceEndpoint";
    public const string ContainerNameElement = "ContainerName";
    public const string ShareNameElement = "ShareName";
    public const string DirectoryPathElement = "DirectoryPath";
    public const string BlobsElement = "Blobs";
    public const string PrefixElement = "Prefix";
    public const string MaxResultsElement = "MaxResults";
    public const string MarkerElement = "Marker";
    public const string NextMarkerElement = "NextMarker";
    public const string EtagElement = "Etag";
    public const string LastModifiedElement = "Last-Modified";
    public const string ServerEncryptionElement = "ServerEncrypted";
    public const string UrlElement = "Url";
    public const string BlobElement = "Blob";
    public const string CopyIdElement = "CopyId";
    public const string CopyStatusElement = "CopyStatus";
    public const string CopySourceElement = "CopySource";
    public const string CopyProgressElement = "CopyProgress";
    public const string CopyCompletionTimeElement = "CopyCompletionTime";
    public const string CopyStatusDescriptionElement = "CopyStatusDescription";
    public const string IncrementalCopy = "IncrementalCopy";
    public const string CopyDestinationSnapshotElement = "CopyDestinationSnapshot";
    public const string DeletedElement = "Deleted";
    public const string DeletedTimeElement = "DeletedTime";
    public const string RemainingRetentionDaysElement = "RemainingRetentionDays";
    public const string PageBlobValue = "PageBlob";
    public const string BlockBlobValue = "BlockBlob";
    public const string AppendBlobValue = "AppendBlob";
    public const string LockedValue = "locked";
    public const string UnlockedValue = "unlocked";
    public const string LeaseAvailableValue = "available";
    public const string LeasedValue = "leased";
    public const string LeaseExpiredValue = "expired";
    public const string LeaseBreakingValue = "breaking";
    public const string LeaseBrokenValue = "broken";
    public const string LeaseInfiniteValue = "infinite";
    public const string LeaseFixedValue = "fixed";
    public const int MinimumBreakLeasePeriod = 0;
    public const int MaximumBreakLeasePeriod = 60;
    public const int MinimumLeaseDuration = 15;
    public const int MaximumLeaseDuration = 60;
    public const string CopyPendingValue = "pending";
    public const string CopySuccessValue = "success";
    public const string CopyAbortedValue = "aborted";
    public const string CopyFailedValue = "failed";
    public const string RehydratePendingToHot = "rehydrate-pending-to-hot";
    public const string RehydratePendingToCool = "rehydrate-pending-to-cool";
    public const string GeoUnavailableValue = "unavailable";
    public const string GeoLiveValue = "live";
    public const string GeoBootstrapValue = "bootstrap";
    public const string AccessTierElement = "AccessTier";
    public const string AccessTierInferred = "AccessTierInferred";
    public const string AccessTierChangeTimeElement = "AccessTierChangeTime";
    public const string ArchiveStatusElement = "ArchiveStatus";
    public const string BlobTypeElement = "BlobType";
    public const string LeaseStatusElement = "LeaseStatus";
    public const string LeaseStateElement = "LeaseState";
    public const string LeaseDurationElement = "LeaseDuration";
    public const string PublicAccessElement = "PublicAccess";
    public const string SnapshotElement = "Snapshot";
    public const string ContainersElement = "Containers";
    public const string ContainerElement = "Container";
    public const string SharesElement = "Shares";
    public const string ShareElement = "Share";
    public const string QuotaElement = "Quota";
    public const string FileRangeElement = "Range";
    public const string FileRangeListElement = "Ranges";
    public const string EntriesElement = "Entries";
    public const string FileElement = "File";
    public const string FileDirectoryElement = "Directory";
    public const string QueuesElement = "Queues";
    public const string QueueNameElement = "Name";
    public const string QueueElement = "Queue";
    public const string PropertiesElement = "Properties";
    public const string MetadataElement = "Metadata";
    public const string InvalidMetadataName = "x-ms-invalid-name";
    public const string MaxResults = "MaxResults";
    public const string CommittedElement = "Committed";
    public const string UncommittedElement = "Uncommitted";
    public const string LatestElement = "Latest";
    public const string SignedIdentifiers = "SignedIdentifiers";
    public const string SignedIdentifier = "SignedIdentifier";
    public const string AccessPolicy = "AccessPolicy";
    public const string Id = "Id";
    public const string Start = "Start";
    public const string Expiry = "Expiry";
    public const string Permission = "Permission";
    public const string Messages = "messages";
    internal const string ErrorException = "exceptiondetails";
    public const string ErrorRootElement = "Error";
    public const string ErrorCode = "Code";
    internal const string ErrorCodePreview = "code";
    public const string ErrorMessage = "Message";
    internal const string ErrorMessagePreview = "message";
    public const string ErrorExceptionMessage = "ExceptionMessage";
    public const string ErrorExceptionStackTrace = "StackTrace";
    internal const string EdmEntityTypeNamespaceName = "AzureTableStorage";
    internal const string EdmEntityTypeName = "DefaultContainer";
    internal const string EntitySetName = "Tables";
    internal const string Edm = "Edm.";
    internal const string DefaultNamespaceName = "account";
    internal const string DefaultTableName = "TableName";
    internal const string XMLAcceptHeaderValue = "application/xml";
    internal const string JsonLightAcceptHeaderValue = "application/json;odata=minimalmetadata";
    internal const string JsonFullMetadataAcceptHeaderValue = "application/json;odata=fullmetadata";
    internal const string JsonNoMetadataAcceptHeaderValue = "application/json;odata=nometadata";
    internal const string NoMetadata = "odata=nometadata";
    internal const string JsonContentTypeHeaderValue = "application/json";
    internal const string ETagPrefix = "\"datetime'";

    public static class QueryConstants
    {
        public const string Snapshot = "snapshot";
        public const string ShareSnapshot = "sharesnapshot";
        public const string SignedStart = "st";
        public const string SignedExpiry = "se";
        public const string SignedResource = "sr";
        public const string SignedResourceTypes = "srt";
        public const string SignedServices = "ss";
        public const string SignedProtocols = "spr";
        public const string SignedIP = "sip";
        public const string SasTableName = "tn";
        public const string SignedPermissions = "sp";
        public const string StartPartitionKey = "spk";
        public const string StartRowKey = "srk";
        public const string EndPartitionKey = "epk";
        public const string EndRowKey = "erk";
        public const string SignedIdentifier = "si";
        public const string SignedKey = "sk";
        public const string SignedVersion = "sv";
        public const string Signature = "sig";
        public const string CacheControl = "rscc";
        public const string ContentType = "rsct";
        public const string ContentEncoding = "rsce";
        public const string ContentLanguage = "rscl";
        public const string ContentDisposition = "rscd";
        public const string ApiVersion = "api-version";
        public const string MessageTimeToLive = "messagettl";
        public const string VisibilityTimeout = "visibilitytimeout";
        public const string NumOfMessages = "numofmessages";
        public const string PopReceipt = "popreceipt";
        public const string ResourceType = "restype";
        public const string Component = "comp";
        public const string CopyId = "copyid";
    }

    public static class ContinuationConstants
    {
        public const string ContinuationTopElement = "ContinuationToken";
        public const string NextMarkerElement = "NextMarker";
        public const string NextPartitionKeyElement = "NextPartitionKey";
        public const string NextRowKeyElement = "NextRowKey";
        public const string NextTableNameElement = "NextTableName";
        public const string TargetLocationElement = "TargetLocation";
        public const string VersionElement = "Version";
        public const string CurrentVersion = "2.0";
        public const string TypeElement = "Type";
        public const string BlobType = "Blob";
        public const string QueueType = "Queue";
        public const string TableType = "Table";
        public const string FileType = "File";
    }

    public static class VersionConstants
    {
        public const string August2013 = "2013-08-15";
        public const string February2012 = "2012-02-12";
    }

    public static class AnalyticsConstants
    {
        public const string LogsContainer = "$logs";
        public const string MetricsCapacityBlob = "$MetricsCapacityBlob";
        public const string MetricsHourPrimaryTransactionsBlob = "$MetricsHourPrimaryTransactionsBlob";
        public const string MetricsHourPrimaryTransactionsTable = "$MetricsHourPrimaryTransactionsTable";
        public const string MetricsHourPrimaryTransactionsQueue = "$MetricsHourPrimaryTransactionsQueue";
        public const string MetricsHourPrimaryTransactionsFile = "$MetricsHourPrimaryTransactionsFile";
        public const string MetricsMinutePrimaryTransactionsBlob = "$MetricsMinutePrimaryTransactionsBlob";
        public const string MetricsMinutePrimaryTransactionsTable = "$MetricsMinutePrimaryTransactionsTable";
        public const string MetricsMinutePrimaryTransactionsQueue = "$MetricsMinutePrimaryTransactionsQueue";
        public const string MetricsMinutePrimaryTransactionsFile = "$MetricsMinutePrimaryTransactionsFile";
        public const string MetricsHourSecondaryTransactionsBlob = "$MetricsHourSecondaryTransactionsBlob";
        public const string MetricsHourSecondaryTransactionsTable = "$MetricsHourSecondaryTransactionsTable";
        public const string MetricsHourSecondaryTransactionsQueue = "$MetricsHourSecondaryTransactionsQueue";
        public const string MetricsHourSecondaryTransactionsFile = "$MetricsHourSecondaryTransactionsFile";
        public const string MetricsMinuteSecondaryTransactionsBlob = "$MetricsMinuteSecondaryTransactionsBlob";
        public const string MetricsMinuteSecondaryTransactionsTable = "$MetricsMinuteSecondaryTransactionsTable";
        public const string MetricsMinuteSecondaryTransactionsQueue = "$MetricsMinuteSecondaryTransactionsQueue";
        public const string MetricsMinuteSecondaryTransactionsFile = "$MetricsMinuteSecondaryTransactionsFile";
        public const string LoggingVersionV1 = "1.0";
        public const string MetricsVersionV1 = "1.0";
    }

    public static class EncryptionConstants
    {
        internal const string EncryptionProtocolV1 = "1.0";
        internal const string KeyWrappingIV = "KeyWrappingIV";
        public const string BlobEncryptionData = "encryptiondata";
        public const string TableEncryptionKeyDetails = "_ClientEncryptionMetadata1";
        public const string TableEncryptionPropertyDetails = "_ClientEncryptionMetadata2";
        public const string AgentMetadataKey = "EncryptionLibrary";
        public const string AgentMetadataValue = ".NET 9.3.1";

    }
}

}