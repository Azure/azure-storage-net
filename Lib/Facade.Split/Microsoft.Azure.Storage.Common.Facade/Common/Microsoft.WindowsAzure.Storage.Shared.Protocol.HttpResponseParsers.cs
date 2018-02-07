using Microsoft.Azure.Storage.Core.Executor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
 
 
using System.Xml;
using System.Xml.Linq;
namespace Microsoft.Azure.Storage.Shared.Protocol
{
internal static class HttpResponseParsers
{

    internal static DateTime ToUTCTime(this string str)
    {
        throw new System.NotImplementedException();
    }
    internal static T ProcessExpectedStatusCodeNoException<T>(HttpStatusCode expectedStatusCode, HttpStatusCode actualStatusCode, T retVal, StorageCommandBase<T> cmd, Exception ex)
    {
        throw new System.NotImplementedException();
    }
    internal static T ProcessExpectedStatusCodeNoException<T>(HttpStatusCode[] expectedStatusCodes, HttpStatusCode actualStatusCode, T retVal, StorageCommandBase<T> cmd, Exception ex)
    {
        throw new System.NotImplementedException();
    }
    internal static void ValidateResponseStreamMd5AndLength<T>(long? length, string md5, StorageCommandBase<T> cmd)
    {
        throw new System.NotImplementedException();
    }
    internal static ServiceProperties ReadServiceProperties(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
    internal static ServiceStats ReadServiceStats(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
    internal static void ReadSharedAccessIdentifiers<T>(IDictionary<string, T> sharedAccessPolicies, AccessPolicyResponseBase<T> policyResponse) where T : new()
    {
        throw new System.NotImplementedException();
    }
}

}