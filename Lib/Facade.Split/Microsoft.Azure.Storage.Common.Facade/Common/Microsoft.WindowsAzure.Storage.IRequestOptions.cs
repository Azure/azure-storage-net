using Microsoft.Azure.Storage.RetryPolicies;
using System;
namespace Microsoft.Azure.Storage
{
public interface IRequestOptions
{
    IRetryPolicy RetryPolicy
    {
        get; set;
    }

    Microsoft.Azure.Storage.RetryPolicies.LocationMode? LocationMode
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