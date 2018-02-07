using System.IO;
namespace Microsoft.Azure.Storage.Core
{
public class SyncMemoryStream : MemoryStream
{
    public SyncMemoryStream()
    {
        throw new System.NotImplementedException();
    }
    public SyncMemoryStream(byte[] buffer)
      : base(buffer)
    {
        throw new System.NotImplementedException();
    }
    public SyncMemoryStream(byte[] buffer, int index)
      : base(buffer, index, buffer.Length - index)
    {
        throw new System.NotImplementedException();
    }
    public SyncMemoryStream(byte[] buffer, int index, int count)
      : base(buffer, index, count)
    {
        throw new System.NotImplementedException();
    }
}

}