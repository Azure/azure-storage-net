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
    using System.Threading;
    using System.Globalization;
#if NETCORE
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
#endif

    public partial class CloudQueue
    {
        /// <summary>
        /// Creates the queue.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync()
        {
            return this.CreateAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Creates the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.CreateAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsyncNullReturn(
                this.CreateQueueImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Creates the queue if it does not already exist.
        /// </summary>
        /// <returns><c>true</c> if the queue did not already exist and was created; otherwise, <c>false</c>.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync()
        {
            return this.CreateIfNotExistsAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Creates the queue if it does not already exist.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the queue did not already exist and was created; otherwise <c>false</c>.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(QueueRequestOptions options, OperationContext operationContext)
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
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual async Task<bool> CreateIfNotExistsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            try
            {
                await this.CreateAsync(modifiedOptions, operationContext, cancellationToken).ConfigureAwait(false);
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
#pragma warning disable 618
                    if ((extendedInfo == null) ||
                        (extendedInfo.ErrorCode == QueueErrorCodeStrings.QueueAlreadyExists))
#pragma warning restore 618
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
        }

        /// <summary>
        /// Deletes the queue.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
        {
            return this.DeleteAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Deletes the queue.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the queue.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsyncNullReturn(
                this.DeleteQueueImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Deletes the queue if it already exists.
        /// </summary>
        /// <returns><c>true</c> if the queue already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
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
        public virtual Task<bool> DeleteIfExistsAsync(QueueRequestOptions options, OperationContext operationContext)
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
        public virtual async Task<bool> DeleteIfExistsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            try
            {
                bool exists = await this.ExistsAsync(modifiedOptions, operationContext, cancellationToken).ConfigureAwait(false);
                if (!exists)
                {
                    return false;
                }
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode != (int)HttpStatusCode.Forbidden)
                {
                    throw;
                }
            }

            try
            {
                await this.DeleteAsync(modifiedOptions, operationContext, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception)
            {
                if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    StorageExtendedErrorInformation extendedInfo = operationContext.LastResult.ExtendedErrorInformation;
                    if ((extendedInfo == null) ||
#pragma warning disable 618
                        (extendedInfo.ErrorCode == QueueErrorCodeStrings.QueueNotFound))
#pragma warning restore 618
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
        }

        /// <summary>
        /// Sets permissions for the queue.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the queue.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(QueuePermissions permissions)
        {
            return this.SetPermissionsAsync(permissions, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Sets permissions for the queue.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the queue.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.SetPermissionsAsync(permissions, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Sets permissions for the queue.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the queue.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsyncNullReturn(
                this.SetPermissionsImpl(permissions, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Gets the permissions settings for the queue.
        /// </summary>
        /// <returns>The queue's permissions.</returns>
        [DoesServiceRequest]
        public virtual Task<QueuePermissions> GetPermissionsAsync()
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
        public virtual Task<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.GetPermissionsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the permissions settings for the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The queue's permissions.</returns>
        [DoesServiceRequest]
        public virtual Task<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsync(
                this.GetPermissionsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Checks existence of the queue.
        /// </summary>
        /// <returns><c>true</c> if the queue exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
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
        public virtual Task<bool> ExistsAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(false, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Checks existence of the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the queue exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.ExistsAsync(false, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Checks existence of the queue.
        /// </summary>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the queue exists.</returns>
        private Task<bool> ExistsAsync(bool primaryOnly, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsync(
                this.ExistsImpl(modifiedOptions, primaryOnly),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Retrieves the queue's attributes.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync()
        {
            return this.FetchAttributesAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Retrieves the queue's attributes.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves the queue's attributes.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsyncNullReturn(
                this.FetchAttributesImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Sets the queue's user-defined metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync()
        {
            return this.SetMetadataAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Sets the queue's user-defined metadata.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Sets the queue's user-defined metadata.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsyncNullReturn(
                this.SetMetadataImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Adds a message to the queue.
        /// </summary>
        /// <param name="message">The message to add.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>The <see cref="CloudQueueMessage"/> message passed in will be populated with the pop receipt, message ID, and the insertion/expiration time.</remarks>
        [DoesServiceRequest]
        public virtual Task AddMessageAsync(CloudQueueMessage message)
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
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>The <see cref="CloudQueueMessage"/> message passed in will be populated with the pop receipt, message ID, and the insertion/expiration time.</remarks>
        [DoesServiceRequest]
        public virtual Task AddMessageAsync(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.AddMessageAsync(message, timeToLive, initialVisibilityDelay, options, operationContext, CancellationToken.None);
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
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>The <see cref="CloudQueueMessage"/> message passed in will be populated with the pop receipt, message ID, and the insertion/expiration time.</remarks>
        [DoesServiceRequest]
        public virtual Task AddMessageAsync(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsyncNullReturn(
                this.AddMessageImpl(message, timeToLive, initialVisibilityDelay, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Updates a message.
        /// </summary>
        /// <param name="message">The message to update.</param>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="updateFields">The message update fields.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields)
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
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.UpdateMessageAsync(message, visibilityTimeout, updateFields, options, operationContext, CancellationToken.None);
        }

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
        public virtual Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsyncNullReturn(
                this.UpdateMessageImpl(message, visibilityTimeout, updateFields, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteMessageAsync(CloudQueueMessage message)
        {
            return this.DeleteMessageAsync(message, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteMessageAsync(CloudQueueMessage message, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteMessageAsync(message.Id, message.PopReceipt, options, operationContext);
        }

        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="popReceipt">The pop receipt value.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteMessageAsync(string messageId, string popReceipt)
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
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteMessageAsync(string messageId, string popReceipt, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteMessageAsync(messageId, popReceipt, options, operationContext, CancellationToken.None);
        }

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
        public virtual Task DeleteMessageAsync(string messageId, string popReceipt, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsyncNullReturn(
                this.DeleteMessageImpl(messageId, popReceipt, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Gets a list of messages from the queue.
        /// </summary>
        /// <param name="messageCount">The number of messages to retrieve.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount)
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
        public virtual Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.GetMessagesAsync(messageCount, visibilityTimeout, options, operationContext, CancellationToken.None);
        }

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
        public virtual Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsync(
                this.GetMessagesImpl(messageCount, visibilityTimeout, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Gets a single message from the queue.
        /// </summary>
        /// <returns>A message.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudQueueMessage> GetMessageAsync()
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
        public virtual Task<CloudQueueMessage> GetMessageAsync(TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.GetMessageAsync(visibilityTimeout, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets a single message from the queue.
        /// </summary>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A message.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudQueueMessage> GetMessageAsync(TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsync(
                this.GetMessageImpl(visibilityTimeout, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Peeks a list of messages from the queue.
        /// </summary>
        /// <param name="messageCount">The number of messages to retrieve.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount)
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
        public virtual Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options, OperationContext operationContext)
        {
            return this.PeekMessagesAsync(messageCount, null /* options */, null /* operationContext */, CancellationToken.None);
        }

        /// <summary>
        /// Peeks a list of messages from the queue.
        /// </summary>
        /// <param name="messageCount">The number of messages to retrieve.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An enumerable collection of messages.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsync(
                this.PeekMessagesImpl(messageCount, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Peeks a single message from the queue.
        /// </summary>
        /// <returns>A message.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudQueueMessage> PeekMessageAsync()
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
        public virtual Task<CloudQueueMessage> PeekMessageAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.PeekMessageAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Peeks a single message from the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A message.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudQueueMessage> PeekMessageAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsync(
                this.PeekMessageImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Clears the messages of the queue.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ClearAsync()
        {
            return this.ClearAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Clears the messages of the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ClearAsync(QueueRequestOptions options, OperationContext operationContext)
        {
            return this.ClearAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Clears the messages of the queue.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ClearAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            QueueRequestOptions modifiedOptions = QueueRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsyncNullReturn(
                this.ClearImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Implementation for the Create method.
        /// </summary>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the queue.</returns>
        private RESTCommand<NullType> CreateQueueImpl(QueueRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = QueueHttpRequestMessageFactory.Create(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                QueueHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpStatusCode[] expectedHttpStatusCodes = new HttpStatusCode[2];
                expectedHttpStatusCodes[0] = HttpStatusCode.Created;
                expectedHttpStatusCodes[1] = HttpStatusCode.NoContent;
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(expectedHttpStatusCodes, resp, NullType.Value, cmd, ex);

                // no payload to parse

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
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.Delete(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.Delete(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetMetadata(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetMetadata(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = QueueHttpRequestMessageFactory.SetMetadata(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                QueueHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);

                // no payload to parse

                return NullType.Value;
            };

            return putCmd;
        }

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
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.SetAcl(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null /* md5 */, cmd, ctx);
            putCmd.StreamToDispose = memoryStream;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);

                // no payload to parse

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
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetAcl(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = async (cmd, resp, ctx, ct) =>
            {
                QueuePermissions queueAcl = new QueuePermissions();
                await QueueHttpResponseParsers.ReadSharedAccessIdentifiersAsync(cmd.ResponseStream, queueAcl, ct).ConfigureAwait(false);
                return queueAcl;
            };

            return getCmd;
        }

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
            long? timeToLiveInSeconds = null;
            int? initialVisibilityDelayInSeconds = null;

            if (timeToLive.HasValue)
            {
                if (timeToLive.Value.Equals(TimeSpan.FromSeconds(-1)) || timeToLive.Value >= TimeSpan.Zero)
                {
                    timeToLiveInSeconds = (long)timeToLive.Value.TotalSeconds;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("timeToLive", string.Format(CultureInfo.InvariantCulture, SR.ArgumentOutOfRangeError, timeToLive));
                }
            }

            if (initialVisibilityDelay.HasValue)
            {
                // If ttl is null, it will default to 7 days (MaxVisibilityTimeout) on the service
                TimeSpan? effectiveTtl = timeToLive ?? CloudQueueMessage.MaxVisibilityTimeout;

                // Ensures the visibilityTimeout is less than the max allowed and strictly less than the TTL
                TimeSpan visibilityUpperBound = ((effectiveTtl.Value < TimeSpan.Zero) ||
                    (effectiveTtl.Value.Add(TimeSpan.FromSeconds(-1)) > CloudQueueMessage.MaxVisibilityTimeout)) ?
                    CloudQueueMessage.MaxVisibilityTimeout : effectiveTtl.Value.Add(TimeSpan.FromSeconds(-1));
                CommonUtility.AssertInBounds("initialVisibilityDelay", initialVisibilityDelay.Value, TimeSpan.Zero, visibilityUpperBound);

                initialVisibilityDelayInSeconds = (int)initialVisibilityDelay.Value.TotalSeconds;
            }

            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            QueueRequest.WriteMessageContent(message.GetMessageContentForTransfer(this.EncodeMessage), memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.GetMessageRequestAddress());

            options.ApplyToStorageCommand(putCmd);
            putCmd.RetrieveResponseStream = true;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.AddMessage(uri, serverTimeout, timeToLiveInSeconds, initialVisibilityDelayInSeconds, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null, cmd, ctx);
            putCmd.StreamToDispose = memoryStream;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, null /* retVal */, cmd, ex);
            putCmd.PostProcessResponseAsync = async (cmd, resp, ctx, ct) =>
            {
                var messages = await GetMessagesResponse.ParseAsync(cmd.ResponseStream, ct).ConfigureAwait(false);
                CopyMessage(message, messages.First());
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
            CommonUtility.AssertInBounds("visibilityTimeout", visibilityTimeout, TimeSpan.Zero, CloudQueueMessage.MaxVisibilityTimeout);

            if ((updateFields & MessageUpdateFields.Visibility) == 0)
            {
                throw new ArgumentException(SR.UpdateMessageVisibilityRequired, "updateFlags");
            }

            StorageUri messageUri = this.GetIndividualMessageAddress(message.Id);
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, messageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.UpdateMessage(uri, serverTimeout, message.PopReceipt, visibilityTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);

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
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.DeleteMessage(uri, serverTimeout, popReceipt, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetMessages(uri, serverTimeout, messageCount, visibilityTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = async (cmd, resp, ctx, ct) =>
            {
                return
                    (await GetMessagesResponse.ParseAsync(cmd.ResponseStream, ct).ConfigureAwait(false))
                    .Select(m => SelectGetMessageResponse(m, options))
                    .ToList();
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
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.PeekMessages(uri, serverTimeout, messageCount, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null, cmd, ex);
            getCmd.PostProcessResponseAsync = async (cmd, resp, ctx, ct) =>
            {
                return
                    (await GetMessagesResponse.ParseAsync(cmd.ResponseStream, ct).ConfigureAwait(false))
                    .Select(m => SelectPeekMessageResponse(m, options))
                    .ToList()
                    ;
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the GetMessage method.
        /// </summary>
        /// <param name="visibilityTimeout">The visibility timeout interval.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that gets the permissions.</returns>
        private RESTCommand<CloudQueueMessage> GetMessageImpl(TimeSpan? visibilityTimeout, QueueRequestOptions options)
        {
            RESTCommand<CloudQueueMessage> getCmd = new RESTCommand<CloudQueueMessage>(this.ServiceClient.Credentials, this.GetMessageRequestAddress());

            options.ApplyToStorageCommand(getCmd);
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.GetMessages(uri, serverTimeout, 1, visibilityTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = async (cmd, resp, ctx, ct) =>
            {
                return
                    (await GetMessagesResponse.ParseAsync(cmd.ResponseStream, ct).ConfigureAwait(false))
                    .Select(m => SelectGetMessageResponse(m, options))
                    .FirstOrDefault();
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
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => QueueHttpRequestMessageFactory.PeekMessages(uri, serverTimeout, 1, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = async (cmd, resp, ctx, ct) =>
            {
                return
                    (await GetMessagesResponse.ParseAsync(cmd.ResponseStream, ct).ConfigureAwait(false))
                    .Select(m => SelectPeekMessageResponse(m, options))
                    .FirstOrDefault();
            };

            return getCmd;
        }

        private static readonly Task<CloudQueueMessage> Null = Task.FromResult(default(CloudQueueMessage));

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
