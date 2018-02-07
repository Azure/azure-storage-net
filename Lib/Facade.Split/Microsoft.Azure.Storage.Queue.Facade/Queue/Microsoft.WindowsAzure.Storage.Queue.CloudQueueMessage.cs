using System;
using System.Globalization;
 
using System.Text;
 
namespace Microsoft.Azure.Storage.Queue
{
public sealed class CloudQueueMessage
{
    private static readonly TimeSpan MaximumTimeToLive = TimeSpan.FromDays(7.0);
    private static UTF8Encoding utf8Encoder = new UTF8Encoding(false, true);
    private const long MaximumMessageSize = 65536;
    private const int MaximumNumberOfMessagesToPeek = 32;

    public static long MaxMessageSize
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public static TimeSpan MaxTimeToLive
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public static int MaxNumberOfMessagesToPeek
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public byte[] AsBytes
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public string Id
    {
        get; internal set;
    }

    public string PopReceipt
    {
        get; internal set;
    }

    public DateTimeOffset? InsertionTime
    {
        get; internal set;
    }

    public DateTimeOffset? ExpirationTime
    {
        get; internal set;
    }

    public DateTimeOffset? NextVisibleTime
    {
        get; internal set;
    }

    public string AsString
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public int DequeueCount
    {
        get; internal set;
    }

    internal QueueMessageType MessageType
    {
        get; private set;
    }

    internal string RawString
    {
        get; set;
    }

    internal byte[] RawBytes
    {
        get; set;
    }

    internal CloudQueueMessage()
    {
        throw new System.NotImplementedException();
    }
    public CloudQueueMessage(string content)
    {
        throw new System.NotImplementedException();
    }
    public CloudQueueMessage(string messageId, string popReceipt)
    {
        throw new System.NotImplementedException();
    }
    internal CloudQueueMessage(string content, bool isBase64Encoded)
    {
        throw new System.NotImplementedException();
    }
    public static CloudQueueMessage CreateCloudQueueMessageFromByteArray(byte[] content)
    {
        throw new System.NotImplementedException();
    }
    public void SetMessageContent(byte[] content)
    {
        throw new System.NotImplementedException();
    }
    internal string GetMessageContentForTransfer(bool shouldEncodeMessage, QueueRequestOptions options = null)
    {
        throw new System.NotImplementedException();
    }
    public void SetMessageContent(string content)
    {
        throw new System.NotImplementedException();
    }
}

}