using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Core
{
internal class ReadLengthLimitingStream : Stream
{
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

    public ReadLengthLimitingStream(Stream wrappedStream, long length)
    {
        throw new System.NotImplementedException();
    }
    public override void SetLength(long value)
    {
        throw new System.NotImplementedException();
    }
    public override void Flush()
    {
        throw new System.NotImplementedException();
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new System.NotImplementedException();
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    protected override void Dispose(bool disposing)
    {
        throw new System.NotImplementedException();
    }
}

}