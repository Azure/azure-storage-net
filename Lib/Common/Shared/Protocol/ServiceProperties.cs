// -----------------------------------------------------------------------------------------
// <copyright file="ServiceProperties.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// Class representing a set of properties pertaining to a cloud storage service.
    /// </summary>
    public sealed class ServiceProperties
    {
        /// <summary>
        /// The name of the root XML element.
        /// </summary>
        internal const string StorageServicePropertiesName = "StorageServiceProperties";

        /// <summary>
        /// The name of the logging XML element.
        /// </summary>
        internal const string LoggingName = "Logging";

        /// <summary>
        /// The name of the metrics XML element.
        /// </summary>
        internal const string HourMetricsName = "HourMetrics";

        /// <summary>
        /// The name of the CORS XML element.
        /// </summary>
        internal const string CorsName = "Cors";

        /// <summary>
        /// The name of the minute metrics XML element.
        /// </summary>
        internal const string MinuteMetricsName = "MinuteMetrics";

        /// <summary>
        /// The name of the version XML element.
        /// </summary>
        internal const string VersionName = "Version";

        /// <summary>
        /// The name of the delete operation XML element.
        /// </summary>
        internal const string DeleteName = "Delete";

        /// <summary>
        /// The name of the read operation XML element.
        /// </summary>
        internal const string ReadName = "Read";

        /// <summary>
        /// The name of the write operation XML element.
        /// </summary>
        internal const string WriteName = "Write";

        /// <summary>
        /// The name of the retention policy XML element.
        /// </summary>
        internal const string RetentionPolicyName = "RetentionPolicy";

        /// <summary>
        /// The name of the enabled XML element.
        /// </summary>
        internal const string EnabledName = "Enabled";

        /// <summary>
        /// The name of the days XML element.
        /// </summary>
        internal const string DaysName = "Days";

        /// <summary>
        /// The name of the include APIs XML element.
        /// </summary>
        internal const string IncludeApisName = "IncludeAPIs";

        /// <summary>
        /// The name of the default service version XML element.
        /// </summary>
        internal const string DefaultServiceVersionName = "DefaultServiceVersion";

        /// <summary>
        /// The name of the CORS Rule XML element.
        /// </summary>
        internal const string CorsRuleName = "CorsRule";

        /// <summary>
        /// The name of the Allowed Origin XML element.
        /// </summary>
        internal const string AllowedOriginsName = "AllowedOrigins";

        /// <summary>
        /// The name of the Allowed Method XML element.
        /// </summary>
        internal const string AllowedMethodsName = "AllowedMethods";

        /// <summary>
        /// The name of the Maximum Age XML element.
        /// </summary>
        internal const string MaxAgeInSecondsName = "MaxAgeInSeconds";

        /// <summary>
        /// The name of the Exposed Headers XML element.
        /// </summary>
        internal const string ExposedHeadersName = "ExposedHeaders";

        /// <summary>
        /// The name of the Allowed Headers XML element.
        /// </summary>
        internal const string AllowedHeadersName = "AllowedHeaders";

        /// <summary>
        /// Initializes a new instance of the ServiceProperties class.
        /// </summary>
        public ServiceProperties()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ServiceProperties class.
        /// </summary>
        public ServiceProperties(LoggingProperties logging = null, MetricsProperties hourMetrics = null, MetricsProperties minuteMetrics = null, CorsProperties cors = null)
        {
            this.Logging = logging;
            this.HourMetrics = hourMetrics;
            this.MinuteMetrics = minuteMetrics;
            this.Cors = cors;
        }

        /// <summary>
        /// Gets or sets the logging properties.
        /// </summary>
        /// <value>The logging properties.</value>
        public LoggingProperties Logging
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the hour metrics properties.
        /// </summary>
        /// <value>The metrics properties.</value>
        public MetricsProperties HourMetrics
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Cross Origin Resource Sharing (CORS) properties.
        /// </summary>
        /// <value>The CORS properties.</value>
        public CorsProperties Cors
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the minute metrics properties.
        /// </summary>
        /// <value>The minute metrics properties.</value>
        public MetricsProperties MinuteMetrics
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default service version.
        /// </summary>
        /// <value>The default service version identifier.</value>
        public string DefaultServiceVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Constructs a <c>ServiceProperties</c> object from an XML document received from the service.
        /// </summary>
        /// <param name="servicePropertiesDocument">The XML document.</param>
        /// <returns>A <c>ServiceProperties</c> object containing the properties in the XML document.</returns>
        internal static ServiceProperties FromServiceXml(XDocument servicePropertiesDocument)
        {
            XElement servicePropertiesElement = servicePropertiesDocument.Element(StorageServicePropertiesName);
            ServiceProperties properties = new ServiceProperties
            {
                Logging = ReadLoggingPropertiesFromXml(servicePropertiesElement.Element(LoggingName)),
                HourMetrics = ReadMetricsPropertiesFromXml(servicePropertiesElement.Element(HourMetricsName)),
                MinuteMetrics = ReadMetricsPropertiesFromXml(servicePropertiesElement.Element(MinuteMetricsName)),
                Cors = ReadCorsPropertiesFromXml(servicePropertiesElement.Element(CorsName))
            };

            XElement defaultServiceVersionXml = servicePropertiesElement.Element(DefaultServiceVersionName);
            if (defaultServiceVersionXml != null)
            {
                properties.DefaultServiceVersion = defaultServiceVersionXml.Value;
            }

            return properties;
        }

        /// <summary>
        /// Converts these properties into XML for communicating with the service.
        /// </summary>
        /// <returns>An XML document containing the service properties.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "SetServiceProperties", Justification = "API name is properly spelled")]
        internal XDocument ToServiceXml()
        {
            if (this.Logging == null && this.HourMetrics == null && this.MinuteMetrics == null && this.Cors == null && this.DefaultServiceVersion == null)
            {
                throw new InvalidOperationException(SR.SetServicePropertiesRequiresNonNullSettings);
            }

            XElement storageServiceElement = new XElement(StorageServicePropertiesName);

            if (this.Logging != null)
            {
                storageServiceElement.Add(GenerateLoggingXml(this.Logging));
            }

            if (this.HourMetrics != null)
            {
                storageServiceElement.Add(GenerateMetricsXml(this.HourMetrics, HourMetricsName));
            }

            if (this.MinuteMetrics != null)
            {
                storageServiceElement.Add(GenerateMetricsXml(this.MinuteMetrics, MinuteMetricsName));
            }

            if (this.Cors != null)
            {
                storageServiceElement.Add(GenerateCorsXml(this.Cors));
            }

            if (this.DefaultServiceVersion != null)
            {
                storageServiceElement.Add(new XElement(DefaultServiceVersionName, this.DefaultServiceVersion));
            }

            return new XDocument(storageServiceElement);
        }

        /// <summary>
        /// Generates XML representing the given retention policy.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain, or <c>null</c> if the policy is disabled.</param>
        /// <returns>An XML retention policy element.</returns>
        private static XElement GenerateRetentionPolicyXml(int? retentionDays)
        {
            bool enabled = retentionDays != null;
            XElement xml = new XElement(RetentionPolicyName, new XElement(EnabledName, enabled));

            if (enabled)
            {
                xml.Add(new XElement(DaysName, (int)retentionDays));
            }

            return xml;
        }

        /// <summary>
        /// Generates XML representing the given metrics properties.
        /// </summary>
        /// <param name="metrics">The metrics properties.</param>
        /// <param name="metricsName">The XML name for these metrics.</param>
        /// <returns>An XML metrics element.</returns>
        private static XElement GenerateMetricsXml(MetricsProperties metrics, string metricsName)
        {
            if (!Enum.IsDefined(typeof(MetricsLevel), metrics.MetricsLevel))
            {
                throw new InvalidOperationException(SR.InvalidMetricsLevel);
            }

            if (string.IsNullOrEmpty(metrics.Version))
            {
                throw new InvalidOperationException(SR.MetricVersionNull);
            }

            bool enabled = metrics.MetricsLevel != MetricsLevel.None;

            XElement xml = new XElement(
                metricsName,
                new XElement(VersionName, metrics.Version),
                new XElement(EnabledName, enabled),
                GenerateRetentionPolicyXml(metrics.RetentionDays));

            if (enabled)
            {
                xml.Add(new XElement(IncludeApisName, metrics.MetricsLevel == MetricsLevel.ServiceAndApi));
            }

            return xml;
        }

        /// <summary>
        /// Generates XML representing the given logging properties.
        /// </summary>
        /// <param name="logging">The logging properties.</param>
        /// <returns>An XML logging element.</returns>
        private static XElement GenerateLoggingXml(LoggingProperties logging)
        {
            if ((LoggingOperations.All & logging.LoggingOperations) != logging.LoggingOperations)
            {
                throw new InvalidOperationException(SR.InvalidLoggingLevel);
            }

            if (string.IsNullOrEmpty(logging.Version))
            {
                throw new InvalidOperationException(SR.LoggingVersionNull);
            }

            return new XElement(
                LoggingName,
                new XElement(VersionName, logging.Version),
                new XElement(DeleteName, (logging.LoggingOperations & LoggingOperations.Delete) != 0),
                new XElement(ReadName, (logging.LoggingOperations & LoggingOperations.Read) != 0),
                new XElement(WriteName, (logging.LoggingOperations & LoggingOperations.Write) != 0),
                GenerateRetentionPolicyXml(logging.RetentionDays));
        }

        /// <summary>
        /// Generates XML representing the given CORS properties.
        /// </summary>
        /// <param name="cors">The CORS properties.</param>
        /// <returns>An XML logging element.</returns>
        private static XElement GenerateCorsXml(CorsProperties cors)
        {
            CommonUtility.AssertNotNull("cors", cors);

            IList<CorsRule> corsRules = cors.CorsRules;

            XElement ret = new XElement(CorsName);

            foreach (CorsRule rule in corsRules)
            {
                if (rule.AllowedOrigins.Count < 1 || rule.AllowedMethods == CorsHttpMethods.None || rule.MaxAgeInSeconds < 0)
                {
                    throw new InvalidOperationException(SR.InvalidCorsRule);
                }

                XElement ruleElement = new XElement(
                    CorsRuleName,
                    new XElement(AllowedOriginsName, string.Join(",", rule.AllowedOrigins.ToArray())),
                    new XElement(AllowedMethodsName, rule.AllowedMethods.ToString().Replace(" ", string.Empty).ToUpperInvariant()),
                    new XElement(ExposedHeadersName, string.Join(",", rule.ExposedHeaders.ToArray())),
                    new XElement(AllowedHeadersName, string.Join(",", rule.AllowedHeaders.ToArray())),
                    new XElement(MaxAgeInSecondsName, rule.MaxAgeInSeconds));

                ret.Add(ruleElement);
            }

            return ret;
        }

        /// <summary>
        /// Constructs a <c>LoggingProperties</c> object from an XML element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>A <c>LoggingProperties</c> object containing the properties in the element.</returns>
        private static LoggingProperties ReadLoggingPropertiesFromXml(XElement element)
        {
            if (element == null)
            {
                return null;
            }

            LoggingOperations state = LoggingOperations.None;

            if (bool.Parse(element.Element(DeleteName).Value))
            {
                state |= LoggingOperations.Delete;
            }

            if (bool.Parse(element.Element(ReadName).Value))
            {
                state |= LoggingOperations.Read;
            }

            if (bool.Parse(element.Element(WriteName).Value))
            {
                state |= LoggingOperations.Write;
            }

            return new LoggingProperties
            {
                Version = element.Element(VersionName).Value,
                LoggingOperations = state,
                RetentionDays = ReadRetentionPolicyFromXml(element.Element(RetentionPolicyName))
            };
        }

        /// <summary>
        /// Constructs a <c>MetricsProperties</c> object from an XML element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>A <c>MetricsProperties</c> object containing the properties in the element.</returns>
        internal static MetricsProperties ReadMetricsPropertiesFromXml(XElement element)
        {
            if (element == null)
            {
                return null;
            }
            
            MetricsLevel state = MetricsLevel.None;

            if (bool.Parse(element.Element(EnabledName).Value))
            {
                state = MetricsLevel.Service;

                if (bool.Parse(element.Element(IncludeApisName).Value))
                {
                    state = MetricsLevel.ServiceAndApi;
                }
            }

            return new MetricsProperties
            {
                Version = element.Element(VersionName).Value,
                MetricsLevel = state,
                RetentionDays = ReadRetentionPolicyFromXml(element.Element(RetentionPolicyName))
            };
        }

        /// <summary>
        /// Constructs a <c>CorsProperties</c> object from an XML element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>A <c>CorsProperties</c> object containing the properties in the element.</returns>
        internal static CorsProperties ReadCorsPropertiesFromXml(XElement element)
        {
            if (element == null)
            {
                return null;
            }

            CorsProperties ret = new CorsProperties();

            IEnumerable<XElement> corsRules = element.Descendants(CorsRuleName);

            ret.CorsRules =
                corsRules.Select(
                    rule =>
                    new CorsRule
                    {
                        AllowedOrigins = rule.Element(AllowedOriginsName).Value.Split(',').ToList(),
                        AllowedMethods =
                            (CorsHttpMethods)
                            Enum.Parse(
                                typeof(CorsHttpMethods), rule.Element(AllowedMethodsName).Value, true),
                        AllowedHeaders =
                            rule.Element(AllowedHeadersName)
                                .Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .ToList(),
                        ExposedHeaders =
                            rule.Element(ExposedHeadersName)
                                .Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .ToList(),
                        MaxAgeInSeconds =
                            int.Parse(
                                rule.Element(MaxAgeInSecondsName).Value, CultureInfo.InvariantCulture)
                    }).ToList();

            return ret;
        }

        /// <summary>
        /// Constructs a retention policy (number of days) from an XML element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The number of days to retain, or <c>null</c> if retention is disabled.</returns>
        private static int? ReadRetentionPolicyFromXml(XElement element)
        {
            if (!bool.Parse(element.Element(EnabledName).Value))
            {
                return null;
            }

            return int.Parse(element.Element(DaysName).Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Writes service properties to a stream, formatted in XML.
        /// </summary>
        /// <param name="outputStream">The stream to which the formatted properties are to be written.</param>
        internal void WriteServiceProperties(Stream outputStream)
        {
            XDocument propertiesDocument = this.ToServiceXml();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.NewLineHandling = NewLineHandling.Entitize;

            using (XmlWriter writer = XmlWriter.Create(outputStream, settings))
            {
                propertiesDocument.Save(writer);
            }
        }
    }
}
