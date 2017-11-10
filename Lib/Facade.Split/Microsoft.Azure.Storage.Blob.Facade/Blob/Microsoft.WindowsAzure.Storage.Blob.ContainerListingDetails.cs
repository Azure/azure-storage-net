using System;
namespace Microsoft.Azure.Storage.Blob
{
[Flags]
public enum ContainerListingDetails
{
    None = 0,
    Metadata = 1,
    All = Metadata,
}

}