using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Globalization;
namespace Microsoft.WindowsAzure.Storage.Core.Auth
{
internal static class QueueSharedAccessSignatureHelper
{
    internal static UriQueryBuilder GetSignature(SharedAccessQueuePolicy policy, string accessPolicyIdentifier, string signature, string accountKeyName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetHash(SharedAccessQueuePolicy policy, string accessPolicyIdentifier, string resourceName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange, byte[] keyValue)
    {
        throw new System.NotImplementedException();
    }
}

}