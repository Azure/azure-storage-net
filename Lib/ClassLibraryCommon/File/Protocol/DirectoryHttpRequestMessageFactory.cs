// -----------------------------------------------------------------------------------------
// <copyright file="DirectoryHttpRequestMessageFactory.cs" company="Microsoft">
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
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Auth;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    internal static class DirectoryHttpRequestMessageFactory
    {
        /// <summary>
        /// Constructs a web request to create a new directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the directory.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Create(Uri uri, int? timeout, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            return HttpRequestMessageFactory.Create(uri, timeout, directoryBuilder, content, operationContext, canonicalizer, credentials);
        }

        /// <summary>
        /// Constructs a web request to delete the directory and all of the files within it.
        /// </summary>
        /// <param name="uri">The absolute URI to the directory.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Delete(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            StorageRequestMessage request = HttpRequestMessageFactory.Delete(uri, timeout, directoryBuilder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the properties and user-defined metadata for this directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the directory.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="shareSnapshot">A <see cref="DateTimeOffset"/> specifying the share snapshot timestamp, if the share is a snapshot.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage GetProperties(Uri uri, int? timeout, DateTimeOffset? shareSnapshot, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            DirectoryHttpRequestMessageFactory.AddShareSnapshot(directoryBuilder, shareSnapshot);

            StorageRequestMessage request = HttpRequestMessageFactory.GetProperties(uri, timeout, directoryBuilder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the user-defined metadata for this directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the directory.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="shareSnapshot">A <see cref="DateTimeOffset"/> specifying the share snapshot timestamp, if the share is a snapshot.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage GetMetadata(Uri uri, int? timeout, DateTimeOffset? shareSnapshot, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            DirectoryHttpRequestMessageFactory.AddShareSnapshot(directoryBuilder, shareSnapshot);

            StorageRequestMessage request = HttpRequestMessageFactory.GetMetadata(uri, timeout, directoryBuilder, content, operationContext, canonicalizer, credentials);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return a listing of all files and subdirectories in the directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the share.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="shareSnapshot">A <see cref="DateTimeOffset"/> specifying the share snapshot timestamp, if the share is a snapshot.</param>
        /// <param name="listingContext">A set of parameters for the listing operation.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage List(Uri uri, int? timeout, DateTimeOffset? shareSnapshot, FileListingContext listingContext, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            DirectoryHttpRequestMessageFactory.AddShareSnapshot(directoryBuilder, shareSnapshot);
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
                
                if (listingContext.Prefix != null)
                {
                    directoryBuilder.Add("prefix", listingContext.Prefix);
                }
            }

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Get, uri, timeout, directoryBuilder, content, operationContext, canonicalizer, credentials);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set user-defined metadata for the directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the directory.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="accessCondition">The access condition to apply to the request.</param>
        /// <returns>A web request for performing the operation.</returns>
        public static StorageRequestMessage SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, HttpContent content, OperationContext operationContext, ICanonicalizer canonicalizer, StorageCredentials credentials)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            StorageRequestMessage request = HttpRequestMessageFactory.SetMetadata(uri, timeout, directoryBuilder, content, operationContext, canonicalizer, credentials);
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
