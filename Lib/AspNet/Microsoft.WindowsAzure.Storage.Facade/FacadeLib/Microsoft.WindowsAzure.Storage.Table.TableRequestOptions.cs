using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
namespace Microsoft.WindowsAzure.Storage.Table
{
public sealed class TableRequestOptions : IRequestOptions
{
    internal static TableRequestOptions BaseDefaultRequestOptions = new TableRequestOptions() { RetryPolicy = (IRetryPolicy) new NoRetry(), LocationMode = new Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode?(Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode.PrimaryOnly), ServerTimeout = new TimeSpan?(), MaximumExecutionTime = new TimeSpan?(), PayloadFormat = new TablePayloadFormat?(TablePayloadFormat.Json), PropertyResolver = (Func<string, string, string, string, EdmType>) null, ProjectSystemProperties = new bool?(true) };


    internal DateTime? OperationExpiryTime
    {
        get; set;
    }

    public IRetryPolicy RetryPolicy
    {
        get; set;
    }

    public bool? ProjectSystemProperties
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

    public TablePayloadFormat? PayloadFormat
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

    public Func<string, string, string, string, EdmType> PropertyResolver
    {
        get; set;
    }

    public TableRequestOptions()
    {
        throw new System.NotImplementedException();
    }
    public TableRequestOptions(TableRequestOptions other)
    {
        throw new System.NotImplementedException();
    }
    internal static TableRequestOptions ApplyDefaults(TableRequestOptions requestOptions, CloudTableClient serviceClient)
    {
        throw new System.NotImplementedException();
    }
    internal static TableRequestOptions ApplyDefaultsAndClearEncryption(TableRequestOptions requestOptions, CloudTableClient serviceClient)
    {
        throw new System.NotImplementedException();
    }
    internal void ApplyToStorageCommand<T>(RESTCommand<T> cmd)
    {
        throw new System.NotImplementedException();
    }
    private void ApplyToStorageCommandCommon<T>(StorageCommandBase<T> cmd)
    {
        throw new System.NotImplementedException();
    }
}

}