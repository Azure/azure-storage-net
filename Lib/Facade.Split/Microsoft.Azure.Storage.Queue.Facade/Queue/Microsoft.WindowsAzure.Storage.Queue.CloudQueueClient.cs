using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Core.Auth;
using Microsoft.Azure.Storage.Core.Executor;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Queue.Protocol;
using Microsoft.Azure.Storage.RetryPolicies;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
 
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.Azure.Storage.Queue
{
public class CloudQueueClient
{

    public AuthenticationScheme AuthenticationScheme
    {
        get
        {
            throw new System.NotImplementedException();
        }
        set
        {
            throw new System.NotImplementedException();
        }
    }

    public IBufferManager BufferManager
    {
        get; set;
    }

    public StorageCredentials Credentials
    {
        get; private set;
    }

    public Uri BaseUri
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public StorageUri StorageUri
    {
        get; private set;
    }

    public QueueRequestOptions DefaultRequestOptions
    {
        get; set;
    }

    internal bool UsePathStyleUris
    {
        get; private set;
    }

    public CloudQueueClient(Uri baseUri, StorageCredentials credentials)
      : this(new StorageUri(baseUri), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudQueueClient(StorageUri storageUri, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<QueueResultSegment> ListQueuesSegmentedAsync(QueueContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueListingDetails detailsIncluded, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix, QueueListingDetails detailsIncluded, int? maxResults, QueueContinuationToken currentToken, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ResultSegment<CloudQueue>> ListQueuesImpl(string prefix, int? maxResults, QueueListingDetails detailsIncluded, QueueRequestOptions options, QueueContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceProperties> GetServicePropertiesAsync()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceProperties> GetServicePropertiesAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceProperties> GetServicePropertiesAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ServiceProperties> GetServicePropertiesImpl(QueueRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task SetServicePropertiesAsync(ServiceProperties properties)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task SetServicePropertiesAsync(ServiceProperties properties, QueueRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task SetServicePropertiesAsync(ServiceProperties properties, QueueRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetServicePropertiesImpl(ServiceProperties properties, QueueRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceStats> GetServiceStatsAsync()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceStats> GetServiceStatsAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceStats> GetServiceStatsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ServiceStats> GetServiceStatsImpl(QueueRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudQueue GetQueueReference(string queueName)
    {
        throw new System.NotImplementedException();
    }
    internal ICanonicalizer GetCanonicalizer()
    {
        throw new System.NotImplementedException();
    }
}

}