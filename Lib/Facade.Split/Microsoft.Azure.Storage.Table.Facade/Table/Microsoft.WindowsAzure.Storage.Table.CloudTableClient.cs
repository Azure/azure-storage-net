using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Core.Auth;
using Microsoft.Azure.Storage.Core.Executor;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.RetryPolicies;
using Microsoft.Azure.Storage.Shared.Protocol;
using Microsoft.Azure.Storage.Table.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
 
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.Azure.Storage.Table
{
public class CloudTableClient
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

    public TableRequestOptions DefaultRequestOptions
    {
        get; set;
    }

    internal bool UsePathStyleUris
    {
        get; private set;
    }

    internal string AccountName
    {
        get
        {
            throw new System.NotImplementedException();
        }
        private set
        {
            throw new System.NotImplementedException();
        }
    }

    public CloudTableClient(Uri baseUri, StorageCredentials credentials)
      : this(new StorageUri(baseUri), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudTableClient(StorageUri storageUri, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<TableResultSegment> ListTablesSegmentedAsync(TableContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, TableContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, int? maxResults, TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, int? maxResults, TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ServiceProperties> GetServicePropertiesAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ServiceProperties> GetServicePropertiesAsync(TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ServiceProperties> GetServicePropertiesAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ServiceProperties> GetServicePropertiesImpl(TableRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetServicePropertiesAsync(ServiceProperties properties)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetServicePropertiesAsync(ServiceProperties properties, TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetServicePropertiesAsync(ServiceProperties properties, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetServicePropertiesImpl(ServiceProperties properties, TableRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ServiceStats> GetServiceStatsAsync()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceStats> GetServiceStatsAsync(TableRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<ServiceStats> GetServiceStatsAsync(TableRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ServiceStats> GetServiceStatsImpl(TableRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudTable GetTableReference(string tableName)
    {
        throw new System.NotImplementedException();
    }
    internal ICanonicalizer GetCanonicalizer()
    {
        throw new System.NotImplementedException();
    }
}

}