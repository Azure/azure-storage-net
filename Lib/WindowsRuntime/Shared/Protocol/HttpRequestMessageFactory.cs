// -----------------------------------------------------------------------------------------
// <copyright file="HttpRequestMessageFactory.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    internal static class HttpRequestMessageFactory
    {
        /// <summary>
        /// Creates the web request.
        /// </summary>
        /// <param name="uri">The request Uri.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static StorageRequestMessage CreateRequestMessage(HttpMethod method, Uri uri, int? timeout, UriQueryBuilder builder, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            if (timeout.HasValue && timeout.Value > 0)
            {
                builder.Add("timeout", timeout.ToString());
            }

#if WINDOWS_RT && !NETCORE
            // Windows Phone does not allow the caller to disable caching, so a random GUID
            // is added to every URI to make it look like a different request.
            builder.Add("randomguid", Guid.NewGuid().ToString("N"));
#endif

            Uri uriRequest = builder.AddToUri(uri);

            StorageRequestMessage msg = new StorageRequestMessage(method, uriRequest, canonicalizer, credentials, credentials.AccountName);
            msg.Content = content;

            return msg;
        }

        /// <summary>
        /// Creates the specified Uri.
        /// </summary>
        /// <param name="uri">The Uri to create.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static StorageRequestMessage Create(Uri uri, int? timeout, UriQueryBuilder builder, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage request = CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the ACL for a cloud resource.
        /// </summary>
        /// <param name="uri">The absolute URI to the resource.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="builder">An optional query builder to use.</param>
        /// <returns><returns>A web request to use to perform the operation.</returns></returns>
        internal static StorageRequestMessage GetAcl(Uri uri, int? timeout, UriQueryBuilder builder, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "acl");

            StorageRequestMessage request = CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

#if WINDOWS_PHONE
            // Windows phone adds */* as the Accept type when we don't set one explicitly.
            request.Headers.Add(Constants.HeaderConstants.PayloadAcceptHeader, Constants.XMLAcceptHeaderValue);
#endif
            return request;
        }

        /// <summary>
        /// Constructs a web request to set the ACL for a cloud resource.
        /// </summary>
        /// <param name="uri">The absolute URI to the resource.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="builder">An optional query builder to use.</param>
        /// <returns><returns>A web request to use to perform the operation.</returns></returns>
        internal static StorageRequestMessage SetAcl(Uri uri, int? timeout, UriQueryBuilder builder, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "acl");

            StorageRequestMessage request = CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

#if WINDOWS_PHONE
            // Windows phone adds */* as the Accept type when we don't set one explicitly.
            request.Headers.Add(Constants.HeaderConstants.PayloadAcceptHeader, Constants.XMLAcceptHeaderValue);
#endif
            return request;
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <param name="uri">The Uri to query.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static StorageRequestMessage GetProperties(Uri uri, int? timeout, UriQueryBuilder builder, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage request = CreateRequestMessage(HttpMethod.Head, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            return request;
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="uri">The blob Uri.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static StorageRequestMessage GetMetadata(Uri uri, int? timeout, UriQueryBuilder builder, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "metadata");

            StorageRequestMessage request = CreateRequestMessage(HttpMethod.Head, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            return request;
        }

        /// <summary>
        /// Sets the metadata.
        /// </summary>
        /// <param name="uri">The blob Uri.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static StorageRequestMessage SetMetadata(Uri uri, int? timeout, UriQueryBuilder builder, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "metadata");

            StorageRequestMessage request = CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            return request;
        }

        /// <summary>
        /// Adds the metadata.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="metadata">The metadata.</param>
        internal static void AddMetadata(StorageRequestMessage request, IDictionary<string, string> metadata)
        {
            if (metadata != null)
            {
                foreach (KeyValuePair<string, string> entry in metadata)
                {
                    AddMetadata(request, entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// Adds the metadata.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="name">The metadata name.</param>
        /// <param name="value">The metadata value.</param>
        internal static void AddMetadata(StorageRequestMessage request, string name, string value)
        {
            CommonUtility.AssertNotNull("value", value);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(SR.ArgumentEmptyError, value);
            }

            request.Headers.Add("x-ms-meta-" + name, value);
        }

        /// <summary>
        /// Deletes the specified Uri.
        /// </summary>
        /// <param name="uri">The Uri to delete.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static StorageRequestMessage Delete(Uri uri, int? timeout, UriQueryBuilder builder, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            StorageRequestMessage request = CreateRequestMessage(HttpMethod.Delete, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            return request;
        }

        /// <summary>
        /// Creates a web request to get the properties of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <returns>A web request to get the service properties.</returns>
        internal static StorageRequestMessage GetServiceProperties(Uri uri, int? timeout, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = GetServiceUriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, null /* content */, operationContext, canonicalizer, credentials);
            return request;
        }

        /// <summary>
        /// Creates a web request to set the properties of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <returns>A web request to set the service properties.</returns>
        internal static StorageRequestMessage SetServiceProperties(Uri uri, int? timeout, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = GetServiceUriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);
            return request;
        }

        /// <summary>
        /// Creates a web request to get the stats of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <returns>A web request to get the service stats.</returns>
        internal static StorageRequestMessage GetServiceStats(Uri uri, int? timeout, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder builder = GetServiceUriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "stats");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, builder, null /* content */, operationContext, canonicalizer, credentials);
            return request;
        }

        /// <summary>
        /// Generates a query builder for building service requests.
        /// </summary>
        /// <returns>A <see cref="UriQueryBuilder"/> for building service requests.</returns>
        internal static UriQueryBuilder GetServiceUriQueryBuilder()
        {
            UriQueryBuilder uriBuilder = new UriQueryBuilder();
            uriBuilder.Add(Constants.QueryConstants.ResourceType, "service");
            return uriBuilder;
        }
    }
}
