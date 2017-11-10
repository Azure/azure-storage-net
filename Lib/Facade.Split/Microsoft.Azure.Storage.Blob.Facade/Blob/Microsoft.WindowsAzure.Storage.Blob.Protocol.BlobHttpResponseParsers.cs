using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
 
namespace Microsoft.Azure.Storage.Blob.Protocol
{
internal static class BlobHttpResponseParsers
{

    public static ServiceProperties ReadServiceProperties(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
    public static ServiceStats ReadServiceStats(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
    internal static LeaseStatus GetLeaseStatus(string leaseStatus)
    {
        throw new System.NotImplementedException();
    }
    internal static LeaseState GetLeaseState(string leaseState)
    {
        throw new System.NotImplementedException();
    }
    internal static LeaseDuration GetLeaseDuration(string leaseDuration)
    {
        throw new System.NotImplementedException();
    }
    internal static CopyState GetCopyAttributes(string copyStatusString, string copyId, string copySourceString, string copyProgressString, string copyCompletionTimeString, string copyStatusDescription, string copyDestinationSnapshotTimeString)
    {
        throw new System.NotImplementedException();
    }
    public static bool GetServerEncrypted(string encryptionHeader)
    {
        throw new System.NotImplementedException();
    }
    public static bool GetIncrementalCopyStatus(string incrementalCopyHeader)
    {
        throw new System.NotImplementedException();
    }
    private static bool CheckIfTrue(string header)
    {
        throw new System.NotImplementedException();
    }
    internal static void GetBlobTier(BlobType blobType, string blobTierString, out StandardBlobTier? standardBlobTier, out PremiumPageBlobTier? premiumPageBlobTier)
    {
        throw new System.NotImplementedException();
    }
    internal static RehydrationStatus? GetRehydrationStatus(string rehydrationStatus)
    {
        throw new System.NotImplementedException();
    }
}

}