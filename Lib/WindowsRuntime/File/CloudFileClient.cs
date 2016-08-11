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
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Threading;
#if NETCORE
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
#endif

    public partial class CloudFileClient
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
        /// Returns a result segment containing a collection of shares.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <returns>A result segment of shares.</returns>
        [DoesServiceRequest]
        public virtual Task<ShareResultSegment> ListSharesSegmentedAsync(FileContinuationToken currentToken)
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
        public virtual Task<ShareResultSegment> ListSharesSegmentedAsync(string prefix, FileContinuationToken currentToken)
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
        public virtual Task<ShareResultSegment> ListSharesSegmentedAsync(string prefix, ShareListingDetails detailsIncluded, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ListSharesSegmentedAsync(prefix, detailsIncluded, maxResults, currentToken, options, operationContext, CancellationToken.None);
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
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A result segment of shares.</returns>
        [DoesServiceRequest]
        public virtual Task<ShareResultSegment> ListSharesSegmentedAsync(string prefix, ShareListingDetails detailsIncluded, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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

        /// <summary>
        /// Gets the properties of the File service.
        /// </summary>
        /// <returns>The File service properties.</returns>
        [DoesServiceRequest]
        public virtual Task<FileServiceProperties> GetServicePropertiesAsync()
        {
            return this.GetServicePropertiesAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets the properties of the File service.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The File service properties.</returns>
        [DoesServiceRequest]
        public virtual Task<FileServiceProperties> GetServicePropertiesAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.GetServicePropertiesAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the properties of the File service.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The File service properties.</returns>
        [DoesServiceRequest]
        public virtual Task<FileServiceProperties> GetServicePropertiesAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(
                async () => await Executor.ExecuteAsync(
                    this.GetServicePropertiesImpl(modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Sets the properties of the File service.
        /// </summary>
        /// <param name="properties">The File service properties.</param>
        /// <returns>The properties of the File service.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(FileServiceProperties properties)
        {
            return this.SetServicePropertiesAsync(properties, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets the properties of the File service.
        /// </summary>
        /// <param name="properties">The File service properties.</param>
        /// <param name="requestOptions">A <see cref="FileRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The properties of the File service.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(FileServiceProperties properties, FileRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.SetServicePropertiesAsync(properties, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the properties of the File service.
        /// </summary>
        /// <param name="properties">The File service properties.</param>
        /// <param name="requestOptions">A <see cref="FileRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The properties of the File service.</returns>
        [DoesServiceRequest]
        public virtual Task SetServicePropertiesAsync(FileServiceProperties properties, FileRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(requestOptions, this);
            operationContext = operationContext ?? new OperationContext();
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                 this.SetServicePropertiesImpl(properties, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

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
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.List(uri, serverTimeout, listingContext, detailsIncluded, cnt, ctx, this.GetCanonicalizer(), this.Credentials);
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

        private RESTCommand<FileServiceProperties> GetServicePropertiesImpl(FileRequestOptions requestOptions)
        {
            RESTCommand<FileServiceProperties> retCmd = new RESTCommand<FileServiceProperties>(this.Credentials, this.StorageUri);

            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.GetServiceProperties(uri, serverTimeout, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);

            retCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task.Factory.StartNew(() => FileHttpResponseParsers.ReadServiceProperties(cmd.ResponseStream));
            };

            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }

        private RESTCommand<NullType> SetServicePropertiesImpl(FileServiceProperties properties, FileRequestOptions requestOptions)
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
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.SetServiceProperties(uri, serverTimeout, cnt, ctx, this.GetCanonicalizer(), this.Credentials);
            retCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null /* md5 */, cmd, ctx);
            retCmd.StreamToDispose = memoryStream;
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, null /* retVal */, cmd, ex);
            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }
    }
}
