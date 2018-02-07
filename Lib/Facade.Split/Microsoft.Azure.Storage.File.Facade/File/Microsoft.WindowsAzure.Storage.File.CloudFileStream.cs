using System.IO;
using System.Threading.Tasks;
namespace Microsoft.Azure.Storage.File
{
public abstract class CloudFileStream : Stream
{
    public abstract Task CommitAsync();
}

}