using Microsoft.Azure.Storage.Core.Auth;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
namespace Microsoft.Azure.Storage.Queue.Protocol
{
internal static class QueueRequest
{
    public static void WriteSharedAccessIdentifiers(SharedAccessQueuePolicies sharedAccessPolicies, Stream outputStream)
    {
        throw new System.NotImplementedException();
    }
    public static void WriteMessageContent(string messageContent, Stream outputStream)
    {
        throw new System.NotImplementedException();
    }
}

}