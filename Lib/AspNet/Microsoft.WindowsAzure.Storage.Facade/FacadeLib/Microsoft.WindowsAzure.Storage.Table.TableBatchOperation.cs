using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
 
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Table
{
public sealed class TableBatchOperation : IList<TableOperation>, ICollection<TableOperation>, IEnumerable<TableOperation>, IEnumerable
{

    internal bool ContainsWrites
    {
        get; private set;
    }

    public TableOperation this[int index]
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

    public void Retrieve<TElement>(string partitionKey, string rowkey, List<string> selectedColumns = null) where TElement : ITableEntity
    {
        throw new System.NotImplementedException();
    }
    public void Retrieve<TResult>(string partitionKey, string rowkey, EntityResolver<TResult> resolver, List<string> selectedColumns = null)
    {
        throw new System.NotImplementedException();
    }
    public void Delete(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public void Insert(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public void Insert(ITableEntity entity, bool echoContent)
    {
        throw new System.NotImplementedException();
    }
    public void InsertOrMerge(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public void InsertOrReplace(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public void Merge(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public void Replace(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public void Retrieve(string partitionKey, string rowKey)
    {
        throw new System.NotImplementedException();
    }
    public int IndexOf(TableOperation item)
    {
        throw new System.NotImplementedException();
    }
    public void Insert(int index, TableOperation item)
    {
        throw new System.NotImplementedException();
    }
    public void RemoveAt(int index)
    {
        throw new System.NotImplementedException();
    }
    public void Add(TableOperation item)
    {
        throw new System.NotImplementedException();
    }
    public void Clear()
    {
        throw new System.NotImplementedException();
    }
    public bool Contains(TableOperation item)
    {
        throw new System.NotImplementedException();
    }
    public void CopyTo(TableOperation[] array, int arrayIndex)
    {
        throw new System.NotImplementedException();
    }
    public bool Remove(TableOperation item)
    {
        throw new System.NotImplementedException();
    }
    public IEnumerator<TableOperation> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    private void CheckSingleQueryPerBatch(TableOperation item)
    {
        throw new System.NotImplementedException();
    }
    private void LockToPartitionKey(string partitionKey)
    {
        throw new System.NotImplementedException();
    }
    private static void CheckPartitionKeyRowKeyPresent(TableOperation item)
    {
        throw new System.NotImplementedException();
    }
}

}