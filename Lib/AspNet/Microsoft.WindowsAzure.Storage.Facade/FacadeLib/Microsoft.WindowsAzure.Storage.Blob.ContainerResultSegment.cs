using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.Blob
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

    public ContainerResultSegment()
    {
        throw new System.NotImplementedException();
    }
    internal ContainerResultSegment(IEnumerable<CloudBlobContainer> containers, BlobContinuationToken continuationToken)
    {
        throw new System.NotImplementedException();
    }
}

}