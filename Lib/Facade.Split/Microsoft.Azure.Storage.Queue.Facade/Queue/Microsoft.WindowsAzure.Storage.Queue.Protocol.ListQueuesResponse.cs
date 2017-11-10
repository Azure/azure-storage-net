using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
namespace Microsoft.Azure.Storage.Queue.Protocol
{
internal sealed class ListQueuesResponse : ResponseParsingBase<QueueEntry>
{

    public ListingContext ListingContext
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public IEnumerable<QueueEntry> Queues
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public string Prefix
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public string Marker
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public int MaxResults
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public string NextMarker
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public ListQueuesResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    private QueueEntry ParseQueueEntry(Uri baseUri)
    {
        throw new System.NotImplementedException();
    }
    protected override IEnumerable<QueueEntry> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}