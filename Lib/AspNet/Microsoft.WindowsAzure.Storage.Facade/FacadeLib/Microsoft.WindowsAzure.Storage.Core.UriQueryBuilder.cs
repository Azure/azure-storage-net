using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace Microsoft.WindowsAzure.Storage.Core
{
internal class UriQueryBuilder
{
    protected IDictionary<string, string> Parameters
    {
        get; private set;
    }

    public string this[string name]
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public UriQueryBuilder()
      : this((UriQueryBuilder) null)
    {
        throw new System.NotImplementedException();
    }
    public UriQueryBuilder(UriQueryBuilder builder)
    {
        throw new System.NotImplementedException();
    }
    public virtual void Add(string name, string value)
    {
        throw new System.NotImplementedException();
    }
    public void AddRange(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        throw new System.NotImplementedException();
    }
    public bool ContainsQueryStringName(string name)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
    public StorageUri AddToUri(StorageUri storageUri)
    {
        throw new System.NotImplementedException();
    }
    public virtual Uri AddToUri(Uri uri)
    {
        throw new System.NotImplementedException();
    }
    protected Uri AddToUriCore(Uri uri)
    {
        throw new System.NotImplementedException();
    }
}

}