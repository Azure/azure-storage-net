// -----------------------------------------------------------------------------------------
// <copyright file="CloudQueueClient.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Queue
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Queue.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a client-side logical representation of the Microsoft Azure Queue service. This client is used to configure and execute requests against the Queue service.
    /// </summary>
    /// <remarks>The service client encapsulates the endpoint or endpoints for the Queue service. If the service client will be used for authenticated access, it also encapsulates the credentials for accessing the storage account.</remarks>
    public partial class CloudQueueClient
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

#if SYNC
        /// <summary>
        /// Returns an enumerable collection of the queues in the storage account whose names begin with the specified prefix and that are retrieved lazily.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="queueListingDetails">A <see cref="QueueListingDetails"/> enumeration value that indicates which details to include in the listing.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects of type <see cref="CloudQueue"/> that are retrieved lazily.</returns>
        public virtual IEnumerable<CloudQueue> ListQueues(string prefix = null, QueueListingDetails queueListingDetails = QueueListingDetails.None, QueueRequestOptions options = null, OperationContext operationContext = null)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this);
            operationContext = operationContext ?? new OperationContext();

            return CommonUtility.LazyEnumerable(
                (token) => this.ListQueuesSegmentedCore(prefix, queueListingDetails, null, token as QueueContinuationToken, modifiedOptions, operationContext),
                long.MaxValue);
        }

        /// <summary>
        /// Returns a result segment containing a collection of queues.
        /// </summary>
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> continuation token returned by a previous listing operation.</param>
        /// <returns>A <see cref="QueueResultSegment"/> object.</returns>
        public virtual QueueResultSegment ListQueuesSegmented(QueueContinuationToken currentToken)
        {
            return this.ListQueuesSegmented(null, QueueListingDetails.None, null, currentToken, null, null);
        }

        /// <summary>
        /// Returns a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> continuation token returned by a previous listing operation.</param>
        /// <returns>A <see cref="QueueResultSegment"/> object.</returns>
        public virtual QueueResultSegment ListQueuesSegmented(string prefix, QueueContinuationToken currentToken)
        {
            return this.ListQueuesSegmented(prefix, QueueListingDetails.None, null, currentToken, null, null);
        }

        /// <summary>
        /// Returns a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="queueListingDetails">A <see cref="QueueListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="QueueResultSegment"/> object.</returns>
        public virtual QueueResultSegment ListQueuesSegmented(string prefix, QueueListingDetails queueListingDetails, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options = null, OperationContext operationContext = null)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this);
            operationContext = operationContext ?? new OperationContext();

            ResultSegment<CloudQueue> resultSegment = this.ListQueuesSegmentedCore(prefix, queueListingDetails, maxResults, currentToken, modifiedOptions, operationContext);
            return new QueueResultSegment(resultSegment.Results, (QueueContinuationToken)resultSegment.ContinuationToken);
        }

        /// <summary>
        /// Returns a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="queueListingDetails">A <see cref="QueueListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="ResultSegment{T}"/> of type <see cref="CloudQueue"/>.</returns>
        private ResultSegment<CloudQueue> ListQueuesSegmentedCore(string prefix, QueueListingDetails queueListingDetails, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options, OperationContext operationContext)
        {
            return Executor.ExecuteSync(
                this.ListQueuesImpl(prefix, maxResults, queueListingDetails, options, currentToken),
                options.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        public virtual ICancellableAsyncResult BeginListQueuesSegmented(QueueContinuationToken currentToken, AsyncCallback callback, object state)
        {
            return this.BeginListQueuesSegmented(null, QueueListingDetails.None, null, currentToken, null, null, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        public virtual ICancellableAsyncResult BeginListQueuesSegmented(string prefix, QueueContinuationToken currentToken, AsyncCallback callback, object state)
        {
            return this.BeginListQueuesSegmented(prefix, QueueListingDetails.None, null, currentToken, null, null, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="queueListingDetails">A <see cref="QueueListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        public virtual ICancellableAsyncResult BeginListQueuesSegmented(string prefix, QueueListingDetails queueListingDetails, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.ListQueuesSegmentedAsync(prefix, queueListingDetails, maxResults, currentToken, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="QueueResultSegment"/> object.</returns>
        public virtual QueueResultSegment EndListQueuesSegmented(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<QueueResultSegment>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>      
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="QueueResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<QueueResultSegment> ListQueuesSegmentedAsync(QueueContinuationToken currentToken)
        {
            return this.ListQueuesSegmentedAsync(currentToken, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>     
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="QueueResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<QueueResultSegment> ListQueuesSegmentedAsync(QueueContinuationToken currentToken, CancellationToken cancellationToken)
        {
            return this.ListQueuesSegmentedAsync(prefix: null, queueListingDetails: QueueListingDetails.None, maxResults: null, currentToken: currentToken, options: null, operationContext: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>  
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="QueueResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueContinuationToken currentToken)
        {
            return this.ListQueuesSegmentedAsync(prefix, currentToken, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>    
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="QueueResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueContinuationToken currentToken, CancellationToken cancellationToken)
        {
            return this.ListQueuesSegmentedAsync(prefix, QueueListingDetails.None, maxResults:null, currentToken:currentToken, options:null, operationContext:null, cancellationToken:cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="queueListingDetails">A <see cref="QueueListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="QueueResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueListingDetails queueListingDetails, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.ListQueuesSegmentedAsync(prefix, queueListingDetails, maxResults, currentToken, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="queueListingDetails">A <see cref="QueueListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is null, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="QueueResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueListingDetails queueListingDetails, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this);
            operationContext = operationContext ?? new OperationContext();

            ResultSegment<CloudQueue> resultSegment = await Executor.ExecuteAsync(
                this.ListQueuesImpl(prefix, maxResults, queueListingDetails, modifiedOptions, currentToken),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken).ConfigureAwait(false);

            return new QueueResultSegment(resultSegment.Results, (QueueContinuationToken)resultSegment.ContinuationToken);
        }
#endif

        /// <summary>
        /// Core implementation of the ListQueues method.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>
        /// <param name="queueListingDetails">A <see cref="QueueListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="currentToken">The continuation token.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that lists the queues.</returns>
        private RESTCommand<ResultSegment<CloudQueue>> ListQueuesImpl(string prefix, int? maxResults, QueueListingDetails queueListingDetails, QueueRequestOptions options, QueueContinuationToken currentToken)
        {
            ListingContext listingContext = new ListingContext(prefix, maxResults)
            {
                Marker = currentToken != null ? currentToken.NextMarker : null
            };

            RESTCommand<ResultSegment<CloudQueue>> getCmd = new RESTCommand<ResultSegment<CloudQueue>>(this.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommonUtility.GetListingLocationMode(currentToken);
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.List(uri, serverTimeout, listingContext, queueListingDetails, cnt, ctx, this.GetCanonicalizer(), this.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = async (cmd, resp, ctx, ct) =>
            {
                ListQueuesResponse listQueuesResponse = await ListQueuesResponse.ParseAsync(cmd.ResponseStream, ct).ConfigureAwait(false);

                List<CloudQueue> queuesList = listQueuesResponse.Queues.Select(item => new CloudQueue(item.Metadata, item.Name, this)).ToList();

                QueueContinuationToken continuationToken = null;
                if (listQueuesResponse.NextMarker != null)
                {
                    continuationToken = new QueueContinuationToken()
                    {
                        NextMarker = listQueuesResponse.NextMarker,
                        TargetLocation = cmd.CurrentResult.TargetLocation,
                    };
                }

                return new ResultSegment<CloudQueue>(queuesList)
                {
                    ContinuationToken = continuationToken,
                };
            };

            return getCmd;
        }

        #region Analytics

        /// <summary>
        /// Begins an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetServiceProperties(AsyncCallback callback, object state)
        {
            return this.BeginGetServiceProperties(null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetServiceProperties(QueueRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.GetServicePropertiesAsync(options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="asyncResult">The result returned from a prior call to <see cref="BeginGetServiceProperties(AsyncCallback, object)"/>.</param>
        /// <returns>A <see cref="ServiceProperties"/> object.</returns>
        public virtual ServiceProperties EndGetServiceProperties(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<ServiceProperties>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync()
        {
            return this.GetServicePropertiesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync(CancellationToken cancellationToken)
        {
            return this.GetServicePropertiesAsync(options:null, operationContext:null, cancellationToken:cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync(QueueRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.GetServicePropertiesAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsync(
                this.GetServicePropertiesImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Gets service properties for the Queue service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="ServiceProperties"/> containing the Queue service properties.</returns>
        [DoesServiceRequest]
        public virtual ServiceProperties GetServiceProperties(QueueRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = QueueRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();
            return Executor.ExecuteSync(
                this.GetServicePropertiesImpl(requestOptions),
                requestOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetServiceProperties(ServiceProperties properties, AsyncCallback callback, object state)
        {
            return this.BeginSetServiceProperties(properties, null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetServiceProperties(ServiceProperties properties, QueueRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            requestOptions = QueueRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();

            return CancellableAsyncResultTaskWrapper.Create(token => this.SetServicePropertiesAsync(properties, requestOptions, operationContext), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="asyncResult">The <see cref="IAsyncResult"/> returned from a prior call to <see cref="BeginSetServiceProperties(ServiceProperties, AsyncCallback, object)"/>.</param>
        public virtual void EndSetServiceProperties(IAsyncResult asyncResult)
        {
            CommonUtility.AssertNotNull(nameof(asyncResult), asyncResult);
            ((CancellableAsyncResultTaskWrapper)(asyncResult)).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties)
        {
            return this.SetServicePropertiesAsync(properties, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties, CancellationToken cancellationToken)
        {
            return this.SetServicePropertiesAsync(properties, default(QueueRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.SetServicePropertiesAsync(properties, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            options = QueueRequestOptions.ApplyDefaults(options, this);
            operationContext = operationContext ?? new OperationContext();
            return Executor.ExecuteAsync(
                this.SetServicePropertiesImpl(properties, options),
                options.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Sets service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void SetServiceProperties(ServiceProperties properties, QueueRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = QueueRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();
            Executor.ExecuteSync(this.SetServicePropertiesImpl(properties, requestOptions), requestOptions.RetryPolicy, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetServiceStats(AsyncCallback callback, object state)
        {
            return this.BeginGetServiceStats(null /* requestOptions */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetServiceStats(QueueRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.GetServiceStatsAsync(options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="ServiceStats"/> object.</returns>
        public virtual ServiceStats EndGetServiceStats(IAsyncResult asyncResult)
        {
            CommonUtility.AssertNotNull(nameof(asyncResult), asyncResult);
            return ((CancellableAsyncResultTaskWrapper<ServiceStats>)(asyncResult)).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync()
        {
            return this.GetServiceStatsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync(CancellationToken cancellationToken)
        {
            return this.GetServiceStatsAsync(options:null, operationContext:null, cancellationToken:cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync(QueueRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.GetServiceStatsAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsync(
                this.GetServiceStatsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Gets service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="ServiceStats"/> object.</returns>
        [DoesServiceRequest]
        public virtual ServiceStats GetServiceStats(QueueRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = QueueRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();
            return Executor.ExecuteSync(
                this.GetServiceStatsImpl(requestOptions),
                requestOptions.RetryPolicy,
                operationContext);
        }
#endif

        private RESTCommand<ServiceProperties> GetServicePropertiesImpl(QueueRequestOptions requestOptions)
        {
            RESTCommand<ServiceProperties> retCmd = new RESTCommand<ServiceProperties>(this.Credentials, this.StorageUri);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetServiceProperties(uri, serverTimeout, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            retCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) => QueueHttpResponseParsers.ReadServicePropertiesAsync(cmd.ResponseStream, ct);

            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }

        private RESTCommand<NullType> SetServicePropertiesImpl(ServiceProperties properties, QueueRequestOptions requestOptions)
        {
            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(this.BufferManager, (int)(1 * Constants.KB));
            try
            {
                properties.WriteServiceProperties(memoryStream);
            }
            catch (InvalidOperationException invalidOpException)
            {
                memoryStream.Dispose();
                throw new ArgumentException(invalidOpException.Message, "properties");
            }

            RESTCommand<NullType> retCmd = new RESTCommand<NullType>(this.Credentials, this.StorageUri);
            requestOptions.ApplyToStorageCommand(retCmd);
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.SetServiceProperties(uri, serverTimeout, cnt, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null /* md5 */, cmd, ctx);
            retCmd.StreamToDispose = memoryStream;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, null /* retVal */, cmd, ex);
            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }

        private RESTCommand<ServiceStats> GetServiceStatsImpl(QueueRequestOptions requestOptions)
        {
            if (RetryPolicies.LocationMode.PrimaryOnly == requestOptions.LocationMode)
            {
                throw new InvalidOperationException(SR.GetServiceStatsInvalidOperation);
            }

            RESTCommand<ServiceStats> retCmd = new RESTCommand<ServiceStats>(this.Credentials, this.StorageUri);
            requestOptions.ApplyToStorageCommand(retCmd);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetServiceStats(uri, serverTimeout, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            retCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) => QueueHttpResponseParsers.ReadServiceStatsAsync(cmd.ResponseStream, ct);
            return retCmd;
        }

        #endregion
    }
}
