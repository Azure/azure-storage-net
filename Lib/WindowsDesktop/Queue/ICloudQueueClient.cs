using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

namespace Microsoft.WindowsAzure.Storage.Queue
{
    public interface ICloudQueueClient
    {
        /// <summary>
        /// Gets or sets the authentication scheme to use to sign HTTP requests.
        /// </summary>
        /// <remarks>
        /// This property is set only when Shared Key or Shared Key Lite credentials are used; it does not apply to authentication via a shared access signature 
        /// or anonymous access.
        /// </remarks>
        AuthenticationScheme AuthenticationScheme { get; set; }

        /// <summary>
        /// Gets or sets a buffer manager that implements the <see cref="IBufferManager"/> interface, 
        /// specifying a buffer pool for use with operations against the Queue service client.
        /// </summary>
        /// <value>An object of type <see cref="IBufferManager"/>.</value>
        IBufferManager BufferManager { get; set; }

        /// <summary>
        /// Gets the account credentials used to create the Queue service client.
        /// </summary>
        /// <value>A <see cref="StorageCredentials"/> object.</value>
        StorageCredentials Credentials { get; }

        /// <summary>
        /// Gets the base URI for the Queue service client, at the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> object for the Queue service client, at the primary location.</value>
        Uri BaseUri { get; }

        /// <summary>
        /// Gets the Queue service endpoints for both the primary and secondary locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing Queue service URIs for both the primary and secondary locations.</value>
        StorageUri StorageUri { get; }

        /// <summary>
        /// Gets and sets the default request options for requests made via the Queue service client.
        /// </summary>
        /// <value>A <see cref="QueueRequestOptions"/> object.</value>
        QueueRequestOptions DefaultRequestOptions { get; set; }

        /// <summary>
        /// Gets or sets the default retry policy for requests made via the Queue service client.
        /// </summary>
        /// <value>An object of type <see cref="IRetryPolicy"/>.</value>
        [Obsolete("Use DefaultRequestOptions.RetryPolicy.")]
        IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Gets or sets the default location mode for requests made via the Queue service client.
        /// </summary>
        /// <value>A <see cref="Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode"/> enumeration value.</value>
        [Obsolete("Use DefaultRequestOptions.LocationMode.")]
        LocationMode? LocationMode { get; set; }

        /// <summary>
        /// Gets or sets the default server timeout for requests made via the Queue service client.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the server timeout interval.</value>
        [Obsolete("Use DefaultRequestOptions.ServerTimeout.")]
        TimeSpan? ServerTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time across all potential retries.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the maximum execution time across all potential retries.</value>
        [Obsolete("Use DefaultRequestOptions.MaximumExecutionTime.")]
        TimeSpan? MaximumExecutionTime { get; set; }

        /// <summary>
        /// Returns an enumerable collection of the queues in the storage account whose names begin with the specified prefix and that are retrieved lazily.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="queueListingDetails">A <see cref="QueueListingDetails"/> enumeration value that indicates which details to include in the listing.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects of type <see cref="CloudQueue"/> that are retrieved lazily.</returns>
        IEnumerable<CloudQueue> ListQueues(string prefix = null, QueueListingDetails queueListingDetails = QueueListingDetails.None, QueueRequestOptions options = null, OperationContext operationContext = null);

        /// <summary>
        /// Returns a result segment containing a collection of queues.
        /// </summary>
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> continuation token returned by a previous listing operation.</param>
        /// <returns>A <see cref="QueueResultSegment"/> object.</returns>
        QueueResultSegment ListQueuesSegmented(QueueContinuationToken currentToken);

        /// <summary>
        /// Returns a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> continuation token returned by a previous listing operation.</param>
        /// <returns>A <see cref="QueueResultSegment"/> object.</returns>
        QueueResultSegment ListQueuesSegmented(string prefix, QueueContinuationToken currentToken);

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
        QueueResultSegment ListQueuesSegmented(string prefix, QueueListingDetails queueListingDetails, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options = null, OperationContext operationContext = null);

        /// <summary>
        /// Begins an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        ICancellableAsyncResult BeginListQueuesSegmented(QueueContinuationToken currentToken, AsyncCallback callback, object state);

        /// <summary>
        /// Begins an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        ICancellableAsyncResult BeginListQueuesSegmented(string prefix, QueueContinuationToken currentToken, AsyncCallback callback, object state);

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
        ICancellableAsyncResult BeginListQueuesSegmented(string prefix, QueueListingDetails queueListingDetails, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="QueueResultSegment"/> object.</returns>
        QueueResultSegment EndListQueuesSegmented(IAsyncResult asyncResult);

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>      
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="QueueResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<QueueResultSegment> ListQueuesSegmentedAsync(QueueContinuationToken currentToken);

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>     
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="QueueResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<QueueResultSegment> ListQueuesSegmentedAsync(QueueContinuationToken currentToken, CancellationToken cancellationToken);

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>  
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="QueueResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueContinuationToken currentToken);

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of queues.
        /// </summary>
        /// <param name="prefix">A string containing the queue name prefix.</param>    
        /// <param name="currentToken">A <see cref="QueueContinuationToken"/> returned by a previous listing operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="QueueResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueContinuationToken currentToken, CancellationToken cancellationToken);

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
        Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueListingDetails queueListingDetails, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options, OperationContext operationContext);

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
        Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueListingDetails queueListingDetails, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken);

        /// <summary>
        /// Begins an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        ICancellableAsyncResult BeginGetServiceProperties(AsyncCallback callback, object state);

        /// <summary>
        /// Begins an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        ICancellableAsyncResult BeginGetServiceProperties(QueueRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="asyncResult">The result returned from a prior call to <see cref="CloudQueueClient.BeginGetServiceProperties(System.AsyncCallback,object)"/>.</param>
        /// <returns>A <see cref="ServiceProperties"/> object.</returns>
        ServiceProperties EndGetServiceProperties(IAsyncResult asyncResult);

        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<ServiceProperties> GetServicePropertiesAsync();

        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<ServiceProperties> GetServicePropertiesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<ServiceProperties> GetServicePropertiesAsync(QueueRequestOptions requestOptions, OperationContext operationContext);

        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Queue service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<ServiceProperties> GetServicePropertiesAsync(QueueRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken);

        /// <summary>
        /// Gets service properties for the Queue service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="ServiceProperties"/> containing the Queue service properties.</returns>
        [DoesServiceRequest]
        ServiceProperties GetServiceProperties(QueueRequestOptions requestOptions = null, OperationContext operationContext = null);

        /// <summary>
        /// Begins an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        ICancellableAsyncResult BeginSetServiceProperties(ServiceProperties properties, AsyncCallback callback, object state);

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
        ICancellableAsyncResult BeginSetServiceProperties(ServiceProperties properties, QueueRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="asyncResult">The <see cref="IAsyncResult"/> returned from a prior call to <see cref="CloudQueueClient.BeginSetServiceProperties(Microsoft.WindowsAzure.Storage.Shared.Protocol.ServiceProperties,System.AsyncCallback,object)"/>.</param>
        void EndSetServiceProperties(IAsyncResult asyncResult);

        /// <summary>
        /// Initiates an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task SetServicePropertiesAsync(ServiceProperties properties);

        /// <summary>
        /// Initiates an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task SetServicePropertiesAsync(ServiceProperties properties, CancellationToken cancellationToken);

        /// <summary>
        /// Initiates an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task SetServicePropertiesAsync(ServiceProperties properties, QueueRequestOptions options, OperationContext operationContext);

        /// <summary>
        /// Initiates an asynchronous operation to set service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task SetServicePropertiesAsync(ServiceProperties properties, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken);

        /// <summary>
        /// Sets service properties for the Queue service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        void SetServiceProperties(ServiceProperties properties, QueueRequestOptions requestOptions = null, OperationContext operationContext = null);

        /// <summary>
        /// Begins an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        ICancellableAsyncResult BeginGetServiceStats(AsyncCallback callback, object state);

        /// <summary>
        /// Begins an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        ICancellableAsyncResult BeginGetServiceStats(QueueRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="ServiceStats"/> object.</returns>
        ServiceStats EndGetServiceStats(IAsyncResult asyncResult);

        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<ServiceStats> GetServiceStatsAsync();

        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<ServiceStats> GetServiceStatsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<ServiceStats> GetServiceStatsAsync(QueueRequestOptions requestOptions, OperationContext operationContext);

        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        Task<ServiceStats> GetServiceStatsAsync(QueueRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken);

        /// <summary>
        /// Gets service stats for the secondary Queue service endpoint.
        /// </summary>
        /// <param name="requestOptions">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="ServiceStats"/> object.</returns>
        [DoesServiceRequest]
        ServiceStats GetServiceStats(QueueRequestOptions requestOptions = null, OperationContext operationContext = null);

        /// <summary>
        /// Returns a reference to a <see cref="CloudQueue"/> object with the specified name.
        /// </summary>
        /// <param name="queueName">A string containing the name of the queue.</param>
        /// <returns>A <see cref="CloudQueue"/> object.</returns>
        CloudQueue GetQueueReference(string queueName);
    }
}