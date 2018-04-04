﻿// -----------------------------------------------------------------------------------------
// <copyright file="HttpWebRequestFactory.cs" company="Microsoft">
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
﻿
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Net;

    internal static class HttpWebRequestFactory
    {
        /// <summary>
        /// Creates the web request.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="uri">The request URI.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>
        /// A web request for performing the operation.
        /// </returns>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "operationContext")]
        internal static HttpWebRequest CreateWebRequest(string method, Uri uri, int? timeout, UriQueryBuilder builder, bool useVersionHeader, OperationContext operationContext)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            if (timeout.HasValue && timeout.Value > 0)
            {
                builder.Add("timeout", timeout.Value.ToString(CultureInfo.InvariantCulture));
            }

#if WINDOWS_PHONE || !WINDOWS_DESKTOP
            // Windows Phone does not allow the caller to disable caching, so a random GUID
            // is added to every URI to make it look a different request.
            builder.Add("randomguid", Guid.NewGuid().ToString("N"));
#endif

            Uri uriRequest = builder.AddToUri(uri);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriRequest);
            request.Method = method;

#if !WINDOWS_PHONE
            request.Proxy = operationContext.Proxy ?? request.Proxy;
#endif
                        
            // Set the Content-Length of requests to 0 by default for all put requests. 
            if (method.Equals(WebRequestMethods.Http.Put, StringComparison.OrdinalIgnoreCase))
            {
                request.ContentLength = 0;
            }

            request.UserAgent = Constants.HeaderConstants.UserAgent;

            if (useVersionHeader)
            {
                request.Headers[Constants.HeaderConstants.StorageVersionHeader] = Constants.HeaderConstants.TargetStorageVersion;
            }

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
            request.KeepAlive = true;

            // Disable the Expect 100-Continue
            request.ServicePoint.Expect100Continue = false;
#endif

            return request;
        }

        /// <summary>
        /// Creates the specified URI.
        /// </summary>
        /// <param name="uri">The URI to create.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static HttpWebRequest Create(Uri uri, int? timeout, UriQueryBuilder builder, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the ACL for a cloud resource.
        /// </summary>
        /// <param name="uri">The absolute URI to the resource.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="builder">An optional query builder to use.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        internal static HttpWebRequest GetAcl(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "acl");

            HttpWebRequest request = CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);

            // Windows phone adds */* as the Accept type when we don't set one explicitly.
#if WINDOWS_PHONE
            request.Accept = Constants.XMLAcceptHeaderValue;
#endif
            return request;
        }

        /// <summary>
        /// Constructs a web request to set the ACL for a cloud resource.
        /// </summary>
        /// <param name="uri">The absolute URI to the resource.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="builder">An optional query builder to use.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        internal static HttpWebRequest SetAcl(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "acl");

            HttpWebRequest request = CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);

            // Windows phone adds */* as the Accept type when we don't set one explicitly.
#if WINDOWS_PHONE
            request.Accept = Constants.XMLAcceptHeaderValue;
#endif
            return request;
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <param name="uri">The URI to query.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static HttpWebRequest GetProperties(Uri uri, int? timeout, UriQueryBuilder builder, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = CreateWebRequest(WebRequestMethods.Http.Head, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="uri">The blob Uri.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static HttpWebRequest GetMetadata(Uri uri, int? timeout, UriQueryBuilder builder, bool useVersionHeader, OperationContext operationContext)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "metadata");

            HttpWebRequest request = CreateWebRequest(WebRequestMethods.Http.Head, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Sets the metadata.
        /// </summary>
        /// <param name="uri">The blob Uri.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static HttpWebRequest SetMetadata(Uri uri, int? timeout, UriQueryBuilder builder, bool useVersionHeader, OperationContext operationContext)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "metadata");

            HttpWebRequest request = CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Adds the metadata.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="metadata">The metadata.</param>
        internal static void AddMetadata(HttpWebRequest request, IDictionary<string, string> metadata)
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
        internal static void AddMetadata(HttpWebRequest request, string name, string value)
        {
            CommonUtility.AssertNotNull("value", value);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(SR.ArgumentEmptyError, value);
            }

            request.Headers.Add("x-ms-meta-" + name, value);
        }

        /// <summary>
        /// Deletes the specified URI.
        /// </summary>
        /// <param name="uri">The URI of the resource to delete.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static HttpWebRequest Delete(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = CreateWebRequest("DELETE", uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Undeletes the specified URI.
        /// </summary>
        /// <param name="uri">The URI of the resource to delete.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request for performing the operation.</returns>
        internal static HttpWebRequest Undelete(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "undelete");
            HttpWebRequest request = CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Creates a web request to get the properties of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>
        /// A web request to get the service properties.
        /// </returns>
        internal static HttpWebRequest GetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "properties");
            builder.Add(Constants.QueryConstants.ResourceType, "service");

            return CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Creates a web request to set the properties of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>
        /// A web request to set the service properties.
        /// </returns>
        internal static HttpWebRequest SetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "properties");
            builder.Add(Constants.QueryConstants.ResourceType, "service");

            return CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Creates a web request to get the stats of the service.
        /// </summary>
        /// <param name="uri">The absolute URI to the service.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>
        /// A web request to get the service stats.
        /// </returns>
        internal static HttpWebRequest GetServiceStats(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            if (builder == null)
            {
                builder = new UriQueryBuilder();
            }

            builder.Add(Constants.QueryConstants.Component, "stats");
            builder.Add(Constants.QueryConstants.ResourceType, "service");

            return CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
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
