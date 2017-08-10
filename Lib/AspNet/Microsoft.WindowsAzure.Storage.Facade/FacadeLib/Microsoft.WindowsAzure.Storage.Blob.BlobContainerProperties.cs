using System;
namespace Microsoft.WindowsAzure.Storage.Blob
{
    public sealed class BlobContainerProperties
    {
        public string ETag { get; internal set; }

        public DateTimeOffset? LastModified { get; internal set; }

        public LeaseStatus LeaseStatus { get; internal set; }

        public LeaseState LeaseState { get; internal set; }

        public LeaseDuration LeaseDuration { get; internal set; }

        public BlobContainerPublicAccessType? PublicAccess { get; internal set; }
    }
}