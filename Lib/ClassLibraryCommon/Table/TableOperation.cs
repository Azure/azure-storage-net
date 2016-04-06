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
    using System.IO;
    using System.Net;

    /// <summary>
    /// Represents a single table operation.
    /// </summary>
    public partial class TableOperation
    {
#if SYNC
        internal TableResult Execute(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);

            return Executor.ExecuteSync(this.GenerateCMDForOperation(client, table, modifiedOptions), modifiedOptions.RetryPolicy, operationContext);
        }
#endif

        [DoesServiceRequest]
        internal ICancellableAsyncResult BeginExecute(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);

            return Executor.BeginExecuteAsync(
                                          this.GenerateCMDForOperation(client, table, modifiedOptions),
                                          modifiedOptions.RetryPolicy,
                                          operationContext,
                                          callback,
                                          state);
        }

        internal static TableResult EndExecute(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<TableResult>(asyncResult);
        }

        internal RESTCommand<TableResult> GenerateCMDForOperation(CloudTableClient client, CloudTable table, TableRequestOptions modifiedOptions)
        {
            if (this.OperationType == TableOperationType.Insert ||
                this.OperationType == TableOperationType.InsertOrMerge ||
                this.OperationType == TableOperationType.InsertOrReplace)
            {
                if (!this.isTableEntity && this.OperationType != TableOperationType.Insert)
                {
                    CommonUtility.AssertNotNull("Upserts require a valid PartitionKey", this.Entity.PartitionKey);
                    CommonUtility.AssertNotNull("Upserts require a valid RowKey", this.Entity.RowKey);
                }

                return InsertImpl(this, client, table, modifiedOptions);
            }
            else if (this.OperationType == TableOperationType.Delete)
            {
                if (!this.isTableEntity)
                {
                    CommonUtility.AssertNotNullOrEmpty("Delete requires a valid ETag", this.Entity.ETag);
                    CommonUtility.AssertNotNull("Delete requires a valid PartitionKey", this.Entity.PartitionKey);
                    CommonUtility.AssertNotNull("Delete requires a valid RowKey", this.Entity.RowKey);
                }

                return DeleteImpl(this, client, table, modifiedOptions);
            }
            else if (this.OperationType == TableOperationType.Merge)
            {
                CommonUtility.AssertNotNullOrEmpty("Merge requires a valid ETag", this.Entity.ETag);
                CommonUtility.AssertNotNull("Merge requires a valid PartitionKey", this.Entity.PartitionKey);
                CommonUtility.AssertNotNull("Merge requires a valid RowKey", this.Entity.RowKey);

                return MergeImpl(this, client, table, modifiedOptions);
            }
            else if (this.OperationType == TableOperationType.Replace)
            {
                CommonUtility.AssertNotNullOrEmpty("Replace requires a valid ETag", this.Entity.ETag);
                CommonUtility.AssertNotNull("Replace requires a valid PartitionKey", this.Entity.PartitionKey);
                CommonUtility.AssertNotNull("Replace requires a valid RowKey", this.Entity.RowKey);

                return ReplaceImpl(this, client, table, modifiedOptions);
            }
            else if (this.OperationType == TableOperationType.Retrieve)
            {
                return RetrieveImpl(this, client, table, modifiedOptions);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static RESTCommand<TableResult> InsertImpl(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
        {
            RESTCommand<TableResult> insertCmd = new RESTCommand<TableResult>(client.Credentials, operation.GenerateRequestURI(client.StorageUri, table.Name));
            requestOptions.ApplyToStorageCommand(insertCmd);

            TableResult result = new TableResult() { Result = operation.Entity };
            insertCmd.RetrieveResponseStream = true;
            insertCmd.SignRequest = client.AuthenticationHandler.SignRequest;
            insertCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            insertCmd.BuildRequestDelegate = (uri, builder, timeout, useVersionHeader, ctx) =>
            {
                Tuple<HttpWebRequest, Stream> res = TableOperationHttpWebRequestFactory.BuildRequestForTableOperation(uri, builder, client.BufferManager, timeout, operation, useVersionHeader, ctx, requestOptions, client.AccountName);
                insertCmd.SendStream = res.Item2;
                return res.Item1;
            };

            insertCmd.PreProcessResponse = (cmd, resp, ex, ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex);

            insertCmd.PostProcessResponse = (cmd, resp, ctx) => TableOperationHttpResponseParsers.TableOperationPostProcess(result, operation, cmd, resp, ctx, requestOptions, client.AccountName);

            return insertCmd;
        }

        private static RESTCommand<TableResult> DeleteImpl(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
        {
            RESTCommand<TableResult> deleteCmd = new RESTCommand<TableResult>(client.Credentials, operation.GenerateRequestURI(client.StorageUri, table.Name));
            requestOptions.ApplyToStorageCommand(deleteCmd);

            TableResult result = new TableResult() { Result = operation.Entity };
            deleteCmd.RetrieveResponseStream = false;
            deleteCmd.SignRequest = client.AuthenticationHandler.SignRequest;
            deleteCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            deleteCmd.BuildRequestDelegate = (uri, builder, timeout, useVersionHeader, ctx) => TableOperationHttpWebRequestFactory.BuildRequestForTableOperation(uri, builder, client.BufferManager, timeout, operation, useVersionHeader, ctx, requestOptions, client.AccountName).Item1;
            deleteCmd.PreProcessResponse = (cmd, resp, ex, ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex);

            return deleteCmd;
        }

        private static RESTCommand<TableResult> MergeImpl(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
        {
            RESTCommand<TableResult> mergeCmd = new RESTCommand<TableResult>(client.Credentials, operation.GenerateRequestURI(client.StorageUri, table.Name));
            requestOptions.ApplyToStorageCommand(mergeCmd);

            TableResult result = new TableResult() { Result = operation.Entity };
            mergeCmd.RetrieveResponseStream = false;
            mergeCmd.SignRequest = client.AuthenticationHandler.SignRequest;
            mergeCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            mergeCmd.BuildRequestDelegate = (uri, builder, timeout, useVersionHeader, ctx) =>
            {
                Tuple<HttpWebRequest, Stream> res = TableOperationHttpWebRequestFactory.BuildRequestForTableOperation(uri, builder, client.BufferManager, timeout, operation, useVersionHeader, ctx, requestOptions, client.AccountName);
                mergeCmd.SendStream = res.Item2;
                return res.Item1;
            };

            mergeCmd.PreProcessResponse = (cmd, resp, ex, ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex);

            return mergeCmd;
        }

        private static RESTCommand<TableResult> ReplaceImpl(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
        {
            RESTCommand<TableResult> replaceCmd = new RESTCommand<TableResult>(client.Credentials, operation.GenerateRequestURI(client.StorageUri, table.Name));
            requestOptions.ApplyToStorageCommand(replaceCmd);

            TableResult result = new TableResult() { Result = operation.Entity };
            replaceCmd.RetrieveResponseStream = false;
            replaceCmd.SignRequest = client.AuthenticationHandler.SignRequest;
            replaceCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            replaceCmd.BuildRequestDelegate = (uri, builder, timeout, useVersionHeader, ctx) =>
            {
                Tuple<HttpWebRequest, Stream> res = TableOperationHttpWebRequestFactory.BuildRequestForTableOperation(uri, builder, client.BufferManager, timeout, operation, useVersionHeader, ctx, requestOptions, client.AccountName);
                replaceCmd.SendStream = res.Item2;
                return res.Item1;
            };

            replaceCmd.PreProcessResponse = (cmd, resp, ex, ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex);

            return replaceCmd;
        }

        private static RESTCommand<TableResult> RetrieveImpl(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
        {
            requestOptions.AssertPolicyIfRequired();

            RESTCommand<TableResult> retrieveCmd = new RESTCommand<TableResult>(client.Credentials, operation.GenerateRequestURI(client.StorageUri, table.Name));
            requestOptions.ApplyToStorageCommand(retrieveCmd);

            TableResult result = new TableResult();
            if (operation.SelectColumns != null && operation.SelectColumns.Count > 0)
            {
                // If encryption policy is set, then add the encryption metadata column to Select columns in order to be able to decrypt properties.
                if (requestOptions.EncryptionPolicy != null)
                {
                    operation.SelectColumns.Add(Constants.EncryptionConstants.TableEncryptionKeyDetails);
                    operation.SelectColumns.Add(Constants.EncryptionConstants.TableEncryptionPropertyDetails);
                }

                retrieveCmd.Builder = operation.GenerateQueryBuilder(requestOptions.ProjectSystemProperties);
            }

            retrieveCmd.CommandLocationMode = operation.isPrimaryOnlyRetrieve ? CommandLocationMode.PrimaryOnly : CommandLocationMode.PrimaryOrSecondary;
            retrieveCmd.RetrieveResponseStream = true;
            retrieveCmd.SignRequest = client.AuthenticationHandler.SignRequest;
            retrieveCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            retrieveCmd.BuildRequestDelegate = (uri, builder, timeout, useVersionHeader, ctx) => TableOperationHttpWebRequestFactory.BuildRequestForTableOperation(uri, builder, client.BufferManager, timeout, operation, useVersionHeader, ctx, requestOptions, client.AccountName).Item1;
            retrieveCmd.PreProcessResponse = (cmd, resp, ex, ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex);
            retrieveCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return result;
                }

                result = TableOperationHttpResponseParsers.TableOperationPostProcess(result, operation, cmd, resp, ctx, requestOptions, client.AccountName);
                return result;
            };

            return retrieveCmd;
        }

        internal string HttpMethod
        {
            get
            {
                switch (this.OperationType)
                {
                    case TableOperationType.Insert:
                        return "POST";
                    case TableOperationType.Merge:
                    case TableOperationType.InsertOrMerge:
                        return "POST"; // Post tunneling for merge
                    case TableOperationType.Replace:
                    case TableOperationType.InsertOrReplace:
                        return "PUT";
                    case TableOperationType.Delete:
                        return "DELETE";
                    case TableOperationType.Retrieve:
                        return "GET";
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }
}
