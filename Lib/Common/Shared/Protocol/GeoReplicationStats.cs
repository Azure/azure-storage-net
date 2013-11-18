// -----------------------------------------------------------------------------------------
// <copyright file="GeoReplicationStats.cs" company="Microsoft">
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
    using System;
    using System.Globalization;
    using System.Xml.Linq;

    /// <summary>
    /// Class representing the geo-replication stats.
    /// </summary>
    public sealed class GeoReplicationStats
    {
        /// <summary>
        /// The name of the status XML element.
        /// </summary>
        private const string StatusName = "Status";

        /// <summary>
        /// The name of the last sync time XML element.
        /// </summary>
        private const string LastSyncTimeName = "LastSyncTime";

        /// <summary>
        /// Initializes a new instance of the GeoReplicationStats class.
        /// </summary>
        private GeoReplicationStats()
        {
        }

        /// <summary>
        /// Gets or sets the status of geo-replication.
        /// </summary>
        /// <value>The status of geo-replication.</value>
        public GeoReplicationStatus Status { get; private set; }

        /// <summary>
        /// Gets or sets the last synchronization time.
        /// </summary>
        /// <value>The last synchronization time.</value>
        /// <remarks>All primary writes preceding this value are guaranteed to be available for read operations. Primary writes following this point in time may or may not be available for reads.</remarks>
        public DateTimeOffset? LastSyncTime { get; private set; }

        /// <summary>
        /// Gets a <see cref="GeoReplicationStatus"/> from a string.
        /// </summary>
        /// <param name="geoReplicationStatus">The geo-replication status string.</param>
        /// <returns>A <see cref="GeoReplicationStatus"/> enumeration.</returns>
        /// <exception cref="System.ArgumentException">The string contains an unrecognized value.</exception>
        internal static GeoReplicationStatus GetGeoReplicationStatus(string geoReplicationStatus)
        {
            switch (geoReplicationStatus)
            {
                case Constants.GeoUnavailableValue:
                    return GeoReplicationStatus.Unavailable;

                case Constants.GeoLiveValue:
                    return GeoReplicationStatus.Live;

                case Constants.GeoBootstrapValue:
                    return GeoReplicationStatus.Bootstrap;

                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidGeoReplicationStatus, geoReplicationStatus), "geoReplicationStatus");
            }
        }

        /// <summary>
        /// Constructs a <c>GeoReplicationStats</c> object from an XML element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>A <c>GeoReplicationStats</c> object containing the properties in the element.</returns>
        internal static GeoReplicationStats ReadGeoReplicationStatsFromXml(XElement element)
        {
            string lastSyncTime = element.Element(LastSyncTimeName).Value;
            return new GeoReplicationStats()
            {
                Status = GeoReplicationStats.GetGeoReplicationStatus(element.Element(StatusName).Value),
                LastSyncTime = string.IsNullOrEmpty(lastSyncTime) ? (DateTimeOffset?)null : DateTimeOffset.Parse(lastSyncTime, CultureInfo.InvariantCulture),
            };
        }
    }
}
