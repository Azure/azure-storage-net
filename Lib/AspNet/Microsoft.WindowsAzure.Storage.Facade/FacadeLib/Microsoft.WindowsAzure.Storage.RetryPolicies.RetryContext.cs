using System;
using System.Globalization;
namespace Microsoft.WindowsAzure.Storage.RetryPolicies
{
public sealed class RetryContext
{
    public StorageLocation NextLocation
    {
        get; private set;
    }

    public LocationMode LocationMode
    {
        get; private set;
    }

    public int CurrentRetryCount
    {
        get; private set;
    }

    public RequestResult LastRequestResult
    {
        get; private set;
    }

    internal RetryContext(int currentRetryCount, RequestResult lastRequestResult, StorageLocation nextLocation, LocationMode locationMode)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
}

}