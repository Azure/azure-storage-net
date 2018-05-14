using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
namespace Microsoft.Azure.Storage
{
public sealed class StorageExtendedErrorInformation
{
    public string ErrorCode
    {
        get; internal set;
    }

    public string ErrorMessage
    {
        get; internal set;
    }

    public IDictionary<string, string> AdditionalDetails
    {
        get; internal set;
    }


    public static Task<StorageExtendedErrorInformation> ReadFromStreamAsync(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
    public static Task<StorageExtendedErrorInformation> ReadFromStreamAsync(Stream inputStream, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    internal void WriteXml(XmlWriter writer)
    {
        throw new System.NotImplementedException();
    }
}

}