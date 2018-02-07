using System;
namespace Microsoft.Azure.Storage.Blob
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