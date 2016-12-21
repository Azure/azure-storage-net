using System;
namespace Microsoft.WindowsAzure.Storage.Table
{
[Flags]
internal enum EntityReadFlags
{
    PartitionKey = 1,
    RowKey = 2,
    Timestamp = 4,
    Etag = 8,
    Properties = 16,
    All = Properties | Etag | Timestamp | RowKey | PartitionKey,
}

}