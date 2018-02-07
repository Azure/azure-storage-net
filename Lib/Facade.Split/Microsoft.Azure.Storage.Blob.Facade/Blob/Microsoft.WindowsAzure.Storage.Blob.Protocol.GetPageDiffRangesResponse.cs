using Microsoft.Azure.Storage.Shared.Protocol;
using System.Collections.Generic;
using System.IO;
namespace Microsoft.Azure.Storage.Blob.Protocol
{
internal sealed class GetPageDiffRangesResponse : ResponseParsingBase<PageDiffRange>
{
    public IEnumerable<PageDiffRange> PageDiffRanges
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public GetPageDiffRangesResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    private PageDiffRange ParsePageDiffRange(bool isCleared)
    {
        throw new System.NotImplementedException();
    }
    protected override IEnumerable<PageDiffRange> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}