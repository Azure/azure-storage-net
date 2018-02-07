using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob.Protocol;
using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Core.Auth;
using Microsoft.Azure.Storage.Core.Executor;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.RetryPolicies;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
 
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.Azure.Storage.Blob
{
public class CloudBlobClient
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

    public BlobRequestOptions DefaultRequestOptions
    {
        get; set;
    }

    [Obsolete("Use DefaultRequestOptions.RetryPolicy.")]
    public IRetryPolicy RetryPolicy
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

    public string DefaultDelimiter
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

    internal bool UsePathStyleUris
    {
        get; private set;
    }

    public CloudBlobClient(Uri baseUri)
      : this(baseUri, (StorageCredentials) null)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlobClient(Uri baseUri, StorageCredentials credentials)
      : this(new StorageUri(baseUri), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlobClient(StorageUri storageUri, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(BlobContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix, BlobContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix, ContainerListingDetails detailsIncluded, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix, ContainerListingDetails detailsIncluded, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, BlobContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(Uri blobUri)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(Uri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(StorageUri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(StorageUri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ResultSegment<CloudBlobContainer>> ListContainersImpl(string prefix, ContainerListingDetails detailsIncluded, BlobContinuationToken currentToken, int? maxResults, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ICloudBlob> GetBlobReferenceImpl(StorageUri blobUri, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<ServiceProperties> GetServicePropertiesAsync()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceProperties> GetServicePropertiesAsync(BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceProperties> GetServicePropertiesAsync(BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ServiceProperties> GetServicePropertiesImpl(BlobRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task SetServicePropertiesAsync(ServiceProperties properties)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task SetServicePropertiesAsync(ServiceProperties properties, BlobRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task SetServicePropertiesAsync(ServiceProperties properties, BlobRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetServicePropertiesImpl(ServiceProperties properties, BlobRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<ServiceStats> GetServiceStatsAsync()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceStats> GetServiceStatsAsync(BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceStats> GetServiceStatsAsync(BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ServiceStats> GetServiceStatsImpl(BlobRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudBlobContainer GetRootContainerReference()
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudBlobContainer GetContainerReference(string containerName)
    {
        throw new System.NotImplementedException();
    }
    internal ICanonicalizer GetCanonicalizer()
    {
        throw new System.NotImplementedException();
    }
    private static void ParseUserPrefix(string prefix, out string containerName, out string listingPrefix)
    {
        throw new System.NotImplementedException();
    }
}

}