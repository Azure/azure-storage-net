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

namespace Microsoft.Azure.Storage.File
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.File.Protocol;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
#if NETCORE
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
#endif

    public partial class CloudFileDirectory
    {
        /// <summary>
        /// Creates the directory.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task CreateAsync()
        {
            return this.CreateAsync(null, null);
        }

        /// <summary>
        /// Creates the directory.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task CreateAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.CreateAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates the directory.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task CreateAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsyncNullReturn(
                this.CreateDirectoryImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Creates the directory if it does not already exist.
        /// </summary>
        /// <returns><c>true</c> if the directory did not already exist and was created; otherwise, <c>false</c>.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync()
        {
            return this.CreateIfNotExistsAsync(null, null);
        }

        /// <summary>
        /// Creates the directory if it does not already exist.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the directory did not already exist and was created; otherwise <c>false</c>.</returns>
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext)
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
        /// <remarks>This API requires Create or Write permissions.</remarks>
        [DoesServiceRequest]
        public virtual async Task<bool> CreateIfNotExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            try
            {
                // Root directory always exists if the share exists.
                // We cannot call this.CreateDirectoryImpl if this is the root directory, because the service will always 
                // return a 405 error in that case, regardless of whether or not the share exists.
                if (string.IsNullOrEmpty(this.Name))
                {
                    // If the share does not exist, this fetch call will throw a 404, which is what we want.
                    await Executor.ExecuteAsync(
                        this.FetchAttributesImpl(null, modifiedOptions),
                        modifiedOptions.RetryPolicy,
                        operationContext,
                        cancellationToken).ConfigureAwait(false);
                    return false;
                }

                await Executor.ExecuteAsync(
                    this.CreateDirectoryImpl(modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch (StorageException e)
            {
#pragma warning disable 618
                if ((e.RequestInformation.ExtendedErrorInformation != null) &&
                    (e.RequestInformation.ExtendedErrorInformation.ErrorCode == FileErrorCodeStrings.ResourceAlreadyExists))
#pragma warning restore 618
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Deletes the directory.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
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
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the directory.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the directory. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsyncNullReturn(
                this.DeleteDirectoryImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Deletes the directory if it already exists.
        /// </summary>
        /// <returns><c>true</c> if the directory already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
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
        public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
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
        public virtual async Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            try
            {
                bool exists = await Executor.ExecuteAsync(
                    this.ExistsImpl(modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken).ConfigureAwait(false);

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
                await Executor.ExecuteAsync(
                    this.DeleteDirectoryImpl(accessCondition, modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken).ConfigureAwait(false);

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
        }

        /// <summary>
        /// Checks existence of the directory.
        /// </summary>
        /// <returns><c>true</c> if the directory exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
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
        public virtual Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Checks existence of the directory.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the directory exists.</returns>
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

        /// <summary>
        /// Populates a directory's properties and metadata.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync()
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
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Populates a directory's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsyncNullReturn(
                this.FetchAttributesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Returns a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param> 
        /// <returns>A result segment containing objects that implement <see cref="IListFileItem"/>.</returns>
        [DoesServiceRequest]
        public virtual Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(FileContinuationToken currentToken)
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(null /* prefix */, null /* maxResults */, currentToken, null /* options */, null /* operationContext */);
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
        public virtual Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(null /* prefix */, maxResults, currentToken, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="prefix">A string containing the file or directory name prefix.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is zero, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <returns>A file result segment.</returns>
        [DoesServiceRequest]
        public virtual Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(string prefix, int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(prefix, maxResults, currentToken, options, operationContext, CancellationToken.None);
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
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A file result segment.</returns>
        [DoesServiceRequest]
        public virtual Task<FileResultSegment> ListFilesAndDirectoriesSegmentedAsync(int? maxResults, FileContinuationToken currentToken, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.ListFilesAndDirectoriesSegmentedAsync(null /* prefix */, maxResults, currentToken, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a result segment containing a collection of file items 
        /// in the share.
        /// </summary>
        /// <param name="prefix">A string containing the file or directory name prefix.</param>
        /// <param name="maxResults">A non-negative integer value that indicates the maximum number of results to be returned at a time, up to the 
        /// per-operation limit of 5000. If this value is zero, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="currentToken">A continuation token returned by a previous listing operation.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A file result segment.</returns>
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

        /// <summary> 
        /// Returns a task that performs an asynchronous operation to get the SMB handles open on this directory. 
        /// </summary> 
        /// <param name="token">Continuation token for paginated results.</param> 
        /// <param name="maxResults">The maximum number of results to be returned by the server.</param> 
        /// <param name="recursive">Whether to recurse through this directory's files and subfolders. A lack of value is interpreted as false.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param> 
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns> 
        [DoesServiceRequest]
        public virtual Task<FileHandleResultSegment> ListHandlesSegmentedAsync(FileContinuationToken token = null, int? maxResults = null, bool? recursive = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null, CancellationToken? cancellationToken = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.ListHandlesImpl(token, maxResults, recursive, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken ?? CancellationToken.None);
        }

        /// <summary> 
        /// Returns a task that performs an asynchronous operation to close all SMB handles on this directory. 
        /// </summary> 
        /// <param name="token">Continuation token for closing the handles.</param> 
        /// <param name="recursive">Whether to recurse through this directory's sub files and folders. A lack of value is interpreted as false.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param> 
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns> 
        [DoesServiceRequest]
        public virtual Task<CloseFileHandleResultSegment> CloseAllHandlesSegmentedAsync(FileContinuationToken token = null, bool? recursive = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null, CancellationToken? cancellationToken = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.CloseHandleImpl(token, Constants.HeaderConstants.AllFileHandles, recursive, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken ?? CancellationToken.None);
        }

        /// <summary> 
        /// Returns a task that performs an asynchronous operation to close the specified SMB handle on this directory. 
        /// </summary> 
        /// <param name="handleId">Id of the handle, "*" if all handles on the file.</param> 
        /// <param name="token">Continuation token for when closing the handle takes exceedingly long.</param> 
        /// <param name="recursive">Whether to recurse through this directory's sub files and folders. A lack of value is interpreted as false.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param> 
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns> 
        [DoesServiceRequest]
        public virtual Task<CloseFileHandleResultSegment> CloseHandleSegmentedAsync(string handleId, FileContinuationToken token = null, bool? recursive = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null, CancellationToken? cancellationToken = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.CloseHandleImpl(token, handleId, recursive, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken ?? CancellationToken.None);
        }

        /// <summary> 
        /// Returns a task that performs an asynchronous operation to close the specified SMB handle on this directory. 
        /// </summary> 
        /// <param name="handleId">Id of the handle</param> 
        /// <param name="token">Continuation token for when closing the handle takes exceedingly long.</param> 
        /// <param name="recursive">Whether to recurse through this directory's sub files and folders. A lack of value is interpreted as false.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param> 
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns> 
        [DoesServiceRequest]
        public virtual Task<CloseFileHandleResultSegment> CloseHandleSegmentedAsync(ulong handleId, FileContinuationToken token = null, bool? recursive = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null, CancellationToken? cancellationToken = null)
        {
            return this.CloseHandleSegmentedAsync(handleId.ToString(), token, recursive, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Updates the directory's metadata.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task SetMetdataAsync()
        {
            return this.SetMetadataAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Updates the directory's metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the directory. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Updates the directory's metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the directory. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsyncNullReturn(
                this.SetMetadataImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Implementation for the Create method.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the directory.</returns>
        private RESTCommand<NullType> CreateDirectoryImpl(FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

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
        /// <param name="accessCondition">An object that represents the access conditions for the directory. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that deletes the directory.</returns>
        private RESTCommand<NullType> DeleteDirectoryImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => DirectoryHttpRequestMessageFactory.Delete(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
            RESTCommand<bool> getCmd = new RESTCommand<bool>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(getCmd);
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
        /// Gets the list handles implementation. 
        /// </summary> 
        /// <param name="token">Continuation token for paged responses.</param> 
        /// <param name="maxResults">The maximum number of results to be returned by the server.</param> 
        /// <param name="recursive">Whether to recurse through this directory's files and subfolders.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <returns>A <see cref="RESTCommand{T}"/> for getting the handles.</returns> 
        private RESTCommand<FileHandleResultSegment> ListHandlesImpl(FileContinuationToken token, int? maxResults, bool? recursive, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<FileHandleResultSegment> getCmd = new RESTCommand<FileHandleResultSegment>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);
            options.ApplyToStorageCommand(getCmd);

            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.ListHandles(uri, serverTimeout, this.Share.SnapshotTime, maxResults, recursive, token, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) =>
            {
                ListHandlesResponse listHandlesResponse = new ListHandlesResponse(cmd.ResponseStream);

                return Task.FromResult(new FileHandleResultSegment()
                {
                    Results = listHandlesResponse.Handles,
                    ContinuationToken = new FileContinuationToken()
                    {
                        NextMarker = listHandlesResponse.NextMarker
                    }
                });
            };

            return getCmd;
        }

        /// <summary> 
        /// Gets the close handles implementation. 
        /// </summary> 
        /// <param name="token">Continuation token for closing many files.</param> 
        /// <param name="handleId">Id of the handle, "*" if all handles on the file.</param> 
        /// <param name="recursive">Whether to recurse through this directory's files and subfolders.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <returns>A <see cref="RESTCommand{T}"/> for closing the handles.</returns> 
        private RESTCommand<CloseFileHandleResultSegment> CloseHandleImpl(FileContinuationToken token, string handleId, bool? recursive, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<CloseFileHandleResultSegment> putCmd = new RESTCommand<CloseFileHandleResultSegment>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);
            options.ApplyToStorageCommand(putCmd);

            putCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            putCmd.RetrieveResponseStream = true;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.CloseHandle(uri, serverTimeout, this.Share.SnapshotTime, handleId, recursive, token, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                CloseFileHandleResultSegment res = HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
                int handlesClosed;

                if (!int.TryParse(resp.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.NumHandlesClosed), out handlesClosed))
                {
                    handlesClosed = -1;
                }

                FileContinuationToken continuation = null;
                string marker;

                if ((marker = resp.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.Marker)) != "")
                {
                    continuation = new FileContinuationToken()
                    {
                        NextMarker = marker
                    };
                }

                return new CloseFileHandleResultSegment()
                {
                    NumHandlesClosed = handlesClosed,
                    ContinuationToken = continuation
                };
            };

            return putCmd;
        }

        /// <summary>
        /// Implements the FetchAttributes method. The attributes are updated immediately.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that fetches the attributes.</returns>
        private RESTCommand<NullType> FetchAttributesImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

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
        /// per-operation limit of 5000. If this value is zero, the maximum possible number of results will be returned, up to 5000.</param>         
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="currentToken">The continuation token.</param>
        /// <param name="prefix">A string containing the file or directory name prefix.</param>
        /// <returns>A <see cref="RESTCommand"/> that lists the files.</returns>
        private RESTCommand<ResultSegment<IListFileItem>> ListFilesAndDirectoriesImpl(int? maxResults, FileRequestOptions options, FileContinuationToken currentToken, string prefix)
        {
            FileListingContext listingContext = new FileListingContext(maxResults)
            {
                Marker = currentToken != null ? currentToken.NextMarker : null,
                Prefix = string.IsNullOrEmpty(prefix) ? null : prefix
            };

            RESTCommand<ResultSegment<IListFileItem>> getCmd = new RESTCommand<ResultSegment<IListFileItem>>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

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
        /// <param name="accessCondition">An object that represents the access conditions for the directory. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetMetadataImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

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
