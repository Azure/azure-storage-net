using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
namespace Microsoft.WindowsAzure.Storage.Table
{
public class TableEntity : ITableEntity
{
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

    public TableEntity()
    {
        throw new System.NotImplementedException();
    }
    public TableEntity(string partitionKey, string rowKey)
    {
        throw new System.NotImplementedException();
    }
    public virtual void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public static void ReadUserObject(object entity, IDictionary<string, EntityProperty> properties, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    private static void ReflectionRead(object entity, IDictionary<string, EntityProperty> properties, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public virtual IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public static IDictionary<string, EntityProperty> WriteUserObject(object entity, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    private static IDictionary<string, EntityProperty> ReflectionWrite(object entity, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    internal static bool ShouldSkipProperty(PropertyInfo property, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
}

}