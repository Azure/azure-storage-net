// -----------------------------------------------------------------------------------------
// <copyright file="TableOperation.Common.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Represents a single table operation.
    /// </summary>
    public partial class TableOperation
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TableOperation"/> class given the
        /// entity to operate on and the type of operation that is being
        /// performed.
        /// </summary>
        /// <param name="entity">The entity on which the operation is being performed.</param>
        /// <param name="operationType">The type of operation.</param>
        internal TableOperation(ITableEntity entity, TableOperationType operationType)
            : this(entity, operationType, true)
        {
        }

        internal TableOperation(ITableEntity entity, TableOperationType operationType, bool echoContent)
        {
            if (entity == null && operationType != TableOperationType.Retrieve)
            {
                throw new ArgumentNullException("entity");
            }

            this.Entity = entity;
            this.OperationType = operationType;
            this.EchoContent = echoContent;
        }

        private bool isTableEntity = false;

        internal bool IsTableEntity
        {
            get { return this.isTableEntity; }
            set { this.isTableEntity = value; }
        }

        private bool isPrimaryOnlyRetrieve = false;

        internal bool IsPrimaryOnlyRetrieve
        {
            get { return this.isPrimaryOnlyRetrieve; }
            set { this.isPrimaryOnlyRetrieve = value; }
        }

        // Retrieve operation, typically this would be in a derived class, but since we want to export to winmd, it is specialized via the below locals
        internal string RetrievePartitionKey { get; set; }

        internal string RetrieveRowKey { get; set; }

        private Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, object> retrieveResolver = null;

        internal Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, object> RetrieveResolver
        {
            get
            {
                if (this.retrieveResolver == null)
                {
                    this.retrieveResolver = DynamicEntityResolver;
                }

                return this.retrieveResolver;
            }

            set 
            { 
                this.retrieveResolver = value; 
            }
        }

        internal Type PropertyResolverType { get; set; }

        /// <summary>
        /// Gets the entity that is being operated upon.
        /// </summary>
        internal ITableEntity Entity { get; private set; }

        /// <summary>
        /// Gets the type of operation.
        /// </summary>
        internal TableOperationType OperationType { get; private set; }

        /// <summary>
        /// Gets or sets the value that represents whether the message payload should be returned in the response.
        /// </summary>
        internal bool EchoContent { get; set; }

        /// <summary>
        /// List of columns to project with for the retrieve operation.
        /// </summary>
        internal List<string> SelectColumns { get; set; }

        /// <summary>
        /// Creates a new table operation that deletes the given entity
        /// from a table.
        /// </summary>
        /// <param name="entity">The <see cref="ITableEntity"/> object to be deleted from the table.</param>
        /// <returns>The <see cref="TableOperation"/> object.</returns>
        public static TableOperation Delete(ITableEntity entity)
        {
            // Validate the arguments.
            CommonUtility.AssertNotNull("entity", entity);

            // Delete requires an ETag.
            if (string.IsNullOrEmpty(entity.ETag))
            {
                throw new ArgumentException(SR.ETagMissingForDelete);
            }

            // Create and return the table operation.
            return new TableOperation(entity, TableOperationType.Delete);
        }

        /// <summary>
        /// Creates a new table operation that inserts the given entity
        /// into a table.
        /// </summary>
        /// <param name="entity">The <see cref="ITableEntity"/> object to be inserted into the table.</param>
        /// <returns>The <see cref="TableOperation"/> object.</returns>
        public static TableOperation Insert(ITableEntity entity)
        {
            return Insert(entity, false);
        }

        /// <summary>
        /// Creates a new table operation that inserts the given entity
        /// into a table.
        /// </summary>
        /// <param name="entity">The <see cref="ITableEntity"/> object to be inserted into the table.</param>
        /// <param name="echoContent"><c>true</c> if the message payload should be returned in the response to the insert operation. <c>false</c> otherwise.</param>
        /// <returns>The <see cref="TableOperation"/> object.</returns>
        public static TableOperation Insert(ITableEntity entity, bool echoContent)
        {
            // Validate the arguments.
            CommonUtility.AssertNotNull("entity", entity);

            // Create and return the table operation.
            return new TableOperation(entity, TableOperationType.Insert, echoContent);
        }

        /// <summary>
        /// Creates a new table operation that inserts the given entity
        /// into a table if the entity does not exist; if the entity does
        /// exist then its contents are merged with the provided entity.
        /// </summary>
        /// <param name="entity">The <see cref="ITableEntity"/> object to be inserted or merged.</param>
        /// <returns>The <see cref="TableOperation"/> object.</returns>
        public static TableOperation InsertOrMerge(ITableEntity entity)
        {
            // Validate the arguments.
            CommonUtility.AssertNotNull("entity", entity);

            // Create and return the table operation.
            return new TableOperation(entity, TableOperationType.InsertOrMerge);
        }

        /// <summary>
        /// Creates a new table operation that inserts the given entity
        /// into a table if the entity does not exist; if the entity does
        /// exist then its contents are replaced with the provided entity.
        /// </summary>
        /// <param name="entity">The <see cref="ITableEntity"/> object to be inserted or replaced.</param>
        /// <returns>The <see cref="TableOperation"/> object.</returns>
        public static TableOperation InsertOrReplace(ITableEntity entity)
        {
            // Validate the arguments.
            CommonUtility.AssertNotNull("entity", entity);

            // Create and return the table operation.
            return new TableOperation(entity, TableOperationType.InsertOrReplace);
        }

        /// <summary>
        /// Creates a new table operation that merges the contents of
        /// the given entity with the existing entity in a table.
        /// </summary>
        /// <param name="entity">The <see cref="ITableEntity"/> object to be merged.</param>
        /// <returns>The <see cref="TableOperation"/> object.</returns>
        public static TableOperation Merge(ITableEntity entity)
        {
            // Validate the arguments.
            CommonUtility.AssertNotNull("entity", entity);

            // Merge requires an ETag.
            if (string.IsNullOrEmpty(entity.ETag))
            {
                throw new ArgumentException(SR.ETagMissingForMerge);
            }

            // Create and return the table operation.
            return new TableOperation(entity, TableOperationType.Merge);
        }

        /// <summary>
        /// Creates a new table operation that replaces the contents of
        /// the given entity in a table.
        /// </summary>
        /// <param name="entity">The <see cref="ITableEntity"/> object to be replaced.</param>
        /// <returns>The <see cref="TableOperation"/> object.</returns>
        public static TableOperation Replace(ITableEntity entity)
        {
            // Validate the arguments.
            CommonUtility.AssertNotNull("entity", entity);

            // Replace requires an ETag.
            if (string.IsNullOrEmpty(entity.ETag))
            {
                throw new ArgumentException(SR.ETagMissingForReplace);
            }

            // Create and return the table operation.
            return new TableOperation(entity, TableOperationType.Replace);
        }

        /// <summary>
        /// Creates a new table operation that retrieves the contents of
        /// the given entity in a table.
        /// </summary>
        /// <typeparam name="TElement">The class of type for the entity to retrieve.</typeparam>
        /// <param name="partitionKey">A string containing the partition key of the entity to retrieve.</param>
        /// <param name="rowkey">A string containing the row key of the entity to retrieve.</param>
        /// <param name="selectColumns">List of column names for projection.</param>
        /// <returns>The <see cref="TableOperation"/> object.</returns>
        [SuppressMessage("Microsoft.Design",
            "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Reviewed")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "rowkey",
            Justification = "Reviewed : rowkey is acceptable.")]
        public static TableOperation Retrieve<TElement>(string partitionKey, string rowkey, List<string> selectColumns = null)
            where TElement : ITableEntity
        {
            CommonUtility.AssertNotNull("partitionKey", partitionKey);
            CommonUtility.AssertNotNull("rowkey", rowkey);

            // Create and return the table operation.
            return new TableOperation(null /* entity */, TableOperationType.Retrieve)
            {
                RetrievePartitionKey = partitionKey,
                RetrieveRowKey = rowkey,
                SelectColumns = selectColumns,
                RetrieveResolver =
                    (pk, rk, ts, prop, etag) => EntityUtilities.ResolveEntityByType<TElement>(
                            pk,
                            rk,
                            ts,
                            prop,
                            etag),
                PropertyResolverType = typeof(TElement)
            };
        }

        /// <summary>
        /// Creates a new table operation that retrieves the contents of
        /// the given entity in a table.
        /// </summary>
        /// <typeparam name="TResult">The return type which the specified <see cref="EntityResolver{T}"/> will resolve the given entity to.</typeparam>
        /// <param name="partitionKey">A string containing the partition key of the entity to retrieve.</param>
        /// <param name="rowkey">A string containing the row key of the entity to retrieve.</param>
        /// <param name="resolver">The <see cref="EntityResolver{TResult}"/> implementation to project the entity to retrieve as a particular type in the result.</param>
        /// <param name="selectedColumns">List of column names for projection.</param>
        /// <returns>The <see cref="TableOperation"/> object.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "rowkey", Justification = "Reviewed : rowkey is acceptable.")]
        public static TableOperation Retrieve<TResult>(string partitionKey, string rowkey, EntityResolver<TResult> resolver, List<string> selectedColumns = null)
        {
            CommonUtility.AssertNotNull("partitionKey", partitionKey);
            CommonUtility.AssertNotNull("rowkey", rowkey);

            // Create and return the table operation.
            return new TableOperation(null /* entity */, TableOperationType.Retrieve) { RetrievePartitionKey = partitionKey, RetrieveRowKey = rowkey, RetrieveResolver = (pk, rk, ts, prop, etag) => resolver(pk, rk, ts, prop, etag), SelectColumns = selectedColumns };
        }

        /// <summary>
        /// Creates a new table operation that retrieves the contents of
        /// the given entity in a table.
        /// </summary>
        /// <param name="partitionKey">A string containing the partition key of the entity to be retrieved.</param>
        /// <param name="rowkey">A string containing the row key of the entity to be retrieved.</param>
        /// <param name="selectedColumns">List of column names for projection.</param>
        /// <returns>The <see cref="TableOperation"/> object.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "rowkey", Justification = "Reviewed : rowkey is allowed.")]
        public static TableOperation Retrieve(string partitionKey, string rowkey, List<string> selectedColumns = null)
        {
            CommonUtility.AssertNotNull("partitionKey", partitionKey);
            CommonUtility.AssertNotNull("rowkey", rowkey);

            // Create and return the table operation.
            return new TableOperation(null /* entity */, TableOperationType.Retrieve) { RetrievePartitionKey = partitionKey, RetrieveRowKey = rowkey, SelectColumns = selectedColumns };
        }

        private static object DynamicEntityResolver(string partitionKey, string rowKey, DateTimeOffset timestamp, IDictionary<string, EntityProperty> properties, string etag)
        {
            ITableEntity entity = new DynamicTableEntity();

            entity.PartitionKey = partitionKey;
            entity.RowKey = rowKey;
            entity.Timestamp = timestamp;
            entity.ReadEntity(properties, null);
            entity.ETag = etag;

            return entity;
        }

        internal StorageUri GenerateRequestURI(StorageUri uriList, string tableName)
        {
            return new StorageUri(
                this.GenerateRequestURI(uriList.PrimaryUri, tableName),
                this.GenerateRequestURI(uriList.SecondaryUri, tableName));
        }

        internal Uri GenerateRequestURI(Uri uri, string tableName)
        {
            if (uri == null)
            {
                return null;
            }

            if (this.OperationType == TableOperationType.Insert)
            {
                return NavigationHelper.AppendPathToSingleUri(uri, tableName + "()");
            }
            else
            {
                string identity = null;
                if (this.isTableEntity)
                {
                    // Note tableEntity is only used internally, so we can assume operationContext is not needed
                    identity = string.Format(CultureInfo.InvariantCulture, "'{0}'", this.Entity.WriteEntity(null /* OperationContext  */)[TableConstants.TableName].StringValue);
                }
                else if (this.OperationType == TableOperationType.Retrieve)
                {
                    // OData readers expect single quote to be escaped in a param value.
                    identity = string.Format(CultureInfo.InvariantCulture, "{0}='{1}',{2}='{3}'", TableConstants.PartitionKey, this.RetrievePartitionKey.Replace("'", "''"), TableConstants.RowKey, this.RetrieveRowKey.Replace("'", "''"));
                }
                else
                {
                    // OData readers expect single quote to be escaped in a param value.
                    identity = string.Format(CultureInfo.InvariantCulture, "{0}='{1}',{2}='{3}'", TableConstants.PartitionKey, this.Entity.PartitionKey.Replace("'", "''"), TableConstants.RowKey, this.Entity.RowKey.Replace("'", "''"));
                }

                return NavigationHelper.AppendPathToSingleUri(uri, string.Format(CultureInfo.InvariantCulture, "{0}({1})", tableName, identity));
            }
        }

        internal UriQueryBuilder GenerateQueryBuilder(bool? projectSystemProperties)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            // select
            if (this.SelectColumns != null && this.SelectColumns.Count > 0)
            {
                StringBuilder colBuilder = new StringBuilder();
                bool foundRk = false;
                bool foundPk = false;
                bool foundTs = false;

                for (int m = 0; m < this.SelectColumns.Count; m++)
                {
                    if (this.SelectColumns[m] == TableConstants.PartitionKey)
                    {
                        foundPk = true;
                    }
                    else if (this.SelectColumns[m] == TableConstants.RowKey)
                    {
                        foundRk = true;
                    }
                    else if (this.SelectColumns[m] == TableConstants.Timestamp)
                    {
                        foundTs = true;
                    }

                    colBuilder.Append(this.SelectColumns[m]);
                    if (m < this.SelectColumns.Count - 1)
                    {
                        colBuilder.Append(",");
                    }
                }

                if (projectSystemProperties.Value)
                {
                    if (!foundPk)
                    {
                        colBuilder.Append(",");
                        colBuilder.Append(TableConstants.PartitionKey);
                    }

                    if (!foundRk)
                    {
                        colBuilder.Append(",");
                        colBuilder.Append(TableConstants.RowKey);
                    }

                    if (!foundTs)
                    {
                        colBuilder.Append(",");
                        colBuilder.Append(TableConstants.Timestamp);
                    }
                }

                builder.Add(TableConstants.Select, colBuilder.ToString());
            }

            return builder;
        }
    }
}
