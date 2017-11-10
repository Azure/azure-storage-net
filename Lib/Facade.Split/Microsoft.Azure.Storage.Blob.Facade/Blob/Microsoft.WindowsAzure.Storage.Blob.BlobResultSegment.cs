using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Blob
{
public class BlobResultSegment
{
    public IEnumerable<IListBlobItem> Results
    {
        get; private set;
    }

    public BlobContinuationToken ContinuationToken
    {
        get; private set;
    }

    internal BlobResultSegment(IEnumerable<IListBlobItem> blobs, BlobContinuationToken continuationToken)
    {
        throw new System.NotImplementedException();
    }
}

}