using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
namespace Microsoft.WindowsAzure.Storage.Core.Executor
{
internal abstract class ExecutorBase
{
    protected static void ApplyUserHeaders<T>(ExecutionState<T> executionState)
    {
        throw new System.NotImplementedException();
    }
    protected static void StartRequestAttempt<T>(ExecutionState<T> executionState)
    {
        throw new System.NotImplementedException();
    }
    protected static StorageLocation GetNextLocation(StorageLocation lastLocation, LocationMode locationMode)
    {
        throw new System.NotImplementedException();
    }
    protected static void FinishRequestAttempt<T>(ExecutionState<T> executionState)
    {
        throw new System.NotImplementedException();
    }
    protected static void FireSendingRequest<T>(ExecutionState<T> executionState)
    {
        throw new System.NotImplementedException();
    }
    protected static void FireResponseReceived<T>(ExecutionState<T> executionState)
    {
        throw new System.NotImplementedException();
    }
    protected static void FireRequestCompleted<T>(ExecutionState<T> executionState)
    {
        throw new System.NotImplementedException();
    }
    protected static void FireRetrying<T>(ExecutionState<T> executionState)
    {
        throw new System.NotImplementedException();
    }
    private static RequestEventArgs GenerateRequestEventArgs<T>(ExecutionState<T> executionState)
    {
        throw new System.NotImplementedException();
    }
    protected static bool CheckTimeout<T>(ExecutionState<T> executionState, bool throwOnTimeout)
    {
        throw new System.NotImplementedException();
    }
}

}