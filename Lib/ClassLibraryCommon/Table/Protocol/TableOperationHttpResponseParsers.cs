// -----------------------------------------------------------------------------------------
// <copyright file="TableOperationHttpResponseParsers.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    internal static class TableOperationHttpResponseParsers
    {
        internal static TableResult TableOperationPreProcess(TableResult result, TableOperation operation, HttpWebResponse resp, Exception ex)
        {
            result.HttpStatusCode = (int)resp.StatusCode;

            if (operation.OperationType == TableOperationType.Retrieve)
            {
                if (resp.StatusCode != HttpStatusCode.OK && resp.StatusCode != HttpStatusCode.NotFound)
                {
                    CommonUtility.AssertNotNull("ex", ex);
                    throw ex;
                }
            }
            else
            {
                if (ex != null)
                {
                    throw ex;
                }
                else if (operation.OperationType == TableOperationType.Insert)
                {
                    if (operation.EchoContent)
                    {
                        if (resp.StatusCode != HttpStatusCode.Created)
                        {
                            throw ex;
                        }
                    }
                    else
                    {
                        if (resp.StatusCode != HttpStatusCode.NoContent)
                        {
                            throw ex;
                        }
                    }
                }
                else
                {
                    if (resp.StatusCode != HttpStatusCode.NoContent)
                    {
                        throw ex;
                    }
                }
            }

            string etag = HttpResponseParsers.GetETag(resp);
            if (etag != null)
            {
                result.Etag = etag;
                if (operation.Entity != null)
                {
                    operation.Entity.ETag = result.Etag;
                }
            }

            return result;
        }

        internal static TableResult TableOperationPostProcess(TableResult result, TableOperation operation, RESTCommand<TableResult> cmd, HttpWebResponse resp, OperationContext ctx, TableRequestOptions options, string accountName)
        {
            if (operation.OperationType != TableOperationType.Retrieve && operation.OperationType != TableOperationType.Insert)
            {
                result.Etag = HttpResponseParsers.GetETag(resp);
                operation.Entity.ETag = result.Etag;
            }
            else if (operation.OperationType == TableOperationType.Insert && (!operation.EchoContent))
            {
                if (HttpResponseParsers.GetETag(resp) != null)
                {
                    result.Etag = HttpResponseParsers.GetETag(resp);
                    operation.Entity.ETag = result.Etag;
                    operation.Entity.Timestamp = ParseETagForTimestamp(result.Etag);
                }
            }
            else
            {
                // Parse entity
                if (resp.ContentType.Contains(Constants.JsonNoMetadataAcceptHeaderValue))
                {
                    result.Etag = resp.Headers[Constants.HeaderConstants.EtagHeader];
                    CommonUtility.RunWithoutSynchronizationContext(() => ReadEntityUsingJsonParserAsync(result, operation, cmd.ResponseStream, ctx, options, System.Threading.CancellationToken.None).GetAwaiter().GetResult());
                }
                else
                {
                    CommonUtility.RunWithoutSynchronizationContext(() => ReadOdataEntityAsync(result, operation, cmd.ResponseStream, ctx, accountName, options, System.Threading.CancellationToken.None).GetAwaiter().GetResult());
                }
            }

            return result;
        }

        internal static IList<TableResult> TableBatchOperationPostProcess(IList<TableResult> result, TableBatchOperation batch, RESTCommand<IList<TableResult>> cmd, HttpWebResponse resp, OperationContext ctx, TableRequestOptions options, string accountName)
        {
            return CommonUtility.RunWithoutSynchronizationContext(() => TableBatchOperationPostProcessAsync(result, batch, cmd, resp, ctx, options, accountName, System.Threading.CancellationToken.None).GetAwaiter().GetResult());
        }


        internal static async Task<IList<TableResult>> TableBatchOperationPostProcessAsync(IList<TableResult> result, TableBatchOperation batch, RESTCommand<IList<TableResult>> cmd, HttpWebResponse resp, OperationContext ctx, TableRequestOptions options, string accountName, System.Threading.CancellationToken cancellationToken)
        {
            Stream responseStream = cmd.ResponseStream;
            StreamReader streamReader = new StreamReader(responseStream);
            string currentLine = await streamReader.ReadLineAsync().ConfigureAwait(false);
            currentLine = await streamReader.ReadLineAsync().ConfigureAwait(false);

            // Index of the currently-being-parsed entity in the batch.  Used for parsing errors, if one entity fails but prior ones succeed..
            int index = 0; 
            bool failError = false;
            bool failUnexpected = false;

            while ((currentLine != null) && !(currentLine.StartsWith(@"--batchresponse")))
            {
                while (!(currentLine.StartsWith("HTTP")))
                {
                    currentLine = await streamReader.ReadLineAsync().ConfigureAwait(false);
                }

                // The first line of the response looks like this:
                // HTTP/1.1 204 No Content
                // The HTTP status code is chars 9 - 11.
                int statusCode = Int32.Parse(currentLine.Substring(9, 3));

                Dictionary<string, string> headers = new Dictionary<string, string>();
                currentLine = await streamReader.ReadLineAsync().ConfigureAwait(false);
                while (!string.IsNullOrWhiteSpace(currentLine))
                {
                    // The headers all look like this:
                    // Cache-Control: no-cache
                    // This code below parses out the header names and values, by noting the location of the colon.
                    int colonIndex = currentLine.IndexOf(':');
                    headers[currentLine.Substring(0, colonIndex)] = currentLine.Substring(colonIndex + 2);
                    currentLine = await streamReader.ReadLineAsync().ConfigureAwait(false);
                }

                MemoryStream bodyStream = null;

                currentLine = await streamReader.ReadLineAsync().ConfigureAwait(false);
                if (statusCode != 204)
                {
                    bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(currentLine));
                }

                currentLine = await streamReader.ReadLineAsync().ConfigureAwait(false);
                currentLine = await streamReader.ReadLineAsync().ConfigureAwait(false);

                TableOperation currentOperation = batch[index];
                TableResult currentResult = new TableResult() { Result = currentOperation.Entity };
                result.Add(currentResult);

                string contentType = null;

                if (headers.ContainsKey(Constants.ContentTypeElement))
                {
                    contentType = headers[Constants.ContentTypeElement];
                }

                currentResult.HttpStatusCode = statusCode;

                // Validate Status Code.
                if (currentOperation.OperationType == TableOperationType.Insert)
                {
                    failError = statusCode == (int)HttpStatusCode.Conflict;
                    if (currentOperation.EchoContent)
                    {
                        failUnexpected = statusCode != (int)HttpStatusCode.Created;
                    }
                    else
                    {
                        failUnexpected = statusCode != (int)HttpStatusCode.NoContent;
                    }
                }
                else if (currentOperation.OperationType == TableOperationType.Retrieve)
                {
                    if (statusCode == (int)HttpStatusCode.NotFound)
                    {
                        index++;

                        // Operation => next
                        continue;
                    }

                    failUnexpected = statusCode != (int)HttpStatusCode.OK;
                }
                else
                {
                    failError = statusCode == (int)HttpStatusCode.NotFound;
                    failUnexpected = statusCode != (int)HttpStatusCode.NoContent;
                }

                if (failError)
                {
                    // If the parse error is null, then don't get the extended error information and the StorageException will contain SR.ExtendedErrorUnavailable message.
                    if (cmd.ParseError != null)
                    {
                        cmd.CurrentResult.ExtendedErrorInformation = cmd.ParseError(bodyStream, resp, contentType);
                    }

                    cmd.CurrentResult.HttpStatusCode = statusCode;
                    if (!string.IsNullOrEmpty(cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage))
                    {
                        string msg = cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage;
                        cmd.CurrentResult.HttpStatusMessage = msg.Substring(0, msg.IndexOf("\n", StringComparison.Ordinal));
                    }
                    else
                    {
                        cmd.CurrentResult.HttpStatusMessage = statusCode.ToString(CultureInfo.InvariantCulture);
                    }

                    throw new StorageException(
                        cmd.CurrentResult,
                        cmd.CurrentResult.ExtendedErrorInformation != null ? cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage : SR.ExtendedErrorUnavailable,
                        null)
                    {
                        IsRetryable = false
                    };
                }

                if (failUnexpected)
                {
                    // If the parse error is null, then don't get the extended error information and the StorageException will contain SR.ExtendedErrorUnavailable message.
                    if (cmd.ParseError != null)
                    {
                        cmd.CurrentResult.ExtendedErrorInformation = cmd.ParseError(bodyStream, resp, contentType);
                    }

                    cmd.CurrentResult.HttpStatusCode = statusCode;
                    if (!string.IsNullOrEmpty(cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage))
                    {
                        string msg = cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage;
                        cmd.CurrentResult.HttpStatusMessage = msg.Substring(0, msg.IndexOf("\n", StringComparison.Ordinal));
                    }
                    else
                    {
                        cmd.CurrentResult.HttpStatusMessage = statusCode.ToString(CultureInfo.InvariantCulture);
                    }

                    string indexString = Convert.ToString(index, CultureInfo.InvariantCulture);

                    // Attempt to extract index of failing entity from extended error info
                    if (cmd.CurrentResult.ExtendedErrorInformation != null &&
                        !string.IsNullOrEmpty(cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage))
                    {
                        string tempIndex = TableRequest.ExtractEntityIndexFromExtendedErrorInformation(cmd.CurrentResult);
                        if (!string.IsNullOrEmpty(tempIndex))
                        {
                            indexString = tempIndex;
                        }
                    }

                    throw new StorageException(cmd.CurrentResult, string.Format(CultureInfo.CurrentCulture, SR.BatchErrorInOperation, indexString), null) { IsRetryable = true };
                }

                // Update etag
                if ((headers.ContainsKey(Constants.HeaderConstants.EtagHeader)) && (!string.IsNullOrEmpty(headers[Constants.HeaderConstants.EtagHeader])))
                {
                    currentResult.Etag = headers[Constants.HeaderConstants.EtagHeader];

                    if (currentOperation.Entity != null)
                    {
                        currentOperation.Entity.ETag = currentResult.Etag;
                    }
                }

                // Parse Entity if needed
                if (currentOperation.OperationType == TableOperationType.Retrieve || (currentOperation.OperationType == TableOperationType.Insert && currentOperation.EchoContent))
                {
                    if (headers[Constants.ContentTypeElement].Contains(Constants.JsonNoMetadataAcceptHeaderValue))
                    {
                        await ReadEntityUsingJsonParserAsync(currentResult, currentOperation, bodyStream, ctx, options, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await ReadOdataEntityAsync(currentResult, currentOperation, bodyStream, ctx, accountName, options, cancellationToken).ConfigureAwait(false);
                    }
                }
                else if (currentOperation.OperationType == TableOperationType.Insert)
                {
                    currentOperation.Entity.Timestamp = ParseETagForTimestamp(currentResult.Etag);
                }

                index++;
            }

            return result;
        }

        internal static ResultSegment<TElement> TableQueryPostProcessGeneric<TElement, TQueryType>(Stream responseStream, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, HttpWebResponse resp, TableRequestOptions options, OperationContext ctx)
        {
            ResultSegment<TElement> retSeg = new ResultSegment<TElement>(new List<TElement>());
            retSeg.ContinuationToken = ContinuationFromResponse(resp);

            if (resp.ContentType.Contains(Constants.JsonNoMetadataAcceptHeaderValue))
            {
                CommonUtility.RunWithoutSynchronizationContext(() => ReadQueryResponseUsingJsonParserAsync(retSeg, responseStream, resp.Headers[Constants.HeaderConstants.EtagHeader], resolver, options.PropertyResolver, typeof(TQueryType), null, options, System.Threading.CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                List<KeyValuePair<string, Dictionary<string, object>>> results = CommonUtility.RunWithoutSynchronizationContext(() => ReadQueryResponseUsingJsonParserMetadataAsync(responseStream, System.Threading.CancellationToken.None).GetAwaiter().GetResult());

                foreach (KeyValuePair<string, Dictionary<string, object>> kvp in results)
                {
                    retSeg.Results.Add(ReadAndResolve(kvp.Key, kvp.Value, resolver, options));
                }
            }

            Logger.LogInformational(ctx, SR.RetrieveWithContinuationToken, retSeg.Results.Count, retSeg.ContinuationToken);
            return retSeg;
        }

        public static async Task ReadQueryResponseUsingJsonParserAsync<TElement>(ResultSegment<TElement> retSeg, Stream responseStream, string etag, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, Func<string, string, string, string, EdmType> propertyResolver, Type type, OperationContext ctx, TableRequestOptions options, System.Threading.CancellationToken cancellationToken)
        {
            StreamReader streamReader = new StreamReader(responseStream);

            // Read this value now and not later so that the cache is either used or not used for the entire query response parsing.
            bool disablePropertyResolverCache = false;

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
            if (TableEntity.DisablePropertyResolverCache)
            {
                disablePropertyResolverCache = TableEntity.DisablePropertyResolverCache;
                Logger.LogVerbose(ctx, SR.PropertyResolverCacheDisabled);
            }
#endif
            using (JsonReader reader = new JsonTextReader(streamReader))
            {
                reader.DateParseHandling = DateParseHandling.None;
                JObject dataSet = await JObject.LoadAsync(reader, cancellationToken).ConfigureAwait(false);
                JToken dataTable = dataSet["value"];

                foreach (JToken token in dataTable)
                {
                    string unused;
                    Dictionary<string, object> results = ReadSingleItem(token, out unused);

                    Dictionary<string, string> properties = new Dictionary<string, string>();

                    foreach (string key in results.Keys)
                    {
                        if (results[key] == null)
                        {
                            properties.Add(key, null);
                            continue;
                        }

                        if (results[key] is string)
                        {
                            properties.Add(key, (string)results[key]);
                        }
                        else if (results[key] is DateTime)
                        {
                            properties.Add(key, ((DateTime)results[key]).ToUniversalTime().ToString("o"));
                        }
                        // This should never be a long; if requires a 64-bit number the service will send it as a string instead.
                        else if (results[key] is bool || results[key] is double || results[key] is int)
                        {
                            properties.Add(key, (results[key]).ToString());
                        }
                        else
                        {
                            throw new StorageException(string.Format(CultureInfo.InvariantCulture, SR.InvalidTypeInJsonDictionary, results[key].GetType().ToString()));
                        }
                    }

                    retSeg.Results.Add(ReadAndResolveWithEdmTypeResolver(properties, resolver, propertyResolver, etag, type, ctx, disablePropertyResolverCache, options));
                }

                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.JsonReaderNotInCompletedState));
                }
            }
        }

        public static async Task<List<KeyValuePair<string, Dictionary<string, object>>>> ReadQueryResponseUsingJsonParserMetadataAsync(Stream responseStream, System.Threading.CancellationToken cancellationToken)
        {
            List<KeyValuePair<string, Dictionary<string, object>>> results = new List<KeyValuePair<string, Dictionary<string, object>>>();
            StreamReader streamReader = new StreamReader(responseStream);
            using (JsonReader reader = new JsonTextReader(streamReader))
            {
                reader.DateParseHandling = DateParseHandling.None;
                JObject dataSet = await JObject.LoadAsync(reader, cancellationToken).ConfigureAwait(false);
                JToken dataTable = dataSet["value"];
                
                foreach (JToken token in dataTable)
                {
                    string etag;
                    Dictionary<string, object> properties = ReadSingleItem(token, out etag);

                    results.Add(new KeyValuePair<string, Dictionary<string, object>>(etag, properties));
                }

                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.JsonReaderNotInCompletedState));
                }
            }

            return results;
        }

        public static async Task<KeyValuePair<string, Dictionary<string, object>>> ReadSingleItemResponseUsingJsonParserMetadataAsync(Stream responseStream, System.Threading.CancellationToken cancellationToken)
        {
            StreamReader streamReader = new StreamReader(responseStream);
            using (JsonReader reader = new JsonTextReader(streamReader))
            {
                reader.DateParseHandling = DateParseHandling.None;
                JObject dataSet = await JObject.LoadAsync(reader, cancellationToken).ConfigureAwait(false);

                string etag;
                Dictionary<string, object> properties = ReadSingleItem(dataSet, out etag);

                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.JsonReaderNotInCompletedState));
                }
                return new KeyValuePair<string, Dictionary<string, object>>(etag, properties);
            }
        }

        private static Dictionary<string, object> ReadSingleItem(JToken token, out string etag)
        {
            Dictionary<string, object> properties = token.ToObject<Dictionary<string, object>>(DefaultSerializer.Instance);

            // Parse the etag, and remove all the "odata.*" properties we don't use.
            if (properties.ContainsKey(@"odata.etag"))
            {
                etag = (string)properties[@"odata.etag"];
            }
            else
            {
                etag = null;
            }

            foreach (string odataPropName in properties.Keys.Where(key => key.StartsWith(@"odata.", StringComparison.Ordinal)).ToArray())
            {
                properties.Remove(odataPropName);
            }

            // We have to special-case timestamp here, because in the 'minimalmetadata' case, 
            // Timestamp doesn't have an "@odata.type" property - the assumption is that you know
            // the type.
            if (properties.ContainsKey(@"Timestamp") && properties[@"Timestamp"].GetType() == typeof(string))
            {
                properties[@"Timestamp"] = DateTime.Parse((string)properties[@"Timestamp"], CultureInfo.InvariantCulture);
            }

            // In the full metadata case, this property will exist, and we need to remove it.
            if (properties.ContainsKey(@"Timestamp@odata.type"))
            {
                properties.Remove(@"Timestamp@odata.type");
            }

            // Replace all the 'long's with 'int's (the JSON parser parses all integer types into longs, but the odata spec specifies that they're int's.
            foreach (KeyValuePair<string, object> odataProp in properties.Where(kvp => (kvp.Value != null) && (kvp.Value.GetType() == typeof(long))).ToArray())
            {
                // We first have to unbox the value into a "long", then downcast into an "int".  C# will not combine the operations.
                properties[odataProp.Key] = (int)(long)odataProp.Value;
            }

            foreach (KeyValuePair<string, object> typeAnnotation in properties.Where(kvp => kvp.Key.EndsWith(@"@odata.type", StringComparison.Ordinal)).ToArray())
            {
                properties.Remove(typeAnnotation.Key);
                string propName = typeAnnotation.Key.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries)[0];
                switch ((string)typeAnnotation.Value)
                {
                    case Constants.EdmBinary:
                        properties[propName] = Convert.FromBase64String((string)properties[propName]);
                        break;
                    case Constants.EdmBoolean:
                        properties[propName] = Boolean.Parse((string)properties[propName]);
                        break;
                    case Constants.EdmDateTime:
                        properties[propName] = DateTime.Parse((string)properties[propName], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                        break;
                    case Constants.EdmDouble:
                        properties[propName] = Double.Parse((string)properties[propName], CultureInfo.InvariantCulture);
                        break;
                    case Constants.EdmGuid:
                        properties[propName] = Guid.Parse((string)properties[propName]);
                        break;
                    case Constants.EdmInt32:
                        properties[propName] = Int32.Parse((string)properties[propName], CultureInfo.InvariantCulture);
                        break;
                    case Constants.EdmInt64:
                        properties[propName] = Int64.Parse((string)properties[propName], CultureInfo.InvariantCulture);
                        break;
                    case Constants.EdmString:
                        properties[propName] = (string)properties[propName];
                        break;
                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.UnexpectedEDMType, typeAnnotation.Value));
                }
            }

            return properties;
        }

        /// <summary>
        /// Gets the table continuation from response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>The continuation.</returns>
        internal static TableContinuationToken ContinuationFromResponse(HttpWebResponse response)
        {
            string nextPartitionKey = response.Headers[TableConstants.TableServicePrefixForTableContinuation + TableConstants.TableServiceNextPartitionKey];
            string nextRowKey = response.Headers[TableConstants.TableServicePrefixForTableContinuation + TableConstants.TableServiceNextRowKey];
            string nextTableName = response.Headers[TableConstants.TableServicePrefixForTableContinuation + TableConstants.TableServiceNextTableName];

            nextPartitionKey = string.IsNullOrEmpty(nextPartitionKey) ? null : nextPartitionKey;
            nextRowKey = string.IsNullOrEmpty(nextRowKey) ? null : nextRowKey;
            nextTableName = string.IsNullOrEmpty(nextTableName) ? null : nextTableName;

            if (nextPartitionKey == null && nextRowKey == null && nextTableName == null)
            {
                return null;
            }

            TableContinuationToken newContinuationToken = new TableContinuationToken()
            {
                NextPartitionKey = nextPartitionKey,
                NextRowKey = nextRowKey,
                NextTableName = nextTableName
            };

            return newContinuationToken;
        }

        private static async Task ReadOdataEntityAsync(TableResult result, TableOperation operation, Stream responseStream, OperationContext ctx, string accountName, TableRequestOptions options, System.Threading.CancellationToken cancellationToken)
        {
            KeyValuePair<string, Dictionary<string, object>> rawEntity = await ReadSingleItemResponseUsingJsonParserMetadataAsync(responseStream, cancellationToken).ConfigureAwait(false);


            if (operation.OperationType == TableOperationType.Retrieve)
            {
                result.Result = ReadAndResolve(rawEntity.Key, rawEntity.Value, operation.RetrieveResolver, options);
                result.Etag = rawEntity.Key;
            }
            else
            {
                result.Etag = ReadAndUpdateTableEntity(
                                                        operation.Entity,
                                                        rawEntity.Key, 
                                                        rawEntity.Value,
                                                        EntityReadFlags.Timestamp | EntityReadFlags.Etag,
                                                        ctx);
            }
        }

        private static async Task ReadEntityUsingJsonParserAsync(TableResult result, TableOperation operation, Stream stream, OperationContext ctx, TableRequestOptions options, System.Threading.CancellationToken cancellationToken)
        {
            StreamReader streamReader = new StreamReader(stream);
            using (JsonReader reader = new JsonTextReader(streamReader))
            {
                JObject dataSet = await JObject.LoadAsync(reader, cancellationToken).ConfigureAwait(false);

                // We can't use dataSet.ToObject<Dictionary<string, string>>() here, as it doesn't handle
                // DateTime objects properly.

                string temp;
                Dictionary<string, object> results = ReadSingleItem(dataSet, out temp);

                Dictionary<string, string> properties = new Dictionary<string, string>();

                foreach (string key in results.Keys)
                {
                    if (results[key] == null)
                    {
                        properties.Add(key, null);
                        continue;
                    }
                    Type type = results[key].GetType();
                    if (type == typeof(string))
                    {
                        properties.Add(key, (string)results[key]);
                    }
                    else if (type == typeof(DateTime))
                    {
                        properties.Add(key, ((DateTime)results[key]).ToUniversalTime().ToString("o"));
                    }
                    else if (type == typeof(bool))
                    {
                        properties.Add(key, ((bool)results[key]).ToString());
                    }
                    else if (type == typeof(double))
                    {
                        properties.Add(key, ((double)results[key]).ToString());
                    }
                    else if (type == typeof(int))
                    {
                        properties.Add(key, ((int)results[key]).ToString());
                    }
                    else
                    {
                        throw new StorageException();
                    }
                }

                if (operation.OperationType == TableOperationType.Retrieve)
                {
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
                    result.Result = ReadAndResolveWithEdmTypeResolver(properties, operation.RetrieveResolver, options.PropertyResolver, result.Etag, operation.PropertyResolverType, ctx, TableEntity.DisablePropertyResolverCache, options);
#else
                    // doesn't matter what is passed for disablePropertyResolverCache for windows phone because it is not read.
                    result.Result = ReadAndResolveWithEdmTypeResolver(properties, operation.RetrieveResolver, options.PropertyResolver, result.Etag, operation.PropertyResolverType, ctx, true, options);
#endif
                }
                else
                {
                    ReadAndUpdateTableEntityWithEdmTypeResolver(operation.Entity, properties, EntityReadFlags.Timestamp | EntityReadFlags.Etag, options.PropertyResolver, ctx);
                }

                if (reader.Read())
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.JsonReaderNotInCompletedState));
                }
            }
        }

        private static T ReadAndResolve<T>(string etag, Dictionary<string, object> props, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, T> resolver, TableRequestOptions options)
        {
            string pk = null;
            string rk = null;
            byte[] cek = null;
            DateTimeOffset ts = new DateTimeOffset();
            Dictionary<string, EntityProperty> properties = new Dictionary<string, EntityProperty>();

            foreach (KeyValuePair<string, object> prop in props)
            {
                string propName = prop.Key;
                if (propName == TableConstants.PartitionKey)
                {
                    pk = (string)prop.Value;
                }
                else if (propName == TableConstants.RowKey)
                {
                    rk = (string)prop.Value;
                }
                else if (propName == TableConstants.Timestamp)
                {
                    ts = new DateTimeOffset((DateTime)prop.Value);
                }
                else
                {
                    properties.Add(propName, EntityProperty.CreateEntityPropertyFromObject(prop.Value));
                }
            }

            // If encryption policy is set on options, try to decrypt the entity.
            EntityProperty propertyDetailsProperty;
            EntityProperty keyProperty;

            if (options.EncryptionPolicy != null)
            {
                if (properties.TryGetValue(Constants.EncryptionConstants.TableEncryptionPropertyDetails, out propertyDetailsProperty)
                    && properties.TryGetValue(Constants.EncryptionConstants.TableEncryptionKeyDetails, out keyProperty))
                {
                    // Decrypt the metadata property value to get the names of encrypted properties.
                    EncryptionData encryptionData = null;
                    bool isJavaV1 = false;
                    cek = options.EncryptionPolicy.DecryptMetadataAndReturnCEK(pk, rk, keyProperty, propertyDetailsProperty, out encryptionData, out isJavaV1);

                    byte[] binaryVal = propertyDetailsProperty.BinaryValue;
                    HashSet<string> encryptedPropertyDetailsSet;

                    encryptedPropertyDetailsSet = ParseEncryptedPropertyDetailsSet(isJavaV1, binaryVal);

                    properties = options.EncryptionPolicy.DecryptEntity(properties, encryptedPropertyDetailsSet, pk, rk, cek, encryptionData, isJavaV1);
                }
                else
                {
                    if (options.RequireEncryption.HasValue && options.RequireEncryption.Value)
                    {
                        throw new StorageException(SR.EncryptionDataNotPresentError, null) { IsRetryable = false };
                    }
                }
            }

            return resolver(pk, rk, ts, properties, etag);
        }

        private static HashSet<string> ParseEncryptedPropertyDetailsSet(bool isJavav1, byte[] binaryVal)
        {
            HashSet<string> encryptedPropertyDetailsSet;
            if (isJavav1)
            {
                encryptedPropertyDetailsSet = new HashSet<string>(Encoding.UTF8.GetString(binaryVal, 1, binaryVal.Length - 2).Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                encryptedPropertyDetailsSet = JsonConvert.DeserializeObject<HashSet<string>>(Encoding.UTF8.GetString(binaryVal, 0, binaryVal.Length), DefaultSerializerSettings.Instance);
            }

            return encryptedPropertyDetailsSet;
        }

        private static T ReadAndResolveWithEdmTypeResolver<T>(Dictionary<string, string> entityAttributes, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, T> resolver, Func<string, string, string, string, EdmType> propertyResolver, string etag, Type type, OperationContext ctx, bool disablePropertyResolverCache, TableRequestOptions options)
        {
            string pk = null;
            string rk = null;
            byte[] cek = null;
            EncryptionData encryptionData = null;
            DateTimeOffset ts = new DateTimeOffset();
            Dictionary<string, EntityProperty> properties = new Dictionary<string, EntityProperty>();
            Dictionary<string, EdmType> propertyResolverDictionary = null;
            HashSet<string> encryptedPropertyDetailsSet = null;

            if (type != null)
            {
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
                if (!disablePropertyResolverCache)
                {
                    propertyResolverDictionary = TableEntity.PropertyResolverCache.GetOrAdd(type, TableOperationHttpResponseParsers.CreatePropertyResolverDictionary);
                }
                else
                {
                    propertyResolverDictionary = TableOperationHttpResponseParsers.CreatePropertyResolverDictionary(type);
                }
#else
                propertyResolverDictionary = TableOperationHttpResponseParsers.CreatePropertyResolverDictionary(type);
#endif
            }

            // Decrypt the metadata property value to get the names of encrypted properties so that they can be parsed correctly below.
            bool isJavaV1 = false;
            if (options.EncryptionPolicy != null)
            {
                string metadataValue = null;
                string keyPropertyValue = null;

                if (entityAttributes.TryGetValue(Constants.EncryptionConstants.TableEncryptionPropertyDetails, out metadataValue)
                && entityAttributes.TryGetValue(Constants.EncryptionConstants.TableEncryptionKeyDetails, out keyPropertyValue))
                {
                    EntityProperty propertyDetailsProperty = EntityProperty.CreateEntityPropertyFromObject(metadataValue, EdmType.Binary);
                    EntityProperty keyProperty = EntityProperty.CreateEntityPropertyFromObject(keyPropertyValue, EdmType.String);

                    entityAttributes.TryGetValue(TableConstants.PartitionKey, out pk);
                    entityAttributes.TryGetValue(TableConstants.RowKey, out rk);
                    cek = options.EncryptionPolicy.DecryptMetadataAndReturnCEK(pk, rk, keyProperty, propertyDetailsProperty, out encryptionData, out isJavaV1);

                    properties.Add(Constants.EncryptionConstants.TableEncryptionPropertyDetails, propertyDetailsProperty);

                    byte[] binaryVal = propertyDetailsProperty.BinaryValue;
                    encryptedPropertyDetailsSet = ParseEncryptedPropertyDetailsSet(isJavaV1, binaryVal);
                }
                else
                {
                    if (options.RequireEncryption.HasValue && options.RequireEncryption.Value)
                    {
                        throw new StorageException(SR.EncryptionDataNotPresentError, null) { IsRetryable = false };
                    }
                }
            }
            
            foreach (KeyValuePair<string, string> prop in entityAttributes)
            {
                if (prop.Key == TableConstants.PartitionKey)
                {
                    pk = (string)prop.Value;
                }
                else if (prop.Key == TableConstants.RowKey)
                {
                    rk = (string)prop.Value;
                }
                else if (prop.Key == TableConstants.Timestamp)
                {
                    ts = DateTimeOffset.Parse(prop.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    if (etag == null)
                    {
                        etag = GetETagFromTimestamp(prop.Value);
                    }
                }
                else if (prop.Key == Constants.EncryptionConstants.TableEncryptionKeyDetails)
                {
                    // This and the following check are required because in JSON no-metadata, the type information for the properties are not returned and users are 
                    // not expected to provide a type for them. So based on how the user defined property resolvers treat unknown properties, we might get unexpected results.
                    properties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, EdmType.String));
                }
                else if (prop.Key == Constants.EncryptionConstants.TableEncryptionPropertyDetails)
                {
                    if (!properties.ContainsKey(Constants.EncryptionConstants.TableEncryptionPropertyDetails))
                    {
                        // If encryption policy is not set, then add the value as-is to the dictionary.
                        properties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, EdmType.Binary));
                    }
                    else
                    {
                        // Do nothing. Already handled above. 
                    }
                }
                else
                {
                    if (propertyResolver != null)
                    {
                        Logger.LogVerbose(ctx, SR.UsingUserProvidedPropertyResolver);
                        try
                        {
                            EdmType edmType = propertyResolver(pk, rk, prop.Key, prop.Value);
                            Logger.LogVerbose(ctx, SR.AttemptedEdmTypeForTheProperty, prop.Key, edmType);
                            try
                            {
                                CreateEntityPropertyFromObject(properties, encryptedPropertyDetailsSet, prop, edmType);
                            }
                            catch (FormatException ex)
                            {
                                throw new StorageException(string.Format(CultureInfo.InvariantCulture, SR.FailParseProperty, prop.Key, prop.Value, edmType), ex) { IsRetryable = false };
                            }
                        }
                        catch (StorageException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            throw new StorageException(SR.PropertyResolverThrewError, ex) { IsRetryable = false };
                        }
                    }
                    else if (type != null)
                    {
                        Logger.LogVerbose(ctx, SR.UsingDefaultPropertyResolver);
                        EdmType edmType;
                        if (propertyResolverDictionary != null)
                        {
                            propertyResolverDictionary.TryGetValue(prop.Key, out edmType);
                            Logger.LogVerbose(ctx, SR.AttemptedEdmTypeForTheProperty, prop.Key, edmType);
                            CreateEntityPropertyFromObject(properties, encryptedPropertyDetailsSet, prop, edmType);
                        }
                    }
                    else
                    {
                        Logger.LogVerbose(ctx, SR.NoPropertyResolverAvailable);
                        CreateEntityPropertyFromObject(properties, encryptedPropertyDetailsSet, prop, EdmType.String);
                    }
                }
            }

            // If encryption policy is set on options, try to decrypt the entity.
            if (options.EncryptionPolicy != null && encryptionData != null)
            {
                properties = options.EncryptionPolicy.DecryptEntity(properties, encryptedPropertyDetailsSet, pk, rk, cek, encryptionData, isJavaV1);
            }

            return resolver(pk, rk, ts, properties, etag);
        }

        private static void CreateEntityPropertyFromObject(Dictionary<string, EntityProperty> properties, HashSet<string> encryptedPropertyDetailsSet, KeyValuePair<string, string> prop, EdmType edmType)
        {
            // Handle the case where the property is encrypted.
            if (encryptedPropertyDetailsSet != null && encryptedPropertyDetailsSet.Contains(prop.Key))
            {
                properties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, EdmType.Binary));
            }
            else
            {
                properties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, edmType));
            }
        }


        // returns etag
        internal static string ReadAndUpdateTableEntity(ITableEntity entity, string etag, Dictionary<string, object> props, EntityReadFlags flags, OperationContext ctx)
        {
            if ((flags & EntityReadFlags.Etag) > 0)
            {
                entity.ETag = etag;
            }

            Dictionary<string, EntityProperty> entityProperties = (flags & EntityReadFlags.Properties) > 0 ? new Dictionary<string, EntityProperty>() : null;

            if (flags > 0)
            {
                foreach (KeyValuePair<string, object> prop in props)
                {
                    if (prop.Key == TableConstants.PartitionKey)
                    {
                        if ((flags & EntityReadFlags.PartitionKey) == 0)
                        {
                            continue;
                        }

                        entity.PartitionKey = (string)prop.Value;
                    }
                    else if (prop.Key == TableConstants.RowKey)
                    {
                        if ((flags & EntityReadFlags.RowKey) == 0)
                        {
                            continue;
                        }

                        entity.RowKey = (string)prop.Value;
                    }
                    else if (prop.Key == TableConstants.Timestamp)
                    {
                        if ((flags & EntityReadFlags.Timestamp) == 0)
                        {
                            continue;
                        }

                        entity.Timestamp = (DateTime)prop.Value;
                    }
                    else if ((flags & EntityReadFlags.Properties) > 0)
                    {
                        entityProperties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value));
                    }
                }

                if ((flags & EntityReadFlags.Properties) > 0)
                {
                    entity.ReadEntity(entityProperties, ctx);
                }
            }

            return etag;
        }

        internal static void ReadAndUpdateTableEntityWithEdmTypeResolver(ITableEntity entity, Dictionary<string, string> entityAttributes, EntityReadFlags flags, Func<string, string, string, string, EdmType> propertyResolver, OperationContext ctx)
        {
            Dictionary<string, EntityProperty> entityProperties = (flags & EntityReadFlags.Properties) > 0 ? new Dictionary<string, EntityProperty>() : null;
            Dictionary<string, EdmType> propertyResolverDictionary = null;

            // Try to add the dictionary to the cache only if it is not a DynamicTableEntity. If DisablePropertyResolverCache is true, then just use reflection and generate dictionaries for each entity.
            if (entity.GetType() != typeof(DynamicTableEntity))
            {
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
                if (!TableEntity.DisablePropertyResolverCache)
                {
                    propertyResolverDictionary = TableEntity.PropertyResolverCache.GetOrAdd(entity.GetType(), TableOperationHttpResponseParsers.CreatePropertyResolverDictionary);
                }
                else
                {
                    Logger.LogVerbose(ctx, SR.PropertyResolverCacheDisabled);
                    propertyResolverDictionary = TableOperationHttpResponseParsers.CreatePropertyResolverDictionary(entity.GetType());
                }
#else
                propertyResolverDictionary = TableOperationHttpResponseParsers.CreatePropertyResolverDictionary(entity.GetType());
#endif
            }

            if (flags > 0)
            {
                foreach (KeyValuePair<string, string> prop in entityAttributes)
                {
                    if (prop.Key == TableConstants.PartitionKey)
                    {
                        entity.PartitionKey = (string)prop.Value;
                    }
                    else if (prop.Key == TableConstants.RowKey)
                    {
                        entity.RowKey = (string)prop.Value;
                    }
                    else if (prop.Key == TableConstants.Timestamp)
                    {
                        if ((flags & EntityReadFlags.Timestamp) == 0)
                        {
                            continue;
                        }

                        entity.Timestamp = DateTime.Parse(prop.Value, CultureInfo.InvariantCulture);
                    }
                    else if ((flags & EntityReadFlags.Properties) > 0)
                    {
                        if (propertyResolver != null)
                        {
                            Logger.LogVerbose(ctx, SR.UsingUserProvidedPropertyResolver);
                            try
                            {
                                EdmType type = propertyResolver(entity.PartitionKey, entity.RowKey, prop.Key, prop.Value);
                                Logger.LogVerbose(ctx, SR.AttemptedEdmTypeForTheProperty, prop.Key, type.GetType().ToString());
                                try
                                {
                                    entityProperties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, type.GetType()));
                                }
                                catch (FormatException ex)
                                {
                                    throw new StorageException(string.Format(CultureInfo.InvariantCulture, SR.FailParseProperty, prop.Key, prop.Value, type.ToString()), ex) { IsRetryable = false };
                                }
                            }
                            catch (StorageException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                throw new StorageException(SR.PropertyResolverThrewError, ex) { IsRetryable = false };
                            }
                        }
                        else if (entity.GetType() != typeof(DynamicTableEntity))
                        {
                            EdmType edmType;
                            Logger.LogVerbose(ctx, SR.UsingDefaultPropertyResolver);

                            if (propertyResolverDictionary != null)
                            {
                                propertyResolverDictionary.TryGetValue(prop.Key, out edmType);
                                Logger.LogVerbose(ctx, SR.AttemptedEdmTypeForTheProperty, prop.Key, edmType);
                                entityProperties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, edmType));
                            }
                        }
                        else
                        {
                            Logger.LogVerbose(ctx, SR.NoPropertyResolverAvailable);
                            entityProperties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, typeof(string)));
                        }
                    }
                }

                if ((flags & EntityReadFlags.Properties) > 0)
                {
                    entity.ReadEntity(entityProperties, ctx);
                }
            }
        }

        private static DateTimeOffset ParseETagForTimestamp(string etag)
        {
            // Handle strong ETags as well.
            if (etag.StartsWith("W/", StringComparison.Ordinal))
            {
                etag = etag.Substring(2);
            }

            // DateTimeOffset.ParseExact can't be used because the decimal part after seconds may not be present for rounded times.
            etag = etag.Substring(Constants.ETagPrefix.Length, etag.Length - 2 - Constants.ETagPrefix.Length);
            return DateTimeOffset.Parse(Uri.UnescapeDataString(etag), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        private static string GetETagFromTimestamp(string timeStampString)
        {
            timeStampString = Uri.EscapeDataString(timeStampString);
            return "W/\"datetime'" + timeStampString + "'\"";
        }

        private static Dictionary<string, EdmType> CreatePropertyResolverDictionary(Type type)
        {
            Dictionary<string, EdmType> propertyResolverDictionary = new Dictionary<string, EdmType>();

#if WINDOWS_RT
            IEnumerable<PropertyInfo> objectProperties = type.GetRuntimeProperties();
#else
            IEnumerable<PropertyInfo> objectProperties = type.GetProperties();
#endif

            foreach (PropertyInfo property in objectProperties)
            {
                if (property.PropertyType == typeof(byte[]))
                {
                    propertyResolverDictionary.Add(property.Name, EdmType.Binary);
                }
                else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                {
                    propertyResolverDictionary.Add(property.Name, EdmType.Boolean);
                }
                else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?) || property.PropertyType == typeof(DateTimeOffset) || property.PropertyType == typeof(DateTimeOffset?))
                {
                    propertyResolverDictionary.Add(property.Name, EdmType.DateTime);
                }
                else if (property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
                {
                    propertyResolverDictionary.Add(property.Name, EdmType.Double);
                }
                else if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
                {
                    propertyResolverDictionary.Add(property.Name, EdmType.Guid);
                }
                else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                {
                    propertyResolverDictionary.Add(property.Name, EdmType.Int32);
                }
                else if (property.PropertyType == typeof(long) || property.PropertyType == typeof(long?))
                {
                    propertyResolverDictionary.Add(property.Name, EdmType.Int64);
                }
                else
                {
                    propertyResolverDictionary.Add(property.Name, EdmType.String);
                }
            }

            return propertyResolverDictionary;
        }
    }
}
