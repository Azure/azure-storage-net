// -----------------------------------------------------------------------------------------
// <copyright file="CloudTable.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Table.Queryable;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a Microsoft Azure table.
    /// </summary>
    public partial class CloudTable
    {
        #region TableOperation Execute Methods
#if SYNC
        /// <summary>
        /// Executes an operation on a table.  
        /// </summary>
        /// <param name="operation">A <see cref="TableOperation"/> object that represents the operation to perform.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="TableResult"/> object.</returns>
        [DoesServiceRequest]
        public virtual TableResult Execute(TableOperation operation, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("operation", operation);

            return operation.Execute(this.ServiceClient, this, requestOptions, operationContext);
        }
#endif

        /// <summary>
        /// Begins execution of an asynchronous table operation.
        /// </summary>
        /// <param name="operation">A <see cref="TableOperation"/> object that represents the operation to perform.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecute(TableOperation operation, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("batch", operation);
            return this.BeginExecute(operation, null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins execution of an asynchronous table operation.
        /// </summary>
        /// <param name="operation">A <see cref="TableOperation"/> object that represents the operation to perform.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecute(TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("operation", operation);

            return operation.BeginExecute(this.ServiceClient, this, requestOptions, operationContext, callback, state);
        }

        /// <summary>
        /// Ends execution of an asynchronous table operation.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="TableResult"/> containing the result executing the operation on the table.</returns>
        public virtual TableResult EndExecute(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<TableResult>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation that executes an asynchronous table operation.
        /// </summary>
        /// <param name="operation">A <see cref="TableOperation"/> object that represents the operation to perform.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableResult"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableResult> ExecuteAsync(TableOperation operation)
        {
            return this.ExecuteAsync(operation, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that executes an asynchronous table operation.
        /// </summary>
        /// <param name="operation">A <see cref="TableOperation"/> object that represents the operation to perform.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableResult"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableResult> ExecuteAsync(TableOperation operation, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecute, this.EndExecute, operation, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation that executes an asynchronous table operation.
        /// </summary>
        /// <param name="operation">A <see cref="TableOperation"/> object that represents the operation to perform.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableResult"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableResult> ExecuteAsync(TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExecuteAsync(operation, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that executes an asynchronous table operation.
        /// </summary>
        /// <param name="operation">A <see cref="TableOperation"/> object that represents the operation to perform.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableResult"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableResult> ExecuteAsync(TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecute, this.EndExecute, operation, requestOptions, operationContext, cancellationToken);
        }
#endif

        #endregion

        #region TableBatchOperation Execute Methods
#if SYNC
        /// <summary>
        /// Executes a batch operation on a table as an atomic operation.
        /// </summary>
        /// <param name="batch">The <see cref="TableBatchOperation"/> object representing the operations to execute on the table.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of <see cref="TableResult"/> objects that contains the results, in order, of each operation in the <see cref="TableBatchOperation"/> on the table.</returns>
        [DoesServiceRequest]
        public virtual IList<TableResult> ExecuteBatch(TableBatchOperation batch, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("batch", batch);
            return batch.Execute(this.ServiceClient, this, requestOptions, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to execute a batch of operations on a table.
        /// </summary>
        /// <param name="batch">The <see cref="TableBatchOperation"/> object representing the operations to execute on the table.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteBatch(TableBatchOperation batch, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("batch", batch);
            return this.BeginExecuteBatch(batch, null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to execute a batch of operations on a table.
        /// </summary>
        /// <param name="batch">The <see cref="TableBatchOperation"/> object representing the operations to execute on the table.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteBatch(TableBatchOperation batch, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("batch", batch);

            return batch.BeginExecute(this.ServiceClient, this, requestOptions, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to execute a batch of operations on a table.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A enumerable collection of type <see cref="TableResult"/> that contains the results, in order, of each operation in the <see cref="TableBatchOperation"/> on the table.</returns>
        public virtual IList<TableResult> EndExecuteBatch(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<IList<TableResult>>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to execute a batch of operations on a table.
        /// </summary>
        /// <param name="batch">The <see cref="TableBatchOperation"/> object representing the operations to execute on the table.</param>
        /// <returns>A <see cref="Task{T}"/> object that is list of type <see cref="TableResult"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batch)
        {
            return this.ExecuteBatchAsync(batch, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a batch of operations on a table.
        /// </summary>
        /// <param name="batch">The <see cref="TableBatchOperation"/> object representing the operations to execute on the table.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that is list of type <see cref="TableResult"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batch, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteBatch, this.EndExecuteBatch, batch, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a batch of operations on a table.
        /// </summary>
        /// <param name="batch">The <see cref="TableBatchOperation"/> object representing the operations to execute on the table.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that is list of type <see cref="TableResult"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batch, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExecuteBatchAsync(batch, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a batch of operations on a table.
        /// </summary>
        /// <param name="batch">The <see cref="TableBatchOperation"/> object representing the operations to execute on the table.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that is list of type <see cref="TableResult"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batch, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteBatch, this.EndExecuteBatch, batch, requestOptions, operationContext, cancellationToken);
        }
#endif

        #endregion

        #region TableQuery Execute Methods
        #region NonGeneric
#if SYNC
        /// <summary>
        /// Executes a query on a table and returns an enumerable collection of <see cref="DynamicTableEntity"/> objects.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of <see cref="DynamicTableEntity"/> objects, representing table entities returned by the query.</returns>
        [DoesServiceRequest]
        public virtual IEnumerable<DynamicTableEntity> ExecuteQuery(TableQuery query, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.Execute(this.ServiceClient, this, requestOptions, operationContext);
        }

        /// <summary>
        /// Executes a segmented query on a table and returns a <see cref="TableQuerySegment{TResult}"/> containing <see cref="DynamicTableEntity"/> objects.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="TableQuerySegment{TResult}"/> object of type <see cref="DynamicTableEntity"/> containing the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual TableQuerySegment<DynamicTableEntity> ExecuteQuerySegmented(TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.ExecuteQuerySegmented(token, this.ServiceClient, this, requestOptions, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous segmented query on a table.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteQuerySegmented(TableQuery query, TableContinuationToken token, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("query", query);
            return this.BeginExecuteQuerySegmented(query, token, null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous segmented query on a table.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteQuerySegmented(TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.BeginExecuteQuerySegmented(token, this.ServiceClient, this, requestOptions, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous segmented query on a table. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="TableQuerySegment{TResult}"/> object of type <see cref="DynamicTableEntity"/> containing the results of executing the query.</returns>
        public virtual TableQuerySegment<DynamicTableEntity> EndExecuteQuerySegmented(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<TableQuerySegment<DynamicTableEntity>>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to perform a segmented query on a table.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableQuerySegment{DynamicTableEntity}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<DynamicTableEntity>> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token)
        {
            return this.ExecuteQuerySegmentedAsync(query, token, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to perform a segmented query on a table.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableQuerySegment{DynamicTableEntity}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<DynamicTableEntity>> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteQuerySegmented, this.EndExecuteQuerySegmented, query, token, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to perform a segmented query on a table.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableQuerySegment{DynamicTableEntity}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<DynamicTableEntity>> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExecuteQuerySegmentedAsync(query, token, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to perform a segmented query on a table.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableQuerySegment{DynamicTableEntity}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<DynamicTableEntity>> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteQuerySegmented, this.EndExecuteQuerySegmented, query, token, requestOptions, operationContext, cancellationToken);
        }
#endif

        #region With Resolver
#if SYNC
        /// <summary>
        /// Executes a query on a table and applies the specified <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection, containing the projection into type <c>TResult</c>, of the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual IEnumerable<TResult> ExecuteQuery<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("query", query);
            CommonUtility.AssertNotNull("resolver", resolver);
            return query.Execute(this.ServiceClient, this, resolver, requestOptions, operationContext);
        }

        /// <summary>
        /// Executes a segmented query on a table and applies the specified <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="TableQuerySegment{TResult}"/> object containing the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual TableQuerySegment<TResult> ExecuteQuerySegmented<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.ExecuteQuerySegmented(token, this.ServiceClient, this, resolver, requestOptions, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to execute a segmented query and apply the specified <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteQuerySegmented<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, AsyncCallback callback, object state)
        {
            return this.BeginExecuteQuerySegmented(query, resolver, token, null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to execute a segmented query and apply the specified <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteQuerySegmented<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("query", query);
            CommonUtility.AssertNotNull("resolver", resolver);
            return query.BeginExecuteQuerySegmented(token, this.ServiceClient, this, resolver, requestOptions, operationContext, callback, state);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to execute a segmented query and apply the specified <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableQuerySegment{TResult}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token)
        {
            return this.ExecuteQuerySegmentedAsync(query, resolver, token, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a segmented query and apply the specified <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableQuerySegment{TResult}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteQuerySegmented, this.EndExecuteQuerySegmented<TResult>, query, resolver, token, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a segmented query and apply the specified <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableQuerySegment{TResult}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExecuteQuerySegmentedAsync(query, resolver, token, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a segmented query and apply the specified <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TableQuerySegment{TResult}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteQuerySegmented, this.EndExecuteQuerySegmented<TResult>, query, resolver, token, requestOptions, operationContext, cancellationToken);
        }
#endif

        #endregion
        #endregion

        #region Generic
        /// <summary>
        /// A factory method that creates a query that can be modified using LINQ. The query may be subsequently executed using one of the execution methods available for <see cref="CloudTable"/>, 
        /// such as <see cref="ExecuteQuery"/>, <see cref="ExecuteQuerySegmented"/>, or <see cref="ExecuteQuerySegmentedAsync(TableQuery,TableContinuationToken)"/>.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <returns>A <see cref="TableQuery"/> object, specialized for type <c>TElement</c>, that may subsequently be executed.</returns>
        /// <remarks>
        /// The <see cref="Microsoft.WindowsAzure.Storage.Table.Queryable"/> namespace includes extension methods for the <see cref="TableQuery"/> object, 
        /// including <see cref="M:WithOptions"/>, <see cref="M:WithContext"/>, and <see cref="M:AsTableQuery"/>. To use these methods, include a <c>using</c>
        /// statement that references the <see cref="Microsoft.WindowsAzure.Storage.Table.Queryable"/> namespace.
        /// </remarks>
        public virtual TableQuery<TElement> CreateQuery<TElement>() where TElement : ITableEntity, new()
        {
            return new TableQuery<TElement>(this);
        }

#if SYNC
        /// <summary>
        /// Executes a query on a table.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <param name="query">A TableQuery instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection, specialized for type <c>TElement</c>, of the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual IEnumerable<TElement> ExecuteQuery<TElement>(TableQuery<TElement> query, TableRequestOptions requestOptions = null, OperationContext operationContext = null) where TElement : ITableEntity, new()
        {
            CommonUtility.AssertNotNull("query", query);
            if (query.Provider != null)
            {
                return query.Execute(requestOptions, operationContext);
            }
            else
            {
                return query.ExecuteInternal(this.ServiceClient, this, requestOptions, operationContext);
            }
        }

        /// <summary>
        /// Executes a query on a table in segmented mode.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="TableQuerySegment{T}"/>, specialized for type <c>TElement</c>, containing the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual TableQuerySegment<TElement> ExecuteQuerySegmented<TElement>(TableQuery<TElement> query, TableContinuationToken token, TableRequestOptions requestOptions = null, OperationContext operationContext = null) where TElement : ITableEntity, new()
        {
            CommonUtility.AssertNotNull("query", query);
            if (query.Provider != null)
            {
                return query.ExecuteSegmented(token, requestOptions, operationContext);
            }
            else
            {
                return query.ExecuteQuerySegmentedInternal(token, this.ServiceClient, this, requestOptions, operationContext);
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to query a table in segmented mode.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteQuerySegmented<TElement>(TableQuery<TElement> query, TableContinuationToken token, AsyncCallback callback, object state) where TElement : ITableEntity, new()
        {
            CommonUtility.AssertNotNull("query", query);
            return this.BeginExecuteQuerySegmented(query, token, null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to query a table in segmented mode.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteQuerySegmented<TElement>(TableQuery<TElement> query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state) where TElement : ITableEntity, new()
        {
            CommonUtility.AssertNotNull("query", query);
            if (query.Provider != null)
            {
                return query.BeginExecuteSegmented(token, requestOptions, operationContext, callback, state);
            }
            else
            {
                return query.BeginExecuteQuerySegmentedInternal(token, this.ServiceClient, this, requestOptions, operationContext, callback, state);
            }
        }

        /// <summary>
        /// Ends an asynchronous operation to query a table in segmented mode.
        /// </summary>
        /// <typeparam name="TResult">The type of the results to be returned. Can be the entity type specified in the Begin or the result type of the resolver</typeparam>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="TableQuerySegment{TResult}"/> containing the results of executing the query.</returns>
        public virtual TableQuerySegment<TResult> EndExecuteQuerySegmented<TResult>(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<TableQuerySegment<TResult>>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to query a table in segmented mode.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TElement>> ExecuteQuerySegmentedAsync<TElement>(TableQuery<TElement> query, TableContinuationToken token) where TElement : ITableEntity, new()
        {
            return this.ExecuteQuerySegmentedAsync(query, token, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to query a table in segmented mode.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TElement>> ExecuteQuerySegmentedAsync<TElement>(TableQuery<TElement> query, TableContinuationToken token, CancellationToken cancellationToken) where TElement : ITableEntity, new()
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteQuerySegmented, this.EndExecuteQuerySegmented<TElement>, query, token, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to query a table in segmented mode.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TElement>> ExecuteQuerySegmentedAsync<TElement>(TableQuery<TElement> query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext) where TElement : ITableEntity, new()
        {
            return this.ExecuteQuerySegmentedAsync(query, token, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to query a table in segmented mode.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TElement>> ExecuteQuerySegmentedAsync<TElement>(TableQuery<TElement> query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TElement : ITableEntity, new()
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteQuerySegmented, this.EndExecuteQuerySegmented<TElement>, query, token, requestOptions, operationContext, cancellationToken);
        }
#endif

        #endregion

        #region With Resolvers
#if SYNC
        /// <summary>
        /// Executes a query and applies the specified <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection, containing the projection into type <c>TResult</c>, of the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual IEnumerable<TResult> ExecuteQuery<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableRequestOptions requestOptions = null, OperationContext operationContext = null) where TElement : ITableEntity, new()
        {
            CommonUtility.AssertNotNull("query", query);
            CommonUtility.AssertNotNull("resolver", resolver);
            if (query.Provider != null)
            {
                return TableQueryableExtensions.Resolve(query, resolver).Execute(requestOptions, operationContext);
            }
            else
            {
                return query.ExecuteInternal(this.ServiceClient, this, resolver, requestOptions, operationContext);
            }
        }

        /// <summary>
        /// Executes a query in segmented mode and applies the specified <see cref="EntityResolver{T}"/> to the results.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="TableQuerySegment{TResult}"/> containing the projection into type <c>TResult</c> of the results of executing the query. </returns>
        [DoesServiceRequest]
        public virtual TableQuerySegment<TResult> ExecuteQuerySegmented<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions = null, OperationContext operationContext = null) where TElement : ITableEntity, new()
        {
            CommonUtility.AssertNotNull("query", query);
            CommonUtility.AssertNotNull("resolver", resolver);
            if (query.Provider != null)
            {
                return TableQueryableExtensions.Resolve(query, resolver).ExecuteSegmented(token, requestOptions, operationContext);
            }
            else
            {
                return query.ExecuteQuerySegmentedInternal(token, this.ServiceClient, this, resolver, requestOptions, operationContext);
            }
        }

#endif
        /// <summary>
        /// Begins an asynchronous operation to query a table in segmented mode and apply the specified <see cref="EntityResolver{T}"/> to the results.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteQuerySegmented<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token, AsyncCallback callback, object state) where TElement : ITableEntity, new()
        {
            return this.BeginExecuteQuerySegmented(query, resolver, token, null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to execute a query in segmented mode and apply the specified <see cref="EntityResolver{T}"/> to the results.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteQuerySegmented<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state) where TElement : ITableEntity, new()
        {
            CommonUtility.AssertNotNull("query", query);
            CommonUtility.AssertNotNull("resolver", resolver);
            if (query.Provider != null)
            {
                return TableQueryableExtensions.Resolve(query, resolver).BeginExecuteSegmented(token, requestOptions, operationContext, callback, state);
            }
            else
            {
                return query.BeginExecuteQuerySegmentedInternal(token, this.ServiceClient, this, resolver, requestOptions, operationContext, callback, state);
            }
        }

        /// <summary>
        /// Ends an asynchronous operation to execute a query in segmented mode.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="TableQuerySegment{TResult}"/> containing the projection into type <c>TResult</c> of the results of executing the query. </returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Reveiewed.")]
        public virtual TableQuerySegment<TResult> EndExecuteQuerySegmented<TElement, TResult>(IAsyncResult asyncResult) where TElement : ITableEntity, new()
        {
            return Executor.EndExecuteAsync<TableQuerySegment<TResult>>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to execute a query in segmented mode and apply the specified <see cref="EntityResolver{T}"/> to the results.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token) where TElement : ITableEntity, new()
        {
            return this.ExecuteQuerySegmentedAsync(query, resolver, token, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a query in segmented mode and apply the specified <see cref="EntityResolver{T}"/> to the results.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token, CancellationToken cancellationToken) where TElement : ITableEntity, new()
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteQuerySegmented, this.EndExecuteQuerySegmented<TElement, TResult>, query, resolver, token, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a query in segmented mode and apply the specified <see cref="EntityResolver{T}"/> to the results.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext) where TElement : ITableEntity, new()
        {
            return this.ExecuteQuerySegmentedAsync(query, resolver, token, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a query in segmented mode and apply the specified <see cref="EntityResolver{T}"/> to the results.
        /// </summary>
        /// <typeparam name="TElement">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="query">A <see cref="TableQuery"/> instance specifying the table to query and the query parameters to use, specialized for a type <c>TElement</c>.</param>
        /// <param name="resolver">An <see cref="EntityResolver{T}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TElement : ITableEntity, new()
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteQuerySegmented, this.EndExecuteQuerySegmented<TElement, TResult>, query, resolver, token, requestOptions, operationContext, cancellationToken);
        }
#endif

        #endregion

        #endregion

        #region Create
#if SYNC
        /// <summary>
        /// Creates a table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void Create(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = TableRequestOptions.ApplyDefaultsAndClearEncryption(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            DynamicTableEntity tblEntity = new DynamicTableEntity();
            tblEntity.Properties.Add(TableConstants.TableName, new EntityProperty(this.Name));
            TableOperation operation = new TableOperation(tblEntity, TableOperationType.Insert, false);
            operation.IsTableEntity = true;
            CloudTable serviceTable = this.ServiceClient.GetTableReference(TableConstants.TableServiceTablesName);

            operation.Execute(this.ServiceClient, serviceTable, requestOptions, operationContext);
        }
#endif
        /// <summary>
        /// Begins an asynchronous operation to create a table.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreate(AsyncCallback callback, object state)
        {
            return this.BeginCreate(null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to create a table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreate(TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            requestOptions = TableRequestOptions.ApplyDefaultsAndClearEncryption(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            DynamicTableEntity tblEntity = new DynamicTableEntity();
            tblEntity.Properties.Add(TableConstants.TableName, new EntityProperty(this.Name));
            TableOperation operation = new TableOperation(tblEntity, TableOperationType.Insert, false);
            operation.IsTableEntity = true;
            CloudTable serviceTable = this.ServiceClient.GetTableReference(TableConstants.TableServiceTablesName);

            return operation.BeginExecute(this.ServiceClient, serviceTable, requestOptions, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to create a table.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndCreate(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<TableResult>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to create a table.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync()
        {
            return this.CreateAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a table.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginCreate, this.EndCreate, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.CreateAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginCreate, this.EndCreate, requestOptions, operationContext, cancellationToken);
        }
#endif

        #endregion

        #region CreateIfNotExists
#if SYNC
        /// <summary>
        /// Creates the table if it does not already exist.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if table was created; otherwise, <c>false</c>.</returns>
        /// <remarks>This API performs an existence check and therefore requires list permissions.</remarks>
        [DoesServiceRequest]
        public virtual bool CreateIfNotExists(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            if (this.Exists(true, modifiedOptions, operationContext))
            {
                return false;
            }
            else
            {
                try
                {
                    this.Create(modifiedOptions, operationContext);
                    return true;
                }
                catch (StorageException e)
                {
                    if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
                    {
                        if ((e.RequestInformation.ExtendedErrorInformation == null) ||
                            (e.RequestInformation.ExtendedErrorInformation.ErrorCode == TableErrorCodeStrings.TableAlreadyExists))
                        {
                            return false;
                        }
                        else
                        {
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to create a table if it does not already exist.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>This API performs an existence check and therefore requires list permissions.</remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreateIfNotExists(AsyncCallback callback, object state)
        {
            return this.BeginCreateIfNotExists(null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to create a table if it does not already exist.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>This API performs an existence check and therefore requires list permissions.</remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreateIfNotExists(TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            StorageAsyncResult<bool> storageAsyncResult = new StorageAsyncResult<bool>(callback, state)
            {
                RequestOptions = modifiedOptions,
                OperationContext = operationContext,
            };

            ICancellableAsyncResult currentRes = this.BeginExists(true, modifiedOptions, operationContext, this.CreateIfNotExistHandler, storageAsyncResult);

            // We do not need to do this inside a lock, as storageAsyncResult is
            // not returned to the user yet.
            storageAsyncResult.CancelDelegate = currentRes.Cancel;
            return storageAsyncResult;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        private void CreateIfNotExistHandler(IAsyncResult asyncResult)
        {
            StorageAsyncResult<bool> storageAsyncResult = asyncResult.AsyncState as StorageAsyncResult<bool>;
            bool exists = false;

            lock (storageAsyncResult.CancellationLockerObject)
            {
                storageAsyncResult.CancelDelegate = null;
                storageAsyncResult.UpdateCompletedSynchronously(asyncResult.CompletedSynchronously);

                try
                {
                    exists = this.EndExists(asyncResult);

                    if (exists)
                    {
                        storageAsyncResult.Result = false;
                        storageAsyncResult.OnComplete();
                    }
                    else
                    {
                        ICancellableAsyncResult currentRes = this.BeginCreate(
                             (TableRequestOptions)storageAsyncResult.RequestOptions,
                             storageAsyncResult.OperationContext,
                             createRes =>
                             {
                                 storageAsyncResult.CancelDelegate = null;
                                 storageAsyncResult.UpdateCompletedSynchronously(storageAsyncResult.CompletedSynchronously);

                                 try
                                 {
                                     this.EndCreate(createRes);
                                     storageAsyncResult.Result = true;
                                     storageAsyncResult.OnComplete();
                                 }
                                 catch (StorageException e)
                                 {
                                     if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
                                     {
                                         if ((e.RequestInformation.ExtendedErrorInformation == null) ||
                                             (e.RequestInformation.ExtendedErrorInformation.ErrorCode == TableErrorCodeStrings.TableAlreadyExists))
                                         {
                                             storageAsyncResult.Result = false;
                                             storageAsyncResult.OnComplete();
                                         }
                                         else
                                         {
                                             storageAsyncResult.OnComplete(e);
                                         }
                                     }
                                     else
                                     {
                                         storageAsyncResult.OnComplete(e);
                                     }
                                 }
                                 catch (Exception createEx)
                                 {
                                     storageAsyncResult.OnComplete(createEx);
                                 }
                             },
                             null);

                        storageAsyncResult.CancelDelegate = currentRes.Cancel;
                    }
                }
                catch (Exception ex)
                {
                    storageAsyncResult.OnComplete(ex);
                }
            }
        }

        /// <summary>
        /// Ends an asynchronous operation to create a table if it does not already exist.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if table was created; otherwise, <c>false</c>.</returns>
        public virtual bool EndCreateIfNotExists(IAsyncResult asyncResult)
        {
            StorageAsyncResult<bool> res = asyncResult as StorageAsyncResult<bool>;
            CommonUtility.AssertNotNull("AsyncResult", res);
            res.End();
            return res.Result;
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to create a table if it does not already exist.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        /// <remarks>This API performs an existence check and therefore requires list permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync()
        {
            return this.CreateIfNotExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a table if it does not already exist.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        /// <remarks>This API performs an existence check and therefore requires list permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginCreateIfNotExists, this.EndCreateIfNotExists, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a table if it does not already exist.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        /// <remarks>This API performs an existence check and therefore requires list permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.CreateIfNotExistsAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a table if it does not already exist.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        /// <remarks>This API performs an existence check and therefore requires list permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginCreateIfNotExists, this.EndCreateIfNotExists, requestOptions, operationContext, cancellationToken);
        }
#endif

        #endregion

        #region Delete
#if SYNC
        /// <summary>
        /// Deletes a table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void Delete(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            DynamicTableEntity tblEntity = new DynamicTableEntity();
            tblEntity.Properties.Add(TableConstants.TableName, new EntityProperty(this.Name));
            TableOperation operation = new TableOperation(tblEntity, TableOperationType.Delete);
            operation.IsTableEntity = true;
            CloudTable serviceTable = this.ServiceClient.GetTableReference(TableConstants.TableServiceTablesName);

            operation.Execute(this.ServiceClient, serviceTable, requestOptions, operationContext);
        }
#endif
        /// <summary>
        /// Begins an asynchronous operation to delete a table.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(AsyncCallback callback, object state)
        {
            return this.BeginDelete(null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to delete a table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            DynamicTableEntity tblEntity = new DynamicTableEntity();
            tblEntity.Properties.Add(TableConstants.TableName, new EntityProperty(this.Name));
            TableOperation operation = new TableOperation(tblEntity, TableOperationType.Delete);
            operation.IsTableEntity = true;
            CloudTable serviceTable = this.ServiceClient.GetTableReference(TableConstants.TableServiceTablesName);

            return operation.BeginExecute(this.ServiceClient, serviceTable, requestOptions, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to delete a table.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDelete(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<TableResult>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to delete a table.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
        {
            return this.DeleteAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete a table.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginDelete, this.EndDelete, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete a table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.DeleteAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete a table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginDelete, this.EndDelete, requestOptions, operationContext, cancellationToken);
        }
#endif

        #endregion

        #region DeleteIfExists
#if SYNC
        /// <summary>
        /// Deletes the table if it exists.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the table was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual bool DeleteIfExists(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            if (!this.Exists(true, modifiedOptions, operationContext))
            {
                return false;
            }
            else
            {
                try
                {
                    this.Delete(modifiedOptions, operationContext);
                    return true;
                }
                catch (StorageException e)
                {
                    if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                    {
                        if ((e.RequestInformation.ExtendedErrorInformation == null) ||
                            (e.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ResourceNotFound))
                        {
                            return false;
                        }
                        else
                        {
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to delete the table if it exists.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(AsyncCallback callback, object state)
        {
            return this.BeginDeleteIfExists(null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to delete the table if it exists.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            StorageAsyncResult<bool> storageAsyncResult = new StorageAsyncResult<bool>(callback, state)
            {
                RequestOptions = modifiedOptions,
                OperationContext = operationContext
            };

            ICancellableAsyncResult currentRes = this.BeginExists(true, modifiedOptions, operationContext, this.DeleteIfExistsHandler, storageAsyncResult);

            // We do not need to do this inside a lock, as storageAsyncResult is
            // not returned to the user yet.
            storageAsyncResult.CancelDelegate = currentRes.Cancel;
            return storageAsyncResult;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        private void DeleteIfExistsHandler(IAsyncResult asyncResult)
        {
            StorageAsyncResult<bool> storageAsyncResult = asyncResult.AsyncState as StorageAsyncResult<bool>;
            bool exists = false;
            lock (storageAsyncResult.CancellationLockerObject)
            {
                storageAsyncResult.CancelDelegate = null;
                storageAsyncResult.UpdateCompletedSynchronously(asyncResult.CompletedSynchronously);

                try
                {
                    exists = this.EndExists(asyncResult);

                    if (!exists)
                    {
                        storageAsyncResult.Result = false;
                        storageAsyncResult.OnComplete();
                    }
                    else
                    {
                        ICancellableAsyncResult currentRes = this.BeginDelete(
                            (TableRequestOptions)storageAsyncResult.RequestOptions,
                            storageAsyncResult.OperationContext,
                            (deleteRes) =>
                            {
                                storageAsyncResult.CancelDelegate = null;
                                storageAsyncResult.UpdateCompletedSynchronously(deleteRes.CompletedSynchronously);

                                try
                                {
                                    this.EndDelete(deleteRes);
                                    storageAsyncResult.Result = true;
                                    storageAsyncResult.OnComplete();
                                }
                                catch (StorageException e)
                                {
                                    if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                                    {
                                        if ((e.RequestInformation.ExtendedErrorInformation == null) ||
                                            (e.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ResourceNotFound))
                                        {
                                            storageAsyncResult.Result = false;
                                            storageAsyncResult.OnComplete();
                                        }
                                        else
                                        {
                                            storageAsyncResult.OnComplete(e);
                                        }
                                    }
                                    else
                                    {
                                        storageAsyncResult.OnComplete(e);
                                    }
                                }
                                catch (Exception createEx)
                                {
                                    storageAsyncResult.OnComplete(createEx);
                                }
                            },
                            null);

                        storageAsyncResult.CancelDelegate = currentRes.Cancel;
                    }
                }
                catch (Exception ex)
                {
                    storageAsyncResult.OnComplete(ex);
                }
            }
        }

        /// <summary>
        /// Ends an asynchronous operation to delete the table if it exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the table was deleted; otherwise, <c>false</c>.</returns>
        public virtual bool EndDeleteIfExists(IAsyncResult asyncResult)
        {
            StorageAsyncResult<bool> res = asyncResult as StorageAsyncResult<bool>;
            CommonUtility.AssertNotNull("AsyncResult", res);
            res.End();
            return res.Result;
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to delete the table if it exists.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
        {
            return this.DeleteIfExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the table if it exists.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDeleteIfExists, this.EndDeleteIfExists, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the table if it exists.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the table if it exists.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDeleteIfExists, this.EndDeleteIfExists, requestOptions, operationContext, cancellationToken);
        }
#endif
        #endregion

        #region Exists
#if SYNC
        /// <summary>
        /// Checks whether the table exists.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if table exists; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual bool Exists(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            return this.Exists(false, requestOptions, operationContext);
        }

        /// <summary>
        /// Checks whether the table exists.
        /// </summary>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if table exists; otherwise, <c>false</c>.</returns>
        private bool Exists(bool primaryOnly, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            requestOptions = TableRequestOptions.ApplyDefaultsAndClearEncryption(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            DynamicTableEntity tblEntity = new DynamicTableEntity();
            tblEntity.Properties.Add(TableConstants.TableName, new EntityProperty(this.Name));
            TableOperation operation = new TableOperation(tblEntity, TableOperationType.Retrieve);
            operation.IsTableEntity = true;
            operation.IsPrimaryOnlyRetrieve = primaryOnly;
            CloudTable serviceTable = this.ServiceClient.GetTableReference(TableConstants.TableServiceTablesName);

            TableResult res = operation.Execute(this.ServiceClient, serviceTable, requestOptions, operationContext);

            // Only other option is not found, other status codes will throw prior to this.            
            return res.HttpStatusCode == (int)HttpStatusCode.OK;
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to determine whether a table exists.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExists(AsyncCallback callback, object state)
        {
            return this.BeginExists(null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to determine whether a table exists.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExists(TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginExists(false, requestOptions, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to determine whether a table exists.
        /// </summary>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        private ICancellableAsyncResult BeginExists(bool primaryOnly, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            requestOptions = TableRequestOptions.ApplyDefaultsAndClearEncryption(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            DynamicTableEntity tblEntity = new DynamicTableEntity();
            tblEntity.Properties.Add(TableConstants.TableName, new EntityProperty(this.Name));
            TableOperation operation = new TableOperation(tblEntity, TableOperationType.Retrieve);
            operation.IsTableEntity = true;
            operation.IsPrimaryOnlyRetrieve = primaryOnly;
            CloudTable serviceTable = this.ServiceClient.GetTableReference(TableConstants.TableServiceTablesName);

            return operation.BeginExecute(this.ServiceClient, serviceTable, requestOptions, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to determine whether a table exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if table exists; otherwise, <c>false</c>.</returns>
        public virtual bool EndExists(IAsyncResult asyncResult)
        {
            TableResult res = Executor.EndExecuteAsync<TableResult>(asyncResult);

            // Only other option is not found, other status codes will throw prior to this.            
            return res.HttpStatusCode == (int)HttpStatusCode.OK;
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to determine whether a table exists.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
        {
            return this.ExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to determine whether a table exists.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExists, this.EndExists, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to determine whether a table exists.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExistsAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to determine whether a table exists.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExists, this.EndExists, requestOptions, operationContext, cancellationToken);
        }
#endif

        #endregion

        #region Permissions
#if SYNC
        /// <summary>
        /// Gets the permissions settings for the table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="TablePermissions"/> object.</returns>
        [DoesServiceRequest]
        public virtual TablePermissions GetPermissions(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteSync(this.GetAclImpl(requestOptions), requestOptions.RetryPolicy, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous request to get the permissions settings for the table.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetPermissions(AsyncCallback callback, object state)
        {
            return this.BeginGetPermissions(null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to get the permissions settings for the table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetPermissions(TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            return Executor.BeginExecuteAsync(
                                            this.GetAclImpl(requestOptions),
                                            requestOptions.RetryPolicy,
                                            operationContext,
                                            callback,
                                            state);
        }

        /// <summary>
        /// Returns the asynchronous result of the request to get the permissions settings for the table.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="TablePermissions"/> object.</returns>
        public virtual TablePermissions EndGetPermissions(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<TablePermissions>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to get the permissions settings for the table.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TablePermissions"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TablePermissions> GetPermissionsAsync()
        {
            return this.GetPermissionsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get the permissions settings for the table.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TablePermissions"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TablePermissions> GetPermissionsAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginGetPermissions, this.EndGetPermissions, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get the permissions settings for the table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TablePermissions"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TablePermissions> GetPermissionsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.GetPermissionsAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get the permissions settings for the table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="TablePermissions"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TablePermissions> GetPermissionsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginGetPermissions, this.EndGetPermissions, requestOptions, operationContext, cancellationToken);
        }
#endif

        private RESTCommand<TablePermissions> GetAclImpl(TableRequestOptions requestOptions)
        {
            RESTCommand<TablePermissions> retCmd = new RESTCommand<TablePermissions>(this.ServiceClient.Credentials, this.StorageUri);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequestDelegate = TableHttpWebRequestFactory.GetAcl;
            retCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            retCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(System.Net.HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);

            retCmd.PostProcessResponse = this.ParseGetAcl;
            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }

        private TablePermissions ParseGetAcl(RESTCommand<TablePermissions> cmd, HttpWebResponse resp, OperationContext ctx)
        {
            TablePermissions tableAcl = new TablePermissions();

            // Get the policies from the web response.
            TableHttpWebResponseParsers.ReadSharedAccessIdentifiers(cmd.ResponseStream, tableAcl);

            return tableAcl;
        }

#if SYNC
        /// <summary>
        /// Sets the permissions settings for the table.
        /// </summary>
        /// <param name="permissions">A <see cref="TablePermissions"/> object that represents the permissions to set.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void SetPermissions(TablePermissions permissions, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            Executor.ExecuteSync(this.SetAclImpl(permissions, requestOptions), requestOptions.RetryPolicy, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous request to set permissions for the table.
        /// </summary>
        /// <param name="permissions">A <see cref="TablePermissions"/> object that represents the permissions to set.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetPermissions(TablePermissions permissions, AsyncCallback callback, object state)
        {
            return this.BeginSetPermissions(permissions, null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to set permissions for the table.
        /// </summary>
        /// <param name="permissions">A <see cref="TablePermissions"/> object that represents the permissions to set.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetPermissions(TablePermissions permissions, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            return Executor.BeginExecuteAsync(
                                            this.SetAclImpl(permissions, requestOptions),
                                            requestOptions.RetryPolicy,
                                            operationContext,
                                            callback,
                                            state);
        }

        /// <summary>
        /// Returns the asynchronous result of the request to get the permissions settings for the table.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>        
        public virtual void EndSetPermissions(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to set permissions for the table.
        /// </summary>
        /// <param name="permissions">A <see cref="TablePermissions"/> object that represents the permissions to set.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(TablePermissions permissions)
        {
            return this.SetPermissionsAsync(permissions, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to set permissions for the table.
        /// </summary>
        /// <param name="permissions">A <see cref="TablePermissions"/> object that represents the permissions to set.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(TablePermissions permissions, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetPermissions, this.EndSetPermissions, permissions, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to set permissions for the table.
        /// </summary>
        /// <param name="permissions">A <see cref="TablePermissions"/> object that represents the permissions to set.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(TablePermissions permissions, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.SetPermissionsAsync(permissions, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to set permissions for the table.
        /// </summary>
        /// <param name="permissions">A <see cref="TablePermissions"/> object that represents the permissions to set.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(TablePermissions permissions, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetPermissions, this.EndSetPermissions, permissions, requestOptions, operationContext, cancellationToken);
        }
#endif

        private RESTCommand<NullType> SetAclImpl(TablePermissions permissions, TableRequestOptions requestOptions)
        {
            MultiBufferMemoryStream str = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            TableRequest.WriteSharedAccessIdentifiers(permissions.SharedAccessPolicies, str);
            str.Seek(0, SeekOrigin.Begin);

            RESTCommand<NullType> retCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);
            retCmd.BuildRequestDelegate = TableHttpWebRequestFactory.SetAcl;
            retCmd.SendStream = str;
            retCmd.StreamToDispose = str;
            retCmd.RecoveryAction = RecoveryActions.RewindStream;
            retCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            retCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);

            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }

        #endregion
    }
}