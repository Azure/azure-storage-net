using System.Collections.Generic;
using System.Xml;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
internal static class Response
{
    internal static void ReadSharedAccessIdentifiers<T>(IDictionary<string, T> sharedAccessPolicies, AccessPolicyResponseBase<T> policyResponse) where T : new()
    {
        throw new System.NotImplementedException();
    }
    internal static IDictionary<string, string> ParseMetadata(XmlReader reader)
    {
        throw new System.NotImplementedException();
    }
}

}