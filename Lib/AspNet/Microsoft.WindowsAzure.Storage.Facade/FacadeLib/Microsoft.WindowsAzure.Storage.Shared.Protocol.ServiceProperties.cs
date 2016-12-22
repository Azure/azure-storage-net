using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
public sealed class ServiceProperties
{
    internal const string StorageServicePropertiesName = "StorageServiceProperties";
    internal const string LoggingName = "Logging";
    internal const string HourMetricsName = "HourMetrics";
    internal const string CorsName = "Cors";
    internal const string MinuteMetricsName = "MinuteMetrics";
    internal const string VersionName = "Version";
    internal const string DeleteName = "Delete";
    internal const string ReadName = "Read";
    internal const string WriteName = "Write";
    internal const string RetentionPolicyName = "RetentionPolicy";
    internal const string EnabledName = "Enabled";
    internal const string DaysName = "Days";
    internal const string IncludeApisName = "IncludeAPIs";
    internal const string DefaultServiceVersionName = "DefaultServiceVersion";
    internal const string CorsRuleName = "CorsRule";
    internal const string AllowedOriginsName = "AllowedOrigins";
    internal const string AllowedMethodsName = "AllowedMethods";
    internal const string MaxAgeInSecondsName = "MaxAgeInSeconds";
    internal const string ExposedHeadersName = "ExposedHeaders";
    internal const string AllowedHeadersName = "AllowedHeaders";

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

    public ServiceProperties()
    {
        throw new System.NotImplementedException();
    }
    public ServiceProperties(LoggingProperties logging = null, MetricsProperties hourMetrics = null, MetricsProperties minuteMetrics = null, CorsProperties cors = null)
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