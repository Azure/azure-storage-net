using System;
namespace Microsoft.Azure.Storage.Shared.Protocol
{

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

}