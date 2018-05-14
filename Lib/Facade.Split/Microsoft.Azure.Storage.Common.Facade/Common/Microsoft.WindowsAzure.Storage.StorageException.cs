using Microsoft.Azure.Storage.Core.Util;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage
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