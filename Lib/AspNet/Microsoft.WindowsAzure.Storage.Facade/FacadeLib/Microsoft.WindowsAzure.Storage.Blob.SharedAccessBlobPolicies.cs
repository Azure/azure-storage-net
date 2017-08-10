using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.Blob
{
public sealed class SharedAccessBlobPolicies : IDictionary<string, SharedAccessBlobPolicy>, ICollection<KeyValuePair<string, SharedAccessBlobPolicy>>, IEnumerable<KeyValuePair<string, SharedAccessBlobPolicy>>, IEnumerable
{
    private Dictionary<string, SharedAccessBlobPolicy> policies = new Dictionary<string, SharedAccessBlobPolicy>();

    public ICollection<string> Keys
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public ICollection<SharedAccessBlobPolicy> Values
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public SharedAccessBlobPolicy this[string key]
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

    public void Add(string key, SharedAccessBlobPolicy value)
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
    public bool TryGetValue(string key, out SharedAccessBlobPolicy value)
    {
        throw new System.NotImplementedException();
    }
    public void Add(KeyValuePair<string, SharedAccessBlobPolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public void Clear()
    {
        throw new System.NotImplementedException();
    }
    public bool Contains(KeyValuePair<string, SharedAccessBlobPolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public void CopyTo(KeyValuePair<string, SharedAccessBlobPolicy>[] array, int arrayIndex)
    {
        throw new System.NotImplementedException();
    }
    public bool Remove(KeyValuePair<string, SharedAccessBlobPolicy> item)
    {
        throw new System.NotImplementedException();
    }
    public IEnumerator<KeyValuePair<string, SharedAccessBlobPolicy>> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}

}