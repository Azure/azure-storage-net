using Microsoft.Azure.Storage.Core.Util;
using System;
namespace Microsoft.Azure.Storage.RetryPolicies
{
public sealed class ExponentialRetry : IExtendedRetryPolicy, IRetryPolicy
{
    private static readonly TimeSpan DefaultClientBackoff = TimeSpan.FromSeconds(4.0);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(120.0);
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(3.0);

    public ExponentialRetry()
      : this(ExponentialRetry.DefaultClientBackoff, 3)
    {
        throw new System.NotImplementedException();
    }
    public ExponentialRetry(TimeSpan deltaBackoff, int maxAttempts)
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