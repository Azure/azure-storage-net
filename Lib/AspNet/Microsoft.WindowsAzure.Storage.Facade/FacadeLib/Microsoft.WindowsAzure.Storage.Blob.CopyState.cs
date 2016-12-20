using System;
namespace Microsoft.WindowsAzure.Storage.Blob
{
public sealed class CopyState
{
    public string CopyId
    {
        get; internal set;
    }

    public DateTimeOffset? CompletionTime
    {
        get; internal set;
    }

    public CopyStatus Status
    {
        get; internal set;
    }

    public Uri Source
    {
        get; internal set;
    }

    public long? BytesCopied
    {
        get; internal set;
    }

    public long? TotalBytes
    {
        get; internal set;
    }

    public string StatusDescription
    {
        get; internal set;
    }

    public DateTimeOffset? DestinationSnapshotTime
    {
        get; internal set;
    }
}

}