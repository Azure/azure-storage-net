using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
namespace Microsoft.Azure.Storage.Shared.Protocol
{

internal abstract class ResponseParsingBase<T> : IDisposable
{
    protected IList<T> outstandingObjectsToParse = (IList<T>) new List<T>();
    protected bool allObjectsParsed;
    protected XmlReader reader;
    private IEnumerator<T> parser;
    private bool enumerableConsumed;

    protected IEnumerable<T> ObjectsToParse
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    protected ResponseParsingBase(Stream stream)
    {
        throw new System.NotImplementedException();
    }
    public void Dispose()
    {
        throw new System.NotImplementedException();
    }
    protected abstract IEnumerable<T> ParseXml();

    protected virtual void Dispose(bool disposing)
    {
        throw new System.NotImplementedException();
    }
    protected void Variable(ref bool consumable)
    {
        throw new System.NotImplementedException();
    }
    private IEnumerable<T> ParseXmlAndClose()
    {
        throw new System.NotImplementedException();
    }
}

}