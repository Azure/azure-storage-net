
namespace Microsoft.Azure.Storage.Queue.Protocol
{
internal static class QueueErrorCodeStrings
{
    public static readonly string QueueNotFound = "QueueNotFound";
    public static readonly string QueueDisabled = "QueueDisabled";
    public static readonly string QueueAlreadyExists = "QueueAlreadyExists";
    public static readonly string QueueNotEmpty = "QueueNotEmpty";
    public static readonly string QueueBeingDeleted = "QueueBeingDeleted";
    public static readonly string PopReceiptMismatch = "PopReceiptMismatch";
    public static readonly string InvalidParameter = "InvalidParameter";
    public static readonly string MessageNotFound = "MessageNotFound";
    public static readonly string MessageTooLarge = "MessageTooLarge";
    public static readonly string InvalidMarker = "InvalidMarker";
}

}