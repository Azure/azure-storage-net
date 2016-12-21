using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.File
{
public sealed class FileResultSegment
{
    public IEnumerable<IListFileItem> Results
    {
        get; private set;
    }

    public FileContinuationToken ContinuationToken
    {
        get; private set;
    }

    internal FileResultSegment(IEnumerable<IListFileItem> files, FileContinuationToken continuationToken)
    {
        throw new System.NotImplementedException();
    }
}

}