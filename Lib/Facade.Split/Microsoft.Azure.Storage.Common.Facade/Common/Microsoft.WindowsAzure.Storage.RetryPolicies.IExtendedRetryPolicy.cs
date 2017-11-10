
namespace Microsoft.Azure.Storage.RetryPolicies
{
public interface IExtendedRetryPolicy : IRetryPolicy
{
    RetryInfo Evaluate(RetryContext retryContext, OperationContext operationContext);
}

}