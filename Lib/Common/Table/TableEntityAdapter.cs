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
    /// Adapter class to allow reading and writing simple and complex POCO objects to azure table storage without inheriting from <see cref="TableEntity"/> class
    /// or implementing <see cref="ITableEntity"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of POCO object to read and write to azure table storage.</typeparam>
    /// <remarks>Automatically handles flattening and recomposing the POCO object of type T for reading and writing to azure table storage.</remarks>
    public class TableEntityAdapter<T> : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntityAdapter{T}"/> class.
        /// </summary>
        public TableEntityAdapter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntityAdapter{T}"/> class with the specified POCO object.
        /// </summary>
        /// <param name="originalEntity">The POCO object to write to azure table storage.</param>
        public TableEntityAdapter(T originalEntity)
        {
            this.OriginalEntity = originalEntity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntityAdapter{T}"/> class with the specified POCO object, partition key and row key.
        /// </summary>
        /// <param name="originalEntity">The POCO object to write to azure table storage.</param>
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
        /// <see cref="OriginalEntity"/> to typed <see cref="EntityProperty"/> values.
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
        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return Flatten(this.OriginalEntity, operationContext);
        }
    }
}
