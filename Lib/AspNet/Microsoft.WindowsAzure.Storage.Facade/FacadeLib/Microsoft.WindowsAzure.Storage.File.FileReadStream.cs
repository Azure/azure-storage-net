using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.File
{
internal sealed class FileReadStream : FileReadStreamBase
{

    internal FileReadStream(CloudFile file, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
      : base(file, accessCondition, options, operationContext)
    {
        throw new System.NotImplementedException();
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
}

}