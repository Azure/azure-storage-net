// -----------------------------------------------------------------------------------------
// <copyright file="CloudQueue.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Queue
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Queue.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
#if ASPNET_K || PORTABLE
    using System.Threading;
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
#endif

    /// <summary>
    /// This class represents a queue in the Windows Azure Queue service.
    /// </summary>
    public sealed partial class CloudQueue
    {
        /// <summary>
        /// Creates the queue.
        /// </summary>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task CreateAsync()
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction CreateAsync()
#endif
        {
            return this.CreateAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Creates the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task CreateAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.CreateAsync(options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction CreateAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.CreateQueueImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Creates the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task CreateAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.CreateQueueImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Creates the queue if it does not already exist.
        /// </summary>
        /// <returns><c>true</c> if the queue did not already exist and was created; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<bool> CreateIfNotExistsAsync()
#else
        public IAsyncOperation<bool> CreateIfNotExistsAsync()
#endif
        {
            return this.CreateIfNotExistsAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Creates the queue if it does not already exist.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the queue did not already exist and was created; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<bool> CreateIfNotExistsAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.CreateIfNotExistsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates the queue if it does not already exist.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the queue did not already exist and was created; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
        public Task<bool> CreateIfNotExistsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
#else
        public IAsyncOperation<bool> CreateIfNotExistsAsync(QueueRequestOptions options, OperationContext operationContext)
#endif
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

#if ASPNET_K || PORTABLE 
            return Task.Run(async () =>
            {
                bool exists = await this.ExistsAsync(true, modifiedOptions, operationContext, cancellationToken);
#else
            return AsyncInfo.Run(async (token) =>
            {
                bool exists = await this.ExistsAsync(true, modifiedOptions, operationContext).AsTask(token);
#endif

                if (exists)
                {
                    return false;
                }

                try
                {
#if ASPNET_K || PORTABLE 
                    await this.CreateAsync(modifiedOptions, operationContext, cancellationToken);
#else
                    await this.CreateAsync(modifiedOptions, operationContext).AsTask(token);
#endif
                    if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.NoContent)
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception)
                {
                    if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.Conflict)
                    {
                        StorageExtendedErrorInformation extendedInfo = operationContext.LastResult.ExtendedErrorInformation;
                        if ((extendedInfo == null) ||
                            (extendedInfo.ErrorCode == QueueErrorCodeStrings.QueueAlreadyExists))
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
#else
            });
#endif
        }

        /// <summary>
        /// Deletes the queue.
        /// </summary>
#if ASPNET_K || PORTABLE 
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task DeleteAsync()
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction DeleteAsync()
#endif
        {
            return this.DeleteAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Deletes the queue.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE 
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task DeleteAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction DeleteAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.DeleteQueueImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE 
        /// <summary>
        /// Deletes the queue.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task DeleteAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.DeleteQueueImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Deletes the queue if it already exists.
        /// </summary>
        /// <returns><c>true</c> if the queue already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE 
        public Task<bool> DeleteIfExistsAsync()
#else
        public IAsyncOperation<bool> DeleteIfExistsAsync()
#endif
        {
            return this.DeleteIfExistsAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Deletes the queue if it already exists.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the queue already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE 
        public Task<bool> DeleteIfExistsAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the queue if it already exists.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the queue already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public Task<bool> DeleteIfExistsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
#else        
        public IAsyncOperation<bool> DeleteIfExistsAsync(QueueRequestOptions options, OperationContext operationContext)
#endif
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

#if ASPNET_K || PORTABLE 
            return Task.Run(async () =>
            {
                bool exists = await this.ExistsAsync(true, modifiedOptions, operationContext, cancellationToken);
#else
            return AsyncInfo.Run(async (token) =>
            {
                bool exists = await this.ExistsAsync(true, modifiedOptions, operationContext).AsTask(token);
#endif

                if (!exists)
                {
                    return false;
                }

                try
                {
#if ASPNET_K || PORTABLE 
                    await this.DeleteAsync(modifiedOptions, operationContext, cancellationToken);
#else
                    await this.DeleteAsync(modifiedOptions, operationContext).AsTask(token);
#endif
                    return true;
                }
                catch (Exception)
                {
                    if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.NotFound)
                    {
                        StorageExtendedErrorInformation extendedInfo = operationContext.LastResult.ExtendedErrorInformation;
                        if ((extendedInfo == null) ||
                            (extendedInfo.ErrorCode == QueueErrorCodeStrings.QueueNotFound))
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
#else
            });
#endif
        }

#if !PORTABLE
        /// <summary>
        /// Sets permissions for the queue.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the queue.</param>
#if ASPNET_K || PORTABLE 
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task SetPermissionsAsync(QueuePermissions permissions)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction SetPermissionsAsync(QueuePermissions permissions)
#endif
        {
            return this.SetPermissionsAsync(permissions, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Sets permissions for the queue.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the queue.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE 
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.SetPermissionsAsync(permissions, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.SetPermissionsImpl(permissions, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE 
        /// <summary>
        /// Sets permissions for the queue.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the queue.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.SetPermissionsImpl(permissions, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Gets the permissions settings for the queue.
        /// </summary>
        /// <returns>The queue's permissions.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE 
        public Task<QueuePermissions> GetPermissionsAsync()
#else
        public IAsyncOperation<QueuePermissions> GetPermissionsAsync()
#endif
        {
            return this.GetPermissionsAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets the permissions settings for the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The queue's permissions.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE 
        public Task<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.GetPermissionsAsync(options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsync(
                this.GetPermissionsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE 
        /// <summary>
        /// Gets the permissions settings for the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The queue's permissions.</returns>
        [DoesServiceRequest]
        public Task<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsync(
                this.GetPermissionsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif 
#endif

        /// <summary>
        /// Checks existence of the queue.
        /// </summary>
        /// <returns><c>true</c> if the queue exists.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<bool> ExistsAsync()
#else
        public IAsyncOperation<bool> ExistsAsync()
#endif
        {
            return this.ExistsAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Checks existence of the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the queue exists.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<bool> ExistsAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(false, options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<bool> ExistsAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(false, options, operationContext);
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Checks existence of the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the queue exists.</returns>
        [DoesServiceRequest]
        public Task<bool> ExistsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.ExistsAsync(false, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Checks existence of the queue.
        /// </summary>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the queue exists.</returns>
        private Task<bool> ExistsAsync(bool primaryOnly, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsync(
                this.ExistsImpl(modifiedOptions, primaryOnly),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#else
        /// <returns><c>true</c> if the queue exists.</returns>
        private IAsyncOperation<bool> ExistsAsync(bool primaryOnly, QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsync(
                this.ExistsImpl(modifiedOptions, primaryOnly),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

        /// <summary>
        /// Retrieves the queue's attributes.
        /// </summary>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task FetchAttributesAsync()
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction FetchAttributesAsync()
#endif
        {
            return this.FetchAttributesAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Retrieves the queue's attributes.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task FetchAttributesAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction FetchAttributesAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
               this.FetchAttributesImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Retrieves the queue's attributes.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task FetchAttributesAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
               this.FetchAttributesImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Sets the queue's user-defined metadata.
        /// </summary>
#if ASPNET_K || PORTABLE 
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task SetMetadataAsync()
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction SetMetadataAsync()
#endif
        {
            return this.SetMetadataAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Sets the queue's user-defined metadata.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE 
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task SetMetadataAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction SetMetadataAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.SetMetadataImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE 
        /// <summary>
        /// Sets the queue's user-defined metadata.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task SetMetadataAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.SetMetadataImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Adds a message to the queue.
        /// </summary>
        /// <param name="message">The message to add.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task AddMessageAsync(CloudQueueMessage message)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction AddMessageAsync(CloudQueueMessage message)
#endif
        {
            return this.AddMessageAsync(message, null /* timeToLive */, null /* initialVisibilityDelay */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Adds a message to the queue.
        /// </summary>
        /// <param name="message">The message to add.</param>
        /// <param name="timeToLive">The maximum time to allow the message to be in the queue, or null.</param>
        /// <param name="initialVisibilityDelay">The length of time from now during which the message will be invisible.
        /// If <c>null</c> then the message will be visible immediately.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task AddMessageAsync(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.AddMessageAsync(message, timeToLive, initialVisibilityDelay, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction AddMessageAsync(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.AddMessageImpl(message, timeToLive, initialVisibilityDelay, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Adds a message to the queue.
        /// </summary>
        /// <param name="message">The message to add.</param>
        /// <param name="timeToLive">The maximum time to allow the message to be in the queue, or null.</param>
        /// <param name="initialVisibilityDelay">The length of time from now during which the message will be invisible.
        /// If <c>null</c> then the message will be visible immediately.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task AddMessageAsync(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.AddMessageImpl(message, timeToLive, initialVisibilityDelay, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Updates a message.
        /// </summary>
        /// <param name="message">The message to update.</param>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="updateFields">The message update fields.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields)
#endif
        {
            return this.UpdateMessageAsync(message, visibilityTimeout, updateFields, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Updates a message.
        /// </summary>
        /// <param name="message">The message to update.</param>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="updateFields">The message update fields.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.UpdateMessageAsync(message, visibilityTimeout, updateFields, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.UpdateMessageImpl(message, visibilityTimeout, updateFields, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Updates a message.
        /// </summary>
        /// <param name="message">The message to update.</param>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="updateFields">The message update fields.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.UpdateMessageImpl(message, visibilityTimeout, updateFields, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="message">The message to delete.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task DeleteMessageAsync(CloudQueueMessage message)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction DeleteMessageAsync(CloudQueueMessage message)
#endif
        {
            return this.DeleteMessageAsync(message, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task DeleteMessageAsync(CloudQueueMessage message, QueueRequestOptions options, OperationContext operationContext)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction DeleteMessageAsync(CloudQueueMessage message, QueueRequestOptions options, OperationContext operationContext)
#endif
        {
            return this.DeleteMessageAsync(message.Id, message.PopReceipt, options, operationContext);
        }

        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="popReceipt">The pop receipt value.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task DeleteMessageAsync(string messageId, string popReceipt)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction DeleteMessageAsync(string messageId, string popReceipt)
#endif
        {
            return this.DeleteMessageAsync(messageId, popReceipt, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="popReceipt">The pop receipt value.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task DeleteMessageAsync(string messageId, string popReceipt, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteMessageAsync(messageId, popReceipt, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction DeleteMessageAsync(string messageId, string popReceipt, QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.DeleteMessageImpl(messageId, popReceipt, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="popReceipt">The pop receipt value.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task DeleteMessageAsync(string messageId, string popReceipt, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.DeleteMessageImpl(messageId, popReceipt, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Gets a list of messages from the queue.
        /// </summary>
        /// <param name="messageCount">The number of messages to retrieve.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncOperation<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount)
#endif
        {
            return this.GetMessagesAsync(messageCount, null /* visibilityTimeout */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets a list of messages from the queue.
        /// </summary>
        /// <param name="messageCount">The number of messages to retrieve.</param>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of messages.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.GetMessagesAsync(messageCount, visibilityTimeout, options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsync(
                this.GetMessagesImpl(messageCount, visibilityTimeout, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Gets a list of messages from the queue.
        /// </summary>
        /// <param name="messageCount">The number of messages to retrieve.</param>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An enumerable collection of messages.</returns>
        [DoesServiceRequest]
        public Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsync(
                this.GetMessagesImpl(messageCount, visibilityTimeout, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Gets a single message from the queue.
        /// </summary>
        /// <returns>A message.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<CloudQueueMessage> GetMessageAsync()
#else
        public IAsyncOperation<CloudQueueMessage> GetMessageAsync()
#endif
        {
            return this.GetMessageAsync(null /* visibilityTimeout */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets a single message from the queue.
        /// </summary>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A message.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<CloudQueueMessage> GetMessageAsync(TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.GetMessageAsync(visibilityTimeout, options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<CloudQueueMessage> GetMessageAsync(TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsync(
                this.GetMessageImpl(visibilityTimeout, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Gets a single message from the queue.
        /// </summary>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A message.</returns>
        [DoesServiceRequest]
        public Task<CloudQueueMessage> GetMessageAsync(TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsync(
                this.GetMessageImpl(visibilityTimeout, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Peeks a list of messages from the queue.
        /// </summary>
        /// <param name="messageCount">The number of messages to retrieve.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncOperation<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount)
#endif
        {
            return this.PeekMessagesAsync(messageCount, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Peeks a list of messages from the queue.
        /// </summary>
        /// <param name="messageCount">The number of messages to retrieve.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of messages.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.PeekMessagesAsync(messageCount, null /* options */, null /* operationContext */, CancellationToken.None);
        }
#else
        public IAsyncOperation<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsync(
                this.PeekMessagesImpl(messageCount, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Peeks a list of messages from the queue.
        /// </summary>
        /// <param name="messageCount">The number of messages to retrieve.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An enumerable collection of messages.</returns>
        [DoesServiceRequest]
        public Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsync(
                this.PeekMessagesImpl(messageCount, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Peeks a single message from the queue.
        /// </summary>
        /// <returns>A message.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<CloudQueueMessage> PeekMessageAsync()
#else
        public IAsyncOperation<CloudQueueMessage> PeekMessageAsync()
#endif
        {
            return this.PeekMessageAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Peeks a single message from the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A message.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<CloudQueueMessage> PeekMessageAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.PeekMessageAsync(options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<CloudQueueMessage> PeekMessageAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsync(
                this.PeekMessageImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Peeks a single message from the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A message.</returns>
        [DoesServiceRequest]
        public Task<CloudQueueMessage> PeekMessageAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsync(
                this.PeekMessageImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Clears the messages of the queue.
        /// </summary>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task ClearAsync()
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction ClearAsync()
#endif
        {
            return this.ClearAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Clears the messages of the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task ClearAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.ClearAsync(options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction ClearAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.ClearImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Clears the messages of the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task ClearAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.ClearImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Implementation for the Create method.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the queue.</returns>
        private RESTCommand<NullType> CreateQueueImpl(QueueRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                HttpRequestMessage msg = QueueHttpRequestMessageFactory.Create(uri, serverTimeout, cnt, ctx);
                QueueHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpStatusCode[] expectedHttpStatusCodes = new HttpStatusCode[2];
                expectedHttpStatusCodes[0] = HttpStatusCode.Created;
                expectedHttpStatusCodes[1] = HttpStatusCode.NoContent;
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(expectedHttpStatusCodes, resp, NullType.Value, cmd, ex);
                GetMessageCountAndMetadataFromResponse(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Delete method.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that deletes the queue.</returns>
        private RESTCommand<NullType> DeleteQueueImpl(QueueRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.Delete(uri, serverTimeout, cnt, ctx);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Clear method.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that deletes the queue.</returns>
        private RESTCommand<NullType> ClearImpl(QueueRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.GetMessageRequestAddress());

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.Delete(uri, serverTimeout, cnt, ctx);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);

            return putCmd;
        }

        /// <summary>
        /// Implementation for the FetchAttributes method.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that fetches the attributes.</returns>
        private RESTCommand<NullType> FetchAttributesImpl(QueueRequestOptions options)
        {
            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetMetadata(uri, serverTimeout, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                GetMessageCountAndMetadataFromResponse(resp);
                return NullType.Value;
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the Exists method.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <returns>A <see cref="RESTCommand"/> that checks existence.</returns>
        private RESTCommand<bool> ExistsImpl(QueueRequestOptions options, bool primaryOnly)
        {
            RESTCommand<bool> getCmd = new RESTCommand<bool>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = primaryOnly ? CommandLocationMode.PrimaryOnly : CommandLocationMode.PrimaryOrSecondary;
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetMetadata(uri, serverTimeout, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                if (resp.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    return true;
                }

                return HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, true, cmd, ex);
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the SetMetadata method.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetMetadataImpl(QueueRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                HttpRequestMessage msg = QueueHttpRequestMessageFactory.SetMetadata(uri, serverTimeout, cnt, ctx);
                QueueHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);
                GetMessageCountAndMetadataFromResponse(resp);
                return NullType.Value;
            };

            return putCmd;
        }
        
#if !PORTABLE
        /// <summary>
        /// Implementation for the SetPermissions method.
        /// </summary>
        /// <param name="acl">The permissions to set.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the permissions.</returns>
        private RESTCommand<NullType> SetPermissionsImpl(QueuePermissions acl, QueueRequestOptions options)
        {
            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            QueueRequest.WriteSharedAccessIdentifiers(acl.SharedAccessPolicies, memoryStream);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.SetAcl(uri, serverTimeout, cnt, ctx);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null /* md5 */, cmd, ctx);
            putCmd.StreamToDispose = memoryStream;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);
                GetMessageCountAndMetadataFromResponse(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the GetPermissions method.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that gets the permissions.</returns>
        private RESTCommand<QueuePermissions> GetPermissionsImpl(QueueRequestOptions options)
        {
            RESTCommand<QueuePermissions> getCmd = new RESTCommand<QueuePermissions>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetAcl(uri, serverTimeout, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                this.GetMessageCountAndMetadataFromResponse(resp);
                return Task<QueuePermissions>.Factory.StartNew(() =>
                {
                    QueuePermissions queueAcl = new QueuePermissions();
                    QueueHttpResponseParsers.ReadSharedAccessIdentifiers(cmd.ResponseStream, queueAcl);
                    return queueAcl;
                });
            };

            return getCmd;
        }
#endif

        /// <summary>
        /// Implementation for the AddMessage method.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeToLive">The maximum time to allow the message to be in the queue, or null.</param>
        /// <param name="initialVisibilityDelay">The length of time from now during which the message will be invisible.
        /// If <c>null</c> then the message will be visible immediately.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the permissions.</returns>
        private RESTCommand<NullType> AddMessageImpl(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, QueueRequestOptions options)
        {
            int? timeToLiveInSeconds = null;
            int? initialVisibilityDelayInSeconds = null;

            if (timeToLive.HasValue)
            {
                CommonUtility.AssertInBounds("timeToLive", timeToLive.Value, TimeSpan.Zero, CloudQueueMessage.MaxTimeToLive);
                timeToLiveInSeconds = (int)timeToLive.Value.TotalSeconds;
            }

            if (initialVisibilityDelay.HasValue)
            {
                CommonUtility.AssertInBounds("initialVisibilityDelay", initialVisibilityDelay.Value, TimeSpan.Zero, timeToLive ?? CloudQueueMessage.MaxTimeToLive);
                initialVisibilityDelayInSeconds = (int)initialVisibilityDelay.Value.TotalSeconds;
            }

            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            QueueRequest.WriteMessageContent(message.GetMessageContentForTransfer(this.EncodeMessage), memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.GetMessageRequestAddress());

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.AddMessage(uri, serverTimeout, timeToLiveInSeconds, initialVisibilityDelayInSeconds, cnt, ctx);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null, cmd, ctx);
            putCmd.StreamToDispose = memoryStream;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                GetMessageCountAndMetadataFromResponse(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the UpdateMessage method.
        /// </summary>
        /// <param name="message">The message to update.</param>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="updateFields">The message update fields.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the permissions.</returns>
        private RESTCommand<NullType> UpdateMessageImpl(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options)
        {
            CommonUtility.AssertNotNull("message", message);
            CommonUtility.AssertNotNullOrEmpty("messageId", message.Id);
            CommonUtility.AssertNotNullOrEmpty("popReceipt", message.PopReceipt);
            CommonUtility.AssertInBounds("visibilityTimeout", visibilityTimeout, TimeSpan.Zero, CloudQueueMessage.MaxTimeToLive);

            if ((updateFields & MessageUpdateFields.Visibility) == 0)
            {
                throw new ArgumentException(SR.UpdateMessageVisibilityRequired, "updateFlags");
            }

            StorageUri messageUri = this.GetIndividualMessageAddress(message.Id);
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, messageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.UpdateMessage(uri, serverTimeout, message.PopReceipt, visibilityTimeout, cnt, ctx);

            if ((updateFields & MessageUpdateFields.Content) != 0)
            {
                MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
                QueueRequest.WriteMessageContent(message.GetMessageContentForTransfer(this.EncodeMessage), memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null, cmd, ctx);
                putCmd.StreamToDispose = memoryStream;
            }

            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);
                GetPopReceiptAndNextVisibleTimeFromResponse(message, resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the DeleteMessage method.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="popReceipt">The pop receipt value.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that deletes the queue.</returns>
        private RESTCommand<NullType> DeleteMessageImpl(string messageId, string popReceipt, QueueRequestOptions options)
        {
            StorageUri messageUri = this.GetIndividualMessageAddress(messageId);
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, messageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.DeleteMessage(uri, serverTimeout, popReceipt, cnt, ctx);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);

            return putCmd;
        }

        /// <summary>
        /// Implementation for the GetPermissions method.
        /// </summary>
        /// <param name="messageCount">The message count.</param>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that gets the permissions.</returns>
        private RESTCommand<IEnumerable<CloudQueueMessage>> GetMessagesImpl(int messageCount, TimeSpan? visibilityTimeout, QueueRequestOptions options)
        {
            RESTCommand<IEnumerable<CloudQueueMessage>> getCmd = new RESTCommand<IEnumerable<CloudQueueMessage>>(this.ServiceClient.Credentials, this.GetMessageRequestAddress());

            options.ApplyToStorageCommand(getCmd);
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetMessages(uri, serverTimeout, messageCount, visibilityTimeout, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    GetMessagesResponse getMessagesResponse = new GetMessagesResponse(cmd.ResponseStream);

                    IEnumerable<CloudQueueMessage> messagesList = getMessagesResponse.Messages.Select(item => SelectGetMessageResponse(item)).ToList();

                    return messagesList;
                });
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the PeekMessages method.
        /// </summary>
        /// <param name="messageCount">The message count.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that gets the permissions.</returns>
        private RESTCommand<IEnumerable<CloudQueueMessage>> PeekMessagesImpl(int messageCount, QueueRequestOptions options)
        {
            RESTCommand<IEnumerable<CloudQueueMessage>> getCmd = new RESTCommand<IEnumerable<CloudQueueMessage>>(this.ServiceClient.Credentials, this.GetMessageRequestAddress());

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.PeekMessages(uri, serverTimeout, messageCount, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    GetMessagesResponse getMessagesResponse = new GetMessagesResponse(cmd.ResponseStream);

                    IEnumerable<CloudQueueMessage> messagesList = getMessagesResponse.Messages.Select(item => SelectPeekMessageResponse(item)).ToList();

                    return messagesList;
                });
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the GetPermissions method.
        /// </summary>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that gets the permissions.</returns>
        private RESTCommand<CloudQueueMessage> GetMessageImpl(TimeSpan? visibilityTimeout, QueueRequestOptions options)
        {
            RESTCommand<CloudQueueMessage> getCmd = new RESTCommand<CloudQueueMessage>(this.ServiceClient.Credentials, this.GetMessageRequestAddress());

            options.ApplyToStorageCommand(getCmd);
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetMessages(uri, serverTimeout, 1, visibilityTimeout, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    using (IEnumerator<QueueMessage> enumerator = new GetMessagesResponse(cmd.ResponseStream).Messages.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                        {
                            return SelectGetMessageResponse(enumerator.Current);
                        }
                    }

                    return null;
                });
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the PeekMessage method.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that gets the permissions.</returns>
        private RESTCommand<CloudQueueMessage> PeekMessageImpl(QueueRequestOptions options)
        {
            RESTCommand<CloudQueueMessage> getCmd = new RESTCommand<CloudQueueMessage>(this.ServiceClient.Credentials, this.GetMessageRequestAddress());

            options.ApplyToStorageCommand(getCmd);
            getCmd.Handler = this.ServiceClient.AuthenticationHandler;
            getCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.PeekMessages(uri, serverTimeout, 1, cnt, ctx);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    using (IEnumerator<QueueMessage> enumerator = new GetMessagesResponse(cmd.ResponseStream).Messages.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                        {
                            return SelectPeekMessageResponse(enumerator.Current);
                        }
                    }

                    return null;
                });
            };

            return getCmd;
        }

        /// <summary>
        /// Gets the ApproximateMessageCount and metadata from response.
        /// </summary>
        /// <param name="response">The web response.</param>
        private void GetMessageCountAndMetadataFromResponse(HttpResponseMessage response)
        {
            this.Metadata = QueueHttpResponseParsers.GetMetadata(response);

            string count = QueueHttpResponseParsers.GetApproximateMessageCount(response);
            this.ApproximateMessageCount = string.IsNullOrEmpty(count) ? (int?)null : int.Parse(count);
        }

        /// <summary>
        /// Update the message pop receipt and next visible time.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="webResponse">The web response.</param>
        private void GetPopReceiptAndNextVisibleTimeFromResponse(CloudQueueMessage message, HttpResponseMessage webResponse)
        {
            message.PopReceipt = webResponse.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.PopReceipt);
            message.NextVisibleTime = DateTime.Parse(
                webResponse.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.NextVisibleTime),
                System.Globalization.DateTimeFormatInfo.InvariantInfo,
                System.Globalization.DateTimeStyles.AdjustToUniversal);
        }
    }
}
