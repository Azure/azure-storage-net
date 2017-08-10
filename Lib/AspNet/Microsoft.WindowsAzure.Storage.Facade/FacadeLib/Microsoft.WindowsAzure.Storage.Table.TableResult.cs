
namespace Microsoft.WindowsAzure.Storage.Table
{
public sealed class TableResult
{
    public object Result
    {
        get; set;
    }

    public int HttpStatusCode
    {
        get; set;
    }

    public string Etag
    {
        get; set;
    }
}

}