using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
 
namespace Microsoft.Azure.Storage.Core.Executor
{
internal abstract class StorageCommandBase<T>
{
    public int? ServerTimeoutInSeconds = new int?();
    public Action<StorageCommandBase<T>, Exception, OperationContext> RecoveryAction = (Action<StorageCommandBase<T>, Exception, OperationContext>) null;
    public Func<Stream, IDictionary<string, string>, string, StorageExtendedErrorInformation> ParseDataServiceError = (Func<Stream, IDictionary<string, string>, string, StorageExtendedErrorInformation>) null;

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