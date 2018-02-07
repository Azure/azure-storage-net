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
    Deleted = 16,
    All = Deleted | Copy | UncommittedBlobs | Metadata | Snapshots,
}

}