using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System.IO;
using System.Xml.Linq;
namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
public sealed class FileServiceProperties
{
        internal ServiceProperties serviceProperties;

        public CorsProperties Cors
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

    public MetricsProperties HourMetrics
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

    public MetricsProperties MinuteMetrics
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

    public FileServiceProperties()
    {
        throw new System.NotImplementedException();
    }
    public FileServiceProperties(MetricsProperties hourMetrics = null, MetricsProperties minuteMetrics = null, CorsProperties cors = null)
    {
        throw new System.NotImplementedException();
    }

    internal static FileServiceProperties FromServiceXml(XDocument servicePropertiesDocument)
    {
        throw new System.NotImplementedException();
    }
    internal XDocument ToServiceXml()
    {
        throw new System.NotImplementedException();
    }
    internal void WriteServiceProperties(Stream outputStream)
    {
        throw new System.NotImplementedException();
    }
}

}