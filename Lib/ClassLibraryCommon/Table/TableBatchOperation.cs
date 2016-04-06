// -----------------------------------------------------------------------------------------
// <copyright file="TableBatchOperation.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;

    /// <summary>
    /// Represents a batch operation on a table.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Back compatibility.")]
    public sealed partial class TableBatchOperation : IList<TableOperation>
    {
        #region Factories
        /// <summary>
        /// Inserts a <see cref="TableOperation"/> into the batch that retrieves an entity based on its row key and partition key. The entity will be deserialized into the specified class type which extends <see cref="ITableEntity"/>.
        /// </summary>
        /// <typeparam name="TElement">The class of type for the entity to retrieve.</typeparam>
        /// <param name="partitionKey">A string containing the partition key of the entity to retrieve.</param>
        /// <param name="rowKey">A string containing the row key of the entity to retrieve.</param>
        /// <param name="selectedColumns">List of column names for projection.</param>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Reviewed.")]
        public void Retrieve<TElement>(string partitionKey, string rowKey, List<string> selectedColumns = null) where TElement : ITableEntity
        {
            CommonUtility.AssertNotNull("partitionKey", partitionKey);
            CommonUtility.AssertNotNull("rowkey", rowKey);

            // Add the table operation.
            this.Add(new TableOperation(null /* entity */, TableOperationType.Retrieve) { RetrievePartitionKey = partitionKey, RetrieveRowKey = rowKey, SelectColumns = selectedColumns, RetrieveResolver = (pk, rk, ts, prop, etag) => EntityUtilities.ResolveEntityByType<TElement>(pk, rk, ts, prop, etag) });
        }

        /// <summary>
        /// Adds a table operation to retrieve an entity of the specified class type with the specified partition key and row key to the batch operation.
        /// </summary>
        /// <typeparam name="TResult">The return type which the specified <see cref="EntityResolver{TResult}"/> will resolve the given entity to.</typeparam>
        /// <param name="partitionKey">A string containing the partition key of the entity to retrieve.</param>
        /// <param name="rowKey">A string containing the row key of the entity to retrieve.</param>
        /// <param name="resolver">The <see cref="EntityResolver{TResult}"/> implementation to project the entity to retrieve as a particular type in the result.</param>
        /// <param name="selectedColumns">List of column names for projection.</param>
        public void Retrieve<TResult>(string partitionKey, string rowKey, EntityResolver<TResult> resolver, List<string> selectedColumns = null)
        {
            CommonUtility.AssertNotNull("partitionKey", partitionKey);
            CommonUtility.AssertNotNull("rowkey", rowKey);

            // Add the table operation.
            this.Add(new TableOperation(null /* entity */, TableOperationType.Retrieve) { RetrievePartitionKey = partitionKey, RetrieveRowKey = rowKey, SelectColumns = selectedColumns, RetrieveResolver = (pk, rk, ts, prop, etag) => resolver(pk, rk, ts, prop, etag) });
        }

        #endregion

#if SYNC
        [DoesServiceRequest]
        internal IList<TableResult> Execute(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);

            if (this.operations.Count == 0)
            {
                throw new InvalidOperationException(SR.EmptyBatchOperation);
            }

            if (this.operations.Count > 100)
            {
                throw new InvalidOperationException(SR.BatchExceededMaximumNumberOfOperations);
            }

            return Executor.ExecuteSync(BatchImpl(this, client, table, modifiedOptions), modifiedOptions.RetryPolicy, operationContext);
        }
#endif

        [DoesServiceRequest]
        internal ICancellableAsyncResult BeginExecute(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);

            if (this.operations.Count == 0)
            {
                throw new InvalidOperationException(SR.EmptyBatchOperation);
            }

            if (this.operations.Count > 100)
            {
                throw new InvalidOperationException(SR.BatchExceededMaximumNumberOfOperations);
            }

            return Executor.BeginExecuteAsync(
                                          BatchImpl(this, client, table, modifiedOptions),
                                          modifiedOptions.RetryPolicy,
                                          operationContext,
                                          callback,
                                          state);
        }

        internal static IList<TableResult> EndExecute(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<IList<TableResult>>(asyncResult);
        }

        private static RESTCommand<IList<TableResult>> BatchImpl(TableBatchOperation batch, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
        {
            RESTCommand<IList<TableResult>> batchCmd = new RESTCommand<IList<TableResult>>(client.Credentials, client.StorageUri);
            requestOptions.ApplyToStorageCommand(batchCmd);

            List<TableResult> results = new List<TableResult>();

            batchCmd.CommandLocationMode = batch.ContainsWrites ? CommandLocationMode.PrimaryOnly : CommandLocationMode.PrimaryOrSecondary;
            batchCmd.RetrieveResponseStream = true;
            batchCmd.SignRequest = client.AuthenticationHandler.SignRequest;
            batchCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            batchCmd.BuildRequestDelegate = (uri, builder, timeout, useVersionHeader, ctx) =>
            {
                Tuple<HttpWebRequest, Stream> res = TableOperationHttpWebRequestFactory.BuildRequestForTableBatchOperation(uri, builder, client.BufferManager, timeout, table.Name, batch, useVersionHeader, ctx, requestOptions, client.AccountName);
                batchCmd.SendStream = res.Item2;
                return res.Item1;
            };

            batchCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp != null ? resp.StatusCode : HttpStatusCode.Unused, results, cmd, ex);
            batchCmd.PostProcessResponse = (cmd, resp, ctx) => TableOperationHttpResponseParsers.TableBatchOperationPostProcess(results, batch, cmd, resp, ctx, requestOptions, client.AccountName);
            batchCmd.RecoveryAction = (cmd, ex, ctx) => results.Clear();

            return batchCmd;
        }
    }
}
