using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
internal sealed class ListBlobsResponse : ResponseParsingBase<IListBlobEntry>
{
    public BlobListingContext ListingContext
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public IEnumerable<IListBlobEntry> Blobs
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

    public string Delimiter
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

    public ListBlobsResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    private IListBlobEntry ParseBlobEntry(Uri baseUri)
    {
        throw new System.NotImplementedException();
    }
    private IListBlobEntry ParseBlobPrefixEntry()
    {
        throw new System.NotImplementedException();
    }
    protected override IEnumerable<IListBlobEntry> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}