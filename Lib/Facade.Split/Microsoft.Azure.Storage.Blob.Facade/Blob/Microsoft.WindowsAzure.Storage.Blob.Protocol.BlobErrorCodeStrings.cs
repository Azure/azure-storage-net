
namespace Microsoft.Azure.Storage.Blob.Protocol
{
internal static class BlobErrorCodeStrings
{
    public static readonly string InvalidAppendCondition = "AppendPositionConditionNotMet";
    public static readonly string InvalidMaxBlobSizeCondition = "MaxBlobSizeConditionNotMet";
    public static readonly string InvalidBlobOrBlock = "InvalidBlobOrBlock";
    public static readonly string InvalidBlockId = "InvalidBlockId";
    public static readonly string InvalidBlockList = "InvalidBlockList";
    public static readonly string ContainerNotFound = "ContainerNotFound";
    public static readonly string BlobNotFound = "BlobNotFound";
    public static readonly string ContainerAlreadyExists = "ContainerAlreadyExists";
    public static readonly string ContainerDisabled = "ContainerDisabled";
    public static readonly string ContainerBeingDeleted = "ContainerBeingDeleted";
    public static readonly string BlobAlreadyExists = "BlobAlreadyExists";
    public static readonly string LeaseNotPresentWithBlobOperation = "LeaseNotPresentWithBlobOperation";
    public static readonly string LeaseNotPresentWithContainerOperation = "LeaseNotPresentWithContainerOperation";
    public static readonly string LeaseLost = "LeaseLost";
    public static readonly string LeaseIdMismatchWithBlobOperation = "LeaseIdMismatchWithBlobOperation";
    public static readonly string LeaseIdMismatchWithContainerOperation = "LeaseIdMismatchWithContainerOperation";
    public static readonly string LeaseIdMissing = "LeaseIdMissing";
    public static readonly string LeaseNotPresentWithLeaseOperation = "LeaseNotPresentWithLeaseOperation";
    public static readonly string LeaseIdMismatchWithLeaseOperation = "LeaseIdMismatchWithLeaseOperation";
    public static readonly string LeaseAlreadyPresent = "LeaseAlreadyPresent";
    public static readonly string LeaseAlreadyBroken = "LeaseAlreadyBroken";
    public static readonly string LeaseIsBrokenAndCannotBeRenewed = "LeaseIsBrokenAndCannotBeRenewed";
    public static readonly string LeaseIsBreakingAndCannotBeAcquired = "LeaseIsBreakingAndCannotBeAcquired";
    public static readonly string LeaseIsBreakingAndCannotBeChanged = "LeaseIsBreakingAndCannotBeChanged";
    public static readonly string InfiniteLeaseDurationRequired = "InfiniteLeaseDurationRequired";
    public static readonly string SnapshotsPresent = "SnapshotsPresent";
    public static readonly string InvalidBlobType = "InvalidBlobType";
    public static readonly string InvalidVersionForPageBlobOperation = "InvalidVersionForPageBlobOperation";
    public static readonly string InvalidPageRange = "InvalidPageRange";
    public static readonly string SequenceNumberConditionNotMet = "SequenceNumberConditionNotMet";
    public static readonly string SequenceNumberIncrementTooLarge = "SequenceNumberIncrementTooLarge";
    public static readonly string SourceConditionNotMet = "SourceConditionNotMet";
    public static readonly string TargetConditionNotMet = "TargetConditionNotMet";
    public static readonly string CopyAcrossAccountsNotSupported = "CopyAcrossAccountsNotSupported";
    public static readonly string CannotVerifyCopySource = "CannotVerifyCopySource";
    public static readonly string PendingCopyOperation = "PendingCopyOperation";
    public static readonly string NoPendingCopyOperation = "NoPendingCopyOperation";
    public static readonly string CopyIdMismatch = "CopyIdMismatch";
}

}