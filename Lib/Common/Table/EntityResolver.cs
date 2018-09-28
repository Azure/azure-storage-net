// -----------------------------------------------------------------------------------------
// <copyright file="EntityResolver.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Table
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Returns a delegate for resolving entities.
    /// </summary>
    /// <typeparam name="T">The type into which the query results are projected.</typeparam>
    /// <param name="partitionKey">A string containing the partition key for the entity.</param>
    /// <param name="rowKey">A string containing the row key for the entity.</param>
    /// <param name="timestamp">A <see cref="DateTimeOffset"/> containing the timestamp for the entity.</param>
    /// <param name="properties">An <see cref="IDictionary{TKey,TValue}"/> object containing the properties for the entity.</param>
    /// <param name="etag">The ETag.</param>
    /// <returns></returns>  
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "etag", Justification = "Reviewed: etag can be used for identifier names.")]
    public delegate T EntityResolver<T>(string partitionKey, string rowKey, DateTimeOffset timestamp, IDictionary<string, EntityProperty> properties, string etag);
}
