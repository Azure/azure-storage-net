using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.File
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

    public ShareResultSegment(IEnumerable<CloudFileShare> shares, FileContinuationToken continuationToken)
    {
        throw new System.NotImplementedException();
    }
}

}