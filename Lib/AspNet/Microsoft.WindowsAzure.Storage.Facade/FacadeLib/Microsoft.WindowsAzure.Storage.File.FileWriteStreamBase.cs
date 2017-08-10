using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.IO;
namespace Microsoft.WindowsAzure.Storage.File
{
internal abstract class FileWriteStreamBase : CloudFileStream
{
    protected CloudFile file;
    protected long fileSize;
    protected bool newFile;
    protected long currentOffset;
    protected long currentFileOffset;
    protected int streamWriteSizeInBytes;
    protected MultiBufferMemoryStream internalBuffer;
    protected AccessCondition accessCondition;
    protected FileRequestOptions options;
    protected OperationContext operationContext;
    protected CounterEvent noPendingWritesEvent;
    protected MD5Wrapper fileMD5;
    protected MD5Wrapper rangeMD5;
    protected AsyncSemaphore parallelOperationSemaphore;
    protected volatile Exception lastException;
    protected volatile bool committed;
    protected bool disposed;

    public override bool CanRead
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public override bool CanSeek
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public override bool CanWrite
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public override long Length
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public override long Position
    {
        get
        {
            throw new System.NotImplementedException();
        }
        set
        {
            throw new System.NotImplementedException();
        }
    }

    protected FileWriteStreamBase(CloudFile file, long fileSize, bool createNew, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    protected long GetNewOffset(long offset, SeekOrigin origin)
    {
        throw new System.NotImplementedException();
    }
    public override void SetLength(long value)
    {
        throw new System.NotImplementedException();
    }
    protected override void Dispose(bool disposing)
    {
        throw new System.NotImplementedException();
    }
}

}