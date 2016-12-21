using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
namespace Microsoft.WindowsAzure.Storage.Queue
{
public sealed class QueueRequestOptions : IRequestOptions
{
    internal static QueueRequestOptions BaseDefaultRequestOptions = new QueueRequestOptions() { RetryPolicy = (IRetryPolicy) new NoRetry(), LocationMode = new Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode?(Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode.PrimaryOnly), ServerTimeout = new TimeSpan?(), MaximumExecutionTime = new TimeSpan?() };

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

    public QueueRequestOptions()
    {
        throw new System.NotImplementedException();
    }
    internal QueueRequestOptions(QueueRequestOptions other)
      : this()
    {
        throw new System.NotImplementedException();
    }
    internal static QueueRequestOptions ApplyDefaults(QueueRequestOptions options, CloudQueueClient serviceClient)
    {
        throw new System.NotImplementedException();
    }
    internal void ApplyToStorageCommand<T>(RESTCommand<T> cmd)
    {
        throw new System.NotImplementedException();
    }
}

}