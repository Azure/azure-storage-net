using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.Azure.Storage.Core
{
public class MultiBufferMemoryStream : Stream
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

    public MultiBufferMemoryStream(IBufferManager bufferManager, int bufferSize = 65536)
    {
        throw new System.NotImplementedException();
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
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
    public Task FastCopyToAsync(Stream destination, DateTime? expiryTime)
    {
        throw new System.NotImplementedException();
    }
    public string ComputeMD5Hash()
    {
        throw new System.NotImplementedException();
    }
    private void Reserve(long requiredSize)
    {
        throw new System.NotImplementedException();
    }
    private void AddBlock()
    {
        throw new System.NotImplementedException();
    }
    private int ReadInternal(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    private void WriteInternal(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }
    private void AdvancePosition(ref int offset, ref int leftToProcess, int amountProcessed)
    {
        throw new System.NotImplementedException();
    }
    private void AdvancePosition(ref long leftToProcess, int amountProcessed)
    {
        throw new System.NotImplementedException();
    }
    private ArraySegment<byte> GetCurrentBlock()
    {
        throw new System.NotImplementedException();
    }
    protected override void Dispose(bool disposing)
    {
        throw new System.NotImplementedException();
    }
    private class CopyState
    {
        public Stream Destination
        {
            get; set;
        }

        public DateTime? ExpiryTime
        {
            get; set;
        }
    }
}

}