// -----------------------------------------------------------------------------------------
// <copyright file="ShareHttpRequestMessageFactory.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;

    internal static class ShareHttpRequestMessageFactory
    {
        /// <summary>
        /// Constructs a web request to create a new share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="properties">Properties to set on the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Create(Uri uri, FileShareProperties properties, int? timeout, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            
            StorageRequestMessage request = HttpRequestMessageFactory.Create(uri, timeout, shareBuilder, content, operationContext, canonicalizer, credentials);
            if (properties != null && properties.Quota.HasValue)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.ShareQuota, properties.Quota.Value);
            }

            return request;
        }

        /// <summary>
        /// Constructs a web request to delete the share and all of the files within it.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Delete(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();

            StorageRequestMessage request = HttpRequestMessageFactory.Delete(uri, timeout, shareBuilder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the user-defined metadata for this share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage GetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();

            StorageRequestMessage request = HttpRequestMessageFactory.GetMetadata(uri, timeout, shareBuilder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the properties and user-defined metadata for this share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage GetProperties(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();

            StorageRequestMessage request = HttpRequestMessageFactory.GetProperties(uri, timeout, shareBuilder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to set user-defined metadata for the share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            StorageRequestMessage request = HttpRequestMessageFactory.SetMetadata(uri, timeout, shareBuilder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set system properties for a share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="properties">The share's properties.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage SetProperties(Uri uri, int? timeout, FileShareProperties properties, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            CommonUtility.AssertNotNull("properties", properties);

            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            shareBuilder.Add(Constants.QueryConstants.Component, "properties");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, shareBuilder, content, operationContext, canonicalizer, credentials);
            if (properties.Quota.HasValue)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.ShareQuota, properties.Quota.Value);
            }

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
        /// Constructs a web request to return a listing of all shares in this storage account.
        /// </summary>
        /// <param name="uri">The absolute URI for the account.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="listingContext">A set of parameters for the listing operation.</param>
        /// <param name="detailsIncluded">Additional details to return with the listing.</param>
        /// <returns>A web request for the specified operation.</returns>
        public static StorageRequestMessage List(Uri uri, int? timeout, ListingContext listingContext, ShareListingDetails detailsIncluded, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
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

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the ACL for a share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns><returns>A web request to use to perform the operation.</returns></returns>
        public static StorageRequestMessage GetAcl(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage request = HttpRequestMessageFactory.GetAcl(uri, timeout, GetShareUriQueryBuilder(), content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set the ACL for a share.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="publicAccess">The type of public access to allow for the share.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns><returns>A web request to use to perform the operation.</returns></returns>
        public static StorageRequestMessage SetAcl(Uri uri, int? timeout, FileSharePublicAccessType publicAccess, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage request = HttpRequestMessageFactory.SetAcl(uri, timeout, GetShareUriQueryBuilder(), content, operationContext, canonicalizer, credentials);

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to get the stats of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <returns>A StorageRequestMessage to get the service stats.</returns>
        public static StorageRequestMessage GetStats(Uri uri, int? timeout, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder shareBuilder = GetShareUriQueryBuilder();
            shareBuilder.Add(Constants.QueryConstants.Component, "stats");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, shareBuilder, null /* content */, operationContext, canonicalizer, credentials);

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
