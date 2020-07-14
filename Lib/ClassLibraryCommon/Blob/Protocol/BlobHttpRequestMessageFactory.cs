// -----------------------------------------------------------------------------------------
// <copyright file="BlobHttpRequestMessageFactory.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Blob.Protocol
{
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Auth.Protocol;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Auth;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    internal static class BlobHttpRequestMessageFactory
    {
        /// <summary>
        /// Constructs a web request to commit a block to an append blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static StorageRequestMessage AppendBlock(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext,
            ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "appendblock");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            request.ApplyAppendCondition(accessCondition);
            BlobRequest.ApplyCustomerProvidedKeyOrEncryptionScope(request, options, isSource: false);
            return request;
        }

        /// <summary>
        /// Constructs a web request to commit a block to an append blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="sourceContentChecksum">The checksum calculated for the range of bytes of the source.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="sourceAccessCondition">The source access condition to apply to the request.</param>
        /// <param name="destAccessCondition">The destination access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static StorageRequestMessage AppendBlock(Uri uri, Uri sourceUri, long? offset, long? count, Checksum sourceContentChecksum, int? timeout, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "appendblock");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            request.ApplyAccessConditionToSource(sourceAccessCondition);
            request.ApplyAccessCondition(destAccessCondition);
            request.ApplyAppendCondition(destAccessCondition);

            AddCopySource(request, sourceUri);
            AddSourceRange(request, offset, count);

            request.ApplySourceContentChecksumHeaders(sourceContentChecksum);

            BlobRequest.ApplyCustomerProvidedKeyOrEncryptionScope(request, options, isSource: false);

            return request;
        }

        /// <summary>
        /// Constructs a web request to create a new block blob or page blob, or to update the content 
        /// of an existing block blob. 
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="properties">The properties to set for the blob.</param>
        /// <param name="blobType">The type of the blob.</param>
        /// <param name="pageBlobSize">For a page blob, the size of the blob. This parameter is ignored
        /// for block blobs.</param>
        /// <param name="pageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> containing blob request options</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Put(Uri uri, int? timeout, BlobProperties properties, BlobType blobType, long pageBlobSize, PremiumPageBlobTier? pageBlobTier, 
            AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("properties", properties);

            if (blobType == BlobType.Unspecified)
            {
                throw new InvalidOperationException(SR.UndefinedBlobType);
            }

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, null /* builder */, content, operationContext, canonicalizer, credentials);

            if (properties.CacheControl != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.BlobCacheControlHeader, properties.CacheControl);
            }

            if (properties.ContentType != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentTypeHeader, properties.ContentType);
            }

            if (properties.ContentLanguage != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentLanguageHeader, properties.ContentLanguage);
            }

            if (properties.ContentEncoding != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentEncodingHeader, properties.ContentEncoding);
            }

            if (properties.ContentDisposition != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentDispositionRequestHeader, properties.ContentDisposition);
            }

            request.ApplyBlobContentChecksumHeaders(properties.ContentChecksum);

            if (blobType == BlobType.PageBlob)
            {
                request.Headers.Add(Constants.HeaderConstants.BlobType, Constants.HeaderConstants.PageBlob);
                request.Headers.Add(Constants.HeaderConstants.BlobContentLengthHeader, pageBlobSize.ToString(NumberFormatInfo.InvariantInfo));
                properties.Length = pageBlobSize;

                if (pageBlobTier.HasValue)
                {
                    request.Headers.Add(Constants.HeaderConstants.AccessTierHeader, pageBlobTier.Value.ToString());
                }
            }
            else if (blobType == BlobType.BlockBlob)
            {
                request.Headers.Add(Constants.HeaderConstants.BlobType, Constants.HeaderConstants.BlockBlob);
            }
            else 
            {
                request.Headers.Add(Constants.HeaderConstants.BlobType, Constants.HeaderConstants.AppendBlob);
            }

            request.ApplyAccessCondition(accessCondition);
            BlobRequest.ApplyCustomerProvidedKeyOrEncryptionScope(request, options, isSource: false);
            return request;
        }

        /// <summary>
        /// Adds the snapshot.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="snapshot">The snapshot version, if the blob is a snapshot.</param>
        private static void AddSnapshot(UriQueryBuilder builder, DateTimeOffset? snapshot)
        {
            if (snapshot.HasValue)
            {
                builder.Add("snapshot", Request.ConvertDateTimeToSnapshotString(snapshot.Value));
            }
        }

        /// <summary>
        /// Constructs a web request to return the list of valid page ranges for a page blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="snapshot">The snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="count">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage GetPageRanges(Uri uri, int? timeout, DateTimeOffset? snapshot, long? offset, long? count, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (offset.HasValue)
            {
                CommonUtility.AssertNotNull("count", count);
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "pagelist");
            BlobHttpRequestMessageFactory.AddSnapshot(builder, snapshot);

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            AddRange(request, offset, count);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the list of page ranges that differ between a specified snapshot and this object.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="snapshot">The snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="previousSnapshotTime">A <see cref="DateTimeOffset"/> representing the snapshot timestamp to use as the starting point for the diff. If this CloudPageBlob represents a snapshot, the previousSnapshotTime parameter must be prior to the current snapshot timestamp.</param>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="count">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage GetPageRangesDiff(Uri uri, int? timeout, DateTimeOffset? snapshot, DateTimeOffset previousSnapshotTime, long? offset, long? count, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (offset.HasValue)
            {
                CommonUtility.AssertNotNull("count", count);
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "pagelist");
            BlobHttpRequestMessageFactory.AddSnapshot(builder, snapshot);
            builder.Add("prevsnapshot", Request.ConvertDateTimeToSnapshotString(previousSnapshotTime));

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            AddRange(request, offset, count);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Adds the Copy Source Header for Blob Service Operations.
        /// </summary>
        /// <param name="request">The <see cref="StorageRequestMessage"/> to add the copy source header to.</param>
        /// <param name="sourceUri">URI of the source</param>
        private static void AddCopySource(StorageRequestMessage request, Uri sourceUri)
        {
            request.Headers.Add(Constants.HeaderConstants.CopySourceHeader, sourceUri.AbsoluteUri);
        }

        /// <summary>
        /// Adds the Range Header for Blob Service Operations.
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="offset">Starting byte of the range</param>
        /// <param name="count">Number of bytes in the range</param>
        private static void AddRange(StorageRequestMessage request, long? offset, long? count)
        {
            AddRangeImpl(Constants.HeaderConstants.RangeHeader, request, offset, count);
        }

        /// <summary>
        /// Adds the Source Range Header for Blob Service Operations.
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="offset">Starting byte of the range</param>
        /// <param name="count">Number of bytes in the range</param>
        private static void AddSourceRange(StorageRequestMessage request, long? offset, long? count)
        {
            AddRangeImpl(Constants.HeaderConstants.SourceRangeHeader, request, offset, count);
        }

        /// <summary>
        /// Adds the Range Header for Blob Service Operations.
        /// </summary>
        /// <param name="header">Name of the header</param>
        /// <param name="request">Request</param>
        /// <param name="offset">Starting byte of the range</param>
        /// <param name="count">Number of bytes in the range</param>
        private static void AddRangeImpl(string header, StorageRequestMessage request, long? offset, long? count)
        {
            if (count.HasValue)
            {
                CommonUtility.AssertNotNull("offset", offset);
                CommonUtility.AssertInBounds("count", count.Value, 1, long.MaxValue);
            }

            if (offset.HasValue)
            {
                string rangeStart = offset.ToString();
                string rangeEnd = string.Empty;
                if (count.HasValue)
                {
                    rangeEnd = (offset + count.Value - 1).ToString();
                }

                string rangeHeaderValue = string.Format(CultureInfo.InvariantCulture, Constants.HeaderConstants.RangeHeaderFormat, rangeStart, rangeEnd);
                request.Headers.Add(header, rangeHeaderValue);
            }
        }

        /// <summary>
        /// Constructs a web request to return the blob's system properties.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="snapshot">The snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> containing blob request options</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage GetProperties(Uri uri, int? timeout, DateTimeOffset? snapshot, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, 
            ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            BlobHttpRequestMessageFactory.AddSnapshot(builder, snapshot);

            StorageRequestMessage request = HttpRequestMessageFactory.GetProperties(uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);

            BlobRequest.ApplyCustomerProvidedKey(request, options?.CustomerProvidedKey, isSource: false);

            return request;
        }

        /// <summary>
        /// Constructs a web request to set system properties for a blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="properties">The blob's properties.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage SetProperties(Uri uri, int? timeout, BlobProperties properties, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, 
            ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            CommonUtility.AssertNotNull("properties", properties);

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            if (properties != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.BlobCacheControlHeader, properties.CacheControl);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentDispositionRequestHeader, properties.ContentDisposition);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentEncodingHeader, properties.ContentEncoding);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentLanguageHeader, properties.ContentLanguage);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentTypeHeader, properties.ContentType); 
                request.ApplyBlobContentChecksumHeaders(properties.ContentChecksum);
            }

            request.ApplyAccessCondition(accessCondition);

            return request;
        }

        /// <summary>
        /// Constructs a web request to get a user delegation key for user-delegation-based SAS.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="content">The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A <see cref="StorageRequestMessage"/> object.</returns>
        public static StorageRequestMessage GetUserDelegationKey(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.ResourceType, "service");
            builder.Add(Constants.QueryConstants.Component, "userdelegationkey");
            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Post, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to resize a page blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="newBlobSize">The new blob size, if the blob is a page blob. Set this parameter to <c>null</c> to keep the existing blob size.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Resize(Uri uri, int? timeout, long newBlobSize, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            request.Headers.Add(Constants.HeaderConstants.BlobContentLengthHeader, newBlobSize.ToString(NumberFormatInfo.InvariantInfo));

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set a page blob's sequence number.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="sequenceNumberAction">A value of type <see cref="SequenceNumberAction"/>, indicating the operation to perform on the sequence number.</param>
        /// <param name="sequenceNumber">The sequence number. Set this parameter to <c>null</c> if this operation is an increment action.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage SetSequenceNumber(Uri uri, int? timeout, SequenceNumberAction sequenceNumberAction, long? sequenceNumber, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            CommonUtility.AssertInBounds("sequenceNumberAction", sequenceNumberAction, SequenceNumberAction.Max, SequenceNumberAction.Increment);
            if (sequenceNumberAction == SequenceNumberAction.Increment)
            {
                if (sequenceNumber.HasValue)
                {
                    throw new ArgumentException(SR.BlobInvalidSequenceNumber, "sequenceNumber");
                }
            }
            else
            {
                CommonUtility.AssertNotNull("sequenceNumber", sequenceNumber);
                CommonUtility.AssertInBounds("sequenceNumber", sequenceNumber.Value, 0);
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            request.Headers.Add(Constants.HeaderConstants.SequenceNumberAction, sequenceNumberAction.ToString());
            if (sequenceNumberAction != SequenceNumberAction.Increment)
            {
                request.Headers.Add(Constants.HeaderConstants.BlobSequenceNumber, sequenceNumber.Value.ToString(CultureInfo.InvariantCulture));
            }

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the user-defined metadata for the blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="snapshot">The snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object containing blob request options</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage GetMetadata(Uri uri, int? timeout, DateTimeOffset? snapshot, AccessCondition accessCondition, HttpContent content, 
            OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            BlobHttpRequestMessageFactory.AddSnapshot(builder, snapshot);

            StorageRequestMessage request = HttpRequestMessageFactory.GetMetadata(uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);

            BlobRequest.ApplyCustomerProvidedKey(request, options?.CustomerProvidedKey, isSource: false);

            return request;
        }

        /// <summary>
        /// Constructs a web request to set user-defined metadata for the blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, 
            ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            StorageRequestMessage request = HttpRequestMessageFactory.SetMetadata(uri, timeout, null /* builder */, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            BlobRequest.ApplyCustomerProvidedKeyOrEncryptionScope(request, options, isSource: false);
            return request;
        }

        /// <summary>
        /// Adds user-defined metadata to the request as one or more name-value pairs.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <param name="metadata">The user-defined metadata.</param>
        public static void AddMetadata(StorageRequestMessage request, IDictionary<string, string> metadata)
        {
            HttpRequestMessageFactory.AddMetadata(request, metadata);
        }

        /// <summary>
        /// Adds user-defined metadata to the request as a single name-value pair.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <param name="name">The metadata name.</param>
        /// <param name="value">The metadata value.</param>
        public static void AddMetadata(StorageRequestMessage request, string name, string value)
        {
            HttpRequestMessageFactory.AddMetadata(request, name, value);
        }

        /// <summary>
        /// Constructs a web request to delete a blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="snapshot">The snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to delete only blobs, only snapshots, or both.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Delete(Uri uri, int? timeout, DateTimeOffset? snapshot, DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if ((snapshot != null) && (deleteSnapshotsOption != DeleteSnapshotsOption.None))
            {
                throw new InvalidOperationException(string.Format(SR.DeleteSnapshotsNotValidError, "deleteSnapshotsOption", "snapshot"));
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            BlobHttpRequestMessageFactory.AddSnapshot(builder, snapshot);

            StorageRequestMessage request = HttpRequestMessageFactory.Delete(uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            switch (deleteSnapshotsOption)
            {
                case DeleteSnapshotsOption.None:
                    break; // nop

                case DeleteSnapshotsOption.IncludeSnapshots:
                    request.Headers.Add(
                        Constants.HeaderConstants.DeleteSnapshotHeader,
                        Constants.HeaderConstants.IncludeSnapshotsValue);
                    break;

                case DeleteSnapshotsOption.DeleteSnapshotsOnly:
                    request.Headers.Add(
                        Constants.HeaderConstants.DeleteSnapshotHeader,
                        Constants.HeaderConstants.SnapshotsOnlyValue);
                    break;
            }

            request.ApplyAccessCondition(accessCondition);

            return request;
        }

        /// <summary>
        /// Constructs a web request to undelete a soft-deleted blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Undelete(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            StorageRequestMessage request = HttpRequestMessageFactory.Undelete(uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to create a snapshot of a blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Snapshot(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, 
            ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "snapshot");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            BlobRequest.ApplyCustomerProvidedKeyOrEncryptionScope(request, options, isSource: false);
            return request;
        }

        /// <summary>
        /// Generates a web request to use to acquire, renew, change, release or break the lease for the blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="action">The lease action to perform.</param>
        /// <param name="proposedLeaseId">A lease ID to propose for the result of an acquire or change operation,
        /// or null if no ID is proposed for an acquire operation. This should be null for renew, release, and break operations.</param>
        /// <param name="leaseDuration">The lease duration, in seconds, for acquire operations.
        /// If this is -1 then an infinite duration is specified. This should be null for renew, change, release, and break operations.</param>
        /// <param name="leaseBreakPeriod">The amount of time to wait, in seconds, after a break operation before the lease is broken.
        /// If this is null then the default time is used. This should be null for acquire, renew, change, and release operations.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Lease(Uri uri, int? timeout, LeaseAction action, string proposedLeaseId, int? leaseDuration, int? leaseBreakPeriod, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "lease");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);

            // Add lease headers
            BlobHttpRequestMessageFactory.AddLeaseAction(request, action);
            BlobHttpRequestMessageFactory.AddLeaseDuration(request, leaseDuration);
            BlobHttpRequestMessageFactory.AddProposedLeaseId(request, proposedLeaseId);
            BlobHttpRequestMessageFactory.AddLeaseBreakPeriod(request, leaseBreakPeriod);

            return request;
        }

        /// <summary>
        /// Adds a proposed lease id to a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="proposedLeaseId">The proposed lease id.</param>
        internal static void AddProposedLeaseId(StorageRequestMessage request, string proposedLeaseId)
        {
            request.AddOptionalHeader(Constants.HeaderConstants.ProposedLeaseIdHeader, proposedLeaseId);
        }

        /// <summary>
        /// Adds a lease duration to a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="leaseDuration">The lease duration.</param>
        internal static void AddLeaseDuration(StorageRequestMessage request, int? leaseDuration)
        {
            request.AddOptionalHeader(Constants.HeaderConstants.LeaseDurationHeader, leaseDuration);
        }

        /// <summary>
        /// Adds a lease break period to a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="leaseBreakPeriod">The lease break period.</param>
        internal static void AddLeaseBreakPeriod(StorageRequestMessage request, int? leaseBreakPeriod)
        {
            request.AddOptionalHeader(Constants.HeaderConstants.LeaseBreakPeriodHeader, leaseBreakPeriod);
        }

        /// <summary>
        /// Adds a lease action to a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="leaseAction">The lease action.</param>
        internal static void AddLeaseAction(StorageRequestMessage request, LeaseAction leaseAction)
        {
            request.Headers.Add(Constants.HeaderConstants.LeaseActionHeader, leaseAction.ToString().ToLower());
        }

        /// <summary>
        /// Constructs a web request to write a block to a block blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="blockId">The block ID for this block.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content">The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object containing blob request options</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage PutBlock(Uri uri, int? timeout, string blockId, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, 
            ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "block");
            builder.Add("blockid", blockId);

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyLeaseId(accessCondition);
            BlobRequest.ApplyCustomerProvidedKeyOrEncryptionScope(request, options, isSource: false);
            return request;
        }

        /// <summary>
        /// Constructs a web request to write a block to a block blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="sourceContentChecksum">The checksum calculated for the range of bytes of the source.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="blockId">A string specifying the block ID for this block.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="content">The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object containing blob request options</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage PutBlock(Uri uri, Uri sourceUri, long? offset, long? count, Checksum sourceContentChecksum, int? timeout, string blockId, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            if (offset.HasValue && offset.Value < 0)
            {
                CommonUtility.ArgumentOutOfRange("offset", offset);
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "block");
            builder.Add("blockid", blockId);

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyLeaseId(accessCondition);

            AddCopySource(request, sourceUri);
            AddSourceRange(request, offset, count);

            request.ApplySourceContentChecksumHeaders(sourceContentChecksum);

            BlobRequest.ApplyCustomerProvidedKeyOrEncryptionScope(request, options, isSource: false);

            return request;
        }

        /// <summary>
        /// Constructs a web request to create or update a blob by committing a block list.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="properties">The properties to set for the blob.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object containing blob request options</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage PutBlockList(Uri uri, int? timeout, BlobProperties properties, AccessCondition accessCondition, HttpContent content,
            OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("properties", properties);

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "blocklist");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            if (properties != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.BlobCacheControlHeader, properties.CacheControl);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentTypeHeader, properties.ContentType);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentLanguageHeader, properties.ContentLanguage);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentEncodingHeader, properties.ContentEncoding);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentDispositionRequestHeader, properties.ContentDisposition);
                request.ApplyBlobContentChecksumHeaders(properties.ContentChecksum);
            }

            request.ApplyAccessCondition(accessCondition);

            BlobRequest.ApplyCustomerProvidedKeyOrEncryptionScope(request, options, isSource: false);

            return request;
        }

        /// <summary>
        /// Constructs a web request to return the list of blocks for a block blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="snapshot">The snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="typesOfBlocks">The types of blocks to include in the list: committed, uncommitted, or both.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage GetBlockList(Uri uri, int? timeout, DateTimeOffset? snapshot, BlockListingFilter typesOfBlocks, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "blocklist");
            builder.Add("blocklisttype", typesOfBlocks.ToString());
            BlobHttpRequestMessageFactory.AddSnapshot(builder, snapshot);

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to write or clear a range of pages in a page blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object containing blob request options.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage PutPage(Uri uri, int? timeout, PageRange pageRange, PageWrite pageWrite, AccessCondition accessCondition, HttpContent content, 
            OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "page");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            request.Headers.Add(Constants.HeaderConstants.RangeHeader, pageRange.ToString());
            request.Headers.Add(Constants.HeaderConstants.PageWrite, pageWrite.ToString());

            request.ApplyAccessCondition(accessCondition);
            request.ApplySequenceNumberCondition(accessCondition);
            BlobRequest.ApplyCustomerProvidedKeyOrEncryptionScope(request, options, isSource: false);
            return request;
        }

        /// <summary>
        /// Constructs a web request to write or clear a range of pages in a page blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="sourceContentChecksum">The checksum calculated for the range of bytes of the source.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="pageRange"></param>
        /// <param name="sourceAccessCondition">The source access condition to apply to the request.</param>
        /// <param name="destAccessCondition">The destination access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object containing blob request options</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage PutPage(Uri uri, Uri sourceUri, long? offset, long? count, Checksum sourceContentChecksum, int? timeout, PageRange pageRange, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "page");
            builder.Add("update", default(string));

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            request.Headers.Add(Constants.HeaderConstants.RangeHeader, pageRange.ToString());
            request.Headers.Add(Constants.HeaderConstants.PageWrite, "update");

            request.ApplyAccessConditionToSource(sourceAccessCondition);
            request.ApplyAccessCondition(destAccessCondition);
            request.ApplySequenceNumberCondition(destAccessCondition);

            AddCopySource(request, sourceUri);
            AddSourceRange(request, offset, count);

            request.ApplySourceContentChecksumHeaders(sourceContentChecksum);

            BlobRequest.ApplyCustomerProvidedKeyOrEncryptionScope(request, options, isSource: false);

            return request;
        }

        /// <summary>
        /// Generates a web request to copy a blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the destination blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="source">The absolute URI to the source blob, including any necessary authentication parameters.</param>
        /// <param name="incrementalCopy">A boolean indicating whether or not this is an incremental copy.</param>
        /// <param name="sourceAccessCondition">The access condition to apply to the source blob.</param>
        /// <param name="destAccessCondition">The access condition to apply to the destination blob.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage CopyFrom(Uri uri, int? timeout, Uri source, bool incrementalCopy, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            return BlobHttpRequestMessageFactory.CopyFrom(uri, timeout, source, incrementalCopy, default(PremiumPageBlobTier?) /* premiumPageBlobTier */, default(StandardBlobTier) /*standardBlockBlobTier*/, default(RehydratePriority?) /*rehydratePriority */, sourceAccessCondition, destAccessCondition, content, operationContext, canonicalizer, credentials);
        }

        /// <summary>
        /// Generates a web request to copy a blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the destination blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="source">The absolute URI to the source blob, including any necessary authentication parameters.</param>
        /// <param name="incrementalCopy">A boolean indicating whether or not this is an incremental copy.</param>
        /// <param name="premiumPageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set. Only valid on page blobs.</param>
        /// <param name="standardBlockBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set. Only valid on block blobs.</param>
        /// <param name="rehydratePriority">The priority with which to rehydrate an archived blob.</param>
        /// <param name="sourceAccessCondition">The access condition to apply to the source blob.</param>
        /// <param name="destAccessCondition">The access condition to apply to the destination blob.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage CopyFrom(Uri uri, int? timeout, Uri source, bool incrementalCopy, PremiumPageBlobTier? premiumPageBlobTier, StandardBlobTier? standardBlockBlobTier, RehydratePriority? rehydratePriority, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            return CopyFrom(uri, timeout, source, new Checksum(md5: default(string), crc64: default(string)), incrementalCopy, false /* syncCopy */, premiumPageBlobTier,  standardBlockBlobTier, rehydratePriority, sourceAccessCondition, destAccessCondition, content, operationContext, canonicalizer, credentials);
        }

        /// <summary>
        /// Generates a web request to copy a blob or file to another blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the destination blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="source">A <see cref="System.Uri"/> specifying the absolute URI to the source object, including any necessary authentication parameters.</param>
        /// <param name="sourceContentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="incrementalCopy">A boolean indicating whether or not this is an incremental copy.</param>
        /// <param name="syncCopy">A boolean to enable synchronous server copy of blobs.</param>
        /// <param name="premiumPageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="standardBlockBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="rehydratePriority">The priority with which to rehydrate an archived blob.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met on the source object in order for the request to proceed.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met on the destination blob in order for the request to proceed.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage CopyFrom(Uri uri, int? timeout, Uri source, Checksum sourceContentChecksum, bool incrementalCopy, bool syncCopy, PremiumPageBlobTier? premiumPageBlobTier, StandardBlobTier? standardBlockBlobTier, RehydratePriority? rehydratePriority, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (!syncCopy && sourceContentChecksum.HasAny)
            {
                throw new InvalidOperationException();
            }

            UriQueryBuilder builder = null;
            if (incrementalCopy)
            {
                builder = new UriQueryBuilder();
                builder.Add(Constants.QueryConstants.Component, "incrementalcopy");
            }

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            request.Headers.Add(Constants.HeaderConstants.CopySourceHeader, source.AbsoluteUri);

            request.ApplyAccessCondition(destAccessCondition);
            request.ApplyAccessConditionToSource(sourceAccessCondition);

            if (premiumPageBlobTier.HasValue && standardBlockBlobTier.HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(standardBlockBlobTier), "Cannot specify both page and block tiers at the same time.");
            }

            if (rehydratePriority.HasValue)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.RehydratePriorityHeader, rehydratePriority.Value.ToString());
            }

            if (premiumPageBlobTier.HasValue)
            {
                request.Headers.Add(Constants.HeaderConstants.AccessTierHeader, premiumPageBlobTier.Value.ToString());
            }
            else if (standardBlockBlobTier.HasValue)
            {
                request.Headers.Add(Constants.HeaderConstants.AccessTierHeader, standardBlockBlobTier.Value.ToString());
            }

            if (syncCopy)
            {
                request.Headers.Add(Constants.HeaderConstants.RequiresSyncHeader, Constants.HeaderConstants.TrueHeader);
            }

            request.ApplySourceContentChecksumHeaders(sourceContentChecksum);

            return request;
        }

        /// <summary>
        /// Generates a web request to abort a copy operation.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="copyId">The ID string of the copy operation to be aborted.</param>
        /// <param name="accessCondition">The access condition to apply to the request.
        ///     Only lease conditions are supported for this operation.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage AbortCopy(Uri uri, int? timeout, string copyId, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "copy");
            builder.Add(Constants.QueryConstants.CopyId, copyId);

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            request.Headers.Add(Constants.HeaderConstants.CopyActionHeader, Constants.HeaderConstants.CopyActionAbort);
            request.ApplyAccessCondition(accessCondition);

            return request;
        }

        /// <summary>
        /// Constructs a web request to get the blob's content, properties, and metadata.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="snapshot">The snapshot version, if the blob is a snapshot.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage Get(Uri uri, int? timeout, DateTimeOffset? snapshot, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            if (snapshot.HasValue)
            {
                builder.Add("snapshot", Request.ConvertDateTimeToSnapshotString(snapshot.Value));
            }

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return a specified range of the blob's content, together with its properties and metadata.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="snapshot">The snapshot version, if the blob is a snapshot.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or null to return all bytes through the end of the blob.</param>
        /// <param name="rangeContentChecksumRequested">Indicates which checksum headers are requested for the specified range.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object containing blob request options</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Get(Uri uri, int? timeout, DateTimeOffset? snapshot, long? offset, long? count, ChecksumRequested rangeContentChecksumRequested, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials, BlobRequestOptions options)
        {
            if (offset.HasValue && offset.Value < 0)
            {
                CommonUtility.ArgumentOutOfRange("offset", offset);
            }

            rangeContentChecksumRequested.AssertInBounds(offset, count, Constants.MaxRangeGetContentMD5Size, Constants.MaxRangeGetContentCRC64Size);

            StorageRequestMessage request = Get(uri, timeout, snapshot, accessCondition, content, operationContext, canonicalizer, credentials);
            AddRange(request, offset, count);

            request.ApplyRangeContentChecksumRequested(offset, rangeContentChecksumRequested);

            BlobRequest.ApplyCustomerProvidedKey(request, options?.CustomerProvidedKey, isSource: false);

            return request;
        }

        /// <summary>
        /// Creates a web request to get the properties of the Blob service account.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Blob service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A StorageRequestMessage to get the account properties.</returns>
        public static StorageRequestMessage GetAccountProperties(Uri uri, UriQueryBuilder builder, int? timeout, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            return HttpRequestMessageFactory.GetAccountProperties(uri, builder, timeout, content, operationContext, canonicalizer, credentials);
        }

        /// <summary>
        /// Constructs a web request to get the properties of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A StorageRequestMessage to get the service properties.</returns>
        public static StorageRequestMessage GetServiceProperties(Uri uri, int? timeout, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            return HttpRequestMessageFactory.GetServiceProperties(uri, timeout, operationContext, canonicalizer, credentials);
        }

         /// <summary>
        /// Creates a web request to set the properties of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="content"> The HTTP entity body and content headers.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to set the service properties.</returns>
        internal static StorageRequestMessage SetServiceProperties(Uri uri, int? timeout, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            return HttpRequestMessageFactory.SetServiceProperties(uri, timeout, content, operationContext, canonicalizer, credentials);   
        }

        /// <summary>
        /// Constructs a web request to get the stats of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A StorageRequestMessage to get the service stats.</returns>
        public static StorageRequestMessage GetServiceStats(Uri uri, int? timeout, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            return HttpRequestMessageFactory.GetServiceStats(uri, timeout, operationContext, canonicalizer, credentials);
        }

        /// <summary>
        /// Constructs a web request to set the tier on a page blob.
        /// </summary>
        /// <param name="uri">The absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="blobTier">The blob tier to set.</param>
        /// <param name="rehydratePriority">The priority with which to rehydrate an archived blob.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage SetBlobTier(Uri uri, int? timeout, string blobTier, RehydratePriority? rehydratePriority, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "tier");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.Headers.Add(Constants.HeaderConstants.AccessTierHeader, blobTier);

            if (rehydratePriority.HasValue)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.RehydratePriorityHeader, rehydratePriority.Value.ToString());
            }

            return request;
        }

        public static StorageRequestMessage PrepareBatchRequest(Uri uri, IBufferManager bufferManager, int? timeout, BatchOperation batchOperation, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "batch");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Post, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            return request;
        }

        public static HttpContent WriteBatchBody(CloudBlobClient client, RESTCommand<IList<BlobBatchSubOperationResponse>> cmd, BatchOperation batchOperation, OperationContext operationContext)
        {
            var boundary = Constants.BatchPrefix + batchOperation.BatchID;
            var multipartContent = new MultipartContent("mixed", boundary);

            // HACK remove quotes until server recognizes quoted boundaries
            multipartContent.Headers.ContentType = MediaTypeHeaderValue.Parse($"multipart/mixed; boundary={boundary}");

            var contentID = 0;

            var requests = batchOperation.Operations.Select(
                command =>
                command.BuildRequest(
                    command,
                    command.StorageUri.PrimaryUri,
                    new UriQueryBuilder() /*??*/,
                    command.BuildContent != null ? command.BuildContent(command, operationContext) : null,
                    command.ServerTimeoutInSeconds,
                    operationContext
                    )
                    );

            foreach (var request in requests)
            {
                ExecutorBase.ApplyUserHeaders(operationContext, request);

                if(request.Credentials?.IsSharedKey == true)
                {
                    StorageAuthenticationHttpHandler.AddDateHeader(request);
                    StorageAuthenticationHttpHandler.AddSharedKeyAuth(request);
                }
                else if(request.Credentials?.IsSAS == true)
                {
                    request.RequestUri = request.Credentials.TransformUri(request.RequestUri);
                }
                else if (request.Credentials?.IsToken == true)
                {
                    StorageAuthenticationHttpHandler.AddTokenAuth(request);
                }

                request.Headers.Remove(Constants.HeaderConstants.PrefixForStorageHeader + Constants.HeaderConstants.StorageVersionHeader);

                var sb = new StringBuilder();
                sb.AppendLine($"{request.Method} {request.RequestUri.PathAndQuery} HTTP/1.1");

                sb.Append(request.Headers.ToString());

                if (request.Content != null)
                {
                    sb.AppendLine();
                    sb.Append(request.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult());
                }

                var content = new StringContent(sb.ToString());
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/http");
                content.Headers.TryAddWithoutValidation("Content-Transfer-Encoding", "binary");
                content.Headers.TryAddWithoutValidation("Content-ID", (contentID++).ToString(CultureInfo.InvariantCulture));

                multipartContent.Add(content);
            }

            return multipartContent;
        }
    }

    internal static class BlobRequestMessageExtensions
    {
        internal static void ApplyBlobContentChecksumHeaders(this StorageRequestMessage request, Checksum blobContentChecksum)
        {
            request.AddOptionalHeader(Constants.HeaderConstants.BlobContentMD5Header, blobContentChecksum?.MD5);
            request.AddOptionalHeader(Constants.HeaderConstants.BlobContentCRC64Header, blobContentChecksum?.CRC64);
        }
    }
}
