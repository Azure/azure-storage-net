using Microsoft.Azure.Storage.Blob.Protocol;
using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.Azure.Storage.Blob
{
internal sealed class BlobWriteStream : BlobWriteStreamBase
{

    internal BlobWriteStream(CloudPageBlob pageBlob, long pageBlobSize, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
      : base(pageBlob, pageBlobSize, createNew, accessCondition, options, operationContext)
    {
        throw new System.NotImplementedException();
    }
    internal BlobWriteStream(CloudAppendBlob appendBlob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
      : base(appendBlob, accessCondition, options, operationContext)
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
    public override void Flush()
    {
        throw new System.NotImplementedException();
    }
    protected override void Dispose(bool disposing)
    {
        throw new System.NotImplementedException();
    }
}

}