using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
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
public class TableQuery
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

    public static string GenerateFilterCondition(string propertyName, string operation, string givenValue)
    {
        throw new System.NotImplementedException();
    }
    public static string GenerateFilterConditionForBool(string propertyName, string operation, bool givenValue)
    {
        throw new System.NotImplementedException();
    }
    public static string GenerateFilterConditionForDate(string propertyName, string operation, DateTimeOffset givenValue)
    {
        throw new System.NotImplementedException();
    }
    public static string GenerateFilterConditionForDouble(string propertyName, string operation, double givenValue)
    {
        throw new System.NotImplementedException();
    }
    public static string GenerateFilterConditionForInt(string propertyName, string operation, int givenValue)
    {
        throw new System.NotImplementedException();
    }
    public static string GenerateFilterConditionForLong(string propertyName, string operation, long givenValue)
    {
        throw new System.NotImplementedException();
    }
    public static string GenerateFilterConditionForGuid(string propertyName, string operation, Guid givenValue)
    {
        throw new System.NotImplementedException();
    }
    private static string GenerateFilterCondition(string propertyName, string operation, string givenValue, EdmType edmType)
    {
        throw new System.NotImplementedException();
    }
    public static string CombineFilters(string filterA, string operatorString, string filterB)
    {
        throw new System.NotImplementedException();
    }
    public TableQuery Select(IList<string> columns)
    {
        throw new System.NotImplementedException();
    }
    public TableQuery Take(int? take)
    {
        throw new System.NotImplementedException();
    }
    public TableQuery Where(string filter)
    {
        throw new System.NotImplementedException();
    }
    public TableQuery Copy()
    {
        throw new System.NotImplementedException();
    }
    internal UriQueryBuilder GenerateQueryBuilder(bool? projectSystemProperties)
    {
        throw new System.NotImplementedException();
    }
}

}
