using Microsoft.WindowsAzure.Storage.Core.Util;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
internal class ListingContext
{

    public string Prefix
    {
        get; set;
    }

    public int? MaxResults
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

    public string Marker
    {
        get; set;
    }

    public ListingContext(string prefix, int? maxResults)
    {
        throw new System.NotImplementedException();
    }
}

}