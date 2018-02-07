
namespace Microsoft.Azure.Storage.Blob
{
public sealed class ListBlockItem
{
    public string Name
    {
        get; internal set;
    }

    public long Length
    {
        get; internal set;
    }

    public bool Committed
    {
        get; internal set;
    }
}

}