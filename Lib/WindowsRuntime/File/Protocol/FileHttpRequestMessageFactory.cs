// -----------------------------------------------------------------------------------------
// <copyright file="FileHttpRequestMessageFactory.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Headers;

    internal static class FileHttpRequestMessageFactory
    {
        /// <summary>
        /// Constructs a web request to create a new file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="properties">The properties to set for the file.</param>
        /// <param name="fileSize">For a file, the size of the file. This parameter is ignored
        /// for block files.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Create(Uri uri, int? timeout, FileProperties properties, long fileSize, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, null /* builder */, content, operationContext, canonicalizer, credentials);

            if (properties.CacheControl != null)
            {
                request.Headers.CacheControl = CacheControlHeaderValue.Parse(properties.CacheControl);
            }

            if (content != null)
            {
                if (properties.ContentType != null)
                {
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse(properties.ContentType);
                }

                if (properties.ContentMD5 != null)
                {
                    content.Headers.ContentMD5 = Convert.FromBase64String(properties.ContentMD5);
                }

                if (properties.ContentLanguage != null)
                {
                    content.Headers.ContentLanguage.Add(properties.ContentLanguage);
                }

                if (properties.ContentEncoding != null)
                {
                    content.Headers.ContentEncoding.Add(properties.ContentEncoding);
                }

                if (properties.ContentDisposition != null)
                {
                    content.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse(properties.ContentDisposition);
                }
            }

            request.Headers.Add(Constants.HeaderConstants.FileType, Constants.HeaderConstants.File);
            request.Headers.Add(Constants.HeaderConstants.FileContentLengthHeader, fileSize.ToString(NumberFormatInfo.InvariantInfo));
            properties.Length = fileSize;

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the file's system properties.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage GetProperties(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            StorageRequestMessage request = HttpRequestMessageFactory.GetProperties(uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the user-defined metadata for the file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage GetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            StorageRequestMessage request = HttpRequestMessageFactory.GetMetadata(uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set user-defined metadata for the file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage request = HttpRequestMessageFactory.SetMetadata(uri, timeout, null /* builder */, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
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
        /// Constructs a web request to delete a file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Delete(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage request = HttpRequestMessageFactory.Delete(uri, timeout, null /* builder */, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Adds the Range Header for File Service Operations.
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="offset">Starting byte of the range</param>
        /// <param name="count">Number of bytes in the range</param>
        private static void AddRange(StorageRequestMessage request, long? offset, long? count)
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
        /// Constructs a web request to get the file's content, properties, and metadata.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage Get(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the list of valid ranges for a file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="offset">The starting offset of the data range over which to list file ranges, in bytes.</param>
        /// <param name="count">The length of the data range over which to list file ranges, in bytes.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage ListRanges(Uri uri, int? timeout, long? offset, long? count, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (offset.HasValue)
            {
                CommonUtility.AssertNotNull("count", count);
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "rangelist");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            AddRange(request, offset, count);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set system properties for a file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="properties">The file's properties.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage SetProperties(Uri uri, int? timeout, FileProperties properties, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            if (properties != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileCacheControlHeader, properties.CacheControl);
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentDispositionRequestHeader, properties.ContentDisposition);
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentEncodingHeader, properties.ContentEncoding);
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentLanguageHeader, properties.ContentLanguage);
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentMD5Header, properties.ContentMD5);
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentTypeHeader, properties.ContentType);
            }

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return a specified range of the file's content, together with its properties and metadata.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or null to return all bytes through the end of the file.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Get(Uri uri, int? timeout, long? offset, long? count, bool rangeContentMD5, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
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

            StorageRequestMessage request = Get(uri, timeout, accessCondition, content, operationContext, canonicalizer, credentials);
            AddRange(request, offset, count);

            if (offset.HasValue && rangeContentMD5)
            {
                request.Headers.Add(Constants.HeaderConstants.RangeContentMD5Header, Constants.HeaderConstants.TrueHeader);
            }

            return request;
        }

        /// Constructs a web request to set system properties for a file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="newFileSize">The new file size. Set this parameter to <c>null</c> to keep the existing file size.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Resize(Uri uri, int? timeout, long newFileSize, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            request.Headers.Add(Constants.HeaderConstants.FileContentLengthHeader, newFileSize.ToString(NumberFormatInfo.InvariantInfo));

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to write or clear a range of pages in a file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="fileRange">The beginning and ending offsets.</param>
        /// <param name="fileRangeWrite">Action describing whether we are writing to a file or clearing a set of ranges.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="content">The corresponding Http content.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage PutRange(Uri uri, int? timeout, FileRange fileRange, FileRangeWrite fileRangeWrite, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "range");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            request.AddOptionalHeader(Constants.HeaderConstants.RangeHeader, fileRange.ToString());
            request.Headers.Add(Constants.HeaderConstants.FileRangeWrite, fileRangeWrite.ToString());

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to copy.
        /// </summary>
        /// <param name="uri">The absolute URI to the destination file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="source">The absolute URI to the source object, including any necessary authentication parameters.</param>
        /// <param name="sourceAccessCondition">The access condition to apply to the source object.</param>
        /// <param name="destAccessCondition">The access condition to apply to the destination file.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage CopyFrom(Uri uri, int? timeout, Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, null /* builder */, content, operationContext, canonicalizer, credentials);

            request.Headers.Add(Constants.HeaderConstants.CopySourceHeader, source.AbsoluteUri);
            request.ApplyAccessCondition(destAccessCondition);
            request.ApplyAccessConditionToSource(sourceAccessCondition);

            return request;
        }

        /// <summary>
        /// Constructs a web request to get the properties of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="timeout">The server timeout interval.</param>
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
        /// <returns>A web request to set the service properties.</returns>
        internal static StorageRequestMessage SetServiceProperties(Uri uri, int? timeout, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            return HttpRequestMessageFactory.SetServiceProperties(uri, timeout, content, operationContext, canonicalizer, credentials);
        }

        /// <summary>
        /// Generates a web request to abort a copy operation.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="copyId">The ID string of the copy operation to be aborted.</param>
        /// <param name="accessCondition">The access condition to apply to the request.
        ///     Only lease conditions are supported for this operation.</param>
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
    }
}
