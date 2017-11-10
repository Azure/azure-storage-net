using System.Collections.Generic;
namespace Microsoft.Azure.Storage.File
{
public class ShareResultSegment
{
    public IEnumerable<CloudFileShare> Results
    {
        get; private set;
    }

    public FileContinuationToken ContinuationToken
    {
        get; private set;
    }

    internal ShareResultSegment(IEnumerable<CloudFileShare> shares, FileContinuationToken continuationToken)
    {
        throw new System.NotImplementedException();
    }
}

}