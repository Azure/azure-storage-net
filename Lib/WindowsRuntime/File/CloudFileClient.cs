// -----------------------------------------------------------------------------------------
// <copyright file="CloudFileClient.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.WindowsAzure.Storage.Auth.Protocol;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
#if ASPNET_K
    using System.Threading;
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
#endif

    public sealed partial class CloudFileClient
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
        /// Gets the authentication handler used to sign requests.
        /// </summary>
        /// <value>Authentication handler.</value>
        internal HttpClientHandler AuthenticationHandler
        {
            get
            {
                HttpClientHandler authenticationHandler;
                if (this.Credentials.IsSharedKey)
                {
                    authenticationHandler = new SharedKeyAuthenticationHttpHandler(
                        this.GetCanonicalizer(),
                        this.Credentials,
                        this.Credentials.AccountName);
                }
                else
                {
                    authenticationHandler = new NoOpAuthenticationHttpHandler();
                }

                return authenticationHandler;
            }
        }

        /// <summary>
        /// Returns a result segment containing a collection of shares.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <returns>A result segment of shares.</returns>
        [DoesServiceRequest]
#if ASPNET_K
        public Task<ShareResultSegment> ListSharesSegmentedAsync(FileContinuationToken currentToken)
#else
        public IAsyncOperation<ShareResultSegment> ListSharesSegmentedAsync(FileContinuationToken currentToken)
#endif
        {
            return this.ListSharesSegmentedAsync(null, ShareListingDetails.None, null, currentToken, null, null);
        }

        /// <summary>
        /// Returns a result segment containing a collection of shares.
        /// </summary>
        /// <param name="prefix">The share name prefix.</param>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <returns>A result segment of shares.</returns>
        [DoesServiceRequest]
#if ASPNET_K
        public Task<ShareResultSegment> ListSharesSegmentedAsync(string prefix, FileContinuationToken currentToken)
#else
        public IAsyncOperation<ShareResultSegment> ListSharesSegmentedAsync(string prefix, FileContinuationToken currentToken)
#endif
        {
            return this.ListSharesSegmentedAsync(prefix, ShareListingDetails.None, null, currentToken, null, null);
        }

        /// <summary>
        /// Returns a result segment containing a collection of shares
        /// whose names begin with the specified prefix.
        /// </summary>
        /// <param name="prefix">The share name prefix.</param>
        /// <param name="detailsIncluded">A value that indicates whether to return share metadata with the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000. If this value is null, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <returns>A result segment of shares.</returns>
        [DoesServiceRequest]
#if ASPNET_K
        public Task<ShareResultSegment> ListSharesSegmentedAsync(string prefix, ShareListingDetails detailsIncluded, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ListSharesSegmentedAsync(prefix, detailsIncluded, maxResults, currentToken, options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<ShareResultSegment> ListSharesSegmentedAsync(string prefix, ShareListingDetails detailsIncluded, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            return AsyncInfo.Run(async (token) =>
            {
                FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this);
                ResultSegment<CloudFileShare> resultSegment = await Executor.ExecuteAsync(
                    this.ListSharesImpl(prefix, detailsIncluded, currentToken, maxResults, modifiedOptions),
                    modifiedOptions.RetryPolicy, 
                    operationContext, 
                    token);

                return new ShareResultSegment(resultSegment.Results, (FileContinuationToken)resultSegment.ContinuationToken);
            });
        }
#endif

#if ASPNET_K
        /// <summary>
        /// Returns a result segment containing a collection of shares
        /// whose names begin with the specified prefix.
        /// </summary>
        /// <param name="prefix">The share name prefix.</param>
        /// <param name="detailsIncluded">A value that indicates whether to return share metadata with the listing.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000. If this value is null, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A result segment of shares.</returns>
        [DoesServiceRequest]
        public Task<ShareResultSegment> ListSharesSegmentedAsync(string prefix, ShareListingDetails detailsIncluded, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this);
                ResultSegment<CloudFileShare> resultSegment = await Executor.ExecuteAsync(
                    this.ListSharesImpl(prefix, detailsIncluded, currentToken, maxResults, modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken);

                return new ShareResultSegment(resultSegment.Results, (FileContinuationToken)resultSegment.ContinuationToken);
            }, cancellationToken);
        }
#endif
        /// <summary>
        /// Core implementation for the ListShares method.
        /// </summary>
        /// <param name="prefix">The share prefix.</param>
        /// <param name="detailsIncluded">The details included.</param>
        /// <param name="currentToken">The continuation token.</param>
        /// <param name="pagination">The pagination.</param>
        /// <param name="setResult">The result report delegate.</param>
        /// <returns>A <see cref="TaskSequence"/> that lists the shares.</returns>
        private RESTCommand<ResultSegment<CloudFileShare>> ListSharesImpl(string prefix, ShareListingDetails detailsIncluded, FileContinuationToken currentToken, int? maxResults, FileRequestOptions options)
        {
            ListingContext listingContext = new ListingContext(prefix, maxResults)
            {
                Marker = currentToken != null ? currentToken.NextMarker : null
            };

            RESTCommand<ResultSegment<CloudFileShare>> getCmd = new RESTCommand<ResultSegment<CloudFileShare>>(this.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommonUtility.GetListingLocationMode(currentToken);
            getCmd.RetrieveResponseStream = true;
            getCmd.Handler = this.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.List(uri, serverTimeout, listingContext, detailsIncluded, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task.Factory.StartNew(() =>
                    {
                        ListSharesResponse listSharesResponse = new ListSharesResponse(cmd.ResponseStream);
                        List<CloudFileShare> sharesList = listSharesResponse.Shares.Select(item => new CloudFileShare(item.Properties, item.Metadata, item.Name, this)).ToList();
                        FileContinuationToken continuationToken = null;
                        if (listSharesResponse.NextMarker != null)
                        {
                            continuationToken = new FileContinuationToken()
                            {
                                NextMarker = listSharesResponse.NextMarker,
                                TargetLocation = cmd.CurrentResult.TargetLocation,
                            };
                        }

                        return new ResultSegment<CloudFileShare>(sharesList)
                        {
                            ContinuationToken = continuationToken,
                        };
                    });
            };

            return getCmd;
        }
    }
}
