using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
namespace Microsoft.Azure.Storage.Shared.Protocol
{
public sealed class ServiceProperties
{

    public LoggingProperties Logging
    {
        get; set;
    }

    public MetricsProperties HourMetrics
    {
        get; set;
    }

    public CorsProperties Cors
    {
        get; set;
    }

    public MetricsProperties MinuteMetrics
    {
        get; set;
    }

    public string DefaultServiceVersion
    {
        get; set;
    }

    public DeleteRetentionPolicy DeleteRetentionPolicy
    {
        get; set;
    }

    public ServiceProperties()
    {
        throw new System.NotImplementedException();
    }
    public ServiceProperties(LoggingProperties logging = null, MetricsProperties hourMetrics = null, MetricsProperties minuteMetrics = null, CorsProperties cors = null, DeleteRetentionPolicy deleteRetentionPolicy = null)
    {
        throw new System.NotImplementedException();
    }
    internal static ServiceProperties FromServiceXml(XDocument servicePropertiesDocument)
    {
        throw new System.NotImplementedException();
    }
    internal XDocument ToServiceXml()
    {
        throw new System.NotImplementedException();
    }
    private static XElement GenerateRetentionPolicyXml(int? retentionDays)
    {
        throw new System.NotImplementedException();
    }
    private static XElement GenerateMetricsXml(MetricsProperties metrics, string metricsName)
    {
        throw new System.NotImplementedException();
    }
    private static XElement GenerateLoggingXml(LoggingProperties logging)
    {
        throw new System.NotImplementedException();
    }
    private static XElement GenerateCorsXml(CorsProperties cors)
    {
        throw new System.NotImplementedException();
    }
    private static XElement GenerateDeleteRetentionPolicyXml(DeleteRetentionPolicy deleteRetentionPolicy)
    {
        throw new System.NotImplementedException();
    }
    private static LoggingProperties ReadLoggingPropertiesFromXml(XElement element)
    {
        throw new System.NotImplementedException();
    }
    internal static MetricsProperties ReadMetricsPropertiesFromXml(XElement element)
    {
        throw new System.NotImplementedException();
    }
    internal static CorsProperties ReadCorsPropertiesFromXml(XElement element)
    {
        throw new System.NotImplementedException();
    }
    internal static DeleteRetentionPolicy ReadDeleteRetentionPolicyFromXml(XElement element)
    {
        throw new System.NotImplementedException();
    }
    private static int? ReadRetentionPolicyFromXml(XElement element)
    {
        throw new System.NotImplementedException();
    }
    internal void WriteServiceProperties(Stream outputStream)
    {
        throw new System.NotImplementedException();
    }
}

}