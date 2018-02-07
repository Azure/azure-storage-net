using System;
namespace Microsoft.Azure.Storage.Queue.Protocol
{
internal class QueueMessage
{
    public DateTimeOffset? ExpirationTime
    {
        get; internal set;
    }

    public string Id
    {
        get; internal set;
    }

    public DateTimeOffset? InsertionTime
    {
        get; internal set;
    }

    public DateTimeOffset? NextVisibleTime
    {
        get; internal set;
    }

    public string PopReceipt
    {
        get; internal set;
    }

    public string Text
    {
        get; internal set;
    }

    public int DequeueCount
    {
        get; internal set;
    }

    internal QueueMessage()
    {
        throw new System.NotImplementedException();
    }
}

}