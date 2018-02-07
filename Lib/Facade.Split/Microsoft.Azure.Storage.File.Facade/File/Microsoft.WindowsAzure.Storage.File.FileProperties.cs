using Microsoft.Azure.Storage.Core.Util;
using System;
namespace Microsoft.Azure.Storage.File
{
public sealed class FileProperties
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

    public long Length
    {
        get; internal set;
    }

    public string ContentMD5
    {
        get; set;
    }

    public string ContentType
    {
        get; set;
    }

    public string ETag
    {
        get; internal set;
    }

    public DateTimeOffset? LastModified
    {
        get; internal set;
    }

    public bool IsServerEncrypted
    {
        get; internal set;
    }

    public FileProperties()
    {
        throw new System.NotImplementedException();
    }
    public FileProperties(FileProperties other)
    {
        throw new System.NotImplementedException();
    }
}

}