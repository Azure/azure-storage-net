using Microsoft.Azure.Storage.Shared.Protocol;
namespace Microsoft.Azure.Storage.Blob.Protocol
{
internal sealed class BlobListingContext : ListingContext
{
    public string Delimiter
    {
        get; set;
    }

    public BlobListingDetails Details
    {
        get; set;
    }

    public BlobListingContext(string prefix, int? maxResults, string delimiter, BlobListingDetails details)
      : base(prefix, maxResults)
    {
        throw new System.NotImplementedException();
    }
}

}