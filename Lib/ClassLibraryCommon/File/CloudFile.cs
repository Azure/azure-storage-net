//-----------------------------------------------------------------------
// <copyright file="CloudFile.cs" company="Microsoft">
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
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a file in the Microsoft Azure File service.
    /// </summary>
    public partial class CloudFile : IListFileItem
    {
#if SYNC
        /// <summary>
        /// Opens a stream for reading from the file.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A stream to be used for reading from the file.</returns>
        /// <remarks>On the <see cref="System.IO.Stream"/> object returned by this method, the <see cref="System.IO.Stream.EndRead(IAsyncResult)"/> method must be called exactly once for every <see cref="System.IO.Stream.BeginRead(byte[], int, int, AsyncCallback, Object)"/> call. Failing to end a read process before beginning another read can cause unknown behavior.</remarks>
        [DoesServiceRequest]
        public virtual Stream OpenRead(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            operationContext = operationContext ?? new OperationContext();
            this.FetchAttributes(accessCondition, options, operationContext);
            AccessCondition streamAccessCondition = AccessCondition.CloneConditionWithETag(accessCondition, this.Properties.ETag);
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient, false);
            return new FileReadStream(this, streamAccessCondition, modifiedOptions, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to open a stream for reading from the file.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginOpenRead(AsyncCallback callback, object state)
        {
            return this.BeginOpenRead(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to open a stream for reading from the file.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginOpenRead(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.OpenReadAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to open a stream for reading from the file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A stream to be used for reading from the file.</returns>
        public virtual Stream EndOpenRead(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<Stream>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to open a stream for reading from the file.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync()
        {
            return this.OpenReadAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to open a stream for reading from the file.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync(CancellationToken cancellationToken)
        {
            return this.OpenReadAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to open a stream for reading from the file.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.OpenReadAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to open a stream for reading from the file.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<Stream> OpenReadAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            operationContext = operationContext ?? new OperationContext();
            await this.FetchAttributesAsync(accessCondition, options, operationContext).ConfigureAwait(false);
            AccessCondition streamAccessCondition = AccessCondition.CloneConditionWithETag(accessCondition, this.Properties.ETag);
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient, false);
            return new FileReadStream(this, streamAccessCondition, modifiedOptions, operationContext);
        }
#endif

#if SYNC
        /// <summary>
        /// Opens a stream for writing to the file. If the file already exists, then existing data in the file may be overwritten.
        /// </summary>
        /// <param name="size">The size of the file to create, in bytes, or null.  If null, the file must already exist.  If not null, a new file of the given size will be created.  If size is not null but the file already exists on the service, the already-existing file will be deleted.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="CloudFileStream"/> object to be used for writing to the file.</returns>
        [DoesServiceRequest]
        public virtual CloudFileStream OpenWrite(long? size, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient, false);
            operationContext = operationContext ?? new OperationContext();
            bool createNew = size.HasValue;

            if (createNew)
            {
                this.Create(size.Value, accessCondition, options, operationContext);
            }
            else
            {
                if (modifiedOptions.StoreFileContentMD5.Value)
                {
                    throw new ArgumentException(SR.MD5NotPossible);
                }

                this.FetchAttributes(accessCondition, options, operationContext);
                size = this.Properties.Length;
            }

            if (accessCondition != null)
            {
                accessCondition = AccessCondition.GenerateLeaseCondition(accessCondition.LeaseId);
            }

            return new FileWriteStream(this, size.Value, createNew, accessCondition, modifiedOptions, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to open a stream for writing to the file. If the file already exists, then existing data in the file may be overwritten.
        /// </summary>
        /// <param name="size">The size of the file to create, in bytes, or null.  If null, the file must already exist.  If not null, a new file of the given size will be created.  If size is not null but the file already exists on the service, the already-existing file will be deleted.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginOpenWrite(long? size, AsyncCallback callback, object state)
        {
            return this.BeginOpenWrite(size, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to open a stream for writing to the file. If the file already exists, then existing data in the file may be overwritten.
        /// </summary>
        /// <param name="size">The size of the file to create, in bytes, or null.  If null, the file must already exist.  If not null, a new file of the given size will be created.  If size is not null but the file already exists on the service, the already-existing file will be deleted.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginOpenWrite(long? size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.OpenWriteAsync(size, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to open a stream for writing to the file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="CloudFileStream"/> object to be used for writing to the file.</returns>
        public virtual CloudFileStream EndOpenWrite(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<CloudFileStream>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to open a stream for writing to the file. If the file already exists, then existing data in the file may be overwritten.
        /// </summary>
        /// <param name="size">The size of the file to create, in bytes, or null.  If null, the file must already exist.  If not null, a new file of the given size will be created.  If size is not null but the file already exists on the service, the already-existing file will be deleted.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudFileStream> OpenWriteAsync(long? size)
        {
            return this.OpenWriteAsync(size, default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to open a stream for writing to the file. If the file already exists, then existing data in the file may be overwritten.
        /// </summary>
        /// <param name="size">The size of the file to create, in bytes, or null.  If null, the file must already exist.  If not null, a new file of the given size will be created.  If size is not null but the file already exists on the service, the already-existing file will be deleted.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudFileStream> OpenWriteAsync(long? size, CancellationToken cancellationToken)
        {
            return this.OpenWriteAsync(size, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to open a stream for writing to the file. If the file already exists, then existing data in the file may be overwritten.
        /// </summary>
        /// <param name="size">The size of the file to create, in bytes, or null.  If null, the file must already exist.  If not null, a new file of the given size will be created.  If size is not null but the file already exists on the service, the already-existing file will be deleted.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudFileStream> OpenWriteAsync(long? size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.OpenWriteAsync(size, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to open a stream for writing to the file. If the file already exists, then existing data in the file may be overwritten.
        /// </summary>
        /// <param name="size">The size of the file to create, in bytes, or null.  If null, the file must already exist.  If not null, a new file of the given size will be created.  If size is not null but the file already exists on the service, the already-existing file will be deleted.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<CloudFileStream> OpenWriteAsync(long? size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient, false);
            operationContext = operationContext ?? new OperationContext();
            bool createNew = size.HasValue;

            if (!createNew && modifiedOptions.StoreFileContentMD5.Value)
            {
                throw new ArgumentException(SR.MD5NotPossible);
            }
            if (createNew)
            {
                await this.CreateAsync(size.Value, accessCondition, options, operationContext, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await this.FetchAttributesAsync(accessCondition, options, operationContext, cancellationToken).ConfigureAwait(false);
                size = this.Properties.Length;
            }

            if (accessCondition != null)
            {
                accessCondition = AccessCondition.GenerateLeaseCondition(accessCondition.LeaseId);
            }

            return new FileWriteStream(this, size.Value, createNew, accessCondition, modifiedOptions, operationContext);
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void DownloadToStream(Stream target, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.DownloadRangeToStream(target, null /* offset */, null /* length */, accessCondition, options, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToStream(Stream target, AsyncCallback callback, object state)
        {
            return this.BeginDownloadToStream(target, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToStream(Stream target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginDownloadRangeToStream(target, null /* offset */, null /* length */, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressIncrementer"> An <see cref="IProgress{T}<"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadToStream(Stream target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadToStreamAsync(target, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDownloadToStream(IAsyncResult asyncResult)
        {
            this.EndDownloadRangeToStream(asyncResult);
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target)
        {
            return this.DownloadToStreamAsync(target, default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, CancellationToken cancellationToken)
        {
            return this.DownloadToStreamAsync(target, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToStreamAsync(target, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToStreamAsync(target, null /* offset */, null /* length */, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToStreamAsync(target, null /* offset */, null /* length */, accessCondition, options, operationContext, progressHandler, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads the contents of a file in the File service to a local file.
        /// </summary>
        /// <param name="path">The path to the target file in the local file system.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void DownloadToFile(string path, FileMode mode, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("path", path);
            FileStream fileStream = new FileStream(path, mode, FileAccess.Write, FileShare.None);

            try
            {
                using (fileStream)
                {
                    this.DownloadToStream(fileStream, accessCondition, options, operationContext);
                }
            }
            catch (Exception)
            {
                if (mode == FileMode.Create || mode == FileMode.CreateNew)
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception)
                    {
                        // Best effort to clean up in the event that download was unsuccessful.
                        // Do not throw as we want to throw original exception.
                    }
                }

                throw;
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file in the File service to a local file.
        /// </summary>
        /// <param name="path">The path to the target file in the local file system.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToFile(string path, FileMode mode, AsyncCallback callback, object state)
        {
            return this.BeginDownloadToFile(path, mode, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file in the File service to a local file.
        /// </summary>
        /// <param name="path">The path to the target file in the local file system.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToFile(string path, FileMode mode, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginDownloadToFile(path, mode, accessCondition, options, operationContext, default(IProgress<StorageProgress>), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file in the File service to a local file.
        /// </summary>
        /// <param name="path">The path to the target file in the local file system.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadToFile(string path, FileMode mode, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadToFileAsync(path, mode, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a file in the File service to a local file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDownloadToFile(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file in the File service to a local file.
        /// </summary>
        /// <param name="path">The path to the target file in the local file system.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode)
        {
            return this.DownloadToFileAsync(path, mode, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file in the File service to a local file.
        /// </summary>
        /// <param name="path">The path to the target file in the local file system.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode, CancellationToken cancellationToken)
        {
            return this.DownloadToFileAsync(path, mode, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file in the File service to a local file.
        /// </summary>
        /// <param name="path">The path to the target file in the local file system.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the cloud file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToFileAsync(path, mode, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file in the File service to a local file.
        /// </summary>
        /// <param name="path">The path to the target file in the local file system.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the cloud file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DownloadToFileAsync(path, mode, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file in the File service to a local file.
        /// </summary>
        /// <param name="path">The path to the target file in the local file system.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the cloud file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);
            FileStream stream = new FileStream(path, mode, FileAccess.Write, FileShare.None);

            try
            {
                using (stream)
                {
                    await this.DownloadToStreamAsync(stream, accessCondition, options, operationContext, progressHandler, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                if (mode == FileMode.Create || mode == FileMode.CreateNew)
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception)
                    {
                        // Best effort to clean up in the event that download was unsuccessful.
                        // Do not throw as we want to throw original exception.
                    }
                }

                throw;
            }
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual int DownloadToByteArray(byte[] target, int index, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            return this.DownloadRangeToByteArray(target, index, null /* fileOffset */, null /* length */, accessCondition, options, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToByteArray(byte[] target, int index, AsyncCallback callback, object state)
        {
            return this.BeginDownloadToByteArray(target, index, null /* accessCondition */, null /* options */, null /*operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToByteArray(byte[] target, int index, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginDownloadRangeToByteArray(target, index, null /* fileOffset */, null /* length */, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgressHandler"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadToByteArray(byte[] target, int index, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadToByteArrayAsync(target, index, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public virtual int EndDownloadToByteArray(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<int>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index)
        {
            return this.DownloadToByteArrayAsync(target, index, default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, CancellationToken cancellationToken)
        {
            return this.DownloadToByteArrayAsync(target, index, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToByteArrayAsync(target, index, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, null /* fileOffset */, null/* length */, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, null /* fileOffset */, null/* length */, accessCondition, options, operationContext, progressHandler, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads the file's contents as a string.
        /// </summary>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the type of text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The contents of the file, as a string.</returns>
        [DoesServiceRequest]        
        public virtual string DownloadText(Encoding encoding = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            using (SyncMemoryStream stream = new SyncMemoryStream())
            {
                this.DownloadToStream(stream, accessCondition, options, operationContext);
                byte[] streamAsBytes = stream.GetBuffer();
                return (encoding ?? Encoding.UTF8).GetString(streamAsBytes, 0, (int)stream.Length);
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to download the file's contents as a string.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadText(AsyncCallback callback, object state)
        {
            return this.BeginDownloadText(null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the file's contents as a string.
        /// </summary>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the type of text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadText(Encoding encoding, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginDownloadText(encoding, accessCondition, options, operationContext, default(IProgress<StorageProgress>), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the file's contents as a string.
        /// </summary>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the type of text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgressHandler"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadText(Encoding encoding, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadTextAsync(encoding, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }


        /// <summary>
        /// Ends an asynchronous operation to download the file's contents as a string.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The contents of the file, as a string.</returns>
        public virtual string EndDownloadText(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<string>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the file's contents as a string.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync()
        {
            return this.DownloadTextAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the file's contents as a string.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(CancellationToken cancellationToken)
        {
            return this.DownloadTextAsync(null /*encoding*/, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the file's contents as a string.
        /// </summary>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the type of text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadTextAsync(encoding, accessCondition, options, operationContext, default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the file's contents as a string.
        /// </summary>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the type of text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DownloadTextAsync(encoding, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the file's contents as a string.
        /// </summary>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the type of text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            using (SyncMemoryStream stream = new SyncMemoryStream())
            {
                await this.DownloadToStreamAsync(stream, accessCondition, options, operationContext, progressHandler, cancellationToken).ConfigureAwait(false);

                byte[] streamAsBytes = stream.ToArray();
                return (encoding ?? Encoding.UTF8).GetString(streamAsBytes, 0, (int)streamAsBytes.Length);
            }
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void DownloadRangeToStream(Stream target, long? offset, long? length, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("target", target);
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.GetFileImpl(target, offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadRangeToStream(Stream target, long? offset, long? length, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadRangeToStreamAsync(target, offset, length, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadRangeToStream(Stream target, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadRangeToStream(Stream target, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDownloadRangeToStream(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>),cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("target", target);

            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);

            await Executor.ExecuteAsync(
                this.GetFileImpl(new AggregatingProgressIncrementer(progressHandler).CreateProgressIncrementingStream(target), offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken).ConfigureAwait(false);
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual int DownloadRangeToByteArray(byte[] target, int index, long? fileOffset, long? length, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            using (SyncMemoryStream stream = new SyncMemoryStream(target, index))
            {
                this.DownloadRangeToStream(stream, fileOffset, length, accessCondition, options, operationContext);
                return (int)stream.Position;
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadRangeToByteArray(byte[] target, int index, long? fileOffset, long? length, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadRangeToByteArrayAsync(target, index, fileOffset, length, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadRangeToByteArray(byte[] target, int index, long? fileOffset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadRangeToByteArrayAsync(target, index, fileOffset, length, accessCondition, options, operationContext, default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadRangeToByteArray(byte[] target, int index, long? fileOffset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadRangeToByteArrayAsync(target, index, fileOffset, length, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public virtual int EndDownloadRangeToByteArray(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<int>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, fileOffset, length, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, fileOffset, length, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, fileOffset, length, accessCondition, options, operationContext, default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, fileOffset, length, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to download the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            using (SyncMemoryStream stream = new SyncMemoryStream(target, index))
            {
                await this.DownloadRangeToStreamAsync(stream, fileOffset, length, accessCondition, options, operationContext, progressHandler, cancellationToken).ConfigureAwait(false);
                return (int)stream.Position;
            }
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void UploadFromStream(Stream source, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.UploadFromStreamHelper(source, null /* length */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void UploadFromStream(Stream source, long length, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.UploadFromStreamHelper(source, length, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        internal void UploadFromStreamHelper(Stream source, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("source", source);

            if (!source.CanSeek)
            {
                throw new InvalidOperationException();
            }

            if (length.HasValue)
            {
                CommonUtility.AssertInBounds("length", length.Value, 1, source.Length - source.Position);
            }
            else
            {
                length = source.Length - source.Position;
            }

            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            using (CloudFileStream fileStream = this.OpenWrite(length, accessCondition, modifiedOptions, operationContext))
            {
                using (ExecutionState<NullType> tempExecutionState = FileCommonUtility.CreateTemporaryExecutionState(modifiedOptions))
                {
                    source.WriteToSync(fileStream, length, null /* maxLength */, false, true, tempExecutionState, null /* streamCopyState */);
                    fileStream.Commit();
                }
            }
        }
#endif
        /// <summary>
        /// Uploads a stream to a file. 
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        private async Task UploadFromStreamAsyncHelper(Stream source, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("source", source);

            if (!source.CanSeek)
            {
                throw new InvalidOperationException();
            }

            if (length.HasValue)
            {
                CommonUtility.AssertInBounds("length", length.Value, 1, source.Length - source.Position);
            }
            else
            {
                length = source.Length - source.Position;
            }

            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            using (CloudFileStream fileStream = await this.OpenWriteAsync(length, accessCondition, options, operationContext, cancellationToken).ConfigureAwait(false))
            {
                using (ExecutionState<NullType> tempExecutionState = FileCommonUtility.CreateTemporaryExecutionState(modifiedOptions))
                {
                    // We should always call AsStreamForWrite with bufferSize=0 to prevent buffering. Our
                    // stream copier only writes 64K buffers at a time anyway, so no buffering is needed.
                    await source.WriteToAsync(new AggregatingProgressIncrementer(progressHandler).CreateProgressIncrementingStream(fileStream), this.ServiceClient.BufferManager, length, null /* maxLength */, false, tempExecutionState, default(StreamDescriptor)/* streamCopyState */, cancellationToken).ConfigureAwait(false);
                    await fileStream.CommitAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromStreamAsyncHelper(source, null /*length*/, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten. 
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromStreamAsyncHelper(source, null /*length*/, accessCondition, options, operationContext, default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten. 
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginUploadFromStream(Stream source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromStreamAsyncHelper(source, null /*length*/, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">Specifies the number of bytes from the Stream source to upload from the start position.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, long length, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => UploadFromStreamAsyncHelper(source, length, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">Specifies the number of bytes from the Stream source to upload from the start position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">Specifies the number of bytes from the Stream source to upload from the start position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginUploadFromStream(Stream source, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, progressHandler, token), callback, state);

        }

        /// <summary>
        /// Ends an asynchronous operation to upload a stream to a file. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadFromStream(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source)
        {
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, accessCondition, options, operationContext, default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, accessCondition, options, operationContext, progressHandler, cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length)
        {
            return this.UploadFromStreamAsyncHelper(source, length, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, progressHandler, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a file to the File service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="path">The file providing the content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void UploadFromFile(string path, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("path", path);

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                this.UploadFromStream(fileStream, accessCondition, options, operationContext);
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a file to the File service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="path">The file providing the content.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>        
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromFile(string path, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromFileAsync(path, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a file to the File service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="path">The file providing the content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromFile(string path, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromFileAsync(path, accessCondition, options, operationContext, default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a file to the File service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="path">The file providing the content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHanfler"> An <see cref="IProgress<StorageProgress>"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginUploadFromFile(string path, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromFileAsync(path, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a file to the File service.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadFromFile(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a local file to the File service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="path">The file providing the file content.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path)
        {
            return this.UploadFromFileAsync(path, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a local file to the File service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="path">The file providing the file content.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, CancellationToken cancellationToken)
        {
            return this.UploadFromFileAsync(path, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a local file to the File service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="path">The file providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromFileAsync(path, accessCondition, options, operationContext, default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a local file to the File service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="path">The file providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromFileAsync(path, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a local file to the File service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="path">The file providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task UploadFromFileAsync(string path, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);

            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                await this.UploadFromStreamAsync(stream, accessCondition, options, operationContext, progressHandler, cancellationToken).ConfigureAwait(false);
            }
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void UploadFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            using (SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count))
            {
                this.UploadFromStream(stream, accessCondition, options, operationContext);
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromByteArray(byte[] buffer, int index, int count, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromByteArrayAsync(buffer, index, count, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginUploadFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload the contents of a byte array to a file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadFromByteArray(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, CancellationToken cancellationToken)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.UploadFromStreamAsync(stream, stream.Length, accessCondition, options, operationContext, progressHandler, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">An object that indicates the text encoding to use. If null, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void UploadText(string content, Encoding encoding = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            this.UploadFromByteArray(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a string of text to a file.  If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadText(string content, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadTextAsync(content, null /*encoding*/, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">An object that indicates the text encoding to use. If null, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadText(string content, Encoding encoding, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, default(IProgress<StorageProgress>), token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">An object that indicates the text encoding to use. If null, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginUploadText(string content, Encoding encoding, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a string of text to a file. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadText(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content)
        {
            return this.UploadTextAsync(content, null /*encoding*/, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, CancellationToken cancellationToken)
        {
            return this.UploadTextAsync(content, null /*encoding*/, default(AccessCondition), default(FileRequestOptions), default(OperationContext), default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">An object that indicates the text encoding to use. If null, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, default(IProgress<StorageProgress>), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">An object that indicates the text encoding to use. If null, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to upload a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">An object that indicates the text encoding to use. If null, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.UploadFromByteArrayAsync(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext, progressHandler, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Creates a file. If the file already exists, it will be overwritten.3584
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void Create(long size, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            this.AssertValidFilePermissionOrKey();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.CreateImpl(size, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to create a file. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreate(long size, AsyncCallback callback, object state)
        {
            return this.BeginCreate(size, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to create a file. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreate(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.CreateAsync(size, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to create a file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndCreate(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a file. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size)
        {
            return this.CreateAsync(size, default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a file. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size, CancellationToken cancellationToken)
        {
            return this.CreateAsync(size, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a file. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.CreateAsync(size, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to create a file. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            this.AssertValidFilePermissionOrKey();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.CreateImpl(size, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Checks existence of the file.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the file exists; <c>false</c>, otherwise.</returns>
        [DoesServiceRequest]
        public virtual bool Exists(FileRequestOptions options = null, OperationContext operationContext = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteSync(
                this.ExistsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous request to check existence of the file.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExists(AsyncCallback callback, object state)
        {
            return this.BeginExists(null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to check existence of the file.
        /// </summary>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExists(FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => ExistsAsync(options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the asynchronous result of the request to check existence of the file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the file exists; <c>false</c>, otherwise.</returns>
        public virtual bool EndExists(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<bool>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous request to check existence of the file.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
        {
            return this.ExistsAsync(default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to check existence of the file.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            return this.ExistsAsync(default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to check existence of the file.
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
        /// Returns a task that performs an asynchronous request to check existence of the file.
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
        /// Populates a file's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
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
        /// Begins an asynchronous operation to populate the file's properties and metadata.
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
        /// Begins an asynchronous operation to populate the file's properties and metadata.
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.FetchAttributesAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to populate the file's properties and metadata.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndFetchAttributes(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to populate the file's properties and metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync()
        {
            return this.FetchAttributesAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to populate the file's properties and metadata.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(CancellationToken cancellationToken)
        {
            return this.FetchAttributesAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to populate the file's properties and metadata.
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
        /// Returns a task that performs an asynchronous operation to populate the file's properties and metadata.
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
        /// Deletes the file.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void Delete(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.DeleteFileImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to delete the file.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(AsyncCallback callback, object state)
        {
            return this.BeginDelete(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to delete the file.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DeleteAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to delete the file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDelete(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete the file.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
        {
            return this.DeleteAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete the file.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(CancellationToken cancellationToken)
        {
            return this.DeleteAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete the file.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to delete the file.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.DeleteFileImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Deletes the file if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the file did already exist and was deleted; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual bool DeleteIfExists(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

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
                        (e.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ResourceNotFound))
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
        /// Begins an asynchronous request to delete the file if it already exists.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(AsyncCallback callback, object state)
        {
            return this.BeginDeleteIfExists(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to delete the file if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DeleteIfExistsAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the result of an asynchronous request to delete the file if it already exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the file did already exist and was deleted; otherwise, <c>false</c>.</returns>
        public virtual bool EndDeleteIfExists(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<bool>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the file if it already exists.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
        {
            return this.DeleteIfExistsAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the file if it already exists.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(CancellationToken cancellationToken)
        {
            return this.DeleteIfExistsAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the file if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous request to delete the file if it already exists.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            try
            {
                await this.DeleteAsync(accessCondition, modifiedOptions, operationContext, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception)
            {
                if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    StorageExtendedErrorInformation extendedInfo = operationContext.LastResult.ExtendedErrorInformation;
                    if ((extendedInfo == null) ||
                        (extendedInfo.ErrorCode == StorageErrorCodeStrings.ResourceNotFound))
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
        /// Gets a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="offset">The starting offset of the data range over which to list file ranges, in bytes.</param>
        /// <param name="length">The length of the data range over which to list file ranges, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of ranges.</returns>
        [DoesServiceRequest]
        public virtual IEnumerable<FileRange> ListRanges(long? offset = null, long? length = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteSync(
                this.ListRangesImpl(offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to return a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginListRanges(AsyncCallback callback, object state)
        {
            return this.BeginListRanges(null /* offset */, null /* length */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to return a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="offset">The starting offset of the data range over which to list file ranges, in bytes.</param>
        /// <param name="length">The length of the data range over which to list file ranges, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginListRanges(long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.ListRangesAsync(offset, length, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to return a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>An enumerable collection of ranges.</returns>
        public virtual IEnumerable<FileRange> EndListRanges(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<IEnumerable<FileRange>>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to return a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<FileRange>> ListRangesAsync()
        {
            return this.ListRangesAsync(null /*offset*/, null /*length*/, default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to return a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<FileRange>> ListRangesAsync(CancellationToken cancellationToken)
        {
            return this.ListRangesAsync(null /*offset*/, null /*length*/, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to return a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="offset">The starting offset of the data range over which to list file ranges, in bytes.</param>
        /// <param name="length">The length of the data range over which to list file ranges, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<FileRange>> ListRangesAsync(long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ListRangesAsync(offset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to return a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="offset">The starting offset of the data range over which to list file ranges, in bytes.</param>
        /// <param name="length">The length of the data range over which to list file ranges, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<FileRange>> ListRangesAsync(long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.ListRangesImpl(offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC 
        /// <summary> 
        /// Gets the SMB handles open on this file. 
        /// </summary> 
        /// <param name="token">Continuation token for continuing paginated results.</param> 
        /// <param name="maxResults">The maximum number of results to be returned by the server.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <returns>An enumerable collection of ranges.</returns> 
        [DoesServiceRequest]
        public virtual FileHandleResultSegment ListHandlesSegmented(FileContinuationToken token = null, int? maxResults = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteSync(
                this.ListHandlesImpl(token, maxResults, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary> 
        /// Begins an asynchronous operation to get the SMB handles open on this file. 
        /// </summary> 
        /// <param name="token">Continuation token for paginated results.</param> 
        /// <param name="maxResults">The maximum number of results to be returned by the server.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param> 
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param> 
        /// <returns></returns> 
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginListHandlesSegmented(FileContinuationToken token, int? maxResults, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(cancellationToken => this.ListHandlesSegmentedAsync(token, maxResults, accessCondition, options, operationContext, cancellationToken), callback, state);
        }

        /// <summary> 
        /// Ends an asynchronous operation to get the SMB handles open on this file. 
        /// </summary> 
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param> 
        /// <returns>An enumerable collection of ranges.</returns> 
        public virtual FileHandleResultSegment EndListHandlesSegmented(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<FileHandleResultSegment>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary> 
        /// Returns a task that performs an asynchronous operation to get the SMB handles open on this file. 
        /// </summary> 
        /// <param name="token">Continuation token for paginated results.</param> 
        /// <param name="maxResults">The maximum number of results to be returned by the server.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param> 
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns> 
        [DoesServiceRequest]
        public virtual Task<FileHandleResultSegment> ListHandlesSegmentedAsync(FileContinuationToken token = null, int? maxResults = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null, CancellationToken? cancellationToken = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.ListHandlesImpl(token, maxResults, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken ?? CancellationToken.None);
        }
#endif

#if SYNC
        /// <summary> 
        /// Closes all SMB handles on this file. 
        /// </summary> 
        /// <param name="token">Continuation token for closing many files.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        [DoesServiceRequest]
        public virtual CloseFileHandleResultSegment CloseAllHandlesSegmented(FileContinuationToken token = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteSync(
                this.CloseHandleImpl(token, Constants.HeaderConstants.AllFileHandles, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary> 
        /// Begins an asynchronous operation to close all SMB handles on this file. 
        /// </summary> 
        /// <param name="token">Continuation token for closing many handles.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param> 
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param> 
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns> 
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCloseAllHandlesSegmented(FileContinuationToken token, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(cancellationToken => this.CloseAllHandlesSegmentedAsync(token, accessCondition, options, operationContext, cancellationToken), callback, state);
        }

        /// <summary> 
        /// Ends an asynchronous operation to close all SMB handles on this file. 
        /// </summary> 
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param> 
        /// <returns>An enumerable collection of ranges.</returns> 
        public virtual CloseFileHandleResultSegment EndCloseAllHandlesSegmented(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<CloseFileHandleResultSegment>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary> 
        /// Returns a task that performs an asynchronous operation to close all SMB handles on this file. 
        /// </summary> 
        /// <param name="token">Continuation token for closing many handles.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param> 
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns> 
        [DoesServiceRequest]
        public virtual Task<CloseFileHandleResultSegment> CloseAllHandlesSegmentedAsync(FileContinuationToken token = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null, CancellationToken? cancellationToken = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.CloseHandleImpl(token, Constants.HeaderConstants.AllFileHandles, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken ?? CancellationToken.None);
        }
#endif

#if SYNC
        /// <summary> 
        /// Closes the specified SMB handle on this file. 
        /// </summary> 
        /// <param name="handleId">Id of the handle, "*" if all handles on the file.</param> 
        /// <param name="token">Continuation token for when the handle takes exceedinly long to find.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        [DoesServiceRequest]
        public virtual CloseFileHandleResultSegment CloseHandleSegmented(string handleId, FileContinuationToken token = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteSync(
                this.CloseHandleImpl(token, handleId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary> 
        /// Begins an asynchronous operation to close the specified SMB handle on this file. 
        /// </summary> 
        /// <param name="handleId">Id of the handle, "*" if all handles on the file.</param> 
        /// <param name="token">Continuation token for when the handle takes exceedingly long to find.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param> 
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param> 
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns> 
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCloseHandleSegmented(string handleId, FileContinuationToken token, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(cancellationToken => this.CloseHandleSegmentedAsync(handleId, token, accessCondition, options, operationContext, cancellationToken), callback, state);
        }

        /// <summary> 
        /// Ends an asynchronous operation to close the specified SMB handle on this file. 
        /// </summary> 
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param> 
        /// <returns>An enumerable collection of ranges.</returns> 
        public virtual CloseFileHandleResultSegment EndCloseHandleSegmented(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<CloseFileHandleResultSegment>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary> 
        /// Returns a task that performs an asynchronous operation to close the specified SMB handle on this file. 
        /// </summary> 
        /// <param name="handleId">Id of the handle, "*" if all handles on the file.</param> 
        /// <param name="token">Continuation token for when the handle takes exceedingly long to find.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param> 
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param> 
        /// <returns>A <see cref="Task{T}"/> object that represents the current operation.</returns> 
        [DoesServiceRequest]
        public virtual Task<CloseFileHandleResultSegment> CloseHandleSegmentedAsync(string handleId, FileContinuationToken token = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null, CancellationToken? cancellationToken = null)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.CloseHandleImpl(token, handleId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken ?? CancellationToken.None);
        }
#endif

#if SYNC
        /// <summary>
        /// Updates the file's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void SetProperties(AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            this.AssertValidFilePermissionOrKey();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.SetPropertiesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to update the file's properties.
        /// </summary>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetProperties(AsyncCallback callback, object state)
        {
            return this.BeginSetProperties(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to update the file's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetProperties(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => SetPropertiesAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to update the file's properties.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetProperties(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the file's properties.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync()
        {
            return this.SetPropertiesAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the file's properties.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(CancellationToken cancellationToken)
        {
            return this.SetPropertiesAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the file's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetPropertiesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the file's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();
            this.AssertValidFilePermissionOrKey();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.SetPropertiesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Resizes a file.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void Resize(long size, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.ResizeImpl(size, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to resize a file.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginResize(long size, AsyncCallback callback, object state)
        {
            return this.BeginResize(size, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to resize a file.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginResize(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.ResizeAsync(size, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to resize a file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndResize(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to resize a file.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task ResizeAsync(long size)
        {
            return this.ResizeAsync(size, default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to resize a file.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task ResizeAsync(long size, CancellationToken cancellationToken)
        {
            return this.ResizeAsync(size, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to resize a file.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task ResizeAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ResizeAsync(size, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to resize a file.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task ResizeAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.ResizeImpl(size, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Updates the file's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
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
        /// Begins an asynchronous operation to update the file's metadata.
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
        /// Begins an asynchronous operation to update the file's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetMetadata(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);

            return CancellableAsyncResultTaskWrapper.Create(token => this.SetMetadataAsync(accessCondition, modifiedOptions, operationContext), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to update the file's metadata.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetMetadata(IAsyncResult asyncResult)
        {
            CommonUtility.AssertNotNull(nameof(asyncResult), asyncResult);
            ((CancellableAsyncResultTaskWrapper)(asyncResult)).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the file's metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync()
        {
            return this.SetMetadataAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the file's metadata.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(CancellationToken cancellationToken)
        {
            return this.SetMetadataAsync(default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the file's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to update the file's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
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

#if SYNC
        /// <summary>
        /// Writes range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void WriteRange(Stream rangeData, long startOffset, string contentMD5 = null, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("rangeData", rangeData);

            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            bool requiresContentMD5 = (contentMD5 == null) && modifiedOptions.UseTransactionalMD5.Value;
            operationContext = operationContext ?? new OperationContext();

            Stream seekableStream = rangeData;
            bool seekableStreamCreated = false;

            try
            {
                if (!rangeData.CanSeek || requiresContentMD5)
                {
                    ExecutionState<NullType> tempExecutionState = FileCommonUtility.CreateTemporaryExecutionState(modifiedOptions);

                    Stream writeToStream;
                    if (rangeData.CanSeek)
                    {
                        writeToStream = Stream.Null;
                    }
                    else
                    {
                        seekableStream = new MultiBufferMemoryStream(this.ServiceClient.BufferManager);
                        seekableStreamCreated = true;
                        writeToStream = seekableStream;
                    }

                    long startPosition = seekableStream.Position;
                    StreamDescriptor streamCopyState = new StreamDescriptor();
                    rangeData.WriteToSync(writeToStream, null /* copyLength */, Constants.MaxBlockSize, requiresContentMD5, true, tempExecutionState, streamCopyState);
                    seekableStream.Position = startPosition;

                    if (requiresContentMD5)
                    {
                        contentMD5 = streamCopyState.Md5;
                    }
                }

                Executor.ExecuteSync(
                    this.PutRangeImpl(seekableStream, startOffset, contentMD5, accessCondition, modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext);
            }
            finally
            {
                if (seekableStreamCreated)
                {
                    seekableStream.Dispose();
                }
            }
        }
#endif

                    /// <summary>
                    /// Begins an asynchronous operation to write a range to a file.
                    /// </summary>
                    /// <param name="rangeData">A stream providing the data.</param>
                    /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
                    /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
                    /// on the file. May be <c>null</c> or an empty string.</param>
                    /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
                    /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
                    /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginWriteRange(Stream rangeData, long startOffset, string contentMD5, AsyncCallback callback, object state)
        {
            return this.BeginWriteRange(rangeData, startOffset, contentMD5, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to write a range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginWriteRange(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginWriteRange(rangeData, startOffset, contentMD5, accessCondition, options, operationContext, default(IProgress<StorageProgress>), callback, state);
        }
        /// <summary>
        /// Begins an asynchronous operation to write a range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress<StorageProgress>"/> object to gather progress deltas.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginWriteRange(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.WriteRangeAsync(rangeData, startOffset, contentMD5, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }        

        /// <summary>
        /// Ends an asynchronous operation to write a range to a file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndWriteRange(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to write a range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <c>null</c> or an empty string.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5)
        {
            return this.WriteRangeAsync(rangeData, startOffset, contentMD5, default(AccessCondition), default(FileRequestOptions), default(OperationContext), null /*progressHandler*/, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to write a range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <c>null</c> or an empty string.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5, CancellationToken cancellationToken)
        {
            return this.WriteRangeAsync(rangeData, startOffset, contentMD5, default(AccessCondition), default(FileRequestOptions), default(OperationContext), null /*progressHandler*/, cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to write a range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.WriteRangeAsync(rangeData, startOffset, contentMD5, accessCondition, options, operationContext, null /*progressHandler*/, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to write a range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.WriteRangeAsync(rangeData, startOffset, contentMD5, accessCondition, options, operationContext, null /*progressHandler*/, cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to write a range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual async Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("rangeData", rangeData);

            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            bool requiresContentMD5 = (contentMD5 == null) && modifiedOptions.UseTransactionalMD5.Value;
            operationContext = operationContext ?? new OperationContext();

            DateTime streamCopyStartTime = DateTime.Now;
            
            Stream seekableStream = rangeData;
            bool seekableStreamCreated = false;

            try
            {
                if (!rangeData.CanSeek || requiresContentMD5)
                {
                    ExecutionState<NullType> tempExecutionState = FileCommonUtility.CreateTemporaryExecutionState(modifiedOptions);

                    Stream writeToStream;
                    if (rangeData.CanSeek)
                    {
                        writeToStream = Stream.Null;
                    }
                    else
                    {
                        seekableStream = new MultiBufferMemoryStream(this.ServiceClient.BufferManager);
                        seekableStreamCreated = true;
                        writeToStream = seekableStream;
                    }

                    StreamDescriptor streamCopyState = new StreamDescriptor();
                    long startPosition = seekableStream.Position;
                    await rangeData.WriteToAsync(writeToStream, this.ServiceClient.BufferManager, null /* copyLength */, Constants.MaxBlockSize, requiresContentMD5, tempExecutionState, streamCopyState, cancellationToken).ConfigureAwait(false);
                    seekableStream.Position = startPosition;

                    if (requiresContentMD5)
                    {
                        contentMD5 = streamCopyState.Md5;
                    }
                    if (modifiedOptions.MaximumExecutionTime.HasValue)
                    {
                        modifiedOptions.MaximumExecutionTime -= DateTime.Now.Subtract(streamCopyStartTime);
                    }
                }
                await Executor.ExecuteAsync(
                this.PutRangeImpl(new AggregatingProgressIncrementer(progressHandler).CreateProgressIncrementingStream(seekableStream), startOffset, contentMD5, accessCondition, modifiedOptions),

                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (seekableStreamCreated)
                {
                    seekableStream.Dispose();
                }
            }
        }
#endif

#if SYNC
        /// <summary>
        /// Clears ranges from a file.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing ranges, in bytes. </param>
        /// <param name="length">The length of the range to be cleared, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void ClearRange(long startOffset, long length, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.ClearRangeImpl(startOffset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to clear ranges from a file.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing ranges, in bytes.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginClearRange(long startOffset, long length, AsyncCallback callback, object state)
        {
            return this.BeginClearRange(startOffset, length, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to clear ranges from a file.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing ranges, in bytes.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">The callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginClearRange(long startOffset, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.ClearRangeAsync(startOffset, length, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to clear ranges from a file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndClearRange(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Returns a task that performs an asynchronous operation to clear ranges from a file.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing ranges, in bytes.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task ClearRangeAsync(long startOffset, long length)
        {
            return this.ClearRangeAsync(startOffset, length, default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to clear ranges from a file.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing ranges, in bytes.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task ClearRangeAsync(long startOffset, long length, CancellationToken cancellationToken)
        {
            return this.ClearRangeAsync(startOffset, length, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to clear ranges from a file.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing ranges, in bytes.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task ClearRangeAsync(long startOffset, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ClearRangeAsync(startOffset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Returns a task that performs an asynchronous operation to clear ranges from a file.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing ranges, in bytes.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the current operation.</returns>
        [DoesServiceRequest]
        public virtual Task ClearRangeAsync(long startOffset, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.ClearRangeImpl(startOffset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Begins an operation to start copying another Azure file or blob's contents, properties, and metadata to this Azure file.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob or file.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source object. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the file's ETag, last-modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual string StartCopy(Uri source, AccessCondition sourceAccessCondition = null, AccessCondition destAccessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("source", source);

            this.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteSync(
                this.StartCopyImpl(source, sourceAccessCondition, destAccessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }

        /// <summary>
        /// Begins an operation to start copying another file's contents, properties, and metadata to this file.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the file's ETag, last-modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual string StartCopy(CloudFile source, AccessCondition sourceAccessCondition = null, AccessCondition destAccessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            return this.StartCopy(CloudFile.SourceFileToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext);
        }
#endif
        /// <summary>
        /// Begins an asynchronous operation to start copying another Azure file or blob's contents, properties, and metadata to this Azure file.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob or file.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(Uri source, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(source, null /* sourceAccessCondition */, null /* destAccessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying another file's contents, properties, and metadata to this file.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(CloudFile source, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(CloudFile.SourceFileToUri(source), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying another Azure file or blob's contents, properties, and metadata to this Azure file.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob or file.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source object. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying another file's contents, properties, and metadata to this file.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>        
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(CloudFile source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(CloudFile.SourceFileToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to start copying another Azure file or blob's contents, properties, and metadata to this Azure file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the file's ETag, last-modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        public virtual string EndStartCopy(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<string>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to start copying another Azure file or blob's contents, properties, and metadata to this Azure file.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob or file.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source)
        {
            return this.StartCopyAsync(source, default(AccessCondition), default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another Azure file or blob's contents, properties, and metadata to this Azure file.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob or file.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(source, default(AccessCondition), default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another file's contents, properties, and metadata to this file.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudFile source)
        {
            return this.StartCopyAsync(CloudFile.SourceFileToUri(source), default(AccessCondition), default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another file's contents, properties, and metadata to this file.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudFile source, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(CloudFile.SourceFileToUri(source), default(AccessCondition), default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another Azure file or blob's contents, properties, and metadata to this Azure file.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob or file.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source object. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another Azure file or blob's contents, properties, and metadata to this Azure file.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob or file.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source object. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync<string>(
                this.StartCopyImpl(source, sourceAccessCondition, destAccessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another file's contents, properties, and metadata to this file.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudFile source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(CloudFile.SourceFileToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another file's contents, properties, and metadata to this file.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudFile source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(CloudFile.SourceFileToUri(source), default(AccessCondition), default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Aborts an ongoing copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void AbortCopy(string copyId, AccessCondition accessCondition = null, FileRequestOptions options = null, OperationContext operationContext = null)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            Executor.ExecuteSync(
                this.AbortCopyImpl(copyId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to abort an ongoing copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAbortCopy(string copyId, AsyncCallback callback, object state)
        {
            return this.BeginAbortCopy(copyId, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to abort an ongoing copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAbortCopy(string copyId, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.AbortCopyAsync(copyId, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to abort an ongoing copy operation.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndAbortCopy(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to abort an ongoing copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId)
        {
            return this.AbortCopyAsync(copyId, default(AccessCondition), default(FileRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to abort an ongoing copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId, CancellationToken cancellationToken)
        {
            return this.AbortCopyAsync(copyId, default(AccessCondition), default(FileRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to abort an ongoing copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.AbortCopyAsync(copyId, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to abort an ongoing copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.Share.AssertNoSnapshot();
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.AbortCopyImpl(copyId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

        /// <summary>
        /// Implements getting the stream without specifying a range.
        /// </summary>
        /// <param name="destStream">The destination stream.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}" /> that gets the stream.
        /// </returns>
        private RESTCommand<NullType> GetFileImpl(Stream destStream, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options)
        {
            string lockedETag = null;
            AccessCondition lockedAccessCondition = null;

            bool arePropertiesPopulated = false;
            string storedMD5 = null;

            long startingOffset = offset.HasValue ? offset.Value : 0;
            long? startingLength = length;
            long? validateLength = null;

            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.DestinationStream = destStream;
            getCmd.CalculateMd5ForResponseStream = !options.DisableContentMD5Validation.Value;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.Get(uri, serverTimeout, offset, length, options.UseTransactionalMD5.Value, this.Share.SnapshotTime, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.RecoveryAction = (cmd, ex, ctx) =>
            {
                if ((lockedAccessCondition == null) && !string.IsNullOrEmpty(lockedETag))
                {
                    lockedAccessCondition = AccessCondition.GenerateIfMatchCondition(lockedETag);
                    if (accessCondition != null)
                    {
                        lockedAccessCondition.LeaseId = accessCondition.LeaseId;
                    }
                }

                if (cmd.StreamCopyState != null)
                {
                    offset = startingOffset + cmd.StreamCopyState.Length;
                    if (startingLength.HasValue)
                    {
                        length = startingLength.Value - cmd.StreamCopyState.Length;
                    }
                }

                getCmd.BuildRequest = (command, uri, builder, cnt, serverTimeout, context) => FileHttpRequestMessageFactory.Get(uri, serverTimeout, offset, length, options.UseTransactionalMD5.Value && !arePropertiesPopulated, this.Share.SnapshotTime, accessCondition, cnt, context, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            };

            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(offset.HasValue ? HttpStatusCode.PartialContent : HttpStatusCode.OK, resp, NullType.Value, cmd, ex);

                if (!arePropertiesPopulated)
                {
                    this.UpdateAfterFetchAttributes(resp);

                    if (resp.Content.Headers.ContentMD5 != null)
                    {
                        storedMD5 = HttpResponseParsers.GetContentMD5(resp);
                    }

                    if (!options.DisableContentMD5Validation.Value &&
                        options.UseTransactionalMD5.Value &&
                        string.IsNullOrEmpty(storedMD5))
                    {
                        throw new StorageException(
                            cmd.CurrentResult,
                            SR.MD5NotPresentError,
                            null)
                        {
                            IsRetryable = false
                        };
                    }

                    // If the download fails and Get File needs to resume the download, going to the
                    // same storage location is important to prevent a possible ETag mismatch.
                    getCmd.CommandLocationMode = cmd.CurrentResult.TargetLocation == StorageLocation.Primary ? CommandLocationMode.PrimaryOnly : CommandLocationMode.SecondaryOnly;
                    lockedETag = attributes.Properties.ETag;
                    validateLength = resp.Content.Headers.ContentLength;

                    arePropertiesPopulated = true;
                }
                else
                {
                    if (!resp.Headers.ETag.ToString().Equals(lockedETag, StringComparison.Ordinal))
                    {
                        RequestResult reqResult = new RequestResult();
                        reqResult.HttpStatusMessage = null;
                        reqResult.HttpStatusCode = (int)HttpStatusCode.PreconditionFailed;
                        reqResult.ExtendedErrorInformation = null;
                        throw new StorageException(reqResult, SR.PreconditionFailed, null /* inner */);
                    }
                }

                return NullType.Value;
            };

            getCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) =>
            {
                HttpResponseParsers.ValidateResponseStreamMd5AndLength(validateLength, storedMD5, cmd);
                return NullType.ValueTask;
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the Create method.
        /// </summary>
        /// <param name="sizeInBytes">The size in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that creates the file.</returns>
        private RESTCommand<NullType> CreateImpl(long sizeInBytes, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.Create(
                    uri: uri,
                    timeout: serverTimeout,
                    properties: this.Properties,
                    filePermissionToSet: this.filePermission,
                    fileSize: sizeInBytes,
                    accessCondition: accessCondition,
                    content: cnt,
                    operationContext: ctx,
                    canonicalizer: this.ServiceClient.GetCanonicalizer(),
                    credentials: this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                FileHttpResponseParsers.UpdateSmbProperties(resp, this.Properties);
                this.UpdateETagLMTAndLength(resp, false);
                this.Properties.Length = sizeInBytes;

                // This is by design.  The service doesn't return filePermission in the File.Create, File.SetProperties, File.Get, or File.GetProperties responses.
                // Also, the service sometimes modifies the filePermission server-side, so this field may be inaccurate if we maintained it's value.
                // To retrieve a filePermission, call share.GetFilePermission(filePermissionId).
                this.filePermission = null;

                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implements the FetchAttributes method. The attributes are updated immediately.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that fetches the attributes.</returns>
        private RESTCommand<NullType> FetchAttributesImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.GetProperties(uri, serverTimeout, this.Share.SnapshotTime, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.UpdateAfterFetchAttributes(resp);
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
            RESTCommand<bool> getCmd = new RESTCommand<bool>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.GetProperties(uri, serverTimeout, this.Share.SnapshotTime, null /* accessCondition */, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, true, cmd, ex);
                this.UpdateAfterFetchAttributes(resp);
                return true;
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the DeleteFile method.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that deletes the file.</returns>
        private RESTCommand<NullType> DeleteFileImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> deleteCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(deleteCmd);
            deleteCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.Delete(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            deleteCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex);

            return deleteCmd;
        }

        /// <summary>
        /// Gets the ranges implementation.
        /// </summary>
        /// <param name="offset">The start offset.</param>
        /// <param name="length">Length of the data range to be cleared.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> for getting the ranges.</returns>
        private RESTCommand<IEnumerable<FileRange>> ListRangesImpl(long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<IEnumerable<FileRange>> getCmd = new RESTCommand<IEnumerable<FileRange>>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.ListRanges(uri, serverTimeout, offset, length, this.Share.SnapshotTime, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };

            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) =>
            {
                this.UpdateETagLMTAndLength(resp, true);
                return ListRangesResponse.ParseAsync(cmd.ResponseStream, ct);
            };

            return getCmd;
        }

        /// <summary> 
        /// Gets the list handles implementation. 
        /// </summary> 
        /// <param name="token">Continuation token for paged responses.</param> 
        /// <param name="maxResults">The maximum number of results to be returned by the server.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <returns>A <see cref="RESTCommand{T}"/> for getting the handles.</returns> 
        private RESTCommand<FileHandleResultSegment> ListHandlesImpl(FileContinuationToken token, int? maxResults, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<FileHandleResultSegment> getCmd = new RESTCommand<FileHandleResultSegment>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);
            options.ApplyToStorageCommand(getCmd);

            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.ListHandles(uri, serverTimeout, this.Share.SnapshotTime, maxResults, false, token, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) =>
            {
                this.UpdateETagLMTAndLength(resp, true);
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
        /// <param name="token">Continuation token for closing many handles.</param> 
        /// <param name="handleId">Id of the handle, "*" if all handles on the file.</param> 
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param> 
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param> 
        /// <returns>A <see cref="RESTCommand{T}"/> for closing the handles.</returns> 
        private RESTCommand<CloseFileHandleResultSegment> CloseHandleImpl(FileContinuationToken token, string handleId, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<CloseFileHandleResultSegment> putCmd = new RESTCommand<CloseFileHandleResultSegment>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);
            options.ApplyToStorageCommand(putCmd);
            putCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            putCmd.RetrieveResponseStream = true;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.CloseHandle(uri, serverTimeout, this.Share.SnapshotTime, handleId, false, token, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
        /// Implementation for the SetProperties method.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetPropertiesImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.SetProperties(
                    uri: uri,
                    timeout: serverTimeout,
                    properties: this.Properties,
                    filePermissionToSet: this.FilePermission,
                    accessCondition: accessCondition,
                    content: cnt,
                    operationContext: ctx,
                    canonicalizer: this.ServiceClient.GetCanonicalizer(),
                    credentials: this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                FileHttpResponseParsers.UpdateSmbProperties(resp, this.Properties);
                this.UpdateETagLMTAndLength(resp, false);

                // This is by design.  The service doesn't return filePermission in the File.Create, File.SetProperties, File.Get, or File.GetProperties responses.
                // Also, the service sometimes modifies the filePermission server-side, so this field may be inaccurate if we maintained it's value.
                // To retrieve a filePermission, call share.GetFilePermission(filePermissionId).
                this.filePermission = null;

                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Resize method.
        /// </summary>
        /// <param name="sizeInBytes">The size in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that sets the metadata.</returns>
        private RESTCommand<NullType> ResizeImpl(long sizeInBytes, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.Resize(uri, serverTimeout, sizeInBytes, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.UpdateETagLMTAndLength(resp, false);
                this.Properties.Length = sizeInBytes;
                putCmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the SetMetadata method.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetMetadataImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.SetMetadata(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.UpdateETagLMTAndLength(resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation method for the WriteRange methods.
        /// </summary>
        /// <param name="rangeData">The data.</param>
        /// <param name="startOffset">The start offset.</param> 
        /// <param name="contentMD5">The content MD5.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that writes the range.</returns>
        private RESTCommand<NullType> PutRangeImpl(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options)
        {
            long offset = rangeData.Position;
            long length = rangeData.Length - offset;

            FileRange fileRange = new FileRange(startOffset, startOffset + length - 1);
            FileRangeWrite fileRangeWrite = FileRangeWrite.Update;

            if ((1 + fileRange.EndOffset - fileRange.StartOffset) == 0)
            {
                CommonUtility.ArgumentOutOfRange("rangeData", rangeData);
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(rangeData, offset, length, contentMD5, cmd, ctx);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.PutRange(uri, serverTimeout, fileRange, fileRangeWrite, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                this.UpdateETagLMTAndLength(resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation method for the ClearRange methods.
        /// </summary>
        /// <param name="startOffset">The start offset.</param>
        /// <param name="length">Length of the data range to be cleared.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that clears the range.</returns>
        private RESTCommand<NullType> ClearRangeImpl(long startOffset, long length, AccessCondition accessCondition, FileRequestOptions options)
        {
            CommonUtility.AssertNotNull("options", options);

            if (startOffset < 0)
            {
                CommonUtility.ArgumentOutOfRange("startOffset", startOffset);
            }

            if (length <= 0)
            {
                CommonUtility.ArgumentOutOfRange("length", length);
            }

            FileRange range = new FileRange(startOffset, startOffset + length - 1);
            FileRangeWrite fileWrite = FileRangeWrite.Clear;

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.PutRange(uri, serverTimeout, range, fileWrite, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                this.UpdateETagLMTAndLength(resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation of the StartCopy method. Result is a CloudFileAttributes object derived from the response headers.
        /// </summary>
        /// <param name="source">The URI of the source object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source object. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that starts to copy the file.
        /// </returns>
        /// <exception cref="System.ArgumentException">sourceAccessCondition</exception>
        private RESTCommand<string> StartCopyImpl(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options)
        {
            if (sourceAccessCondition != null && !string.IsNullOrEmpty(sourceAccessCondition.LeaseId))
            {
                throw new ArgumentException(SR.LeaseConditionOnSource, "sourceAccessCondition");
            }

            RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.CopyFrom(uri, serverTimeout, source, sourceAccessCondition, destAccessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, attributes.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, null /* retVal */, cmd, ex);
                CopyState state = FileHttpResponseParsers.GetCopyAttributes(resp);
                this.attributes.Properties = FileHttpResponseParsers.GetProperties(resp);
                this.attributes.Metadata = FileHttpResponseParsers.GetMetadata(resp);
                this.attributes.CopyState = state;
                return state.CopyId;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation of the AbortCopy method. No result is produced.
        /// </summary>
        /// <param name="copyId">The copy ID of the copy operation to abort.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that aborts the copy.
        /// </returns>
        private RESTCommand<NullType> AbortCopyImpl(string copyId, AccessCondition accessCondition, FileRequestOptions options)
        {
            CommonUtility.AssertNotNull("copyId", copyId);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.AbortCopy(uri, serverTimeout, copyId, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);

            return putCmd;
        }

        /// <summary>
        /// Converts the source blob of a copy operation to an appropriate access URI, taking Shared Access Signature credentials into account.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <returns>A URI addressing the source blob, using SAS if appropriate.</returns>
        internal static Uri SourceFileToUri(CloudFile source)
        {
            CommonUtility.AssertNotNull("source", source);
            return source.ServiceClient.Credentials.TransformUri(source.SnapshotQualifiedUri);
        }

        /// <summary>
        /// Updates this file with the given attributes a the end of a fetch attributes operation.
        /// </summary>
        /// <param name="response">The response.</param>
        private void UpdateAfterFetchAttributes(HttpResponseMessage response)
        {
            FileProperties properties = FileHttpResponseParsers.GetProperties(response);
            FileHttpResponseParsers.UpdateSmbProperties(response, properties);
            CopyState state = FileHttpResponseParsers.GetCopyAttributes(response);

            this.attributes.Properties = properties;
            this.attributes.Metadata = FileHttpResponseParsers.GetMetadata(response);
            this.attributes.CopyState = state;
        }

        /// <summary>
        /// Retrieve ETag, LMT and Length from response.
        /// </summary>
        /// <param name="response">The response to parse.</param>
        /// <param name="updateLength">If set to <c>true</c>, update the file length.</param>
        private void UpdateETagLMTAndLength(HttpResponseMessage response, bool updateLength)
        {
            FileProperties parsedProperties = FileHttpResponseParsers.GetProperties(response);
            this.Properties.ETag = parsedProperties.ETag ?? this.Properties.ETag;
            this.Properties.LastModified = parsedProperties.LastModified ?? this.Properties.LastModified;
            if (updateLength)
            {
                this.Properties.Length = parsedProperties.Length;
            }
        }
    }
}
