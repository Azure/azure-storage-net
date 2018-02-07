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
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Table.Protocol
{
internal static class TableOperationHttpResponseParsers
{

    public static Task ReadQueryResponseUsingJsonParserAsync<TElement>(ResultSegment<TElement> retSeg, Stream responseStream, string etag, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, Func<string, string, string, string, EdmType> propertyResolver, Type type, OperationContext ctx, TableRequestOptions options, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public static Task<List<KeyValuePair<string, Dictionary<string, object>>>> ReadQueryResponseUsingJsonParserMetadataAsync(Stream responseStream, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    public static Task<KeyValuePair<string, Dictionary<string, object>>> ReadSingleItemResponseUsingJsonParserMetadataAsync(Stream responseStream, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private static Dictionary<string, object> ReadSingleItem(JToken token, out string etag)
    {
        throw new System.NotImplementedException();
    }
    internal static void ReadAndUpdateTableEntityWithEdmTypeResolver(ITableEntity entity, Dictionary<string, string> entityAttributes, EntityReadFlags flags, Func<string, string, string, string, EdmType> propertyResolver, OperationContext ctx)
    {
        throw new System.NotImplementedException();
    }
    private static T ReadAndResolve<T>(string etag, Dictionary<string, object> props, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, T> resolver, TableRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal static string ReadAndUpdateTableEntity(ITableEntity entity, string etag, Dictionary<string, object> props, EntityReadFlags flags, OperationContext ctx)
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