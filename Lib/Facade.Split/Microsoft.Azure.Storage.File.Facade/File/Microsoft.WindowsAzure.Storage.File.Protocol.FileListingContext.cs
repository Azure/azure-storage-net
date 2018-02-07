using Microsoft.Azure.Storage.Shared.Protocol;
namespace Microsoft.Azure.Storage.File.Protocol
{
internal sealed class FileListingContext : ListingContext
{
    public FileListingContext(int? maxResults)
      : base((string) null, maxResults)
    {
        throw new System.NotImplementedException();
    }
}

}