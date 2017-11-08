using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Auth;
using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
 
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Table
{
public class CloudTable
{
    public CloudTableClient ServiceClient
    {
        get; private set;
    }

    public string Name
    {
        get; private set;
    }

    public Uri Uri
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

    public CloudTable(Uri tableAddress)
      : this(tableAddress, (StorageCredentials) null)
    {
        throw new System.NotImplementedException();
    }
    public CloudTable(Uri tableAbsoluteUri, StorageCredentials credentials)
      : this(new StorageUri(tableAbsoluteUri), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudTable(StorageUri tableAddress, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    internal CloudTable(string tableName, CloudTableClient client)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableResult> ExecuteAsync(TableOperation operation)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableResult> ExecuteAsync(TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableResult> ExecuteAsync(TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batch)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batch, TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batch, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    internal IEnumerable<DynamicTableEntity> ExecuteQuery(TableQuery query)
    {
        throw new System.NotImplementedException();
    }
    internal IEnumerable<DynamicTableEntity> ExecuteQuery(TableQuery query, TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }

    public virtual Task CreateAsync()
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task CreateAsync(TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task CreateAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<bool> CreateIfNotExistsAsync()
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<bool> CreateIfNotExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<bool> CreateIfNotExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task DeleteAsync()
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task DeleteAsync(TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task DeleteAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<bool> DeleteIfExistsAsync()
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<bool> DeleteIfExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<bool> DeleteIfExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<bool> ExistsAsync()
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<bool> ExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<bool> ExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task SetPermissionsAsync(TablePermissions permissions)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task SetPermissionsAsync(TablePermissions permissions, TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task SetPermissionsAsync(TablePermissions permissions, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<TablePermissions> GetPermissionsAsync()
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<TablePermissions> GetPermissionsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<TablePermissions> GetPermissionsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public virtual Task<TableQuerySegment<T>> ExecuteQuerySegmentedAsync<T>(TableQuery<T> query, TableContinuationToken token) where T : ITableEntity, new()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableQuerySegment<T>> ExecuteQuerySegmentedAsync<T>(TableQuery<T> query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext) where T : ITableEntity, new()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableQuerySegment<T>> ExecuteQuerySegmentedAsync<T>(TableQuery<T> query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where T : ITableEntity, new()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<T, TResult>(TableQuery<T> query, EntityResolver<TResult> resolver, TableContinuationToken token) where T : ITableEntity, new()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<T, TResult>(TableQuery<T> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext) where T : ITableEntity, new()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<T, TResult>(TableQuery<T> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where T : ITableEntity, new()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessTablePolicy policy)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessTablePolicy policy, string accessPolicyIdentifier)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessTablePolicy policy, string accessPolicyIdentifier, string startPartitionKey, string startRowKey, string endPartitionKey, string endRowKey)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessTablePolicy policy, string accessPolicyIdentifier, string startPartitionKey, string startRowKey, string endPartitionKey, string endRowKey, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
    private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    private string GetCanonicalName()
    {
        throw new System.NotImplementedException();
    }
}

}