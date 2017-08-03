using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Executor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
 
using System.Reflection;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Table.Protocol
{
internal static class TableOperationHttpResponseParsers
{

    private static void DrainODataReader(ODataReader reader)
    {
        throw new System.NotImplementedException();
    }
    private static void ReadEntityUsingJsonParser(TableResult result, TableOperation operation, Stream stream, OperationContext ctx, TableRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal static void ReadAndUpdateTableEntityWithEdmTypeResolver(ITableEntity entity, Dictionary<string, string> entityAttributes, EntityReadFlags flags, Func<string, string, string, string, EdmType> propertyResolver, OperationContext ctx)
    {
        throw new System.NotImplementedException();
    }
    internal static string ReadAndUpdateTableEntity(ITableEntity entity, ODataEntry entry, EntityReadFlags flags, OperationContext ctx)
    {
        throw new System.NotImplementedException();
    }
    private static DateTimeOffset ParseETagForTimestamp(string etag)
    {
        throw new System.NotImplementedException();
    }
    private static string GetETagFromTimestamp(string timeStampString)
    {
        throw new System.NotImplementedException();
    }
    private static Dictionary<string, EdmType> CreatePropertyResolverDictionary(Type type)
    {
        throw new System.NotImplementedException();
    }
}

}