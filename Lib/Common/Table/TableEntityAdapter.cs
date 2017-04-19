// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TableEntityAdapter.cs" company="Microsoft">
//   Copyright 2013 Microsoft Corporation
//   //    Licensed under the Apache License, Version 2.0 (the "License");
//   //    you may not use this file except in compliance with the License.
//   //    You may obtain a copy of the License at
//   //      http://www.apache.org/licenses/LICENSE-2.0
//   //    Unless required by applicable law or agreed to in writing, software
//   //    distributed under the License is distributed on an "AS IS" BASIS,
//   //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   //    See the License for the specific language governing permissions and
//   //    limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Microsoft.WindowsAzure.Storage.Table
{
    using System.Collections.Generic;

    /// <summary>
    /// Adapter class to allow reading and writing objects to Azure Table Storage without inheriting from <see cref="TableEntity"/> class
    /// or implementing <see cref="ITableEntity"/> interface. The objects can be simple POCO objects or complex objects with nested complex properties.
    /// </summary>
    /// <typeparam name="T">The type of object to read and write to Azure Table Storage, it can be a class or a struct.</typeparam>
    public class TableEntityAdapter<T> : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntityAdapter{T}"/> class.
        /// </summary>
        public TableEntityAdapter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntityAdapter{T}"/> class with the specified object.
        /// </summary>
        /// <param name="originalEntity">The object to write to Azure Table Storage.</param>
        public TableEntityAdapter(T originalEntity)
        {
            this.OriginalEntity = originalEntity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntityAdapter{T}"/> class with the specified object, partition key and row key.
        /// </summary>
        /// <param name="originalEntity">The object to write to Azure Table Storage.</param>
        /// <param name="partitionKey">A string containing the partition key value for the entity.</param>
        /// <param name="rowKey">A string containing the row key value for the entity.</param>
        public TableEntityAdapter(T originalEntity, string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
            this.OriginalEntity = originalEntity;
        }

        /// <summary>
        /// The original entity that is read and written to azure table storage.
        /// </summary>
        public T OriginalEntity { get; set; }

        /// <summary>
        /// Deserializes <see cref="TableEntityAdapter{T}"/> instance using the specified <see cref="IDictionary{TKey,TValue}"/> that maps property names of the
        /// <see cref="OriginalEntity"/> to typed <see cref="EntityProperty"/> values and stores it in the <see cref="OriginalEntity"/> property.
        /// </summary>
        /// <param name="properties">An <see cref="IDictionary{TKey,TValue}"/> object that maps property names to typed <see cref="EntityProperty"/> values.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            this.OriginalEntity = ConvertBack<T>(properties, operationContext);
        }

        /// <summary>
        /// Serializes the <see cref="IDictionary{TKey,TValue}"/> of property names mapped to <see cref="EntityProperty"/> data values from the <see cref="OriginalEntity"/> property.
        /// </summary>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An <see cref="IDictionary{TKey,TValue}"/> object that maps string property names to <see cref="EntityProperty"/> typed values created by
        /// serializing this table entity instance.</returns>
        /// <remarks>If <see cref="OriginalEntity"/> is a simple POCO object with simple properties (primitive types, string, byte[], ...), <see cref="WriteEntity"/> method will create
        ///  <see cref="EntityProperty"/> objects using these properties.<br/>
        /// Ie. A simple POCO object A with properties of B and C with this structure A->B, A->C, will be converted to key value pairs of {"B", EntityProperty(B)}, {"C", EntityProperty(C)}.<br/>
        /// If <see cref="OriginalEntity"/> has complex properties (and potentially these properties having complex properties of their own), <see cref="WriteEntity"/> method will flatten <see cref="OriginalEntity"/> first.<br/>
        /// Ie. An object A with a simple property of B and complex properties of C and D which have their own properties of E and F with this structure A->B, A->C->E and A->D->F, will be flattened to key value pairs of:<br/>
        /// {"B", EntityProperty(B)}, {"C_E", EntityProperty(E)} and {"D_F", EntityProperty(F)}.<br/>
        /// For each key value pair:<br/>
        /// 1. The key is composed by appending the names of the properties visited from root (A) to end node property (E or F) delimited by "_".<br/>
        /// 2. The value is the <see cref="EntityProperty"/> object, instantiated by the value of the end node property.<br/>
        /// All key value pairs will be stored in the returned <see cref="IDictionary{TKey,TValue}"/>.<br/>
        /// <see cref="ReadEntity"/> method recomposes the original object (POCO or complex) using the <see cref="IDictionary{TKey,TValue}"/> returned by this method and store it in <see cref="OriginalEntity"/> property.<br/>
        /// Properties that are marked with <see cref="IgnorePropertyAttribute"/> in the <see cref="OriginalEntity"/> object will be ignored and not processed by this method.</remarks>
        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return Flatten(this.OriginalEntity, operationContext);
        }
    }
}
