using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Blob
{
public class CloudBlobDirectory : IListBlobItem
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

    public CloudBlobContainer Container
    {
        get; private set;
    }

    public CloudBlobDirectory Parent
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public string Prefix
    {
        get; private set;
    }

    internal CloudBlobDirectory(StorageUri uri, string prefix, CloudBlobContainer container)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(BlobContinuationToken currentToken)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
     
    public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudPageBlob GetPageBlobReference(string blobName)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudPageBlob GetPageBlobReference(string blobName, DateTimeOffset? snapshotTime)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudBlockBlob GetBlockBlobReference(string blobName)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudBlockBlob GetBlockBlobReference(string blobName, DateTimeOffset? snapshotTime)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudAppendBlob GetAppendBlobReference(string blobName)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudAppendBlob GetAppendBlobReference(string blobName, DateTimeOffset? snapshotTime)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudBlob GetBlobReference(string blobName)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudBlob GetBlobReference(string blobName, DateTimeOffset? snapshotTime)
    {
        throw new System.NotImplementedException();
    }
    public virtual CloudBlobDirectory GetDirectoryReference(string itemName)
    {
        throw new System.NotImplementedException();
    }
}

}