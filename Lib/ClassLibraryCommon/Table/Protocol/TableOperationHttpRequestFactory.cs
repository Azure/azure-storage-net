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
    using Microsoft.Data.OData;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;

    internal static class TableOperationHttpWebRequestFactory
    {
        internal static HttpWebRequest BuildRequestCore(Uri uri, UriQueryBuilder builder, string method, int? timeout, bool useVersionHeader, OperationContext ctx)
        {
            HttpWebRequest msg = HttpWebRequestFactory.CreateWebRequest(method, uri, timeout, builder, useVersionHeader, ctx);

            msg.Headers.Add("Accept-Charset", "UTF-8");
            msg.Headers.Add("MaxDataServiceVersion", "3.0;NetFx");

            return msg;
        }

        internal static HttpWebRequest BuildRequestForTableQuery(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext ctx, TablePayloadFormat payloadFormat)
        {
            HttpWebRequest msg = BuildRequestCore(uri, builder, "GET", timeout, useVersionHeader, ctx);
            SetAcceptHeaderForHttpWebRequest(msg, payloadFormat);
            Logger.LogInformational(ctx, SR.PayloadFormat, payloadFormat);
            return msg;
        }

        internal static Tuple<HttpWebRequest, Stream> BuildRequestForTableOperation(Uri uri, UriQueryBuilder builder, IBufferManager bufferManager, int? timeout, TableOperation operation, bool useVersionHeader, OperationContext ctx, TablePayloadFormat payloadFormat, string accountName)
        {
            HttpWebRequest msg = BuildRequestCore(uri, builder, operation.HttpMethod, timeout, useVersionHeader, ctx);

            // Set Accept and Content-Type based on the payload format.
            SetAcceptHeaderForHttpWebRequest(msg, payloadFormat);
            Logger.LogInformational(ctx, SR.PayloadFormat, payloadFormat);

            if (operation.HttpMethod != "HEAD" && operation.HttpMethod != "GET")
            {
                SetContentTypeForHttpWebRequest(msg, payloadFormat);
            }

            if (operation.OperationType == TableOperationType.InsertOrMerge || operation.OperationType == TableOperationType.Merge)
            {
                // post tunnelling
                msg.Headers.Add("X-HTTP-Method", "MERGE");
            }

            // etag
            if (operation.OperationType == TableOperationType.Delete ||
                operation.OperationType == TableOperationType.Replace ||
                operation.OperationType == TableOperationType.Merge)
            {
                if (operation.Entity.ETag != null)
                {
                    msg.Headers.Add("If-Match", operation.Entity.ETag);
                }
            }

            // prefer header
            if (operation.OperationType == TableOperationType.Insert)
            {
                msg.Headers.Add("Prefer", operation.EchoContent ? "return-content" : "return-no-content");
            }
            
            if (operation.OperationType == TableOperationType.Insert ||
                operation.OperationType == TableOperationType.Merge ||
                operation.OperationType == TableOperationType.InsertOrMerge ||
                operation.OperationType == TableOperationType.InsertOrReplace ||
                operation.OperationType == TableOperationType.Replace)
            {
                // create the writer, indent for readability of the examples.  
                ODataMessageWriterSettings writerSettings = new ODataMessageWriterSettings()
                {
                    CheckCharacters = false,   // sets this flag on the XmlWriter for ATOM  
                    Version = TableConstants.ODataProtocolVersion // set the Odata version to use when writing the entry 
                };

                HttpWebRequestAdapterMessage adapterMsg = new HttpWebRequestAdapterMessage(msg, bufferManager);

                if (operation.HttpMethod != "HEAD" && operation.HttpMethod != "GET")
                {
                    SetContentTypeForAdapterMessage(adapterMsg, payloadFormat);
                }
                
                ODataMessageWriter odataWriter = new ODataMessageWriter(adapterMsg, writerSettings, new TableStorageModel(accountName));
                ODataWriter writer = odataWriter.CreateODataEntryWriter();
                WriteOdataEntity(operation.Entity, operation.OperationType, ctx, writer);

                return new Tuple<HttpWebRequest, Stream>(adapterMsg.GetPopulatedMessage(), adapterMsg.GetStream());
            }

            return new Tuple<HttpWebRequest, Stream>(msg, null);
        }

        internal static Tuple<HttpWebRequest, Stream> BuildRequestForTableBatchOperation(Uri uri, UriQueryBuilder builder, IBufferManager bufferManager, int? timeout, string tableName, TableBatchOperation batch, bool useVersionHeader, OperationContext ctx, TablePayloadFormat payloadFormat, string accountName)
        {
            HttpWebRequest msg = BuildRequestCore(NavigationHelper.AppendPathToSingleUri(uri, "$batch"), builder, "POST", timeout, useVersionHeader, ctx);
            Logger.LogInformational(ctx, SR.PayloadFormat, payloadFormat);

            // create the writer, indent for readability of the examples.  
            ODataMessageWriterSettings writerSettings = new ODataMessageWriterSettings()
            {
                CheckCharacters = false,   // sets this flag on the XmlWriter for ATOM  
                Version = TableConstants.ODataProtocolVersion // set the Odata version to use when writing the entry 
            };

            HttpWebRequestAdapterMessage adapterMsg = new HttpWebRequestAdapterMessage(msg, bufferManager);

            ODataMessageWriter odataWriter = new ODataMessageWriter(adapterMsg, writerSettings);

            ODataBatchWriter batchWriter = odataWriter.CreateODataBatchWriter();
            batchWriter.WriteStartBatch();

            bool isQuery = batch.Count == 1 && batch[0].OperationType == TableOperationType.Retrieve;

            // Query operations should not be inside changeset in payload
            if (!isQuery)
            {
                // Start Operation
                batchWriter.WriteStartChangeset();
                batchWriter.Flush();
            }

            foreach (TableOperation operation in batch)
            {
                string httpMethod = operation.OperationType == TableOperationType.Merge || operation.OperationType == TableOperationType.InsertOrMerge ? "MERGE" : operation.HttpMethod;

                ODataBatchOperationRequestMessage mimePartMsg = batchWriter.CreateOperationRequestMessage(httpMethod, operation.GenerateRequestURI(uri, tableName));
                SetAcceptAndContentTypeForODataBatchMessage(mimePartMsg, payloadFormat);

                // etag
                if (operation.OperationType == TableOperationType.Delete ||
                    operation.OperationType == TableOperationType.Replace ||
                    operation.OperationType == TableOperationType.Merge)
                {
                    mimePartMsg.SetHeader("If-Match", operation.Entity.ETag);
                }

                // Prefer header
                if (operation.OperationType == TableOperationType.Insert)
                {
                    mimePartMsg.SetHeader("Prefer", operation.EchoContent ? "return-content" : "return-no-content");
                }
                
                if (operation.OperationType != TableOperationType.Delete && operation.OperationType != TableOperationType.Retrieve)
                {
                    using (ODataMessageWriter batchEntryWriter = new ODataMessageWriter(mimePartMsg, writerSettings, new TableStorageModel(accountName)))
                    {
                        // Write entity
                        ODataWriter entryWriter = batchEntryWriter.CreateODataEntryWriter();
                        WriteOdataEntity(operation.Entity, operation.OperationType, ctx, entryWriter);
                    }
                }
            }

            if (!isQuery)
            {
                // End Operation
                batchWriter.WriteEndChangeset();
            }

            // End Batch
            batchWriter.WriteEndBatch();
            batchWriter.Flush();

            return new Tuple<HttpWebRequest, Stream>(adapterMsg.GetPopulatedMessage(), adapterMsg.GetStream());
        }

        private static void WriteOdataEntity(ITableEntity entity, TableOperationType operationType, OperationContext ctx, ODataWriter writer)
        {
            ODataEntry entry = new ODataEntry()
            {
                Properties = GetPropertiesWithKeys(entity, ctx, operationType),
                TypeName = "account.sometype"
            };

            entry.SetAnnotation(new SerializationTypeNameAnnotation { TypeName = null });
            writer.WriteStart(entry);
            writer.WriteEnd();
            writer.Flush();
        }

        #region TableEntity Serialization Helpers

        internal static IEnumerable<ODataProperty> GetPropertiesFromDictionary(IDictionary<string, EntityProperty> properties)
        {
            return properties.Select(kvp => new ODataProperty() { Name = kvp.Key, Value = kvp.Value.PropertyAsObject });
        }

        internal static IEnumerable<ODataProperty> GetPropertiesWithKeys(ITableEntity entity, OperationContext operationContext, TableOperationType operationType)
        {
            if (operationType == TableOperationType.Insert)
            {
                if (entity.PartitionKey != null)
                {
                    yield return new ODataProperty() { Name = TableConstants.PartitionKey, Value = entity.PartitionKey };
                }

                if (entity.RowKey != null)
                {
                    yield return new ODataProperty() { Name = TableConstants.RowKey, Value = entity.RowKey };
                }
            }

            foreach (ODataProperty property in GetPropertiesFromDictionary(entity.WriteEntity(operationContext)))
            {
                yield return property;
            }
        }
        #endregion

        #region Set Headers

        private static void SetAcceptHeaderForHttpWebRequest(HttpWebRequest msg, TablePayloadFormat payloadFormat)
        {
            if (payloadFormat == TablePayloadFormat.AtomPub)
            {
                msg.Accept = Constants.AtomAcceptHeaderValue;
            }
            else if (payloadFormat == TablePayloadFormat.JsonFullMetadata)
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

        private static void SetContentTypeForHttpWebRequest(HttpWebRequest msg, TablePayloadFormat payloadFormat)
        {
            if (payloadFormat == TablePayloadFormat.AtomPub)
            {
                msg.ContentType = Constants.AtomContentTypeHeaderValue;
            }
            else
            {
                msg.ContentType = Constants.JsonContentTypeHeaderValue;
            }
        }

        private static void SetContentTypeForAdapterMessage(HttpWebRequestAdapterMessage adapterMsg, TablePayloadFormat payloadFormat)
        {
            if (payloadFormat == TablePayloadFormat.AtomPub)
            {
                adapterMsg.SetHeader(Constants.HeaderConstants.PayloadContentTypeHeader, Constants.AtomContentTypeHeaderValue);
            }
            else
            {
                adapterMsg.SetHeader(Constants.HeaderConstants.PayloadContentTypeHeader, Constants.JsonContentTypeHeaderValue);
            }
        }

        private static void SetAcceptAndContentTypeForODataBatchMessage(ODataBatchOperationRequestMessage mimePartMsg, TablePayloadFormat payloadFormat)
        {
            if (payloadFormat == TablePayloadFormat.AtomPub)
            {
                mimePartMsg.SetHeader(Constants.HeaderConstants.PayloadAcceptHeader, Constants.AtomAcceptHeaderValue);
                mimePartMsg.SetHeader(Constants.HeaderConstants.PayloadContentTypeHeader, Constants.AtomContentTypeHeaderValue);
            }
            else if (payloadFormat == TablePayloadFormat.JsonFullMetadata)
            {
                mimePartMsg.SetHeader(Constants.HeaderConstants.PayloadAcceptHeader, Constants.JsonFullMetadataAcceptHeaderValue);
                mimePartMsg.SetHeader(Constants.HeaderConstants.PayloadContentTypeHeader, Constants.JsonContentTypeHeaderValue);
            }
            else if (payloadFormat == TablePayloadFormat.Json)
            {
                mimePartMsg.SetHeader(Constants.HeaderConstants.PayloadAcceptHeader, Constants.JsonLightAcceptHeaderValue);
                mimePartMsg.SetHeader(Constants.HeaderConstants.PayloadContentTypeHeader, Constants.JsonContentTypeHeaderValue);
            }
            else
            {
                mimePartMsg.SetHeader(Constants.HeaderConstants.PayloadAcceptHeader, Constants.JsonNoMetadataAcceptHeaderValue);
                mimePartMsg.SetHeader(Constants.HeaderConstants.PayloadContentTypeHeader, Constants.JsonContentTypeHeaderValue);
            }
        }
        #endregion
    }
}
