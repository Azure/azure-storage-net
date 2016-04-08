//-----------------------------------------------------------------------
// <copyright file="BlobHttpWebRequestFactory.cs" company="Microsoft">
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
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net;

    /// <summary>
    /// A factory class for constructing HTTP web requests for the Blob service.
    /// </summary>
    public static class BlobHttpWebRequestFactory
    {
        /// <summary>
        /// Creates a web request to get the properties of the Blob service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Blob service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.GetServiceProperties(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Creates a web request to get the properties of the Blob service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Blob service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        internal static HttpWebRequest GetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.GetServiceProperties(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Creates a web request to set the properties of the Blob service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Blob service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.SetServiceProperties(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Creates a web request to set the properties of the Blob service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Blob service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        internal static HttpWebRequest SetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.SetServiceProperties(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Creates a web request to get the stats of the Blob service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Blob service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetServiceStats(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.GetServiceStats(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Creates a web request to get the stats of the Blob service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Blob service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        internal static HttpWebRequest GetServiceStats(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.GetServiceStats(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Writes Blob service properties to a stream, formatted in XML.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="outputStream">The <see cref="System.IO.Stream"/> object to which the formatted properties are to be written.</param>
        public static void WriteServiceProperties(ServiceProperties properties, Stream outputStream)
        {
            CommonUtility.AssertNotNull("properties", properties);

            properties.WriteServiceProperties(outputStream);
        }

        /// <summary>
        /// Constructs a web request to create a new block blob or page blob, or to update the content 
        /// of an existing block blob. 
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="properties">A <see cref="BlobProperties"/> object.</param>
        /// <param name="blobType">A <see cref="BlobType"/> enumeration value.</param>
        /// <param name="pageBlobSize">For a page blob, the size of the blob. This parameter is ignored
        /// for block blobs.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Put(Uri uri, int? timeout, BlobProperties properties, BlobType blobType, long pageBlobSize, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.Put(uri, timeout, properties, blobType, pageBlobSize, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to create a new block blob or page blob, or to update the content 
        /// of an existing block blob. 
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="properties">A <see cref="BlobProperties"/> object.</param>
        /// <param name="blobType">A <see cref="BlobType"/> enumeration value.</param>
        /// <param name="pageBlobSize">For a page blob, the size of the blob. This parameter is ignored
        /// for block blobs.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Put(Uri uri, int? timeout, BlobProperties properties, BlobType blobType, long pageBlobSize, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("properties", properties);

            if (blobType == BlobType.Unspecified)
            {
                throw new InvalidOperationException(SR.UndefinedBlobType);
            }

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, null /* builder */, useVersionHeader, operationContext);

            if (properties.CacheControl != null)
            {
                request.Headers[HttpRequestHeader.CacheControl] = properties.CacheControl;
            }

            if (properties.ContentType != null)
            {
                // Setting it using Headers is an exception
                request.ContentType = properties.ContentType;
            }

            if (properties.ContentMD5 != null)
            {
                request.Headers[HttpRequestHeader.ContentMd5] = properties.ContentMD5;
            }

            if (properties.ContentLanguage != null)
            {
                request.Headers[HttpRequestHeader.ContentLanguage] = properties.ContentLanguage;
            }

            if (properties.ContentEncoding != null)
            {
                request.Headers[HttpRequestHeader.ContentEncoding] = properties.ContentEncoding;
            }

            if (properties.ContentDisposition != null)
            {
                request.Headers[Constants.HeaderConstants.BlobContentDispositionRequestHeader] = properties.ContentDisposition;
            }

            if (blobType == BlobType.PageBlob)
            {
                request.Headers[Constants.HeaderConstants.BlobType] = Constants.HeaderConstants.PageBlob;
                request.Headers[Constants.HeaderConstants.BlobContentLengthHeader] = pageBlobSize.ToString(NumberFormatInfo.InvariantInfo);
                properties.Length = pageBlobSize;
            }
            else if (blobType == BlobType.BlockBlob)
            {
                request.Headers[Constants.HeaderConstants.BlobType] = Constants.HeaderConstants.BlockBlob;
            }
            else
            {
                request.Headers[Constants.HeaderConstants.BlobType] = Constants.HeaderConstants.AppendBlob;
            }

            request.ApplyAccessCondition(accessCondition);

            return request;
        }

        /// <summary>
        /// Adds the snapshot.
        /// </summary>
        /// <param name="builder">An object of type <see cref="UriQueryBuilder"/> that contains additional parameters to add to the URI query string.</param>
        /// <param name="snapshot">The snapshot version, if the blob is a snapshot.</param>
        private static void AddSnapshot(UriQueryBuilder builder, DateTimeOffset? snapshot)
        {
            if (snapshot.HasValue)
            {
                builder.Add("snapshot", Request.ConvertDateTimeToSnapshotString(snapshot.Value));
            }
        }

        /// <summary>
        /// Constructs a web request to commit a block to an append blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest AppendBlock(Uri uri, int? timeout, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.AppendBlock(uri, timeout, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to commit a block to an append blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest AppendBlock(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "appendblock");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            request.ApplyAppendCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the list of valid page ranges for a page blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="count">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetPageRanges(Uri uri, int? timeout, DateTimeOffset? snapshot, long? offset, long? count, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.GetPageRanges(uri, timeout, snapshot, offset, count, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to return the list of valid page ranges for a page blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="count">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetPageRanges(Uri uri, int? timeout, DateTimeOffset? snapshot, long? offset, long? count, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            if (offset.HasValue)
            {
                CommonUtility.AssertNotNull("count", count);
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "pagelist");
            BlobHttpWebRequestFactory.AddSnapshot(builder, snapshot);

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            AddRange(request, offset, count);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the list of page ranges that differ between a specified snapshot and this object.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="previousSnapshotTime">A <see cref="DateTimeOffset"/> representing the snapshot timestamp to use as the starting point for the diff. If this CloudPageBlob represents a snapshot, the previousSnapshotTime parameter must be prior to the current snapshot timestamp.</param>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="count">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetPageRangesDiff(Uri uri, int? timeout, DateTimeOffset? snapshot, DateTimeOffset previousSnapshotTime, long? offset, long? count, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            if (offset.HasValue)
            {
                CommonUtility.AssertNotNull("count", count);
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "pagelist");
            BlobHttpWebRequestFactory.AddSnapshot(builder, snapshot);
            builder.Add("prevsnapshot", Request.ConvertDateTimeToSnapshotString(previousSnapshotTime));

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            AddRange(request, offset, count);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Adds the Range Header for Blob Service Operations.
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="offset">Starting byte of the range</param>
        /// <param name="count">Number of bytes in the range</param>
        private static void AddRange(HttpWebRequest request, long? offset, long? count)
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
                request.Headers.Add(Constants.HeaderConstants.RangeHeader, rangeHeaderValue);
            }
        }

        /// <summary>
        /// Constructs a web request to return the blob's system properties.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetProperties(Uri uri, int? timeout, DateTimeOffset? snapshot, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.GetProperties(uri, timeout, snapshot, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to return the blob's system properties.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetProperties(Uri uri, int? timeout, DateTimeOffset? snapshot, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            BlobHttpWebRequestFactory.AddSnapshot(builder, snapshot);

            HttpWebRequest request = HttpWebRequestFactory.GetProperties(uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set system properties for a blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="properties">The blob's properties.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetProperties(Uri uri, int? timeout, BlobProperties properties, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.SetProperties(uri, timeout, properties, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to set system properties for a blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="properties">The blob's properties.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetProperties(Uri uri, int? timeout, BlobProperties properties, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("properties", properties);

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);

            if (properties != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.BlobCacheControlHeader, properties.CacheControl);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentDispositionRequestHeader, properties.ContentDisposition);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentEncodingHeader, properties.ContentEncoding);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentLanguageHeader, properties.ContentLanguage);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentMD5Header, properties.ContentMD5);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentTypeHeader, properties.ContentType);
            }

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to resize a page blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="newBlobSize">The new blob size, if the blob is a page blob. Set this parameter to <c>null</c> to keep the existing blob size.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Resize(Uri uri, int? timeout, long newBlobSize, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.Resize(uri, timeout, newBlobSize, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to resize a page blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="newBlobSize">The new blob size, if the blob is a page blob. Set this parameter to <c>null</c> to keep the existing blob size.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Resize(Uri uri, int? timeout, long newBlobSize, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);

            request.Headers.Add(Constants.HeaderConstants.BlobContentLengthHeader, newBlobSize.ToString(NumberFormatInfo.InvariantInfo));

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set a page blob's sequence number.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="sequenceNumberAction">A value of type <see cref="SequenceNumberAction"/>, indicating the operation to perform on the sequence number.</param>
        /// <param name="sequenceNumber">The sequence number. Set this parameter to <c>null</c> if this operation is an increment action.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetSequenceNumber(Uri uri, int? timeout, SequenceNumberAction sequenceNumberAction, long? sequenceNumber, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.SetSequenceNumber(uri, timeout, sequenceNumberAction, sequenceNumber, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to set a page blob's sequence number.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="sequenceNumberAction">A value of type <see cref="SequenceNumberAction"/>, indicating the operation to perform on the sequence number.</param>
        /// <param name="sequenceNumber">The sequence number. Set this parameter to <c>null</c> if this operation is an increment action.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetSequenceNumber(Uri uri, int? timeout, SequenceNumberAction sequenceNumberAction, long? sequenceNumber, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
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

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);
            request.ContentLength = 0;

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
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetMetadata(Uri uri, int? timeout, DateTimeOffset? snapshot, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.GetMetadata(uri, timeout, snapshot, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to return the user-defined metadata for the blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetMetadata(Uri uri, int? timeout, DateTimeOffset? snapshot, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            BlobHttpWebRequestFactory.AddSnapshot(builder, snapshot);

            HttpWebRequest request = HttpWebRequestFactory.GetMetadata(uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set user-defined metadata for the blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.SetMetadata(uri, timeout, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to set user-defined metadata for the blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.SetMetadata(uri, timeout, null /* builder */, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Adds user-defined metadata to the request as one or more name-value pairs.
        /// </summary>
        /// <param name="request">A <see cref="System.Net.HttpWebRequest"/> object.</param>
        /// <param name="metadata">A <see cref="Dictionary{TKey,TValue}"/> object containing the user-defined metadata.</param>
        public static void AddMetadata(HttpWebRequest request, IDictionary<string, string> metadata)
        {
            HttpWebRequestFactory.AddMetadata(request, metadata);
        }

        /// <summary>
        /// Adds user-defined metadata to the request as a single name-value pair.
        /// </summary>
        /// <param name="request">A <see cref="System.Net.HttpWebRequest"/> object.</param>
        /// <param name="name">A string containing the metadata name.</param>
        /// <param name="value">A string containing the metadata value.</param>
        public static void AddMetadata(HttpWebRequest request, string name, string value)
        {
            HttpWebRequestFactory.AddMetadata(request, name, value);
        }

        /// <summary>
        /// Constructs a web request to delete a blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="deleteSnapshotsOption">A set of options indicating whether to delete only blobs, only snapshots, or both.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Delete(Uri uri, int? timeout, DateTimeOffset? snapshot, DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.Delete(uri, timeout, snapshot, deleteSnapshotsOption, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to delete a blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="deleteSnapshotsOption">A set of options indicating whether to delete only blobs, only snapshots, or both.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Delete(Uri uri, int? timeout, DateTimeOffset? snapshot, DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            if ((snapshot != null) && (deleteSnapshotsOption != DeleteSnapshotsOption.None))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.DeleteSnapshotsNotValidError, "deleteSnapshotsOption", "snapshot"));
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            BlobHttpWebRequestFactory.AddSnapshot(builder, snapshot);

            HttpWebRequest request = HttpWebRequestFactory.Delete(uri, builder, timeout, useVersionHeader, operationContext);

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
        /// Constructs a web request to create a snapshot of a blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Snapshot(Uri uri, int? timeout, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.Snapshot(uri, timeout, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to create a snapshot of a blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Snapshot(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "snapshot");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to use to acquire, renew, change, release or break the lease for the blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="action">A <see cref="LeaseAction"/> enumeration value indicating the lease action to perform.</param>
        /// <param name="proposedLeaseId">A string specifying the lease ID to propose for the result of an acquire or change operation,
        /// or <c>null</c> if no ID is proposed for an acquire operation. This parameter should be <c>null</c> for renew, release, and break operations.</param>
        /// <param name="leaseDuration">The lease duration, in seconds, for acquire operations.
        /// If this is -1 then an infinite duration is specified. This should be <c>null</c> for renew, change, release, and break operations.</param>
        /// <param name="leaseBreakPeriod">The amount of time to wait, in seconds, after a break operation before the lease is broken.
        /// If this is <c>null</c> then the default time is used. This should be <c>null</c> for acquire, renew, change, and release operations.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Lease(Uri uri, int? timeout, LeaseAction action, string proposedLeaseId, int? leaseDuration, int? leaseBreakPeriod, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.Lease(uri, timeout, action, proposedLeaseId, leaseDuration, leaseBreakPeriod, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Generates a web request to use to acquire, renew, change, release or break the lease for the blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="action">A <see cref="LeaseAction"/> enumeration value indicating the lease action to perform.</param>
        /// <param name="proposedLeaseId">A string specifying the lease ID to propose for the result of an acquire or change operation,
        /// or <c>null</c> if no ID is proposed for an acquire operation. This parameter should be <c>null</c> for renew, release, and break operations.</param>
        /// <param name="leaseDuration">The lease duration, in seconds, for acquire operations.
        /// If this is -1 then an infinite duration is specified. This should be <c>null</c> for renew, change, release, and break operations.</param>
        /// <param name="leaseBreakPeriod">The amount of time to wait, in seconds, after a break operation before the lease is broken.
        /// If this is <c>null</c> then the default time is used. This should be <c>null</c> for acquire, renew, change, and release operations.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Lease(Uri uri, int? timeout, LeaseAction action, string proposedLeaseId, int? leaseDuration, int? leaseBreakPeriod, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "lease");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);

            // Add lease headers
            BlobHttpWebRequestFactory.AddLeaseAction(request, action);
            BlobHttpWebRequestFactory.AddLeaseDuration(request, leaseDuration);
            BlobHttpWebRequestFactory.AddProposedLeaseId(request, proposedLeaseId);
            BlobHttpWebRequestFactory.AddLeaseBreakPeriod(request, leaseBreakPeriod);

            return request;
        }

        /// <summary>
        /// Adds a proposed lease id to a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="proposedLeaseId">The proposed lease id.</param>
        internal static void AddProposedLeaseId(HttpWebRequest request, string proposedLeaseId)
        {
            request.AddOptionalHeader(Constants.HeaderConstants.ProposedLeaseIdHeader, proposedLeaseId);
        }

        /// <summary>
        /// Adds a lease duration to a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="leaseDuration">The lease duration.</param>
        internal static void AddLeaseDuration(HttpWebRequest request, int? leaseDuration)
        {
            request.AddOptionalHeader(Constants.HeaderConstants.LeaseDurationHeader, leaseDuration);
        }

        /// <summary>
        /// Adds a lease break period to a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="leaseBreakPeriod">The lease break period.</param>
        internal static void AddLeaseBreakPeriod(HttpWebRequest request, int? leaseBreakPeriod)
        {
            request.AddOptionalHeader(Constants.HeaderConstants.LeaseBreakPeriodHeader, leaseBreakPeriod);
        }

        /// <summary>
        /// Adds a lease action to a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="leaseAction">The lease action.</param>
        internal static void AddLeaseAction(HttpWebRequest request, LeaseAction leaseAction)
        {
            request.Headers.Add(Constants.HeaderConstants.LeaseActionHeader, leaseAction.ToString().ToLower());
        }

        /// <summary>
        /// Constructs a web request to write a block to a block blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="blockId">A string specifying the block ID for this block.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest PutBlock(Uri uri, int? timeout, string blockId, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.PutBlock(uri, timeout, blockId, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to write a block to a block blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="blockId">A string specifying the block ID for this block.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest PutBlock(Uri uri, int? timeout, string blockId, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "block");
            builder.Add("blockid", blockId);

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyLeaseId(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to create or update a blob by committing a block list.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="properties">A <see cref="BlobProperties"/> object specifying the properties to set for the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest PutBlockList(Uri uri, int? timeout, BlobProperties properties, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.PutBlockList(uri, timeout, properties, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to create or update a blob by committing a block list.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="properties">A <see cref="BlobProperties"/> object specifying the properties to set for the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest PutBlockList(Uri uri, int? timeout, BlobProperties properties, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("properties", properties);

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "blocklist");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);

            if (properties != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.BlobCacheControlHeader, properties.CacheControl);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentTypeHeader, properties.ContentType);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentMD5Header, properties.ContentMD5);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentLanguageHeader, properties.ContentLanguage);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentEncodingHeader, properties.ContentEncoding);
                request.AddOptionalHeader(Constants.HeaderConstants.BlobContentDispositionRequestHeader, properties.ContentDisposition);
            }

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the list of blocks for a block blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="typesOfBlocks">A <see cref="BlockListingFilter"/> enumeration value.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetBlockList(Uri uri, int? timeout, DateTimeOffset? snapshot, BlockListingFilter typesOfBlocks, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.GetBlockList(uri, timeout, snapshot, typesOfBlocks, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to return the list of blocks for a block blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="typesOfBlocks">A <see cref="BlockListingFilter"/> enumeration value.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetBlockList(Uri uri, int? timeout, DateTimeOffset? snapshot, BlockListingFilter typesOfBlocks, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "blocklist");
            builder.Add("blocklisttype", typesOfBlocks.ToString());
            BlobHttpWebRequestFactory.AddSnapshot(builder, snapshot);

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to write or clear a range of pages in a page blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="pageRange">A <see cref="PageRange"/> object.</param>
        /// <param name="pageWrite">A <see cref="PageWrite"/> enumeration value indicating the operation to perform on the page blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest PutPage(Uri uri, int? timeout, PageRange pageRange, PageWrite pageWrite, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.PutPage(uri, timeout, pageRange, pageWrite, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to write or clear a range of pages in a page blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="pageRange">A <see cref="PageRange"/> object.</param>
        /// <param name="pageWrite">A <see cref="PageWrite"/> enumeration value indicating the operation to perform on the page blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>
        /// A web request to use to perform the operation.
        /// </returns>
        public static HttpWebRequest PutPage(Uri uri, int? timeout, PageRange pageRange, PageWrite pageWrite, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("pageRange", pageRange);

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "page");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);

            request.Headers.Add(Constants.HeaderConstants.RangeHeader, pageRange.ToString());
            request.Headers.Add(Constants.HeaderConstants.PageWrite, pageWrite.ToString());

            request.ApplyAccessCondition(accessCondition);
            request.ApplySequenceNumberCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to copy a blob or file to another blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the destination blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="source">A <see cref="System.Uri"/> specifying the absolute URI to the source object, including any necessary authentication parameters.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met on the source object in order for the request to proceed.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met on the destination blob in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest CopyFrom(Uri uri, int? timeout, Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.CopyFrom(uri, timeout, source, sourceAccessCondition, destAccessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Generates a web request to copy a blob or file to another blob.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the destination blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="source">A <see cref="System.Uri"/> specifying the absolute URI to the source object, including any necessary authentication parameters.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met on the source object in order for the request to proceed.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met on the destination blob in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest CopyFrom(Uri uri, int? timeout, Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("source", source);

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, null /* builder */, useVersionHeader, operationContext);

            request.Headers.Add(Constants.HeaderConstants.CopySourceHeader, source.AbsoluteUri);
            request.ApplyAccessCondition(destAccessCondition);
            request.ApplyAccessConditionToSource(sourceAccessCondition);

            return request;
        }

        /// <summary>
        /// Generates a web request to abort a copy operation.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="copyId">The ID string of the copy operation to be aborted.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. Only lease conditions are supported for this operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest AbortCopy(Uri uri, int? timeout, string copyId, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.AbortCopy(uri, timeout, copyId, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Generates a web request to abort a copy operation.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="copyId">The ID string of the copy operation to be aborted.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. Only lease conditions are supported for this operation.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest AbortCopy(Uri uri, int? timeout, string copyId, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "copy");
            builder.Add(Constants.QueryConstants.CopyId, copyId);

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);

            request.Headers.Add(Constants.HeaderConstants.CopyActionHeader, Constants.HeaderConstants.CopyActionAbort);
            request.ApplyAccessCondition(accessCondition);

            return request;
        }

        /// <summary>
        /// Constructs a web request to get the blob's content, properties, and metadata.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot version, if the blob is a snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Get(Uri uri, int? timeout, DateTimeOffset? snapshot, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.Get(uri, timeout, snapshot, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to get the blob's content, properties, and metadata.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot version, if the blob is a snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Get(Uri uri, int? timeout, DateTimeOffset? snapshot, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            if (snapshot.HasValue)
            {
                builder.Add("snapshot", Request.ConvertDateTimeToSnapshotString(snapshot.Value));
            }

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return a specified range of the blob's content, together with its properties and metadata.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot version, if the blob is a snapshot.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="rangeContentMD5">If set to <c>true</c>, an MD5 header is requested for the specified range.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>
        /// A web request to use to perform the operation.
        /// </returns>
        public static HttpWebRequest Get(Uri uri, int? timeout, DateTimeOffset? snapshot, long? offset, long? count, bool rangeContentMD5, AccessCondition accessCondition, OperationContext operationContext)
        {
            return BlobHttpWebRequestFactory.Get(uri, timeout, snapshot, offset, count, rangeContentMD5, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to return a specified range of the blob's content, together with its properties and metadata.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="snapshot">A <see cref="DateTimeOffset"/> specifying the snapshot version, if the blob is a snapshot.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="rangeContentMD5">If set to <c>true</c>, an MD5 header is requested for the specified range.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>
        /// A web request to use to perform the operation.
        /// </returns>
        public static HttpWebRequest Get(Uri uri, int? timeout, DateTimeOffset? snapshot, long? offset, long? count, bool rangeContentMD5, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            if (offset.HasValue && offset.Value < 0)
            {
                CommonUtility.ArgumentOutOfRange("offset", offset);
            }

            if (offset.HasValue && rangeContentMD5)
            {
                CommonUtility.AssertNotNull("count", count);
                CommonUtility.AssertInBounds("count", count.Value, 1, Constants.MaxBlockSize);
            }

            HttpWebRequest request = Get(uri, timeout, snapshot, accessCondition, useVersionHeader, operationContext);
            AddRange(request, offset, count);

            if (offset.HasValue && rangeContentMD5)
            {
                request.Headers.Add(Constants.HeaderConstants.RangeContentMD5Header, Constants.HeaderConstants.TrueHeader);
            }

            return request;
        }
    }
}
