// -----------------------------------------------------------------------------------------
// <copyright file="QueueHttpWebRequestFactory.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Queue.Protocol
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
    /// A factory class for constructing a web request to manage queues in the Queue service.
    /// </summary>
    public static class QueueHttpWebRequestFactory
    {
        /// <summary>
        /// Creates a web request to get the properties of the Queue service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Queue service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.GetServiceProperties(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Creates a web request to get the properties of the Queue service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Queue service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        internal static HttpWebRequest GetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.GetServiceProperties(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Creates a web request to set the properties of the Queue service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Queue service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.SetServiceProperties(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Creates a web request to set the properties of the Queue service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Queue service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        internal static HttpWebRequest SetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.SetServiceProperties(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Creates a web request to get Queue service stats.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Queue service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetServiceStats(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.GetServiceStats(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Creates a web request to get Queue service stats.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Queue service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        internal static HttpWebRequest GetServiceStats(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.GetServiceStats(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Writes Queue service properties to a stream, formatted in XML.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="outputStream">The <see cref="System.IO.Stream"/> object to which the formatted properties are to be written.</param>
        public static void WriteServiceProperties(ServiceProperties properties, Stream outputStream)
        {
            CommonUtility.AssertNotNull("properties", properties);

            properties.WriteServiceProperties(outputStream);
        }

        /// <summary>
        /// Constructs a web request to create a new queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Create(Uri uri, int? timeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.Create(uri, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to create a new queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Create(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.Create(uri, timeout, null, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Constructs a web request to delete the queue and all of the messages within it.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Delete(Uri uri, int? timeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.Delete(uri, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to delete the queue and all of the messages within it.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Delete(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.Delete(uri, null, timeout, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Constructs a web request to clear all messages in the queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest ClearMessages(Uri uri, int? timeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.ClearMessages(uri, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to clear all messages in the queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest ClearMessages(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.Delete(uri, null, timeout, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the user-defined metadata for this queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetMetadata(Uri uri, int? timeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.GetMetadata(uri, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Generates a web request to return the user-defined metadata for this queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetMetadata(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.GetMetadata(uri, timeout, null, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Generates a web request to set user-defined metadata for the queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetMetadata(Uri uri, int? timeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.SetMetadata(uri, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Generates a web request to set user-defined metadata for the queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetMetadata(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.SetMetadata(uri, timeout, null, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Adds user-defined metadata to the request as one or more name-value pairs.
        /// </summary>
        /// <param name="request">A <see cref="System.Net.HttpWebRequest"/> object.</param>
        /// <param name="metadata">A <see cref="Dictionary{TKey,TValue}"/> object containing the user-defined metadata.</param>
        public static void AddMetadata(HttpWebRequest request, IDictionary<string, string> metadata)
        {
            HttpWebRequestFactory.AddMetadata(request, metadata);
        }

        /// <summary>
        /// Adds user-defined metadata to the request as a single name-value pair.
        /// </summary>
        /// <param name="request">A <see cref="System.Net.HttpWebRequest"/> object.</param>
        /// <param name="name">A string containing the metadata name.</param>
        /// <param name="value">A string containing the metadata value.</param>
        public static void AddMetadata(HttpWebRequest request, string name, string value)
        {
            HttpWebRequestFactory.AddMetadata(request, name, value);
        }

        /// <summary>
        /// Constructs a web request to return a listing of all queues in this storage account.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Queue service endpoint.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="listingContext">A <see cref="ListingContext"/> object.</param>
        /// <param name="detailsIncluded">A <see cref="QueueListingDetails"/> enumeration value that indicates whether to return queue metadata with the listing.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest List(Uri uri, int? timeout, ListingContext listingContext, QueueListingDetails detailsIncluded, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.List(uri, timeout, listingContext, detailsIncluded, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to return a listing of all queues in this storage account.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Queue service endpoint.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="listingContext">A <see cref="ListingContext"/> object.</param>
        /// <param name="detailsIncluded">A <see cref="QueueListingDetails"/> enumeration value that indicates whether to return queue metadata with the listing.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest List(Uri uri, int? timeout, ListingContext listingContext, QueueListingDetails detailsIncluded, bool useVersionHeader, OperationContext operationContext)
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

            if ((detailsIncluded & QueueListingDetails.Metadata) != 0)
            {
                builder.Add("include", "metadata");
            }

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the ACL for a queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetAcl(Uri uri, int? timeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.GetAcl(uri, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to return the ACL for a queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetAcl(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.GetAcl(uri, null, timeout, useVersionHeader, operationContext);
            return request;
        }

                /// <summary>
        /// Constructs a web request to set the ACL for a queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetAcl(Uri uri, int? timeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.SetAcl(uri, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to set the ACL for a queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetAcl(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.SetAcl(uri, null, timeout, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Constructs a web request to add a message for a queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="timeToLiveInSeconds">The message time-to-live, in seconds.</param>
        /// <param name="visibilityTimeoutInSeconds">The length of time during which the message will be invisible, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest AddMessage(Uri uri, int? timeout, int? timeToLiveInSeconds, int? visibilityTimeoutInSeconds, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.AddMessage(uri, timeout, timeToLiveInSeconds, visibilityTimeoutInSeconds, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to add a message for a queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="timeToLiveInSeconds">The message time-to-live, in seconds.</param>
        /// <param name="visibilityTimeoutInSeconds">The length of time during which the message will be invisible, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest AddMessage(Uri uri, int? timeout, int? timeToLiveInSeconds, int? visibilityTimeoutInSeconds, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            if (timeToLiveInSeconds != null)
            {
                builder.Add(Constants.QueryConstants.MessageTimeToLive, timeToLiveInSeconds.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (visibilityTimeoutInSeconds != null)
            {
                builder.Add(Constants.QueryConstants.VisibilityTimeout, visibilityTimeoutInSeconds.Value.ToString(CultureInfo.InvariantCulture));
            }

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Post, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Constructs a web request to update a message.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the message to update.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="popReceipt">A string specifying the pop receipt of the message.</param>
        /// <param name="visibilityTimeoutInSeconds">The length of time during which the message will be invisible, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest UpdateMessage(Uri uri, int? timeout, string popReceipt, int visibilityTimeoutInSeconds, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.UpdateMessage(uri, timeout, popReceipt, visibilityTimeoutInSeconds, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to update a message.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the message to update.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="popReceipt">A string specifying the pop receipt of the message.</param>
        /// <param name="visibilityTimeoutInSeconds">The length of time during which the message will be invisible, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest UpdateMessage(Uri uri, int? timeout, string popReceipt, int visibilityTimeoutInSeconds, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            builder.Add(Constants.QueryConstants.PopReceipt, popReceipt);
            builder.Add(Constants.QueryConstants.VisibilityTimeout, visibilityTimeoutInSeconds.ToString(CultureInfo.InvariantCulture));

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Constructs a web request to update a message.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the message to update.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="popReceipt">A string specifying the pop receipt of the message.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest DeleteMessage(Uri uri, int? timeout, string popReceipt, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.DeleteMessage(uri, timeout, popReceipt, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to update a message.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the message to update.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="popReceipt">A string specifying the pop receipt of the message.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest DeleteMessage(Uri uri, int? timeout, string popReceipt, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.QueryConstants.PopReceipt, popReceipt);

            HttpWebRequest request = HttpWebRequestFactory.Delete(uri, builder, timeout, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Constructs a web request to get messages from a queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="numberOfMessages">An integer specifying the number of messages to get.</param>
        /// <param name="visibilityTimeout">A <see cref="TimeSpan"/> value specifying the visibility timeout.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetMessages(Uri uri, int? timeout, int numberOfMessages, TimeSpan? visibilityTimeout, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.GetMessages(uri, timeout, numberOfMessages, visibilityTimeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to get messages from a queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="numberOfMessages">An integer specifying the number of messages to get.</param>
        /// <param name="visibilityTimeout">A <see cref="TimeSpan"/> value specifying the visibility timeout.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetMessages(Uri uri, int? timeout, int numberOfMessages, TimeSpan? visibilityTimeout, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            builder.Add(Constants.QueryConstants.NumOfMessages, numberOfMessages.ToString(CultureInfo.InvariantCulture));

            if (visibilityTimeout != null)
            {
                builder.Add(Constants.QueryConstants.VisibilityTimeout, visibilityTimeout.Value.RoundUpToSeconds().ToString(CultureInfo.InvariantCulture));
            }

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Constructs a web request to peek messages from a queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="numberOfMessages">An integer specifying the number of messages to peek.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest PeekMessages(Uri uri, int? timeout, int numberOfMessages, OperationContext operationContext)
        {
            return QueueHttpWebRequestFactory.PeekMessages(uri, timeout, numberOfMessages, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to peek messages from a queue.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="numberOfMessages">An integer specifying the number of messages to peek.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest PeekMessages(Uri uri, int? timeout, int numberOfMessages, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = new UriQueryBuilder();
            builder.Add(Constants.HeaderConstants.PeekOnly, Constants.HeaderConstants.TrueHeader);

            builder.Add(Constants.QueryConstants.NumOfMessages, numberOfMessages.ToString(CultureInfo.InvariantCulture));

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }
    }
}
