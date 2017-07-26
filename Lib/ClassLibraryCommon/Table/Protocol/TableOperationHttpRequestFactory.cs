//-----------------------------------------------------------------------
// <copyright file="TableOperationHttpWebRequestFactory.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;

    internal static class TableOperationHttpWebRequestFactory
    {
        internal static HttpWebRequest BuildRequestCore(Uri uri, UriQueryBuilder builder, string method, int? timeout, bool useVersionHeader, OperationContext ctx)
        {
            HttpWebRequest msg = HttpWebRequestFactory.CreateWebRequest(method, uri, timeout, builder, useVersionHeader, ctx);

            msg.Headers.Add(Constants.HeaderConstants.AcceptCharset, Constants.HeaderConstants.AcceptCharsetValue);
            msg.Headers.Add(Constants.HeaderConstants.MaxDataServiceVersion, Constants.HeaderConstants.MaxDataServiceVersionValue);

            return msg;
        }

        internal static HttpWebRequest BuildRequestForTableQuery(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext ctx, TablePayloadFormat payloadFormat)
        {
            HttpWebRequest msg = BuildRequestCore(uri, builder, "GET", timeout, useVersionHeader, ctx);

            // Set Accept and Content-Type based on the payload format.
            SetAcceptHeaderForHttpWebRequest(msg, payloadFormat);
            Logger.LogInformational(ctx, SR.PayloadFormat, payloadFormat);
            return msg;
        }

        internal static DynamicTableEntity GetInnerMergeOperationForKeyRotationOperation(TableOperation operation, TableRequestOptions options)
        {
            DynamicTableEntity innerDTE = new DynamicTableEntity();
            innerDTE.PartitionKey = operation.PartitionKey;
            innerDTE.RowKey = operation.RowKey;
            innerDTE.ETag = operation.keyRotationEntity.ETag;
            innerDTE[Constants.EncryptionConstants.TableEncryptionKeyDetails] = new EntityProperty(CommonUtility.RunWithoutSynchronizationContext(() => TableEncryptionPolicy.RotateEncryptionHelper(operation.keyRotationEntity, options, CancellationToken.None).Result));
            return innerDTE;
        }

        internal static Tuple<HttpWebRequest, Stream> BuildRequestForTableOperation(Uri uri, UriQueryBuilder builder, IBufferManager bufferManager, int? timeout, TableOperation operation, bool useVersionHeader, OperationContext ctx, TableRequestOptions options)
        {
            HttpWebRequest msg = BuildRequestCore(uri, builder, operation.HttpMethod, timeout, useVersionHeader, ctx);

            TablePayloadFormat payloadFormat = options.PayloadFormat.Value;

            // Set Accept and Content-Type based on the payload format.
            SetAcceptHeaderForHttpWebRequest(msg, payloadFormat);
            Logger.LogInformational(ctx, SR.PayloadFormat, payloadFormat);

            msg.Headers.Add(Constants.HeaderConstants.DataServiceVersion, Constants.HeaderConstants.DataServiceVersionValue);
            if (operation.HttpMethod != "HEAD" && operation.HttpMethod != "GET")
            {
                msg.ContentType = Constants.JsonContentTypeHeaderValue;
            }

            if (operation.OperationType == TableOperationType.InsertOrMerge || operation.OperationType == TableOperationType.Merge)
            {
                // Client-side encryption is not supported on merge requests.
                // This is because we maintain the list of encrypted properties as a property on the entity, and we can't update this 
                // properly for merge operations.
                options.AssertNoEncryptionPolicyOrStrictMode();

                // post tunnelling
                msg.Headers.Add(Constants.HeaderConstants.PostTunnelling, "MERGE");
            }

            if (operation.OperationType == TableOperationType.RotateEncryptionKey)
            {
                // post tunnelling
                msg.Headers.Add(Constants.HeaderConstants.PostTunnelling, "MERGE");
            }
            
            // etag
            if (operation.OperationType == TableOperationType.Delete ||
                operation.OperationType == TableOperationType.Replace ||
                operation.OperationType == TableOperationType.Merge ||
                operation.OperationType == TableOperationType.RotateEncryptionKey)
            {
                if (operation.ETag != null)
                {
                    msg.Headers.Add(Constants.HeaderConstants.IfMatch, operation.ETag);
                }
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
                operation.OperationType == TableOperationType.Replace ||
                operation.OperationType == TableOperationType.RotateEncryptionKey)
            {
                MultiBufferMemoryStream ms = new MultiBufferMemoryStream(bufferManager);
                using (JsonTextWriter jsonWriter = new JsonTextWriter(new StreamWriter(new NonCloseableStream(ms))))
                {
                    WriteEntityContent(operation, ctx, options, jsonWriter);
                }

                ms.Seek(0, SeekOrigin.Begin);
                msg.ContentLength = ms.Length;
                return new Tuple<HttpWebRequest, Stream>(msg, ms);
            }

            return new Tuple<HttpWebRequest, Stream>(msg, null);
        }

        private static void WriteEntityContent(TableOperation operation, OperationContext ctx, TableRequestOptions options, JsonTextWriter jsonWriter)
        {
            ITableEntity entityToWrite = (operation.OperationType == TableOperationType.RotateEncryptionKey) ? GetInnerMergeOperationForKeyRotationOperation(operation, options) : operation.Entity;
            Dictionary<string, object> propertyDictionary = new Dictionary<string, object>();


            foreach (KeyValuePair<string, object> kvp in GetPropertiesWithKeys(entityToWrite, ctx, operation.OperationType, options, operation.OperationType == TableOperationType.RotateEncryptionKey))
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

        internal static Tuple<HttpWebRequest, Stream> BuildRequestForTableBatchOperation(Uri uri, UriQueryBuilder builder, IBufferManager bufferManager, int? timeout, string tableName, TableBatchOperation batch, bool useVersionHeader, OperationContext ctx, TableRequestOptions options)
        {
            HttpWebRequest msg = BuildRequestCore(NavigationHelper.AppendPathToSingleUri(uri, "$batch"), builder, "POST", timeout, useVersionHeader, ctx);
            TablePayloadFormat payloadFormat = options.PayloadFormat.Value;
            Logger.LogInformational(ctx, SR.PayloadFormat, payloadFormat);

            MultiBufferMemoryStream batchContentStream = new MultiBufferMemoryStream(bufferManager);

            using (StreamWriter contentWriter = new StreamWriter(new NonCloseableStream(batchContentStream)))
            {
                string batchID = Guid.NewGuid().ToString();
                string changesetID = Guid.NewGuid().ToString();

                msg.Headers.Add(Constants.HeaderConstants.DataServiceVersion, Constants.HeaderConstants.DataServiceVersionValue);
                msg.ContentType = Constants.BatchBoundaryMarker + batchID;

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
                    string httpMethod = operation.HttpMethod;
                    if (operation.OperationType == TableOperationType.Merge || operation.OperationType == TableOperationType.InsertOrMerge)
                    {
                        options.AssertNoEncryptionPolicyOrStrictMode();
                        httpMethod = "MERGE";
                    }

                    if (operation.OperationType == TableOperationType.RotateEncryptionKey)
                    {
                        httpMethod = "MERGE";
                    }

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

                    contentWriter.WriteLine(Constants.HeaderConstants.DataServiceVersion+ ": " + Constants.HeaderConstants.DataServiceVersionValue);

                    // etag
                    if (operation.OperationType == TableOperationType.Delete ||
                        operation.OperationType == TableOperationType.Replace ||
                        operation.OperationType == TableOperationType.Merge ||
                        operation.OperationType == TableOperationType.RotateEncryptionKey)
                    {
                        contentWriter.WriteLine(Constants.HeaderConstants.IfMatch + @": " + operation.ETag);
                    }

                    contentWriter.WriteLine();

                    if (operation.OperationType != TableOperationType.Delete && operation.OperationType != TableOperationType.Retrieve)
                    {
                        using (JsonTextWriter jsonWriter = new JsonTextWriter(contentWriter))
                        {
                            jsonWriter.CloseOutput = false;
                            WriteEntityContent(operation, ctx, options, jsonWriter);
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
            msg.ContentLength = batchContentStream.Length;

            return new Tuple<HttpWebRequest, Stream>(msg, batchContentStream);
        }
        
        #region TableEntity Serialization Helpers

        internal static IEnumerable<KeyValuePair<string, object>> GetPropertiesFromDictionary(IDictionary<string, EntityProperty> properties, TableRequestOptions options, string partitionKey, string rowKey, bool ignoreEncryption)
        {
            // Check if encryption policy is set and invoke EncryptEnity if it is set.
            if (options != null)
            {
                options.AssertPolicyIfRequired();

                if (options.EncryptionPolicy != null && !ignoreEncryption)
                {
                    properties = options.EncryptionPolicy.EncryptEntity(properties, partitionKey, rowKey, options.EncryptionResolver);
                }
            }

            return properties.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value.PropertyAsObject));
        }

        internal static IEnumerable<KeyValuePair<string, object>> GetPropertiesWithKeys(ITableEntity entity, OperationContext operationContext, TableOperationType operationType, TableRequestOptions options, bool ignoreEncryption)
        {
            if (operationType == TableOperationType.Insert)
            {
                if (entity.PartitionKey != null)
                {
                    yield return new KeyValuePair<string, object>(TableConstants.PartitionKey, entity.PartitionKey);
                }

                if (entity.RowKey != null)
                {
                    yield return new KeyValuePair<string, object>(TableConstants.RowKey, entity.RowKey);
                }
            }

            foreach (KeyValuePair<string, object> property in GetPropertiesFromDictionary(entity.WriteEntity(operationContext), options, entity.PartitionKey, entity.RowKey, ignoreEncryption))
            {
                yield return property;
            }
        }
        #endregion

        #region Set Headers
        private static void SetAcceptHeaderForHttpWebRequest(HttpWebRequest msg, TablePayloadFormat payloadFormat)
        {
            if (payloadFormat == TablePayloadFormat.JsonFullMetadata)
            {
                msg.Accept = Constants.JsonFullMetadataAcceptHeaderValue;
            }
            else if (payloadFormat == TablePayloadFormat.Json)
            {
                msg.Accept = Constants.JsonLightAcceptHeaderValue;
            }
            else if (payloadFormat == TablePayloadFormat.JsonNoMetadata)
            {
                msg.Accept = Constants.JsonNoMetadataAcceptHeaderValue;
            }
        }
        #endregion
    }
}
