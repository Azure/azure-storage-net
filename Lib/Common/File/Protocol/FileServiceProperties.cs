// -----------------------------------------------------------------------------------------
// <copyright file="FileServiceProperties.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// Class representing a set of properties pertaining to the Azure File service.
    /// </summary>
    public sealed class FileServiceProperties
    {
        internal ServiceProperties serviceProperties;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileServiceProperties"/> class.
        /// </summary>
        public FileServiceProperties()
        {
            this.serviceProperties = new ServiceProperties();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileServiceProperties"/> class.
        /// </summary>
        public FileServiceProperties(MetricsProperties hourMetrics = null, MetricsProperties minuteMetrics = null, CorsProperties cors = null)
        {
            this.serviceProperties = new ServiceProperties(null, hourMetrics, minuteMetrics, cors);
        }

        /// <summary>
        /// Gets or sets the Cross Origin Resource Sharing (CORS) properties for the File service.
        /// </summary>
        /// <value>The CORS properties.</value>
        public CorsProperties Cors
        {
            get 
            {
                return this.serviceProperties.Cors;
            }

            set
            {
                this.serviceProperties.Cors = value;
            }
        }

        /// <summary>
        /// Gets or sets the hour metrics properties.
        /// </summary>
        /// <value>The metrics properties.</value>
        public MetricsProperties HourMetrics
        {
            get
            {
                return this.serviceProperties.HourMetrics;
            }
            set
            {
                this.serviceProperties.HourMetrics = value;
            }
        }

        /// <summary>
        /// Gets or sets the minutes metrics properties.
        /// </summary>
        /// <value>The metrics properties.</value>
        public MetricsProperties MinuteMetrics
        {
            get
            {
                return this.serviceProperties.MinuteMetrics;
            }
            set
            {
                this.serviceProperties.MinuteMetrics = value;
            }
        }

        /// <summary>
        /// Constructs a <c>ServiceProperties</c> object from an XML document received from the service.
        /// </summary>
        /// <param name="servicePropertiesDocument">The XML document.</param>
        /// <returns>A <c>ServiceProperties</c> object containing the properties in the XML document.</returns>
        internal static FileServiceProperties FromServiceXml(XDocument servicePropertiesDocument)
        {
            XElement servicePropertiesElement =
                servicePropertiesDocument.Element(ServiceProperties.StorageServicePropertiesName);

            FileServiceProperties properties = new FileServiceProperties
            {
                Cors = ServiceProperties.ReadCorsPropertiesFromXml(servicePropertiesElement.Element(ServiceProperties.CorsName)),
                HourMetrics = ServiceProperties.ReadMetricsPropertiesFromXml(
                    servicePropertiesElement.Element(ServiceProperties.HourMetricsName)),
                MinuteMetrics = ServiceProperties.ReadMetricsPropertiesFromXml(
                    servicePropertiesElement.Element(ServiceProperties.MinuteMetricsName))
            };

            return properties;
        }

        /// <summary>
        /// Converts these properties into XML for communicating with the service.
        /// </summary>
        /// <returns>An XML document containing the service properties.</returns>
        internal XDocument ToServiceXml()
        {
            return this.serviceProperties.ToServiceXml();
        }

        /// <summary>
        /// Writes service properties to a stream, formatted in XML.
        /// </summary>
        /// <param name="outputStream">The stream to which the formatted properties are to be written.</param>
        internal void WriteServiceProperties(Stream outputStream)
        {
            this.serviceProperties.WriteServiceProperties(outputStream);
        }
    }
}
