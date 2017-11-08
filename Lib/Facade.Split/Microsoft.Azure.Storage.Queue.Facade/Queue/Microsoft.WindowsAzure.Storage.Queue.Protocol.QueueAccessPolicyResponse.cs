using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.IO;
using System.Xml.Linq;
namespace Microsoft.WindowsAzure.Storage.Queue.Protocol
{
internal class QueueAccessPolicyResponse : AccessPolicyResponseBase<SharedAccessQueuePolicy>
{
    internal QueueAccessPolicyResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    protected override SharedAccessQueuePolicy ParseElement(XElement accessPolicyElement)
    {
        throw new System.NotImplementedException();
    }
}

}