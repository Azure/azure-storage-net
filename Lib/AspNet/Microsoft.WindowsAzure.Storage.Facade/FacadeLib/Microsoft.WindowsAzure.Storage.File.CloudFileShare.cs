using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Auth;
using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.File.Protocol;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
 
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.File
{
public class CloudFileShare
{
    public CloudFileClient ServiceClient
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

    public DateTimeOffset? SnapshotTime
    {
        get; internal set;
    }

    public bool IsSnapshot
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public Uri SnapshotQualifiedUri
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public StorageUri SnapshotQualifiedStorageUri
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public string Name
    {
        get; private set;
    }

    public IDictionary<string, string> Metadata
    {
        get; private set;
    }

    public FileShareProperties Properties
    {
        get; private set;
    }

    public CloudFileShare(Uri shareAddress)
      : this(shareAddress, (StorageCredentials) null)
    {
        throw new System.NotImplementedException();
    }
    public CloudFileShare(Uri shareAddress, StorageCredentials credentials)
      : this(new StorageUri(shareAddress), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudFileShare(Uri shareAddress, DateTimeOffset? snapshotTime, StorageCredentials credentials)
      : this(new StorageUri(shareAddress), snapshotTime, credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudFileShare(StorageUri shareAddress, StorageCredentials credentials)
      : this(shareAddress, new DateTimeOffset?(), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudFileShare(StorageUri shareAddress, DateTimeOffset? snapshotTime, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    internal CloudFileShare(string shareName, DateTimeOffset? snapshotTime, CloudFileClient serviceClient)
      : this(new FileShareProperties(), (IDictionary<string, string>) new Dictionary<string, string>(), shareName, snapshotTime, serviceClient)
    {
        throw new System.NotImplementedException();
    }
    internal CloudFileShare(FileShareProperties properties, IDictionary<string, string> metadata, string shareName, DateTimeOffset? snapshotTime, CloudFileClient serviceClient)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task CreateAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> CreateIfNotExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task DeleteAsync()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task DeleteAsync(DeleteShareSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<bool> DeleteIfExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<bool> DeleteIfExistsAsync(DeleteShareSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> ExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task FetchAttributesAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPermissionsAsync(FileSharePermissions permissions)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPermissionsAsync(FileSharePermissions permissions, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPermissionsAsync(FileSharePermissions permissions, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPropertiesAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPropertiesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPropertiesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<FileSharePermissions> GetPermissionsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<FileSharePermissions> GetPermissionsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<FileSharePermissions> GetPermissionsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ShareStats> GetStatsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ShareStats> GetStatsAsync(FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ShareStats> GetStatsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetMetadataAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> CreateShareImpl(FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<CloudFileShare> SnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> DeleteShareImpl(DeleteShareSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> FetchAttributesImpl(AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<bool> ExistsImpl(FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetPermissionsImpl(FileSharePermissions acl, AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<FileSharePermissions> GetPermissionsImpl(AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ShareStats> GetStatsImpl(FileRequestOptions requestOptions)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetMetadataImpl(AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetPropertiesImpl(AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal void AssertNoSnapshot()
    {
        throw new System.NotImplementedException();
    }
    private string GetSharedAccessCanonicalName()
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessFilePolicy policy)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessFilePolicy policy, string groupPolicyIdentifier)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessFilePolicy policy, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
    private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudFileDirectory GetRootDirectoryReference()
    {
        throw new System.NotImplementedException();
    }
}

}