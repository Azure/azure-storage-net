using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.Blob
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

    public BlobResultSegment(IEnumerable<IListBlobItem> blobs, BlobContinuationToken continuationToken)
    {
        throw new System.NotImplementedException();
    }
}

}