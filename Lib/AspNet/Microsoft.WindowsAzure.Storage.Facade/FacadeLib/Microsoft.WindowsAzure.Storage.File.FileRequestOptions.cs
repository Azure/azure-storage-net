using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
namespace Microsoft.WindowsAzure.Storage.File
{
public sealed class FileRequestOptions : IRequestOptions
{
    internal static FileRequestOptions BaseDefaultRequestOptions = new FileRequestOptions() { RetryPolicy = (IRetryPolicy) new NoRetry(), LocationMode = new Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode?(Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode.PrimaryOnly), ServerTimeout = new TimeSpan?(), MaximumExecutionTime = new TimeSpan?(), ParallelOperationThreadCount = new int?(1), DisableContentMD5Validation = new bool?(false), StoreFileContentMD5 = new bool?(false), UseTransactionalMD5 = new bool?(false) };


    internal DateTime? OperationExpiryTime
    {
        get; set;
    }

    public IRetryPolicy RetryPolicy
    {
        get; set;
    }

    public Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode? LocationMode
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

    public bool? StoreFileContentMD5
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

    public FileRequestOptions()
    {
        throw new System.NotImplementedException();
    }
    internal FileRequestOptions(FileRequestOptions other)
      : this()
    {
        throw new System.NotImplementedException();
    }
    internal static FileRequestOptions ApplyDefaults(FileRequestOptions options, CloudFileClient serviceClient, bool applyExpiry = true)
    {
        throw new System.NotImplementedException();
    }
    internal void ApplyToStorageCommand<T>(RESTCommand<T> cmd)
    {
        throw new System.NotImplementedException();
    }
}

}