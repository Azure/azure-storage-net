// -----------------------------------------------------------------------------------------
// <copyright file="TableEntity.cs" company="Microsoft">
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

using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Core.Util;

namespace Microsoft.WindowsAzure.Storage.Table
{
    /// <summary>
    /// An <see cref="ITableEntity"/> type which facilitates the use of Table Storage functionality
    /// without the need to subclass <see cref="TableEntity" />.
    /// </summary>    
    public class GenericTableEntity<T> : TableEntity where T : class, new() 
    {
        public T Item { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericTableEntity{T}"/> class.
        /// This constructor is primarily for framework use. You should prefer a constructor
        /// that accepts a partition and row key.
        /// </summary>
        public GenericTableEntity()
        {
            Item = new T();
        }

        //To be used externally
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericTableEntity{T}"/> class with the provided
        /// </summary>
        /// <param name="item">The object to wrap.</param>
        /// <param name="partitionKey">The partition key to assign to the data.</param>
        /// <param name="rowKey">The row key to assign to the data.</param>
        public GenericTableEntity(T item, string partitionKey, string rowKey) : base(partitionKey, rowKey)
        {
            CommonUtility.AssertNotNull(nameof(item), item);
            Item = item;
        }

        /// <summary>
        /// Deserializes the entity using the specified <see cref="IDictionary{TKey,TValue}"/> that maps property names to typed <see cref="EntityProperty"/> values. 
        /// </summary>
        /// <param name="properties">An <see cref="IDictionary{TKey,TValue}"/> object that maps property names to typed <see cref="EntityProperty"/> values.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(Item, properties, operationContext);
        }

        /// <summary>
        /// Serializes the <see cref="IDictionary{TKey,TValue}"/> of property names mapped to <see cref="EntityProperty"/> data values from this <see cref="GenericTableEntity{T}"/> instance.
        /// </summary>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An <see cref="IDictionary{TKey,TValue}"/> object that maps string property names to <see cref="EntityProperty"/> typed values created by serializing this table entity instance.</returns>
        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return base.WriteEntity(Item, operationContext);
        }
    }
}