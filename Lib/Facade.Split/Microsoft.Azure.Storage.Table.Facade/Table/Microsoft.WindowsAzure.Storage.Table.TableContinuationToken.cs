using Microsoft.Azure.Storage.Core;
using System;
using System.Globalization;
using System.Xml;
namespace Microsoft.Azure.Storage.Table
{
public sealed class TableContinuationToken : IContinuationToken
{

    private string Version
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

    private string Type
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

    public string NextPartitionKey
    {
        get; set;
    }

    public string NextRowKey
    {
        get; set;
    }

    public string NextTableName
    {
        get; set;
    }

    public StorageLocation? TargetLocation
    {
        get; set;
    }

    internal void ApplyToUriQueryBuilder(UriQueryBuilder builder)
    {
        throw new System.NotImplementedException();
    }
}

}