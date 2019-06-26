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
    using Microsoft.Azure.Storage.Core.Util;
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
        /// <param name="properties">The properties to set for the directory.</param>
        /// <param name="filePermissionToSet">The file permissions to set for the directory.</param>
        /// <param name="content">HttpContent for the request</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage Create(
            Uri uri,
            int? timeout,
            FileDirectoryProperties properties,
            string filePermissionToSet,
            HttpContent content,
            OperationContext operationContext,
            ICanonicalizer canonicalizer,
            StorageCredentials credentials)
        {
            UriQueryBuilder directoryBuilder = GetDirectoryUriQueryBuilder();
            StorageRequestMessage request = HttpRequestMessageFactory.Create(uri, timeout, directoryBuilder, content, operationContext, canonicalizer, credentials);

            AddFilePermissionOrFilePermissionKey(request, filePermissionToSet, properties, Constants.HeaderConstants.FilePermissionInherit);
            AddNtfsFileAttributes(request, properties, Constants.HeaderConstants.FileAttributesNone);
            AddCreationTime(request, properties, Constants.HeaderConstants.FileTimeNow);
            AddLastWriteTime(request, properties, Constants.HeaderConstants.FileTimeNow);

            return request;
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
        /// Constructs a web request to set system properties for a directory.
        /// </summary>
        /// <param name="uri">The absolute URI to the file.</param>
        /// <param name="timeout">The server timeout interval.</param>
        /// <param name="properties">The directory's properties.</param>
        /// <param name="filePermissionToSet">The file's file permission</param>
        /// <param name="content">HttpContent for the request</param>
        /// <param name="operationContext">An <see cref="OperationContext" /> object for tracking the current operation.</param>
        /// <param name="canonicalizer">A canonicalizer that converts HTTP request data into a standard form appropriate for signing.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object providing credentials for the request.</param>
        /// <returns>A web request to use to perform the operation.</returns>
        public static StorageRequestMessage SetProperties(
            Uri uri,
            int? timeout,
            FileDirectoryProperties properties,
            string filePermissionToSet,
            HttpContent content,
            OperationContext operationContext,
            ICanonicalizer canonicalizer,
            StorageCredentials credentials)
        {
            CommonUtility.AssertNotNull("properties", properties);
            UriQueryBuilder builder = GetDirectoryUriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "properties");

            StorageRequestMessage request = HttpRequestMessageFactory.CreateRequestMessage(HttpMethod.Put, uri, timeout, builder, content, operationContext, canonicalizer, credentials);

            AddFilePermissionOrFilePermissionKey(request, filePermissionToSet, properties, Constants.HeaderConstants.Preserve);
            AddNtfsFileAttributes(request, properties, Constants.HeaderConstants.Preserve);
            AddCreationTime(request, properties, Constants.HeaderConstants.Preserve);
            AddLastWriteTime(request, properties, Constants.HeaderConstants.Preserve);

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

        /// <summary>
        /// Adds the File Permission or File Permission Key to a StorageRequest.
        /// </summary>
        /// <param name="request">The <see cref="StorageRequestMessage"/></param>
        /// <param name="filePermissionToSet">The File Permission</param>
        /// <param name="properties">The <see cref="FileDirectoryProperties"/></param>
        /// <param name="defaultValue">The default value to set if fileermissionToSet and properties.filePermissionKeyToSet are null</param>
        private static void AddFilePermissionOrFilePermissionKey(
            StorageRequestMessage request,
            string filePermissionToSet,
            FileDirectoryProperties properties,
            string defaultValue)
        {
            if (filePermissionToSet == null && properties?.filePermissionKeyToSet == null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FilePermission, defaultValue);
            }
            else if (filePermissionToSet != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FilePermission, filePermissionToSet);
            }
            else
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FilePermissionKey, properties.filePermissionKeyToSet);
            }
        }

        /// <summary>
        /// Adds the <see cref="CloudFileNtfsAttributes"/> to the <see cref="StorageRequestMessage"/>
        /// </summary>
        /// <param name="request">The <see cref="StorageRequestMessage"/></param>
        /// <param name="properties">The <see cref="FileDirectoryProperties"/></param>
        /// <param name="defaultValue">The default value to set if properties.ntfsAttributesToSet is null</param>
        private static void AddNtfsFileAttributes(
            StorageRequestMessage request,
            FileDirectoryProperties properties,
            string defaultValue)
        {
            if (properties?.ntfsAttributesToSet != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileAttributes, CloudFileNtfsAttributesHelper.ToString(properties.ntfsAttributesToSet.Value));
            }
            else
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileAttributes, defaultValue);
            }
        }

        /// <summary>
        /// Adds the File Creation Time to the <see cref="StorageRequestMessage"/>
        /// </summary>
        /// <param name="request">The <see cref="StorageRequestMessage"/></param>
        /// <param name="properties">The <see cref="FileDirectoryProperties"/></param>
        /// <param name="defaultValue">The value to set if properties.creationTimeToSet is null</param>
        private static void AddCreationTime(
            StorageRequestMessage request,
            FileDirectoryProperties properties,
            string defaultValue)
        {
            if (properties?.creationTimeToSet != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileCreationTime, Request.ConvertDateTimeToSnapshotString(properties.creationTimeToSet.Value));
            }
            else
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileCreationTime, defaultValue);
            }
        }

        /// <summary>
        /// Adds the File Last Write Time to the <see cref="StorageRequestMessage"/>
        /// </summary>
        /// <param name="request">The <see cref="StorageRequestMessage"/></param>
        /// <param name="properties">The <see cref="FileDirectoryProperties"/></param>
        /// <param name="defaultValue">The default value to set if properties.lastWriteTimeToSet is null</param>
        private static void AddLastWriteTime(
            StorageRequestMessage request,
            FileDirectoryProperties properties,
            string defaultValue)
        {
            if (properties?.lastWriteTimeToSet != null)
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileLastWriteTime, Request.ConvertDateTimeToSnapshotString(properties.lastWriteTimeToSet.Value));
            }
            else
            {
                request.AddOptionalHeader(Constants.HeaderConstants.FileLastWriteTime, defaultValue);
            }
        }
    }
}
