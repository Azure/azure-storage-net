using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
namespace Microsoft.Azure.Storage.Shared.Protocol
{
internal static class Request
{
    internal static string ConvertDateTimeToSnapshotString(DateTimeOffset dateTime)
    {
        throw new System.NotImplementedException();
    }
    internal static void WriteSharedAccessIdentifiers<T>(IDictionary<string, T> sharedAccessPolicies, Stream outputStream, Action<T, XmlWriter> writePolicyXml)
    {
        throw new System.NotImplementedException();
    }
}

}