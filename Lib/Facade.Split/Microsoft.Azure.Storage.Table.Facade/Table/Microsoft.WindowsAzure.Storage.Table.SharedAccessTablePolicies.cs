using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections;
using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Table
{
public sealed class SharedAccessTablePolicies : IDictionary<string, SharedAccessTablePolicy>, ICollection<KeyValuePair<string, SharedAccessTablePolicy>>, IEnumerable<KeyValuePair<string, SharedAccessTablePolicy>>, IEnumerable
{
    private readonly Dictionary<string, SharedAccessTablePolicy> policies = new Dictionary<string, SharedAccessTablePolicy>();

    public ICollection<string> Keys
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public ICollection<SharedAccessTablePolicy> Values
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public SharedAccessTablePolicy this[string key]
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

    public void Add(string key, SharedAccessTablePolicy value)
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
    public bool TryGetValue(string key, out SharedAccessTablePolicy value)
    {
        throw new System.NotImplementedException();
    }
    public void Add(KeyValuePair<string, SharedAccessTablePolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public void Clear()
    {
        throw new System.NotImplementedException();
    }
    public bool Contains(KeyValuePair<string, SharedAccessTablePolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public void CopyTo(KeyValuePair<string, SharedAccessTablePolicy>[] array, int arrayIndex)
    {
        throw new System.NotImplementedException();
    }
    public bool Remove(KeyValuePair<string, SharedAccessTablePolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public IEnumerator<KeyValuePair<string, SharedAccessTablePolicy>> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}

}