//-----------------------------------------------------------------------
// <copyright file="CloudBlob.cs" company="Microsoft">
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
namespace Microsoft.WindowsAzure.Storage.Blob
{
    using Microsoft.WindowsAzure.Storage.Blob.Protocol;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class CloudBlob : IListBlobItem
    {
#if SYNC
        /// <summary>
        /// Opens a stream for reading from the blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.IO.Stream"/> object to be used for reading from the blob.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="FetchAttributes(AccessCondition, BlobRequestOptions, OperationContext)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamMinimumReadSizeInBytes"/> property before calling this method to specify the minimum
        /// number of bytes to buffer when reading from the stream. The value must be at least 16 KB.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual Stream OpenRead(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.FetchAttributes(accessCondition, options, operationContext);
            AccessCondition streamAccessCondition = AccessCondition.CloneConditionWithETag(accessCondition, this.Properties.ETag);
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient, false);
            return new BlobReadStream(this, streamAccessCondition, modifiedOptions, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to open a stream for reading from the blob.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// <para>On the <see cref="System.IO.Stream"/> object returned by the <see cref="EndOpenRead"/> method, the <see cref="System.IO.Stream.EndRead(IAsyncResult)"/> 
        /// method must be called exactly once for every <see cref="System.IO.Stream.BeginRead(byte[], int, int, AsyncCallback, Object)"/> call. 
        /// Failing to end the read process before beginning another read process can cause unexpected behavior.</para>
        /// <para>Note that this method always makes a call to the <see cref="BeginFetchAttributes(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamMinimumReadSizeInBytes"/> property before calling this method to specify the minimum
        /// number of bytes to buffer when reading from the stream. The value must be at least 16 KB.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginOpenRead(AsyncCallback callback, object state)
        {
            return this.BeginOpenRead(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to open a stream for reading from the blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// <para>On the <see cref="System.IO.Stream"/> object returned by the <see cref="EndOpenRead"/> method, the <see cref="System.IO.Stream.EndRead(IAsyncResult)"/> 
        /// method must be called exactly once for every <see cref="System.IO.Stream.BeginRead(byte[], int, int, AsyncCallback, Object)"/> call. 
        /// Failing to end the read process before beginning another read process can cause unexpected behavior.</para>
        /// <para>Note that this method always makes a call to the <see cref="BeginFetchAttributes(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamMinimumReadSizeInBytes"/> property before calling this method to specify the minimum
        /// number of bytes to buffer when reading from the stream. The value must be at least 16 KB.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginOpenRead(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            StorageAsyncResult<Stream> storageAsyncResult = new StorageAsyncResult<Stream>(callback, state);

            ICancellableAsyncResult result = this.BeginFetchAttributes(
                accessCondition,
                options,
                operationContext,
                ar =>
                {
                    try
                    {
                        this.EndFetchAttributes(ar);
                        storageAsyncResult.UpdateCompletedSynchronously(ar.CompletedSynchronously);
                        AccessCondition streamAccessCondition = AccessCondition.CloneConditionWithETag(accessCondition, this.Properties.ETag);
                        BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient, false);
                        storageAsyncResult.Result = new BlobReadStream(this, streamAccessCondition, modifiedOptions, operationContext);
                        storageAsyncResult.OnComplete();
                    }
                    catch (Exception e)
                    {
                        storageAsyncResult.OnComplete(e);
                    }
                },
                null /* state */);

            storageAsyncResult.CancelDelegate = result.Cancel;
            return storageAsyncResult;
        }

        /// <summary>
        /// Ends an asynchronous operation to open a stream for reading from the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="System.IO.Stream"/> object to be used for reading from the blob.</returns>
        /// <remarks>
        /// <para>On the <see cref="System.IO.Stream"/> object returned by this method, the <see cref="System.IO.Stream.EndRead(IAsyncResult)"/> 
        /// method must be called exactly once for every <see cref="System.IO.Stream.BeginRead(byte[], int, int, AsyncCallback, Object)"/> call. 
        /// Failing to end the read process before beginning another read process can cause unexpected behavior.</para>
        /// </remarks>
        public virtual Stream EndOpenRead(IAsyncResult asyncResult)
        {
            StorageAsyncResult<Stream> storageAsyncResult = (StorageAsyncResult<Stream>)asyncResult;
            storageAsyncResult.End();
            return storageAsyncResult.Result;
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to open a stream for reading from the blob.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="System.IO.Stream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// <para>On the <see cref="System.IO.Stream"/> object returned by this method, the <see cref="System.IO.Stream.EndRead(IAsyncResult)"/> 
        /// method must be called exactly once for every <see cref="System.IO.Stream.BeginRead(byte[], int, int, AsyncCallback, Object)"/> call. 
        /// Failing to end the read process before beginning another read process can cause unexpected behavior.</para>
        /// <para>Note that this method always makes a call to the <see cref="FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamMinimumReadSizeInBytes"/> property before calling this method to specify the minimum
        /// number of bytes to buffer when reading from the stream. The value must be at least 16 KB.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync()
        {
            return this.OpenReadAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to open a stream for reading from the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="System.IO.Stream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// <para>On the <see cref="System.IO.Stream"/> object returned by this method, the <see cref="System.IO.Stream.EndRead(IAsyncResult)"/> 
        /// method must be called exactly once for every <see cref="System.IO.Stream.BeginRead(byte[], int, int, AsyncCallback, Object)"/> call. 
        /// Failing to end the read process before beginning another read process can cause unexpected behavior.</para>
        /// <para>Note that this method always makes a call to the <see cref="FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamMinimumReadSizeInBytes"/> property before calling this method to specify the minimum
        /// number of bytes to buffer when reading from the stream. The value must be at least 16 KB.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginOpenRead, this.EndOpenRead, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to open a stream for reading from the blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="System.IO.Stream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// <para>On the <see cref="System.IO.Stream"/> object returned by this method, the <see cref="System.IO.Stream.EndRead(IAsyncResult)"/> 
        /// method must be called exactly once for every <see cref="System.IO.Stream.BeginRead(byte[], int, int, AsyncCallback, Object)"/> call. 
        /// Failing to end the read process before beginning another read process can cause unexpected behavior.</para>
        /// <para>Note that this method always makes a call to the <see cref="FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamMinimumReadSizeInBytes"/> property before calling this method to specify the minimum
        /// number of bytes to buffer when reading from the stream. The value must be at least 16 KB.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.OpenReadAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to open a stream for reading from the blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="System.IO.Stream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// <para>On the <see cref="System.IO.Stream"/> object returned by this method, the <see cref="System.IO.Stream.EndRead(IAsyncResult)"/> 
        /// method must be called exactly once for every <see cref="System.IO.Stream.BeginRead(byte[], int, int, AsyncCallback, Object)"/> call. 
        /// Failing to end the read process before beginning another read process can cause unexpected behavior.</para>
        /// <para>Note that this method always makes a call to the <see cref="FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamMinimumReadSizeInBytes"/> property before calling this method to specify the minimum
        /// number of bytes to buffer when reading from the stream. The value must be at least 16 KB.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginOpenRead, this.EndOpenRead, accessCondition, options, operationContext, cancellationToken);
        }
#endif
#if SYNC
        /// <summary>
        /// Downloads the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void DownloadToStream(Stream target, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.DownloadRangeToStream(target, null /* offset */, null /* length */, accessCondition, options, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToStream(Stream target, AsyncCallback callback, object state)
        {
            return this.BeginDownloadToStream(target, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToStream(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginDownloadRangeToStream(target, null /* offset */, null /* length */, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a blob to a stream.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDownloadToStream(IAsyncResult asyncResult)
        {
            this.EndDownloadRangeToStream(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target)
        {
            return this.DownloadToStreamAsync(target, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginDownloadToStream, this.EndDownloadToStream, target, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToStreamAsync(target, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginDownloadToStream, this.EndDownloadToStream, target, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads the contents of a blob to a file.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void DownloadToFile(string path, FileMode mode, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("path", path);
            FileStream fileStream = new FileStream(path, mode, FileAccess.Write);

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
        /// Begins an asynchronous operation to download the contents of a blob to a file.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToFile(string path, FileMode mode, AsyncCallback callback, object state)
        {
            return this.BeginDownloadToFile(path, mode, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a blob to a file.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToFile(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("path", path);

            FileStream fileStream = new FileStream(path, mode, FileAccess.Write);
            StorageAsyncResult<NullType> storageAsyncResult = new StorageAsyncResult<NullType>(callback, state)
            {
                OperationState = Tuple.Create<FileStream, FileMode>(fileStream, mode)
            };

            try
            {
                ICancellableAsyncResult asyncResult = this.BeginDownloadToStream(fileStream, accessCondition, options, operationContext, this.DownloadToFileCallback, storageAsyncResult);
                storageAsyncResult.CancelDelegate = asyncResult.Cancel;
                return storageAsyncResult;
            }
            catch (Exception)
            {
                fileStream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Called when the asynchronous DownloadToStream operation completes.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        private void DownloadToFileCallback(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> storageAsyncResult = (StorageAsyncResult<NullType>)asyncResult.AsyncState;
            Exception exception = null;
            bool tryFileDelete = false;
            string filePath = null;

            try
            {
                this.EndDownloadToStream(asyncResult);
            }
            catch (Exception e)
            {
                exception = e;
                tryFileDelete = true;
            }

            // We should do FileStream disposal in a separate try-catch block
            // because we want to close the file even if the operation fails.
            try
            {
                FileStream fileStream = ((Tuple<FileStream, FileMode>)storageAsyncResult.OperationState).Item1;
                filePath = fileStream.Name;
                fileStream.Dispose();
            }
            catch (Exception e)
            {
                exception = e;
            }

            if (tryFileDelete)
            {
                try
                {
                    FileMode mode = ((Tuple<FileStream, FileMode>)storageAsyncResult.OperationState).Item2;
                    if (mode == FileMode.Create || mode == FileMode.CreateNew)
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception)
                {
                    // Best effort to clean up in the event that download was unsuccessful.
                    // Do not throw as we want to throw original exception.
                }
            }

            storageAsyncResult.OnComplete(exception);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a blob to a file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDownloadToFile(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> res = (StorageAsyncResult<NullType>)asyncResult;
            res.End();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a file.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode)
        {
            return this.DownloadToFileAsync(path, mode, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a file.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginDownloadToFile, this.EndDownloadToFile, path, mode, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a file.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToFileAsync(path, mode, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a file.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginDownloadToFile, this.EndDownloadToFile, path, mode, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual int DownloadToByteArray(byte[] target, int index, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            return this.DownloadRangeToByteArray(target, index, null /* blobOffset */, null /* length */, accessCondition, options, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToByteArray(byte[] target, int index, AsyncCallback callback, object state)
        {
            return this.BeginDownloadToByteArray(target, index, null /* accessCondition */, null /* options */, null /*operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadToByteArray(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginDownloadRangeToByteArray(target, index, null /* blobOffset */, null /* length */, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a blob to a byte array.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public virtual int EndDownloadToByteArray(IAsyncResult asyncResult)
        {
            return this.EndDownloadRangeToByteArray(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>int</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index)
        {
            return this.DownloadToByteArrayAsync(target, index, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>int</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDownloadToByteArray, this.EndDownloadToByteArray, target, index, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>int</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToByteArrayAsync(target, index, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>int</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDownloadToByteArray, this.EndDownloadToByteArray, target, index, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void DownloadRangeToStream(Stream target, long? offset, long? length, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("target", target);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            Executor.ExecuteSync(
                this.GetBlobImpl(this.attributes, target, offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to download a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadRangeToStream(Stream target, long? offset, long? length, AsyncCallback callback, object state)
        {
            return this.BeginDownloadRangeToStream(target, offset, length, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadRangeToStream(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("target", target);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.GetBlobImpl(this.attributes, target, offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDownloadRangeToStream(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to download a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginDownloadRangeToStream, this.EndDownloadRangeToStream, target, offset, length, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginDownloadRangeToStream, this.EndDownloadRangeToStream, target, offset, length, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual int DownloadRangeToByteArray(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            using (SyncMemoryStream stream = new SyncMemoryStream(target, index))
            {
                this.DownloadRangeToStream(stream, blobOffset, length, accessCondition, options, operationContext);
                return (int)stream.Position;
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to download a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadRangeToByteArray(byte[] target, int index, long? blobOffset, long? length, AsyncCallback callback, object state)
        {
            return this.BeginDownloadRangeToByteArray(target, index, blobOffset, length, null /* accesCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadRangeToByteArray(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            SyncMemoryStream stream = new SyncMemoryStream(target, index);
            StorageAsyncResult<int> storageAsyncResult = new StorageAsyncResult<int>(callback, state) { OperationState = stream };

            ICancellableAsyncResult result = this.BeginDownloadRangeToStream(
                stream,
                blobOffset,
                length,
                accessCondition,
                options,
                operationContext,
                this.DownloadRangeToByteArrayCallback,
                storageAsyncResult);

            storageAsyncResult.CancelDelegate = result.Cancel;
            return storageAsyncResult;
        }

        /// <summary>
        /// Called when the asynchronous DownloadRangeToStream operation completes.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        private void DownloadRangeToByteArrayCallback(IAsyncResult asyncResult)
        {
            StorageAsyncResult<int> storageAsyncResult = (StorageAsyncResult<int>)asyncResult.AsyncState;

            try
            {
                this.EndDownloadRangeToStream(asyncResult);

                SyncMemoryStream stream = (SyncMemoryStream)storageAsyncResult.OperationState;
                storageAsyncResult.Result = (int)stream.Position;
                storageAsyncResult.OnComplete();
            }
            catch (Exception e)
            {
                storageAsyncResult.OnComplete(e);
            }
        }

        /// <summary>
        /// Ends an asynchronous operation to download a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public virtual int EndDownloadRangeToByteArray(IAsyncResult asyncResult)
        {
            StorageAsyncResult<int> res = (StorageAsyncResult<int>)asyncResult;
            res.End();
            return res.Result;
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to download a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>int</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, blobOffset, length, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>int</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDownloadRangeToByteArray, this.EndDownloadRangeToByteArray, target, index, blobOffset, length, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>int</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, blobOffset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>int</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDownloadRangeToByteArray, this.EndDownloadRangeToByteArray, target, index, blobOffset, length, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Checks existence of the blob.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the blob exists.</returns>
        [DoesServiceRequest]
        public virtual bool Exists(BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            return this.Exists(false, options, operationContext);
        }

        /// <summary>
        /// Checks existence of the blob.
        /// </summary>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the blob exists.</returns>
        private bool Exists(bool primaryOnly, BlobRequestOptions options, OperationContext operationContext)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteSync(
                this.ExistsImpl(this.attributes, modifiedOptions, primaryOnly),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous request to check existence of the blob.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExists(AsyncCallback callback, object state)
        {
            return this.BeginExists(null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to check existence of the blob.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExists(BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginExists(false, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to check existence of the blob.
        /// </summary>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        private ICancellableAsyncResult BeginExists(bool primaryOnly, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.ExistsImpl(this.attributes, modifiedOptions, primaryOnly),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Returns the asynchronous result of the request to check existence of the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the blob exists.</returns>
        public virtual bool EndExists(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<bool>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to check existence of the blob.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
        {
            return this.ExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to check existence of the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExists, this.EndExists, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to check existence of the blob.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to check existence of the blob.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExists, this.EndExists, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Populates a blob's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void FetchAttributes(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            Executor.ExecuteSync(
                this.FetchAttributesImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to populate the blob's properties and metadata.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginFetchAttributes(AsyncCallback callback, object state)
        {
            return this.BeginFetchAttributes(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to populate the blob's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginFetchAttributes(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.FetchAttributesImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to populate the blob's properties and metadata.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndFetchAttributes(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to populate the blob's properties and metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync()
        {
            return this.FetchAttributesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to populate the blob's properties and metadata.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginFetchAttributes, this.EndFetchAttributes, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to populate the blob's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to populate the blob's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginFetchAttributes, this.EndFetchAttributes, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Updates the blob's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void SetMetadata(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            Executor.ExecuteSync(
                this.SetMetadataImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to update the blob's metadata.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetMetadata(AsyncCallback callback, object state)
        {
            return this.BeginSetMetadata(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to update the blob's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetMetadata(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.SetMetadataImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to update the blob's metadata.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetMetadata(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to update the blob's metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync()
        {
            return this.SetMetadataAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the blob's metadata.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetMetadata, this.EndSetMetadata, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the blob's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the blob's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetMetadata, this.EndSetMetadata, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Updates the blob's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void SetProperties(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            Executor.ExecuteSync(
                this.SetPropertiesImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to update the blob's properties.
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
        /// Begins an asynchronous operation to update the blob's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetProperties(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.SetPropertiesImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to update the blob's properties.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetProperties(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to update the blob's properties.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync()
        {
            return this.SetPropertiesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the blob's properties.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetProperties, this.EndSetProperties, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the blob's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.SetPropertiesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the blob's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginSetProperties, this.EndSetProperties, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Deletes the blob.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void Delete(DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            Executor.ExecuteSync(
                this.DeleteBlobImpl(this.attributes, deleteSnapshotsOption, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to delete the blob.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(AsyncCallback callback, object state)
        {
            return this.BeginDelete(DeleteSnapshotsOption.None, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to delete the blob.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.DeleteBlobImpl(this.attributes, deleteSnapshotsOption, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to delete the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDelete(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to delete the blob.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
        {
            return this.DeleteAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginDelete, this.EndDelete, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(deleteSnapshotsOption, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginDelete, this.EndDelete, deleteSnapshotsOption, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Deletes the blob if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the blob did already exist and was deleted; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual bool DeleteIfExists(DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            bool exists = this.Exists(true, modifiedOptions, operationContext);
            if (!exists)
            {
                return false;
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
                        (e.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobNotFound))
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
        /// Begins an asynchronous request to delete the blob if it already exists.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(AsyncCallback callback, object state)
        {
            return this.BeginDeleteIfExists(DeleteSnapshotsOption.None, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous request to delete the blob if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            StorageAsyncResult<bool> storageAsyncResult = new StorageAsyncResult<bool>(callback, state)
            {
                RequestOptions = modifiedOptions,
                OperationContext = operationContext,
            };

            this.DeleteIfExistsHandler(deleteSnapshotsOption, accessCondition, modifiedOptions, operationContext, storageAsyncResult);
            return storageAsyncResult;
        }

        private void DeleteIfExistsHandler(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, StorageAsyncResult<bool> storageAsyncResult)
        {
            ICancellableAsyncResult savedExistsResult = this.BeginExists(
                true,
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

                            ICancellableAsyncResult savedDeleteResult = this.BeginDelete(
                                deleteSnapshotsOption,
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
                                                (e.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobNotFound))
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
                                null /* state */);

                            storageAsyncResult.CancelDelegate = savedDeleteResult.Cancel;
                            if (storageAsyncResult.CancelRequested)
                            {
                                storageAsyncResult.Cancel();
                            }
                        }
                        catch (Exception e)
                        {
                            storageAsyncResult.OnComplete(e);
                        }
                    }
                },
                null /* state */);

            // We do not need to do this inside a lock, as storageAsyncResult is
            // not returned to the user yet.
            storageAsyncResult.CancelDelegate = savedExistsResult.Cancel;
        }

        /// <summary>
        /// Returns the result of an asynchronous request to delete the blob if it already exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the blob did already exist and was deleted; otherwise, <c>false</c>.</returns>
        public virtual bool EndDeleteIfExists(IAsyncResult asyncResult)
        {
            StorageAsyncResult<bool> res = (StorageAsyncResult<bool>)asyncResult;
            res.End();
            return res.Result;
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to delete the blob if it already exists.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
        {
            return this.DeleteIfExistsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob if it already exists.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDeleteIfExists, this.EndDeleteIfExists, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(deleteSnapshotsOption, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDeleteIfExists, this.EndDeleteIfExists, deleteSnapshotsOption, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Acquires a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The ID of the acquired lease.</returns>
        [DoesServiceRequest]
        public virtual string AcquireLease(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteSync(
                this.AcquireLeaseImpl(this.attributes, leaseTime, proposedLeaseId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="callback">An optional callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAcquireLease(TimeSpan? leaseTime, string proposedLeaseId, AsyncCallback callback, object state)
        {
            return this.BeginAcquireLease(leaseTime, proposedLeaseId, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An optional callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAcquireLease(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.AcquireLeaseImpl(this.attributes, leaseTime, proposedLeaseId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that references the pending asynchronous operation.</param>
        /// <returns>The ID of the acquired lease.</returns>
        public virtual string EndAcquireLease(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<string>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId = null)
        {
            return this.AcquireLeaseAsync(leaseTime, proposedLeaseId, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginAcquireLease, this.EndAcquireLease, leaseTime, proposedLeaseId, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AcquireLeaseAsync(leaseTime, proposedLeaseId, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginAcquireLease, this.EndAcquireLease, leaseTime, proposedLeaseId, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Renews a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.  If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void RenewLease(AccessCondition accessCondition, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            Executor.ExecuteSync(
                this.RenewLeaseImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to renew a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="callback">An optional callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginRenewLease(AccessCondition accessCondition, AsyncCallback callback, object state)
        {
            return this.BeginRenewLease(accessCondition, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to renew a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An optional callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginRenewLease(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.RenewLeaseImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to renew a lease on this blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndRenewLease(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to renew a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task RenewLeaseAsync(AccessCondition accessCondition)
        {
            return this.RenewLeaseAsync(accessCondition, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to renew a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task RenewLeaseAsync(AccessCondition accessCondition, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginRenewLease, this.EndRenewLease, accessCondition, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to renew a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.RenewLeaseAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to renew a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginRenewLease, this.EndRenewLease, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Changes the lease ID on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease. This cannot be null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The new lease ID.</returns>
        [DoesServiceRequest]
        public virtual string ChangeLease(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteSync(
                this.ChangeLeaseImpl(this.attributes, proposedLeaseId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to change the lease on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease. This cannot be null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="callback">An optional callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginChangeLease(string proposedLeaseId, AccessCondition accessCondition, AsyncCallback callback, object state)
        {
            return this.BeginChangeLease(proposedLeaseId, accessCondition, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to change the lease on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease. This cannot be null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An optional callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginChangeLease(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.ChangeLeaseImpl(this.attributes, proposedLeaseId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to change the lease on this blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The new lease ID.</returns>
        public virtual string EndChangeLease(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<string>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to change the lease on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease. This cannot be null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition)
        {
            return this.ChangeLeaseAsync(proposedLeaseId, accessCondition, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to change the lease on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease. This cannot be null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginChangeLease, this.EndChangeLease, proposedLeaseId, accessCondition, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to change the lease on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease. This cannot be null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ChangeLeaseAsync(proposedLeaseId, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to change the lease on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease. This cannot be null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginChangeLease, this.EndChangeLease, proposedLeaseId, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Releases the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void ReleaseLease(AccessCondition accessCondition, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            Executor.ExecuteSync(
                this.ReleaseLeaseImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to release the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="callback">An optional callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginReleaseLease(AccessCondition accessCondition, AsyncCallback callback, object state)
        {
            return this.BeginReleaseLease(accessCondition, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to release the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An optional callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginReleaseLease(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.ReleaseLeaseImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to release the lease on this blob.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that references the pending asynchronous operation.</param>
        public virtual void EndReleaseLease(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to release the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task ReleaseLeaseAsync(AccessCondition accessCondition)
        {
            return this.ReleaseLeaseAsync(accessCondition, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to release the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task ReleaseLeaseAsync(AccessCondition accessCondition, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginReleaseLease, this.EndReleaseLease, accessCondition, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to release the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ReleaseLeaseAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to release the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginReleaseLease, this.EndReleaseLease, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Breaks the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="System.TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds. If <c>null</c>, the break period is the remainder of the current lease,
        /// or zero for infinite leases.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.TimeSpan"/> representing the amount of time before the lease ends, to the second.</returns>
        [DoesServiceRequest]
        public virtual TimeSpan BreakLease(TimeSpan? breakPeriod = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteSync(
                this.BreakLeaseImpl(this.attributes, breakPeriod, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to break the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="System.TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds. If <c>null</c>, the break period is the remainder of the current lease,
        /// or zero for infinite leases.</param>
        /// <param name="callback">An optional callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginBreakLease(TimeSpan? breakPeriod, AsyncCallback callback, object state)
        {
            return this.BeginBreakLease(breakPeriod, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to break the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="System.TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds. If <c>null</c>, the break period is the remainder of the current lease,
        /// or zero for infinite leases.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An optional callback delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginBreakLease(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.BreakLeaseImpl(this.attributes, breakPeriod, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to break the current lease on this blob.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="System.TimeSpan"/> representing the amount of time before the lease ends, to the second.</returns>
        public virtual TimeSpan EndBreakLease(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<TimeSpan>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to break the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="System.TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds. If <c>null</c>, the break period is the remainder of the current lease,
        /// or zero for infinite leases.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="System.TimeSpan"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod)
        {
            return this.BreakLeaseAsync(breakPeriod, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to break the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="System.TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds. If <c>null</c>, the break period is the remainder of the current lease,
        /// or zero for infinite leases.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="System.TimeSpan"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginBreakLease, this.EndBreakLease, breakPeriod, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to break the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="System.TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds. If <c>null</c>, the break period is the remainder of the current lease,
        /// or zero for infinite leases.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="System.TimeSpan"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.BreakLeaseAsync(breakPeriod, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to break the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="System.TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds. If <c>null</c>, the break period is the remainder of the current lease,
        /// or zero for infinite leases.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="System.TimeSpan"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginBreakLease, this.EndBreakLease, breakPeriod, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Begins an operation to start copying another blob's contents, properties, and metadata to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last-modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual string StartCopy(Uri source, AccessCondition sourceAccessCondition = null, AccessCondition destAccessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("source", source);
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteSync(
                this.StartCopyImpl(this.attributes, source, sourceAccessCondition, destAccessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to start copying another blob's contents, properties, and metadata to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(Uri source, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(source, null /* sourceAccessCondition */, null /* destAccessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying another blob's contents, properties, and metadata to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("source", source);
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.StartCopyImpl(this.attributes, source, sourceAccessCondition, destAccessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to start copying another blob's contents, properties, and metadata to this blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A string containing the copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last-modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        public virtual string EndStartCopy(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<string>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to start copying another blob's contents, properties, and metadata
        /// to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source)
        {
            return this.StartCopyAsync(source, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another blob's contents, properties, and metadata
        /// to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginStartCopy, this.EndStartCopy, source, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another blob's contents, properties, and metadata
        /// to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another blob's contents, properties, and metadata
        /// to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginStartCopy, this.EndStartCopy, source, sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }

#endif

#if SYNC
        /// <summary>
        /// Aborts an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void AbortCopy(string copyId, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            Executor.ExecuteSync(
                this.AbortCopyImpl(this.attributes, copyId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to abort an ongoing blob copy operation.
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
        /// Begins an asynchronous operation to abort an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAbortCopy(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.AbortCopyImpl(this.attributes, copyId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to abort an ongoing blob copy operation.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndAbortCopy(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to abort an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId)
        {
            return this.AbortCopyAsync(copyId, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to abort an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAbortCopy, this.EndAbortCopy, copyId, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to abort an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AbortCopyAsync(copyId, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to abort an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAbortCopy, this.EndAbortCopy, copyId, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="CloudBlob"/> object that is a blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual CloudBlob Snapshot(IDictionary<string, string> metadata = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteSync(
                this.SnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSnapshot(AsyncCallback callback, object state)
        {
            return this.BeginSnapshot(null /* metadata */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSnapshot(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.SnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="CloudBlob"/> object that is a blob snapshot.</returns>
        public virtual CloudBlob EndSnapshot(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<CloudBlob>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlob> SnapshotAsync()
        {
            return this.SnapshotAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlob> SnapshotAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginSnapshot, this.EndSnapshot, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlob> SnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.SnapshotAsync(metadata, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlob> SnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginSnapshot, this.EndSnapshot, metadata, accessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Implements getting the stream without specifying a range.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="destStream">The destination stream.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that gets the stream.
        /// </returns>
        private RESTCommand<NullType> GetBlobImpl(BlobAttributes blobAttributes, Stream destStream, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options)
        {
            string lockedETag = null;
            AccessCondition lockedAccessCondition = null;

            bool isRangeGet = offset.HasValue;

            // Adjust the range if the encryption policy is set and it is a range download. For now, we can do it this way.
            // Once we have multiple versions/algorithms, we will not be able to do this adjustment deterministically without fetching 
            // properties from the service. Since it is an extra call, we will add it once we support multiple versions.
            int discardFirst = 0;
            long? endOffset = null;
            bool bufferIV = false;
            long? userSpecifiedLength = length;

            options.AssertPolicyIfRequired();

            if (isRangeGet && options.EncryptionPolicy != null)
            {
#if WINDOWS_PHONE
                // Windows phone does not allow setting padding mode to none. Uses PKCS7 by default. So we cannot download closed ranges that do not
                // cover the last block of data. However, since we cannot know the length unless we do a fetch attributes, we will just throw 
                // for now if length is specified. Open ranges like x - are still allowed.
                if (length.HasValue)
                {
                    throw new InvalidOperationException(SR.RangeDownloadNotPermittedOnPhone);
                }
#endif
                // Let's say the user requests a download with offset = 39 and length = 54

                // First calculate the endOffset if length has value.
                // endOffset starts at 92 (39 + 54 - 1), but then gets increased to 95 (one less than the next higher multiple of 16)
                if (length.HasValue)
                {
                    endOffset = offset.Value + length.Value - 1;

                    // AES-CBC works in 16 byte blocks. So if a user specifies a range whose start and end offsets are not multiples of 16,
                    // update them so we can download entire AES blocks to decrypt.
                    // Adjust the end offset to be a multiple of 16.
                    if ((endOffset.Value + 1) % 16 != 0)
                    {
                        endOffset += (int)(16 - ((endOffset.Value + 1) % 16));
                    }
                }

                // Adjust the end offset to be a multiple of 16.
                // offset gets reduced down to the highest multiple of 16 lower then the current value (32) 
                discardFirst = (int)(offset.Value % 16);
                offset -= discardFirst;

                // We need another 16 bytes for IV if offset is not 0. If the offset is 0, it is the first AES block
                // and the IV is obtained from blob metadata.
                // offset is reduced by another 16 (to a final value of 16)
                if (offset > 15)
                {
                    offset -= 16;
                    bufferIV = true;
                }

                // Adjust the length according to the new start and end offsets.
                // length = 80 (a multiple of 16)
                if (endOffset.HasValue)
                {
                    length = endOffset.Value - offset.Value + 1;
                }
            }

            bool arePropertiesPopulated = false;
            bool decryptStreamCreated = false;
            ICryptoTransform transform = null;
            string storedMD5 = null;

            long startingOffset = offset.HasValue ? offset.Value : 0;
            long? startingLength = length;
            long? validateLength = null;

            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.DestinationStream = destStream;
            getCmd.CalculateMd5ForResponseStream = !options.DisableContentMD5Validation.Value;
            getCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) =>
                BlobHttpWebRequestFactory.Get(uri, serverTimeout, blobAttributes.SnapshotTime, offset, length, options.UseTransactionalMD5.Value, accessCondition, useVersionHeader, ctx);
            getCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
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

                getCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, context) =>
                    BlobHttpWebRequestFactory.Get(uri, serverTimeout, blobAttributes.SnapshotTime, offset, length, options.UseTransactionalMD5.Value && !arePropertiesPopulated, lockedAccessCondition ?? accessCondition, useVersionHeader, context);
            };

            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(offset.HasValue ? HttpStatusCode.PartialContent : HttpStatusCode.OK, resp, NullType.Value, cmd, ex);

                if (!arePropertiesPopulated)
                {
                    CloudBlob.UpdateAfterFetchAttributes(blobAttributes, resp, isRangeGet);
                    storedMD5 = resp.Headers[HttpResponseHeader.ContentMd5];

                    if (options.EncryptionPolicy != null)
                    {
                        cmd.DestinationStream = BlobEncryptionPolicy.WrapUserStreamWithDecryptStream(this, cmd.DestinationStream, options, blobAttributes, isRangeGet, out transform, endOffset, userSpecifiedLength, discardFirst, bufferIV);
                        decryptStreamCreated = true;
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

                    // If the download fails and Get Blob needs to resume the download, going to the
                    // same storage location is important to prevent a possible ETag mismatch.
                    getCmd.CommandLocationMode = cmd.CurrentResult.TargetLocation == StorageLocation.Primary ? CommandLocationMode.PrimaryOnly : CommandLocationMode.SecondaryOnly;
                    lockedETag = blobAttributes.Properties.ETag;
                    if (resp.ContentLength >= 0)
                    {
                        validateLength = resp.ContentLength;
                    }

                    arePropertiesPopulated = true;
                }

                return NullType.Value;
            };

            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                HttpResponseParsers.ValidateResponseStreamMd5AndLength(validateLength, storedMD5, cmd);
                return NullType.Value;
            };

            getCmd.DisposeAction = (cmd) =>
            {
                // Crypto stream should be closed in order for it to flush the final block of data to the underlying stream and to clear internal buffers. 
                // It is ok to do this here because we have ensured that we don't end up closing the user provided stream in the process of closing the 
                // cryptostream by wrapping the user provided stream within the NonCloseableStream.
                if (decryptStreamCreated)
                {
                    try
                    {
                        // This only throws a NotSupportedException if the current stream is not writable (which should never be true in our case).
                        // But the try/catch is for safe exit if something unexpected happens and we get an exception 
                        // when Close is invoked.
                        cmd.DestinationStream.Close();

                        // Dispose the ICryptoTransform object created for decryption if required. For the range download case, BlobDecryptStream will
                        // dispose the transform function. For full blob downloads, we will dispose it here.
                        if (transform != null)
                        {
                            transform.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new StorageException(
                            cmd.CurrentResult,
                            SR.CryptoError,
                            ex)
                        {
                            IsRetryable = false
                        };
                    }
                }
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the FetchAttributes method. The attributes are updated immediately.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that fetches the attributes.
        /// </returns>
        private RESTCommand<NullType> FetchAttributesImpl(BlobAttributes blobAttributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.GetProperties(uri, serverTimeout, blobAttributes.SnapshotTime, accessCondition, useVersionHeader, ctx);
            getCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateAfterFetchAttributes(blobAttributes, resp, false);
                return NullType.Value;
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the Exists method.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that checks existence.
        /// </returns>
        private RESTCommand<bool> ExistsImpl(BlobAttributes blobAttributes, BlobRequestOptions options, bool primaryOnly)
        {
            RESTCommand<bool> getCmd = new RESTCommand<bool>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = primaryOnly ? CommandLocationMode.PrimaryOnly : CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.GetProperties(uri, serverTimeout, blobAttributes.SnapshotTime, null /* accessCondition */, useVersionHeader, ctx);
            getCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, true, cmd, ex);
                CloudBlob.UpdateAfterFetchAttributes(blobAttributes, resp, false);
                return true;
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the SetMetadata method.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that sets the metadata.
        /// </returns>
        private RESTCommand<NullType> SetMetadataImpl(BlobAttributes blobAttributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.SetMetadata(uri, serverTimeout, accessCondition, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) => BlobHttpWebRequestFactory.AddMetadata(r, blobAttributes.Metadata);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(blobAttributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the SetProperties method.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that sets the properties.
        /// </returns>
        private RESTCommand<NullType> SetPropertiesImpl(BlobAttributes blobAttributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.SetProperties(uri, serverTimeout, blobAttributes.Properties, accessCondition, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) => BlobHttpWebRequestFactory.AddMetadata(r, blobAttributes.Metadata);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(blobAttributes, resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implements the DeleteBlob method.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that deletes the blob.
        /// </returns>
        private RESTCommand<NullType> DeleteBlobImpl(BlobAttributes blobAttributes, DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> deleteCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(deleteCmd);
            deleteCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.Delete(uri, serverTimeout, blobAttributes.SnapshotTime, deleteSnapshotsOption, accessCondition, useVersionHeader, ctx);
            deleteCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            deleteCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex);

            return deleteCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand{T}"/> for acquiring a lease.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If null, an infinite lease will be acquired. If not null, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> implementing the acquire lease operation.
        /// </returns>
        private RESTCommand<string> AcquireLeaseImpl(BlobAttributes blobAttributes, TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options)
        {
            int leaseDuration = -1;
            if (leaseTime.HasValue)
            {
                CommonUtility.AssertInBounds("leaseTime", leaseTime.Value, TimeSpan.FromSeconds(1), TimeSpan.MaxValue);
                leaseDuration = (int)leaseTime.Value.TotalSeconds;
            }

            RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.Lease(uri, serverTimeout, LeaseAction.Acquire, proposedLeaseId, leaseDuration, null /* leaseBreakPeriod */, accessCondition, useVersionHeader, ctx);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, null, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(blobAttributes, resp, false);
                return BlobHttpResponseParsers.GetLeaseId(resp);
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand{T}"/> for renewing a lease.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> implementing the renew lease operation.
        /// </returns>
        /// <exception cref="System.ArgumentException">accessCondition</exception>
        private RESTCommand<NullType> RenewLeaseImpl(BlobAttributes blobAttributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("accessCondition", accessCondition);
            if (accessCondition.LeaseId == null)
            {
                throw new ArgumentException(SR.MissingLeaseIDRenewing, "accessCondition");
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.Lease(uri, serverTimeout, LeaseAction.Renew, null /* proposedLeaseId */, null /* leaseDuration */, null /* leaseBreakPeriod */, accessCondition, useVersionHeader, ctx);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(blobAttributes, resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand{T}"/> for changing a lease ID.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="proposedLeaseId">The proposed new lease ID.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> implementing the change lease ID operation.
        /// </returns>
        /// <exception cref="System.ArgumentException">accessCondition</exception>
        private RESTCommand<string> ChangeLeaseImpl(BlobAttributes blobAttributes, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("accessCondition", accessCondition);
            CommonUtility.AssertNotNull("proposedLeaseId", proposedLeaseId);
            if (accessCondition.LeaseId == null)
            {
                throw new ArgumentException(SR.MissingLeaseIDChanging, "accessCondition");
            }

            RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.Lease(uri, serverTimeout, LeaseAction.Change, proposedLeaseId, null /* leaseDuration */, null /* leaseBreakPeriod */, accessCondition, useVersionHeader, ctx);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(blobAttributes, resp, false);
                return BlobHttpResponseParsers.GetLeaseId(resp);
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand{T}"/> for releasing a lease.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> implementing the release lease operation.
        /// </returns>
        /// <exception cref="System.ArgumentException">accessCondition</exception>
        private RESTCommand<NullType> ReleaseLeaseImpl(BlobAttributes blobAttributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("accessCondition", accessCondition);
            if (accessCondition.LeaseId == null)
            {
                throw new ArgumentException(SR.MissingLeaseIDReleasing, "accessCondition");
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.Lease(uri, serverTimeout, LeaseAction.Release, null /* proposedLeaseId */, null /* leaseDuration */, null /* leaseBreakPeriod */, accessCondition, useVersionHeader, ctx);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(blobAttributes, resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand{T}"/> for breaking a lease.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="breakPeriod">The amount of time to allow the lease to remain, rounded down to seconds.
        /// If null, the break period is the remainder of the current lease, or zero for infinite leases.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> implementing the break lease operation.
        /// </returns>
        private RESTCommand<TimeSpan> BreakLeaseImpl(BlobAttributes blobAttributes, TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options)
        {
            int? breakSeconds = null;
            if (breakPeriod.HasValue)
            {
                CommonUtility.AssertInBounds("breakPeriod", breakPeriod.Value, TimeSpan.Zero, TimeSpan.MaxValue);
                breakSeconds = (int)breakPeriod.Value.TotalSeconds;
            }

            RESTCommand<TimeSpan> putCmd = new RESTCommand<TimeSpan>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.Lease(uri, serverTimeout, LeaseAction.Break, null /* proposedLeaseId */, null /* leaseDuration */, breakSeconds, accessCondition, useVersionHeader, ctx);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, TimeSpan.Zero, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(blobAttributes, resp, false);

                int? remainingLeaseTime = BlobHttpResponseParsers.GetRemainingLeaseTime(resp);
                if (!remainingLeaseTime.HasValue)
                {
                    // Unexpected result from service.
                    throw new StorageException(cmd.CurrentResult, SR.LeaseTimeNotReceived, null /* inner */);
                }

                return TimeSpan.FromSeconds(remainingLeaseTime.Value);
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation of the StartCopy method. Result is a BlobAttributes object derived from the response headers.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="source">The URI of the source blob.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source object. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that starts to copy.
        /// </returns>
        /// <exception cref="System.ArgumentException">sourceAccessCondition</exception>
        private RESTCommand<string> StartCopyImpl(BlobAttributes blobAttributes, Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options)
        {
            if (sourceAccessCondition != null && !string.IsNullOrEmpty(sourceAccessCondition.LeaseId))
            {
                throw new ArgumentException(SR.LeaseConditionOnSource, "sourceAccessCondition");
            }

            RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.CopyFrom(uri, serverTimeout, source, sourceAccessCondition, destAccessCondition, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) => BlobHttpWebRequestFactory.AddMetadata(r, blobAttributes.Metadata);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, null /* retVal */, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(blobAttributes, resp, false);
                CopyState state = BlobHttpResponseParsers.GetCopyAttributes(resp);
                blobAttributes.CopyState = state;
                return state.CopyId;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation of the AbortCopy method. No result is produced.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="copyId">The copy ID of the copy operation to abort.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that aborts the copy.
        /// </returns>
        private RESTCommand<NullType> AbortCopyImpl(BlobAttributes blobAttributes, string copyId, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("copyId", copyId);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.AbortCopy(uri, serverTimeout, copyId, accessCondition, useVersionHeader, ctx);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Snapshot method.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot, or <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that creates the snapshot.</returns>
        /// <remarks>If the <c>metadata</c> parameter is <c>null</c> then no metadata is associated with the request.</remarks>
        private RESTCommand<CloudBlob> SnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<CloudBlob> putCmd = new RESTCommand<CloudBlob>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.Snapshot(uri, serverTimeout, accessCondition, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) =>
            {
                if (metadata != null)
                {
                    BlobHttpWebRequestFactory.AddMetadata(r, metadata);
                }
            };
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, null /* retVal */, cmd, ex);
                DateTimeOffset snapshotTime = NavigationHelper.ParseSnapshotTime(BlobHttpResponseParsers.GetSnapshotTime(resp));

                CloudBlob snapshot = new CloudBlob(this.Name, snapshotTime, this.Container);

                snapshot.attributes.Metadata = new Dictionary<string, string>(metadata ?? this.Metadata);
                snapshot.attributes.Properties = new BlobProperties(this.Properties);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(snapshot.attributes, resp, false);
                return snapshot;
            };

            return putCmd;
        }

        /// <summary>
        /// Called when the asynchronous operation to commit the blob started by UploadFromStream finishes.
        /// </summary>
        /// <param name="result">The result of the asynchronous operation.</param>
        internal static void BlobOutputStreamCommitCallback(IAsyncResult result)
        {
            StorageAsyncResult<NullType> storageAsyncResult = (StorageAsyncResult<NullType>)result.AsyncState;
            CloudBlobStream blobStream = (CloudBlobStream)storageAsyncResult.OperationState;
            storageAsyncResult.UpdateCompletedSynchronously(result.CompletedSynchronously);

            try
            {
                blobStream.EndCommit(result);
                blobStream.Dispose();
                storageAsyncResult.OnComplete();
            }
            catch (Exception e)
            {
                storageAsyncResult.OnComplete(e);
            }
        }

        /// <summary>
        /// Updates this blob with the given attributes at the end of a fetch attributes operation.
        /// </summary>
        /// <param name="blobAttributes">The new attributes.</param>
        /// <param name="response">The response.</param>
        /// <param name="ignoreMD5">if set to <c>true</c>, blob's MD5 will not be updated.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
        internal static void UpdateAfterFetchAttributes(BlobAttributes blobAttributes, HttpWebResponse response, bool ignoreMD5)
        {
            BlobProperties properties = BlobHttpResponseParsers.GetProperties(response);

            // If BlobType is specified and the value returned from cloud is different, 
            // then it's a client error and we need to throw.
            if (blobAttributes.Properties.BlobType != BlobType.Unspecified && blobAttributes.Properties.BlobType != properties.BlobType)
            {
                throw new InvalidOperationException(SR.BlobTypeMismatch);
            }

            if (ignoreMD5)
            {
                properties.ContentMD5 = blobAttributes.Properties.ContentMD5;
            }

            blobAttributes.Properties = properties;
            blobAttributes.Metadata = BlobHttpResponseParsers.GetMetadata(response);
            blobAttributes.CopyState = BlobHttpResponseParsers.GetCopyAttributes(response);
        }

        /// <summary>
        /// Retrieve ETag, LMT, Length and Sequence-Number from response.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="response">The response to parse.</param>
        /// <param name="updateLength">If set to <c>true</c>, update the blob length.</param>
        internal static void UpdateETagLMTLengthAndSequenceNumber(BlobAttributes blobAttributes, HttpWebResponse response, bool updateLength)
        {
            BlobProperties parsedProperties = BlobHttpResponseParsers.GetProperties(response);
            blobAttributes.Properties.ETag = parsedProperties.ETag ?? blobAttributes.Properties.ETag;
            blobAttributes.Properties.LastModified = parsedProperties.LastModified ?? blobAttributes.Properties.LastModified;
            blobAttributes.Properties.PageBlobSequenceNumber = parsedProperties.PageBlobSequenceNumber ?? blobAttributes.Properties.PageBlobSequenceNumber;
            blobAttributes.Properties.AppendBlobCommittedBlockCount = parsedProperties.AppendBlobCommittedBlockCount ?? blobAttributes.Properties.AppendBlobCommittedBlockCount;

            if (updateLength)
            {
                blobAttributes.Properties.Length = parsedProperties.Length;
            }
        }

        /// <summary>
        /// Converts the source blob of a copy operation to an appropriate access URI, taking Shared Access Signature credentials into account.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <returns>A URI addressing the source blob, using SAS if appropriate.</returns>
        internal static Uri SourceBlobToUri(CloudBlob source)
        {
            CommonUtility.AssertNotNull("source", source);
            return source.ServiceClient.Credentials.TransformUri(source.SnapshotQualifiedUri);
        }

        /// <summary>
        /// Parses the server request encrypted response header.
        /// </summary>
        /// <param name="response">Response to be parsed.</param>
        /// <returns><c>true</c> if write content was encrypted by service or <c>false</c> if not.</returns>
        internal static bool ParseServerRequestEncrypted(HttpWebResponse response)
        {
            string requestEncrypted = response.Headers[Constants.HeaderConstants.ServerRequestEncrypted];
            return string.Equals(requestEncrypted, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);
        }
    }
}