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
    using System.Net;

    /// <summary>
    /// A factory class for constructing a web request to manage directories in the File service.
    /// </summary>
    public static class DirectoryHttpWebRequestFactory
    {
        /// <summary>
        /// Constructs a web request to create a new directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the directory.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="useVersionHeader">A flag indicating whether to set the x-ms-version HTTP header.</param>
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
        /// <param name="useVersionHeader">A flag indicating whether to set the x-ms-version HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
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
        /// <param name="useVersionHeader">A flag indicating whether to set the x-ms-version HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest GetProperties(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.GetProperties(uri, timeout, directoryBuilder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return a listing of all files and subdirectories in the directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="listingContext">A set of parameters for the listing operation.</param>
        /// <param name="useVersionHeader">A flag indicating whether to set the x-ms-version HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest List(Uri uri, int? timeout, FileListingContext listingContext, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = GetDirectoryUriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "list");

            if (listingContext != null)
            {
                if (listingContext.Marker != null)
                {
                    builder.Add("marker", listingContext.Marker);
                }

                if (listingContext.MaxResults.HasValue)
                {
                    builder.Add("maxresults", listingContext.MaxResults.ToString());
                }
            }

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
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
