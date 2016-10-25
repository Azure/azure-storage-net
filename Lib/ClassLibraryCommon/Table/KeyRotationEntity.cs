// -----------------------------------------------------------------------------------------
// <copyright file="KeyRotationEntity.cs" company="Microsoft">
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
    using Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Shared.Protocol;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An entity used for key rotation
    /// </summary>    
    public class KeyRotationEntity
    {
        internal KeyRotationEntity(DynamicTableEntity dte)
        {
            CommonUtility.AssertNotNull("properties", dte.Properties);

            // Store the information about this entity.  Make a copy of the properties list, in case the caller decides to reuse the list.
            this.PartitionKey = dte.PartitionKey;
            this.RowKey = dte.RowKey;
            this.Timestamp = dte.Timestamp;
            this.ETag = dte.ETag;
            if (!dte.Properties.ContainsKey(Constants.EncryptionConstants.TableEncryptionKeyDetails))
            {
                // This should only be possible if RequireEncryption is true, otherwise the entity would have been filtered out in the query.
                throw new InvalidOperationException(SR.KeyRotationNoEncryptionMetadata);
            }
            else
            {
                this.encryptionMetadataJson = dte.Properties[Constants.EncryptionConstants.TableEncryptionKeyDetails].StringValue;
            }
            Dictionary <string, EntityProperty> properties = new Dictionary<string, EntityProperty>(dte.Properties);
            properties.Remove(Constants.EncryptionConstants.TableEncryptionKeyDetails);
            properties.Remove(Constants.EncryptionConstants.TableEncryptionPropertyDetails);
            this.Properties = new System.Collections.ObjectModel.ReadOnlyDictionary<string, EntityProperty>(properties);
        }

        /// <summary>
        /// Gets the properties in the table entity, indexed by property name.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> object containing the entity's properties.</value>
        public IReadOnlyDictionary<string, EntityProperty> Properties { get; private set; }

        internal String encryptionMetadataJson { get; set; }

        /// <summary>
        /// Gets or sets the entity's partition key.
        /// </summary>
        /// <value>A string containing the partition key value for the entity.</value>
        public string PartitionKey { get; private set; }

        /// <summary>
        /// Gets or sets the entity's row key.
        /// </summary>
        /// <value>A string containing the row key value for the entity.</value>
        public string RowKey { get; private set; }

        /// <summary>
        /// Gets or sets the entity's timestamp.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> containing the timestamp for the entity.</value>
        public DateTimeOffset Timestamp { get; private set; }

        /// <summary>
        /// Gets or sets the entity's current ETag.
        /// </summary>
        /// <value>A string containing the ETag for the entity.</value>
        /// <remarks>Set this value to '*' to blindly overwrite an entity as part of an update operation.</remarks>
        public string ETag { get; private set; }

#if !(WINDOWS_RT ||  NETCORE)
        /// <summary>
        /// Gets or sets the entity's property, given the name of the property.
        /// </summary>
        /// <param name="key">A string containing the name of the property.</param>
        /// <returns>An <see cref="EntityProperty"/> object.</returns>
        public EntityProperty this[string key]
        {
            get { return this.Properties[key]; }
        }
#endif
    }
}