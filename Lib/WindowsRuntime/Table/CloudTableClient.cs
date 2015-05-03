﻿// -----------------------------------------------------------------------------------------
// <copyright file="CloudTableClient.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Auth.Protocol;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
#if ASPNET_K || PORTABLE
    using System.Threading;
#else
    using Windows.Foundation;
    using System.Runtime.InteropServices.WindowsRuntime;
#endif
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a client-side logical representation of the Windows Azure Table Service. This client is used to configure and execute requests against the Table Service.
    /// </summary>
    /// <remarks>The service client encapsulates the base URI for the Table service. If the service client will be used for authenticated access, it also encapsulates the credentials for accessing the storage account.</remarks>    
    public sealed partial class CloudTableClient
    {
        /// <summary>
        /// Gets or sets the authentication scheme to use to sign HTTP requests.
        /// </summary>
        public AuthenticationScheme AuthenticationScheme
        {
            get
            {
                return this.authenticationScheme;
            }

            set
            {
                this.authenticationScheme = value;
            }
        }

        /// <summary>
        /// Gets the authentication handler used to sign HTTP requests.
        /// </summary>
        /// <value>The authentication handler.</value>
        internal HttpClientHandler AuthenticationHandler
        {
            get
            {
                HttpClientHandler authenticationHandler;
                if (this.Credentials.IsSharedKey)
                {
#if PORTABLE
                    throw new NotSupportedException(SR.PortableDoesNotSupportSharedKey);
#else
                    authenticationHandler = new SharedKeyAuthenticationHttpHandler(
                        this.GetCanonicalizer(),
                        this.Credentials,
                        this.Credentials.AccountName);
#endif
                }
                else
                {
                    authenticationHandler = new NoOpAuthenticationHttpHandler();
                }

                return authenticationHandler;
            }
        }

        #region TableOperation Execute Methods
#if ASPNET_K || PORTABLE
        internal Task<TableResult> ExecuteAsync(string tableName, TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("operation", operation);

            return operation.ExecuteAsync(this, tableName, requestOptions, operationContext, cancellationToken);
        }
#else
        internal IAsyncOperation<TableResult> ExecuteAsync(string tableName, TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("operation", operation);

            return operation.ExecuteAsync(this, tableName, requestOptions, operationContext);
        }
#endif
        #endregion

        #region TableQuery Execute Methods
#if ASPNET_K || PORTABLE
        internal Task<TableQuerySegment> ExecuteQuerySegmentedAsync(string tableName, TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.ExecuteQuerySegmentedAsync(token, this, tableName, requestOptions, operationContext, CancellationToken.None);
        }
#else
        internal IAsyncOperation<TableQuerySegment> ExecuteQuerySegmentedAsync(string tableName, TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.ExecuteQuerySegmentedAsync(token, this, tableName, requestOptions, operationContext);
        }
#endif

#if ASPNET_K || PORTABLE
        internal Task<TableQuerySegment> ExecuteQuerySegmentedAsync(string tableName, TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.ExecuteQuerySegmentedAsync(token, this, tableName, requestOptions, operationContext, cancellationToken);
        }
#endif
        #endregion

        #region List Tables
#if !PORTABLE
        private TableQuery GenerateListTablesQuery(string prefix, int? maxResults)
        {
            TableQuery query = new TableQuery();

            if (!string.IsNullOrEmpty(prefix))
            {
                // Append Max char to end  '{' is 1 + 'z' in AsciiTable
                string uppperBound = prefix + '{';

                query = query.Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(TableConstants.TableName, QueryComparisons.GreaterThanOrEqual, prefix),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(TableConstants.TableName, QueryComparisons.LessThan, uppperBound)));
            }

            if (maxResults.HasValue)
            {
                query = query.Take(maxResults.Value);
            }

            return query;
        }

        internal IEnumerable<CloudTable> ListTables()
        {
            return this.ListTables(null);
        }

        internal IEnumerable<CloudTable> ListTables(string prefix)
        {
            return this.ListTables(prefix, null /* RequestOptions */, null /* OperationContext */);
        }

        internal IEnumerable<CloudTable> ListTables(string prefix, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();

            return this.GenerateListTablesQuery(prefix, null).Execute(this, TableConstants.TableServiceTablesName, requestOptions, operationContext).Select(
                    tbl => new CloudTable(tbl.Properties[TableConstants.TableName].StringValue, this));
        }

        /// <summary>
        /// Returns a collection of table items.
        /// </summary>
        /// <param name="currentToken">A <see cref="TableContinuationToken"/> token returned by a previous listing operation.</param>
        /// <returns>The result segment containing the collection of tables.</returns>
#if ASPNET_K 
        public Task<TableResultSegment> ListTablesSegmentedAsync(TableContinuationToken currentToken)
#else
        public IAsyncOperation<TableResultSegment> ListTablesSegmentedAsync(TableContinuationToken currentToken)
#endif
        {
            return this.ListTablesSegmentedAsync(null, null /* maxResults */, currentToken, null /* TableRequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of table items beginning with the specified prefix.
        /// </summary>
        /// <param name="prefix">The table name prefix.</param>
        /// <param name="currentToken">A <see cref="TableContinuationToken"/> token returned by a previous listing operation.</param>
        /// <returns>The result segment containing the collection of tables.</returns>
#if ASPNET_K 
        public Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, TableContinuationToken currentToken)
#else
        public IAsyncOperation<TableResultSegment> ListTablesSegmentedAsync(string prefix, TableContinuationToken currentToken)
#endif
        {
            return this.ListTablesSegmentedAsync(prefix, null /* maxResults */, currentToken, null /* TableRequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of tables beginning with the specified prefix.
        /// </summary>
        /// <param name="prefix">The table name prefix.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c> the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="TableContinuationToken"/> token returned by a previous listing operation.</param> 
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that provides information on how the operation executed.</param>
        /// <returns>The result segment containing the collection of tables.</returns>
#if ASPNET_K 
        public Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, int? maxResults, TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ListTablesSegmentedAsync(prefix, maxResults, currentToken, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a result segment containing a collection of tables beginning with the specified prefix.
        /// </summary>
        /// <param name="prefix">The table name prefix.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c> the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="TableContinuationToken"/> token returned by a previous listing operation.</param> 
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that provides information on how the operation executed.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The result segment containing the collection of tables.</returns>
        public Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, int? maxResults, TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
#else
        public IAsyncOperation<TableResultSegment> ListTablesSegmentedAsync(string prefix, int? maxResults, TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext)
#endif
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();

            TableQuery query = this.GenerateListTablesQuery(prefix, maxResults);

#if ASPNET_K 
            return Task.Run(async () =>
            {
                TableQuerySegment seg = await this.ExecuteQuerySegmentedAsync(TableConstants.TableServiceTablesName, query, currentToken, requestOptions, operationContext, cancellationToken);
#else
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                TableQuerySegment seg = await this.ExecuteQuerySegmentedAsync(TableConstants.TableServiceTablesName, query, currentToken, requestOptions, operationContext).AsTask(cancellationToken);
#endif

                TableResultSegment retSegment = new TableResultSegment(seg.Results.Select(tbl => new CloudTable(tbl.Properties[TableConstants.TableName].StringValue, this)).ToList());
                retSegment.ContinuationToken = seg.ContinuationToken;
                return retSegment;
#if ASPNET_K 
            }, cancellationToken);
#else
            });
#endif
        }
#endif
#endregion

#region Analytics
#if !PORTABLE
        /// <summary>
        /// Gets the properties of the table service.
        /// </summary>
        /// <returns>The table service properties as a <see cref="ServiceProperties"/> object.</returns>
        [DoesServiceRequest]
#if ASPNET_K 
        public Task<ServiceProperties> GetServicePropertiesAsync()
#else
        public IAsyncOperation<ServiceProperties> GetServicePropertiesAsync()
#endif
        {
            return this.GetServicePropertiesAsync(null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Gets the properties of the table service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>The table service properties as a <see cref="ServiceProperties"/> object.</returns>
        [DoesServiceRequest]
#if ASPNET_K 
        public Task<ServiceProperties> GetServicePropertiesAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.GetServicePropertiesAsync(requestOptions, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<ServiceProperties> GetServicePropertiesAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (cancellationToken) => await Executor.ExecuteAsync(
                                                                      this.GetServicePropertiesImpl(modifiedOptions),
                                                                      modifiedOptions.RetryPolicy,
                                                                      operationContext,
                                                                      cancellationToken));
        }
#endif

#if ASPNET_K 
        /// <summary>
        /// Gets the properties of the table service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>The table service properties as a <see cref="ServiceProperties"/> object.</returns>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public Task<ServiceProperties> GetServicePropertiesAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsync(
                                                        this.GetServicePropertiesImpl(modifiedOptions),
                                                        modifiedOptions.RetryPolicy,
                                                        operationContext,
                                                        cancellationToken), cancellationToken);
        }
#endif

        private RESTCommand<ServiceProperties> GetServicePropertiesImpl(TableRequestOptions requestOptions)
        {
            RESTCommand<ServiceProperties> retCmd = new RESTCommand<ServiceProperties>(this.Credentials, this.StorageUri);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableHttpRequestMessageFactory.GetServiceProperties(uri, serverTimeout, ctx);
            retCmd.RetrieveResponseStream = true;
            retCmd.Handler = this.AuthenticationHandler;
            retCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);

            retCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task.Factory.StartNew(() => HttpResponseParsers.ReadServiceProperties(cmd.ResponseStream));
            };

            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }

        /// <summary>
        /// Gets the properties of the table service.
        /// </summary>
        /// <param name="properties">The table service properties.</param>
        [DoesServiceRequest]
#if ASPNET_K 
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        public Task SetServicePropertiesAsync(ServiceProperties properties)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        public IAsyncAction SetServicePropertiesAsync(ServiceProperties properties)
#endif
        {
            return this.SetServicePropertiesAsync(properties, null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Gets the properties of the table service.
        /// </summary>
        /// <param name="properties">The table service properties.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
#if ASPNET_K 
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task SetServicePropertiesAsync(ServiceProperties properties, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.SetServicePropertiesAsync(properties, requestOptions, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction SetServicePropertiesAsync(ServiceProperties properties, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();
            return AsyncInfo.Run(async (cancellationToken) => await Executor.ExecuteAsyncNullReturn(
                                                                                    this.SetServicePropertiesImpl(properties, modifiedOptions),
                                                                                    modifiedOptions.RetryPolicy,
                                                                                    operationContext,
                                                                                    cancellationToken));
        }
#endif

#if ASPNET_K 
        /// <summary>
        /// Gets the properties of the table service.
        /// </summary>
        /// <param name="properties">The table service properties.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task SetServicePropertiesAsync(ServiceProperties properties, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                                                                this.SetServicePropertiesImpl(properties, modifiedOptions),
                                                                modifiedOptions.RetryPolicy,
                                                                operationContext,
                                                                cancellationToken), cancellationToken);
        }
#endif

        private RESTCommand<NullType> SetServicePropertiesImpl(ServiceProperties properties, TableRequestOptions requestOptions)
        {
            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            try
            {
                properties.WriteServiceProperties(memoryStream);
            }
            catch (InvalidOperationException invalidOpException)
            {
                throw new ArgumentException(invalidOpException.Message, "properties");
            }

            RESTCommand<NullType> retCmd = new RESTCommand<NullType>(this.Credentials, this.StorageUri);
            requestOptions.ApplyToStorageCommand(retCmd);
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableHttpRequestMessageFactory.SetServiceProperties(uri, serverTimeout, cnt, ctx);
            retCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null /* md5 */, cmd, ctx);
            retCmd.Handler = this.AuthenticationHandler;
            retCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, null /* retVal */, cmd, ex);

            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }

        /// <summary>
        /// Gets the stats of the table service.
        /// </summary>
        /// <returns>The table service stats.</returns>
        [DoesServiceRequest]
#if ASPNET_K 
        public Task<ServiceStats> GetServiceStatsAsync()
#else
        public IAsyncOperation<ServiceStats> GetServiceStatsAsync()
#endif
        {
            return this.GetServiceStatsAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets the stats of the table service.
        /// </summary>
        /// <param name="options">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The table service stats.</returns>
        [DoesServiceRequest]
#if ASPNET_K 
        public Task<ServiceStats> GetServiceStatsAsync(TableRequestOptions options, OperationContext operationContext)
        {
            return this.GetServiceStatsAsync(options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<ServiceStats> GetServiceStatsAsync(TableRequestOptions options, OperationContext operationContext)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(options, this);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(
                async (token) => await Executor.ExecuteAsync(
                    this.GetServiceStatsImpl(modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    token));
        }
#endif

#if ASPNET_K 
        /// <summary>
        /// Gets the stats of the table service.
        /// </summary>
        /// <param name="options">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The table service stats.</returns>
        [DoesServiceRequest]
        public Task<ServiceStats> GetServiceStatsAsync(TableRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(options, this);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(
                async () => await Executor.ExecuteAsync(
                    this.GetServiceStatsImpl(modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken), cancellationToken);
        }

#endif

        private RESTCommand<ServiceStats> GetServiceStatsImpl(TableRequestOptions requestOptions)
        {
            RESTCommand<ServiceStats> retCmd = new RESTCommand<ServiceStats>(this.Credentials, this.StorageUri);
            requestOptions.ApplyToStorageCommand(retCmd);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableHttpRequestMessageFactory.GetServiceStats(uri, serverTimeout, ctx);
            retCmd.RetrieveResponseStream = true;
            retCmd.Handler = this.AuthenticationHandler;
            retCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            retCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            retCmd.PostProcessResponse = (cmd, resp, ctx) => Task.Factory.StartNew(() => HttpResponseParsers.ReadServiceStats(cmd.ResponseStream));
            return retCmd;
        }
#endif
        #endregion
    }
}
