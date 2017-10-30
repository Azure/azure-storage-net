using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
namespace Microsoft.WindowsAzure.Storage.Blob
{
public sealed class BlobProperties
{
    public string CacheControl
    {
        get; set;
    }

    public string ContentDisposition
    {
        get; set;
    }

    public string ContentEncoding
    {
        get; set;
    }

    public string ContentLanguage
    {
        get; set;
    }

    public long Length
    {
        get; internal set;
    }

    public string ContentMD5
    {
        get; set;
    }

    public string ContentType
    {
        get; set;
    }

    public string ETag
    {
        get; internal set;
    }

    public DateTimeOffset? LastModified
    {
        get; internal set;
    }

    public BlobType BlobType
    {
        get; internal set;
    }

    public LeaseStatus LeaseStatus
    {
        get; internal set;
    }

    public LeaseState LeaseState
    {
        get; internal set;
    }

    public LeaseDuration LeaseDuration
    {
        get; internal set;
    }

    public long? PageBlobSequenceNumber
    {
        get; internal set;
    }

    public int? AppendBlobCommittedBlockCount
    {
        get; internal set;
    }

    public bool IsServerEncrypted
    {
        get; internal set;
    }

    public bool IsIncrementalCopy
    {
        get; internal set;
    }

    public Microsoft.WindowsAzure.Storage.Blob.StandardBlobTier? StandardBlobTier
    {
        get; internal set;
    }

    public Microsoft.WindowsAzure.Storage.Blob.RehydrationStatus? RehydrationStatus
    {
        get; internal set;
    }

    public Microsoft.WindowsAzure.Storage.Blob.PremiumPageBlobTier? PremiumPageBlobTier
    {
        get; internal set;
    }

    public bool? BlobTierInferred
    {
        get; internal set;
    }

    public BlobProperties()
    {
        throw new System.NotImplementedException();
    }
    public BlobProperties(BlobProperties other)
    {
        throw new System.NotImplementedException();
    }
}

}