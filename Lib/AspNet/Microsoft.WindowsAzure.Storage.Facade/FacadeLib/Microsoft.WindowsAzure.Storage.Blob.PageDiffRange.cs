
namespace Microsoft.WindowsAzure.Storage.Blob
{
public sealed class PageDiffRange : PageRange
{
    public bool IsClearedPageRange
    {
        get; internal set;
    }

    public PageDiffRange(long start, long end, bool isCleared)
      : base(start, end)
    {
        throw new System.NotImplementedException();
    }
}

}