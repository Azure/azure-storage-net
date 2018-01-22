using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Auth;
using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.File.Protocol;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
 
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.File
{
public class CloudFileClient
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

    public FileRequestOptions DefaultRequestOptions
    {
        get; set;
    }

    internal bool UsePathStyleUris
    {
        get; private set;
    }

    public CloudFileClient(Uri baseUri, StorageCredentials credentials)
      : this(new StorageUri(baseUri), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudFileClient(StorageUri storageUri, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ShareResultSegment> ListSharesSegmentedAsync(FileContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ShareResultSegment> ListSharesSegmentedAsync(string prefix, FileContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ShareResultSegment> ListSharesSegmentedAsync(string prefix, ShareListingDetails detailsIncluded, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ShareResultSegment> ListSharesSegmentedAsync(string prefix, ShareListingDetails detailsIncluded, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<FileServiceProperties> GetServicePropertiesAsync()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<FileServiceProperties> GetServicePropertiesAsync(FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<FileServiceProperties> GetServicePropertiesAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task SetServicePropertiesAsync(FileServiceProperties properties)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetServicePropertiesAsync(FileServiceProperties properties, FileRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetServicePropertiesAsync(FileServiceProperties properties, FileRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ResultSegment<CloudFileShare>> ListSharesImpl(string prefix, ShareListingDetails detailsIncluded, FileContinuationToken currentToken, int? maxResults, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<FileServiceProperties> GetServicePropertiesImpl(FileRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetServicePropertiesImpl(FileServiceProperties properties, FileRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudFileShare GetShareReference(string shareName)
    {
        throw new System.NotImplementedException();
    }
    public CloudFileShare GetShareReference(string shareName, DateTimeOffset? snapshotTime)
    {
        throw new System.NotImplementedException();
    }
    internal ICanonicalizer GetCanonicalizer()
    {
        throw new System.NotImplementedException();
    }
}

}