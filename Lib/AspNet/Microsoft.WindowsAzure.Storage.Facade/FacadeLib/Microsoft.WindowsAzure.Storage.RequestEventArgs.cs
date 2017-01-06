using System;
namespace Microsoft.WindowsAzure.Storage
{
public sealed class RequestEventArgs
{
    public RequestResult RequestInformation
    {
        get; internal set;
    }

    public RequestEventArgs(RequestResult res)
    {
        throw new System.NotImplementedException();
    }
}

}