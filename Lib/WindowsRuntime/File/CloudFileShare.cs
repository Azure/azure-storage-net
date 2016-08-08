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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
#if NETCORE
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
#endif

    public partial class CloudFileShare
    {
        /// <summary>
        /// Creates the share.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task CreateAsync()
        {
            return this.CreateAsync(null, null);
        }

        /// <summary>
        /// Creates the share.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task CreateAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.CreateAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates the share.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task CreateAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.CreateShareImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Creates the share if it does not already exist.
        /// </summary>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise, <c>false</c>.</returns>
        /// <remarks>This API performs an existence check and therefore requires read permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync()
        {
            return this.CreateIfNotExistsAsync(null, null);
        }

        /// <summary>
        /// Creates the share if it does not already exist.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise <c>false</c>.</returns>
        /// <remarks>This API performs an existence check and therefore requires read permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext)
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
        /// <remarks>This API performs an existence check and therefore requires read permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () =>
            {
                bool exists = await this.ExistsAsync(modifiedOptions, operationContext, cancellationToken);

                if (exists)
                    {
                        return false;
                    }

                    try
                {
                    await this.CreateAsync(modifiedOptions, operationContext, cancellationToken);
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
            }, cancellationToken);
        }

        /// <summary>
        /// Deletes the share.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
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
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the share.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.DeleteShareImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Deletes the share if it already exists.
        /// </summary>
        /// <returns><c>true</c> if the share already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
        {
            return this.DeleteIfExistsAsync(null, null, null);
        }

        /// <summary>
        /// Deletes the share if it already exists.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the share already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
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
        public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () =>
            {
                bool exists = await this.ExistsAsync(modifiedOptions, operationContext, cancellationToken);

                if (!exists)
                {
                    return false;
                }

                try
                {
                    await this.DeleteAsync(accessCondition, modifiedOptions, operationContext, cancellationToken);
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
            }, cancellationToken);
        }

        /// <summary>
        /// Checks existence of the share.
        /// </summary>
        /// <returns><c>true</c> if the share exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
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
        public virtual Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(options, operationContext, CancellationToken.None);
        }


        /// <summary>
        /// Checks existence of the share.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the share exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.ExistsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Retrieves the share's attributes.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync()
        {
            return this.FetchAttributesAsync(null, null, null);
        }

        /// <summary>
        /// Retrieves the share's attributes.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves the share's attributes.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.FetchAttributesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Sets permissions for the share.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the share.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(FileSharePermissions permissions)
        {
            return this.SetPermissionsAsync(permissions, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Sets permissions for the share.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the share.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(FileSharePermissions permissions, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetPermissionsAsync(permissions, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Sets permissions for the share.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the share.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(FileSharePermissions permissions, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.SetPermissionsImpl(permissions, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Updates the share's properties.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync()
        {
            return this.SetPropertiesAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Updates the share's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetPropertiesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Updates the share's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.SetPropertiesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Gets the permissions settings for the share.
        /// </summary>
        /// <returns>The share's permissions.</returns>
        [DoesServiceRequest]
        public virtual Task<FileSharePermissions> GetPermissionsAsync()
        {
            return this.GetPermissionsAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets the permissions settings for the share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The share's permissions.</returns>
        [DoesServiceRequest]
        public virtual Task<FileSharePermissions> GetPermissionsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.GetPermissionsAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the permissions settings for the share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The share's permissions.</returns>
        [DoesServiceRequest]
        public virtual Task<FileSharePermissions> GetPermissionsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync<FileSharePermissions>(
                this.GetPermissionsImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Gets the stats of share.
        /// </summary>
        /// <returns>The share stats.</returns>
        [DoesServiceRequest]
        public virtual Task<ShareStats> GetStatsAsync()
        {
            return this.GetStatsAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets the stats of the share.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The share stats.</returns>
        [DoesServiceRequest]
        public virtual Task<ShareStats> GetStatsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.GetStatsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the stats of the share.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies execution options, such as retry policy and timeout settings, for the operation.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The share stats.</returns>
        [DoesServiceRequest]
        public virtual Task<ShareStats> GetStatsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync<ShareStats>(
                this.GetStatsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Sets the share's user-defined metadata.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync()
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
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Sets the share's user-defined metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.SetMetadataImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Implementation for the Create method.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the share.</returns>
        private RESTCommand<NullType> CreateShareImpl(FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => 
            {
                StorageRequestMessage msg = ShareHttpRequestMessageFactory.Create(uri, this.Properties, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
            deleteCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.Delete(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.GetProperties(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.GetProperties(uri, serverTimeout, null, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
        /// Implementation for the SetPermissions method.
        /// </summary>
        /// <param name="acl">The permissions to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the permissions.</returns>
        private RESTCommand<NullType> SetPermissionsImpl(FileSharePermissions acl, AccessCondition accessCondition, FileRequestOptions options)
        {
            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            FileRequest.WriteSharedAccessIdentifiers(acl.SharedAccessPolicies, memoryStream);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.SetAcl(uri, serverTimeout, FileSharePublicAccessType.Off, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null /* md5 */, cmd, ctx);
            putCmd.StreamToDispose = memoryStream;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.UpdateETagAndLastModified(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the GetPermissions method.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that gets the permissions.</returns>
        private RESTCommand<FileSharePermissions> GetPermissionsImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            FileSharePermissions shareAcl = null;

            RESTCommand<FileSharePermissions> getCmd = new RESTCommand<FileSharePermissions>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.GetAcl(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
                shareAcl = new FileSharePermissions();
                return shareAcl;
            };
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                this.UpdateETagAndLastModified(resp);
                return Task.Factory.StartNew(() =>
                {
                    ShareHttpResponseParsers.ReadSharedAccessIdentifiers(cmd.ResponseStream, shareAcl);
                    return shareAcl;
                });
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the GetStats method.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that gets the share stats.</returns>
        private RESTCommand<ShareStats> GetStatsImpl(FileRequestOptions requestOptions)
        {
            RESTCommand<ShareStats> retCmd = new RESTCommand<ShareStats>(this.ServiceClient.Credentials, this.StorageUri);
            requestOptions.ApplyToStorageCommand(retCmd);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.GetStats(uri, serverTimeout, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            retCmd.PostProcessResponse = (cmd, resp, ctx) => Task.Factory.StartNew(() => ShareHttpResponseParsers.ReadShareStats(cmd.ResponseStream));
            return retCmd;
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
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = ShareHttpRequestMessageFactory.SetMetadata(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
        /// Implementation for the SetProperties method.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the share. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetPropertiesImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = ShareHttpRequestMessageFactory.SetProperties(uri, serverTimeout, this.Properties, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
