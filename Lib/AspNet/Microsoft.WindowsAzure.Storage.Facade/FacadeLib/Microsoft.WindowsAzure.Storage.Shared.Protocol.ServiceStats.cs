using System.Xml.Linq;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
public sealed class ServiceStats
{
    private const string StorageServiceStatsName = "StorageServiceStats";
    private const string GeoReplicationName = "GeoReplication";

    public GeoReplicationStats GeoReplication
    {
        get; private set;
    }

    private ServiceStats()
    {
        throw new System.NotImplementedException();
    }
    internal static ServiceStats FromServiceXml(XDocument serviceStatsDocument)
    {
        throw new System.NotImplementedException();
    }
}

}