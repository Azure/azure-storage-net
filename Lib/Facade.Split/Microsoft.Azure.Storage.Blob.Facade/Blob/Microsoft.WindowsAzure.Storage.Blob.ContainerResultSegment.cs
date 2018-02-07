using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Blob
{
public class ContainerResultSegment
{
    public IEnumerable<CloudBlobContainer> Results
    {
        get; private set;
    }

    public BlobContinuationToken ContinuationToken
    {
        get; private set;
    }

    internal ContainerResultSegment(IEnumerable<CloudBlobContainer> containers, BlobContinuationToken continuationToken)
    {
        throw new System.NotImplementedException();
    }
}

}