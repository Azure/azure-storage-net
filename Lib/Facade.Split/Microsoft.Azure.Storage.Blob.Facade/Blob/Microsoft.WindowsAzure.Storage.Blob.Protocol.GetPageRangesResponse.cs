using Microsoft.Azure.Storage.Shared.Protocol;
using System.Collections.Generic;
using System.IO;
namespace Microsoft.Azure.Storage.Blob.Protocol
{
internal sealed class GetPageRangesResponse : ResponseParsingBase<PageRange>
{
    public IEnumerable<PageRange> PageRanges
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public GetPageRangesResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    private PageRange ParsePageRange()
    {
        throw new System.NotImplementedException();
    }
    protected override IEnumerable<PageRange> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}