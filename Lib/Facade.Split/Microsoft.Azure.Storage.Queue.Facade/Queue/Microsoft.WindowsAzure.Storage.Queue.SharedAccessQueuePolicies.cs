using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections;
using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Queue
{
public sealed class SharedAccessQueuePolicies : IDictionary<string, SharedAccessQueuePolicy>, ICollection<KeyValuePair<string, SharedAccessQueuePolicy>>, IEnumerable<KeyValuePair<string, SharedAccessQueuePolicy>>, IEnumerable
{
    private Dictionary<string, SharedAccessQueuePolicy> policies = new Dictionary<string, SharedAccessQueuePolicy>();

    public ICollection<string> Keys
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public ICollection<SharedAccessQueuePolicy> Values
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public SharedAccessQueuePolicy this[string key]
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

    public void Add(string key, SharedAccessQueuePolicy value)
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
    public bool TryGetValue(string key, out SharedAccessQueuePolicy value)
    {
        throw new System.NotImplementedException();
    }
    public void Add(KeyValuePair<string, SharedAccessQueuePolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public void Clear()
    {
        throw new System.NotImplementedException();
    }
    public bool Contains(KeyValuePair<string, SharedAccessQueuePolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public void CopyTo(KeyValuePair<string, SharedAccessQueuePolicy>[] array, int arrayIndex)
    {
        throw new System.NotImplementedException();
    }
    public bool Remove(KeyValuePair<string, SharedAccessQueuePolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public IEnumerator<KeyValuePair<string, SharedAccessQueuePolicy>> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}

}