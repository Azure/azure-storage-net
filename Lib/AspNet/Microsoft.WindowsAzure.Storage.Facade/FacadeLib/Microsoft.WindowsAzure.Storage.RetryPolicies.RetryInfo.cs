using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Globalization;
namespace Microsoft.WindowsAzure.Storage.RetryPolicies
{
public sealed class RetryInfo
{
    private TimeSpan interval = TimeSpan.FromSeconds(3.0);

    public StorageLocation TargetLocation
    {
        get; set;
    }

    public LocationMode UpdatedLocationMode
    {
        get; set;
    }

    public TimeSpan RetryInterval
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

    public RetryInfo()
    {
        throw new System.NotImplementedException();
    }
    public RetryInfo(RetryContext retryContext)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
}

}