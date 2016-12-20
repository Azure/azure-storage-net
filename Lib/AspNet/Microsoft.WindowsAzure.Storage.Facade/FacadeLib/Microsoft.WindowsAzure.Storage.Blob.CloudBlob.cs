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
using System.Net;
 
using System.Threading;
using System.Threading.Tasks;
 
 
namespace Microsoft.WindowsAzure.Storage.Blob
{
public class CloudBlob : IListBlobItem
{

    public CloudBlobClient ServiceClient
    {
        get; private set;
    }

    public int StreamMinimumReadSizeInBytes
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

    public BlobProperties Properties
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public IDictionary<string, string> Metadata
    {
        get
        {
            throw new System.NotImplementedException();
        }
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
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public DateTimeOffset? SnapshotTime
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

    public CopyState CopyState
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

    public CloudBlobContainer Container
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public CloudBlobDirectory Parent
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public BlobType BlobType
    {
        get
        {
            throw new System.NotImplementedException();
        }
        internal set
        {
            throw new System.NotImplementedException();
        }
    }

    public CloudBlob(Uri blobAbsoluteUri)
      : this(blobAbsoluteUri, (StorageCredentials) null)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlob(Uri blobAbsoluteUri, StorageCredentials credentials)
      : this(blobAbsoluteUri, new DateTimeOffset?(), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlob(Uri blobAbsoluteUri, DateTimeOffset? snapshotTime, StorageCredentials credentials)
      : this(new StorageUri(blobAbsoluteUri), snapshotTime, credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlob(StorageUri blobAbsoluteUri, DateTimeOffset? snapshotTime, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    internal CloudBlob(string blobName, DateTimeOffset? snapshotTime, CloudBlobContainer container)
    {
        throw new System.NotImplementedException();
    }
    internal CloudBlob(BlobAttributes attributes, CloudBlobClient serviceClient)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<Stream> OpenReadAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadToStreamAsync(Stream target)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
    [DoesServiceRequest]
    public virtual Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
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
    public virtual Task SetPropertiesAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
    [DoesServiceRequest]
    public virtual Task<CloudBlob> SnapshotAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudBlob> SnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudBlob> SnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(Uri source)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task AbortCopyAsync(string copyId)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> GetBlobImpl(BlobAttributes attributes, Stream destStream, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> FetchAttributesImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<bool> ExistsImpl(BlobAttributes attributes, BlobRequestOptions options, bool primaryOnly)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetMetadataImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetPropertiesImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> DeleteBlobImpl(BlobAttributes attributes, DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<string> AcquireLeaseImpl(BlobAttributes attributes, TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> RenewLeaseImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<string> ChangeLeaseImpl(BlobAttributes attributes, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> ReleaseLeaseImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<TimeSpan> BreakLeaseImpl(BlobAttributes attributes, TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<string> StartCopyImpl(BlobAttributes attributes, Uri source, bool incrementalCopy, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> AbortCopyImpl(BlobAttributes attributes, string copyId, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<CloudBlob> SnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal static Uri SourceBlobToUri(CloudBlob source)
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
    public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string groupPolicyIdentifier)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
    private string GetCanonicalName(bool ignoreSnapshotTime)
    {
        throw new System.NotImplementedException();
    }
    private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
}

}