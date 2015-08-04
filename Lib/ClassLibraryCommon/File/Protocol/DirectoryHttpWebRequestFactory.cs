//-----------------------------------------------------------------------
// <copyright file="DirectoryHttpWebRequestFactory.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// A factory class for constructing web requests for operations on directories in the File service.
    /// </summary>
    public static class DirectoryHttpWebRequestFactory
    {
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
        /// Constructs a web request to create a new directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the directory.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest Create(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            return HttpWebRequestFactory.Create(uri, timeout, directoryBuilder, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Constructs a web request to delete the directory and all of the files within it.
        /// </summary>
        /// <param name="uri">The absolute URI to the directory.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Delete(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.Delete(uri, directoryBuilder, timeout, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the properties and user-defined metadata for this directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the directory.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetProperties(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();

            HttpWebRequest request = HttpWebRequestFactory.GetProperties(uri, timeout, directoryBuilder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the user-defined metadata for this directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the directory.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();

            HttpWebRequest request = HttpWebRequestFactory.GetMetadata(uri, timeout, directoryBuilder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return a listing of all files and subdirectories in the directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="listingContext">A set of parameters for the listing operation.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest List(Uri uri, int? timeout, FileListingContext listingContext, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            directoryBuilder.Add(Constants.QueryConstants.Component, "list");

            if (listingContext != null)
            {
                if (listingContext.Marker != null)
                {
                    directoryBuilder.Add("marker", listingContext.Marker);
                }

                if (listingContext.MaxResults.HasValue)
                {
                    directoryBuilder.Add("maxresults", listingContext.MaxResults.ToString());
                }
            }

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, directoryBuilder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set user-defined metadata for the directory.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the destination blob.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static HttpWebRequest SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.SetMetadata(uri, timeout, directoryBuilder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Gets the directory Uri query builder.
        /// </summary>
        /// <returns>A <see cref="UriQueryBuilder"/> for the directory.</returns>
        internal static UriQueryBuilder GetDirectoryUriQueryBuilder()
        {
            UriQueryBuilder uriBuilder = new UriQueryBuilder();
            uriBuilder.Add(Constants.QueryConstants.ResourceType, "directory");
            return uriBuilder;
        }
    }
}
