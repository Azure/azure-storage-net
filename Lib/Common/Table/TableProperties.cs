// -----------------------------------------------------------------------------------------
// <copyright file="TableProperties.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table
{
    /// <summary>
    /// The table properties.
    /// </summary>
    public sealed class TableProperties
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableProperties"/> class.
        /// </summary>
        public TableProperties()
        {
            this.TableStatus = TableStatus.Unspecified;
        }

        /// <summary>
        /// Gets the <see cref="TableStatus"/>.
        /// </summary>
        /// <remarks>This is only supported for premium tables</remarks>
        /// <value></value>
        public TableStatus TableStatus { get; internal set; }

        /// <summary>
        /// Gets the provisioned IOPS for the table. Value indicates the IOPS for which the table was provisioned last.
        /// </summary>
        /// <remarks>This is only supported for premium tables.</remarks>
        /// <value>The requested IOPS for the table. Value indicates the IOPS for which the table was provisioned last.</value>
        public int? ProvisionedIops { get; internal set; }

        /// <summary>
        /// Gets or sets the requested IOPS for the table.
        /// </summary>
        /// <remarks>This is only supported for premium tables.</remarks>
        /// <value>The requested IOPS for the table.</value>
        public int? RequestedIops { get; set; }
    }
}