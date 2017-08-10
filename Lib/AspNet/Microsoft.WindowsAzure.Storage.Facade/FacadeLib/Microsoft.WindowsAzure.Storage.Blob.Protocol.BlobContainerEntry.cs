using System;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
internal sealed class BlobContainerEntry
{
    public IDictionary<string, string> Metadata
    {
        get; internal set;
    }

    public BlobContainerProperties Properties
    {
        get; internal set;
    }

    public string Name
    {
        get; internal set;
    }

    public Uri Uri
    {
        get; internal set;
    }

    internal BlobContainerEntry()
    {
        throw new System.NotImplementedException();
    }
}

}