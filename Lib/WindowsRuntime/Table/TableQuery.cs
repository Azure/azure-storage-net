// -----------------------------------------------------------------------------------------
// <copyright file="TableQuery.cs" company="Microsoft">
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

    public sealed partial class TableQuery
    {
        #region Impl
        internal IEnumerable<DynamicTableEntity> Execute(CloudTableClient client, string tableName, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", tableName);
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            IEnumerable<DynamicTableEntity> enumerable =
                CommonUtility.LazyEnumerable<DynamicTableEntity>(
                (continuationToken) =>
                {
                    TableQuerySegment seg = CommonUtility.RunWithoutSynchronizationContext(() => this.ExecuteQuerySegmentedAsync((TableContinuationToken)continuationToken, client, tableName, modifiedOptions, operationContext).Result);

                    return new ResultSegment<DynamicTableEntity>((List<DynamicTableEntity>)seg.Results) { ContinuationToken = seg.ContinuationToken };
                },
                this.takeCount.HasValue ? this.takeCount.Value : long.MaxValue);

            return enumerable;
        }

        internal Task<TableQuerySegment> ExecuteQuerySegmentedAsync(TableContinuationToken continuationToken, CloudTableClient client, string tableName, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return ExecuteQuerySegmentedAsync(continuationToken, client, tableName, requestOptions, operationContext, CancellationToken.None);
        }

        internal Task<TableQuerySegment> ExecuteQuerySegmentedAsync(TableContinuationToken continuationToken, CloudTableClient client, string tableName, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", tableName);
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            RESTCommand<TableQuerySegment> cmdToExecute = QueryImpl(this, continuationToken, client, tableName, EntityUtilities.ResolveEntityByType<DynamicTableEntity>, modifiedOptions);

            return Task.Run(async () => await Executor.ExecuteAsync(
                                                            cmdToExecute,
                                                            modifiedOptions.RetryPolicy,
                                                            operationContext,
                                                            cancellationToken), cancellationToken);
        }

        private static RESTCommand<TableQuerySegment> QueryImpl(TableQuery query, TableContinuationToken token, CloudTableClient client, string tableName, EntityResolver<DynamicTableEntity> resolver, TableRequestOptions requestOptions)
        {
            UriQueryBuilder builder = query.GenerateQueryBuilder(requestOptions.ProjectSystemProperties);

            if (token != null)
            {
                token.ApplyToUriQueryBuilder(builder);
            }

            StorageUri tempUriList = NavigationHelper.AppendPathToUri(client.StorageUri, tableName);
            RESTCommand<TableQuerySegment> queryCmd = new RESTCommand<TableQuerySegment>(client.Credentials, tempUriList);
            requestOptions.ApplyToStorageCommand(queryCmd);

            queryCmd.CommandLocationMode = CommonUtility.GetListingLocationMode(token);
            queryCmd.RetrieveResponseStream = true;
            queryCmd.Builder = builder;
            queryCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            queryCmd.BuildRequest = (cmd, uri, queryBuilder, cnt, serverTimeout, ctx) => TableOperationHttpRequestMessageFactory.BuildRequestForTableQuery(uri, builder, serverTimeout, cnt, ctx, requestOptions.PayloadFormat.Value, client.GetCanonicalizer(), client.Credentials);
            queryCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp.StatusCode, null /* retVal */, cmd, ex);
            queryCmd.PostProcessResponse = async (cmd, resp, ctx) =>
            {
                ResultSegment<DynamicTableEntity> resSeg = await TableOperationHttpResponseParsers.TableQueryPostProcessGeneric<DynamicTableEntity>(cmd.ResponseStream, resolver.Invoke, resp, requestOptions, ctx, client.AccountName);
                if (resSeg.ContinuationToken != null)
                {
                    resSeg.ContinuationToken.TargetLocation = cmd.CurrentResult.TargetLocation;
                }

                return new TableQuerySegment(resSeg);
            };

            return queryCmd;
        }

        internal Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableContinuationToken continuationToken, CloudTableClient client, string tableName, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", tableName);
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            RESTCommand<TableQuerySegment<TResult>> cmdToExecute = this.QueryImpl<TResult>(continuationToken, client, tableName, resolver, modifiedOptions);

            return Task.Run(async () => await Executor.ExecuteAsync(
                                                            cmdToExecute,
                                                            modifiedOptions.RetryPolicy,
                                                            operationContext,
                                                            cancellationToken), cancellationToken);
        }

        private RESTCommand<TableQuerySegment<RESULT_TYPE>> QueryImpl<RESULT_TYPE>(TableContinuationToken token, CloudTableClient client, string tableName, EntityResolver<RESULT_TYPE> resolver, TableRequestOptions requestOptions)
        {
            UriQueryBuilder builder = this.GenerateQueryBuilder(requestOptions.ProjectSystemProperties);

            if (token != null)
            {
                token.ApplyToUriQueryBuilder(builder);
            }

            StorageUri tempUri = NavigationHelper.AppendPathToUri(client.StorageUri, tableName);
            RESTCommand<TableQuerySegment<RESULT_TYPE>> queryCmd = new RESTCommand<TableQuerySegment<RESULT_TYPE>>(client.Credentials, tempUri);
            requestOptions.ApplyToStorageCommand(queryCmd);

            queryCmd.CommandLocationMode = CommonUtility.GetListingLocationMode(token);
            queryCmd.RetrieveResponseStream = true;
            queryCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            queryCmd.Builder = builder;
            queryCmd.BuildRequest = (cmd, uri, queryBuilder, cnt, serverTimeout, ctx) => TableOperationHttpRequestMessageFactory.BuildRequestForTableQuery(uri, builder, serverTimeout, cnt, ctx, requestOptions.PayloadFormat.Value, client.GetCanonicalizer(), client.Credentials);
            queryCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp.StatusCode, null /* retVal */, cmd, ex);
            queryCmd.PostProcessResponse = async (cmd, resp, ctx) =>
            {
                ResultSegment<RESULT_TYPE> resSeg = await TableOperationHttpResponseParsers.TableQueryPostProcessGeneric<RESULT_TYPE>(cmd.ResponseStream, resolver.Invoke, resp, requestOptions, ctx, client.AccountName);
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
