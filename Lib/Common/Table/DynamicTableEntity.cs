// -----------------------------------------------------------------------------------------
// <copyright file="DynamicTableEntity.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A <see cref="ITableEntity"/> type which allows callers direct access to the property map of the entity. 
    /// This class eliminates the use of reflection for serialization and deserialization.
    /// </summary>    
    public sealed class DynamicTableEntity : ITableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicTableEntity"/> class.
        /// </summary>
        public DynamicTableEntity()
        {
            this.Properties = new Dictionary<string, EntityProperty>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicTableEntity"/> class with the specified partition key and row key.
        /// </summary>
        /// <param name="partitionKey">A string containing the partition key value for the entity.</param>
        /// <param name="rowKey">A string containing the row key value for the entity.</param>
        public DynamicTableEntity(string partitionKey, string rowKey)
            : this(partitionKey, rowKey, DateTimeOffset.MinValue, null /* timestamp */, new Dictionary<string, EntityProperty>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicTableEntity"/> class with the entity's partition key, row key, ETag (if available/required), and properties.
        /// </summary>
        /// <param name="partitionKey">A string containing the partition key value for the entity.</param>
        /// <param name="rowKey">A string containing the row key value for the entity.</param>
        /// <param name="etag">A string containing the ETag for the entity.</param>
        /// <param name="properties">An <see cref="IDictionary{TKey,TValue}"/> object containing the entity's properties, indexed by property name.</param>
        public DynamicTableEntity(string partitionKey, string rowKey, string etag, IDictionary<string, EntityProperty> properties)
            : this(partitionKey, rowKey, DateTimeOffset.MinValue, etag, properties)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicTableEntity"/> class with the entity's partition key, row key, timestamp, ETag (if available/required), and properties.
        /// </summary>
        /// <param name="partitionKey">A string containing the partition key value for the entity.</param>
        /// <param name="rowKey">A string containing the row key value for the entity.</param>
        /// <param name="timestamp">A <see cref="DateTimeOffset"/> value containing the timestamp for this entity.</param>
        /// <param name="etag">A string containing the ETag for the entity.</param>
        /// <param name="properties">An <see cref="IDictionary{TKey,TValue}"/> object containing the entity's properties, indexed by property name.</param>
        internal DynamicTableEntity(string partitionKey, string rowKey, DateTimeOffset timestamp, string etag, IDictionary<string, EntityProperty> properties)
        {
            CommonUtility.AssertNotNull("properties", properties);

            // Store the information about this entity.  Make a copy of
            // the properties list, in case the caller decides to reuse
            // the list.
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
            this.Timestamp = timestamp;
            this.ETag = etag;

            this.Properties = properties;
        }

        /// <summary>
        /// Gets or sets the properties in the table entity, indexed by property name.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> object containing the entity's properties.</value>
        public IDictionary<string, EntityProperty> Properties { get; set; }

        /// <summary>
        /// Gets or sets the entity's partition key.
        /// </summary>
        /// <value>A string containing the partition key value for the entity.</value>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the entity's row key.
        /// </summary>
        /// <value>A string containing the row key value for the entity.</value>
        public string RowKey { get; set; }

        /// <summary>
        /// Gets or sets the entity's timestamp.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> containing the timestamp for the entity.</value>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the entity's current ETag.
        /// </summary>
        /// <value>A string containing the ETag for the entity.</value>
        /// <remarks>Set this value to '*' to blindly overwrite an entity as part of an update operation.</remarks>
        public string ETag { get; set; }

#if !(WINDOWS_RT ||  NETCORE)
        /// <summary>
        /// Gets or sets the entity's property, given the name of the property.
        /// </summary>
        /// <param name="key">A string containing the name of the property.</param>
        /// <returns>An <see cref="EntityProperty"/> object.</returns>
        public EntityProperty this[string key]
        {
            get { return this.Properties[key]; }
            set { this.Properties[key] = value; }
        }
#endif

        /// <summary>
        /// Deserializes this <see cref="DynamicTableEntity"/> instance using the specified <see cref="IDictionary{TKey,TValue}"/> of property names to values of type <see cref="EntityProperty"/>.
        /// </summary>
        /// <param name="properties">A collection containing the <see cref="IDictionary{TKey,TValue}"/> of string property names mapped to values of type <see cref="EntityProperty"/> to store in this <see cref="DynamicTableEntity"/> instance.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>The properties dictionary passed to this API is stored internally as a reference, not a copy.</remarks>
        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            this.Properties = properties;
        }

        /// <summary>
        /// Serializes the <see cref="IDictionary{TKey,TValue}"/> of property names mapped to values of type <see cref="EntityProperty"/> from this <see cref="DynamicTableEntity"/> instance.
        /// </summary>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An <see cref="IDictionary{TKey,TValue}"/> object containing the map of string property names to values of type <see cref="EntityProperty"/> stored in this <see cref="DynamicTableEntity"/> instance.</returns>
        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return this.Properties;
        }
    }
}