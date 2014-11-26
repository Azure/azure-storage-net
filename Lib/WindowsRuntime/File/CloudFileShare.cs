// -----------------------------------------------------------------------------------------
// <copyright file="CloudFileShare.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Net;
    using System.Net.Http;
#if ASPNET_K || PORTABLE
    using System.Threading;
    using System.Threading.Tasks;
#elif WINDOWS_RT
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
#endif

    public sealed partial class CloudFileShare
    {
        /// <summary>
        /// Creates the share.
        /// </summary>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task CreateAsync()
#elif WINDOWS_RT
        public IAsyncAction CreateAsync()
#endif
        {
            return this.CreateAsync(null, null);
        }

        /// <summary>
        /// Creates the share.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task CreateAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.CreateAsync(options, operationContext, CancellationToken.None);
        }
#elif WINDOWS_RT
        public IAsyncAction CreateAsync(FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.CreateShareImpl(modifiedOptions),
                modifiedOptions.RetryPolicy, 
                operationContext, 
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Creates the share.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public Task CreateAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.CreateShareImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Creates the share if it does not already exist.
        /// </summary>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<bool> CreateIfNotExistsAsync()
#elif WINDOWS_RT
        public IAsyncOperation<bool> CreateIfNotExistsAsync()
#endif
        {
            return this.CreateIfNotExistsAsync(null, null);
        }

        /// <summary>
        /// Creates the share if it does not already exist.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.CreateIfNotExistsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates the share if it does not already exist.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
        public Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
#elif WINDOWS_RT
        public IAsyncOperation<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext)
#endif
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

#if ASPNET_K || PORTABLE
            return Task.Run(async () =>
            {
                bool exists = await this.ExistsAsync(modifiedOptions, operationContext, cancellationToken);
#elif WINDOWS_RT
            return AsyncInfo.Run(async (token) =>
            {
                bool exists = await this.ExistsAsync(modifiedOptions, operationContext).AsTask(token); 
#endif

                if (exists)
                    {
                        return false;
                    }

                    try
                {
#if ASPNET_K || PORTABLE
                    await this.CreateAsync(modifiedOptions, operationContext, cancellationToken);
#elif WINDOWS_RT
                    await this.CreateAsync(modifiedOptions, operationContext).AsTask(token);
#endif
                    return true;
                    }
                    catch (Exception)
                    {
                        if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.Conflict)
                        {
                            StorageExtendedErrorInformation extendedInfo = operationContext.LastResult.ExtendedErrorInformation;
                            if ((extendedInfo == null) ||
                                (extendedInfo.ErrorCode == FileErrorCodeStrings.ShareAlreadyExists))
                            {
                                return false;
                            }
                            else
                            {
                                throw;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
#if ASPNET_K || PORTABLE
            }, cancellationToken);
#elif WINDOWS_RT
            });
#endif
        }

            /// <summary>
            /// Deletes the share.
            /// </summary>
            [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task DeleteAsync()
#elif WINDOWS_RT
        public IAsyncAction DeleteAsync()
#endif
        {
            return this.DeleteAsync(null, null, null);
        }

        /// <summary>
        /// Deletes the share.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(accessCondition, options, operationContext, CancellationToken.None);
        }
#elif WINDOWS_RT
        public IAsyncAction DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.DeleteShareImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy, 
                operationContext, 
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Deletes the share.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.DeleteShareImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Deletes the share if it already exists.
        /// </summary>
        /// <returns><c>true</c> if the share already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<bool> DeleteIfExistsAsync()
#elif WINDOWS_RT
        public IAsyncOperation<bool> DeleteIfExistsAsync()
#endif
        {
            return this.DeleteIfExistsAsync(null, null, null);
        }

        /// <summary>
        /// Deletes the share if it already exists.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the share already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the share if it already exists.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the share already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
#elif WINDOWS_RT
        public IAsyncOperation<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
#endif
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

#if ASPNET_K || PORTABLE
            return Task.Run(async () =>
            {
                bool exists = await this.ExistsAsync(modifiedOptions, operationContext, cancellationToken);
#elif WINDOWS_RT
            return AsyncInfo.Run(async (token) =>
            {
                bool exists = await this.ExistsAsync(modifiedOptions, operationContext).AsTask(token);
#endif

                if (!exists)
                {
                    return false;
                }

                try
                {
#if ASPNET_K || PORTABLE
                    await this.DeleteAsync(accessCondition, modifiedOptions, operationContext, cancellationToken);
#elif WINDOWS_RT
                    await this.DeleteAsync(accessCondition, modifiedOptions, operationContext).AsTask(token);
#endif
                    return true;
                }
                catch (Exception)
                {
                    if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.NotFound)
                    {
                        StorageExtendedErrorInformation extendedInfo = operationContext.LastResult.ExtendedErrorInformation;
                        if ((extendedInfo == null) ||
                            (extendedInfo.ErrorCode == FileErrorCodeStrings.ShareNotFound))
                        {
                            return false;
                        }
                        else
                        {
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
#if ASPNET_K || PORTABLE
            }, cancellationToken);
#elif WINDOWS_RT
            });
#endif
        }

        /// <summary>
        /// Checks existence of the share.
        /// </summary>
        /// <returns><c>true</c> if the share exists.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<bool> ExistsAsync()
#elif WINDOWS_RT
        public IAsyncOperation<bool> ExistsAsync()
#endif
        {
            return this.ExistsAsync(null, null);
        }

        /// <summary>
        /// Checks existence of the share.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the share exists.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(options, operationContext, CancellationToken.None);
        }
#elif WINDOWS_RT
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

#if ASPNET_K || PORTABLE

        /// <summary>
        /// Checks existence of the share.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the share exists.</returns>
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
        /// Retrieves the share's attributes.
        /// </summary>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task FetchAttributesAsync()
#elif WINDOWS_RT
        public IAsyncAction FetchAttributesAsync()
#endif
        {
            return this.FetchAttributesAsync(null, null, null);
        }

        /// <summary>
        /// Retrieves the share's attributes.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

#elif WINDOWS_RT
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

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Retrieves the share's attributes.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
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
        /// Sets the share's user-defined metadata.
        /// </summary>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task SetMetadataAsync()
#elif WINDOWS_RT
        public IAsyncAction SetMetadataAsync()
#endif
        {
            return this.SetMetadataAsync(null, null, null);
        }

        /// <summary>
        /// Sets the share's user-defined metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(accessCondition, options, operationContext, CancellationToken.None);
        }
#elif WINDOWS_RT
        public IAsyncAction SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.SetMetadataImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy, 
                operationContext, 
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Sets the share's user-defined metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.SetMetadataImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Implementation for the Create method.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the share.</returns>
        private RESTCommand<NullType> CreateShareImpl(FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => 
            {
                HttpRequestMessage msg = ShareHttpRequestMessageFactory.Create(uri, serverTimeout, cnt, ctx);
                ShareHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                this.Properties = ShareHttpResponseParsers.GetProperties(resp);
                this.Metadata = ShareHttpResponseParsers.GetMetadata(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Delete method.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that deletes the share.</returns>
        private RESTCommand<NullType> DeleteShareImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> deleteCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(deleteCmd);
            deleteCmd.Handler = this.ServiceClient.AuthenticationHandler;
            deleteCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            deleteCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.Delete(uri, serverTimeout, accessCondition, cnt, ctx);
            deleteCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex);

            return deleteCmd;
        }

        /// <summary>
        /// Implementation for the FetchAttributes method.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that fetches the attributes.</returns>
        private RESTCommand<NullType> FetchAttributesImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.GetProperties(uri, serverTimeout, accessCondition, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.Properties = ShareHttpResponseParsers.GetProperties(resp);
                this.Metadata = ShareHttpResponseParsers.GetMetadata(resp);
                return NullType.Value;
            };

            return getCmd;
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
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.GetProperties(uri, serverTimeout, null, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, true, cmd, ex);
                this.Properties = ShareHttpResponseParsers.GetProperties(resp);
                this.Metadata = ShareHttpResponseParsers.GetMetadata(resp);
                return true;
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the SetMetadata method.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetMetadataImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                HttpRequestMessage msg = ShareHttpRequestMessageFactory.SetMetadata(uri, serverTimeout, accessCondition, cnt, ctx);
                ShareHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.UpdateETagAndLastModified(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Retrieve ETag and LastModified date time from response.
        /// </summary>
        /// <param name="response">The response to parse.</param>
        private void UpdateETagAndLastModified(HttpResponseMessage response)
        {
            FileShareProperties parsedProperties = ShareHttpResponseParsers.GetProperties(response);
            this.Properties.ETag = parsedProperties.ETag;
            this.Properties.LastModified = parsedProperties.LastModified;
        }
    }
}
