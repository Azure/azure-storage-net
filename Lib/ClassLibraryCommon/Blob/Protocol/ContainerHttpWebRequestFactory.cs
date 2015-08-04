//-----------------------------------------------------------------------
// <copyright file="ContainerHttpWebRequestFactory.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Text;

    /// <summary>
    /// A factory class for constructing a web request to manage containers in the Blob service.
    /// </summary>
    public static class ContainerHttpWebRequestFactory
    {
        /// <summary>
        /// Constructs a web request to create a new container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Create(Uri uri, int? timeout, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.Create(uri, timeout, true /* useVersionHeader */, operationContext, BlobContainerPublicAccessType.Off);
        }

        /// <summary>
        /// Constructs a web request to create a new container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Create(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.Create(uri, timeout, useVersionHeader, operationContext, BlobContainerPublicAccessType.Off);
        }

        /// <summary>
        /// Constructs a web request to create a new container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="accessType">An <see cref="BlobContainerPublicAccessType"/> object that specifies whether data in the container may be accessed publicly and the level of access.</param>                
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Create(Uri uri, int? timeout, OperationContext operationContext, BlobContainerPublicAccessType accessType)
        {
            return ContainerHttpWebRequestFactory.Create(uri, timeout, true /* useVersionHeader */, operationContext, accessType);
        }

        /// <summary>
        /// Constructs a web request to create a new container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="accessType">An <see cref="BlobContainerPublicAccessType"/> object that specifies whether data in the container may be accessed publicly and the level of access.</param>                
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Create(Uri uri, int? timeout, bool useVersionHeader, OperationContext operationContext, BlobContainerPublicAccessType accessType)
        {
            UriQueryBuilder containerBuilder = GetContainerUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.Create(uri, timeout, containerBuilder, useVersionHeader, operationContext);

            if (accessType != BlobContainerPublicAccessType.Off)
            {
                request.Headers.Add(Constants.HeaderConstants.ContainerPublicAccessType, accessType.ToString().ToLower(CultureInfo.InvariantCulture));
            }

            return request;
        }
        
        /// <summary>
        /// Constructs a web request to delete the container and all of the blobs within it.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Delete(Uri uri, int? timeout, AccessCondition accessCondition, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.Delete(uri, timeout, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to delete the container and all of the blobs within it.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Delete(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder containerBuilder = GetContainerUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.Delete(uri, containerBuilder, timeout, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the user-defined metadata for this container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.GetMetadata(uri, timeout, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Generates a web request to return the user-defined metadata for this container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder containerBuilder = GetContainerUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.GetMetadata(uri, timeout, containerBuilder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return the properties and user-defined metadata for this container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetProperties(Uri uri, int? timeout, AccessCondition accessCondition, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.GetProperties(uri, timeout, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Generates a web request to return the properties and user-defined metadata for this container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetProperties(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder containerBuilder = GetContainerUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.GetProperties(uri, timeout, containerBuilder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to set user-defined metadata for the container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.SetMetadata(uri, timeout, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Generates a web request to set user-defined metadata for the container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetMetadata(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder containerBuilder = GetContainerUriQueryBuilder();
            HttpWebRequest request = HttpWebRequestFactory.SetMetadata(uri, timeout, containerBuilder, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to use to acquire, renew, change, release or break the lease for the container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="action">A <see cref="LeaseAction"/> enumeration value indicating the lease action to perform.</param>
        /// <param name="proposedLeaseId">A string specifying the lease ID to propose for the result of an acquire or change operation,
        /// or <c>null</c> if no ID is proposed for an acquire operation. This parameter should be <c>null</c> for renew, release, and break operations.</param>
        /// <param name="leaseDuration">The lease duration, in seconds, for acquire operations.
        /// If this is -1 then an infinite duration is specified. This should be <c>null</c> for renew, change, release, and break operations.</param>
        /// <param name="leaseBreakPeriod">The amount of time to wait, in seconds, after a break operation before the lease is broken.
        /// If this is <c>null</c> then the default time is used. This should be <c>null</c> for acquire, renew, change, and release operations.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Lease(Uri uri, int? timeout, LeaseAction action, string proposedLeaseId, int? leaseDuration, int? leaseBreakPeriod, AccessCondition accessCondition, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.Lease(uri, timeout, action, proposedLeaseId, leaseDuration, leaseBreakPeriod, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Generates a web request to use to acquire, renew, change, release or break the lease for the container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="action">A <see cref="LeaseAction"/> enumeration value indicating the lease action to perform.</param>
        /// <param name="proposedLeaseId">A string specifying the lease ID to propose for the result of an acquire or change operation,
        /// or <c>null</c> if no ID is proposed for an acquire operation. This parameter should be <c>null</c> for renew, release, and break operations.</param>
        /// <param name="leaseDuration">The lease duration, in seconds, for acquire operations.
        /// If this is -1 then an infinite duration is specified. This should be <c>null</c> for renew, change, release, and break operations.</param>
        /// <param name="leaseBreakPeriod">The amount of time to wait, in seconds, after a break operation before the lease is broken.
        /// If this is <c>null</c> then the default time is used. This should be <c>null</c> for acquire, renew, change, and release operations.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest Lease(Uri uri, int? timeout, LeaseAction action, string proposedLeaseId, int? leaseDuration, int? leaseBreakPeriod, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = GetContainerUriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "lease");

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Put, uri, timeout, builder, useVersionHeader, operationContext);

            // Add Headers
            BlobHttpWebRequestFactory.AddLeaseAction(request, action);
            BlobHttpWebRequestFactory.AddLeaseDuration(request, leaseDuration);
            BlobHttpWebRequestFactory.AddProposedLeaseId(request, proposedLeaseId);
            BlobHttpWebRequestFactory.AddLeaseBreakPeriod(request, leaseBreakPeriod);

            request.ApplyAccessCondition(accessCondition);
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
        /// Constructs a web request to return a listing of all containers in this storage account.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Blob service endpoint.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="listingContext">A <see cref="ListingContext"/> object.</param>
        /// <param name="detailsIncluded">A <see cref="ContainerListingDetails"/> enumeration value that indicates whether to return container metadata with the listing.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request for the specified operation.</returns>
        public static HttpWebRequest List(Uri uri, int? timeout, ListingContext listingContext, ContainerListingDetails detailsIncluded, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.List(uri, timeout, listingContext, detailsIncluded, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to return a listing of all containers in this storage account.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Blob service endpoint.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="listingContext">A <see cref="ListingContext"/> object.</param>
        /// <param name="detailsIncluded">A <see cref="ContainerListingDetails"/> enumeration value that indicates whether to return container metadata with the listing.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A web request for the specified operation.</returns>
        public static HttpWebRequest List(Uri uri, int? timeout, ListingContext listingContext, ContainerListingDetails detailsIncluded, bool useVersionHeader, OperationContext operationContext)
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

            if ((detailsIncluded & ContainerListingDetails.Metadata) != 0)
            {
                builder.Add("include", "metadata");
            }

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Constructs a web request to return the ACL for a container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetAcl(Uri uri, int? timeout, AccessCondition accessCondition, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.GetAcl(uri, timeout, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to return the ACL for a container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetAcl(Uri uri, int? timeout, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.GetAcl(uri, GetContainerUriQueryBuilder(), timeout, useVersionHeader, operationContext);
            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Constructs a web request to set the ACL for a container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="publicAccess">The type of public access to allow for the container.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetAcl(Uri uri, int? timeout, BlobContainerPublicAccessType publicAccess, AccessCondition accessCondition, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.SetAcl(uri, timeout, publicAccess, accessCondition, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to set the ACL for a container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="publicAccess">The type of public access to allow for the container.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetAcl(Uri uri, int? timeout, BlobContainerPublicAccessType publicAccess, AccessCondition accessCondition, bool useVersionHeader, OperationContext operationContext)
        {
            HttpWebRequest request = HttpWebRequestFactory.SetAcl(uri, GetContainerUriQueryBuilder(), timeout, useVersionHeader, operationContext);

            if (publicAccess != BlobContainerPublicAccessType.Off)
            {
                request.Headers.Add(Constants.HeaderConstants.ContainerPublicAccessType, publicAccess.ToString().ToLower());
            }

            request.ApplyAccessCondition(accessCondition);
            return request;
        }

        /// <summary>
        /// Generates a web request to return a listing of all blobs in the container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="listingContext">A <see cref="ListingContext"/> object.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest ListBlobs(Uri uri, int? timeout, BlobListingContext listingContext, OperationContext operationContext)
        {
            return ContainerHttpWebRequestFactory.ListBlobs(uri, timeout, listingContext, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Generates a web request to return a listing of all blobs in the container.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI to the container.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="listingContext">A <see cref="ListingContext"/> object.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest ListBlobs(Uri uri, int? timeout, BlobListingContext listingContext, bool useVersionHeader, OperationContext operationContext)
        {
            UriQueryBuilder builder = ContainerHttpWebRequestFactory.GetContainerUriQueryBuilder();
            builder.Add(Constants.QueryConstants.Component, "list");

            if (listingContext != null)
            {
                if (listingContext.Prefix != null)
                {
                    builder.Add("prefix", listingContext.Prefix);
                }

                if (listingContext.Delimiter != null)
                {
                    builder.Add("delimiter", listingContext.Delimiter);
                }

                if (listingContext.Marker != null)
                {
                    builder.Add("marker", listingContext.Marker);
                }

                if (listingContext.MaxResults.HasValue)
                {
                    builder.Add("maxresults", listingContext.MaxResults.ToString());
                }

                if (listingContext.Details != BlobListingDetails.None)
                {
                    StringBuilder sb = new StringBuilder();

                    bool started = false;

                    if ((listingContext.Details & BlobListingDetails.Snapshots) == BlobListingDetails.Snapshots)
                    {
                        if (!started)
                        {
                            started = true;
                        }
                        else
                        {
                            sb.Append(",");
                        }

                        sb.Append("snapshots");
                    }

                    if ((listingContext.Details & BlobListingDetails.UncommittedBlobs) == BlobListingDetails.UncommittedBlobs)
                    {
                        if (!started)
                        {
                            started = true;
                        }
                        else
                        {
                            sb.Append(",");
                        }

                        sb.Append("uncommittedblobs");
                    }

                    if ((listingContext.Details & BlobListingDetails.Metadata) == BlobListingDetails.Metadata)
                    {
                        if (!started)
                        {
                            started = true;
                        }
                        else
                        {
                            sb.Append(",");
                        }

                        sb.Append("metadata");
                    }

                    if ((listingContext.Details & BlobListingDetails.Copy) == BlobListingDetails.Copy)
                    {
                        if (!started)
                        {
                            started = true;
                        }
                        else
                        {
                            sb.Append(",");
                        }

                        sb.Append("copy");
                    }

                    builder.Add("include", sb.ToString());
                }
            }

            HttpWebRequest request = HttpWebRequestFactory.CreateWebRequest(WebRequestMethods.Http.Get, uri, timeout, builder, useVersionHeader, operationContext);
            return request;
        }

        /// <summary>
        /// Gets the container Uri query builder.
        /// </summary>
        /// <returns>A <see cref="UriQueryBuilder"/> for the container.</returns>
        internal static UriQueryBuilder GetContainerUriQueryBuilder()
        {
            UriQueryBuilder uriBuilder = new UriQueryBuilder();
            uriBuilder.Add(Constants.QueryConstants.ResourceType, "container");
            return uriBuilder;
        }
    }
}
