using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Globalization;
using System.IO;
namespace Microsoft.Azure.Storage.Blob
{
internal abstract class BlobReadStreamBase : Stream
{
    protected CloudBlob blob;
    protected BlobProperties blobProperties;
    protected long currentOffset;
    protected MultiBufferMemoryStream internalBuffer;
    protected int streamMinimumReadSizeInBytes;
    protected AccessCondition accessCondition;
    protected BlobRequestOptions options;
    protected OperationContext operationContext;
    protected MD5Wrapper blobMD5;
    protected volatile Exception lastException;

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

    public override long Length
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    protected BlobReadStreamBase(CloudBlob blob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new System.NotImplementedException();
    }
    public override void SetLength(long value)
    {
        throw new System.NotImplementedException();
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    public override void Flush()
    {
        throw new System.NotImplementedException();
    }
    protected int ConsumeBuffer(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    protected int GetReadSize()
    {
        throw new System.NotImplementedException();
    }
    protected void VerifyBlobMD5(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    protected override void Dispose(bool disposing)
    {
        throw new System.NotImplementedException();
    }
}

}