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
    using System.Net;
    using System.Threading;
#if NETCORE
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
#endif
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a batch operation on a table.
    /// </summary>
    public sealed partial class TableBatchOperation : IList<TableOperation>
    {
        internal Task<IList<TableResult>> ExecuteAsync(CloudTableClient client, string tableName, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            CommonUtility.AssertNotNullOrEmpty("tableName", tableName);
            if (this.operations.Count == 0)
            {
                throw new InvalidOperationException(SR.EmptyBatchOperation);
            }

            if (this.operations.Count > 100)
            {
                throw new InvalidOperationException(SR.BatchExceededMaximumNumberOfOperations);
            }

            RESTCommand<IList<TableResult>> cmdToExecute = BatchImpl(this, client, tableName, modifiedOptions);

            return Task.Run(async () => await Executor.ExecuteAsync(
                                                            cmdToExecute,
                                                            modifiedOptions.RetryPolicy,
                                                            operationContext,
                                                            cancellationToken), cancellationToken);
        }

        private static RESTCommand<IList<TableResult>> BatchImpl(TableBatchOperation batch, CloudTableClient client, string tableName, TableRequestOptions requestOptions)
        {
            RESTCommand<IList<TableResult>> batchCmd = new RESTCommand<IList<TableResult>>(client.Credentials, client.StorageUri);
            requestOptions.ApplyToStorageCommand(batchCmd);

            List<TableResult> results = new List<TableResult>();

            batchCmd.CommandLocationMode = batch.ContainsWrites ? CommandLocationMode.PrimaryOnly : CommandLocationMode.PrimaryOrSecondary;
            batchCmd.RetrieveResponseStream = true;
            batchCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            batchCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableOperationHttpRequestMessageFactory.BuildRequestForTableBatchOperation(cmd, uri, builder, serverTimeout, tableName, batch, client, cnt, ctx, requestOptions.PayloadFormat.Value, client.GetCanonicalizer(), client.Credentials);
            batchCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp.StatusCode, results, cmd, ex);
            batchCmd.PostProcessResponse = (cmd, resp, ctx) => TableOperationHttpResponseParsers.TableBatchOperationPostProcess(results, batch, cmd, resp, ctx, requestOptions, client.AccountName);
            batchCmd.RecoveryAction = (cmd, ex, ctx) => results.Clear();

            return batchCmd;
        }

        /// <summary>
        /// Adds a table operation that retrieves an entity with the specified partition key and row key to the batch operation.  The entity will be deserialized into the specified class type which extends <see cref="ITableEntity"/>.
        /// </summary>
        /// <typeparam name="TElement">The class of type for the entity to retrieve.</typeparam>
        /// <param name="batch">The input <see cref="TableBatchOperation"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="partitionKey">A string containing the partition key of the entity to be retrieved.</param>
        /// <param name="rowkey">A string containing the row key of the entity to be retrieved.</param>
        /// <param name="selectedColumns">List of column names for projection.</param>
        public void Retrieve<TElement>(string partitionKey, string rowkey, List<string> selectedColumns = null) where TElement : ITableEntity
        {
            CommonUtility.AssertNotNull("partitionKey", partitionKey);
            CommonUtility.AssertNotNull("rowkey", rowkey);

            // Add the table operation.
            this.Add(new TableOperation(null /* entity */, TableOperationType.Retrieve) { RetrievePartitionKey = partitionKey, RetrieveRowKey = rowkey, SelectColumns = selectedColumns, RetrieveResolver = (pk, rk, ts, prop, etag) => EntityUtilities.ResolveEntityByType<TElement>(pk, rk, ts, prop, etag) });
        }

        /// <summary>
        /// Adds a table operation that retrieves an entity with the specified partition key and row key to the batch operation.
        /// </summary>
        /// <typeparam name="TResult">The return type which the specified <see cref="EntityResolver{T}"/> will resolve the given entity to.</typeparam>
        /// <param name="batch">The input <see cref="TableBatchOperation"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="partitionKey">A string containing the partition key of the entity to be retrieved.</param>
        /// <param name="rowkey">A string containing the row key of the entity to be retrieved.</param>
        /// <param name="resolver">The <see cref="EntityResolver{R}"/> implementation to project the entity to retrieve as a particular type in the result.</param>
        /// <param name="selectedColumns">List of column names for projection.</param>
        public void Retrieve<TResult>(string partitionKey, string rowkey, EntityResolver<TResult> resolver, List<string> selectedColumns = null)
        {
            CommonUtility.AssertNotNull("partitionKey", partitionKey);
            CommonUtility.AssertNotNull("rowkey", rowkey);

            // Add the table operation.
            this.Add(new TableOperation(null /* entity */, TableOperationType.Retrieve) { RetrievePartitionKey = partitionKey, RetrieveRowKey = rowkey, SelectColumns = selectedColumns, RetrieveResolver = (pk, rk, ts, prop, etag) => resolver(pk, rk, ts, prop, etag) });
        }
    }
}
