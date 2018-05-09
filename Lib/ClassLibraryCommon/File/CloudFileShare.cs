//-----------------------------------------------------------------------
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
//-----------------------------------------------------------------------

namespace Microsoft.Azure.Storage.File
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.File.Protocol;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a share in the Microsoft Azure File service.
    /// </summary>
    /// <remarks>Shares hold directories, which are encapsulated as <see cref="CloudFileDirectory"/> objects, and directories hold files. Directories can also contain sub-directories.</remarks>
    public partial class CloudFileShare
    {
#if SYNC
        /// <summary>
        /// Creates the share.
        /// </summary>
        /// <param name="requestOptions">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation. This object
        /// is used to track requests to the storage service, and to provide additional runtime information about the operation. </param>
        [DoesServiceRequest]
        public virtual void Create(FileRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();

            if (this.Properties.Quota.HasValue)
            {
                CommonUtility.AssertInBounds("Quota", this.Properties.Quota.Value, 1);
            }

            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            Executor.ExecuteSync(
                this.CreateShareImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to create a share.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreate(AsyncCallback callback, object state)
        {
            return this.BeginCreate(null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to create a share.
        /// </summary>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreate(FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper(token => CreateAsync(options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to create a share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndCreate(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a share.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync()
        {
            return this.CreateAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a share.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(CancellationToken cancellationToken)
        {
            return this.CreateAsync(null, null, cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a share.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.CreateAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a share.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();

            if (this.Properties.Quota.HasValue)
            {
                CommonUtility.AssertInBounds("Quota", this.Properties.Quota.Value, 1);
            }

            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsync(
                this.CreateShareImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Creates the share if it does not already exist.
        /// </summary>
        /// <param name="requestOptions">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise <c>false</c>.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual bool CreateIfNotExists(FileRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            try
            {
                this.Create(requestOptions, operationContext);
                return true;
            }
            catch (StorageException e)
            {
                if ((e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict) &&
                    ((e.RequestInformation.ExtendedErrorInformation == null) ||
                    (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ShareAlreadyExists)))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreateIfNotExists(AsyncCallback callback, object state)
        {
            return this.BeginCreateIfNotExists(null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreateIfNotExists(FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper<bool>(token => CreateIfNotExistsAsync(options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the result of an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise, <c>false</c>.</returns>
        public virtual bool EndCreateIfNotExists(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<bool>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync()
        {
            return this.CreateIfNotExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(CancellationToken cancellationToken)
        {
            return this.CreateIfNotExistsAsync(options: null, operationContext:null, cancellationToken:cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.CreateIfNotExistsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual async Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            try
            {
                await this.CreateAsync(options, operationContext, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (StorageException e)
            {
                if ((e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict) &&
                    ((e.RequestInformation.ExtendedErrorInformation == null) ||
                    (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ShareAlreadyExists)))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }
#endif

#if SYNC
        /// <summary>
        /// Creates a snapshot of the share.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request, or <c>null</c>. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="CloudFileShare"/> object that is a share snapshot.</returns>
        [DoesServiceRequest]
        public CloudFileShare Snapshot(IDictionary<string, string> metadata = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteSync(
                this.SnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to create a snapshot of the share.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public ICancellableAsyncResult BeginSnapshot(AsyncCallback callback, object state)
        {
            return this.BeginSnapshot(null /* metadata */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to create a snapshot of the share.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public ICancellableAsyncResult BeginSnapshot(IDictionary<string, string> metadata, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper<CloudFileShare>(token => this.SnapshotAsync(metadata, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to create a snapshot of the share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="CloudFileShare"/> object that is a share snapshot.</returns>
        public CloudFileShare EndSnapshot(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<CloudFileShare>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the share.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudFileShare"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public Task<CloudFileShare> SnapshotAsync()
        {
            return this.SnapshotAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the share.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudFileShare"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public Task<CloudFileShare> SnapshotAsync(CancellationToken cancellationToken)
        {
            return this.SnapshotAsync(default(IDictionary<string, string>), default(AccessCondition), default(FileRequestOptions),default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the share.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudFileShare"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public Task<CloudFileShare> SnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SnapshotAsync(metadata, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the share.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudFileShare"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public Task<CloudFileShare> SnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync<CloudFileShare>(
                this.SnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Deletes the share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void Delete(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.Delete(DeleteShareSnapshotsOption.None, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Deletes the share.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteShareSnapshotsOption"/> object indicating whether to only delete the share or delete the share and all snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void Delete(DeleteShareSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.DeleteShareImpl(deleteSnapshotsOption, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to delete a share.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(AsyncCallback callback, object state)
        {
            return this.BeginDelete(DeleteShareSnapshotsOption.None, null /* accessCondition */, null /* options */, null /*operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to delete a share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginDelete(DeleteShareSnapshotsOption.None, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to delete a share.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteShareSnapshotsOption"/> object indicating whether to only delete the share or delete the share and all snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(DeleteShareSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition,
            FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper(token => this.DeleteAsync(deleteSnapshotsOption, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to delete a share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDelete(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete a share.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
        {
            return this.DeleteAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete a share.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(CancellationToken cancellationToken)
        {
            return this.DeleteAsync(DeleteShareSnapshotsOption.None, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete a share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(DeleteShareSnapshotsOption.None, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete a share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DeleteAsync(DeleteShareSnapshotsOption.None, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete a share.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteShareSnapshotsOption"/> object indicating whether to only delete the share or delete the share and all snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(DeleteShareSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.DeleteShareImpl(deleteSnapshotsOption, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Deletes the share if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual bool DeleteIfExists(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            return this.DeleteIfExists(DeleteShareSnapshotsOption.None, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Deletes the share if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteShareSnapshotsOption"/> object indicating whether to only delete the share or delete the share and all snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual bool DeleteIfExists(DeleteShareSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            try
            {
                bool exists = this.Exists(modifiedOptions, operationContext);
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
                this.Delete(deleteSnapshotsOption, accessCondition, modifiedOptions, operationContext);
                return true;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    if ((e.RequestInformation.ExtendedErrorInformation == null) ||
                        (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ShareNotFound))
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
#endif

        /// <summary>
        /// Begins an asynchronous request to delete the share if it already exists.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(AsyncCallback callback, object state)
        {
            return this.BeginDeleteIfExists(DeleteShareSnapshotsOption.None, null, null, null, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to delete the share if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginDeleteIfExists(DeleteShareSnapshotsOption.None, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to delete the share if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteShareSnapshotsOption"/> object indicating whether to only delete the share or delete the share and all snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(DeleteShareSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition,
            FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper<bool>(token => this.DeleteIfExistsAsync(deleteSnapshotsOption, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the result of an asynchronous request to delete the share if it already exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise, <c>false</c>.</returns>
        public virtual bool EndDeleteIfExists(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<bool>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the share if it already exists.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
        {
            return this.DeleteIfExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the share if it already exists.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(CancellationToken cancellationToken)
        {
            return this.DeleteIfExistsAsync(DeleteShareSnapshotsOption.None, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the share if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(DeleteShareSnapshotsOption.None, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the share if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DeleteIfExistsAsync(DeleteShareSnapshotsOption.None, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the share if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteShareSnapshotsOption"/> object indicating whether to only delete the share or delete the share and all snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<bool> DeleteIfExistsAsync(DeleteShareSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition,
            FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
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
                await this.DeleteAsync(deleteSnapshotsOption, accessCondition, modifiedOptions, operationContext, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    if ((e.RequestInformation.ExtendedErrorInformation == null) ||
                        (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ShareNotFound))
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
#endif

#if SYNC
        /// <summary>
        /// Checks whether the share exists.
        /// </summary>
        /// <param name="requestOptions">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the share exists.</returns>
        [DoesServiceRequest]
        public virtual bool Exists(FileRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            return Executor.ExecuteSync(
                this.ExistsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous request to check whether the share exists.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExists(AsyncCallback callback, object state)
        {
            return this.BeginExists(null, null, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to check whether the share exists.
        /// </summary>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExists(FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper<bool>(token => ExistsAsync(options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the asynchronous result of the request to check whether the share exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the share exists.</returns>
        public virtual bool EndExists(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<bool>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous request to check whether the share exists.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
        {
            return this.ExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to check whether the share exists.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            return this.ExistsAsync(default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to check whether the share exists.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to check whether the share exists.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.ExistsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Retrieves the share's attributes.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void FetchAttributes(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.FetchAttributesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to retrieve the share's attributes.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginFetchAttributes(AsyncCallback callback, object state)
        {
            return this.BeginFetchAttributes(null, null, null, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to retrieve the share's attributes.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginFetchAttributes(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper(token => this.FetchAttributesAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to retrieve the share's attributes.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndFetchAttributes(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to retrieve the share's attributes.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync()
        {
            return this.FetchAttributesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to retrieve the share's attributes.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(CancellationToken cancellationToken)
        {
            return this.FetchAttributesAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to retrieve the share's attributes.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to retrieve the share's attributes.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.FetchAttributesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Gets the permissions settings for the share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="FileSharePermissions"/> object.</returns>
        [DoesServiceRequest]
        public virtual FileSharePermissions GetPermissions(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteSync(
                this.GetPermissionsImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous request to get the permissions settings for the share.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetPermissions(AsyncCallback callback, object state)
        {
            return this.BeginGetPermissions(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to get the permissions settings for the share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetPermissions(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper<FileSharePermissions>(token => this.GetPermissionsAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the asynchronous result of the request to get the permissions settings for the share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="FileSharePermissions"/> object.</returns>
        public virtual FileSharePermissions EndGetPermissions(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<FileSharePermissions>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation that gets the permissions settings for the share.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="FileSharePermissions"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<FileSharePermissions> GetPermissionsAsync()
        {
            return this.GetPermissionsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that gets the permissions settings for the share.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="FileSharePermissions"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<FileSharePermissions> GetPermissionsAsync(CancellationToken cancellationToken)
        {
            return this.GetPermissionsAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation that gets the permissions settings for the share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="FileSharePermissions"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<FileSharePermissions> GetPermissionsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.GetPermissionsAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that gets the permissions settings for the share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="FileSharePermissions"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<FileSharePermissions> GetPermissionsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync<FileSharePermissions>(
                this.GetPermissionsImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Gets stats for the share.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="ShareStats"/> object.</returns>
        [DoesServiceRequest]
        public virtual ShareStats GetStats(FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            options = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            return Executor.ExecuteSync(
                this.GetStatsImpl(options),
                options.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to get stats for the share.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetStats(AsyncCallback callback, object state)
        {
            return this.BeginGetStats(null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to get stats for the share.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetStats(FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper<ShareStats>(token => this.GetStatsAsync(options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to get stats for the share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="ShareStats"/> object.</returns>
        public virtual ShareStats EndGetStats(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<ShareStats>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to get stats for the share.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ShareStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ShareStats> GetStatsAsync()
        {
            return this.GetStatsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get stats for the share.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ShareStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ShareStats> GetStatsAsync(CancellationToken cancellationToken)
        {
            return this.GetStatsAsync(default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get stats for the share.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ShareStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ShareStats> GetStatsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.GetStatsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get stats for the share.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="ShareStats"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<ShareStats> GetStatsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync<ShareStats>(
                this.GetStatsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Sets the share's user-defined metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void SetMetadata(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.SetMetadataImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to set user-defined metadata on the share.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetMetadata(AsyncCallback callback, object state)
        {
            return this.BeginSetMetadata(null, null, null, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to set user-defined metadata on the share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetMetadata(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper(token => this.SetMetadataAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous request operation to set user-defined metadata on the share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetMetadata(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to set user-defined metadata on the share.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync()
        {
            return this.SetMetadataAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to set user-defined metadata on the share.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(CancellationToken cancellationToken)
        {
            return this.SetMetadataAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to set user-defined metadata on the share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to set user-defined metadata on the share.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.SetMetadataImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Sets permissions for the share.
        /// </summary>
        /// <param name="permissions">A <see cref="FileSharePermissions"/> object.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void SetPermissions(FileSharePermissions permissions, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.SetPermissionsImpl(permissions, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous request to set permissions for the share.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the share.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetPermissions(FileSharePermissions permissions, AsyncCallback callback, object state)
        {
            return this.BeginSetPermissions(permissions, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to set permissions for the share.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the share.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetPermissions(FileSharePermissions permissions, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper(token => this.SetPermissionsAsync(permissions, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the result of an asynchronous request to set permissions for the share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetPermissions(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation that sets permissions for the share.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the share.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(FileSharePermissions permissions)
        {
            return this.SetPermissionsAsync(permissions, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that sets permissions for the share.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the share.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(FileSharePermissions permissions, CancellationToken cancellationToken)
        {
            return this.SetPermissionsAsync(permissions, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation that sets permissions for the share.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the share.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(FileSharePermissions permissions, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetPermissionsAsync(permissions, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation that sets permissions for the share.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the share.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPermissionsAsync(FileSharePermissions permissions, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.SetPermissionsImpl(permissions, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Updates the share's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void SetProperties(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();

            if (this.Properties.Quota.HasValue)
            {
                CommonUtility.AssertInBounds("Quota", this.Properties.Quota.Value, 1);
            }

            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.SetPropertiesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to update the share's properties.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetProperties(AsyncCallback callback, object state)
        {
            return this.BeginSetProperties(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to update the share's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetProperties(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper(token => this.SetPropertiesAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to update the share's properties.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetProperties(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to update the share's properties.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync()
        {
            return this.SetPropertiesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the share's properties.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(CancellationToken cancellationToken)
        {
            return this.SetPropertiesAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the share's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetPropertiesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the share's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();
            if (this.Properties.Quota.HasValue)
            {
                CommonUtility.AssertInBounds("Quota", this.Properties.Quota.Value, 1);
            }

            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.SetPropertiesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

        /// <summary>
        /// Implementation for the Create method.
        /// </summary>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that creates the share.</returns>
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
        /// Implementation for the Snapshot method.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot, or <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that creates the snapshot.</returns>
        /// <remarks>If the <c>metadata</c> parameter is <c>null</c> then no metadata is associated with the request.</remarks>
        internal RESTCommand<CloudFileShare> SnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<CloudFileShare> putCmd = new RESTCommand<CloudFileShare>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = ShareHttpRequestMessageFactory.Snapshot(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                if (metadata != null)
                {
                    FileHttpRequestMessageFactory.AddMetadata(msg, metadata);
                }

                return msg;
            };

            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, null /* retVal */, cmd, ex);
                DateTimeOffset snapshotTime = NavigationHelper.ParseSnapshotTime(ShareHttpResponseParsers.GetSnapshotTime(resp));
                CloudFileShare snapshot = new CloudFileShare(this.Properties, new Dictionary<string, string>(metadata ?? this.Metadata), this.Name, snapshotTime, this.ServiceClient);
                this.UpdateETagAndLastModified(resp);
                return snapshot;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Delete method.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteShareSnapshotsOption"/> object indicating whether to only delete the share or delete the share and all snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that deletes the share.</returns>
        private RESTCommand<NullType> DeleteShareImpl(DeleteShareSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> deleteCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(deleteCmd);
            deleteCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.Delete(uri, serverTimeout, this.SnapshotTime, deleteSnapshotsOption, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            deleteCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex);

            return deleteCmd;
        }

        /// <summary>
        /// Implementation for the FetchAttributes method.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that fetches the attributes.</returns>
        private RESTCommand<NullType> FetchAttributesImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.GetProperties(uri, serverTimeout, this.SnapshotTime, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that checks existence.</returns>
        private RESTCommand<bool> ExistsImpl(FileRequestOptions options)
        {
            RESTCommand<bool> getCmd = new RESTCommand<bool>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.GetProperties(uri, serverTimeout, this.SnapshotTime, null, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
        /// Implementation for the GetPermissions method.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that gets the permissions.</returns>
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
            getCmd.PostProcessResponseAsync = async (cmd, resp, ctx, ct) =>
            {
                this.UpdateETagAndLastModified(resp);
                await ShareHttpResponseParsers.ReadSharedAccessIdentifiersAsync(cmd.ResponseStream, shareAcl, ct).ConfigureAwait(false);

                return shareAcl;
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the GetStats method.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns></returns>
        private RESTCommand<ShareStats> GetStatsImpl(FileRequestOptions options)
        {
            RESTCommand<ShareStats> retCmd = new RESTCommand<ShareStats>(this.ServiceClient.Credentials, this.StorageUri);
            options.ApplyToStorageCommand(retCmd);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.GetStats(uri, serverTimeout, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            retCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) => ShareHttpResponseParsers.ReadShareStatsAsync(cmd.ResponseStream, ct);
            return retCmd;
        }

        /// <summary>
        /// Implementation for the SetMetadata method.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that sets the metadata.</returns>
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
        /// Implementation for the SetPermissions method.
        /// </summary>
        /// <param name="acl">The permissions to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that sets the permissions.</returns>
        private RESTCommand<NullType> SetPermissionsImpl(FileSharePermissions acl, AccessCondition accessCondition, FileRequestOptions options)
        {
            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(this.ServiceClient.BufferManager, (int)(1 * Constants.KB));
            FileRequest.WriteSharedAccessIdentifiers(acl.SharedAccessPolicies, memoryStream);
            memoryStream.Seek(0, System.IO.SeekOrigin.Begin);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => ShareHttpRequestMessageFactory.SetAcl(uri, serverTimeout, FileSharePublicAccessType.Off, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, null /* md5 */, cmd, ctx);
            putCmd.StreamToDispose = memoryStream;
            putCmd.RecoveryAction = RecoveryActions.RewindStream;
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
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that sets the properties.
        /// </returns>
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
