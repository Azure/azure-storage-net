using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.IO;
using System.Xml.Linq;
namespace Microsoft.Azure.Storage.Blob.Protocol
{
internal class BlobAccessPolicyResponse : AccessPolicyResponseBase<SharedAccessBlobPolicy>
{
    internal BlobAccessPolicyResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    protected override SharedAccessBlobPolicy ParseElement(XElement accessPolicyElement)
    {
        throw new System.NotImplementedException();
    }
}

}