using System;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
internal sealed class FileShareEntry
{
    public IDictionary<string, string> Metadata
    {
        get; internal set;
    }

    public FileShareProperties Properties
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

    internal DateTimeOffset? SnapshotTime
    {
        get; set;
    }

    internal FileShareEntry()
    {
        throw new System.NotImplementedException();
    }
}

}