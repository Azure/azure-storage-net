// -----------------------------------------------------------------------------------------
// <copyright file="ShareStats.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.File.Protocol
{
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Globalization;
    using System.Xml.Linq;

    /// <summary>
    /// Class representing a set of stats pertaining to a File Share.
    /// </summary>
    public sealed class ShareStats
    {
        /// <summary>
        /// The name of the root XML element.
        /// </summary>
        private const string ShareStatsName = "ShareStats";

        /// <summary>
        /// The name of the share usage XML element.
        /// </summary>
        private const string ShareUsageBytes = "ShareUsageBytes";

        /// <summary>
        /// Initializes a new instance of the ServiceStats class.
        /// </summary>
        private ShareStats()
        {
        }

        /// <summary>
        /// Gets the share usage in GB, rounded up.
        /// </summary>
        /// <value>The share usage, in GB.</value>
        public int Usage { get; private set; }

        /// <summary>
        /// Gets the share usage in bytes.
        /// </summary>
        /// <value>The share usage, in bytes.</value>
        public long UsageInBytes { get; private set; }

        /// <summary>
        /// Constructs a <c>ShareStats</c> object from an XML document received from the service.
        /// </summary>
        /// <param name="shareStatsDocument">The XML document.</param>
        /// <returns>A <c>ShareStats</c> object containing the properties in the XML document.</returns>
        internal static ShareStats FromServiceXml(XDocument shareStatsDocument)
        {
            XElement shareStatsElement = shareStatsDocument.Element(ShareStatsName);

            long usageInBytes = long.Parse(shareStatsElement.Element(ShareUsageBytes).Value, CultureInfo.InvariantCulture);
            int usage = (int)Math.Ceiling(usageInBytes / (double)Constants.GB);

            return new ShareStats()
            {
                Usage = usage,
                UsageInBytes = usageInBytes
            };
        }
    }
}
