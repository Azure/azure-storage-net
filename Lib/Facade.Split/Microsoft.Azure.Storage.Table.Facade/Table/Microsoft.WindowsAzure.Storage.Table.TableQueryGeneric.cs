using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Core.Executor;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using Microsoft.Azure.Storage.Table.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.Table
{
public class TableQuery<TElement> where TElement : ITableEntity, new()
{
    public int? TakeCount
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

    public string FilterString
    {
        get; set;
    }

    public IList<string> SelectColumns
    {
        get; set;
    }

    public TableQuery<TElement> Select(IList<string> columns)
    {
        throw new System.NotImplementedException();
    }
    public TableQuery<TElement> Take(int? take)
    {
        throw new System.NotImplementedException();
    }
    public TableQuery<TElement> Where(string filter)
    {
        throw new System.NotImplementedException();
    }
    public TableQuery<TElement> Copy()
    {
        throw new System.NotImplementedException();
    }
    internal UriQueryBuilder GenerateQueryBuilder(bool? projectSystemProperties)
    {
        throw new System.NotImplementedException();
    }
}

}