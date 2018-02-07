using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.File
{
public class FileResultSegment
{
    public IEnumerable<IListFileItem> Results
    {
        get; private set;
    }

    public FileContinuationToken ContinuationToken
    {
        get; private set;
    }

    public FileResultSegment(IEnumerable<IListFileItem> files, FileContinuationToken continuationToken)
    {
        throw new System.NotImplementedException();
    }
}

}