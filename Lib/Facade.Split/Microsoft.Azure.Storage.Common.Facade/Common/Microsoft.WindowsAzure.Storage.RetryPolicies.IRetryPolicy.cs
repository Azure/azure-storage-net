using System;
namespace Microsoft.Azure.Storage.RetryPolicies
{
public interface IRetryPolicy
{
    IRetryPolicy CreateInstance();

    bool ShouldRetry(int currentRetryCount, int statusCode, Exception lastException, out TimeSpan retryInterval, OperationContext operationContext);
}

}