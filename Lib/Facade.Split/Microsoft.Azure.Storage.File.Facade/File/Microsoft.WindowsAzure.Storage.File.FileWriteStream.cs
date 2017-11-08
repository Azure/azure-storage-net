using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.File
{
internal sealed class FileWriteStream : FileWriteStreamBase
{
    internal FileWriteStream(CloudFile file, long fileSize, bool createNew, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
: base(file, fileSize, createNew, accessCondition, options, operationContext)
    {
        throw new System.NotImplementedException();
    }

        public override long Seek(long offset, SeekOrigin origin)
    {
        throw new System.NotImplementedException();
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public override void Flush()
    {
        throw new System.NotImplementedException();
    }
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    protected override void Dispose(bool disposing)
    {
        throw new System.NotImplementedException();
    }
    public override  Task CommitAsync()
    {
        throw new System.NotImplementedException();
    }
    private  Task DispatchWriteAsync()
    {
        throw new System.NotImplementedException();
    }
    private  Task WriteRangeAsync(Stream rangeData, long offset, string contentMD5)
    {
        throw new System.NotImplementedException();
    }
}

}