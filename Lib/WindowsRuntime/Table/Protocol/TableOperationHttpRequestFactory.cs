// -----------------------------------------------------------------------------------------
// <copyright file="TableOperationHttpRequestFactory.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Table.Protocol
{
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Auth;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;

    internal class TableOperationHttpRequestMessageFactory
    {
        internal static StorageRequestMessage BuildRequestCore(Uri uri, UriQueryBuilder builder, HttpMethod method, int? timeout, HttpContent content, OperationContext ctx, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage msg = HttpRequestMessageFactory.CreateRequestMessage(method, uri, timeout, builder, content, ctx, canonicalizer, credentials);

            msg.Headers.Add(Constants.HeaderConstants.AcceptCharset, Constants.HeaderConstants.AcceptCharsetValue);
            msg.Headers.Add(Constants.HeaderConstants.MaxDataServiceVersion, Constants.HeaderConstants.MaxDataServiceVersionValue);

            return msg;
        }

        internal static StorageRequestMessage BuildRequestForTableQuery(Uri uri, UriQueryBuilder builder, int? timeout, HttpContent content, OperationContext ctx, TablePayloadFormat payloadFormat, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage msg = BuildRequestCore(uri, builder, HttpMethod.Get, timeout, content, ctx, canonicalizer, credentials);

            // Set Accept and Content-Type based on the payload format.
            SetAcceptHeaderForHttpWebRequest(msg, payloadFormat);
            Logger.LogInformational(ctx, SR.PayloadFormat, payloadFormat);
            return msg;
        }

        internal static StorageRequestMessage BuildRequestForTableOperation<T>(RESTCommand<T> cmd, Uri uri, UriQueryBuilder builder, int? timeout, TableOperation operation, CloudTableClient client, HttpContent content, OperationContext ctx, TablePayloadFormat payloadFormat, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage msg = BuildRequestCore(uri, builder, operation.HttpMethod, timeout, content, ctx, canonicalizer, credentials);

            // Set Accept and Content-Type based on the payload format.
            SetAcceptHeaderForHttpWebRequest(msg, payloadFormat);
            Logger.LogInformational(ctx, SR.PayloadFormat, payloadFormat);

            msg.Headers.Add(Constants.HeaderConstants.DataServiceVersion, Constants.HeaderConstants.DataServiceVersionValue);

            if (operation.OperationType == TableOperationType.InsertOrMerge || operation.OperationType == TableOperationType.Merge)
            {
                // post tunnelling
                msg.Headers.Add(Constants.HeaderConstants.PostTunnelling, "MERGE");
            }

            // etag
            if (operation.OperationType == TableOperationType.Delete ||
                operation.OperationType == TableOperationType.Replace ||
                operation.OperationType == TableOperationType.Merge)
            {
                msg.Headers.Add(Constants.HeaderConstants.IfMatch, operation.ETag);
            }

            // Prefer header
            if (operation.OperationType == TableOperationType.Insert)
            {
                msg.Headers.Add(Constants.HeaderConstants.Prefer, operation.EchoContent ? Constants.HeaderConstants.PreferReturnContent : Constants.HeaderConstants.PreferReturnNoContent);
            }

            if (operation.OperationType == TableOperationType.Insert ||
                operation.OperationType == TableOperationType.Merge ||
                operation.OperationType == TableOperationType.InsertOrMerge ||
                operation.OperationType == TableOperationType.InsertOrReplace ||
                operation.OperationType == TableOperationType.Replace)
            {

                MultiBufferMemoryStream ms = new MultiBufferMemoryStream(client.BufferManager);
                using (JsonTextWriter jsonWriter = new JsonTextWriter(new StreamWriter(new NonCloseableStream(ms))))
                {
                    WriteEntityContent(operation, ctx, jsonWriter);
                }

                ms.Seek(0, SeekOrigin.Begin);
                msg.Content = new StreamContent(ms);
                msg.Content.Headers.ContentLength = ms.Length;
                if (!operation.HttpMethod.Equals("HEAD") && !operation.HttpMethod.Equals("GET"))
                {
                    SetContentTypeForHttpWebRequest(msg);
                }
                return msg;
            }

            return msg;
        }

        private static void WriteEntityContent(TableOperation operation, OperationContext ctx, JsonTextWriter jsonWriter)
        {
            ITableEntity entityToWrite = operation.Entity;
            Dictionary<string, object> propertyDictionary = new Dictionary<string, object>();


            foreach (KeyValuePair<string, object> kvp in GetPropertiesWithKeys(entityToWrite, ctx, operation.OperationType))
            {
                if (kvp.Value == null)
                {
                    continue;
                }

                if (kvp.Value.GetType() == typeof(DateTime))
                {
                    propertyDictionary[kvp.Key] = ((DateTime)kvp.Value).ToUniversalTime().ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                    propertyDictionary[kvp.Key + Constants.OdataTypeString] = Constants.EdmDateTime;
                    continue;
                }

                if (kvp.Value.GetType() == typeof(byte[]))
                {
                    propertyDictionary[kvp.Key] = Convert.ToBase64String((byte[])kvp.Value);
                    propertyDictionary[kvp.Key + Constants.OdataTypeString] = Constants.EdmBinary;
                    continue;
                }

                if (kvp.Value.GetType() == typeof(Int64))
                {
                    propertyDictionary[kvp.Key] = kvp.Value.ToString();
                    propertyDictionary[kvp.Key + Constants.OdataTypeString] = Constants.EdmInt64;
                    continue;
                }

                if (kvp.Value.GetType() == typeof(Guid))
                {
                    propertyDictionary[kvp.Key] = kvp.Value.ToString();
                    propertyDictionary[kvp.Key + Constants.OdataTypeString] = Constants.EdmGuid;
                    continue;
                }

                propertyDictionary[kvp.Key] = kvp.Value;
            }

            JObject json = JObject.FromObject(propertyDictionary);

            json.WriteTo(jsonWriter);
        }


        internal static StorageRequestMessage BuildRequestForTableBatchOperation<T>(RESTCommand<T> cmd, Uri uri, UriQueryBuilder builder, int? timeout, string tableName, TableBatchOperation batch, CloudTableClient client, HttpContent content, OperationContext ctx, TablePayloadFormat payloadFormat, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage msg = BuildRequestCore(NavigationHelper.AppendPathToSingleUri(uri, "$batch"), builder, HttpMethod.Post, timeout, content, ctx, canonicalizer, credentials);
            Logger.LogInformational(ctx, SR.PayloadFormat, payloadFormat);



            MultiBufferMemoryStream batchContentStream = new MultiBufferMemoryStream(client.BufferManager);

            string batchID = Guid.NewGuid().ToString();
            string changesetID = Guid.NewGuid().ToString();
            using (StreamWriter contentWriter = new StreamWriter(new NonCloseableStream(batchContentStream)))
            {

                msg.Headers.Add(Constants.HeaderConstants.DataServiceVersion, Constants.HeaderConstants.DataServiceVersionValue);

                string batchSeparator = Constants.BatchSeparator + batchID;
                string changesetSeparator = Constants.ChangesetSeparator + changesetID;
                string acceptHeader = "Accept: ";

                switch (payloadFormat)
                {
                    case TablePayloadFormat.Json:
                        acceptHeader = acceptHeader + Constants.JsonLightAcceptHeaderValue;
                        break;
                    case TablePayloadFormat.JsonFullMetadata:
                        acceptHeader = acceptHeader + Constants.JsonFullMetadataAcceptHeaderValue;
                        break;
                    case TablePayloadFormat.JsonNoMetadata:
                        acceptHeader = acceptHeader + Constants.JsonNoMetadataAcceptHeaderValue;
                        break;
                }

                contentWriter.WriteLine(batchSeparator);

                bool isQuery = batch.Count == 1 && batch[0].OperationType == TableOperationType.Retrieve;

                // Query operations should not be inside changeset in payload
                if (!isQuery)
                {
                    // Start Operation
                    contentWriter.WriteLine(Constants.ChangesetBoundaryMarker + changesetID);
                    contentWriter.WriteLine();
                }

                foreach (TableOperation operation in batch)
                {
                    string httpMethod = operation.OperationType == TableOperationType.Merge || operation.OperationType == TableOperationType.InsertOrMerge ? "MERGE" : operation.HttpMethod.Method;

                    if (!isQuery)
                    {
                        contentWriter.WriteLine(changesetSeparator);
                    }

                    contentWriter.WriteLine(Constants.ContentTypeApplicationHttp);
                    contentWriter.WriteLine(Constants.ContentTransferEncodingBinary);
                    contentWriter.WriteLine();

                    string tableURI = Uri.EscapeUriString(operation.GenerateRequestURI(uri, tableName).ToString());

                    // "EscapeUriString" is almost exactly what we need, except that it contains special logic for 
                    // the percent sign, which results in an off-by-one error in the number of times "%" is encoded.
                    // This corrects for that.
                    tableURI = tableURI.Replace(@"%25", @"%");

                    contentWriter.WriteLine(httpMethod + " " + tableURI + " " + Constants.HTTP1_1);
                    contentWriter.WriteLine(acceptHeader);
                    contentWriter.WriteLine(Constants.ContentTypeApplicationJson);

                    if (operation.OperationType == TableOperationType.Insert)
                    {
                        contentWriter.WriteLine(Constants.HeaderConstants.Prefer + @": " + (operation.EchoContent ? Constants.HeaderConstants.PreferReturnContent : Constants.HeaderConstants.PreferReturnNoContent));
                    }

                    contentWriter.WriteLine(Constants.HeaderConstants.DataServiceVersion + ": " + Constants.HeaderConstants.DataServiceVersionValue);

                    // etag
                    if (operation.OperationType == TableOperationType.Delete ||
                        operation.OperationType == TableOperationType.Replace ||
                        operation.OperationType == TableOperationType.Merge)
                    {
                        contentWriter.WriteLine(Constants.HeaderConstants.IfMatch + @": " + operation.ETag);
                    }

                    contentWriter.WriteLine();

                    if (operation.OperationType != TableOperationType.Delete && operation.OperationType != TableOperationType.Retrieve)
                    {
                        using (JsonTextWriter jsonWriter = new JsonTextWriter(contentWriter))
                        {
                            jsonWriter.CloseOutput = false;
                            WriteEntityContent(operation, ctx, jsonWriter);
                        }
                        contentWriter.WriteLine();
                   }
                }

                if (!isQuery)
                {
                    contentWriter.WriteLine(changesetSeparator + "--");
                }

                contentWriter.WriteLine(batchSeparator + "--");
            }

            batchContentStream.Seek(0, SeekOrigin.Begin);
            msg.Content = new StreamContent(batchContentStream);
            msg.Content.Headers.ContentLength = batchContentStream.Length;
            msg.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(Constants.BatchBoundaryMarker + batchID);//new System.Net.Http.Headers.MediaTypeHeaderValue(Constants.BatchBoundaryMarker + batchID);

            return msg;
        }

        #region TableEntity Serialization Helpers

        internal static IEnumerable<KeyValuePair<string, object>> GetPropertiesFromDictionary(IDictionary<string, EntityProperty> properties)
        {
            return properties.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value.PropertyAsObject));
        }

        internal static IEnumerable<KeyValuePair<string, object>> GetPropertiesWithKeys(ITableEntity entity, OperationContext operationContext, TableOperationType operationType)
        {
            List<KeyValuePair<string, object>> retProps = GetPropertiesFromDictionary(entity.WriteEntity(operationContext)).ToList();
            if (operationType == TableOperationType.Insert)
            {
                if (entity.PartitionKey != null)
                {
                    retProps.Add(new KeyValuePair<string, object>(TableConstants.PartitionKey, entity.PartitionKey));
                }

                if (entity.RowKey != null)
                {
                    retProps.Add(new KeyValuePair<string, object>(TableConstants.RowKey, entity.RowKey));
                }
            }

            return retProps;
        }
        #endregion

        #region Set Headers
        private static void SetAcceptHeaderForHttpWebRequest(StorageRequestMessage msg, TablePayloadFormat payloadFormat)
        {
            if (payloadFormat == TablePayloadFormat.JsonFullMetadata)
            {
                msg.Headers.Add(Constants.HeaderConstants.PayloadAcceptHeader, Constants.JsonFullMetadataAcceptHeaderValue);
            }
            else if (payloadFormat == TablePayloadFormat.Json)
            {
                msg.Headers.Add(Constants.HeaderConstants.PayloadAcceptHeader, Constants.JsonLightAcceptHeaderValue);
            }
            else if (payloadFormat == TablePayloadFormat.JsonNoMetadata)
            {
                msg.Headers.Add(Constants.HeaderConstants.PayloadAcceptHeader, Constants.JsonNoMetadataAcceptHeaderValue);
            }
        }

        private static void SetContentTypeForHttpWebRequest(StorageRequestMessage msg)
        {
            if (msg.Content == null || msg.Content.Headers == null)
            {
                return;
            }

            msg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Constants.JsonContentTypeHeaderValue);
        }
        #endregion
    }
}
