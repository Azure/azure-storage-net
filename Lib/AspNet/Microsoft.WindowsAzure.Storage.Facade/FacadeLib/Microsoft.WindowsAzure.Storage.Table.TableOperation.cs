using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
 
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Table
{
public class TableOperation
{

    internal bool IsTableEntity
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

    internal bool IsPrimaryOnlyRetrieve
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

    internal string RetrievePartitionKey
    {
        get; set;
    }

    internal string RetrieveRowKey
    {
        get; set;
    }

    internal string PartitionKey
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal string RowKey
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal string ETag
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, object> RetrieveResolver
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

    internal Type PropertyResolverType
    {
        get; set;
    }

    internal ITableEntity Entity
    {
        get; private set;
    }

    internal TableOperationType OperationType
    {
        get; private set;
    }

    internal bool EchoContent
    {
        get; set;
    }

    internal List<string> SelectColumns
    {
        get; set;
    }

    internal TableOperation(ITableEntity entity, TableOperationType operationType)
      : this(entity, operationType, true)
    {
        throw new System.NotImplementedException();
    }
    internal TableOperation(ITableEntity entity, TableOperationType operationType, bool echoContent)
    {
        throw new System.NotImplementedException();
    }
    public static TableOperation Delete(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public static TableOperation Insert(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public static TableOperation Insert(ITableEntity entity, bool echoContent)
    {
        throw new System.NotImplementedException();
    }
    public static TableOperation InsertOrMerge(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public static TableOperation InsertOrReplace(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public static TableOperation Merge(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public static TableOperation Replace(ITableEntity entity)
    {
        throw new System.NotImplementedException();
    }
    public static TableOperation Retrieve<TElement>(string partitionKey, string rowkey, List<string> selectColumns = null) where TElement : ITableEntity
    {
        throw new System.NotImplementedException();
    }
    public static TableOperation Retrieve<TResult>(string partitionKey, string rowkey, EntityResolver<TResult> resolver, List<string> selectedColumns = null)
    {
        throw new System.NotImplementedException();
    }
    public static TableOperation Retrieve(string partitionKey, string rowkey, List<string> selectedColumns = null)
    {
        throw new System.NotImplementedException();
    }
    private static object DynamicEntityResolver(string partitionKey, string rowKey, DateTimeOffset timestamp, IDictionary<string, EntityProperty> properties, string etag)
    {
        throw new System.NotImplementedException();
    }
    internal StorageUri GenerateRequestURI(StorageUri uriList, string tableName)
    {
        throw new System.NotImplementedException();
    }
    internal Uri GenerateRequestURI(Uri uri, string tableName)
    {
        throw new System.NotImplementedException();
    }
    internal UriQueryBuilder GenerateQueryBuilder(bool? projectSystemProperties)
    {
        throw new System.NotImplementedException();
    }
}

}