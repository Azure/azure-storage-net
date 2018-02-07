using System;
namespace Microsoft.Azure.Storage.Queue.Protocol
{
[Flags]
public enum QueueListingDetails
{
    None = 0,
    Metadata = 1,
    All = Metadata,
}

}