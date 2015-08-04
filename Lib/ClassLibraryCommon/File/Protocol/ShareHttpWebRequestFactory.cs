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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;

    /// <summary>
    /// A factory class for constructing web requests for operations on shares in the File service.
    /// </summary>
    public static class ShareHttpWebRequestFactory
    {
        /// <summary>
        /// Constructs a web request to create a new share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest Create(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return ShareHttpWebRequestFactory.Create(uri, null /* properties */, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Constructs a web request to create a new share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="properties">Properties to set on the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static HttpWebRequest Create(Uri uri, FileShareProperties properties, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
        
            HttpWebRequest request = HttpWebRequestFactory.Create(uri, timeout, shareBuilder, useVersionHeader, operationContext);
            if (properties != null && properties.Quota.HasValue)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.ShareQuota, properties.Quota);
            }

            return request;
        }

        /// <summary>
        /// Constructs a web request to delete the share and all of the files within it.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
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
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
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
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
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
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
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
        /// Constructs a web request to set system properties for a share.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the share.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="properties">The share's properties.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetProperties(Uri uri, int? timeout, FileShareProperties properties, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("properties", properties);

            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            shareBuilder.Add(Constants.QueryConstants.Component, "properties");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, shareBuilder, useVersionHeader, operationContext);

            if (properties.Quota.HasValue)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.ShareQuota, properties.Quota.Value);
            }

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Creates a web request to get the stats of the share.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the share.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetStats(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            shareBuilder.Add(Constants.QueryConstants.Component, "stats");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, shareBuilder, useVersionHeader, operationContext);

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
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
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
        /// Constructs a web request to return the ACL for a share.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the share.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetAcl(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.GetAcl(uri, GetShareUriQueryBuilder(), timeout, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set the ACL for a share.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the share.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="publicAccess">The type of public access to allow for the share.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetAcl(Uri uri, int? timeout, FileSharePublicAccessType publicAccess, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.SetAcl(uri, GetShareUriQueryBuilder(), timeout, useVersionHeader, operationContext);

            request.ApplyAccessCondition(accessCondition);
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
