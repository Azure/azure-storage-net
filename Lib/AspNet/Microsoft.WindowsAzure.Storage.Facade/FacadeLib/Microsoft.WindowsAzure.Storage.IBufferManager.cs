 
namespace Microsoft.WindowsAzure.Storage
{
public interface IBufferManager
{

    byte[] TakeBuffer(int bufferSize);

    int GetDefaultBufferSize();
}

}