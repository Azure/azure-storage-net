using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
internal sealed class ListSharesResponse : ResponseParsingBase<FileShareEntry>
{

    public ListingContext ListingContext
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public IEnumerable<FileShareEntry> Shares
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

    public ListSharesResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    private FileShareEntry ParseShareEntry(Uri baseUri)
    {
        throw new System.NotImplementedException();
    }
    protected override IEnumerable<FileShareEntry> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}