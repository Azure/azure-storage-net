using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System.Collections.Generic;
using System.IO;
 
namespace Microsoft.WindowsAzure.Storage.Queue.Protocol
{
internal static class QueueHttpResponseParsers
{

    public static ServiceProperties ReadServiceProperties(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
    public static ServiceStats ReadServiceStats(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
    public static void ReadSharedAccessIdentifiers(Stream inputStream, QueuePermissions permissions)
    {
        throw new System.NotImplementedException();
    }
}

}