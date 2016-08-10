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
    using System.Net;
    using System.Reflection;
    using System.Text;

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
                ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings();
                readerSettings.MessageQuotas = new ODataMessageQuotas() { MaxPartsPerBatch = TableConstants.TableServiceMaxResults, MaxReceivedMessageSize = TableConstants.TableServiceMaxPayload };

                if (resp.ContentType.Contains(Constants.JsonNoMetadataAcceptHeaderValue))
                {
                    result.Etag = resp.Headers[Constants.HeaderConstants.EtagHeader];
                    ReadEntityUsingJsonParser(result, operation, cmd.ResponseStream, ctx, options);
                }
                else
                {
                    ReadOdataEntity(result, operation, new HttpResponseAdapterMessage(resp, cmd.ResponseStream), ctx, readerSettings, accountName, options);
                }
            }

            return result;
        }

        internal static IList<TableResult> TableBatchOperationPostProcess(IList<TableResult> result, TableBatchOperation batch, RESTCommand<IList<TableResult>> cmd, HttpWebResponse resp, OperationContext ctx, TableRequestOptions options, string accountName)
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

                    // Validate Status Code.
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

                        cmd.CurrentResult.HttpStatusCode = mimePartResponseMessage.StatusCode;
                        if (!string.IsNullOrEmpty(cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage))
                        {
                            string msg = cmd.CurrentResult.ExtendedErrorInformation.ErrorMessage;
                            cmd.CurrentResult.HttpStatusMessage = msg.Substring(0, msg.IndexOf("\n", StringComparison.Ordinal));
                        }
                        else
                        {
                            cmd.CurrentResult.HttpStatusMessage = mimePartResponseMessage.StatusCode.ToString(CultureInfo.InvariantCulture);
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
                            cmd.CurrentResult.HttpStatusMessage = msg.Substring(0, msg.IndexOf("\n", StringComparison.Ordinal));
                        }
                        else
                        {
                            cmd.CurrentResult.HttpStatusMessage = mimePartResponseMessage.StatusCode.ToString(CultureInfo.InvariantCulture);
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
                    if (!string.IsNullOrEmpty(mimePartResponseMessage.GetHeader(Constants.HeaderConstants.EtagHeader)))
                    {
                        currentResult.Etag = mimePartResponseMessage.GetHeader(Constants.HeaderConstants.EtagHeader);

                        if (currentOperation.Entity != null)
                        {
                            currentOperation.Entity.ETag = currentResult.Etag;
                        }
                    }

                    // Parse Entity if needed
                    if (currentOperation.OperationType == TableOperationType.Retrieve || (currentOperation.OperationType == TableOperationType.Insert && currentOperation.EchoContent))
                    {
                        if (mimePartResponseMessage.GetHeader(Constants.ContentTypeElement).Contains(Constants.JsonNoMetadataAcceptHeaderValue))
                        {
                            ReadEntityUsingJsonParser(currentResult, currentOperation, mimePartResponseMessage.GetStream(), ctx, options);
                        }
                        else
                        {
                            ReadOdataEntity(currentResult, currentOperation, mimePartResponseMessage, ctx, readerSettings, accountName, options);
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
        }

        internal static ResultSegment<TElement> TableQueryPostProcessGeneric<TElement, TQueryType>(Stream responseStream, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, HttpWebResponse resp, TableRequestOptions options, OperationContext ctx, string accountName)
        {
            ResultSegment<TElement> retSeg = new ResultSegment<TElement>(new List<TElement>());
            retSeg.ContinuationToken = ContinuationFromResponse(resp);

            if (resp.ContentType.Contains(Constants.JsonNoMetadataAcceptHeaderValue))
            {
                ReadQueryResponseUsingJsonParser(retSeg, responseStream, resp.Headers[Constants.HeaderConstants.EtagHeader], resolver, options.PropertyResolver, typeof(TQueryType), null, options);
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

                        retSeg.Results.Add(ReadAndResolve(entry, resolver, options));

                        // Entry End => ?
                        reader.Read();
                    }

                    DrainODataReader(reader);
                }
            }

            Logger.LogInformational(ctx, SR.RetrieveWithContinuationToken, retSeg.Results.Count, retSeg.ContinuationToken);
            return retSeg;
        }

        private static void DrainODataReader(ODataReader reader)
        {
            if (reader.State == ODataReaderState.FeedEnd)
            {
                reader.Read();
            }

            if (reader.State != ODataReaderState.Completed)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.ODataReaderNotInCompletedState, reader.State));
            }
        }

        private static void ReadQueryResponseUsingJsonParser<TElement>(ResultSegment<TElement> retSeg, Stream responseStream, string etag, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, TElement> resolver, Func<string, string, string, string, EdmType> propertyResolver, Type type, OperationContext ctx, TableRequestOptions options)
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
                JObject dataSet = JObject.Load(reader);
                JToken dataTable = dataSet["value"];

                foreach (JToken token in dataTable)
                {
                    Dictionary<string, string> properties = token.ToObject<Dictionary<string, string>>();
                    retSeg.Results.Add(ReadAndResolveWithEdmTypeResolver(properties, resolver, propertyResolver, etag, type, ctx, disablePropertyResolverCache, options));
                }

                if (reader.Read())
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.JsonReaderNotInCompletedState));
                }
            }
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

        private static void ReadOdataEntity(TableResult result, TableOperation operation, IODataResponseMessage respMsg, OperationContext ctx, ODataMessageReaderSettings readerSettings, string accountName, TableRequestOptions options)
        {
            using (ODataMessageReader messageReader = new ODataMessageReader(respMsg, readerSettings, new TableStorageModel(accountName)))
            {
                // create a reader  
                ODataReader reader = messageReader.CreateODataEntryReader();

                while (reader.Read())
                {
                    if (reader.State == ODataReaderState.EntryEnd)
                    {
                        ODataEntry entry = (ODataEntry)reader.Item;

                        if (operation.OperationType == TableOperationType.Retrieve)
                        {
                            result.Result = ReadAndResolve(entry, operation.RetrieveResolver, options);
                            result.Etag = entry.ETag;
                        }
                        else
                        {
                            result.Etag = ReadAndUpdateTableEntity(
                                                                    operation.Entity,
                                                                    entry,
                                                                    EntityReadFlags.Timestamp | EntityReadFlags.Etag,
                                                                    ctx);
                        }
                    }
                }

                DrainODataReader(reader);
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

        private static T ReadAndResolve<T>(ODataEntry entry, Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, T> resolver, TableRequestOptions options)
        {
            string pk = null;
            string rk = null;
            byte[] cek = null;
            DateTimeOffset ts = new DateTimeOffset();
            Dictionary<string, EntityProperty> properties = new Dictionary<string, EntityProperty>();

            foreach (ODataProperty prop in entry.Properties)
            {
                string propName = prop.Name;
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

            return resolver(pk, rk, ts, properties, entry.ETag);
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
                encryptedPropertyDetailsSet = JsonConvert.DeserializeObject<HashSet<string>>(Encoding.UTF8.GetString(binaryVal, 0, binaryVal.Length));
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
