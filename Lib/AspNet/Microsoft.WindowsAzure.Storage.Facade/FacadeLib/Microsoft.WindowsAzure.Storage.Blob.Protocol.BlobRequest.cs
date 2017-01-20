using Microsoft.WindowsAzure.Storage.Core.Auth;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
internal static class BlobRequest
{
    public static void WriteSharedAccessIdentifiers(SharedAccessBlobPolicies sharedAccessPolicies, Stream outputStream)
    {
        throw new System.NotImplementedException();
    }
    public static void WriteBlockListBody(IEnumerable<PutBlockListItem> blocks, Stream outputStream)
    {
        throw new System.NotImplementedException();
    }
}

}