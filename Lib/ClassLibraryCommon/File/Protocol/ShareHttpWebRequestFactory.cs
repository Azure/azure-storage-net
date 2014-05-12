//-----------------------------------------------------------------------
// <copyright file="ShareHttpWebRequestFactory.cs" company="Microsoft">
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
    /// A factory class for constructing a web request to manage shares in the File service.
    /// </summary>
    public static class ShareHttpWebRequestFactory
    {
        /// <summary>
        /// Constructs a web request to create a new share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="useVersionHeader">A flag indicating whether to set the x-ms-version HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest Create(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            return HttpWebRequestFactory.Create(uri, timeout, shareBuilder, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Constructs a web request to delete the share and all of the files within it.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A flag indicating whether to set the x-ms-version HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest Delete(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.Delete(uri, shareBuilder, timeout, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the user-defined metadata for this share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A flag indicating whether to set the x-ms-version HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest GetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.GetMetadata(uri, timeout, shareBuilder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the properties and user-defined metadata for this share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A flag indicating whether to set the x-ms-version HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest GetProperties(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.GetProperties(uri, timeout, shareBuilder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to set user-defined metadata for the share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A flag indicating whether to set the x-ms-version HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.SetMetadata(uri, timeout, shareBuilder, useVersionHeader, operationContext);
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
        /// Constructs a web request to return a listing of all shares in this storage account.
        /// </summary>
        /// <param name="uri">The absolute URI for the account.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="listingContext">A set of parameters for the listing operation.</param>
        /// <param name="detailsIncluded">Additional details to return with the listing.</param>
        /// <param name="useVersionHeader">A flag indicating whether to set the x-ms-version HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request for the specified operation.</returns>
        public static HttpWebRequest List(Uri uri, int? timeout, ListingContext listingContext, ShareListingDetails detailsIncluded, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "list");

            if (listingContext != null)
            {
                if (listingContext.Prefix != null)
                {
                    builder.Add("prefix", listingContext.Prefix);
                }

                if (listingContext.Marker != null)
                {
                    builder.Add("marker", listingContext.Marker);
                }

                if (listingContext.MaxResults.HasValue)
                {
                    builder.Add("maxresults", listingContext.MaxResults.ToString());
                }
            }

            if ((detailsIncluded & ShareListingDetails.Metadata) != 0)
            {
                builder.Add("include", "metadata");
            }

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Gets the share Uri query builder.
        /// </summary>
        /// <returns>A <see cref="UriQueryBuilder"/> for the share.</returns>
        internal static UriQueryBuilder GetShareUriQueryBuilder()
        {
            UriQueryBuilder uriBuilder = new UriQueryBuilder();
            uriBuilder.Add(Constants.QueryConstants.ResourceType, "share");
            return uriBuilder;
        }
    }
}
