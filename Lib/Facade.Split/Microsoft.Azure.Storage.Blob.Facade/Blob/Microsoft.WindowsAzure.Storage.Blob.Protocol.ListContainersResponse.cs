using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
namespace Microsoft.Azure.Storage.Blob.Protocol
{
internal sealed class ListContainersResponse : ResponseParsingBase<BlobContainerEntry>
{
    public ListingContext ListingContext
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public IEnumerable<BlobContainerEntry> Containers
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

    public ListContainersResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    private BlobContainerEntry ParseContainerEntry(Uri baseUri)
    {
        throw new System.NotImplementedException();
    }
    protected override IEnumerable<BlobContainerEntry> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}