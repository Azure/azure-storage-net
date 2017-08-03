using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
internal abstract class AccessPolicyResponseBase<T> : ResponseParsingBase<KeyValuePair<string, T>> where T : new()
{
    public IEnumerable<KeyValuePair<string, T>> AccessIdentifiers
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    protected AccessPolicyResponseBase(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    protected abstract T ParseElement(XElement accessPolicyElement);

    protected override IEnumerable<KeyValuePair<string, T>> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}