// -----------------------------------------------------------------------------------------
// <copyright file="CapacityEntity.cs" company="Microsoft">
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
// ----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Analytics
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an entity in a storage analytics capacity table.
    /// </summary>
    public class CapacityEntity : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityEntity"/> class.
        /// </summary>
        public CapacityEntity()
        {
        }

        /// <summary>
        /// Gets the capacity entity's timestamp in UTC, representing the start time for that log entry.
        /// </summary>
        /// <value>A string containing a timestamp in UTC.</value>
        public DateTimeOffset Time
        {
            get
            {
                return DateTimeOffset.ParseExact(this.PartitionKey, "yyyyMMdd'T'HHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            }
        }

        /// <summary>
        /// Gets or sets the Capacity property for capacity entity, which indicates the quantity of Blob storage used by the storage account.
        /// </summary>
        /// <value>A long containing the quantity of Blob storage used by the storage account, per this capacity entity.</value>
        public long Capacity { get; set; }

        /// <summary>
        /// Gets or sets the ContainerCount property for the capacity entity, which indicates the number of blob containers in the storage account.
        /// </summary>
        /// <value>A long containing the number of blob containers in the storage account, per this capacity entity.</value>
        public long ContainerCount { get; set; }

        /// <summary>
        /// Gets or sets the ObjectCount property for the capacity entity, which indicates the number of committed and uncommitted blobs in the storage account.
        /// </summary>
        /// <value>A long containing the number of committed and uncommitted blobs in the storage account, per this capacity entity.</value>
        public long ObjectCount { get; set; }
    }
}
