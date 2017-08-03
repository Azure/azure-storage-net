using System;
using System.Globalization;
using System.Xml.Linq;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
public sealed class GeoReplicationStats
{
    private const string StatusName = "Status";
    private const string LastSyncTimeName = "LastSyncTime";

    public GeoReplicationStatus Status
    {
        get; private set;
    }

    public DateTimeOffset? LastSyncTime
    {
        get; private set;
    }

    private GeoReplicationStats()
    {
        throw new System.NotImplementedException();
    }
    internal static GeoReplicationStatus GetGeoReplicationStatus(string geoReplicationStatus)
    {
        throw new System.NotImplementedException();
    }
    internal static GeoReplicationStats ReadGeoReplicationStatsFromXml(XElement element)
    {
        throw new System.NotImplementedException();
    }
}

}