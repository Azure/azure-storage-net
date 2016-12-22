using System;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
internal sealed class ListBlobEntry : IListBlobEntry
{
    internal BlobAttributes Attributes
    {
        get; private set;
    }

    public string Name
    {
        get; private set;
    }

    public BlobProperties Properties
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public IDictionary<string, string> Metadata
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public Uri Uri
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public DateTimeOffset? SnapshotTime
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public CopyState CopyState
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal ListBlobEntry(string name, BlobAttributes attributes)
    {
        throw new System.NotImplementedException();
    }
}

}