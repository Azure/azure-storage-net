using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
 
 
using System.Threading;
using System.Threading.Tasks;
 
 
 
namespace Microsoft.WindowsAzure.Storage.Blob
{
public class CloudPageBlob : CloudBlob, ICloudBlob, IListBlobItem
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

    public CloudPageBlob(Uri blobAbsoluteUri)
      : this(blobAbsoluteUri, (StorageCredentials) null)
    {
            throw new System.NotImplementedException();
        }
        public CloudPageBlob(Uri blobAbsoluteUri, StorageCredentials credentials)
      : this(blobAbsoluteUri, new DateTimeOffset?(), credentials)
    {
            throw new System.NotImplementedException();
        }
        public CloudPageBlob(Uri blobAbsoluteUri, DateTimeOffset? snapshotTime, StorageCredentials credentials)
      : this(new StorageUri(blobAbsoluteUri), snapshotTime, credentials)
    {
            throw new System.NotImplementedException();
        }

        public CloudPageBlob(StorageUri blobAbsoluteUri, DateTimeOffset? snapshotTime, StorageCredentials credentials)
      : base(blobAbsoluteUri, snapshotTime, credentials)
    {
        throw new System.NotImplementedException();
    }
    internal CloudPageBlob(string blobName, DateTimeOffset? snapshotTime, CloudBlobContainer container)
      : base(blobName, snapshotTime, container)
    {
        throw new System.NotImplementedException();
    }
    internal CloudPageBlob(BlobAttributes attributes, CloudBlobClient serviceClient)
      : base(attributes, serviceClient)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudBlobStream> OpenWriteAsync(long? size)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudBlobStream> OpenWriteAsync(long? size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudBlobStream> OpenWriteAsync(long? size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
    public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(long size)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(long size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(long size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ResizeAsync(long size)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ResizeAsync(long size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ResizeAsync(long size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetSequenceNumberAsync(SequenceNumberAction sequenceNumberAction, long? sequenceNumber)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetSequenceNumberAsync(SequenceNumberAction sequenceNumberAction, long? sequenceNumber, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetSequenceNumberAsync(SequenceNumberAction sequenceNumberAction, long? sequenceNumber, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<PageRange>> GetPageRangesAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<PageRange>> GetPageRangesAsync(long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<PageRange>> GetPageRangesAsync(long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<IEnumerable<PageDiffRange>> GetPageRangesDiffAsync(DateTimeOffset previousSnapshotTime)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<IEnumerable<PageDiffRange>> GetPageRangesDiffAsync(DateTimeOffset previousSnapshotTime, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<IEnumerable<PageDiffRange>> GetPageRangesDiffAsync(DateTimeOffset previousSnapshotTime, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<CloudPageBlob> CreateSnapshotAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudPageBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudPageBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task WritePagesAsync(Stream pageData, long startOffset, string contentMD5)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task WritePagesAsync(Stream pageData, long startOffset, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task WritePagesAsync(Stream pageData, long startOffset, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ClearPagesAsync(long startOffset, long length)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ClearPagesAsync(long startOffset, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ClearPagesAsync(long startOffset, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(CloudPageBlob source)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<string> StartIncrementalCopyAsync(CloudPageBlob sourceSnapshot)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<string> StartCopyAsync(CloudPageBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<string> StartCopyAsync(CloudPageBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual Task<string> StartIncrementalCopyAsync(CloudPageBlob sourceSnapshot, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> CreateImpl(long sizeInBytes, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> ResizeImpl(long sizeInBytes, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetSequenceNumberImpl(SequenceNumberAction sequenceNumberAction, long? sequenceNumber, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<IEnumerable<PageRange>> GetPageRangesImpl(long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<IEnumerable<PageDiffRange>> GetPageRangesDiffImpl(DateTimeOffset previousSnapshotTime, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> PutPageImpl(Stream pageData, long startOffset, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> ClearPageImpl(long startOffset, long length, AccessCondition accessCondition, BlobRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
}

}