
namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
internal sealed class ListBlobPrefixEntry : IListBlobEntry
{
    public string Name
    {
        get; internal set;
    }
}

}