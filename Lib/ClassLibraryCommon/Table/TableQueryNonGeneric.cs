// -----------------------------------------------------------------------------------------
// <copyright file="TableQueryNonGeneric.cs" company="Microsoft">
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

    public partial class TableQuery
    {
        /// <summary>
        /// Represents a query against a specified table.
        /// </summary>
        /// <remarks>A <see cref="TableQuery"/> instance aggregates the query parameters to use when the query is executed. One of the <c>executeQuery</c> or <c>executeQuerySegmented</c> methods 
        /// of <see cref="CloudTableClient"/> must be called to execute the query. The parameters are encoded and passed to the server when the table query is executed.</remarks>
        public TableQuery()
        {
        }

        /// <summary>
        /// Specifies the names of the entity properties to return when the query is executed against the table. 
        /// </summary>
        /// <remarks>The Project clause is optional on a query, used to limit the properties returned from the server. By default, a query will return all properties from the entity.</remarks>
        /// <typeparam name="T">The entity type of the query.</typeparam>       
        /// <param name="entity">The entity instance to project off of.</param>
        /// <param name="columns">A list of string objects containing the names of the entity properties to return when the query is executed.</param>
        /// <returns>A <see cref="TableQuery"/> instance set with the entity properties to return.</returns>
        public static T Project<T>(T entity, params string[] columns)
        {
            return entity;
        }

        #region Impl
#if SYNC
        internal IEnumerable<DynamicTableEntity> Execute(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);

            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            IEnumerable<DynamicTableEntity> enumerable = CommonUtility.LazyEnumerable<DynamicTableEntity>(
                (continuationToken) =>
                {
                    TableQuerySegment<DynamicTableEntity> seg = this.ExecuteQuerySegmented((TableContinuationToken)continuationToken, client, table, modifiedOptions, operationContext);

                    return new ResultSegment<DynamicTableEntity>(seg.Results) { ContinuationToken = seg.ContinuationToken };
                },
                this.takeCount.HasValue ? this.takeCount.Value : long.MaxValue);

            return enumerable;
        }

        internal IEnumerable<TResult> Execute<TResult>(CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);

            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            IEnumerable<TResult> enumerable = CommonUtility.LazyEnumerable<TResult>(
                (continuationToken) =>
                {
                    TableQuerySegment<TResult> seg = this.ExecuteQuerySegmented((TableContinuationToken)continuationToken, client, table, resolver, modifiedOptions, operationContext);

                    return new ResultSegment<TResult>(seg.Results) { ContinuationToken = seg.ContinuationToken };
                },
                this.takeCount.HasValue ? this.takeCount.Value : long.MaxValue);

            return enumerable;
        }

        internal TableQuerySegment<DynamicTableEntity> ExecuteQuerySegmented(TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);

            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            RESTCommand<TableQuerySegment<DynamicTableEntity>> cmdToExecute = QueryImpl(this, token, client, table, modifiedOptions);

            return Executor.ExecuteSync(cmdToExecute, modifiedOptions.RetryPolicy, operationContext);
        }

        internal TableQuerySegment<TResult> ExecuteQuerySegmented<TResult>(TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
            CommonUtility.AssertNotNull("resolver", resolver);

            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            RESTCommand<TableQuerySegment<TResult>> cmdToExecute = QueryImpl(this, token, client, table, resolver, modifiedOptions);

            return Executor.ExecuteSync(cmdToExecute, modifiedOptions.RetryPolicy, operationContext);
        }
#endif

        [DoesServiceRequest]
        internal ICancellableAsyncResult BeginExecuteQuerySegmented(TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);

            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            return Executor.BeginExecuteAsync(
                                          QueryImpl(this, token, client, table, modifiedOptions),
                                          modifiedOptions.RetryPolicy,
                                          operationContext,
                                          callback,
                                          state);
        }

        [DoesServiceRequest]
        internal ICancellableAsyncResult BeginExecuteQuerySegmented<TResult>(TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);

            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            return Executor.BeginExecuteAsync(
                                          QueryImpl(this, token, client, table, resolver, modifiedOptions),
                                          modifiedOptions.RetryPolicy,
                                          operationContext,
                                          callback,
                                          state);
        }

        internal TableQuerySegment<DynamicTableEntity> EndExecuteQuerySegmented(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<TableQuerySegment<DynamicTableEntity>>(asyncResult);
        }

        private static RESTCommand<TableQuerySegment<DynamicTableEntity>> QueryImpl(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
        {
            requestOptions.AssertPolicyIfRequired();

            // If encryption policy is set, then add the encryption metadata column to Select columns in order to be able to decrypt properties.
            if (requestOptions.EncryptionPolicy != null && query.SelectColumns != null)
            {
                query.SelectColumns.Add(Constants.EncryptionConstants.TableEncryptionKeyDetails);
                query.SelectColumns.Add(Constants.EncryptionConstants.TableEncryptionPropertyDetails);
            }

            UriQueryBuilder builder = query.GenerateQueryBuilder(requestOptions.ProjectSystemProperties);

            if (token != null)
            {
                token.ApplyToUriQueryBuilder(builder);
            }

            StorageUri tempUri = NavigationHelper.AppendPathToUri(client.StorageUri, table.Name);
            RESTCommand<TableQuerySegment<DynamicTableEntity>> queryCmd = new RESTCommand<TableQuerySegment<DynamicTableEntity>>(client.Credentials, tempUri);
            requestOptions.ApplyToStorageCommand(queryCmd);

            queryCmd.CommandLocationMode = CommonUtility.GetListingLocationMode(token);
            queryCmd.RetrieveResponseStream = true;
            queryCmd.SignRequest = client.AuthenticationHandler.SignRequest;
            queryCmd.Builder = builder;
            queryCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            queryCmd.BuildRequestDelegate = (uri, queryBuilder, timeout, useVersionHeader, ctx) => TableOperationHttpWebRequestFactory.BuildRequestForTableQuery(uri, queryBuilder, timeout, useVersionHeader, ctx, requestOptions.PayloadFormat.Value);
            queryCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp != null ? resp.StatusCode : HttpStatusCode.Unused, null /* retVal */, cmd, ex);
            queryCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                ResultSegment<DynamicTableEntity> resSeg = TableOperationHttpResponseParsers.TableQueryPostProcessGeneric<DynamicTableEntity, DynamicTableEntity>(cmd.ResponseStream, EntityUtilities.ResolveDynamicEntity, resp, requestOptions, ctx, client.AccountName);
                if (resSeg.ContinuationToken != null)
                {
                    resSeg.ContinuationToken.TargetLocation = cmd.CurrentResult.TargetLocation;
                }

                return new TableQuerySegment<DynamicTableEntity>(resSeg);
            };

            return queryCmd;
        }

        private static RESTCommand<TableQuerySegment<RESULT_TYPE>> QueryImpl<RESULT_TYPE>(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<RESULT_TYPE> resolver, TableRequestOptions requestOptions)
        {
            UriQueryBuilder builder = query.GenerateQueryBuilder(requestOptions.ProjectSystemProperties);

            if (token != null)
            {
                token.ApplyToUriQueryBuilder(builder);
            }

            StorageUri tempUri = NavigationHelper.AppendPathToUri(client.StorageUri, table.Name);
            RESTCommand<TableQuerySegment<RESULT_TYPE>> queryCmd = new RESTCommand<TableQuerySegment<RESULT_TYPE>>(client.Credentials, tempUri);
            requestOptions.ApplyToStorageCommand(queryCmd);

            queryCmd.CommandLocationMode = CommonUtility.GetListingLocationMode(token);
            queryCmd.RetrieveResponseStream = true;
            queryCmd.SignRequest = client.AuthenticationHandler.SignRequest;
            queryCmd.Builder = builder;
            queryCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            queryCmd.BuildRequestDelegate = (uri, queryBuilder, timeout, useVersionHeader, ctx) => TableOperationHttpWebRequestFactory.BuildRequestForTableQuery(uri, queryBuilder, timeout, useVersionHeader, ctx, requestOptions.PayloadFormat.Value);
            queryCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp != null ? resp.StatusCode : HttpStatusCode.Unused, null /* retVal */, cmd, ex);
            queryCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                ResultSegment<RESULT_TYPE> resSeg = TableOperationHttpResponseParsers.TableQueryPostProcessGeneric<RESULT_TYPE, DynamicTableEntity>(cmd.ResponseStream, resolver.Invoke, resp, requestOptions, ctx, client.AccountName);
                if (resSeg.ContinuationToken != null)
                {
                    resSeg.ContinuationToken.TargetLocation = cmd.CurrentResult.TargetLocation;
                }

                return new TableQuerySegment<RESULT_TYPE>(resSeg);
            };

            return queryCmd;
        }
        #endregion
    }
}
