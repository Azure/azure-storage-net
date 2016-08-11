// -----------------------------------------------------------------------------------------
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
    using System.Threading;
#if NETCORE

#else
    using Windows.Foundation;
    using System.Runtime.InteropServices.WindowsRuntime;
#endif
    using System.Threading.Tasks;

    public partial class CloudTableClient
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

        #region TableOperation Execute Methods
        internal Task<TableResult> ExecuteAsync(string tableName, TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("operation", operation);

            return operation.ExecuteAsync(this, tableName, requestOptions, operationContext, cancellationToken);
        }
        #endregion

        #region TableQuery Execute Methods
        internal Task<TableQuerySegment> ExecuteQuerySegmentedAsync(string tableName, TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.ExecuteQuerySegmentedAsync(token, this, tableName, requestOptions, operationContext, CancellationToken.None);
        }

        internal Task<TableQuerySegment> ExecuteQuerySegmentedAsync(string tableName, TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("query", query);
            return query.ExecuteQuerySegmentedAsync(token, this, tableName, requestOptions, operationContext, cancellationToken);
        }
        #endregion

        #region List Tables
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
        [DoesServiceRequest]
        public virtual Task<TableResultSegment> ListTablesSegmentedAsync(TableContinuationToken currentToken)
        {
            return this.ListTablesSegmentedAsync(null, null /* maxResults */, currentToken, null /* TableRequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of table items beginning with the specified prefix.
        /// </summary>
        /// <param name="prefix">The table name prefix.</param>
        /// <param name="currentToken">A <see cref="TableContinuationToken"/> token returned by a previous listing operation.</param>
        /// <returns>The result segment containing the collection of tables.</returns>
        [DoesServiceRequest]
        public virtual Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, TableContinuationToken currentToken)
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
        [DoesServiceRequest]
        public virtual Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, int? maxResults, TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext)
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
        [DoesServiceRequest]
        public virtual Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, int? maxResults, TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();

            TableQuery query = this.GenerateListTablesQuery(prefix, maxResults);

            return Task.Run(async () =>
            {
                TableQuerySegment seg = await this.ExecuteQuerySegmentedAsync(TableConstants.TableServiceTablesName, query, currentToken, requestOptions, operationContext, cancellationToken);
                TableResultSegment retSegment = new TableResultSegment(seg.Results.Select(tbl => new CloudTable(tbl.Properties[TableConstants.TableName].StringValue, this)).ToList());
                retSegment.ContinuationToken = seg.ContinuationToken;
                return retSegment;
            }, cancellationToken);
        }
#endregion

#region Analytics
        /// <summary>
        /// Gets the properties of the table service.
        /// </summary>
        /// <returns>The table service properties as a <see cref="ServiceProperties"/> object.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync()
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
        public virtual Task<ServiceProperties> GetServicePropertiesAsync(TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.GetServicePropertiesAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the properties of the table service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>The table service properties as a <see cref="ServiceProperties"/> object.</returns>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsync(
                                                        this.GetServicePropertiesImpl(modifiedOptions),
                                                        modifiedOptions.RetryPolicy,
                                                        operationContext,
                                                        cancellationToken), cancellationToken);
        }

        private RESTCommand<ServiceProperties> GetServicePropertiesImpl(TableRequestOptions requestOptions)
        {
            RESTCommand<ServiceProperties> retCmd = new RESTCommand<ServiceProperties>(this.Credentials, this.StorageUri);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableHttpRequestMessageFactory.GetServiceProperties(uri, serverTimeout, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.RetrieveResponseStream = true;
            retCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
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
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties)
        {
            return this.SetServicePropertiesAsync(properties, null /* RequestOptions */, null /* OperationContext */);
        }

        /// <summary>
        /// Gets the properties of the table service.
        /// </summary>
        /// <param name="properties">The table service properties.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.SetServicePropertiesAsync(properties, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the properties of the table service.
        /// </summary>
        /// <param name="properties">The table service properties.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                                                                this.SetServicePropertiesImpl(properties, modifiedOptions),
                                                                modifiedOptions.RetryPolicy,
                                                                operationContext,
                                                                cancellationToken), cancellationToken);
        }

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
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableHttpRequestMessageFactory.SetServiceProperties(uri, serverTimeout, cnt, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null /* md5 */, cmd, ctx);
            retCmd.StreamToDispose = memoryStream;
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
        public virtual Task<ServiceStats> GetServiceStatsAsync()
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
        public virtual Task<ServiceStats> GetServiceStatsAsync(TableRequestOptions options, OperationContext operationContext)
        {
            return this.GetServiceStatsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the stats of the table service.
        /// </summary>
        /// <param name="options">A <see cref="TableRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The table service stats.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync(TableRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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


        private RESTCommand<ServiceStats> GetServiceStatsImpl(TableRequestOptions requestOptions)
        {
            if (RetryPolicies.LocationMode.PrimaryOnly == requestOptions.LocationMode)
            {
                throw new InvalidOperationException(SR.GetServiceStatsInvalidOperation);
            }  

            RESTCommand<ServiceStats> retCmd = new RESTCommand<ServiceStats>(this.Credentials, this.StorageUri);
            requestOptions.ApplyToStorageCommand(retCmd);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => TableHttpRequestMessageFactory.GetServiceStats(uri, serverTimeout, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.RetrieveResponseStream = true;
            retCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            retCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            retCmd.PostProcessResponse = (cmd, resp, ctx) => Task.Factory.StartNew(() => HttpResponseParsers.ReadServiceStats(cmd.ResponseStream));
            return retCmd;
        }
        #endregion
    }
}
