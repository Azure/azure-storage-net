using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
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
using System.Linq;
using System.Net;
 
using System.Text;
using System.Threading;
using System.Threading.Tasks;
 
 
 
namespace Microsoft.WindowsAzure.Storage.File
{
public class CloudFile : IListFileItem
{
    public CloudFileClient ServiceClient
    {
        get; private set;
    }

    public int StreamWriteSizeInBytes
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

    public FileProperties Properties
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

    internal Uri SnapshotQualifiedUri
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal StorageUri SnapshotQualifiedStorageUri
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public Microsoft.WindowsAzure.Storage.Blob.CopyState CopyState
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

    public CloudFileShare Share
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public CloudFileDirectory Parent
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public CloudFile(Uri fileAbsoluteUri)
      : this(fileAbsoluteUri, (StorageCredentials) null)
    {
        throw new System.NotImplementedException();
    }
    public CloudFile(Uri fileAbsoluteUri, StorageCredentials credentials)
      : this(new StorageUri(fileAbsoluteUri), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudFile(StorageUri fileAbsoluteUri, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    internal CloudFile(StorageUri uri, string fileName, CloudFileShare share)
    {
        throw new System.NotImplementedException();
    }
    internal CloudFile(CloudFileAttributes attributes, CloudFileClient serviceClient)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<Stream> OpenReadAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudFileStream> OpenWriteAsync(long? size)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudFileStream> OpenWriteAsync(long? size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudFileStream> OpenWriteAsync(long? size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromStreamAsync(Stream source)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromStreamAsync(Stream source, long length)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadTextAsync(string content)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadToStreamAsync(Stream target)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> DownloadTextAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(long size)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
    public virtual Task DeleteAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<FileRange>> ListRangesAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<FileRange>> ListRangesAsync(long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<FileRange>> ListRangesAsync(long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
    public virtual Task ResizeAsync(long size)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ResizeAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ResizeAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
    [DoesServiceRequest]
    public virtual Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task ClearRangeAsync(long startOffset, long length)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ClearRangeAsync(long startOffset, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ClearRangeAsync(long startOffset, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(Uri source)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(CloudBlob source)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(CloudFile source)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(CloudBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task AbortCopyAsync(string copyId)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> GetFileImpl(Stream destStream, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> CreateImpl(long sizeInBytes, AccessCondition accessCondition, FileRequestOptions options)
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
    private RESTCommand<NullType> DeleteFileImpl(AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<IEnumerable<FileRange>> ListRangesImpl(long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetPropertiesImpl(AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> ResizeImpl(long sizeInBytes, AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetMetadataImpl(AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> PutRangeImpl(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> ClearRangeImpl(long startOffset, long length, AccessCondition accessCondition, FileRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal static Uri SourceFileToUri(CloudFile source)
    {
        throw new System.NotImplementedException();
    }
    internal void AssertNoSnapshot()
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
    public string GetSharedAccessSignature(SharedAccessFilePolicy policy, SharedAccessFileHeaders headers)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessFilePolicy policy, SharedAccessFileHeaders headers, string groupPolicyIdentifier)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessFilePolicy policy, SharedAccessFileHeaders headers, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
    private string GetCanonicalName()
    {
        throw new System.NotImplementedException();
    }
    private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
}

}