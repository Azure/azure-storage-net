using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.IO;
using System.Xml.Linq;
namespace Microsoft.WindowsAzure.Storage.Table.Protocol
{
internal class TableAccessPolicyResponse : AccessPolicyResponseBase<SharedAccessTablePolicy>
{
    internal TableAccessPolicyResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    protected override SharedAccessTablePolicy ParseElement(XElement accessPolicyElement)
    {
        throw new System.NotImplementedException();
    }
}

}