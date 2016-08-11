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

    public partial class CloudTable
    {
        #region TableOperation Execute Methods
        /// <summary>
        /// Executes the operation on a table.
        /// </summary>
        /// <param name="operation">A <see cref="TableOperation"/> object that represents the operation to perform.</param>
        /// <returns>A <see cref="TableResult"/> containing the result of executing the operation on the table.</returns>
        [DoesServiceRequest]
        public virtual Task<TableResult> ExecuteAsync(TableOperation operation)
        {
            return this.ExecuteAsync(operation, null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Executes the operation on a table, using the specified <see cref="TableRequestOptions"/> and <see cref="OperationContext"/>.
        /// </summary>
        /// <param name="operation">A <see cref="TableOperation"/> object that represents the operation to perform.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>A <see cref="TableResult"/> containing the result of executing the operation on the table.</returns>
        [DoesServiceRequest]
        public virtual Task<TableResult> ExecuteAsync(TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExecuteAsync(operation, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Executes the operation on a table, using the specified <see cref="TableRequestOptions"/> and <see cref="OperationContext"/>.
        /// </summary>
        /// <param name="operation">A <see cref="TableOperation"/> object that represents the operation to perform.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>A <see cref="TableResult"/> containing the result of executing the operation on the table.</returns>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public Task<TableResult> ExecuteAsync(TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("operation", operation);

            return operation.ExecuteAsync(this.ServiceClient, this.Name, requestOptions, operationContext, cancellationToken);
        }

        #endregion

        #region TableBatchOperation Execute Methods
        /// <summary>
        /// Executes a batch operation on a table as an atomic operation.
        /// </summary>
        /// <param name="batch">The <see cref="TableBatchOperation"/> object representing the operations to execute on the table.</param>
        /// <returns>An enumerable collection of <see cref="TableResult"/> objects that contains the results, in order, of each operation in the <see cref="TableBatchOperation"/> on the table.</returns>
        [DoesServiceRequest]
        public Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batch)
        {
            return this.ExecuteBatchAsync(batch, null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Executes a batch operation on a table as an atomic operation, using the specified <see cref="TableRequestOptions"/> and <see cref="OperationContext"/>.
        /// </summary>
        /// <param name="batch">The <see cref="TableBatchOperation"/> object representing the operations to execute on the table.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>An enumerable collection of <see cref="TableResult"/> objects that contains the results, in order, of each operation in the <see cref="TableBatchOperation"/> on the table.</returns>
        [DoesServiceRequest]
        public Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batch, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExecuteBatchAsync(batch, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Executes a batch operation on a table as an atomic operation, using the specified <see cref="TableRequestOptions"/> and <see cref="OperationContext"/>.
        /// </summary>
        /// <param name="batch">The <see cref="TableBatchOperation"/> object representing the operations to execute on the table.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>An enumerable collection of <see cref="TableResult"/> objects that contains the results, in order, of each operation in the <see cref="TableBatchOperation"/> on the table.</returns>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batch, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("batch", batch);
            return batch.ExecuteAsync(this.ServiceClient, this.Name, requestOptions, operationContext, cancellationToken);
        }

        #endregion

        #region TableQuery Execute Methods
        internal IEnumerable<DynamicTableEntity> ExecuteQuery(TableQuery query)
        {
            return this.ExecuteQuery(query, null /* RequestOptions */, null /* OperationContext */);
        }

        internal IEnumerable<DynamicTableEntity> ExecuteQuery(TableQuery query, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.Execute(this.ServiceClient, this.Name, requestOptions, operationContext);
        }

        /// <summary>
        /// Executes a query in segmented mode with the specified <see cref="TableContinuationToken"/> continuation token.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="ResultContinuation"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <returns>A <see cref="TableQuerySegment"/> object containing the results of executing the query.</returns>        
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token)
        {
            return this.ExecuteQuerySegmentedAsync(query, token, null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Executes a query in segmented mode with the specified <see cref="TableContinuationToken"/> continuation token, <see cref="TableRequestOptions"/>, and <see cref="OperationContext"/>.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="ResultContinuation"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>A <see cref="TableQuerySegment"/> object containing the results of executing the query.</returns>        
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExecuteQuerySegmentedAsync(query, token, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Executes a query in segmented mode with the specified <see cref="TableContinuationToken"/> continuation token, <see cref="TableRequestOptions"/>, and <see cref="OperationContext"/>.
        /// </summary>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="ResultContinuation"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="TableQuerySegment"/> object containing the results of executing the query.</returns>        
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.ExecuteQuerySegmentedAsync(token, this.ServiceClient, this.Name, requestOptions, operationContext, cancellationToken);
        }
        #endregion

        #region Create

        /// <summary>
        /// Creates the Table.
        /// </summary>
        [DoesServiceRequest]
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        public virtual Task CreateAsync()
        {
            return this.CreateAsync(null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Creates the Table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.CreateAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates the Table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            DynamicTableEntity tblEntity = new DynamicTableEntity();
            tblEntity.Properties.Add(TableConstants.TableName, new EntityProperty(this.Name));
            TableOperation operation = new TableOperation(tblEntity, TableOperationType.Insert, false);
            operation.IsTableEntity = true;

            return this.ServiceClient.ExecuteAsync(TableConstants.TableServiceTablesName, operation, requestOptions, operationContext, cancellationToken);
        }

        #endregion

        #region CreateIfNotExists

        /// <summary>
        /// Creates the table if it does not already exist.
        /// </summary>
        /// <returns><c>true</c> if table was created; otherwise, <c>false</c>.</returns>
        /// <remarks>This API performs an existence check and therefore requires list permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync()
        {
            return this.CreateIfNotExistsAsync(null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Creates the table if it does not already exist.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>        
        /// <returns><c>true</c> if table was created; otherwise, <c>false</c>.</returns>
        /// <remarks>This API performs an existence check and therefore requires list permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.CreateIfNotExistsAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates the table if it does not already exist.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>        
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if table was created; otherwise, <c>false</c>.</returns>
        /// <remarks>This API performs an existence check and therefore requires list permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () =>
            {
                if (await this.ExistsAsync(true, requestOptions, operationContext, cancellationToken))
                {
                    return false;
                }
                else
                {
                    try
                    {
                        await this.CreateAsync(requestOptions, operationContext, cancellationToken);
                        return true;
                    }
                    catch (Exception)
                    {
                        if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.Conflict)
                        {
                            StorageExtendedErrorInformation extendedInfo = operationContext.LastResult.ExtendedErrorInformation;
                            if ((extendedInfo == null) ||
                                (extendedInfo.ErrorCode == TableErrorCodeStrings.TableAlreadyExists))
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
            }, cancellationToken);
        }
        #endregion

        #region Delete

        /// <summary>
        /// Deletes the Table.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
        {
            return this.DeleteAsync(null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Deletes the Table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.DeleteAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the Table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            DynamicTableEntity tblEntity = new DynamicTableEntity();
            tblEntity.Properties.Add(TableConstants.TableName, new EntityProperty(this.Name));
            TableOperation operation = new TableOperation(tblEntity, TableOperationType.Delete);
            operation.IsTableEntity = true;

            return this.ServiceClient.ExecuteAsync(TableConstants.TableServiceTablesName, operation, requestOptions, operationContext, cancellationToken);
        }
        #endregion

        #region DeleteIfExists

        /// <summary>
        /// Deletes the table if it already exists.
        /// </summary>
        /// <returns><c>true</c> if the table already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
        {
            return this.DeleteIfExistsAsync(null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Deletes the table if it already exists.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns><c>true</c> if the table already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the table if it already exists.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns><c>true</c> if the table already existed and was deleted; otherwise, <c>false</c>.</returns>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);

            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () =>
            {
                if (!await this.ExistsAsync(true, requestOptions, operationContext, cancellationToken))
                {
                    return false;
                }
                else
                {
                    try
                    {
                        await this.DeleteAsync(requestOptions, operationContext, cancellationToken);
                        return true;
                    }
                    catch (Exception)
                    {
                        if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.NotFound)
                        {
                            StorageExtendedErrorInformation extendedInfo = operationContext.LastResult.ExtendedErrorInformation;
                            if ((extendedInfo == null) ||
                                (extendedInfo.ErrorCode == StorageErrorCodeStrings.ResourceNotFound))
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
            }, cancellationToken);
        }
        #endregion

        #region Exists
        /// <summary>
        /// Checks existence of the table.
        /// </summary>
        /// <returns><c>true</c> if the table exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
        {
            return this.ExistsAsync(null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Checks existence of the table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns><c>true</c> if the table exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExistsAsync(false, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Checks existence of the table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the table exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.ExistsAsync(false, requestOptions, operationContext, cancellationToken);
        }

        /// <summary>
        /// Checks existence of the table.
        /// </summary>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the table exists.</returns>
        [DoesServiceRequest]
        private Task<bool> ExistsAsync(bool primaryOnly, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            DynamicTableEntity tblEntity = new DynamicTableEntity();
            tblEntity.Properties.Add(TableConstants.TableName, new EntityProperty(this.Name));
            TableOperation operation = new TableOperation(tblEntity, TableOperationType.Retrieve);
            operation.IsTableEntity = true;
            operation.IsPrimaryOnlyRetrieve = primaryOnly;

            return Task.Run(async () =>
            {
                TableResult res = await this.ServiceClient.ExecuteAsync(TableConstants.TableServiceTablesName, operation, requestOptions, operationContext, cancellationToken);

                // Only other option is not found, other status codes will throw prior to this.            
                return res.HttpStatusCode == (int)HttpStatusCode.OK;
            }, cancellationToken);
        }
        #endregion

        #region Permissions
        /// <summary>
        /// Sets permissions for the Table.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the Table.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(TablePermissions permissions)
        {
            return this.SetPermissionsAsync(permissions, null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Sets permissions for the Table.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the Table.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(TablePermissions permissions, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.SetPermissionsAsync(permissions, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Sets permissions for the Table.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the Table.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(TablePermissions permissions, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                                                            this.SetPermissionsImpl(permissions, modifiedOptions),
                                                            modifiedOptions.RetryPolicy,
                                                            operationContext,
                                                            cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Implementation for the SetPermissions method.
        /// </summary>
        /// <param name="acl">The permissions to set.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the permissions.</returns>
        private RESTCommand<NullType> SetPermissionsImpl(TablePermissions acl, TableRequestOptions requestOptions)
        {
            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            TableRequest.WriteSharedAccessIdentifiers(acl.SharedAccessPolicies, memoryStream);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            requestOptions.ApplyToStorageCommand(putCmd);
            putCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableHttpRequestMessageFactory.SetAcl(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null /* md5 */, cmd, ctx);
            putCmd.StreamToDispose = memoryStream;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Gets the permissions settings for the Table.
        /// </summary>
        /// <returns>The Table's permissions.</returns>
        [DoesServiceRequest]
        public virtual Task<TablePermissions> GetPermissionsAsync()
        {
            return this.GetPermissionsAsync(null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Gets the permissions settings for the Table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>The Table's permissions.</returns>
        [DoesServiceRequest]
        public virtual Task<TablePermissions> GetPermissionsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.GetPermissionsAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the permissions settings for the Table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The Table's permissions.</returns>
        [DoesServiceRequest]
        public virtual Task<TablePermissions> GetPermissionsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsync(
                                                        this.GetPermissionsImpl(modifiedOptions),
                                                        modifiedOptions.RetryPolicy,
                                                        operationContext,
                                                        cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Implementation for the GetPermissions method.
        /// </summary>
        /// <param name="requestOptions">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that gets the permissions.</returns>
        private RESTCommand<TablePermissions> GetPermissionsImpl(TableRequestOptions requestOptions)
        {
            RESTCommand<TablePermissions> getCmd = new RESTCommand<TablePermissions>(this.ServiceClient.Credentials, this.StorageUri);

            requestOptions.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableHttpRequestMessageFactory.GetAcl(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task<TablePermissions>.Factory.StartNew(() =>
                {
                    TablePermissions TableAcl = new TablePermissions();
                    HttpResponseParsers.ReadSharedAccessIdentifiers(TableAcl.SharedAccessPolicies, new TableAccessPolicyResponse(cmd.ResponseStream));
                    return TableAcl;
                });
            };

            return getCmd;
        }
        #endregion

        #region TableQuery Execute Methods
        /// <summary>
        /// Executes a query asynchronously in segmented mode with the specified <see cref="TableQuery{T}"/> query and <see cref="TableContinuationToken"/> continuation token.
        /// </summary>
        /// <typeparam name="T">The entity type of the query.</typeparam>
        /// <param name="table">The input <see cref="CloudTable"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="query">A <see cref="TableQuery{T}"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <returns>A <see cref="TableQuerySegment{T}"/> object containing the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<T>> ExecuteQuerySegmentedAsync<T>(TableQuery<T> query, TableContinuationToken token) where T : ITableEntity, new()
        {
            return this.ExecuteQuerySegmentedAsync<T>(query, token, null /* requestOptions */, null /* operationContext */);
        }

        /// <summary>
        /// Executes a query asynchronously in segmented mode with the specified <see cref="TableQuery{T}"/> query, <see cref="TableContinuationToken"/> continuation token, <see cref="TableRequestOptions"/> options, and <see cref="OperationContext"/> context.
        /// </summary>
        /// <typeparam name="T">The entity type of the query.</typeparam>
        /// <param name="table">The input <see cref="CloudTable"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="query">A <see cref="TableQuery{T}"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>A <see cref="TableQuerySegment{T}"/> object containing the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<T>> ExecuteQuerySegmentedAsync<T>(TableQuery<T> query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext) where T : ITableEntity, new()
        {
            return this.ExecuteQuerySegmentedAsync<T>(query, token, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Executes a query asynchronously in segmented mode with the specified <see cref="TableQuery{T}"/> query, <see cref="TableContinuationToken"/> continuation token, <see cref="TableRequestOptions"/> options, and <see cref="OperationContext"/> context.
        /// </summary>
        /// <typeparam name="T">The entity type of the query.</typeparam>
        /// <param name="table">The input <see cref="CloudTable"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="query">A <see cref="TableQuery{T}"/> representing the query to execute.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="TableQuerySegment{T}"/> object containing the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<T>> ExecuteQuerySegmentedAsync<T>(TableQuery<T> query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where T : ITableEntity, new()
        {
            CommonUtility.AssertNotNull("query", query);
            return query.ExecuteQuerySegmentedAsync(token, this.ServiceClient, this.Name, requestOptions, operationContext, cancellationToken);
        }

        /// <summary>
        /// Executes a query asynchronously in segmented mode, using the specified <see cref="TableQuery{T}"/> query and <see cref="TableContinuationToken"/> continuation token, and applies the <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="T">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="table">The input <see cref="CloudTable"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="query">A <see cref="TableQuery{T}"/> representing the query to execute.</param>
        /// <param name="resolver">An <see cref="EntityResolver{R}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <returns>A <see cref="TableQuerySegment{R}"/> containing the projection into type <c>TResult</c> of the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<T, TResult>(TableQuery<T> query, EntityResolver<TResult> resolver, TableContinuationToken token) where T : ITableEntity, new()
        {
            return this.ExecuteQuerySegmentedAsync<T, TResult>(query, resolver, token, null /* requestOptions */, null /* operationContext */);
        }

        /// <summary>
        /// Executes a query asynchronously in segmented mode, using the specified <see cref="TableQuery{T}"/> query, <see cref="TableContinuationToken"/> continuation token, <see cref="TableRequestOptions"/> options, and <see cref="OperationContext"/> context, and applies the <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="T">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="table">The input <see cref="CloudTable"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="query">A <see cref="TableQuery{T}"/> representing the query to execute.</param>
        /// <param name="resolver">An <see cref="EntityResolver{R}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>A <see cref="TableQuerySegment{R}"/> containing the projection into type <c>TResult</c> of the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<T, TResult>(TableQuery<T> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext) where T : ITableEntity, new()
        {
            return this.ExecuteQuerySegmentedAsync<T, TResult>(query, resolver, token, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Executes a query asynchronously in segmented mode, using the specified <see cref="TableQuery{T}"/> query, <see cref="TableContinuationToken"/> continuation token, <see cref="TableRequestOptions"/> options, and <see cref="OperationContext"/> context, and applies the <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="T">The entity type of the query.</typeparam>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="table">The input <see cref="CloudTable"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="query">A <see cref="TableQuery{T}"/> representing the query to execute.</param>
        /// <param name="resolver">An <see cref="EntityResolver{R}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="TableQuerySegment{R}"/> containing the projection into type <c>TResult</c> of the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<T, TResult>(TableQuery<T> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where T : ITableEntity, new()
        {
            CommonUtility.AssertNotNull("query", query);
            CommonUtility.AssertNotNull("resolver", resolver);
            return query.ExecuteQuerySegmentedAsync(token, this.ServiceClient, this.Name, resolver, requestOptions, operationContext, cancellationToken);
        }

        /// <summary>
        /// Executes a query asynchronously in segmented mode, using the specified <see cref="TableContinuationToken"/> continuation token, and applies the <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="table">The input <see cref="CloudTable"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="resolver">An <see cref="EntityResolver{R}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <returns>A <see cref="TableQuerySegment{R}"/> containing the projection into type <c>TResult</c> of the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token)
        {
            return this.ExecuteQuerySegmentedAsync<TResult>(query, resolver, token, null /* requestOptions */, null /* operationContext */);
        }

        /// <summary>
        /// Executes a query asynchronously in segmented mode, using the specified <see cref="TableContinuationToken"/> continuation token, <see cref="TableRequestOptions"/> options, and <see cref="OperationContext"/> context, and applies the <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="table">The input <see cref="CloudTable"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="resolver">An <see cref="EntityResolver{R}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>A <see cref="TableQuerySegment{R}"/> containing the projection into type <c>TResult</c> of the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExecuteQuerySegmentedAsync<TResult>(query, resolver, token, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Executes a query asynchronously in segmented mode, using the specified <see cref="TableContinuationToken"/> continuation token, <see cref="TableRequestOptions"/> options, and <see cref="OperationContext"/> context, and applies the <see cref="EntityResolver{T}"/> to the result.
        /// </summary>
        /// <typeparam name="TResult">The type into which the <see cref="EntityResolver{T}"/> will project the query results.</typeparam>
        /// <param name="table">The input <see cref="CloudTable"/>, which acts as the <c>this</c> instance for the extension method.</param>
        /// <param name="query">A <see cref="TableQuery"/> representing the query to execute.</param>
        /// <param name="resolver">An <see cref="EntityResolver{R}"/> instance which creates a projection of the table query result entities into the specified type <c>TResult</c>.</param>
        /// <param name="token">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="TableQuerySegment{R}"/> containing the projection into type <c>TResult</c> of the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("query", query);
            CommonUtility.AssertNotNull("resolver", resolver);
            return query.ExecuteQuerySegmentedAsync<TResult>(token, this.ServiceClient, this.Name, resolver, requestOptions, operationContext, cancellationToken);
        }

        #endregion
    }
}
