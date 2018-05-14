using System.Xml.Linq;
namespace Microsoft.Azure.Storage.Shared.Protocol
{
public sealed class ServiceStats
{

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