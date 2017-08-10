using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
 
namespace Microsoft.WindowsAzure.Storage.Core.Executor
{
internal abstract class StorageCommandBase<T>
{
    public int? ServerTimeoutInSeconds = new int?();
    internal DateTime? OperationExpiryTime = new DateTime?();
    private IList<RequestResult> requestResults = (IList<RequestResult>) new List<RequestResult>();
    internal object OperationState;
    private volatile StreamDescriptor streamCopyState;
    private volatile RequestResult currentResult;
    public Action<StorageCommandBase<T>, Exception, OperationContext> RecoveryAction;
    public Func<Stream, IDictionary<string, string>, string, StorageExtendedErrorInformation> ParseDataServiceError;

    internal StreamDescriptor StreamCopyState
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

    internal RequestResult CurrentResult
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

    internal IList<RequestResult> RequestResults
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }
}

}