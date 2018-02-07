using Microsoft.Azure.Storage.Shared.Protocol;
namespace Microsoft.Azure.Storage.Queue.Protocol
{
internal sealed class QueueListingContext : ListingContext
{
    public QueueListingDetails Include
    {
        get; set;
    }

    public QueueListingContext(string prefix, int? maxResults, QueueListingDetails include)
      : base(prefix, maxResults)
    {
        throw new System.NotImplementedException();
    }
}

}