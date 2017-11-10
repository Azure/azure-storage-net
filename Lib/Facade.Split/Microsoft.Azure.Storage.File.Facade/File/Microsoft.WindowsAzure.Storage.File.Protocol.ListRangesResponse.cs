using Microsoft.Azure.Storage.Shared.Protocol;
using System.Collections.Generic;
using System.IO;
namespace Microsoft.Azure.Storage.File.Protocol
{
internal sealed class ListRangesResponse : ResponseParsingBase<FileRange>
{
    public IEnumerable<FileRange> Ranges
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public ListRangesResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    private FileRange ParseRange()
    {
        throw new System.NotImplementedException();
    }
    protected override IEnumerable<FileRange> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}