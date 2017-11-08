using System;
namespace Microsoft.WindowsAzure.Storage.File
{
[Flags]
public enum ShareListingDetails
{
    None = 0,
    Metadata = 1,
    All = Metadata,
}

}