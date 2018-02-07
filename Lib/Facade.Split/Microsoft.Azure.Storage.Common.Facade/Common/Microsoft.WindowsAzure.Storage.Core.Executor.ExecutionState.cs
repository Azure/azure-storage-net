using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.RetryPolicies;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Globalization;
using System.IO;
 
using System.Threading;
namespace Microsoft.Azure.Storage.Core.Executor
{
internal class ExecutionState<T> : IDisposable
{
    internal OperationContext OperationContext
    {
        get; private set;
    }

    internal DateTime? OperationExpiryTime
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal IRetryPolicy RetryPolicy
    {
        get; private set;
    }

    internal StorageCommandBase<T> Cmd
    {
        get; private set;
    }

    internal StorageLocation CurrentLocation
    {
        get; set;
    }

    internal RESTCommand<T> RestCMD
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal ExecutorOperation CurrentOperation
    {
        get; set;
    }

    internal TimeSpan RemainingTimeout
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal int RetryCount
    {
        get; set;
    }

    internal Stream ReqStream
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

    internal Exception ExceptionRef
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

    internal T Result
    {
        get; set;
    }

    internal bool ReqTimedOut
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

    public ExecutionState(StorageCommandBase<T> cmd, IRetryPolicy policy, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    internal void Init()
    {
        throw new System.NotImplementedException();
    }
    private void CheckDisposeSendStream()
    {
        throw new System.NotImplementedException();
    }
    private void CheckDisposeAction()
    {
        throw new System.NotImplementedException();
    }
    private void InitializeLocation()
    {
        throw new System.NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
    }

}