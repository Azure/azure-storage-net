using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
namespace Microsoft.WindowsAzure.Storage.RetryPolicies
{
public sealed class LinearRetry : IExtendedRetryPolicy, IRetryPolicy
{
    private static readonly TimeSpan DefaultClientBackoff = TimeSpan.FromSeconds(30.0);


    public LinearRetry()
      : this(LinearRetry.DefaultClientBackoff, 3)
    {
        throw new System.NotImplementedException();
    }
    public LinearRetry(TimeSpan deltaBackoff, int maxAttempts)
    {
        throw new System.NotImplementedException();
    }
    public bool ShouldRetry(int currentRetryCount, int statusCode, Exception lastException, out TimeSpan retryInterval, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public RetryInfo Evaluate(RetryContext retryContext, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public IRetryPolicy CreateInstance()
    {
        throw new System.NotImplementedException();
    }
}

}