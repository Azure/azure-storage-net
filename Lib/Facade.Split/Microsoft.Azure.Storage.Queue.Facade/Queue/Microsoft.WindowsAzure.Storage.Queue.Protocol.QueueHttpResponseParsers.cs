using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using System.Collections.Generic;
using System.IO;
 
namespace Microsoft.Azure.Storage.Queue.Protocol
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