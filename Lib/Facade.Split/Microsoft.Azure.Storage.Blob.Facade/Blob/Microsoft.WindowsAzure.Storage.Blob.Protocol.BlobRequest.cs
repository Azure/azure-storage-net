using Microsoft.Azure.Storage.Core.Auth;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
namespace Microsoft.Azure.Storage.Blob.Protocol
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