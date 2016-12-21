using Microsoft.WindowsAzure.Storage.Core.Util;
namespace Microsoft.WindowsAzure.Storage.Blob
{
public sealed class SharedAccessBlobHeaders
{
    public string CacheControl
    {
        get; set;
    }

    public string ContentDisposition
    {
        get; set;
    }

    public string ContentEncoding
    {
        get; set;
    }

    public string ContentLanguage
    {
        get; set;
    }

    public string ContentType
    {
        get; set;
    }

    public SharedAccessBlobHeaders()
    {
        throw new System.NotImplementedException();
    }
    public SharedAccessBlobHeaders(SharedAccessBlobHeaders sharedAccessBlobHeaders)
    {
        throw new System.NotImplementedException();
    }
}

}