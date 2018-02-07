using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
 
namespace Microsoft.Azure.Storage.Blob.Protocol
{
internal static class ContainerHttpResponseParsers
{

    public static void ReadSharedAccessIdentifiers(Stream inputStream, BlobContainerPermissions permissions)
    {
        throw new System.NotImplementedException();
    }
    internal static BlobContainerPublicAccessType GetContainerAcl(string acl)
    {
        throw new System.NotImplementedException();
    }
}

}