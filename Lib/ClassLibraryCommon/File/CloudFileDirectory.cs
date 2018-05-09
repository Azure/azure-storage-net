//-----------------------------------------------------------------------
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
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a directory of files, designated by a delimiter character.
    /// </summary>
    /// <remarks>Shares, which are encapsulated as <see cref="CloudFileShare"/> objects, hold directories, and directories hold files. Directories can also contain sub-directories.</remarks>
    public partial class CloudFileDirectory
    {
#if SYNC
        /// <summary>
        /// Creates the directory.
        /// </summary>
        /// <param name="requestOptions">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation. This object
        /// is used to track requests to the storage service, and to provide additional runtime information about the operation. </param>
        [DoesServiceRequest]
        public virtual void Create(FileRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(requestOptions, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            Executor.ExecuteSync(
                this.CreateDirectoryImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to create a directory.
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
        /// Begins an asynchronous operation to create a directory.
        /// </summary>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreate(FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper(token => this.CreateAsync(options, operationContext), callback, state);
        }

        /// <summary> 
        /// Ends an asynchronous operation to create a directory.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndCreate(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a directory.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync()
        {
            return this.CreateAsync(options: null, operationContext: null, cancellationToken: CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a directory.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(CancellationToken cancellationToken)
        {
            return this.CreateAsync(options:null, operationContext:null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a directory.
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
        /// Returns a task that performs an asynchronous operation to create a directory.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.CreateDirectoryImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Creates the directory if it does not already exist.
        /// </summary>
        /// <param name="requestOptions">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the directory did not already exist and was created; otherwise <c>false</c>.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual bool CreateIfNotExists(FileRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            // Root directory always exists if the share exists.
            // We cannot call this.Create() if this is the root directory, because the service will always 
            // return a 405 error in that case, regardless of whether or not the share exists.
            if (String.IsNullOrEmpty(this.Name))
            {
                // If the share does not exist, this will throw a 404, which is what we want.
                // This.Create() will throw the same 404 if the share does not exist, and this is not the root directory.
                this.ServiceClient.GetShareReference(this.Share.Name, this.Share.SnapshotTime).FetchAttributes(null, requestOptions, operationContext);

                // If the above call did not throw an exception, then the share (and thus the root directory) already exists.
                return false;
            }

            try
            {
                this.Create(requestOptions, operationContext);
                return true;
            }
            catch (StorageException e)
            {
                if ((e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict) &&
                    ((e.RequestInformation.ExtendedErrorInformation == null) ||
                    (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ResourceAlreadyExists)))
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
        /// Begins an asynchronous request to create the directory if it does not already exist.
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
        /// Begins an asynchronous request to create the directory if it does not already exist.
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
            return new CancellableAsyncResultTaskWrapper<bool>(token => this.CreateIfNotExistsAsync(options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the result of an asynchronous request to create the directory if it does not already exist.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the directory did not already exist and was created; otherwise, <c>false</c>.</returns>
        public virtual bool EndCreateIfNotExists(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<bool>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous request to create the directory if it does not already exist.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync()
        {
            return this.CreateIfNotExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to create the directory if it does not already exist.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(CancellationToken cancellationToken)
        {
            return this.CreateIfNotExistsAsync(options: null, operationContext: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to create the directory if it does not already exist.
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
        /// Returns a task that performs an asynchronous request to create the directory if it does not already exist.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual async Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            // Root directory always exists if the share exists.
            // We cannot call this.Create() if this is the root directory, because the service will always 
            // return a 405 error in that case, regardless of whether or not the share exists.
            if (String.IsNullOrEmpty(this.Name))
            {
                // If the share does not exist, this will throw a 404, which is what we want.
                // This.Create() will throw the same 404 if the share does not exist, and this is not the root directory.
                await this.ServiceClient.GetShareReference(this.Share.Name, this.Share.SnapshotTime).FetchAttributesAsync(null, options, operationContext, cancellationToken).ConfigureAwait(false);

                // If the above call did not throw an exception, then the share (and thus the root directory) already exists.
                return false;
            }

            try
            {
                await this.CreateAsync(options, operationContext, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (StorageException e)
            {
                if ((e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict) &&
                    ((e.RequestInformation.ExtendedErrorInformation == null) ||
                    (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ResourceAlreadyExists)))
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
        /// Deletes the directory.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void Delete(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.DeleteDirectoryImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to delete a directory.
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
        /// Begins an asynchronous operation to delete a directory.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper(token => this.DeleteAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to delete a directory.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDelete(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete a directory.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
        {
            return this.DeleteAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete a directory.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(CancellationToken cancellationToken)
        {
            return this.DeleteAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete a directory.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete a directory.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.DeleteDirectoryImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Deletes the directory if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the directory did already exist and was deleted; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual bool DeleteIfExists(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            try
            {
                bool exists = this.Exists(options, operationContext);
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
                this.Delete(accessCondition, options, operationContext);
                return true;
            }
            catch (StorageException storageEx)
            {
                if (storageEx.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
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
        /// Begins an asynchronous request to delete the directory if it already exists.
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
        /// Begins an asynchronous request to delete the directory if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper<bool>(token => this.DeleteIfExistsAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the result of an asynchronous request to delete the directory if it already exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the directory did already exist and was deleted; otherwise, <c>false</c>.</returns>
        public virtual bool EndDeleteIfExists(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<bool>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the directory if it already exists.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
        {
            return this.DeleteIfExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the directory if it already exists.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(CancellationToken cancellationToken)
        {
            return this.DeleteIfExistsAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the directory if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the directory if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            try
            {
                bool exists = await this.ExistsAsync(options, operationContext, cancellationToken).ConfigureAwait(false);
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
                await this.DeleteAsync(accessCondition, options, operationContext, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (StorageException storageEx)
            {
                if (storageEx.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
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
        /// Checks whether the directory exists.
        /// </summary>
        /// <param name="requestOptions">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the directory exists;<c>false</c>, otherwise.</returns>
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
        /// Begins an asynchronous request to check whether the directory exists.
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
        /// Begins an asynchronous request to check whether the directory exists.
        /// </summary>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExists(FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper<bool>(token => this.ExistsAsync(options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the asynchronous result of the request to check whether the directory exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the directory exists; <c>false</c>, otherwise.</returns>
        public virtual bool EndExists(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<bool>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous request to check whether the directory exists.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
        {
            return this.ExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to check whether the directory exists.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            return this.ExistsAsync(default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to check whether the directory exists.
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
        /// Returns a task that performs an asynchronous request to check whether the directory exists.
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
        /// Populates a directory's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
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
        /// Begins an asynchronous operation to populate the directory's properties.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginFetchAttributes(AsyncCallback callback, object state)
        {
            return this.BeginFetchAttributes(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to populate the directory's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginFetchAttributes(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper(token => this.FetchAttributesAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to populate the directory's properties and metadata.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndFetchAttributes(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to populate the directory's properties and metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync()
        {
            return this.FetchAttributesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to populate the directory's properties and metadata.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(CancellationToken cancellationToken)
        {
            return this.FetchAttributesAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to populate the directory's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to populate the directory's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
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
        /// Returns an enumerable collection of the files in the share, which are retrieved lazily.
        /// </summary>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="IListFileItem"/> and are retrieved lazily.</returns>
        [DoesServiceRequest]
        public virtual IEnumerable<IListFileItem> ListFilesAndDirectories(FileRequestOptions options = null, OperationContext operationContext = null)
        {
            return ListFilesAndDirectories(null /* prefix */, options, operationContext);
        }

        /// <summary>
        /// Returns an enumerable collection of the files in the share, which are retrieved lazily.
        /// </summary>
        /// <param name="prefix">A string containing the file or directory name prefix.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="IListFileItem"/> and are retrieved lazily.</returns>
        [DoesServiceRequest]
        public IEnumerable<IListFileItem> ListFilesAndDirectories(string prefix, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return CommonUtility.LazyEnumerable(
                token => this.ListFilesAndDirectoriesSegmentedCore(prefix, null /* maxResults */, (FileContinuationToken)token, modifiedOptions, operationContext),
                long.MaxValue);
        }

        /// <summary>
        /// Returns a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <returns>A result segment containing objects that implement <see cref="IListFileItem"/>.</returns>
        [DoesServiceRequest]
        public virtual FileResultSegment ListFilesAndDirectoriesSegmented(FileContinuationToken currentToken)
        {
            return this.ListFilesAndDirectoriesSegmented(null /* prefix */, null /* maxResults */, currentToken, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A result segment containing objects that implement <see cref="IListFileItem"/>.</returns>
        [DoesServiceRequest]
        public virtual FileResultSegment ListFilesAndDirectoriesSegmented(int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ListFilesAndDirectoriesSegmented(null /* prefix */, maxResults, currentToken, options, operationContext);
        }

        /// <summary>
        /// Returns a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="prefix">A string containing the file or directory name prefix.</param> 
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A result segment containing objects that implement <see cref="IListFileItem"/>.</returns>
        [DoesServiceRequest]
        public FileResultSegment ListFilesAndDirectoriesSegmented(string prefix, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            ResultSegment<IListFileItem> resultSegment = this.ListFilesAndDirectoriesSegmentedCore(prefix, maxResults, currentToken, modifiedOptions, operationContext);
            return new FileResultSegment(resultSegment.Results, (FileContinuationToken)resultSegment.ContinuationToken);
        }

        /// <summary>
        /// Returns a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="prefix">A string containing the file or directory name prefix.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A result segment containing objects that implement <see cref="IListFileItem"/>.</returns>
        private ResultSegment<IListFileItem> ListFilesAndDirectoriesSegmentedCore(string prefix, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            return Executor.ExecuteSync(
                this.ListFilesAndDirectoriesImpl(maxResults, options, currentToken, prefix),
                options.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to return a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginListFilesAndDirectoriesSegmented(FileContinuationToken currentToken, AsyncCallback callback, object state)
        {
            return this.BeginListFilesAndDirectoriesSegmented(null /* prefix */, null /* maxResults */, currentToken, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to return a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginListFilesAndDirectoriesSegmented(int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginListFilesAndDirectoriesSegmented(null /* prefix */, maxResults, currentToken, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to return a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="prefix">A string containing the file or directory name prefix.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public ICancellableAsyncResult BeginListFilesAndDirectoriesSegmented(string prefix, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper<FileResultSegment>(token => this.ListFilesAndDirectoriesSegmentedAsync(prefix, maxResults, currentToken, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to return a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A result segment containing objects that implement <see cref="IListFileItem"/>.</returns>
        public virtual FileResultSegment EndListFilesAndDirectoriesSegmented(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<FileResultSegment>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to return a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(FileContinuationToken currentToken)
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(null /* prefix */, currentToken, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to return a result segment containing a collection of file items 
        /// in the share.
        /// </summary>    
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(FileContinuationToken currentToken, CancellationToken cancellationToken)
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(null /* prefix */, currentToken, cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to return a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="prefix">A string containing the file or directory name prefix.</param>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(string prefix, FileContinuationToken currentToken, CancellationToken cancellationToken)
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(prefix, null, currentToken, null, null, cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to return a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(null /* prefix */, maxResults, currentToken, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to return a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(null /* prefix */, maxResults, currentToken, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to return a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <param name="prefix">A string containing the file or directory name prefix.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(string prefix, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            ResultSegment<IListFileItem> resultSegment = await Executor.ExecuteAsync(
                this.ListFilesAndDirectoriesImpl(maxResults, modifiedOptions, currentToken, prefix),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken).ConfigureAwait(false);

            return new FileResultSegment(resultSegment.Results, (FileContinuationToken)resultSegment.ContinuationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Updates the directory's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
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
        /// Begins an asynchronous operation to update the directory's metadata.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetMetadata(AsyncCallback callback, object state)
        {
            return this.BeginSetMetadata(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to update the directory's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetMetadata(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return new CancellableAsyncResultTaskWrapper(token => this.SetMetadataAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to update the directory's metadata.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetMetadata(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the directory's metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync()
        {
            return this.SetMetadataAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the directory's metadata.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(CancellationToken cancellationToken)
        {
            return this.SetMetadataAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the directory's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the directory's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.SetMetadataImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

        /// <summary>
        /// Implementation for the Create method.
        /// </summary>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that creates the directory.</returns>
        private RESTCommand<NullType> CreateDirectoryImpl(FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = DirectoryHttpRequestMessageFactory.Create(uri, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                DirectoryHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                this.UpdateETagAndLastModified(resp);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Delete method.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that deletes the directory.</returns>
        private RESTCommand<NullType> DeleteDirectoryImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => DirectoryHttpRequestMessageFactory.Delete(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex);

            return putCmd;
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
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => DirectoryHttpRequestMessageFactory.GetProperties(uri, serverTimeout, this.Share.SnapshotTime, null, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, true, cmd, ex);
                this.Properties = DirectoryHttpResponseParsers.GetProperties(resp);
                this.Metadata = DirectoryHttpResponseParsers.GetMetadata(resp);

                return true;
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the FetchAttributes method. The attributes are updated immediately.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that fetches the attributes.</returns>
        private RESTCommand<NullType> FetchAttributesImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => DirectoryHttpRequestMessageFactory.GetProperties(uri, serverTimeout, this.Share.SnapshotTime, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.Properties = DirectoryHttpResponseParsers.GetProperties(resp);
                this.Metadata = DirectoryHttpResponseParsers.GetMetadata(resp);
                return NullType.Value;
            };

            return getCmd;
        }

        /// <summary>
        /// Core implementation of the ListFilesAndDirectories method.
        /// </summary>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is <c>null</c>, the maximum possible number of results will be returned, up to 5000.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="currentToken">The continuation token.</param>
        /// <param name="prefix">A string containing the file or directory name prefix.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that lists the files.</returns>
        private RESTCommand<ResultSegment<IListFileItem>> ListFilesAndDirectoriesImpl(int? maxResults, FileRequestOptions options, FileContinuationToken currentToken, string prefix)
        {
            FileListingContext listingContext = new FileListingContext(maxResults)
            {
                Marker = currentToken != null ? currentToken.NextMarker : null,
                Prefix = string.IsNullOrEmpty(prefix) ? null : prefix
            };

            RESTCommand<ResultSegment<IListFileItem>> getCmd = new RESTCommand<ResultSegment<IListFileItem>>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommonUtility.GetListingLocationMode(currentToken);
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => DirectoryHttpRequestMessageFactory.List(uri, serverTimeout, this.Share.SnapshotTime, listingContext, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = async (cmd, resp, ctx, ct) =>
            {
                ListFilesAndDirectoriesResponse listFilesResponse = await ListFilesAndDirectoriesResponse.ParseAsync(cmd.ResponseStream, ct).ConfigureAwait(false);
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
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the SetMetadata method.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the directory. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetMetadataImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = DirectoryHttpRequestMessageFactory.SetMetadata(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                DirectoryHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.UpdateETagAndLastModified(resp);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
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
            FileDirectoryProperties parsedProperties = DirectoryHttpResponseParsers.GetProperties(response);
            this.Properties.ETag = parsedProperties.ETag;
            this.Properties.LastModified = parsedProperties.LastModified;
        }
    }
}
