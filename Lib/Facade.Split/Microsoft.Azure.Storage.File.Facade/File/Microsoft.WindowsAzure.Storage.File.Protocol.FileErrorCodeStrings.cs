
namespace Microsoft.Azure.Storage.File.Protocol
{
internal static class FileErrorCodeStrings
{
    public static readonly string ShareNotFound = "ShareNotFound";
    public static readonly string ShareAlreadyExists = "ShareAlreadyExists";
    public static readonly string ShareDisabled = "ShareDisabled";
    public static readonly string ShareBeingDeleted = "ShareBeingDeleted";
    public static readonly string DeletePending = "DeletePending";
    public static readonly string ParentNotFound = "ParentNotFound";
    public static readonly string InvalidResourceName = "InvalidResourceName";
    public static readonly string ResourceAlreadyExists = "ResourceAlreadyExists";
    public static readonly string ResourceTypeMismatch = "ResourceTypeMismatch";
    public static readonly string SharingViolation = "SharingViolation";
    public static readonly string CannotDeleteFileOrDirectory = "CannotDeleteFileOrDirectory";
    public static readonly string FileLockConflict = "FileLockConflict";
    public static readonly string ReadOnlyAttribute = "ReadOnlyAttribute";
    public static readonly string ClientCacheFlushDelay = "ClientCacheFlushDelay";
    public static readonly string InvalidFileOrDirectoryPathName = "InvalidFileOrDirectoryPathName";
    public static readonly string ConditionHeadersNotSupported = "ConditionHeadersNotSupported";
}

}