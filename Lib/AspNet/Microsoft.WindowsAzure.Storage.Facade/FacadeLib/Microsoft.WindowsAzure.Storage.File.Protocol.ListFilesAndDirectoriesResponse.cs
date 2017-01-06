using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
internal sealed class ListFilesAndDirectoriesResponse : ResponseParsingBase<IListFileEntry>
{

    public FileListingContext ListingContext
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public IEnumerable<IListFileEntry> Files
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

    public ListFilesAndDirectoriesResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    private IListFileEntry ParseFileEntry(Uri baseUri)
    {
        throw new System.NotImplementedException();
    }
    private IListFileEntry ParseFileDirectoryEntry(Uri baseUri)
    {
        throw new System.NotImplementedException();
    }
    protected override IEnumerable<IListFileEntry> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}