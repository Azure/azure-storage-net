// -----------------------------------------------------------------------------------------
// <copyright file="CloudBlobClient.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Auth.Protocol;
    using Microsoft.WindowsAzure.Storage.Blob.Protocol;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
#if NETCORE
    using System.Threading;
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
    using Windows.Foundation.Metadata;
    using System.Threading;
#endif

    public partial class CloudBlobClient
    {
        /// <summary>
        /// Gets or sets the authentication scheme to use to sign HTTP requests.
        /// </summary>
        public AuthenticationScheme AuthenticationScheme
        {
            get
            {
                return this.authenticationScheme;
            }

            set
            {
                this.authenticationScheme = value;
            }
        }

        /// <summary>
        /// Returns a result segment containing a collection of containers.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param>
        /// <returns>A result segment of containers.</returns>
        [DoesServiceRequest]
        public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(BlobContinuationToken currentToken)
        {
            return this.ListContainersSegmentedAsync(null /* prefix */, ContainerListingDetails.None, null /* maxResults */, currentToken, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of containers.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param>
        /// <returns>A result segment of containers.</returns>
        [DoesServiceRequest]
        public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix, BlobContinuationToken currentToken)
        {
            return this.ListContainersSegmentedAsync(prefix, ContainerListingDetails.None, null /* maxResults */, currentToken, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of containers
        /// whose names begin with the specified prefix.
        /// </summary>
        /// <param name="prefix">The container name prefix.</param>
        /// <param name="detailsIncluded">A value that indicates whether to return container metadata with the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A result segment of containers.</returns>
        [DoesServiceRequest]
        public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix, ContainerListingDetails detailsIncluded, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ListContainersSegmentedAsync(prefix, detailsIncluded, maxResults, currentToken, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a result segment containing a collection of containers
        /// whose names begin with the specified prefix.
        /// </summary>
        /// <param name="prefix">The container name prefix.</param>
        /// <param name="detailsIncluded">A value that indicates whether to return container metadata with the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A result segment of containers.</returns>
        [DoesServiceRequest]
        public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix, ContainerListingDetails detailsIncluded, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this);
                ResultSegment<CloudBlobContainer> resultSegment = await Executor.ExecuteAsync(
                    this.ListContainersImpl(prefix, detailsIncluded, currentToken, maxResults, modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken);

                return new ContainerResultSegment(resultSegment.Results, (BlobContinuationToken)resultSegment.ContinuationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// Returns a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="prefix">The container name prefix.</param>
        /// <param name="currentToken">The continuation token.</param>
        /// <returns>A result segment containing objects that implement <see cref="IListBlobItem"/>.</returns>
        [DoesServiceRequest]
        public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, BlobContinuationToken currentToken)
        {
            return this.ListBlobsSegmentedAsync(prefix, false, BlobListingDetails.None, null /* maxResults */, currentToken, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="prefix">The container name prefix.</param>
        /// <param name="useFlatBlobListing">Whether to list blobs in a flat listing, or whether to list blobs hierarchically, by virtual directory.</param>
        /// <param name="blobListingDetails">A <see cref="BlobListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A result segment containing objects that implement <see cref="IListBlobItem"/>.</returns>
        [DoesServiceRequest]
        public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext)
        {
            string containerName;
            string listingPrefix;
            CloudBlobClient.ParseUserPrefix(prefix, out containerName, out listingPrefix);

            CloudBlobContainer container = this.GetContainerReference(containerName);
            return container.ListBlobsSegmentedAsync(listingPrefix, useFlatBlobListing, blobListingDetails, maxResults, currentToken, options, operationContext);
        }

        /// <summary>
        /// Gets a reference to a blob from the service.
        /// </summary>
        /// <param name="blobUri">The URI of the blob.</param>
        /// <returns>A reference to the blob.</returns>
        [DoesServiceRequest]
        public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(Uri blobUri)
        {
            return this.GetBlobReferenceFromServerAsync(blobUri, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets a reference to a blob from the service.
        /// </summary>
        /// <param name="blobUri">The URI of the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the container. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A reference to the blob.</returns>
        [DoesServiceRequest]
        public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(Uri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("blobUri", blobUri);
            return this.GetBlobReferenceFromServerAsync(new StorageUri(blobUri), accessCondition, options, operationContext);
        }

        /// <summary>
        /// Gets a reference to a blob from the service.
        /// </summary>
        /// <param name="blobUri">The URI of the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the container. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A reference to the blob.</returns>
        [DoesServiceRequest]
        public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(StorageUri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.GetBlobReferenceFromServerAsync(blobUri, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets a reference to a blob from the service.
        /// </summary>
        /// <param name="blobUri">The URI of the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the container. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A reference to the blob.</returns>
        [DoesServiceRequest]
        public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(StorageUri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("blobUri", blobUri);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.GetBlobReferenceImpl(blobUri, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Core implementation for the ListContainers method.
        /// </summary>
        /// <param name="prefix">The container prefix.</param>
        /// <param name="detailsIncluded">The details included.</param>
        /// <param name="currentToken">The continuation token.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <returns>A <see cref="TaskSequence"/> that lists the containers.</returns>
        private RESTCommand<ResultSegment<CloudBlobContainer>> ListContainersImpl(string prefix, ContainerListingDetails detailsIncluded, BlobContinuationToken currentToken, int? maxResults, BlobRequestOptions options)
        {
            ListingContext listingContext = new ListingContext(prefix, maxResults)
            {
                Marker = currentToken != null ? currentToken.NextMarker : null
            };

            RESTCommand<ResultSegment<CloudBlobContainer>> getCmd = new RESTCommand<ResultSegment<CloudBlobContainer>>(this.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommonUtility.GetListingLocationMode(currentToken);
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ContainerHttpRequestMessageFactory.List(uri, serverTimeout, listingContext, detailsIncluded, cnt, ctx, this.GetCanonicalizer(), this.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    ListContainersResponse listContainersResponse = new ListContainersResponse(cmd.ResponseStream);
                    List<CloudBlobContainer> containersList = listContainersResponse.Containers.Select(item => new CloudBlobContainer(item.Properties, item.Metadata, item.Name, this)).ToList();
                    BlobContinuationToken continuationToken = null;
                    if (listContainersResponse.NextMarker != null)
                    {
                        continuationToken = new BlobContinuationToken()
                        {
                            NextMarker = listContainersResponse.NextMarker,
                            TargetLocation = cmd.CurrentResult.TargetLocation,
                        };
                    }

                    return new ResultSegment<CloudBlobContainer>(containersList)
                    {
                        ContinuationToken = continuationToken,
                    };
                });
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the FetchAttributes method. The attributes are updated immediately.
        /// </summary>
        /// <param name="blobUri">The URI of the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that fetches the attributes.</returns>
        private RESTCommand<CloudBlob> GetBlobReferenceFromServerImpl(StorageUri blobUri, AccessCondition accessCondition, BlobRequestOptions options)
        {
            // If the blob Uri contains SAS credentials, we need to use those
            // credentials instead of this service client's stored credentials.
            StorageCredentials parsedCredentials;
            DateTimeOffset? parsedSnapshot;
            blobUri = NavigationHelper.ParseBlobQueryAndVerify(blobUri, out parsedCredentials, out parsedSnapshot);
            CloudBlobClient client = parsedCredentials != null ? new CloudBlobClient(this.StorageUri, parsedCredentials) : this;

            RESTCommand<CloudBlob> getCmd = new RESTCommand<CloudBlob>(client.Credentials, blobUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetProperties(uri, serverTimeout, parsedSnapshot, accessCondition, cnt, ctx, client.GetCanonicalizer(), client.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
                BlobAttributes attributes = new BlobAttributes()
                {
                    StorageUri = blobUri,
                    SnapshotTime = parsedSnapshot,
                };

                CloudBlob.UpdateAfterFetchAttributes(attributes, resp, false);

                switch (attributes.Properties.BlobType)
                {
                    case BlobType.BlockBlob:
                        return new CloudBlockBlob(attributes, client);

                    case BlobType.PageBlob:
                        return new CloudPageBlob(attributes, client);

                    case BlobType.AppendBlob:
                        return new CloudAppendBlob(attributes, client);

                    default:
                        throw new InvalidOperationException();
                }
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the FetchAttributes method. The attributes are updated immediately.
        /// </summary>
        /// <param name="blobUri">The URI of the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that fetches the attributes.</returns>
        private RESTCommand<ICloudBlob> GetBlobReferenceImpl(StorageUri blobUri, AccessCondition accessCondition, BlobRequestOptions options)
        {
            // If the blob Uri contains SAS credentials, we need to use those
            // credentials instead of this service client's stored credentials.
            StorageCredentials parsedCredentials;
            DateTimeOffset? parsedSnapshot;
            blobUri = NavigationHelper.ParseBlobQueryAndVerify(blobUri, out parsedCredentials, out parsedSnapshot);
            CloudBlobClient client = parsedCredentials != null ? new CloudBlobClient(this.StorageUri, parsedCredentials) : this;

            RESTCommand<ICloudBlob> getCmd = new RESTCommand<ICloudBlob>(client.Credentials, blobUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetProperties(uri, serverTimeout, parsedSnapshot, accessCondition, cnt, ctx, client.GetCanonicalizer(), client.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
                BlobAttributes attributes = new BlobAttributes()
                {
                    StorageUri = blobUri,
                    SnapshotTime = parsedSnapshot,
                };

                CloudBlob.UpdateAfterFetchAttributes(attributes, resp, false);

                switch (attributes.Properties.BlobType)
                {
                    case BlobType.BlockBlob:
                        return new CloudBlockBlob(attributes, client);

                    case BlobType.PageBlob:
                        return new CloudPageBlob(attributes, client);

                    case BlobType.AppendBlob:
                        return new CloudAppendBlob(attributes, client);

                    default:
                        throw new InvalidOperationException();
                }
            };

            return getCmd;
        }

#region Analytics

        /// <summary>
        /// Gets the properties of the blob service.
        /// </summary>
        /// <returns>The blob service properties.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync()
        {
            return this.GetServicePropertiesAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets the properties of the blob service.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The blob service properties.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync(BlobRequestOptions options, OperationContext operationContext)
        {
            return this.GetServicePropertiesAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the properties of the blob service.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The blob service properties.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync(BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(
                async () => await Executor.ExecuteAsync(
                    this.GetServicePropertiesImpl(modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken), cancellationToken);
        }

        private RESTCommand<ServiceProperties> GetServicePropertiesImpl(BlobRequestOptions requestOptions)
        {
            RESTCommand<ServiceProperties> retCmd = new RESTCommand<ServiceProperties>(this.Credentials, this.StorageUri);

            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetServiceProperties(uri, serverTimeout, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);

            retCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task.Factory.StartNew(() => BlobHttpResponseParsers.ReadServiceProperties(cmd.ResponseStream));
            };

            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }

        /// <summary>
        /// Gets the properties of the blob service.
        /// </summary>
        /// <param name="properties">The blob service properties.</param>
        /// <returns>The properties of the blob service.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties)
        {
            return this.SetServicePropertiesAsync(properties, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets the properties of the blob service.
        /// </summary>
        /// <param name="properties">The blob service properties.</param>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The properties of the blob service.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties, BlobRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.SetServicePropertiesAsync(properties, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the properties of the blob service.
        /// </summary>
        /// <param name="properties">The blob service properties.</param>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The properties of the blob service.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties, BlobRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, this);
            operationContext = operationContext ?? new OperationContext();
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                 this.SetServicePropertiesImpl(properties, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        private RESTCommand<NullType> SetServicePropertiesImpl(ServiceProperties properties, BlobRequestOptions requestOptions)
        {
            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            try
            {
                properties.WriteServiceProperties(memoryStream);
            }
            catch (InvalidOperationException invalidOpException)
            {
                throw new ArgumentException(invalidOpException.Message, "properties");
            }

            RESTCommand<NullType> retCmd = new RESTCommand<NullType>(this.Credentials, this.StorageUri);
            requestOptions.ApplyToStorageCommand(retCmd);
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.SetServiceProperties(uri, serverTimeout, cnt, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null /* md5 */, cmd, ctx);
            retCmd.StreamToDispose = memoryStream;
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, null /* retVal */, cmd, ex);
            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }

        /// <summary>
        /// Gets service stats for the Blob service.
        /// </summary>
        /// <returns>The blob service stats.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync()
        {
            return this.GetServiceStatsAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets service stats for the Blob service.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The blob service stats.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync(BlobRequestOptions options, OperationContext operationContext)
        {
            return this.GetServiceStatsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets service stats for the Blob service.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The blob service stats.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync(BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(
                async () => await Executor.ExecuteAsync(
                    this.GetServiceStatsImpl(modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken), cancellationToken);
        }

        private RESTCommand<ServiceStats> GetServiceStatsImpl(BlobRequestOptions requestOptions)
        {
            if (RetryPolicies.LocationMode.PrimaryOnly == requestOptions.LocationMode)
            {
                throw new InvalidOperationException(SR.GetServiceStatsInvalidOperation);
            }  

            RESTCommand<ServiceStats> retCmd = new RESTCommand<ServiceStats>(this.Credentials, this.StorageUri);
            requestOptions.ApplyToStorageCommand(retCmd);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetServiceStats(uri, serverTimeout, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            retCmd.PostProcessResponse = (cmd, resp, ctx) => Task.Factory.StartNew(() => BlobHttpResponseParsers.ReadServiceStats(cmd.ResponseStream));
            return retCmd;
        }
#endregion
    }
}
