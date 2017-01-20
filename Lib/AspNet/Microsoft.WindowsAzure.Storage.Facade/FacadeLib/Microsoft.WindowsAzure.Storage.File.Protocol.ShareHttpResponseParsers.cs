using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
 
using System.Xml;
using System.Xml.Linq;
namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
internal static class ShareHttpResponseParsers
{

    public static void ReadSharedAccessIdentifiers(Stream inputStream, FileSharePermissions permissions)
    {
        throw new System.NotImplementedException();
    }
    public static ShareStats ReadShareStats(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
}

}