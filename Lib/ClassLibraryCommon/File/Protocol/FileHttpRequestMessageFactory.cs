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

namespace Microsoft.Azure.Storage.File.Protocol
{
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Auth;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
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
            CommonUtility.AssertNotNull("properties", properties);

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, null /* builder */, content, operationContext, canonicalizer, credentials);

            if (properties.CacheControl != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileCacheControlHeader, properties.CacheControl);
            }

            if (properties.ContentType != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentTypeHeader, properties.ContentType);
            }

            if (properties.ContentMD5 != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentMD5Header, properties.ContentMD5);
            }

            if (properties.ContentLanguage != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentLanguageHeader, properties.ContentLanguage);
            }

            if (properties.ContentEncoding != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentEncodingHeader, properties.ContentEncoding);
            }

            if (properties.ContentDisposition != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentDispositionRequestHeader, properties.ContentDisposition);
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
        /// <param name="shareSnapshot">A <see cref="DateTimeOffset"/> specifying the share snapshot timestamp, if the share is a snapshot.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage GetProperties(Uri uri, int? timeout, DateTimeOffset? shareSnapshot, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            FileHttpRequestMessageFactory.AddShareSnapshot(builder, shareSnapshot);

            StorageRequestMessage request = HttpRequestMessageFactory.GetProperties(uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the user-defined metadata for the file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="shareSnapshot">A <see cref="DateTimeOffset"/> specifying the share snapshot timestamp, if the share is a snapshot.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage GetMetadata(Uri uri, int? timeout, DateTimeOffset? shareSnapshot, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            FileHttpRequestMessageFactory.AddShareSnapshot(builder, shareSnapshot);

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
        /// <param name="shareSnapshot">A <see cref="DateTimeOffset"/> specifying the share snapshot timestamp, if the share is a snapshot.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage Get(Uri uri, int? timeout, DateTimeOffset? shareSnapshot, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            FileHttpRequestMessageFactory.AddShareSnapshot(builder, shareSnapshot);

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
        /// <param name="shareSnapshot">A <see cref="DateTimeOffset"/> specifying the share snapshot timestamp, if the share is a snapshot.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage ListRanges(Uri uri, int? timeout, long? offset, long? count, DateTimeOffset? shareSnapshot, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (offset.HasValue)
            {
                CommonUtility.AssertNotNull("count", count);
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            FileHttpRequestMessageFactory.AddShareSnapshot(builder, shareSnapshot);
            builder.Add(Constants.QueryConstants.Component, "rangelist");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            AddRange(request, offset, count);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary> 
        /// Constructs a web request to return the list of open handles for a file or directory. 
        /// </summary> 
        /// <param name="uri">The absolute URI to the file.</param> 
        /// <param name="timeout">The server timeout interval.</param> 
        /// <param name="maxResults">The maximum number of results to be returned by the server.</param> 
        /// <param name="recursive">Whether to recurse through a directory's files and subfolders.</param>
        /// <param name="nextMarker">Marker returned by a previous call to continue fetching results.</param> 
        /// <param name="accessCondition">The access condition to apply to the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param> 
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns> 
        public static StorageRequestMessage ListHandles(Uri uri, int? timeout, int? maxResults, bool? recursive, FileContinuationToken nextMarker, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "listhandles");

            if (maxResults.HasValue)
            {
                builder.Add(Constants.MaxResults, maxResults.Value.ToString());
            }

            if (nextMarker != null)
            {
                builder.Add(Constants.HeaderConstants.Marker, nextMarker.NextMarker);
            }

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);

            if (recursive.HasValue)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.Recursive, recursive.Value.ToString());
            }

            return request;
        }

        /// <summary> 
        /// Constructs a web request to close one or more open handles for a file or directory. 
        /// </summary> 
        /// <param name="uri">The absolute URI to the file.</param> 
        /// <param name="timeout">The server timeout interval.</param> 
        /// <param name="handleId">ID of the handle to be closed, "*" if all should be closed.</param> 
        /// <param name="recursive">Whether to recurse through this directory's subfiles and folders.</param> 
        /// <param name="token">Continuation token for closing many handles.</param> 
        /// <param name="accessCondition">The access condition to apply to the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param> 
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns> 
        public static StorageRequestMessage CloseHandle(Uri uri, int? timeout, string handleId, bool? recursive, FileContinuationToken token, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "forceclosehandles");

            if (token != null && token.NextMarker != null)
            {
                builder.Add(Constants.HeaderConstants.Marker, token.NextMarker);
            }

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            if (handleId != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.HandleId, handleId);
            }

            if (recursive.HasValue)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.Recursive, recursive.Value.ToString());
            }

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
            CommonUtility.AssertNotNull("properties", properties);
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
        /// <param name="shareSnapshot">A <see cref="DateTimeOffset"/> specifying the share snapshot timestamp, if the share is a snapshot.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Get(Uri uri, int? timeout, long? offset, long? count, bool rangeContentMD5, DateTimeOffset? shareSnapshot, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (offset.HasValue && offset.Value < 0)
            {
                CommonUtility.ArgumentOutOfRange("offset", offset);
            }

            if (offset.HasValue && rangeContentMD5)
            {
                CommonUtility.AssertNotNull("count", count);
                CommonUtility.AssertInBounds("count", count.Value, 1, Constants.MaxRangeGetContentMD5Size);
            }

            StorageRequestMessage request = Get(uri, timeout, shareSnapshot, accessCondition, content, operationContext, canonicalizer, credentials);
            AddRange(request, offset, count);

            if (offset.HasValue && rangeContentMD5)
            {
                request.Headers.Add(Constants.HeaderConstants.RangeContentMD5Header, Constants.HeaderConstants.TrueHeader);
            }

            return request;
        }

        /// <summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="newFileSize">The new file size. Set this parameter to <c>null</c> to keep the existing file size.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        /// </summary>
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

        /// <summary>
        /// Adds the share snapshot.
        /// </summary>
        /// <param name="builder">An object of type <see cref="UriQueryBuilder"/> that contains additional parameters to add to the URI query string.</param>
        /// <param name="snapshot">The snapshot version, if the share is a snapshot.</param>
        private static void AddShareSnapshot(UriQueryBuilder builder, DateTimeOffset? snapshot)
        {
            if (snapshot.HasValue)
            {
                builder.Add(Constants.QueryConstants.ShareSnapshot, Request.ConvertDateTimeToSnapshotString(snapshot.Value));
            }
        }
    }
}
