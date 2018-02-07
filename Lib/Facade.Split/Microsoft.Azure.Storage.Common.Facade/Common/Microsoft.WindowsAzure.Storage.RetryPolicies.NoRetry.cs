using System;
namespace Microsoft.Azure.Storage.RetryPolicies
{
public sealed class NoRetry : IRetryPolicy
{
    public bool ShouldRetry(int currentRetryCount, int statusCode, Exception lastException, out TimeSpan retryInterval, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public IRetryPolicy CreateInstance()
    {
        throw new System.NotImplementedException();
    }
}

}