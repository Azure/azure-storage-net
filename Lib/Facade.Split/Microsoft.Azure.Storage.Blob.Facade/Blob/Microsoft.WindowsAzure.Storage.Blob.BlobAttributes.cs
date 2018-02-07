using System;
using System.Collections.Generic;
using System.Globalization;
namespace Microsoft.Azure.Storage.Blob
{
internal sealed class BlobAttributes
{
    public BlobProperties Properties
    {
        get; internal set;
    }

    public IDictionary<string, string> Metadata
    {
        get; internal set;
    }

    public Uri Uri
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public StorageUri StorageUri
    {
        get; internal set;
    }

    public DateTimeOffset? SnapshotTime
    {
        get; internal set;
    }

    public CopyState CopyState
    {
        get; internal set;
    }

    internal BlobAttributes()
    {
        throw new System.NotImplementedException();
    }
    internal void AssertNoSnapshot()
    {
        throw new System.NotImplementedException();
    }
}

}