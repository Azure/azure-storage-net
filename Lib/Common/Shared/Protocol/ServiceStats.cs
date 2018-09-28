// -----------------------------------------------------------------------------------------
// <copyright file="ServiceStats.cs" company="Microsoft">
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
    using System.Xml.Linq;

    /// <summary>
    /// Class representing a set of stats pertaining to a cloud storage service.
    /// </summary>
    public sealed class ServiceStats
    {
        /// <summary>
        /// The name of the root XML element.
        /// </summary>
        private const string StorageServiceStatsName = "StorageServiceStats";

        /// <summary>
        /// The name of the geo-replication XML element.
        /// </summary>
        private const string GeoReplicationName = "GeoReplication";

        /// <summary>
        /// Initializes a new instance of the ServiceStats class.
        /// </summary>
        private ServiceStats()
        {
        }

        /// <summary>
        /// Gets or sets the geo-replication stats.
        /// </summary>
        /// <value>The geo-replication stats.</value>
        public GeoReplicationStats GeoReplication { get; private set; }

        /// <summary>
        /// Constructs a <c>ServiceStats</c> object from an XML document received from the service.
        /// </summary>
        /// <param name="serviceStatsDocument">The XML document.</param>
        /// <returns>A <c>ServiceStats</c> object containing the properties in the XML document.</returns>
        internal static ServiceStats FromServiceXml(XDocument serviceStatsDocument)
        {
            XElement serviceStatsElement = serviceStatsDocument.Element(StorageServiceStatsName);

            return new ServiceStats()
            {
                GeoReplication = GeoReplicationStats.ReadGeoReplicationStatsFromXml(serviceStatsElement.Element(GeoReplicationName)),
            };
        }
    }
}
