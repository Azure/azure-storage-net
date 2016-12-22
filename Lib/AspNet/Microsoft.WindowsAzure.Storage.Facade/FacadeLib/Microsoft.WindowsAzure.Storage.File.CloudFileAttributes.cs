using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.File
{
internal sealed class CloudFileAttributes
{
    public FileProperties Properties
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

    public CopyState CopyState
    {
        get; internal set;
    }

    internal CloudFileAttributes()
    {
        throw new System.NotImplementedException();
    }
}

}