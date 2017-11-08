using System;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage
{
public sealed class OperationContext
{
    private IList<RequestResult> requestResults = (IList<RequestResult>) new List<RequestResult>();

    public IDictionary<string, string> UserHeaders
    {
        get; set;
    }

    public string ClientRequestID
    {
        get; set;
    }

    public string CustomUserAgent
    {
        get; set;
    }

    public static LogLevel DefaultLogLevel
    {
        get; set;
    }

    public LogLevel LogLevel
    {
        get; set;
    }

    public DateTimeOffset StartTime
    {
        get; set;
    }

    public DateTimeOffset EndTime
    {
        get; set;
    }

    public IList<RequestResult> RequestResults
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public RequestResult LastResult
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }


    static OperationContext()
    {
        throw new System.NotImplementedException();
    }
    public OperationContext()
    {
        throw new System.NotImplementedException();
    }
    internal void FireSendingRequest(RequestEventArgs args)
    {
        throw new System.NotImplementedException();
    }
    internal void FireResponseReceived(RequestEventArgs args)
    {
        throw new System.NotImplementedException();
    }
    internal void FireRequestCompleted(RequestEventArgs args)
    {
        throw new System.NotImplementedException();
    }
    internal void FireRetrying(RequestEventArgs args)
    {
        throw new System.NotImplementedException();
    }
}

}