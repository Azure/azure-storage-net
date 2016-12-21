using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
 
using System.Xml;
using System.Xml.Linq;
namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
internal static class FileHttpResponseParsers
{

    public static FileServiceProperties ReadServiceProperties(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
    public static ServiceStats ReadServiceStats(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
}

}