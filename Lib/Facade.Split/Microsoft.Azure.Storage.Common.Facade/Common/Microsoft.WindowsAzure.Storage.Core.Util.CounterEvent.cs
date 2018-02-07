using System;
using System.Threading;
namespace Microsoft.Azure.Storage.Core.Util
{
internal sealed class CounterEvent : IDisposable
{
    public WaitHandle WaitHandle
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public void Increment()
    {
        throw new System.NotImplementedException();
    }
    public void Decrement()
    {
        throw new System.NotImplementedException();
    }
    public void Wait()
    {
        throw new System.NotImplementedException();
    }
    public bool Wait(int millisecondsTimeout)
    {
        throw new System.NotImplementedException();
    }
    public void Dispose()
    {
        throw new System.NotImplementedException();
    }
}

}