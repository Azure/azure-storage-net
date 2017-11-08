using System;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.Table
{
public delegate T EntityResolver<T>(string partitionKey, string rowKey, DateTimeOffset timestamp, IDictionary<string, EntityProperty> properties, string etag);

}