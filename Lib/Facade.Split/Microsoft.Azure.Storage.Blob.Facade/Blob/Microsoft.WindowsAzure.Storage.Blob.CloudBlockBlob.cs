using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob.Protocol;
using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Core.Executor;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
 
using System.Text;
using System.Threading;
using System.Threading.Tasks;
 
 
namespace Microsoft.Azure.Storage.Blob
{
public class CloudBlockBlob : CloudBlob, ICloudBlob, IListBlobItem
{

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

    internal bool IsStreamWriteSizeModified
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public CloudBlockBlob(Uri blobAbsoluteUri)
      : this(blobAbsoluteUri, (StorageCredentials) null)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlockBlob(Uri blobAbsoluteUri, StorageCredentials credentials)
      : this(blobAbsoluteUri, new DateTimeOffset?(), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlockBlob(Uri blobAbsoluteUri, DateTimeOffset? snapshotTime, StorageCredentials credentials)
      : this(new StorageUri(blobAbsoluteUri), snapshotTime, credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudBlockBlob(StorageUri blobAbsoluteUri, DateTimeOffset? snapshotTime, StorageCredentials credentials)
      : base(blobAbsoluteUri, snapshotTime, credentials)
    {
        throw new System.NotImplementedException();
    }
    internal CloudBlockBlob(string blobName, DateTimeOffset? snapshotTime, CloudBlobContainer container)
      : base(blobName, snapshotTime, container)
    {
        throw new System.NotImplementedException();
    }
    internal CloudBlockBlob(BlobAttributes attributes, CloudBlobClient serviceClient)
      : base(attributes, serviceClient)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<CloudBlobStream> OpenWriteAsync()
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<CloudBlobStream> OpenWriteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<CloudBlobStream> OpenWriteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task UploadFromStreamAsync(Stream source)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadFromStreamAsync(Stream source, long length)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadTextAsync(string content)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<string> DownloadTextAsync()
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<CloudBlockBlob> CreateSnapshotAsync()
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<CloudBlockBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<CloudBlockBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task PutBlockListAsync(IEnumerable<string> blockList)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task PutBlockListAsync(IEnumerable<string> blockList, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task PutBlockListAsync(IEnumerable<string> blockList, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<IEnumerable<ListBlockItem>> DownloadBlockListAsync()
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<IEnumerable<ListBlockItem>> DownloadBlockListAsync(BlockListingFilter blockListingFilter, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<IEnumerable<ListBlockItem>> DownloadBlockListAsync(BlockListingFilter blockListingFilter, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<string> StartCopyAsync(CloudBlockBlob source)
    {
        throw new System.NotImplementedException();
    }

    public virtual Task<string> StartCopyAsync(CloudBlockBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<string> StartCopyAsync(CloudBlockBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public virtual Task SetStandardBlobTierAsync(StandardBlobTier standardBlobTier)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task SetStandardBlobTierAsync(StandardBlobTier standardBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task SetStandardBlobTierAsync(StandardBlobTier standardBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<CloudBlockBlob> CreateSnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> PutBlobImpl(Stream stream, long? length, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<NullType> PutBlockImpl(Stream source, string blockId, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<NullType> PutBlockListImpl(IEnumerable<PutBlockListItem> blocks, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal RESTCommand<IEnumerable<ListBlockItem>> GetBlockListImpl(BlockListingFilter typesOfBlocks, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetStandardBlobTierImpl(StandardBlobTier standardBlobTier, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private IEnumerable<Stream> OpenMultiSubStream(Stream wrappedStream, long? length, SemaphoreSlim mutex)
    {
        throw new System.NotImplementedException();
    }
    internal void CheckAdjustBlockSize(long? streamLength)
    {
        throw new System.NotImplementedException();
    }
}

}