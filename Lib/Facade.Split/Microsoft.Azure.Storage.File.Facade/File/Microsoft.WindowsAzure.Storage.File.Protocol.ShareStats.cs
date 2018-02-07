using System;
using System.Globalization;
using System.Xml.Linq;
namespace Microsoft.Azure.Storage.File.Protocol
{
public sealed class ShareStats
{
    private const string ShareStatsName = "ShareStats";
    private const string ShareUsageName = "ShareUsage";

    public int Usage
    {
        get; private set;
    }

    private ShareStats()
    {
        throw new System.NotImplementedException();
    }
    internal static ShareStats FromServiceXml(XDocument shareStatsDocument)
    {
        throw new System.NotImplementedException();
    }
}

}