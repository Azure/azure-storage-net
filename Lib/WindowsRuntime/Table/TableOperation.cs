// -----------------------------------------------------------------------------------------
// <copyright file="TableOperation.cs" company="Microsoft">
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
    using System.Net;
    using System.Net.Http;
    using System.Threading;

#if NETCORE
#else
    using Windows.Foundation;
    using System.Runtime.InteropServices.WindowsRuntime;
#endif
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a single table operation.
    /// </summary>
    public partial class TableOperation
    {
        internal Task<TableResult> ExecuteAsync(CloudTableClient client, string tableName, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            CommonUtility.AssertNotNullOrEmpty("tableName", tableName);
            RESTCommand<TableResult> cmdToExecute = null;

            if (this.OperationType == TableOperationType.Insert ||
                this.OperationType == TableOperationType.InsertOrMerge ||
                this.OperationType == TableOperationType.InsertOrReplace)
            {
                if (!this.isTableEntity && this.OperationType != TableOperationType.Insert)
                {
                    CommonUtility.AssertNotNull("Upserts require a valid PartitionKey", this.Entity.PartitionKey);
                    CommonUtility.AssertNotNull("Upserts require a valid RowKey", this.Entity.RowKey);
                }

                cmdToExecute = InsertImpl(this, client, tableName, modifiedOptions);
            }
            else if (this.OperationType == TableOperationType.Delete)
            {
                if (!this.isTableEntity)
                {
                    CommonUtility.AssertNotNullOrEmpty("Delete requires a valid ETag", this.Entity.ETag);
                    CommonUtility.AssertNotNull("Delete requires a valid PartitionKey", this.Entity.PartitionKey);
                    CommonUtility.AssertNotNull("Delete requires a valid RowKey", this.Entity.RowKey);
                }

                cmdToExecute = DeleteImpl(this, client, tableName, modifiedOptions);
            }
            else if (this.OperationType == TableOperationType.Merge)
            {
                CommonUtility.AssertNotNullOrEmpty("Merge requires a valid ETag", this.Entity.ETag);
                CommonUtility.AssertNotNull("Merge requires a valid PartitionKey", this.Entity.PartitionKey);
                CommonUtility.AssertNotNull("Merge requires a valid RowKey", this.Entity.RowKey);

                cmdToExecute = MergeImpl(this, client, tableName, modifiedOptions);
            }
            else if (this.OperationType == TableOperationType.Replace)
            {
                CommonUtility.AssertNotNullOrEmpty("Replace requires a valid ETag", this.Entity.ETag);
                CommonUtility.AssertNotNull("Replace requires a valid PartitionKey", this.Entity.PartitionKey);
                CommonUtility.AssertNotNull("Replace requires a valid RowKey", this.Entity.RowKey);

                cmdToExecute = ReplaceImpl(this, client, tableName, modifiedOptions);
            }
            else if (this.OperationType == TableOperationType.Retrieve)
            {
                cmdToExecute = RetrieveImpl(this, client, tableName, modifiedOptions);
            }
            else
            {
                throw new NotSupportedException();
            }

            return Task.Run(() => Executor.ExecuteAsync(
                                            cmdToExecute,
                                            modifiedOptions.RetryPolicy,
                                            operationContext,                                                                       
                                            cancellationToken), cancellationToken);
        }

        private static RESTCommand<TableResult> InsertImpl(TableOperation operation, CloudTableClient client, string tableName, TableRequestOptions requestOptions)
        {
            RESTCommand<TableResult> insertCmd = new RESTCommand<TableResult>(client.Credentials, operation.GenerateRequestURI(client.StorageUri, tableName));
            requestOptions.ApplyToStorageCommand(insertCmd);

            TableResult result = new TableResult() { Result = operation.Entity };
            insertCmd.RetrieveResponseStream = true;
            insertCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            insertCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableOperationHttpRequestMessageFactory.BuildRequestForTableOperation(cmd, uri, builder, serverTimeout, operation, client, cnt, ctx, requestOptions.PayloadFormat.Value, client.GetCanonicalizer(), client.Credentials);
            insertCmd.PreProcessResponse = (cmd, resp, ex, ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex, cmd, ctx);

            insertCmd.PostProcessResponse = (cmd, resp, ctx) => TableOperationHttpResponseParsers.TableOperationPostProcess(result, operation, cmd, resp, ctx, requestOptions, client.AccountName);

            return insertCmd;
        }

        private static RESTCommand<TableResult> DeleteImpl(TableOperation operation, CloudTableClient client, string tableName, TableRequestOptions requestOptions)
        {
            RESTCommand<TableResult> deleteCmd = new RESTCommand<TableResult>(client.Credentials, operation.GenerateRequestURI(client.StorageUri, tableName));
            requestOptions.ApplyToStorageCommand(deleteCmd);

            TableResult result = new TableResult() { Result = operation.Entity };
            deleteCmd.RetrieveResponseStream = false;
            deleteCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            deleteCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableOperationHttpRequestMessageFactory.BuildRequestForTableOperation(cmd, uri, builder, serverTimeout, operation, client, cnt, ctx, requestOptions.PayloadFormat.Value, client.GetCanonicalizer(), client.Credentials);
            deleteCmd.PreProcessResponse = (cmd, resp, ex, ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex, cmd, ctx);

            return deleteCmd;
        }

        private static RESTCommand<TableResult> MergeImpl(TableOperation operation, CloudTableClient client, string tableName, TableRequestOptions requestOptions)
        {
            RESTCommand<TableResult> mergeCmd = new RESTCommand<TableResult>(client.Credentials, operation.GenerateRequestURI(client.StorageUri, tableName));
            requestOptions.ApplyToStorageCommand(mergeCmd);

            TableResult result = new TableResult() { Result = operation.Entity };
            mergeCmd.RetrieveResponseStream = false;
            mergeCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            mergeCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableOperationHttpRequestMessageFactory.BuildRequestForTableOperation(cmd, uri, builder, serverTimeout, operation, client, cnt, ctx, requestOptions.PayloadFormat.Value, client.GetCanonicalizer(), client.Credentials);
            mergeCmd.PreProcessResponse = (cmd, resp, ex, ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex, cmd, ctx);

            return mergeCmd;
        }

        private static RESTCommand<TableResult> ReplaceImpl(TableOperation operation, CloudTableClient client, string tableName, TableRequestOptions requestOptions)
        {
            RESTCommand<TableResult> replaceCmd = new RESTCommand<TableResult>(client.Credentials, operation.GenerateRequestURI(client.StorageUri, tableName));
            requestOptions.ApplyToStorageCommand(replaceCmd);

            TableResult result = new TableResult() { Result = operation.Entity };
            replaceCmd.RetrieveResponseStream = false;
            replaceCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            replaceCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableOperationHttpRequestMessageFactory.BuildRequestForTableOperation(cmd, uri, builder, serverTimeout, operation, client, cnt, ctx, requestOptions.PayloadFormat.Value, client.GetCanonicalizer(), client.Credentials);
            replaceCmd.PreProcessResponse = (cmd, resp, ex, ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex, cmd, ctx);

            return replaceCmd;
        }

        private static RESTCommand<TableResult> RetrieveImpl(TableOperation operation, CloudTableClient client, string tableName, TableRequestOptions requestOptions)
        {
            RESTCommand<TableResult> retrieveCmd = new RESTCommand<TableResult>(client.Credentials, operation.GenerateRequestURI(client.StorageUri, tableName));
            requestOptions.ApplyToStorageCommand(retrieveCmd);

            TableResult result = new TableResult();
            if (operation.SelectColumns != null && operation.SelectColumns.Count > 0)
            {
                retrieveCmd.Builder = operation.GenerateQueryBuilder(requestOptions.ProjectSystemProperties);
            }

            retrieveCmd.CommandLocationMode = operation.isPrimaryOnlyRetrieve ? CommandLocationMode.PrimaryOnly : CommandLocationMode.PrimaryOrSecondary;
            retrieveCmd.RetrieveResponseStream = true;
            retrieveCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            retrieveCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableOperationHttpRequestMessageFactory.BuildRequestForTableOperation(cmd, uri, builder, serverTimeout, operation, client, cnt, ctx, requestOptions.PayloadFormat.Value, client.GetCanonicalizer(), client.Credentials);
            retrieveCmd.PreProcessResponse = (cmd, resp, ex, ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex, cmd, ctx);
            retrieveCmd.PostProcessResponse = (cmd, resp, ctx) =>
                  Task.Run(async () =>
                    {
                        if (resp.StatusCode == HttpStatusCode.NotFound)
                        {
                            return result;
                        }

                        result = await TableOperationHttpResponseParsers.TableOperationPostProcess(result, operation, cmd, resp, ctx, requestOptions, client.AccountName);
                        return result;
                    });
            return retrieveCmd;
        }

        internal HttpMethod HttpMethod
        {
            get
            {
                switch (this.OperationType)
                {
                    case TableOperationType.Insert:
                        return HttpMethod.Post;
                    case TableOperationType.Merge:
                    case TableOperationType.InsertOrMerge:
                        return HttpMethod.Post; // Post tunneling for merge
                    case TableOperationType.Replace:
                    case TableOperationType.InsertOrReplace:
                        return HttpMethod.Put;
                    case TableOperationType.Delete:
                        return HttpMethod.Delete;
                    case TableOperationType.Retrieve:
                        return HttpMethod.Get;
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }
}
