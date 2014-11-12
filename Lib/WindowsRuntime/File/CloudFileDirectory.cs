// -----------------------------------------------------------------------------------------
// <copyright file="CloudFileDirectory.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core;
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

    public sealed partial class CloudFileDirectory
    {
        /// <summary>
        /// Creates the directory.
        /// </summary>
        [DoesServiceRequest]
#if ASPNET_K
        public Task CreateAsync()
#else
        public IAsyncAction CreateAsync()
#endif
        {
            return this.CreateAsync(null, null);
        }

        /// <summary>
        /// Creates the directory.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
#if ASPNET_K
        public Task CreateAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.CreateAsync(options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncAction CreateAsync(FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.CreateDirectoryImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K
        /// <summary>
        /// Creates the directory.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public Task CreateAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async() => await Executor.ExecuteAsyncNullReturn(
                this.CreateDirectoryImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Creates the directory if it does not already exist.
        /// </summary>
        /// <returns><c>true</c> if the directory did not already exist and was created; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K
        public Task<bool> CreateIfNotExistsAsync()
#else
        public IAsyncOperation<bool> CreateIfNotExistsAsync()
#endif
        {
            return this.CreateIfNotExistsAsync(null, null);
        }

        /// <summary>
        /// Creates the directory if it does not already exist.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the directory did not already exist and was created; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K
        public Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.CreateIfNotExistsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates the directory if it does not already exist.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the directory did not already exist and was created; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
        public Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
#else
        public IAsyncOperation<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext)
#endif
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
#if ASPNET_K
            return Task.Run(async () =>
#else
            return AsyncInfo.Run(async (cancellationToken) =>
#endif
            {
                bool exists = await Executor.ExecuteAsync(
                    this.ExistsImpl(modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken);

                if (exists)
                {
                    return false;
                }

                try
                {
                    await Executor.ExecuteAsync(
                        this.CreateDirectoryImpl(modifiedOptions),
                        modifiedOptions.RetryPolicy,
                        operationContext,
                        cancellationToken);

                    return true;
                }
                catch (StorageException e)
                {
                    if ((e.RequestInformation.ExtendedErrorInformation != null) &&
                        (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ResourceAlreadyExists))
                    {
                        return false;
                    }
                    else
                    {
                        throw;
                    }
                }
#if ASPNET_K
            }, cancellationToken);
#else
            });
#endif
        }

            /// <summary>
            /// Deletes the directory.
            /// </summary>
            [DoesServiceRequest]
#if ASPNET_K
        public Task DeleteAsync()
#else
        public IAsyncAction DeleteAsync()
#endif
        {
            return this.DeleteAsync(null, null, null);
        }

        /// <summary>
        /// Deletes the directory.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the directory. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
#if ASPNET_K
        public Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncAction DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.DeleteDirectoryImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }  
#endif

#if ASPNET_K
        /// <summary>
        /// Deletes the directory.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the directory. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.DeleteDirectoryImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif        

        /// <summary>
        /// Deletes the directory if it already exists.
        /// </summary>
        /// <returns><c>true</c> if the directory already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K
        public Task<bool> DeleteIfExistsAsync()
#else
        public IAsyncOperation<bool> DeleteIfExistsAsync()
#endif
        {
            return this.DeleteIfExistsAsync(null, null, null);
        }

        /// <summary>
        /// Deletes the directory if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the directory already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K
        public Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the directory if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the directory already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
#else
        public IAsyncOperation<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
#endif
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
#if ASPNET_K
            return Task.Run(async () =>
#else
            return AsyncInfo.Run(async (cancellationToken) =>
#endif
            {
                bool exists = await Executor.ExecuteAsync(
                    this.ExistsImpl(modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken);

                if (!exists)
                {
                    return false;
                }

                try
                {
                    await Executor.ExecuteAsync(
                        this.DeleteDirectoryImpl(accessCondition, modifiedOptions),
                        modifiedOptions.RetryPolicy,
                        operationContext,
                        cancellationToken);

                    return true;
                }
                catch (StorageException e)
                {
                    if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                    {
                        return false;
                    }
                    else
                    {
                        throw;
                    }
                }
#if ASPNET_K
            }, cancellationToken);
#else
            });
#endif
        }

            /// <summary>
            /// Checks existence of the directory.
            /// </summary>
            /// <returns><c>true</c> if the directory exists.</returns>
            [DoesServiceRequest]
#if ASPNET_K
        public Task<bool> ExistsAsync()
#else
        public IAsyncOperation<bool> ExistsAsync()
#endif
        {
            return this.ExistsAsync(null, null);
        }

        /// <summary>
        /// Checks existence of the directory.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the directory exists.</returns>
        [DoesServiceRequest]
#if ASPNET_K
        public Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsync(
                this.ExistsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K
        /// <summary>
        /// Checks existence of the directory.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the directory exists.</returns>
        [DoesServiceRequest]
        public Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.ExistsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Populates a directory's properties and metadata.
        /// </summary>
        [DoesServiceRequest]
#if ASPNET_K
        public Task FetchAttributesAsync()
#else
        public IAsyncAction FetchAttributesAsync()
#endif
        {
            return this.FetchAttributesAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Populates a directory's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        [DoesServiceRequest]
#if ASPNET_K
        public Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncAction FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.FetchAttributesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K
        /// <summary>
        /// Populates a directory's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.FetchAttributesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Returns a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <returns>A result segment containing objects that implement <see cref="IListFileItem"/>.</returns>
        [DoesServiceRequest]
#if ASPNET_K
        public Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(FileContinuationToken currentToken)
#else
        public IAsyncOperation<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(FileContinuationToken currentToken)
#endif
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(null /* maxResults */, currentToken, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is zero, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <returns>A file result segment.</returns>
        [DoesServiceRequest]
#if ASPNET_K
        public Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(maxResults, currentToken, options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return AsyncInfo.Run(async (token) =>
            {
                ResultSegment<IListFileItem> resultSegment = await Executor.ExecuteAsync(
                    this.ListFilesAndDirectoriesImpl(maxResults, modifiedOptions, currentToken),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    token);

                return new FileResultSegment(resultSegment.Results, (FileContinuationToken)resultSegment.ContinuationToken);
            });
        }
#endif

#if ASPNET_K
        /// <summary>
        /// Returns a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is zero, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A file result segment.</returns>
        [DoesServiceRequest]
        public Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () =>
            {
                ResultSegment<IListFileItem> resultSegment = await Executor.ExecuteAsync(
                    this.ListFilesAndDirectoriesImpl(maxResults, modifiedOptions, currentToken),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken);

                return new FileResultSegment(resultSegment.Results, (FileContinuationToken)resultSegment.ContinuationToken);
            }, cancellationToken);
        }
#endif

        /// <summary>
        /// Implementation for the Create method.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the directory.</returns>
        private RESTCommand<NullType> CreateDirectoryImpl(FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => DirectoryHttpRequestMessageFactory.Create(uri, serverTimeout, cnt, ctx);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                this.UpdateETagAndLastModified(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Delete method.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the directory. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that deletes the directory.</returns>
        private RESTCommand<NullType> DeleteDirectoryImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => DirectoryHttpRequestMessageFactory.Delete(uri, serverTimeout, accessCondition, cnt, ctx);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex);

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Exists method.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that checks existence.</returns>
        private RESTCommand<bool> ExistsImpl(FileRequestOptions options)
        {
            RESTCommand<bool> getCmd = new RESTCommand<bool>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => DirectoryHttpRequestMessageFactory.GetProperties(uri, serverTimeout, null, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, true, cmd, ex);
                this.Properties = DirectoryHttpResponseParsers.GetProperties(resp);
                return true;
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the FetchAttributes method. The attributes are updated immediately.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that fetches the attributes.</returns>
        private RESTCommand<NullType> FetchAttributesImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => DirectoryHttpRequestMessageFactory.GetProperties(uri, serverTimeout, accessCondition, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.Properties = DirectoryHttpResponseParsers.GetProperties(resp);
                return NullType.Value;
            };

            return getCmd;
        }

        /// <summary>
        /// Core implementation of the ListFilesAndDirectories method.
        /// </summary>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is zero, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="currentToken">The continuation token.</param>
        /// <returns>A <see cref="RESTCommand"/> that lists the files.</returns>
        private RESTCommand<ResultSegment<IListFileItem>> ListFilesAndDirectoriesImpl(int? maxResults, FileRequestOptions options, FileContinuationToken currentToken)
        {
            FileListingContext listingContext = new FileListingContext(maxResults)
            {
                Marker = currentToken != null ? currentToken.NextMarker : null
            };

            RESTCommand<ResultSegment<IListFileItem>> getCmd = new RESTCommand<ResultSegment<IListFileItem>>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommonUtility.GetListingLocationMode(currentToken);
            getCmd.RetrieveResponseStream = true;
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => DirectoryHttpRequestMessageFactory.List(uri, serverTimeout, listingContext, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    ListFilesAndDirectoriesResponse listFilesResponse = new ListFilesAndDirectoriesResponse(cmd.ResponseStream);
                    List<IListFileItem> fileList = listFilesResponse.Files.Select(item => this.SelectListFileItem(item)).ToList();
                    FileContinuationToken continuationToken = null;
                    if (listFilesResponse.NextMarker != null)
                    {
                        continuationToken = new FileContinuationToken()
                        {
                            NextMarker = listFilesResponse.NextMarker,
                            TargetLocation = cmd.CurrentResult.TargetLocation,
                        };
                    }

                    return new ResultSegment<IListFileItem>(fileList)
                    {
                        ContinuationToken = continuationToken,
                    };
                });
            };

            return getCmd;
        }

        /// <summary>
        /// Retrieve ETag and LastModified date time from response.
        /// </summary>
        /// <param name="response">The response to parse.</param>
        private void UpdateETagAndLastModified(HttpResponseMessage response)
        {
            FileDirectoryProperties parsedProperties = DirectoryHttpResponseParsers.GetProperties(response);
            this.Properties.ETag = parsedProperties.ETag;
            this.Properties.LastModified = parsedProperties.LastModified;
        }
    }
}
