using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
namespace Microsoft.WindowsAzure.Storage.Blob
{
public sealed class BlobRequestOptions : IRequestOptions
{
    internal static BlobRequestOptions BaseDefaultRequestOptions = new BlobRequestOptions() { RetryPolicy = (IRetryPolicy) new NoRetry(), AbsorbConditionalErrorsOnRetry = new bool?(false), LocationMode = new Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode?(Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode.PrimaryOnly), ServerTimeout = new TimeSpan?(), MaximumExecutionTime = new TimeSpan?(), ParallelOperationThreadCount = new int?(1), SingleBlobUploadThresholdInBytes = new long?(33554432L), DisableContentMD5Validation = new bool?(false), UseTransactionalMD5 = new bool?(false) };


    internal DateTime? OperationExpiryTime
    {
        get; set;
    }

    public IRetryPolicy RetryPolicy
    {
        get; set;
    }

    public bool? AbsorbConditionalErrorsOnRetry
    {
        get; set;
    }

    public Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode? LocationMode
    {
        get; set;
    }

    public TimeSpan? ServerTimeout
    {
        get; set;
    }

    public TimeSpan? MaximumExecutionTime
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

    public int? ParallelOperationThreadCount
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

    public long? SingleBlobUploadThresholdInBytes
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

    public bool? UseTransactionalMD5
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

    public bool? StoreBlobContentMD5
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

    public bool? DisableContentMD5Validation
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

    public BlobRequestOptions()
    {
        throw new System.NotImplementedException();
    }
    internal BlobRequestOptions(BlobRequestOptions other)
    {
        throw new System.NotImplementedException();
    }
    internal static BlobRequestOptions ApplyDefaults(BlobRequestOptions options, BlobType blobType, CloudBlobClient serviceClient, bool applyExpiry = true)
    {
        throw new System.NotImplementedException();
    }
    internal void ApplyToStorageCommand<T>(RESTCommand<T> cmd)
    {
        throw new System.NotImplementedException();
    }
}

}