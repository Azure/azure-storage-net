using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Globalization;
namespace Microsoft.WindowsAzure.Storage.Core.Auth
{
internal static class SharedAccessSignatureHelper
{
    internal static UriQueryBuilder GetSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string accessPolicyIdentifier, string resourceType, string signature, string accountKeyName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
    internal static UriQueryBuilder GetSignature(SharedAccessFilePolicy policy, SharedAccessFileHeaders headers, string accessPolicyIdentifier, string resourceType, string signature, string accountKeyName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
    internal static UriQueryBuilder GetSignature(SharedAccessQueuePolicy policy, string accessPolicyIdentifier, string signature, string accountKeyName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
    internal static UriQueryBuilder GetSignature(SharedAccessTablePolicy policy, string tableName, string accessPolicyIdentifier, string startPartitionKey, string startRowKey, string endPartitionKey, string endRowKey, string signature, string accountKeyName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
    internal static UriQueryBuilder GetSignature(SharedAccessAccountPolicy policy, string signature, string accountKeyName, string sasVersion)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetDateTimeOrEmpty(DateTimeOffset? value)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetDateTimeOrNull(DateTimeOffset? value)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetProtocolString(SharedAccessProtocol? protocols)
    {
        throw new System.NotImplementedException();
    }
    internal static void AddEscapedIfNotNull(UriQueryBuilder builder, string name, string value)
    {
        throw new System.NotImplementedException();
    }
    internal static StorageCredentials ParseQuery(IDictionary<string, string> queryParameters)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetHash(SharedAccessQueuePolicy policy, string accessPolicyIdentifier, string resourceName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange, byte[] keyValue)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetHash(SharedAccessTablePolicy policy, string accessPolicyIdentifier, string startPartitionKey, string startRowKey, string endPartitionKey, string endRowKey, string resourceName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange, byte[] keyValue)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetHash(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string accessPolicyIdentifier, string resourceName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange, byte[] keyValue)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetHash(SharedAccessFilePolicy policy, SharedAccessFileHeaders headers, string accessPolicyIdentifier, string resourceName, string sasVersion, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange, byte[] keyValue)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetHash(SharedAccessAccountPolicy policy, string accountName, string sasVersion, byte[] keyValue)
    {
        throw new System.NotImplementedException();
    }
}

}