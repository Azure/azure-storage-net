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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Globalization;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Reflection;
    using System.Threading;
    using System.Text;

    internal static class TableOperationHttpResponseParsers
    {
        internal static TableResult TableOperationPreProcess<T>(TableResult result, TableOperation operation, HttpResponseMessage resp, Exception ex, StorageCommandBase<T> cmd, OperationContext ctx)
        {
            result.HttpStatusCode = (int)resp.StatusCode;

            if (operation.OperationType == TableOperationType.Retrieve)
            {
                if (resp.StatusCode != HttpStatusCode.OK && resp.StatusCode != HttpStatusCode.NotFound)
                {
                    throw new StorageException(cmd.CurrentResult, string.Format(SR.UnexpectedResponseCode, HttpStatusCode.OK.ToString() + " or " + HttpStatusCode.NotFound.ToString(), resp.StatusCode.ToString()), null);
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

            if (resp.Headers.ETag != null)
            {
                result.Etag = resp.Headers.ETag.ToString();
                if (operation.Entity != null)
                {
                    operation.Entity.ETag = result.Etag;
                }
            }

            return result;
        }

        internal static async Task<TableResult> TableOperationPostProcess(TableResult result, TableOperation operation, RESTCommand<TableResult> cmd, HttpResponseMessage resp, OperationContext ctx, TableRequestOptions options)
        {
            if (operation.OperationType != TableOperationType.Retrieve && operation.OperationType != TableOperationType.Insert)
            {
                result.Etag = resp.Headers.ETag.ToString();
                operation.Entity.ETag = result.Etag;
            }
            else if (operation.OperationType == TableOperationType.Insert && (!operation.EchoContent))
            {
                if (resp.Headers.ETag != null)
                {
                    result.Etag = resp.Headers.ETag.ToString();
                    operation.Entity.ETag = result.Etag;
                    operation.Entity.Timestamp = ParseETagForTimestamp(result.Etag);
                }
            }
            else
            {
                // Parse entity
                IEnumerable<string> contentType;
                resp.Content.Headers.TryGetValues(Constants.HeaderConstants.PayloadContentTypeHeader, out contentType);

                if (contentType != null && contentType.FirstOrDefault() != null && contentType.FirstOrDefault().Contains(Constants.JsonContentTypeHeaderValue) &&
                        contentType.FirstOrDefault().Contains(Constants.NoMetadata))
                {
                    IEnumerable<string> etag;
                    resp.Headers.TryGetValues(Constants.HeaderConstants.EtagHeader, out etag);
                    if (etag != null)
                    {
                        result.Etag = etag.FirstOrDefault();
                    }

                    await ReadEntityUsingJsonParserAsync(result, operation, cmd.ResponseStream, ctx, options, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    await ReadOdataEntityAsync(result, operation, cmd.ResponseStream, ctx, options, CancellationToken.None).ConfigureAwait(false);
                }
            }

            return result;
        }

        internal static async Task<IList<TableResult>> TableBatchOperationPostProcess(IList<TableResult> result, TableBatchOperation batch, RESTCommand<IList<TableResult>> cmd, HttpResponseMessage resp, OperationContext ctx, TableRequestOptions options, CancellationToken cancellationToken)
        {
            Stream responseStream = cmd.ResponseStream;
            StreamReader streamReader = new StreamReader(responseStream);
            string currentLine = await streamReader.ReadLineAsync().ConfigureAwait(false);
            currentLine = await streamReader.ReadLineAsync().ConfigureAwait(false);

            int index = 0;
            bool failError = false;
            bool failUnexpected = false;

            while ((currentLine != null) && !(currentLine.StartsWith(@"--batchresponse")))
                //while (reader.State == ODataBatchReaderState.Operation)
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

                // Validate Status Code 
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
                    else
                    {
                        cmd.CurrentResult.ExtendedErrorInformation = StorageExtendedErrorInformation.ReadFromStream(bodyStream);
                    }

                    cmd.CurrentResult.HttpStatusCode = statusCode;
                    if (!string.IsNullOrEmpty(cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage))
                    {
                        string msg = cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage;
                        cmd.CurrentResult.HttpStatusMessage = msg.Substring(0, msg.IndexOf("\n"));
                    }
                    else
                    {
                        cmd.CurrentResult.HttpStatusMessage = statusCode.ToString();
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
                        cmd.CurrentResult.HttpStatusMessage = msg.Substring(0, msg.IndexOf("\n"));
                    }
                    else
                    {
                        cmd.CurrentResult.HttpStatusMessage = statusCode.ToString(CultureInfo.InvariantCulture);
                    }

                    string indexString = Convert.ToString(index);

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

                    throw new StorageException(cmd.CurrentResult, string.Format(SR.BatchErrorInOperation, indexString), null) { IsRetryable = true };
                }

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
                        await ReadOdataEntityAsync(currentResult, currentOperation, bodyStream, ctx, options, cancellationToken).ConfigureAwait(false);
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

        internal static async Task<ResultSegment<TElement>> TableQueryPostProcessGeneric<TElement>(Stream responseStream, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, HttpResponseMessage resp, TableRequestOptions options, OperationContext ctx, string accountName)
        {
            ResultSegment<TElement> retSeg = new ResultSegment<TElement>(new List<TElement>());
            retSeg.ContinuationToken = ContinuationFromResponse(resp);
            IEnumerable<string> contentType;
            IEnumerable<string> eTag;

            resp.Content.Headers.TryGetValues(Constants.ContentTypeElement, out contentType);
            resp.Headers.TryGetValues(Constants.EtagElement, out eTag);

            string ContentType = contentType != null ? contentType.FirstOrDefault() : null;
            string ETag = eTag != null ? eTag.FirstOrDefault() : null;
            if (ContentType.Contains(Constants.JsonContentTypeHeaderValue) && ContentType.Contains(Constants.NoMetadata))
            {
                await ReadQueryResponseUsingJsonParserAsync(retSeg, responseStream, ETag, resolver, options.PropertyResolver, typeof(TElement), null, options, System.Threading.CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                List<KeyValuePair<string, Dictionary<string, object>>> results = await ReadQueryResponseUsingJsonParserMetadataAsync(responseStream, System.Threading.CancellationToken.None).ConfigureAwait(false);

                foreach (KeyValuePair<string, Dictionary<string, object>> kvp in results)
                {
                    retSeg.Results.Add(ReadAndResolve(kvp.Key, kvp.Value, resolver, options));
                }

                Logger.LogInformational(ctx, SR.RetrieveWithContinuationToken, retSeg.Results.Count, retSeg.ContinuationToken); 
            }

            return retSeg;
        }

        public static async Task ReadQueryResponseUsingJsonParserAsync<TElement>(ResultSegment<TElement> retSeg, Stream responseStream, string etag, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, Func<string, string, string, string, EdmType> propertyResolver, Type type, OperationContext ctx, TableRequestOptions options, System.Threading.CancellationToken cancellationToken)
        {
            StreamReader streamReader = new StreamReader(responseStream);

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

                    retSeg.Results.Add(ReadAndResolveWithEdmTypeResolver(properties, resolver, propertyResolver, etag, type, ctx));
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
            Dictionary<string, object> properties = token.ToObject<Dictionary<string, object>>();

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
                    case @"Edm.DateTime":
                        properties[propName] = DateTime.Parse((string)properties[propName], null, DateTimeStyles.AdjustToUniversal);
                        break;
                    case @"Edm.Binary":
                        properties[propName] = Convert.FromBase64String((string)properties[propName]);
                        break;
                    case @"Edm.Guid":
                        properties[propName] = Guid.Parse((string)properties[propName]);
                        break;
                    case @"Edm.Int64":
                        properties[propName] = Int64.Parse((string)properties[propName], CultureInfo.InvariantCulture);
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
        internal static TableContinuationToken ContinuationFromResponse(HttpResponseMessage response)
        {
            IEnumerable<string> nextPartitionKey;
            IEnumerable<string> nextRowKey;
            IEnumerable<string> nextTableName;

            response.Headers.TryGetValues(
                TableConstants.TableServicePrefixForTableContinuation + TableConstants.TableServiceNextPartitionKey,
                out nextPartitionKey);
            response.Headers.TryGetValues(
                TableConstants.TableServicePrefixForTableContinuation + TableConstants.TableServiceNextRowKey,
                out nextRowKey);
            response.Headers.TryGetValues(
                TableConstants.TableServicePrefixForTableContinuation + TableConstants.TableServiceNextTableName,
                out nextTableName);

            if (nextPartitionKey == null && nextRowKey == null && nextTableName == null)
            {
                return null;
            }

            TableContinuationToken newContinuationToken = new TableContinuationToken()
            {
                NextPartitionKey = nextPartitionKey != null ? nextPartitionKey.FirstOrDefault() : null,
                NextRowKey = nextRowKey != null ? nextRowKey.FirstOrDefault() : null,
                NextTableName = nextTableName != null ? nextTableName.FirstOrDefault() : null
            };

            return newContinuationToken;
        }

        private static async Task ReadOdataEntityAsync(TableResult result, TableOperation operation, Stream responseStream, OperationContext ctx, TableRequestOptions options, System.Threading.CancellationToken cancellationToken)
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
                    result.Result = ReadAndResolveWithEdmTypeResolver(properties, operation.RetrieveResolver, options.PropertyResolver, result.Etag, operation.PropertyResolverType, ctx);
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
       
        internal static void ReadAndUpdateTableEntityWithEdmTypeResolver(ITableEntity entity, Dictionary<string, string> entityAttributes, EntityReadFlags flags, Func<string, string, string, string, EdmType> propertyResolver, OperationContext ctx)
        {
            Dictionary<string, EntityProperty> entityProperties = (flags & EntityReadFlags.Properties) > 0 ? new Dictionary<string, EntityProperty>() : null;
            Dictionary<string, EdmType> propertyResolverDictionary = null;

            if (entity.GetType() != typeof(DynamicTableEntity))
            {
                propertyResolverDictionary = TableOperationHttpResponseParsers.CreatePropertyResolverDictionary(entity.GetType());
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

        private static T ReadAndResolve<T>(string etag, Dictionary<string, object> props, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, T> resolver, TableRequestOptions options)
        {
            string pk = null;
            string rk = null;
            DateTimeOffset ts = new DateTimeOffset();
            Dictionary<string, EntityProperty> properties = new Dictionary<string, EntityProperty>();

            foreach (KeyValuePair<string, object> prop in props)
                //foreach (ODataProperty prop in entry.Properties)
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
                    ts = new DateTimeOffset((DateTime)prop.Value);
                }
                else
                {
                    properties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value));
                }
            }

            return resolver(pk, rk, ts, properties, etag);
        }

        private static T ReadAndResolveWithEdmTypeResolver<T>(Dictionary<string, string> entityAttributes, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, T> resolver, Func<string, string, string, string, EdmType> propertyResolver, string etag, Type type, OperationContext ctx)
        {
            string pk = null;
            string rk = null;
            DateTimeOffset ts = new DateTimeOffset();
            Dictionary<string, EntityProperty> properties = new Dictionary<string, EntityProperty>();
            Dictionary<string, EdmType> propertyResolverDictionary = null;

            if (type != null)
            {
                propertyResolverDictionary = TableOperationHttpResponseParsers.CreatePropertyResolverDictionary(type);
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
                                properties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, edmType));
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
                            properties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, edmType));
                        }
                    }
                    else
                    {
                        Logger.LogVerbose(ctx, SR.NoPropertyResolverAvailable);
                        properties.Add(prop.Key, EntityProperty.CreateEntityPropertyFromObject(prop.Value, EdmType.String));
                    }
                }
            }

            return resolver(pk, rk, ts, properties, etag);
        }
        
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
            IEnumerable<PropertyInfo> objectProperties = type.GetRuntimeProperties();

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
