using Microsoft.Azure.Storage.Shared.Protocol;
using System.Collections.Generic;
using System.IO;
namespace Microsoft.Azure.Storage.Blob.Protocol
{
internal class GetBlockListResponse : ResponseParsingBase<ListBlockItem>
{
    public IEnumerable<ListBlockItem> Blocks
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public GetBlockListResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    private ListBlockItem ParseBlockItem(bool committed)
    {
        throw new System.NotImplementedException();
    }
    protected override IEnumerable<ListBlockItem> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}