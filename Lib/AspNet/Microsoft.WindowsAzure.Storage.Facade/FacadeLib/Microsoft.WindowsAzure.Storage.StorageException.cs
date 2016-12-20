using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.IO;
using System.Net;
using System.Text;
namespace Microsoft.WindowsAzure.Storage
{
public class StorageException : Exception
{
    public RequestResult RequestInformation
    {
        get; private set;
    }

    internal bool IsRetryable
    {
        get; set;
    }

    public StorageException()
      : this((RequestResult) null, (string) null, (Exception) null)
    {
        throw new System.NotImplementedException();
    }
    public StorageException(string message)
      : this((RequestResult) null, message, (Exception) null)
    {
        throw new System.NotImplementedException();
    }
    public StorageException(string message, Exception innerException)
      : this((RequestResult) null, message, innerException)
    {
        throw new System.NotImplementedException();
    }
    public StorageException(RequestResult res, string message, Exception inner)
      : base(message, inner)
    {
        throw new System.NotImplementedException();
    }
    public static StorageException TranslateException(Exception ex, RequestResult reqResult)
    {
        throw new System.NotImplementedException();
    }
    public static StorageException TranslateException(Exception ex, RequestResult reqResult, Func<Stream, StorageExtendedErrorInformation> parseError)
    {
        throw new System.NotImplementedException();
    }
    internal static StorageException TranslateExceptionWithPreBufferedStream(Exception ex, RequestResult reqResult, Func<Stream, StorageExtendedErrorInformation> parseError, Stream responseStream)
    {
        throw new System.NotImplementedException();
    }
    private static StorageException CoreTranslate(Exception ex, RequestResult reqResult, ref Func<Stream, StorageExtendedErrorInformation> parseError)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
}

}