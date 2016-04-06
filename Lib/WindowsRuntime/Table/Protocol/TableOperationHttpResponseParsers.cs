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
    using Microsoft.Data.OData;
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

        internal static Task<TableResult> TableOperationPostProcess(TableResult result, TableOperation operation, RESTCommand<TableResult> cmd, HttpResponseMessage resp, OperationContext ctx, TableRequestOptions options, string accountName)
        {
            return Task.Run(() =>
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
                    ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings();
                    readerSettings.MessageQuotas = new ODataMessageQuotas() { MaxPartsPerBatch = TableConstants.TableServiceMaxResults, MaxReceivedMessageSize = TableConstants.TableServiceMaxPayload };
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

                        ReadEntityUsingJsonParser(result, operation, cmd.ResponseStream, ctx, options);
                    }
                    else
                    {
                        ReadOdataEntity(result, operation, new HttpResponseAdapterMessage(resp, cmd.ResponseStream), ctx, readerSettings, accountName);
                    }
                }

                return result;
            });
        }

        internal static Task<IList<TableResult>> TableBatchOperationPostProcess(IList<TableResult> result, TableBatchOperation batch, RESTCommand<IList<TableResult>> cmd, HttpResponseMessage resp, OperationContext ctx, TableRequestOptions options, string accountName)
        {
            return Task.Run(() =>
            {
                ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings();
                readerSettings.MessageQuotas = new ODataMessageQuotas() { MaxPartsPerBatch = TableConstants.TableServiceMaxResults, MaxReceivedMessageSize = TableConstants.TableServiceMaxPayload };

                using (ODataMessageReader responseReader = new ODataMessageReader(new HttpResponseAdapterMessage(resp, cmd.ResponseStream), readerSettings))
                {
                    // create a reader
                    ODataBatchReader reader = responseReader.CreateODataBatchReader();

                    // Initial => changesetstart 
                    if (reader.State == ODataBatchReaderState.Initial)
                    {
                        reader.Read();
                    }

                    if (reader.State == ODataBatchReaderState.ChangesetStart)
                    {
                        // ChangeSetStart => Operation
                        reader.Read();
                    }

                    int index = 0;
                    bool failError = false;
                    bool failUnexpected = false;

                    while (reader.State == ODataBatchReaderState.Operation)
                    {
                        TableOperation currentOperation = batch[index];
                        TableResult currentResult = new TableResult() { Result = currentOperation.Entity };
                        result.Add(currentResult);

                        ODataBatchOperationResponseMessage mimePartResponseMessage = reader.CreateOperationResponseMessage();
                        string contentType = mimePartResponseMessage.GetHeader(Constants.ContentTypeElement);

                        currentResult.HttpStatusCode = mimePartResponseMessage.StatusCode;

                        // Validate Status Code 
                        if (currentOperation.OperationType == TableOperationType.Insert)
                        {
                            failError = mimePartResponseMessage.StatusCode == (int)HttpStatusCode.Conflict;
                            if (currentOperation.EchoContent)
                            {
                                failUnexpected = mimePartResponseMessage.StatusCode != (int)HttpStatusCode.Created;
                            }
                            else
                            {
                                failUnexpected = mimePartResponseMessage.StatusCode != (int)HttpStatusCode.NoContent;
                            }
                        }
                        else if (currentOperation.OperationType == TableOperationType.Retrieve)
                        {
                            if (mimePartResponseMessage.StatusCode == (int)HttpStatusCode.NotFound)
                            {
                                index++;

                                // Operation => next
                                reader.Read();
                                continue;
                            }

                            failUnexpected = mimePartResponseMessage.StatusCode != (int)HttpStatusCode.OK;
                        }
                        else
                        {
                            failError = mimePartResponseMessage.StatusCode == (int)HttpStatusCode.NotFound;
                            failUnexpected = mimePartResponseMessage.StatusCode != (int)HttpStatusCode.NoContent;
                        }

                        if (failError)
                        {
                            // If the parse error is null, then don't get the extended error information and the StorageException will contain SR.ExtendedErrorUnavailable message.
                            if (cmd.ParseError != null)
                            {
                                cmd.CurrentResult.ExtendedErrorInformation = cmd.ParseError(mimePartResponseMessage.GetStream(), resp, contentType);
                            }
                            else
                            {
                                cmd.CurrentResult.ExtendedErrorInformation = StorageExtendedErrorInformation.ReadFromStream(mimePartResponseMessage.GetStream());
                            }

                            cmd.CurrentResult.HttpStatusCode = mimePartResponseMessage.StatusCode;
                            if (!string.IsNullOrEmpty(cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage))
                            {
                                string msg = cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage;
                                cmd.CurrentResult.HttpStatusMessage = msg.Substring(0, msg.IndexOf("\n"));
                            }
                            else
                            {
                                cmd.CurrentResult.HttpStatusMessage = mimePartResponseMessage.StatusCode.ToString();
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
                                cmd.CurrentResult.ExtendedErrorInformation = cmd.ParseError(mimePartResponseMessage.GetStream(), resp, contentType);
                            }

                            cmd.CurrentResult.HttpStatusCode = mimePartResponseMessage.StatusCode;
                            if (!string.IsNullOrEmpty(cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage))
                            {
                                string msg = cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage;
                                cmd.CurrentResult.HttpStatusMessage = msg.Substring(0, msg.IndexOf("\n"));
                            }
                            else
                            {
                                cmd.CurrentResult.HttpStatusMessage = mimePartResponseMessage.StatusCode.ToString();
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

                        // Update etag
                        if (!string.IsNullOrEmpty(mimePartResponseMessage.GetHeader("ETag")))
                        {
                            currentResult.Etag = mimePartResponseMessage.GetHeader("ETag");

                            if (currentOperation.Entity != null)
                            {
                                currentOperation.Entity.ETag = currentResult.Etag;
                            }
                        }

                        // Parse Entity if needed
                        if (currentOperation.OperationType == TableOperationType.Retrieve || (currentOperation.OperationType == TableOperationType.Insert && currentOperation.EchoContent))
                        {
                            if (mimePartResponseMessage.GetHeader(Constants.ContentTypeElement).Contains(Constants.JsonContentTypeHeaderValue) &&
                                mimePartResponseMessage.GetHeader(Constants.ContentTypeElement).Contains(Constants.NoMetadata))
                            {
                                ReadEntityUsingJsonParser(currentResult, currentOperation, mimePartResponseMessage.GetStream(), ctx, options);
                            }
                            else
                            {
                                ReadOdataEntity(currentResult, currentOperation, mimePartResponseMessage, ctx, readerSettings, accountName);
                            }
                        }
                        else if (currentOperation.OperationType == TableOperationType.Insert)
                        {
                            currentOperation.Entity.Timestamp = ParseETagForTimestamp(currentResult.Etag);
                        }

                        index++;

                        // Operation =>
                        reader.Read();
                    }
                }

                return result;
            });
        }

        internal static Task<ResultSegment<TElement>> TableQueryPostProcessGeneric<TElement>(Stream responseStream, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, HttpResponseMessage resp, TableRequestOptions options, OperationContext ctx, string accountName)
        {
            return Task.Run(() =>
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
                    ReadQueryResponseUsingJsonParser(retSeg, responseStream, ETag, resolver, options.PropertyResolver, typeof(TElement), null);
                }
                else
                {
                    ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings();
                    readerSettings.MessageQuotas = new ODataMessageQuotas() { MaxPartsPerBatch = TableConstants.TableServiceMaxResults, MaxReceivedMessageSize = TableConstants.TableServiceMaxPayload };

                    using (ODataMessageReader responseReader = new ODataMessageReader(new HttpResponseAdapterMessage(resp, responseStream), readerSettings, new TableStorageModel(accountName)))
                    {
                        // create a reader
                        ODataReader reader = responseReader.CreateODataFeedReader();

                        // Start => FeedStart
                        if (reader.State == ODataReaderState.Start)
                        {
                            reader.Read();
                        }

                        // Feedstart 
                        if (reader.State == ODataReaderState.FeedStart)
                        {
                            reader.Read();
                        }

                        while (reader.State == ODataReaderState.EntryStart)
                        {
                            // EntryStart => EntryEnd
                            reader.Read();

                            ODataEntry entry = (ODataEntry)reader.Item;

                            retSeg.Results.Add(ReadAndResolve(entry, resolver));

                            // Entry End => ?
                            reader.Read();
                        }

                        DrainODataReader(reader);
                    }
                }

                return retSeg;
            });
        }

        private static void DrainODataReader(ODataReader reader)
        {
            if (reader.State == ODataReaderState.FeedEnd)
            {
                reader.Read();
            }

            if (reader.State != ODataReaderState.Completed)
            {
                throw new InvalidOperationException(string.Format(SR.ODataReaderNotInCompletedState, reader.State));
            }
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

        private static void ReadOdataEntity(TableResult result, TableOperation operation, IODataResponseMessage respMsg, OperationContext ctx, ODataMessageReaderSettings readerSettings, string accountName)
        {
            using (ODataMessageReader messageReader = new ODataMessageReader(respMsg, readerSettings, new TableStorageModel(accountName)))
            {
                // Create a reader.
                ODataReader reader = messageReader.CreateODataEntryReader();

                while (reader.Read())
                {
                    if (reader.State == ODataReaderState.EntryEnd)
                    {
                        ODataEntry entry = (ODataEntry)reader.Item;

                        if (operation.OperationType == TableOperationType.Retrieve)
                        {
                            result.Result = ReadAndResolve(entry, operation.RetrieveResolver);
                            result.Etag = entry.ETag;
                        }
                        else
                        {
                            result.Etag = ReadAndUpdateTableEntity(operation.Entity, entry, EntityReadFlags.Timestamp | EntityReadFlags.Etag, ctx);
                        }
                    }
                }

                DrainODataReader(reader);
            }
        }

        private static void ReadQueryResponseUsingJsonParser<TElement>(ResultSegment<TElement> retSeg, Stream responseStream, string etag, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, Func<string, string, string, string, EdmType> propertyResolver, Type type, OperationContext ctx)
        {
            StreamReader streamReader = new StreamReader(responseStream);
            using (JsonReader reader = new JsonTextReader(streamReader))
            {
                reader.DateParseHandling = DateParseHandling.None;
                JObject dataSet = JObject.Load(reader);
                JToken dataTable = dataSet["value"];

                foreach (JToken token in dataTable)
                {
                    Dictionary<string, string> properties = token.ToObject<Dictionary<string, string>>();
                    retSeg.Results.Add(ReadAndResolveWithEdmTypeResolver(properties, resolver, propertyResolver, etag, type, ctx));
                }

                if (reader.Read())
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.JsonReaderNotInCompletedState));
                }
            }
        }

        private static void ReadEntityUsingJsonParser(TableResult result, TableOperation operation, Stream stream, OperationContext ctx, TableRequestOptions options)
        {
            StreamReader streamReader = new StreamReader(stream);
            using (JsonReader reader = new JsonTextReader(streamReader))
            {
                JsonSerializer serializer = new JsonSerializer();
                Dictionary<string, string> properties = serializer.Deserialize<Dictionary<string, string>>(reader);
                if (operation.OperationType == TableOperationType.Retrieve)
                {
                    result.Result = ReadAndResolveWithEdmTypeResolver(properties, operation.RetrieveResolver, options.PropertyResolver, result.Etag, operation.PropertyResolverType, ctx);
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

        private static T ReadAndResolve<T>(ODataEntry entry, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, T> resolver)
        {
            string pk = null;
            string rk = null;
            DateTimeOffset ts = new DateTimeOffset();
            Dictionary<string, EntityProperty> properties = new Dictionary<string, EntityProperty>();

            foreach (ODataProperty prop in entry.Properties)
            {
                if (prop.Name == TableConstants.PartitionKey)
                {
                    pk = (string)prop.Value;
                }
                else if (prop.Name == TableConstants.RowKey)
                {
                    rk = (string)prop.Value;
                }
                else if (prop.Name == TableConstants.Timestamp)
                {
                    ts = new DateTimeOffset((DateTime)prop.Value);
                }
                else
                {
                    properties.Add(prop.Name, EntityProperty.CreateEntityPropertyFromObject(prop.Value));
                }
            }

            return resolver(pk, rk, ts, properties, entry.ETag);
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

        // returns etag
        internal static string ReadAndUpdateTableEntity(ITableEntity entity, ODataEntry entry, EntityReadFlags flags, OperationContext ctx)
        {
            if ((flags & EntityReadFlags.Etag) > 0)
            {
                entity.ETag = entry.ETag;
            }

            Dictionary<string, EntityProperty> entityProperties = (flags & EntityReadFlags.Properties) > 0 ? new Dictionary<string, EntityProperty>() : null;

            if (flags > 0)
            {
                foreach (ODataProperty prop in entry.Properties)
                {
                    if (prop.Name == TableConstants.PartitionKey)
                    {
                        if ((flags & EntityReadFlags.PartitionKey) == 0)
                        {
                            continue;
                        }

                        entity.PartitionKey = (string)prop.Value;
                    }
                    else if (prop.Name == TableConstants.RowKey)
                    {
                        if ((flags & EntityReadFlags.RowKey) == 0)
                        {
                            continue;
                        }

                        entity.RowKey = (string)prop.Value;
                    }
                    else if (prop.Name == TableConstants.Timestamp)
                    {
                        if ((flags & EntityReadFlags.Timestamp) == 0)
                        {
                            continue;
                        }

                        entity.Timestamp = (DateTime)prop.Value;
                    }
                    else if ((flags & EntityReadFlags.Properties) > 0)
                    {
                        entityProperties.Add(prop.Name, EntityProperty.CreateEntityPropertyFromObject(prop.Value));
                    }
                }

                if ((flags & EntityReadFlags.Properties) > 0)
                {
                    entity.ReadEntity(entityProperties, ctx);
                }
            }

            return entry.ETag;
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
