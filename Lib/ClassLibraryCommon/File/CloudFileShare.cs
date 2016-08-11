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

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.CreateShareImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to create a share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndCreate(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginCreate, this.EndCreate, cancellationToken);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginCreate, this.EndCreate, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Creates the share if it does not already exist.
        /// </summary>
        /// <param name="requestOptions">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise <c>false</c>.</returns>
        /// <remarks>This API performs an existence check and therefore requires read permissions.</remarks>
        [DoesServiceRequest]
        public virtual bool CreateIfNotExists(FileRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            bool exists = this.Exists(modifiedOptions, operationContext);
            if (exists)
            {
                return false;
            }

            try
            {
                this.Create(modifiedOptions, operationContext);
                return true;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
                {
                    if ((e.RequestInformation.ExtendedErrorInformation == null) ||
                        (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ShareAlreadyExists))
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
        /// Begins an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>This API performs an existence check and therefore requires read permissions.</remarks>
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
        /// <remarks>This API performs an existence check and therefore requires read permissions.</remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreateIfNotExists(FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            StorageAsyncResult<bool> storageAsyncResult = new StorageAsyncResult<bool>(callback, state)
            {
                RequestOptions = modifiedOptions,
                OperationContext = operationContext,
            };

            this.CreateIfNotExistsHandler(modifiedOptions, operationContext, storageAsyncResult);
            return storageAsyncResult;
        }

        private void CreateIfNotExistsHandler(FileRequestOptions options, OperationContext operationContext, StorageAsyncResult<bool> storageAsyncResult)
        {
            ICancellableAsyncResult savedExistsResult = this.BeginExists(
                options,
                operationContext,
                existsResult =>
                {
                    storageAsyncResult.UpdateCompletedSynchronously(existsResult.CompletedSynchronously);
                    lock (storageAsyncResult.CancellationLockerObject)
                    {
                        storageAsyncResult.CancelDelegate = null;
                        try
                        {
                            bool exists = this.EndExists(existsResult);
                            if (exists)
                            {
                                storageAsyncResult.Result = false;
                                storageAsyncResult.OnComplete();
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            storageAsyncResult.OnComplete(e);
                            return;
                        }

                        ICancellableAsyncResult savedCreateResult = this.BeginCreate(
                            options,
                            operationContext,
                            createResult =>
                            {
                                storageAsyncResult.UpdateCompletedSynchronously(createResult.CompletedSynchronously);
                                storageAsyncResult.CancelDelegate = null;
                                try
                                {
                                    this.EndCreate(createResult);
                                    storageAsyncResult.Result = true;
                                    storageAsyncResult.OnComplete();
                                }
                                catch (StorageException e)
                                {
                                    if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
                                    {
                                        if ((e.RequestInformation.ExtendedErrorInformation == null) ||
                                            (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ShareAlreadyExists))
                                        {
                                            storageAsyncResult.Result = false;
                                            storageAsyncResult.OnComplete();
                                        }
                                        else
                                        {
                                            storageAsyncResult.OnComplete(e);
                                        }
                                    }
                                    else
                                    {
                                        storageAsyncResult.OnComplete(e);
                                    }
                                }
                                catch (Exception e)
                                {
                                    storageAsyncResult.OnComplete(e);
                                }
                            },
                            null);

                        storageAsyncResult.CancelDelegate = savedCreateResult.Cancel;
                        if (storageAsyncResult.CancelRequested)
                        {
                            storageAsyncResult.Cancel();
                        }
                    }
                },
                null);

            // We do not need to do this inside a lock, as storageAsyncResult is
            // not returned to the user yet.
            storageAsyncResult.CancelDelegate = savedExistsResult.Cancel;
        }

        /// <summary>
        /// Returns the result of an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise, <c>false</c>.</returns>
        public virtual bool EndCreateIfNotExists(IAsyncResult asyncResult)
        {
            StorageAsyncResult<bool> res = asyncResult as StorageAsyncResult<bool>;
            CommonUtility.AssertNotNull("AsyncResult", res);
            res.End();
            return res.Result;
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        /// <remarks>This API performs an existence check and therefore requires read permissions.</remarks>
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
        /// <remarks>This API performs an existence check and therefore requires read permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginCreateIfNotExists, this.EndCreateIfNotExists, cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to create the share if it does not already exist.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        /// <remarks>This API performs an existence check and therefore requires read permissions.</remarks>
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
        /// <remarks>This API performs an existence check and therefore requires read permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginCreateIfNotExists, this.EndCreateIfNotExists, options, operationContext, cancellationToken);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.DeleteShareImpl(accessCondition, modifiedOptions),
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
            return this.BeginDelete(null /* accessCondition */, null /* options */, null /*operationContext */, callback, state);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.DeleteShareImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to delete a share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDelete(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginDelete, this.EndDelete, cancellationToken);
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
            return this.DeleteAsync(accessCondition, options, operationContext, CancellationToken.None);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginDelete, this.EndDelete, accessCondition, options, operationContext, cancellationToken);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            bool exists = this.Exists(modifiedOptions, operationContext);
            if (!exists)
            {
                return false;
            }

            try
            {
                this.Delete(accessCondition, modifiedOptions, operationContext);
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
            return this.BeginDeleteIfExists(null, null, null, callback, state);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            StorageAsyncResult<bool> storageAsyncResult = new StorageAsyncResult<bool>(callback, state)
            {
                RequestOptions = modifiedOptions,
                OperationContext = operationContext,
            };

            this.DeleteIfExistsHandler(accessCondition, modifiedOptions, operationContext, storageAsyncResult);
            return storageAsyncResult;
        }

        private void DeleteIfExistsHandler(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, StorageAsyncResult<bool> storageAsyncResult)
        {
            ICancellableAsyncResult savedExistsResult = this.BeginExists(
                options,
                operationContext,
                existsResult =>
                {
                    storageAsyncResult.UpdateCompletedSynchronously(existsResult.CompletedSynchronously);
                    lock (storageAsyncResult.CancellationLockerObject)
                    {
                        storageAsyncResult.CancelDelegate = null;
                        try
                        {
                            bool exists = this.EndExists(existsResult);
                            if (!exists)
                            {
                                storageAsyncResult.Result = false;
                                storageAsyncResult.OnComplete();
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            storageAsyncResult.OnComplete(e);
                            return;
                        }

                        ICancellableAsyncResult savedDeleteResult = this.BeginDelete(
                            accessCondition,
                            options,
                            operationContext,
                            deleteResult =>
                            {
                                storageAsyncResult.UpdateCompletedSynchronously(deleteResult.CompletedSynchronously);
                                storageAsyncResult.CancelDelegate = null;
                                try
                                {
                                    this.EndDelete(deleteResult);
                                    storageAsyncResult.Result = true;
                                    storageAsyncResult.OnComplete();
                                }
                                catch (StorageException e)
                                {
                                    if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                                    {
                                        if ((e.RequestInformation.ExtendedErrorInformation == null) ||
                                            (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ShareNotFound))
                                        {
                                            storageAsyncResult.Result = false;
                                            storageAsyncResult.OnComplete();
                                        }
                                        else
                                        {
                                            storageAsyncResult.OnComplete(e);
                                        }
                                    }
                                    else
                                    {
                                        storageAsyncResult.OnComplete(e);
                                    }
                                }
                                catch (Exception e)
                                {
                                    storageAsyncResult.OnComplete(e);
                                }
                            },
                            null);

                        storageAsyncResult.CancelDelegate = savedDeleteResult.Cancel;
                        if (storageAsyncResult.CancelRequested)
                        {
                            storageAsyncResult.Cancel();
                        }
                    }
                },
                null);

            // We do not need to do this inside a lock, as storageAsyncResult is
            // not returned to the user yet.
            storageAsyncResult.CancelDelegate = savedExistsResult.Cancel;
        }

        /// <summary>
        /// Returns the result of an asynchronous request to delete the share if it already exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the share did not already exist and was created; otherwise, <c>false</c>.</returns>
        public virtual bool EndDeleteIfExists(IAsyncResult asyncResult)
        {
            StorageAsyncResult<bool> res = asyncResult as StorageAsyncResult<bool>;
            CommonUtility.AssertNotNull("AsyncResult", res);
            res.End();
            return res.Result;
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
            return AsyncExtensions.TaskFromApm(this.BeginDeleteIfExists, this.EndDeleteIfExists, cancellationToken);
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
            return this.DeleteIfExistsAsync(accessCondition, options, operationContext, CancellationToken.None);
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
            return AsyncExtensions.TaskFromApm(this.BeginDeleteIfExists, this.EndDeleteIfExists, accessCondition, options, operationContext, cancellationToken);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.ExistsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Returns the asynchronous result of the request to check whether the share exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the share exists.</returns>
        public virtual bool EndExists(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<bool>(asyncResult);
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
            return AsyncExtensions.TaskFromApm(this.BeginExists, this.EndExists, cancellationToken);
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
            return AsyncExtensions.TaskFromApm(this.BeginExists, this.EndExists, options, operationContext, cancellationToken);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.FetchAttributesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to retrieve the share's attributes.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndFetchAttributes(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginFetchAttributes, this.EndFetchAttributes, cancellationToken);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginFetchAttributes, this.EndFetchAttributes, accessCondition, options, operationContext, cancellationToken);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.GetPermissionsImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Returns the asynchronous result of the request to get the permissions settings for the share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="FileSharePermissions"/> object.</returns>
        public virtual FileSharePermissions EndGetPermissions(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<FileSharePermissions>(asyncResult);
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
            return AsyncExtensions.TaskFromApm(this.BeginGetPermissions, this.EndGetPermissions, cancellationToken);
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
            return AsyncExtensions.TaskFromApm(this.BeginGetPermissions, this.EndGetPermissions, accessCondition, options, operationContext, cancellationToken);
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
            options = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            return Executor.BeginExecuteAsync(
                this.GetStatsImpl(options),
                options.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to get stats for the share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="ShareStats"/> object.</returns>
        public virtual ShareStats EndGetStats(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<ShareStats>(asyncResult);
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
            return AsyncExtensions.TaskFromApm(this.BeginGetStats, this.EndGetStats, cancellationToken);
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
            return AsyncExtensions.TaskFromApm(this.BeginGetStats, this.EndGetStats, options, operationContext, cancellationToken);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.SetMetadataImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous request operation to set user-defined metadata on the share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetMetadata(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetMetadata, this.EndSetMetadata, cancellationToken);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetMetadata, this.EndSetMetadata, accessCondition, options, operationContext, cancellationToken);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.SetPermissionsImpl(permissions, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Returns the result of an asynchronous request to set permissions for the share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetPermissions(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetPermissions, this.EndSetPermissions, permissions, cancellationToken);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetPermissions, this.EndSetPermissions, permissions, accessCondition, options, operationContext, cancellationToken);
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
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.SetPropertiesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to update the share's properties.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetProperties(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetProperties, this.EndSetProperties, cancellationToken);
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
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetProperties, this.EndSetProperties, accessCondition, options, operationContext, cancellationToken);
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
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => ShareHttpWebRequestFactory.Create(uri, this.Properties, serverTimeout, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) => ShareHttpWebRequestFactory.AddMetadata(r, this.Metadata);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
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
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the share. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that deletes the share.</returns>
        private RESTCommand<NullType> DeleteShareImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> deleteCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(deleteCmd);
            deleteCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => ShareHttpWebRequestFactory.Delete(uri, serverTimeout, accessCondition, useVersionHeader, ctx);
            deleteCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
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
            getCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => ShareHttpWebRequestFactory.GetProperties(uri, serverTimeout, accessCondition, useVersionHeader, ctx);
            getCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
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
            getCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => ShareHttpWebRequestFactory.GetProperties(uri, serverTimeout, null, useVersionHeader, ctx);
            getCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
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
            getCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => ShareHttpWebRequestFactory.GetAcl(uri, serverTimeout, accessCondition, useVersionHeader, ctx);
            getCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
                shareAcl = new FileSharePermissions();
                return shareAcl;
            };
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                ShareHttpResponseParsers.ReadSharedAccessIdentifiers(cmd.ResponseStream, shareAcl);
                this.UpdateETagAndLastModified(resp);
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
            retCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => ShareHttpWebRequestFactory.GetStats(uri, serverTimeout, useVersionHeader, ctx);
            retCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            retCmd.PostProcessResponse = (cmd, resp, ctx) => ShareHttpResponseParsers.ReadShareStats(cmd.ResponseStream);
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
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => ShareHttpWebRequestFactory.SetMetadata(uri, serverTimeout, accessCondition, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) => ShareHttpWebRequestFactory.AddMetadata(r, this.Metadata);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
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
            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            FileRequest.WriteSharedAccessIdentifiers(acl.SharedAccessPolicies, memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => ShareHttpWebRequestFactory.SetAcl(uri, serverTimeout, FileSharePublicAccessType.Off, accessCondition, useVersionHeader, ctx);
            putCmd.SendStream = memoryStream;
            putCmd.StreamToDispose = memoryStream;
            putCmd.RecoveryAction = RecoveryActions.RewindStream;
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
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
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => ShareHttpWebRequestFactory.SetProperties(uri, serverTimeout, this.Properties, accessCondition, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) => ShareHttpWebRequestFactory.AddMetadata(r, this.Metadata);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
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
        private void UpdateETagAndLastModified(HttpWebResponse response)
        {
            FileShareProperties parsedProperties = ShareHttpResponseParsers.GetProperties(response);
            this.Properties.ETag = parsedProperties.ETag;
            this.Properties.LastModified = parsedProperties.LastModified;
        }
    }
}
