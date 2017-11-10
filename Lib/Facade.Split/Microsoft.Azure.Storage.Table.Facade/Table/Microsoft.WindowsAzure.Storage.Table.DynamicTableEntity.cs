using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Table
{
public sealed class DynamicTableEntity : ITableEntity
{
    public IDictionary<string, EntityProperty> Properties
    {
        get; set;
    }

    public string PartitionKey
    {
        get; set;
    }

    public string RowKey
    {
        get; set;
    }

    public DateTimeOffset Timestamp
    {
        get; set;
    }

    public string ETag
    {
        get; set;
    }

    public DynamicTableEntity()
    {
        throw new System.NotImplementedException();
    }
    public DynamicTableEntity(string partitionKey, string rowKey)
      : this(partitionKey, rowKey, DateTimeOffset.MinValue, (string) null, (IDictionary<string, EntityProperty>) new Dictionary<string, EntityProperty>())
    {
        throw new System.NotImplementedException();
    }
    public DynamicTableEntity(string partitionKey, string rowKey, string etag, IDictionary<string, EntityProperty> properties)
      : this(partitionKey, rowKey, DateTimeOffset.MinValue, etag, properties)
    {
        throw new System.NotImplementedException();
    }
    internal DynamicTableEntity(string partitionKey, string rowKey, DateTimeOffset timestamp, string etag, IDictionary<string, EntityProperty> properties)
    {
        throw new System.NotImplementedException();
    }
    public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
}

}