using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Blob
{
internal sealed class BlobReadStream : BlobReadStreamBase
{

    internal BlobReadStream(CloudBlob blob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
      : base(blob, accessCondition, options, operationContext)
    {
        throw new System.NotImplementedException();
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
}

}