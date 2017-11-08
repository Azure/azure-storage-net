using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.IO;
using System.Xml.Linq;
namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
internal class FileAccessPolicyResponse : AccessPolicyResponseBase<SharedAccessFilePolicy>
{
    internal FileAccessPolicyResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    protected override SharedAccessFilePolicy ParseElement(XElement accessPolicyElement)
    {
        throw new System.NotImplementedException();
    }
}

}