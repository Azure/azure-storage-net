using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
namespace Microsoft.WindowsAzure.Storage
{
public interface IRequestOptions
{
    IRetryPolicy RetryPolicy
    {
        get; set;
    }

    Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode? LocationMode
    {
        get; set;
    }

    TimeSpan? ServerTimeout
    {
        get; set;
    }

    TimeSpan? MaximumExecutionTime
    {
        get; set;
    }
}

}