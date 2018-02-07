using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
 
 
namespace Microsoft.Azure.Storage.Blob
{
public interface ICloudBlob : IListBlobItem
{
    string Name
    {
        get;
    }

    CloudBlobClient ServiceClient
    {
        get;
    }

    int StreamWriteSizeInBytes
    {
        get; set;
    }

    int StreamMinimumReadSizeInBytes
    {
        get; set;
    }

    BlobProperties Properties
    {
        get;
    }

    IDictionary<string, string> Metadata
    {
        get;
    }

    DateTimeOffset? SnapshotTime
    {
        get;
    }

    bool IsSnapshot
    {
        get;
    }

    Uri SnapshotQualifiedUri
    {
        get;
    }

    StorageUri SnapshotQualifiedStorageUri
    {
        get;
    }

    CopyState CopyState
    {
        get;
    }

    BlobType BlobType
    {
        get;
    }

    Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task UploadFromStreamAsync(Stream source);

    Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task UploadFromStreamAsync(Stream source, long length);

    Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task UploadFromByteArrayAsync(byte[] buffer, int index, int count);

    Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task DownloadToStreamAsync(Stream target);

    Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task<int> DownloadToByteArrayAsync(byte[] target, int index);

    Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length);

    Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length);

    Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task<bool> ExistsAsync();

    Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext);

    Task FetchAttributesAsync();

    Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task SetMetadataAsync();

    Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task SetPropertiesAsync();

    Task SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task DeleteAsync();

    Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task<bool> DeleteIfExistsAsync();

    Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId = null);

    Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task RenewLeaseAsync(AccessCondition accessCondition);

    Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition);

    Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task ReleaseLeaseAsync(AccessCondition accessCondition);

    Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod);

    Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    Task AbortCopyAsync(string copyId);

    Task AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);

    string GetSharedAccessSignature(SharedAccessBlobPolicy policy);

    string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers);

    string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string groupPolicyIdentifier);

    string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange);
}

}