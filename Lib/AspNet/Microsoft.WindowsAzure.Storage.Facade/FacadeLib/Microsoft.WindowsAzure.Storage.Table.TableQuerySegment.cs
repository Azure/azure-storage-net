using System.Collections;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.Table
{
public class TableQuerySegment<TElement> : IEnumerable<TElement>, IEnumerable
{

    public List<TElement> Results
    {
        get; internal set;
    }

    public TableContinuationToken ContinuationToken
    {
        get
        {
            throw new System.NotImplementedException();
        }
        internal set
        {
            throw new System.NotImplementedException();
        }
    }

    internal TableQuerySegment(List<TElement> result)
    {
        throw new System.NotImplementedException();
    }
    internal TableQuerySegment(ResultSegment<TElement> resSeg)
      : this(resSeg.Results)
    {
        throw new System.NotImplementedException();
    }
    public IEnumerator<TElement> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}

}