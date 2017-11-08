using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
namespace Microsoft.WindowsAzure.Storage.Core.Auth
{
internal static class SharedAccessSignatureHelper
{
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
    internal static string GetHash(SharedAccessAccountPolicy policy, string accountName, string sasVersion, byte[] keyValue)
    {
        throw new System.NotImplementedException();
    }
}

}