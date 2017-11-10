using System;
using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Table
{
internal static class EntityUtilities
{
    internal static TElement ResolveEntityByType<TElement>(string partitionKey, string rowKey, DateTimeOffset timestamp, IDictionary<string, EntityProperty> properties, string etag)
    {
        throw new System.NotImplementedException();
    }
    internal static DynamicTableEntity ResolveDynamicEntity(string partitionKey, string rowKey, DateTimeOffset timestamp, IDictionary<string, EntityProperty> properties, string etag)
    {
        throw new System.NotImplementedException();
    }
    internal static object InstantiateEntityFromType(Type type)
    {
        throw new System.NotImplementedException();
    }
}

}