using System;
namespace Microsoft.WindowsAzure.Storage.Blob
{
[Flags]
public enum BlobListingDetails
{
    None = 0,
    Snapshots = 1,
    Metadata = 2,
    UncommittedBlobs = 4,
    Copy = 8,
    All = Copy | UncommittedBlobs | Metadata | Snapshots,
}

}