using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.File
{
internal sealed class FileReadStream : FileReadStreamBase
{
    public string ContentType
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal FileReadStream(CloudFile file, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
      : base(file, accessCondition, options, operationContext)
    {
        throw new System.NotImplementedException();
    }
    internal FileReadStream(FileReadStream otherStream)
      : this(otherStream.file, otherStream.accessCondition, otherStream.options, otherStream.operationContext)
    {
        throw new System.NotImplementedException();
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    public override  Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}

}