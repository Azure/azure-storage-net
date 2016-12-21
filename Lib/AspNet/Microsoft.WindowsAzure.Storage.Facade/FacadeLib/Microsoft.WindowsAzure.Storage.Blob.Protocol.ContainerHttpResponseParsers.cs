using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
 
namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
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