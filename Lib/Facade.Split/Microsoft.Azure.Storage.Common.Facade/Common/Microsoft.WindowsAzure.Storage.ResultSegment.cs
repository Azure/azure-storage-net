using System.Collections.Generic;
namespace Microsoft.Azure.Storage
{
internal class ResultSegment<TElement>
{

    public List<TElement> Results
    {
        get; internal set;
    }

    public IContinuationToken ContinuationToken
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

    internal ResultSegment(List<TElement> result)
    {
        throw new System.NotImplementedException();
    }
}

}