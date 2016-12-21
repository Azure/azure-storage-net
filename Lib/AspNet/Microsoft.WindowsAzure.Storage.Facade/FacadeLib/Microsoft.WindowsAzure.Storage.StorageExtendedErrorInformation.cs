using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
namespace Microsoft.WindowsAzure.Storage
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

    public static StorageExtendedErrorInformation ReadFromStream(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
}

}