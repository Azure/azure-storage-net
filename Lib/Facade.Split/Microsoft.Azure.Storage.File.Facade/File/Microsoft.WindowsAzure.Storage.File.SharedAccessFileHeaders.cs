using Microsoft.Azure.Storage.Core.Util;
namespace Microsoft.Azure.Storage.File
{
public sealed class SharedAccessFileHeaders
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

    public SharedAccessFileHeaders()
    {
        throw new System.NotImplementedException();
    }
    public SharedAccessFileHeaders(SharedAccessFileHeaders sharedAccessFileHeaders)
    {
        throw new System.NotImplementedException();
    }
}

}