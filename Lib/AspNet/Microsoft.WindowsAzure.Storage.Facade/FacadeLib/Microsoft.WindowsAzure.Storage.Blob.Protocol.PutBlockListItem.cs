
namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
internal sealed class PutBlockListItem
{
    public string Id
    {
        get; private set;
    }

    public BlockSearchMode SearchMode
    {
        get; private set;
    }

    public PutBlockListItem(string id, BlockSearchMode searchMode)
    {
        throw new System.NotImplementedException();
    }
}

}