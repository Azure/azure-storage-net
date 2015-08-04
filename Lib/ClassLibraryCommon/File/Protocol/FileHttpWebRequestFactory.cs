//-----------------------------------------------------------------------
// <copyright file="FileHttpWebRequestFactory.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File.Protocol
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
    /// A factory class for constructing web requests for operations on files in the File service.
    /// </summary>
    public static class FileHttpWebRequestFactory
    {
        /// <summary>
        /// Creates a web request to get the properties of the File service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the File service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.GetServiceProperties(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Creates a web request to set the properties of the File service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the File service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.SetServiceProperties(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Writes File service properties to a stream, formatted in XML.
        /// </summary>
        /// <param name="properties">A <see cref="FileServiceProperties"/> object.</param>
        /// <param name="outputStream">The <see cref="System.IO.Stream"/> object to which the formatted properties are to be written.</param>
        public static void WriteServiceProperties(FileServiceProperties properties, Stream outputStream)
        {
            CommonUtility.AssertNotNull("properties", properties);

            properties.WriteServiceProperties(outputStream);
        }

        /// <summary>
        /// Constructs a web request to create a new file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="properties">The properties to set for the file.</param>
        /// <param name="fileSize">The size of the file.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Create(Uri uri, int? timeout, FileProperties properties, long fileSize, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("properties", properties);
            
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
                request.Headers[Constants.HeaderConstants.FileContentDispositionRequestHeader] = properties.ContentDisposition;
            }

            request.Headers[Constants.HeaderConstants.FileType] = Constants.HeaderConstants.File;
            request.Headers[Constants.HeaderConstants.FileContentLengthHeader] = fileSize.ToString(NumberFormatInfo.InvariantInfo);
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
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static HttpWebRequest GetProperties(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            HttpWebRequest request = HttpWebRequestFactory.GetProperties(uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the user-defined metadata for the file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static HttpWebRequest GetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            HttpWebRequest request = HttpWebRequestFactory.GetMetadata(uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Adds user-defined metadata to the request as one or more name-value pairs.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <param name="metadata">The user-defined metadata.</param>
        public static void AddMetadata(HttpWebRequest request, IDictionary<string, string> metadata)
        {
            HttpWebRequestFactory.AddMetadata(request, metadata);
        }

        /// <summary>
        /// Adds user-defined metadata to the request as a single name-value pair.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <param name="name">The metadata name.</param>
        /// <param name="value">The metadata value.</param>
        public static void AddMetadata(HttpWebRequest request, string name, string value)
        {
            HttpWebRequestFactory.AddMetadata(request, name, value);
        }

        /// <summary>
        /// Constructs a web request to delete a file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Delete(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.Delete(uri, null /* builder */, timeout, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Adds the Range Header for File Service Operations.
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
        /// Constructs a web request to return the list of valid ranges for a file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="offset">The starting offset of the data range over which to list file ranges, in bytes.</param>
        /// <param name="count">The length of the data range over which to list file ranges, in bytes.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest ListRanges(Uri uri, int? timeout, long? offset, long? count, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            if (offset.HasValue)
            {
                CommonUtility.AssertNotNull("count", count);
            }

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "rangelist");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
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
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetProperties(Uri uri, int? timeout, FileProperties properties, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("properties", properties);

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);

            if (properties != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileCacheControlHeader, properties.CacheControl);
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentEncodingHeader, properties.ContentEncoding);
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentDispositionRequestHeader, properties.ContentDisposition);
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentLanguageHeader, properties.ContentLanguage);
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentMD5Header, properties.ContentMD5);
                request.AddOptionalHeader(Constants.HeaderConstants.FileContentTypeHeader, properties.ContentType);
            }

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to resize a file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="newFileSize">The new file size. Set this parameter to <c>null</c> to keep the existing file size.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest Resize(Uri uri, int? timeout, long newFileSize, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);

            request.Headers.Add(Constants.HeaderConstants.FileContentLengthHeader, newFileSize.ToString(NumberFormatInfo.InvariantInfo));

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to get the file's content, properties, and metadata.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static HttpWebRequest Get(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }
        
        /// <summary>
        /// Constructs a web request to set user-defined metadata for the file.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static HttpWebRequest SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.SetMetadata(uri, timeout, null /* builder */, useVersionHeader, operationContext);
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
        /// <param name="rangeContentMD5">If set to <c>true</c>, request an MD5 header for the specified range.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest Get(Uri uri, int? timeout, long? offset, long? count, bool rangeContentMD5, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
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

            HttpWebRequest request = Get(uri, timeout, accessCondition, useVersionHeader, operationContext);
            AddRange(request, offset, count);

            if (offset.HasValue && rangeContentMD5)
            {
                request.Headers.Add(Constants.HeaderConstants.RangeContentMD5Header, Constants.HeaderConstants.TrueHeader);
            }

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
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest PutRange(Uri uri, int? timeout, FileRange fileRange, FileRangeWrite fileRangeWrite, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("fileRange", fileRange);

            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "range");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);

            request.AddOptionalHeader(Constants.HeaderConstants.RangeHeader, fileRange.ToString());
            request.Headers.Add(Constants.HeaderConstants.FileRangeWrite, fileRangeWrite.ToString());

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to copy from a blob or file to another file.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the destination file.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="source">A <see cref="System.Uri"/> specifying the absolute URI to the source object, including any necessary authentication parameters.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met on the source object in order for the request to proceed.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met on the destination file in order for the request to proceed.</param>
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
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the file.</param>
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
    }
}
