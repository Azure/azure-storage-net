using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Auth;
using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
 
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Blob
{
public class CloudBlobContainer
{
    public CloudBlobClient ServiceClient
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

    public string Name
    {
        get; private set;
    }

    public IDictionary<string, string> Metadata
    {
        get; private set;
    }

    public BlobContainerProperties Properties
    {
        get; private set;
    }

    public CloudBlobContainer(Uri containerAddress)
      : this(containerAddress, (StorageCredentials) null)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlobContainer(Uri containerAddress, StorageCredentials credentials)
      : this(new StorageUri(containerAddress), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlobContainer(StorageUri containerAddress, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    internal CloudBlobContainer(string containerName, CloudBlobClient serviceClient)
      : this(new BlobContainerProperties(), (IDictionary<string, string>) new Dictionary<string, string>(), containerName, serviceClient)
    {
        throw new System.NotImplementedException();
    }
    internal CloudBlobContainer(BlobContainerProperties properties, IDictionary<string, string> metadata, string containerName, CloudBlobClient serviceClient)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(BlobContainerPublicAccessType accessType, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(BlobContainerPublicAccessType accessType, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> CreateIfNotExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> CreateIfNotExistsAsync(BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> CreateIfNotExistsAsync(BlobContainerPublicAccessType accessType, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> CreateIfNotExistsAsync(BlobContainerPublicAccessType accessType, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(BlobContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, BlobContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPermissionsAsync(BlobContainerPermissions permissions)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPermissionsAsync(BlobContainerPermissions permissions, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPermissionsAsync(BlobContainerPermissions permissions, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<BlobContainerPermissions> GetPermissionsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<BlobContainerPermissions> GetPermissionsAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<BlobContainerPermissions> GetPermissionsAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> ExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
	
    public virtual Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task FetchAttributesAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetMetadataAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId = null)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task RenewLeaseAsync(AccessCondition accessCondition)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ReleaseLeaseAsync(AccessCondition accessCondition)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<string> AcquireLeaseImpl(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<NullType> RenewLeaseImpl(AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<string> ChangeLeaseImpl(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<NullType> ReleaseLeaseImpl(AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<TimeSpan> BreakLeaseImpl(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> CreateContainerImpl(BlobRequestOptions options, BlobContainerPublicAccessType accessType)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> DeleteContainerImpl(AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> FetchAttributesImpl(AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<bool> ExistsImpl(BlobRequestOptions options, bool primaryOnly)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetMetadataImpl(AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetPermissionsImpl(BlobContainerPermissions acl, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<BlobContainerPermissions> GetPermissionsImpl(AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private IListBlobItem SelectListBlobItem(IListBlobEntry protocolItem)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<ResultSegment<IListBlobItem>> ListBlobsImpl(string prefix, int? maxResults, bool useFlatBlobListing, BlobListingDetails blobListingDetails, BlobRequestOptions options, BlobContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
    private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    private string GetSharedAccessCanonicalName()
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessBlobPolicy policy)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, string groupPolicyIdentifier)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
    public CloudPageBlob GetPageBlobReference(string blobName)
    {
        throw new System.NotImplementedException();
    }
    public CloudPageBlob GetPageBlobReference(string blobName, DateTimeOffset? snapshotTime)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlockBlob GetBlockBlobReference(string blobName)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlockBlob GetBlockBlobReference(string blobName, DateTimeOffset? snapshotTime)
    {
        throw new System.NotImplementedException();
    }
    public CloudAppendBlob GetAppendBlobReference(string blobName)
    {
        throw new System.NotImplementedException();
    }
    public CloudAppendBlob GetAppendBlobReference(string blobName, DateTimeOffset? snapshotTime)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlob GetBlobReference(string blobName)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlob GetBlobReference(string blobName, DateTimeOffset? snapshotTime)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlobDirectory GetDirectoryReference(string relativeAddress)
    {
        throw new System.NotImplementedException();
    }
}

}