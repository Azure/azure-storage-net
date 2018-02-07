using System;
using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Queue.Protocol
{
internal sealed class QueueEntry
{
    public IDictionary<string, string> Metadata
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

    internal QueueEntry()
    {
        throw new System.NotImplementedException();
    }
    internal QueueEntry(string name, Uri uri, IDictionary<string, string> metadata)
    {
        throw new System.NotImplementedException();
    }
}

}