using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
namespace Microsoft.Azure.Storage.Blob
{
internal abstract class BlobWriteStreamBase : CloudBlobStream
{

    protected CloudBlob Blob
    {
        get; private set;
    }

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

    private BlobWriteStreamBase(CloudBlobClient serviceClient, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    protected BlobWriteStreamBase(CloudBlockBlob blockBlob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
      : this(blockBlob.ServiceClient, accessCondition, options, operationContext)
    {
        throw new System.NotImplementedException();
    }
    protected BlobWriteStreamBase(CloudPageBlob pageBlob, long pageBlobSize, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
      : this(pageBlob.ServiceClient, accessCondition, options, operationContext)
    {
        throw new System.NotImplementedException();
    }
    protected BlobWriteStreamBase(CloudAppendBlob appendBlob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
      : this(appendBlob.ServiceClient, accessCondition, options, operationContext)
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
    protected string GetCurrentBlockId()
    {
        throw new System.NotImplementedException();
    }
    protected override void Dispose(bool disposing)
    {
        throw new System.NotImplementedException();
    }
}

}