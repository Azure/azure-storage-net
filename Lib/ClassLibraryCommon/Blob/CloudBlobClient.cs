//-----------------------------------------------------------------------
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
//-----------------------------------------------------------------------

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
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class CloudBlobClient
    {
        private IAuthenticationHandler authenticationHandler;

        /// <summary>
        /// Gets or sets the authentication scheme to use to sign HTTP requests.
        /// </summary>
        /// <remarks>
        /// This property is set only when Shared Key or Shared Key Lite credentials are used; it does not apply to authentication via a shared access signature 
        /// or anonymous access.
        /// </remarks>
        public AuthenticationScheme AuthenticationScheme
        {
            get
            {
                return this.authenticationScheme;
            }

            set
            {
                if (value != this.authenticationScheme)
                {
                    this.authenticationScheme = value;
                    this.authenticationHandler = null;
                }
            }
        }

        /// <summary>
        /// Gets the authentication handler used to sign HTTP requests.
        /// </summary>
        /// <value>The authentication handler.</value>
        internal IAuthenticationHandler AuthenticationHandler
        {
            get
            {
                IAuthenticationHandler result = this.authenticationHandler;
                if (result == null)
                {
                    if (this.Credentials.IsSharedKey)
                    {
                        result = new SharedKeyAuthenticationHandler(
                            this.GetCanonicalizer(),
                            this.Credentials,
                            this.Credentials.AccountName);
                    }
                    else
                    {
                        result = new NoOpAuthenticationHandler();
                    }

                    this.authenticationHandler = result;
                }

                return result;
            }
        }

#if SYNC
        /// <summary>
        /// Returns an enumerable collection of containers whose names 
        /// begin with the specified prefix and that are retrieved lazily.
        /// </summary>
        /// <param name="prefix">A string containing the container name prefix.</param>
        /// <param name="detailsIncluded">A <see cref="ContainerListingDetails"/> enumeration value that indicates whether to return container metadata with the listing.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of <see cref="CloudBlobContainer"/> objects that are retrieved lazily.</returns>
        [DoesServiceRequest]
        public virtual IEnumerable<CloudBlobContainer> ListContainers(string prefix = null, ContainerListingDetails detailsIncluded = ContainerListingDetails.None, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this);
            return CommonUtility.LazyEnumerable(
                token => this.ListContainersSegmentedCore(prefix, detailsIncluded, null /* maxResults */, (BlobContinuationToken)token, modifiedOptions, operationContext),
                long.MaxValue);
        }

        /// <summary>
        /// Returns a result segment containing a collection of <see cref="CloudBlobContainer"/> objects.
        /// </summary>
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <returns>A <see cref="ContainerResultSegment"/> object.</returns>
        [DoesServiceRequest]
        public virtual ContainerResultSegment ListContainersSegmented(BlobContinuationToken currentToken)
        {
            return this.ListContainersSegmented(null /* prefix */, ContainerListingDetails.None, null /* maxResults */, currentToken, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of <see cref="CloudBlobContainer"/> objects.
        /// </summary>
        /// <param name="prefix">A string containing the container name prefix.</param>
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <returns>A <see cref="ContainerResultSegment"/> object.</returns>
        [DoesServiceRequest]
        public virtual ContainerResultSegment ListContainersSegmented(string prefix, BlobContinuationToken currentToken)
        {
            return this.ListContainersSegmented(prefix, ContainerListingDetails.None, null /* maxResults */, currentToken, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of containers whose names begin with the specified prefix.
        /// </summary>
        /// <param name="prefix">A string containing the container name prefix.</param>
        /// <param name="detailsIncluded">A <see cref="ContainerListingDetails"/> enumeration value that indicates whether to return container metadata with the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="ContainerResultSegment"/> object.</returns>
        [DoesServiceRequest]
        public virtual ContainerResultSegment ListContainersSegmented(string prefix, ContainerListingDetails detailsIncluded, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this);
            ResultSegment<CloudBlobContainer> resultSegment = this.ListContainersSegmentedCore(prefix, detailsIncluded, maxResults, currentToken, modifiedOptions, operationContext);
            return new ContainerResultSegment(resultSegment.Results, (BlobContinuationToken)resultSegment.ContinuationToken);
        }

        /// <summary>
        /// Returns a result segment containing a collection of containers
        /// whose names begin with the specified prefix.
        /// </summary>
        /// <param name="prefix">A string containing the container name prefix.</param>
        /// <param name="detailsIncluded">A <see cref="ContainerListingDetails"/> enumeration value that indicates whether to return container metadata with the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="continuationToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="ResultSegment{T}"/> of type <see cref="CloudBlobContainer"/>.</returns>
        private ResultSegment<CloudBlobContainer> ListContainersSegmentedCore(string prefix, ContainerListingDetails detailsIncluded, int? maxResults, BlobContinuationToken continuationToken, BlobRequestOptions options, OperationContext operationContext)
        {
            return Executor.ExecuteSync(
                this.ListContainersImpl(prefix, detailsIncluded, continuationToken, maxResults, options),
                options.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous request to return a result segment containing a collection of containers.
        /// </summary>
        /// <param name="continuationToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginListContainersSegmented(BlobContinuationToken continuationToken, AsyncCallback callback, object state)
        {
            return this.BeginListContainersSegmented(null /* prefix */, ContainerListingDetails.None, null /* maxResults */, continuationToken, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to return a result segment containing a collection of containers.
        /// </summary>
        /// <param name="prefix">A string containing the container name prefix.</param>
        /// <param name="continuationToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginListContainersSegmented(string prefix, BlobContinuationToken continuationToken, AsyncCallback callback, object state)
        {
            return this.BeginListContainersSegmented(prefix, ContainerListingDetails.None, null /* maxResults */, continuationToken, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to return a result segment containing a collection of containers
        /// whose names begin with the specified prefix.
        /// </summary>
        /// <param name="prefix">A string containing the container name prefix.</param>
        /// <param name="detailsIncluded">A <see cref="ContainerListingDetails"/> enumeration value that indicates whether to return container metadata with the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="continuationToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginListContainersSegmented(string prefix, ContainerListingDetails detailsIncluded, int? maxResults, BlobContinuationToken continuationToken, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this);
            return Executor.BeginExecuteAsync(
                this.ListContainersImpl(prefix, detailsIncluded, continuationToken, maxResults, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to return a result segment containing a collection of containers.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="ContainerResultSegment"/> object.</returns>
        public virtual ContainerResultSegment EndListContainersSegmented(IAsyncResult asyncResult)
        {
            ResultSegment<CloudBlobContainer> resultSegment = Executor.EndExecuteAsync<ResultSegment<CloudBlobContainer>>(asyncResult);
            return new ContainerResultSegment(resultSegment.Results, (BlobContinuationToken)resultSegment.ContinuationToken);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of containers.
        /// </summary>
        /// <param name="continuationToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ContainerResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(BlobContinuationToken continuationToken)
        {
            return this.ListContainersSegmentedAsync(continuationToken, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of containers.
        /// </summary>
        /// <param name="continuationToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ContainerResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(BlobContinuationToken continuationToken, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginListContainersSegmented, this.EndListContainersSegmented, continuationToken, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of containers.
        /// </summary>
        /// <param name="prefix">A string containing the container name prefix.</param>
        /// <param name="continuationToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ContainerResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix, BlobContinuationToken continuationToken)
        {
            return this.ListContainersSegmentedAsync(prefix, continuationToken, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of containers.
        /// </summary>
        /// <param name="prefix">A string containing the container name prefix.</param>
        /// <param name="continuationToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ContainerResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix, BlobContinuationToken continuationToken, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginListContainersSegmented, this.EndListContainersSegmented, prefix, continuationToken, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of containers.
        /// </summary>
        /// <param name="prefix">A string containing the container name prefix.</param>
        /// <param name="detailsIncluded">A <see cref="ContainerListingDetails"/> enumeration value that indicates whether to return container metadata with the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="continuationToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ContainerResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix, ContainerListingDetails detailsIncluded, int? maxResults, BlobContinuationToken continuationToken, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ListContainersSegmentedAsync(prefix, detailsIncluded, maxResults, continuationToken, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of containers.
        /// </summary>
        /// <param name="prefix">A string containing the container name prefix.</param>
        /// <param name="detailsIncluded">A <see cref="ContainerListingDetails"/> enumeration value that indicates whether to return container metadata with the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="continuationToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ContainerResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix, ContainerListingDetails detailsIncluded, int? maxResults, BlobContinuationToken continuationToken, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginListContainersSegmented, this.EndListContainersSegmented, prefix, detailsIncluded, maxResults, continuationToken, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Returns an enumerable collection of blobs in the container, retrieved lazily.
        /// </summary>
        /// <param name="prefix">A string containing the blob name prefix.</param>
        /// <param name="useFlatBlobListing">A boolean value that specifies whether to list blobs in a flat listing, or whether to list blobs hierarchically, by virtual directory.</param>
        /// <param name="blobListingDetails">A <see cref="BlobListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="IListBlobItem"/> and are retrieved lazily.</returns>
        [DoesServiceRequest]
        public virtual IEnumerable<IListBlobItem> ListBlobs(string prefix, bool useFlatBlobListing = false, BlobListingDetails blobListingDetails = BlobListingDetails.None, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            string containerName;
            string listingPrefix;
            CloudBlobClient.ParseUserPrefix(prefix, out containerName, out listingPrefix);

            CloudBlobContainer container = this.GetContainerReference(containerName);
            return container.ListBlobs(listingPrefix, useFlatBlobListing, blobListingDetails, options, operationContext);
        }

        /// <summary>
        /// Returns a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="prefix">A string containing the blob name prefix, including the container name.</param>
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <returns>A <see cref="BlobResultSegment"/> object.</returns>
        [DoesServiceRequest]
        public virtual BlobResultSegment ListBlobsSegmented(string prefix, BlobContinuationToken currentToken)
        {
            return this.ListBlobsSegmented(prefix, false, BlobListingDetails.None, null /* maxResults */, currentToken, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="prefix">A string containing the blob name prefix, including the container name.</param>
        /// <param name="useFlatBlobListing">A boolean value that specifies whether to list blobs in a flat listing, or whether to list blobs hierarchically, by virtual directory.</param>
        /// <param name="blobListingDetails">A <see cref="BlobListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="BlobResultSegment"/> object.</returns>
        [DoesServiceRequest]
        public virtual BlobResultSegment ListBlobsSegmented(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext)
        {
            string containerName;
            string listingPrefix;
            CloudBlobClient.ParseUserPrefix(prefix, out containerName, out listingPrefix);

            CloudBlobContainer container = this.GetContainerReference(containerName);
            return container.ListBlobsSegmented(listingPrefix, useFlatBlobListing, blobListingDetails, maxResults, currentToken, options, operationContext);
        }

#endif
        /// <summary>
        /// Begins an asynchronous operation to return a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="prefix">A string containing the blob name prefix, including the container name.</param>
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginListBlobsSegmented(string prefix, BlobContinuationToken currentToken, AsyncCallback callback, object state)
        {
            return this.BeginListBlobsSegmented(prefix, false, BlobListingDetails.None, null /* maxResults */, currentToken, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to return a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="prefix">A string containing the blob name prefix, including the container name.</param>
        /// <param name="useFlatBlobListing">A boolean value that specifies whether to list blobs in a flat listing, or whether to list blobs hierarchically, by virtual directory.</param>
        /// <param name="blobListingDetails">A <see cref="BlobListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginListBlobsSegmented(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            string containerName;
            string listingPrefix;
            CloudBlobClient.ParseUserPrefix(prefix, out containerName, out listingPrefix);

            CloudBlobContainer container = this.GetContainerReference(containerName);
            StorageAsyncResult<BlobResultSegment> result = new StorageAsyncResult<BlobResultSegment>(callback, state);
            ICancellableAsyncResult asyncResult = container.BeginListBlobsSegmented(
                listingPrefix,
                useFlatBlobListing,
                blobListingDetails,
                maxResults,
                currentToken,
                options,
                operationContext,
                ar =>
                {
                    result.UpdateCompletedSynchronously(ar.CompletedSynchronously);

                    try
                    {
                        result.Result = container.EndListBlobsSegmented(ar);
                        result.OnComplete();
                    }
                    catch (Exception e)
                    {
                        result.OnComplete(e);
                    }
                },
                null /* state */);

            result.CancelDelegate = asyncResult.Cancel;
            return result;
        }

        /// <summary>
        /// Ends an asynchronous operation to return a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="BlobResultSegment"/> object.</returns>
        public virtual BlobResultSegment EndListBlobsSegmented(IAsyncResult asyncResult)
        {
            StorageAsyncResult<BlobResultSegment> result = (StorageAsyncResult<BlobResultSegment>)asyncResult;
            result.End();
            return result.Result;
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="prefix">A string containing the blob name prefix, including the container name.</param>
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="BlobResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, BlobContinuationToken currentToken)
        {
            return this.ListBlobsSegmentedAsync(prefix, currentToken, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="prefix">A string containing the blob name prefix.</param>
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="BlobResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, BlobContinuationToken currentToken, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginListBlobsSegmented, this.EndListBlobsSegmented, prefix, currentToken, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="prefix">A string containing the blob name prefix.</param>
        /// <param name="useFlatBlobListing">A boolean value that specifies whether to list blobs in a flat listing, or whether to list blobs hierarchically, by virtual directory.</param>
        /// <param name="blobListingDetails">A <see cref="BlobListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="BlobResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ListBlobsSegmentedAsync(prefix, useFlatBlobListing, blobListingDetails, maxResults, currentToken, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return a result segment containing a collection of blob items 
        /// in the container.
        /// </summary>
        /// <param name="prefix">A string containing the blob name prefix.</param>
        /// <param name="useFlatBlobListing">A boolean value that specifies whether to list blobs in a flat listing, or whether to list blobs hierarchically, by virtual directory.</param>
        /// <param name="blobListingDetails">A <see cref="BlobListingDetails"/> enumeration describing which items to include in the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A <see cref="BlobContinuationToken"/> object returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="BlobResultSegment"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginListBlobsSegmented, this.EndListBlobsSegmented, prefix, useFlatBlobListing, blobListingDetails, maxResults, currentToken, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Gets a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="System.Uri"/> containing the URI of the blob. The service assumes this is the URI for the blob at the primary location.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An <see cref="ICloudBlob"/> object.</returns>
        [DoesServiceRequest]
        public virtual ICloudBlob GetBlobReferenceFromServer(Uri blobUri, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("blobUri", blobUri);
            return this.GetBlobReferenceFromServer(new StorageUri(blobUri), accessCondition, options, operationContext);
        }

        /// <summary>
        /// Gets a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="StorageUri"/> containing the URI of the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies any additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An <see cref="ICloudBlob"/> object.</returns>
        [DoesServiceRequest]
        public virtual ICloudBlob GetBlobReferenceFromServer(StorageUri blobUri, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("blobUri", blobUri);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this);
            return Executor.ExecuteSync(
                this.GetBlobReferenceImpl(blobUri, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to get a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="System.Uri"/> containing the URI of the blob. The service assumes this is the URI for the blob at the primary location.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetBlobReferenceFromServer(Uri blobUri, AsyncCallback callback, object state)
        {
            return this.BeginGetBlobReferenceFromServer(blobUri, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to get a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="System.Uri"/> containing the URI of the blob. The service assumes this is the URI for the blob at the primary location.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetBlobReferenceFromServer(Uri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("blobUri", blobUri);
            return this.BeginGetBlobReferenceFromServer(new StorageUri(blobUri), accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to get a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="StorageUri"/> containing the URI of the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies any additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetBlobReferenceFromServer(StorageUri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("blobUri", blobUri);
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this);
            return Executor.BeginExecuteAsync(
                this.GetBlobReferenceImpl(blobUri, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to get a reference to a blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>An <see cref="ICloudBlob"/> object.</returns>
        public virtual ICloudBlob EndGetBlobReferenceFromServer(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<ICloudBlob>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation that gets a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="System.Uri"/> containing the URI of the blob. The service assumes this is the URI for the blob at the primary location.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ICloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(Uri blobUri)
        {
            return this.GetBlobReferenceFromServerAsync(blobUri, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that gets a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="System.Uri"/> containing the URI of the blob. The service assumes this is the URI for the blob at the primary location.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ICloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(Uri blobUri, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginGetBlobReferenceFromServer, this.EndGetBlobReferenceFromServer, blobUri, cancellationToken);
        }

        /// <summary>
        /// Returns a <see cref="Task{T}"/> object that gets a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="System.Uri"/> containing the URI of the blob. The service assumes this is the URI for the blob at the primary location.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ICloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(Uri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.GetBlobReferenceFromServerAsync(blobUri, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that gets a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="System.Uri"/> containing the URI of the blob. The service assumes this is the URI for the blob at the primary location.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ICloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(Uri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginGetBlobReferenceFromServer, this.EndGetBlobReferenceFromServer, blobUri, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation that gets a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="StorageUri"/> containing the URI of the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="BlobRequestOptions"/> object that specifies any additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ICloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(StorageUri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.GetBlobReferenceFromServerAsync(blobUri, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that gets a reference to a blob.
        /// </summary>
        /// <param name="blobUri">A <see cref="StorageUri"/> containing the URI of the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="BlobRequestOptions"/> object that specifies any additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ICloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ICloudBlob> GetBlobReferenceFromServerAsync(StorageUri blobUri, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginGetBlobReferenceFromServer, this.EndGetBlobReferenceFromServer, blobUri, accessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Core implementation for the ListContainers method.
        /// </summary>
        /// <param name="prefix">The container prefix.</param>
        /// <param name="detailsIncluded">The details included.</param>
        /// <param name="currentToken">The continuation token.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="ResultSegment{T}"/> that lists the containers.</returns>
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
            getCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => ContainerHttpWebRequestFactory.List(uri, serverTimeout, listingContext, detailsIncluded, useVersionHeader, ctx);
            getCmd.SignRequest = this.AuthenticationHandler.SignRequest;
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
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
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the FetchAttributes method. The attributes are updated immediately.
        /// </summary>
        /// <param name="blobUri">A <see cref="StorageUri"/> containing the URI of the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that fetches the attributes.</returns>
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
            getCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.GetProperties(uri, serverTimeout, parsedSnapshot, accessCondition, useVersionHeader, ctx);
            getCmd.SignRequest = client.AuthenticationHandler.SignRequest;
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
        /// Begins an asynchronous operation to get service properties for the Blob service.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetServiceProperties(AsyncCallback callback, object state)
        {
            return this.BeginGetServiceProperties(null /* requestOptions */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to get service properties for the Blob service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetServiceProperties(BlobRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            requestOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, this);
            operationContext = operationContext ?? new OperationContext();
            return Executor.BeginExecuteAsync(
                this.GetServicePropertiesImpl(requestOptions),
                requestOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to get service properties for the Blob service.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="ServiceProperties"/> object.</returns>
        public virtual ServiceProperties EndGetServiceProperties(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<ServiceProperties>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Blob service.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync()
        {
            return this.GetServicePropertiesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Blob service.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginGetServiceProperties, this.EndGetServiceProperties, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Blob service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync(BlobRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.GetServicePropertiesAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service properties for the Blob service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceProperties> GetServicePropertiesAsync(BlobRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginGetServiceProperties, this.EndGetServiceProperties, requestOptions, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Gets service properties for the Blob service.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="ServiceProperties"/> object.</returns>
        [DoesServiceRequest]
        public virtual ServiceProperties GetServiceProperties(BlobRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, this);
            operationContext = operationContext ?? new OperationContext();
            return Executor.ExecuteSync(
                this.GetServicePropertiesImpl(requestOptions),
                requestOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to set service properties for the Blob service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetServiceProperties(ServiceProperties properties, AsyncCallback callback, object state)
        {
            return this.BeginSetServiceProperties(properties, null /* requestOptions */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to set service properties for the Blob service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetServiceProperties(ServiceProperties properties, BlobRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            requestOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, this);
            operationContext = operationContext ?? new OperationContext();
            return Executor.BeginExecuteAsync(
                this.SetServicePropertiesImpl(properties, requestOptions),
                requestOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to set service properties for the Blob service.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetServiceProperties(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation that sets service properties for the Blob service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties)
        {
            return this.SetServicePropertiesAsync(properties, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that sets service properties for the Blob service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetServiceProperties, this.EndSetServiceProperties, properties, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation that sets service properties for the Blob service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties, BlobRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.SetServicePropertiesAsync(properties, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that sets service properties for the Blob service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(ServiceProperties properties, BlobRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetServiceProperties, this.EndSetServiceProperties, properties, requestOptions, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Sets service properties for the Blob service.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object.</param>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void SetServiceProperties(ServiceProperties properties, BlobRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, this);
            operationContext = operationContext ?? new OperationContext();
            Executor.ExecuteSync(
                this.SetServicePropertiesImpl(properties, requestOptions),
                requestOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to get service stats for the secondary Blob service endpoint.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetServiceStats(AsyncCallback callback, object state)
        {
            return this.BeginGetServiceStats(null /* requestOptions */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to get service stats for the secondary Blob service endpoint.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetServiceStats(BlobRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            requestOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, this);
            operationContext = operationContext ?? new OperationContext();
            return Executor.BeginExecuteAsync(
                this.GetServiceStatsImpl(requestOptions),
                requestOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to get service stats for the secondary Blob service endpoint.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="ServiceStats"/> object.</returns>
        public virtual ServiceStats EndGetServiceStats(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<ServiceStats>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Blob service endpoint.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync()
        {
            return this.GetServiceStatsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Blob service endpoint.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginGetServiceStats, this.EndGetServiceStats, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Blob service endpoint.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync(BlobRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.GetServiceStatsAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get service stats for the secondary Blob service endpoint.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ServiceStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ServiceStats> GetServiceStatsAsync(BlobRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginGetServiceStats, this.EndGetServiceStats, requestOptions, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Gets service stats for the secondary Blob service endpoint.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="ServiceStats"/> object.</returns>
        [DoesServiceRequest]
        public virtual ServiceStats GetServiceStats(BlobRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, this);
            operationContext = operationContext ?? new OperationContext();
            return Executor.ExecuteSync(
                this.GetServiceStatsImpl(requestOptions),
                requestOptions.RetryPolicy,
                operationContext);
        }
#endif

        private RESTCommand<ServiceProperties> GetServicePropertiesImpl(BlobRequestOptions requestOptions)
        {
            RESTCommand<ServiceProperties> retCmd = new RESTCommand<ServiceProperties>(this.Credentials, this.StorageUri);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequestDelegate = BlobHttpWebRequestFactory.GetServiceProperties;
            retCmd.SignRequest = this.AuthenticationHandler.SignRequest;
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);

            retCmd.PostProcessResponse =
                (cmd, resp, ctx) => BlobHttpResponseParsers.ReadServiceProperties(cmd.ResponseStream);
            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }

        private RESTCommand<NullType> SetServicePropertiesImpl(ServiceProperties properties, BlobRequestOptions requestOptions)
        {
            MultiBufferMemoryStream str = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            try
            {
                properties.WriteServiceProperties(str);
            }
            catch (InvalidOperationException invalidOpException)
            {
                str.Dispose();
                throw new ArgumentException(invalidOpException.Message, "properties");
            }

            str.Seek(0, SeekOrigin.Begin);

            RESTCommand<NullType> retCmd = new RESTCommand<NullType>(this.Credentials, this.StorageUri);
            retCmd.SendStream = str;
            retCmd.StreamToDispose = str;
            retCmd.BuildRequestDelegate = BlobHttpWebRequestFactory.SetServiceProperties;
            retCmd.RecoveryAction = RecoveryActions.RewindStream;
            retCmd.SignRequest = this.AuthenticationHandler.SignRequest;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex);
            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
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
            retCmd.BuildRequestDelegate = BlobHttpWebRequestFactory.GetServiceStats;
            retCmd.SignRequest = this.AuthenticationHandler.SignRequest;
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            retCmd.PostProcessResponse = (cmd, resp, ctx) => BlobHttpResponseParsers.ReadServiceStats(cmd.ResponseStream);
            return retCmd;
        }

        #endregion
    }
}
