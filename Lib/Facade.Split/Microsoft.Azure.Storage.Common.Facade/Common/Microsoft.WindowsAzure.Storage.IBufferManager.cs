 
namespace Microsoft.Azure.Storage
{
public interface IBufferManager
{

    byte[] TakeBuffer(int bufferSize);

    int GetDefaultBufferSize();
}

}