using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.File
{
public sealed class SharedAccessFilePolicies : IDictionary<string, SharedAccessFilePolicy>, ICollection<KeyValuePair<string, SharedAccessFilePolicy>>, IEnumerable<KeyValuePair<string, SharedAccessFilePolicy>>, IEnumerable
{
    private Dictionary<string, SharedAccessFilePolicy> policies = new Dictionary<string, SharedAccessFilePolicy>();

    public ICollection<string> Keys
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public ICollection<SharedAccessFilePolicy> Values
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public SharedAccessFilePolicy this[string key]
    {
        get
        {
            throw new System.NotImplementedException();
        }
        set
        {
            throw new System.NotImplementedException();
        }
    }

    public int Count
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public bool IsReadOnly
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public void Add(string key, SharedAccessFilePolicy value)
    {
        throw new System.NotImplementedException();
    }
    public bool ContainsKey(string key)
    {
        throw new System.NotImplementedException();
    }
    public bool Remove(string key)
    {
        throw new System.NotImplementedException();
    }
    public bool TryGetValue(string key, out SharedAccessFilePolicy value)
    {
        throw new System.NotImplementedException();
    }
    public void Add(KeyValuePair<string, SharedAccessFilePolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public void Clear()
    {
        throw new System.NotImplementedException();
    }
    public bool Contains(KeyValuePair<string, SharedAccessFilePolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public void CopyTo(KeyValuePair<string, SharedAccessFilePolicy>[] array, int arrayIndex)
    {
        throw new System.NotImplementedException();
    }
    public bool Remove(KeyValuePair<string, SharedAccessFilePolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public IEnumerator<KeyValuePair<string, SharedAccessFilePolicy>> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}

}