using System.Collections;
using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Table
{
public sealed class TableResultSegment : IEnumerable<CloudTable>, IEnumerable
{

    public IList<CloudTable> Results
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

    internal TableResultSegment(List<CloudTable> result)
    {
        throw new System.NotImplementedException();
    }
    public IEnumerator<CloudTable> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}

}