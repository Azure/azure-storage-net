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
    using System.Net.Http;
    using System.Net.Http;
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.OpenReadAsync(accessCondition, options, operationContext, token), callback, state);
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
            return ((CancellableAsyncResultTaskWrapper<Stream>)asyncResult).GetAwaiter().GetResult();
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
            return this.OpenReadAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
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
            return this.OpenReadAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
        public virtual async Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            await this.FetchAttributesAsync(accessCondition, options, operationContext, cancellationToken).ConfigureAwait(false);
            AccessCondition streamAccessCondition = AccessCondition.CloneConditionWithETag(accessCondition, this.Properties.ETag);
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient, false);
            return new BlobReadStream(this, streamAccessCondition, modifiedOptions, operationContext);
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
        /// Begins an asynchronous operation to download the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        internal ICancellableAsyncResult BeginDownloadToStream(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadToStreamAsync(target, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a blob to a stream.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDownloadToStream(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.DownloadRangeToStreamAsync(target, null /*null*/, null /*length*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), AggregatingProgressIncrementer.None, CancellationToken.None);
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
            return this.DownloadRangeToStreamAsync(target, null /*null*/, null /*length*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), AggregatingProgressIncrementer.None, cancellationToken);
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
            return this.DownloadRangeToStreamAsync(target, null /*null*/, null /*length*/, accessCondition, options, operationContext, AggregatingProgressIncrementer.None, CancellationToken.None);
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
            return this.DownloadRangeToStreamAsync(target, null /*null*/, null /*length*/, accessCondition, options, operationContext, AggregatingProgressIncrementer.None, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">A <see cref="System.IO.Stream"/> object representing the target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToStreamAsync(target, null /*null*/, null /*length*/, accessCondition, options, operationContext, progressHandler, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Rotates the encryption key on this blob.
        /// This method rotates only the KEK, not the CEK.  For more information, visit https://azure.microsoft.com/en-us/documentation/articles/storage-client-side-encryption/
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. 
        /// For this operation, there must not be an <see cref="AccessCondition.IfMatchETag"/>, <see cref="AccessCondition.IfNoneMatchETag"/>, 
        /// <see cref="AccessCondition.IfModifiedSinceTime"/>, or <see cref="AccessCondition.IfNotModifiedSinceTime"/> condition.  
        /// An <see cref="AccessCondition.IfMatchETag"/> condition will be added internally.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// This method has a number of prerequisites:
        /// 1. The blob must be encrypted on the service using client-side encryption (not service-side encryption.)
        /// 2. The local object must have the latest attributes from the blob on the service.  This can be done by calling FetchAttributes() on the blob, or by listing blobs in the container with metadata.
        /// 3. The Encryption Policy on the default BlobRequestOptions must contain an IKeyResolver capable of resolving the old encryption key.
        /// 4. The Encryption Policy on the default BlobRequestOptions must contain an IKey with the new encryption key.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void RotateEncryptionKey(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);

            WrappedKeyData wrappedKey = CommonUtility.RunWithoutSynchronizationContext(() => this.RotateEncryptionHelper(accessCondition, modifiedOptions, CancellationToken.None).GetAwaiter().GetResult());
            BlobEncryptionData encryptionData = wrappedKey.encryptionData;

            // Update the encryption metadata with the newly wrapped CEK and call SetMetadata.
            encryptionData.WrappedContentKey = new WrappedKey(modifiedOptions.EncryptionPolicy.Key.Kid, wrappedKey.encryptedKey, wrappedKey.algorithm);

            this.Metadata[Constants.EncryptionConstants.BlobEncryptionData] = Newtonsoft.Json.JsonConvert.SerializeObject(encryptionData, Newtonsoft.Json.Formatting.None);

            if (accessCondition == null)
            {
                accessCondition = new AccessCondition();
            }

            accessCondition.IfMatchETag = this.Properties.ETag;
            try
            {
                this.SetMetadata(accessCondition, modifiedOptions, operationContext);
            }
            // This bit is to improve the error handling case.
            // If the set-metadata fails, we check if it failed with a Precondition Failure.  If so, we wrap the failure with a 
            // new StorageException that contains a better error message.
            // This is important because the library is internally adding an AccessCondition, which may be difficult for users to realize / debug.
            catch (StorageException e)
            {
                // Check to see if our new If-Match condition failed, and if so, give a more helpful exception message.
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                {
                    StorageException wrapperException = new StorageException(e.RequestInformation, SR.KeyRotationPreconditionFailed, e);
                    throw wrapperException;
                }
                else
                {
                    throw;
                }
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to rotate the encryption key on this blob.
        /// This method rotates only the KEK, not the CEK.  For more information, visit https://azure.microsoft.com/en-us/documentation/articles/storage-client-side-encryption/
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// This method has a number of prerequisites:
        /// 1. The blob must be encrypted on the service using client-side encryption (not service-side encryption.)
        /// 2. The local object must have the latest attributes from the blob on the service.  This can be done by calling FetchAttributes() on the blob, or by listing blobs in the container with metadata.
        /// 3. The Encryption Policy on the default BlobRequestOptions must contain an IKeyResolver capable of resolving the old encryption key.
        /// 4. The Encryption Policy on the default BlobRequestOptions must contain an IKey with the new encryption key.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginRotateEncryptionKey(AsyncCallback callback, object state)
        {
            return this.BeginRotateEncryptionKey(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to rotate the encryption key on this blob.
        /// This method rotates only the KEK, not the CEK.  For more information, visit https://azure.microsoft.com/en-us/documentation/articles/storage-client-side-encryption/
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. 
        /// For this operation, there must not be an <see cref="AccessCondition.IfMatchETag"/>, <see cref="AccessCondition.IfNoneMatchETag"/>, 
        /// <see cref="AccessCondition.IfModifiedSinceTime"/>, or <see cref="AccessCondition.IfNotModifiedSinceTime"/> condition.  
        /// An <see cref="AccessCondition.IfMatchETag"/> condition will be added internally.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// This method has a number of prerequisites:
        /// 1. The blob must be encrypted on the service using client-side encryption (not service-side encryption.)
        /// 2. The local object must have the latest attributes from the blob on the service.  This can be done by calling FetchAttributes() on the blob, or by listing blobs in the container with metadata.
        /// 3. The Encryption Policy on the default BlobRequestOptions must contain an IKeyResolver capable of resolving the old encryption key.
        /// 4. The Encryption Policy on the default BlobRequestOptions must contain an IKey with the new encryption key.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginRotateEncryptionKey(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => RotateEncryptionKeyAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to rotate the encryption key on this blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndRotateEncryptionKey(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK

        /// <summary>
        /// Initiates an asynchronous operation to rotate the encryption key on this blob.
        /// This method rotates only the KEK, not the CEK.  For more information, visit https://azure.microsoft.com/en-us/documentation/articles/storage-client-side-encryption/
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns> 
        /// <remarks>
        /// This method has a number of prerequisites:
        /// 1. The blob must be encrypted on the service using client-side encryption (not service-side encryption.)
        /// 2. The local object must have the latest attributes from the blob on the service.  This can be done by calling FetchAttributes() on the blob, or by listing blobs in the container with metadata.
        /// 3. The Encryption Policy on the default BlobRequestOptions must contain an IKeyResolver capable of resolving the old encryption key.
        /// 4. The Encryption Policy on the default BlobRequestOptions must contain an IKey with the new encryption key.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task RotateEncryptionKeyAsync()
        {
            return this.RotateEncryptionKeyAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to rotate the encryption key on this blob.
        /// This method rotates only the KEK, not the CEK.  For more information, visit https://azure.microsoft.com/en-us/documentation/articles/storage-client-side-encryption/
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns> 
        /// <remarks>
        /// This method has a number of prerequisites:
        /// 1. The blob must be encrypted on the service using client-side encryption (not service-side encryption.)
        /// 2. The local object must have the latest attributes from the blob on the service.  This can be done by calling FetchAttributes() on the blob, or by listing blobs in the container with metadata.
        /// 3. The Encryption Policy on the default BlobRequestOptions must contain an IKeyResolver capable of resolving the old encryption key.
        /// 4. The Encryption Policy on the default BlobRequestOptions must contain an IKey with the new encryption key.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task RotateEncryptionKeyAsync(CancellationToken cancellationToken)
        {
            return this.RotateEncryptionKeyAsync(null /* accessCondition */, null /* options */, null /* operationContext */, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to rotate the encryption key on this blob.
        /// This method rotates only the KEK, not the CEK.  For more information, visit https://azure.microsoft.com/en-us/documentation/articles/storage-client-side-encryption/
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. 
        /// For this operation, there must not be an <see cref="AccessCondition.IfMatchETag"/>, <see cref="AccessCondition.IfNoneMatchETag"/>, 
        /// <see cref="AccessCondition.IfModifiedSinceTime"/>, or <see cref="AccessCondition.IfNotModifiedSinceTime"/> condition.  
        /// An <see cref="AccessCondition.IfMatchETag"/> condition will be added internally.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns> 
        /// <remarks>
        /// This method has a number of prerequisites:
        /// 1. The blob must be encrypted on the service using client-side encryption (not service-side encryption.)
        /// 2. The local object must have the latest attributes from the blob on the service.  This can be done by calling FetchAttributes() on the blob, or by listing blobs in the container with metadata.
        /// 3. The Encryption Policy on the default BlobRequestOptions must contain an IKeyResolver capable of resolving the old encryption key.
        /// 4. The Encryption Policy on the default BlobRequestOptions must contain an IKey with the new encryption key.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task RotateEncryptionKeyAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.RotateEncryptionKeyAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to rotate the encryption key on this blob.
        /// This method rotates only the KEK, not the CEK.  For more information, visit https://azure.microsoft.com/en-us/documentation/articles/storage-client-side-encryption/
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. 
        /// For this operation, there must not be an <see cref="AccessCondition.IfMatchETag"/>, <see cref="AccessCondition.IfNoneMatchETag"/>, 
        /// <see cref="AccessCondition.IfModifiedSinceTime"/>, or <see cref="AccessCondition.IfNotModifiedSinceTime"/> condition.  
        /// An <see cref="AccessCondition.IfMatchETag"/> condition will be added internally.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns> 
        /// <remarks>
        /// This method has a number of prerequisites:
        /// 1. The blob must be encrypted on the service using client-side encryption (not service-side encryption.)
        /// 2. The local object must have the latest attributes from the blob on the service.  This can be done by calling FetchAttributes() on the blob, or by listing blobs in the container with metadata.
        /// 3. The Encryption Policy on the default BlobRequestOptions must contain an IKeyResolver capable of resolving the old encryption key.
        /// 4. The Encryption Policy on the default BlobRequestOptions must contain an IKey with the new encryption key.
        /// </remarks>
        [DoesServiceRequest]
        public virtual async Task RotateEncryptionKeyAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);

            WrappedKeyData wrappedKey = await this.RotateEncryptionHelper(accessCondition, modifiedOptions, cancellationToken).ConfigureAwait(false);

            BlobEncryptionData encryptionData = wrappedKey.encryptionData;

            encryptionData.WrappedContentKey = new WrappedKey(modifiedOptions.EncryptionPolicy.Key.Kid, wrappedKey.encryptedKey, wrappedKey.algorithm);

            this.Metadata[Constants.EncryptionConstants.BlobEncryptionData] = Newtonsoft.Json.JsonConvert.SerializeObject(encryptionData, Newtonsoft.Json.Formatting.None);

            if (accessCondition == null)
            {
                accessCondition = new AccessCondition();
            }

            accessCondition.IfMatchETag = this.Properties.ETag;

            try
            {
                await this.SetMetadataAsync(accessCondition, modifiedOptions, operationContext, cancellationToken).ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                // This bit is to improve the error handling case.
                // If the set-metadata fails, we check if it failed with a Precondition Failure.  If so, we wrap the failure with a 
                // new StorageException that contains a better error message.
                // This is important because the library is internally adding an AccessCondition, which may be difficult for users to realize / debug.
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                {
                    throw new StorageException(ex.RequestInformation, SR.KeyRotationPreconditionFailed, ex);
                }
            }
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
            return this.BeginDownloadToFile(path, mode, accessCondition, options, operationContext, null /*progressHandler*/, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the contents of a blob to a file.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadToFile(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadToFileAsync(path, mode, accessCondition, options, operationContext, token), callback, state);
        }        

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a blob to a file.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDownloadToFile(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.DownloadToFileAsync(path, mode, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, CancellationToken.None);
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
            return this.DownloadToFileAsync(path, mode, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, cancellationToken);
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
            return this.DownloadToFileAsync(path, mode, accessCondition, options, operationContext, null /*progressHandler*/, CancellationToken.None);
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
            return this.DownloadToFileAsync(path, mode, accessCondition, options, operationContext, null /*progressHandler*/, cancellationToken);
        }
        /// <summary>
        /// Downloads the contents of a blob to a file.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual async Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);
            FileStream fileStream = new FileStream(path, mode, FileAccess.Write, FileShare.None);

            try
            {
                using (fileStream)
                {
                    await this.DownloadToStreamAsync(fileStream, accessCondition, options, operationContext, progressHandler, cancellationToken).ConfigureAwait(false);
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

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a file by making parallel requests.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="parallelIOCount">The maximum number of ranges that can be downloaded concurrently</param>
        /// <param name="rangeSizeInBytes">The size of each individual range in bytes that is being dowloaded in parallel.
        /// The range size must be a multiple of 4 KB and a minimum of 4 MB. If no value is passed a default value of 16 MB is used or 4MB if transactional MD5 is enabled.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// The parallelIOCount and rangeSizeInBytes should be adjusted depending on the CPU, memory, and bandwidth.
        /// This API should only be used for larger downloads as a HEAD request is made prior to downloading the data.
        /// For smaller blobs, please use DownloadToFileAsync().
        /// To get the best performance, it is recommended to try several values, and measure throughput.
        /// One place to start would be to set the parallelIOCount to the number of CPUs.
        /// Then adjust the rangeSizeInBytes so that parallelIOCount times rangeSizeInBytes equals the amount of memory you want the process to consume.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task DownloadToFileParallelAsync(string path, FileMode mode, int parallelIOCount, long? rangeSizeInBytes)
        {
            return this.DownloadToFileParallelAsync(path, mode, parallelIOCount, rangeSizeInBytes, 0, null, null, null, null, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a file by making parallel requests.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="parallelIOCount">The maximum number of ranges that can be downloaded concurrently.</param>
        /// <param name="rangeSizeInBytes">The size of each individual range in bytes that is being dowloaded in parallel.
        /// The range size must be a multiple of 4 KB and a minimum of 4 MB. If no value is passed a default value of 16 MB is used or 4MB if transactional MD5 is enabled.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// The parallelIOCount and rangeSizeInBytes should be adjusted depending on the CPU, memory, and bandwidth.
        /// This API should only be used for larger downloads as a HEAD request is made prior to downloading the data.
        /// For smaller blobs, please use DownloadToFileAsync().
        /// To get the best performance, it is recommended to try several values, and measure throughput.
        /// One place to start would be to set the parallelIOCount to the number of CPUs.
        /// Then adjust the rangeSizeInBytes so that parallelIOCount times rangeSizeInBytes equals the amount of memory you want the process to consume.
        /// 
        /// ## Examples
        /// [!code-csharp[DownloadToFileParallel](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobDownloadToFileParallelTests.cs#sample_DownloadToFileParallel "DownloadToFileParallel Sample")]
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task DownloadToFileParallelAsync(string path, FileMode mode, int parallelIOCount, long? rangeSizeInBytes, CancellationToken cancellationToken)
        {
            return this.DownloadToFileParallelAsync(path, mode, parallelIOCount, rangeSizeInBytes, 0, null, null, null, null, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a file by making parallel requests.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="parallelIOCount">The maximum number of ranges that can be downloaded concurrently</param>
        /// <param name="rangeSizeInBytes">The size of each individual range in bytes that is being dowloaded in parallel.
        /// The range size must be a multiple of 4 KB and a minimum of 4 MB. If no value is passed a default value of 16 MB or 4MB if transactional MD5 is enabled.</param>
        /// <param name="offset">The offset of the blob.</param>
        /// <param name="length">The number of bytes to download.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// The parallelIOCount and rangeSizeInBytes should be adjusted depending on the CPU, memory, and bandwidth.
        /// This API should only be used for larger downloads as a HEAD request is made prior to downloading the data.
        /// For smaller blobs, please use DownloadToFileAsync().
        /// To get the best performance, it is recommended to try several values, and measure throughput.
        /// One place to start would be to set the parallelIOCount to the number of CPUs.
        /// Then adjust the rangeSizeInBytes so that parallelIOCount times rangeSizeInBytes equals the amount of memory you want the process to consume.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task DownloadToFileParallelAsync(string path, FileMode mode, int parallelIOCount, long? rangeSizeInBytes, long offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            ParallelDownloadToFile pdf = ParallelDownloadToFile.Start(
                this,
                path,
                mode,
                parallelIOCount,
                rangeSizeInBytes,
                offset,
                length,
                Constants.MaxIdleTimeMs,
                accessCondition,
                options,
                operationContext,
                cancellationToken);
            return pdf.Task;
        }
#endif

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
        /// Begins an asynchronous operation to download the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadToByteArray(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadToByteArrayAsync(target, index, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the contents of a blob to a byte array.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public virtual int EndDownloadToByteArray(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<int>)asyncResult).GetAwaiter().GetResult();
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
            return this.DownloadRangeToByteArrayAsync(target, index, null /*blobOffset*/, null /*length*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, CancellationToken.None);
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
            return this.DownloadRangeToByteArrayAsync(target, index, null /*blobOffset*/, null /*length*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, cancellationToken);
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
            return this.DownloadRangeToByteArrayAsync(target, index, null /*blobOffset*/, null /*length*/, accessCondition, options, operationContext, null /*progressHandler*/, CancellationToken.None);
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
            return this.DownloadRangeToByteArrayAsync(target, index, null /*blobOffset*/, null /*length*/, accessCondition, options, operationContext, null /*progressHandler*/, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>int</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, null /*blobOffset*/, null /*length*/, accessCondition, options, operationContext, progressHandler, cancellationToken);
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
            return this.BeginDownloadRangeToStream(target, offset, length, accessCondition, options, operationContext, null /*progressHandler*/, callback, state);
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
        /// <param name="progressHandler"> An <see cref="IProgress"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadRangeToStream(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDownloadRangeToStream(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.DownloadRangeToStreamAsync(target, offset, length, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), AggregatingProgressIncrementer.None, CancellationToken.None);
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
            return this.DownloadRangeToStreamAsync(target, offset, length, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), AggregatingProgressIncrementer.None, cancellationToken);
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
            return this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, AggregatingProgressIncrementer.None, CancellationToken.None);
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
            return this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, AggregatingProgressIncrementer.None, cancellationToken);
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
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, new AggregatingProgressIncrementer(progressHandler), cancellationToken);
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
        /// <param name="progressIncrementer"> A <see cref="AggregatingProgressIncrementer"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        private Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AggregatingProgressIncrementer progressIncrementer, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("target", target);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.GetBlobImpl(this.attributes, progressIncrementer.CreateProgressIncrementingStream(target), offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return this.BeginDownloadRangeToByteArray(target, index, blobOffset, length, accessCondition, options, operationContext, null /*progressHandler*/, callback, state);
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
        /// <param name="progressHandler"> An <see cref="IProgress"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadRangeToByteArray(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadRangeToByteArrayAsync(target, index, blobOffset, length, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public virtual int EndDownloadRangeToByteArray(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<int>)asyncResult).GetAwaiter().GetResult();
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
            return this.DownloadRangeToByteArrayAsync(target, index, blobOffset, length, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, CancellationToken.None);
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
            return this.DownloadRangeToByteArrayAsync(target, index, blobOffset, length, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, cancellationToken);
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
            return this.DownloadRangeToByteArrayAsync(target, index, blobOffset, length, accessCondition, options, operationContext, null /*progressHandler*/, CancellationToken.None);
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
            return this.DownloadRangeToByteArrayAsync(target, index, blobOffset, length, accessCondition, options, operationContext, null /*progressHandler*/, cancellationToken);
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
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>int</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            using (SyncMemoryStream stream = new SyncMemoryStream(target, index))
            {
                await this.DownloadRangeToStreamAsync(stream, blobOffset, length, accessCondition, options, operationContext, progressHandler, cancellationToken).ConfigureAwait(false);
                return (int)stream.Position;
            }
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.ExistsAsync(primaryOnly, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the asynchronous result of the request to check existence of the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the blob exists.</returns>
        public virtual bool EndExists(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<bool>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to check existence of the blob.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
        {
            return this.ExistsAsync(false/*primaryOnly*/, default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to check existence of the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            return this.ExistsAsync(false/*primaryOnly*/, default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            return this.ExistsAsync(false/*primaryOnly*/, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to check existence of the blob.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync( BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.ExistsAsync(false/*primaryOnly*/, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to check existence of the blob.
        /// </summary>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(bool primaryOnly, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.ExistsImpl(this.attributes, modifiedOptions, primaryOnly),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.FetchAttributesAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to populate the blob's properties and metadata.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndFetchAttributes(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to populate the blob's properties and metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync()
        {
            return this.FetchAttributesAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to populate the blob's properties and metadata.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(CancellationToken cancellationToken)
        {
            return this.FetchAttributesAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.FetchAttributesImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.SetMetadataAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to update the blob's metadata.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetMetadata(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to update the blob's metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync()
        {
            return this.SetMetadataAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the blob's metadata.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(CancellationToken cancellationToken)
        {
            return this.SetMetadataAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.SetMetadataImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.SetPropertiesAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to update the blob's properties.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetProperties(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to update the blob's properties.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync()
        {
            return this.SetPropertiesAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to update the blob's properties.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(CancellationToken cancellationToken)
        {
            return this.SetPropertiesAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.SetPropertiesImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Deletes the blob.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
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
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDelete(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DeleteAsync(deleteSnapshotsOption, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to delete the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndDelete(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to delete the blob.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
        {
            return this.DeleteAsync(DeleteSnapshotsOption.None, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(CancellationToken cancellationToken)
        {
            return this.DeleteAsync(DeleteSnapshotsOption.None, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
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
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.DeleteBlobImpl(this.attributes, deleteSnapshotsOption, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

#endif

#if SYNC
        /// <summary>
        /// Deletes the blob if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the blob did already exist and was deleted; otherwise <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual bool DeleteIfExists(DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

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
                        (e.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobNotFound) ||
                        (e.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.ContainerNotFound))
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
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDeleteIfExists(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DeleteIfExistsAsync(deleteSnapshotsOption, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Returns the result of an asynchronous request to delete the blob if it already exists.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns><c>true</c> if the blob did already exist and was deleted; otherwise, <c>false</c>.</returns>
        public virtual bool EndDeleteIfExists(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<bool>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to delete the blob if it already exists.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
        {
            return this.DeleteIfExistsAsync(DeleteSnapshotsOption.None, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob if it already exists.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(CancellationToken cancellationToken)
        {
            return this.DeleteIfExistsAsync(DeleteSnapshotsOption.None, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
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
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

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
                        (e.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobNotFound) ||
                        (e.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.ContainerNotFound))
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
        /// UnDeletes the blob if it is soft-deleted.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void Undelete(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            Executor.ExecuteSync(
                this.UndeleteBlobImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to undelete the soft-deleted blob.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUndelete(AsyncCallback callback, object state)
        {
            return this.BeginUndelete(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }


        /// <summary>
        /// Begins an asynchronous operation to undelete the soft-deleted blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUndelete(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UndeleteAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to undelete the soft-deleted blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUndelete(IAsyncResult asyncResult)
        {
           ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }


#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to undelete the soft-deleted blob.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UndeleteAsync()
        {
            return this.UndeleteAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to delete the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UndeleteAsync(CancellationToken cancellationToken)
        {
            return this.UndeleteAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to undelete the soft-deleted blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UndeleteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UndeleteAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to undelete the soft-deleted blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UndeleteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.UndeleteBlobImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

#endif

#if SYNC
        /// <summary>
        /// Acquires a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// 15 to 60 seconds.</param>
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
        /// 15 to 60 seconds.</param>
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
        /// 15 to 60 seconds.</param>
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
            return CancellableAsyncResultTaskWrapper.Create(token => AcquireLeaseAsync(leaseTime, proposedLeaseId, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that references the pending asynchronous operation.</param>
        /// <returns>The ID of the acquired lease.</returns>
        public virtual string EndAcquireLease(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<string>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// 15 to 60 seconds.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId = null)
        {
            return this.AcquireLeaseAsync(leaseTime, proposedLeaseId, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// 15 to 60 seconds.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, CancellationToken cancellationToken)
        {
            return this.AcquireLeaseAsync(leaseTime, proposedLeaseId, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to acquire a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        /// 15 to 60 seconds.</param>
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
        /// 15 to 60 seconds.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.AcquireLeaseImpl(this.attributes, leaseTime, proposedLeaseId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.RenewLeaseAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to renew a lease on this blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndRenewLease(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.RenewLeaseAsync(accessCondition, default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
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
            return this.RenewLeaseAsync(accessCondition, default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.RenewLeaseImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.ChangeLeaseAsync(proposedLeaseId, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to change the lease on this blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The new lease ID.</returns>
        public virtual string EndChangeLease(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<string>)asyncResult).GetAwaiter().GetResult();
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
            return this.ChangeLeaseAsync(proposedLeaseId, accessCondition, default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
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
            return this.ChangeLeaseAsync(proposedLeaseId, accessCondition, default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.ChangeLeaseImpl(this.attributes, proposedLeaseId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return CancellableAsyncResultTaskWrapper.Create(token => ReleaseLeaseAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to release the lease on this blob.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that references the pending asynchronous operation.</param>
        public virtual void EndReleaseLease(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.ReleaseLeaseAsync(accessCondition, default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
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
            return this.ReleaseLeaseAsync(accessCondition, default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.ReleaseLeaseImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.BreakLeaseAsync(breakPeriod, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to break the current lease on this blob.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="System.TimeSpan"/> representing the amount of time before the lease ends, to the second.</returns>
        public virtual TimeSpan EndBreakLease(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<TimeSpan>)asyncResult).GetAwaiter().GetResult();
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
            return this.BreakLeaseAsync(breakPeriod, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
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
            return this.BreakLeaseAsync(breakPeriod, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            {
                BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
                return Executor.ExecuteAsync(
                    this.BreakLeaseImpl(this.attributes, breakPeriod, accessCondition, modifiedOptions),
                    modifiedOptions.RetryPolicy,
                    operationContext,
                    cancellationToken);
            }
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
            return this.StartCopy(source, null /* premiumPageBlobTier */, sourceAccessCondition, destAccessCondition, options, operationContext);
        }

        /// <summary>
        /// Begins an operation to start copying another blob's contents, properties, and metadata to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="premiumPageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
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
        internal virtual string StartCopy(Uri source, PremiumPageBlobTier? premiumPageBlobTier, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopy(source, default(string) /* contentMD5 */, false /* syncCopy */, premiumPageBlobTier, sourceAccessCondition, destAccessCondition, options, operationContext);
        }

        /// <summary>
        /// Begins an operation to start copying another blob's contents, properties, and metadata to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the operation. May be <c>null</c> or an empty string.</param>
        /// <param name="syncCopy">A boolean to enable synchronous server copy of blobs.</param>
        /// <param name="premiumPageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
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
        internal virtual string StartCopy(Uri source, string contentMD5, bool syncCopy, PremiumPageBlobTier? premiumPageBlobTier, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("source", source);
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteSync(
                this.StartCopyImpl(this.attributes, source, contentMD5, false /* incrementalCopy */, syncCopy, premiumPageBlobTier, sourceAccessCondition, destAccessCondition, modifiedOptions),
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
            return this.BeginStartCopy(source, null /* premiumPageBlobTier */, null /* sourceAccessCondition */, null /* destAccessCondition */, null /* options */, null /* operationContext */, callback, state);
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
            return this.BeginStartCopy(source, null /* premiumPageBlobTier */, sourceAccessCondition, destAccessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying another blob's contents, properties, and metadata to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="premiumPageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        internal virtual ICancellableAsyncResult BeginStartCopy(Uri source, PremiumPageBlobTier? premiumPageBlobTier, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(source, default(string) /* contentMD5 */, false /* incrementalCopy */, false /* syncCopy */, premiumPageBlobTier, sourceAccessCondition, destAccessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying another blob's contents, properties, and metadata to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the operation. May be <c>null</c> or an empty string.</param>
        /// <param name="syncCopy">A boolean to enable synchronous server copy of blobs.</param>
        /// <param name="premiumPageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        internal virtual ICancellableAsyncResult BeginStartCopy(Uri source, string contentMD5, bool incrementalCopy, bool syncCopy, PremiumPageBlobTier? premiumPageBlobTier, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.StartCopyAsync(source, contentMD5, incrementalCopy, syncCopy, premiumPageBlobTier, sourceAccessCondition, destAccessCondition, options, operationContext, token), callback, state);

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
            return ((CancellableAsyncResultTaskWrapper<string>)asyncResult).GetAwaiter().GetResult();
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
            return this.StartCopyAsync(source, null /*premiumPageBlboTier*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            return this.StartCopyAsync(source, null /*premiumPageBlobTier*/, sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another blob's contents, properties, and metadata
        /// to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="premiumPageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, PremiumPageBlobTier? premiumPageBlobTier, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(source, default(string) /* contentMD5 */, false /* incrementalCopy */, false /* syncCopy */, premiumPageBlobTier, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another blob's contents, properties, and metadata
        /// to this blob.
        /// </summary>
        /// <param name="source">The <see cref="System.Uri"/> of the source blob.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the operation. May be <c>null</c> or an empty string.</param>
        /// <param name="syncCopy">A boolean to enable synchronous server copy of blobs.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        private Task<string> StartCopyAsync(Uri source, string contentMD5, bool incrementalCopy, bool syncCopy, PremiumPageBlobTier? premiumPageBlobTier, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("source", source);
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.StartCopyImpl(this.attributes, source, contentMD5, false /* incrementalCopy */, syncCopy, premiumPageBlobTier, sourceAccessCondition, destAccessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.AbortCopyAsync(copyId, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to abort an ongoing blob copy operation.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndAbortCopy(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.AbortCopyAsync(copyId, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
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
            return this.AbortCopyAsync(copyId, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.AbortCopyImpl(this.attributes, copyId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.SnapshotAsync(metadata, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="CloudBlob"/> object that is a blob snapshot.</returns>
        public virtual CloudBlob EndSnapshot(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<CloudBlob>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlob> SnapshotAsync()
        {
            return this.SnapshotAsync(null /*metadata*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlob> SnapshotAsync(CancellationToken cancellationToken)
        {
            return this.SnapshotAsync(null /*metadata*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.SnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Get(uri, serverTimeout, blobAttributes.SnapshotTime, offset, length, options.UseTransactionalMD5.Value, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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

                getCmd.BuildRequest = (command, uri, builder, cnt, serverTimeout, context) => BlobHttpRequestMessageFactory.Get(uri, serverTimeout, blobAttributes.SnapshotTime, offset, length, options.UseTransactionalMD5.Value && !arePropertiesPopulated, lockedAccessCondition ?? accessCondition, cnt, context, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            };

            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(offset.HasValue ? HttpStatusCode.PartialContent : HttpStatusCode.OK, resp, NullType.Value, cmd, ex);

                if (!arePropertiesPopulated)
                {
                    CloudBlob.UpdateAfterFetchAttributes(blobAttributes, resp);
                    storedMD5 = HttpResponseParsers.GetContentMD5(resp);

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
                    validateLength = resp.Content.Headers.ContentLength;

                    arePropertiesPopulated = true;
                }

                return NullType.Value;
            };

            getCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) =>
            {
                HttpResponseParsers.ValidateResponseStreamMd5AndLength(validateLength, storedMD5, cmd);
                return NullType.ValueTask;
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
                        if (cmd.DestinationStream != null)
                        {
                            cmd.DestinationStream.Close();
                        }

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
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetProperties(uri, serverTimeout, blobAttributes.SnapshotTime, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateAfterFetchAttributes(blobAttributes, resp);
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
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetProperties(uri, serverTimeout, blobAttributes.SnapshotTime, null /* accessCondition */, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, true, cmd, ex);
                CloudBlob.UpdateAfterFetchAttributes(blobAttributes, resp);
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
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.SetMetadata(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                BlobHttpRequestMessageFactory.AddMetadata(msg, blobAttributes.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(blobAttributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
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
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.SetProperties(uri, serverTimeout, blobAttributes.Properties, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                BlobHttpRequestMessageFactory.AddMetadata(msg, blobAttributes.Metadata);
                return msg;
            };
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
        /// <param name="attributes">The attributes.</param>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that deletes the blob.
        /// </returns>
        private RESTCommand<NullType> DeleteBlobImpl(BlobAttributes attributes, DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> deleteCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(deleteCmd);
            deleteCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Delete(uri, serverTimeout, attributes.SnapshotTime, deleteSnapshotsOption, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            deleteCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex);

            return deleteCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand{T}"/> for acquiring a lease.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="leaseTime">A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If null, an infinite lease will be acquired. If not null, this must be
        /// 15 to 60 seconds.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> implementing the acquire lease operation.
        /// </returns>
        private RESTCommand<string> AcquireLeaseImpl(BlobAttributes attributes, TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options)
        {
            int leaseDuration = -1;
            if (leaseTime.HasValue)
            {
                CommonUtility.AssertInBounds("leaseTime", leaseTime.Value, TimeSpan.FromSeconds(Constants.MinimumLeaseDuration), TimeSpan.FromSeconds(Constants.MaximumLeaseDuration));
                leaseDuration = (int)leaseTime.Value.TotalSeconds;
            }

            RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Lease(uri, serverTimeout, LeaseAction.Acquire, proposedLeaseId, leaseDuration, null /* leaseBreakPeriod */, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, null /* retVal */, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                return BlobHttpResponseParsers.GetLeaseId(resp);
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand{T}"/> for renewing a lease.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> implementing the renew lease operation.
        /// </returns>
        /// <exception cref="System.ArgumentException">accessCondition</exception>
        private RESTCommand<NullType> RenewLeaseImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("accessCondition", accessCondition);
            if (accessCondition.LeaseId == null)
            {
                throw new ArgumentException(SR.MissingLeaseIDRenewing, "accessCondition");
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Lease(uri, serverTimeout, LeaseAction.Renew, null /* proposedLeaseId */, null /* leaseDuration */, null /* leaseBreakPeriod */, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand{T}"/> for changing a lease ID.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="proposedLeaseId">The proposed new lease ID.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> implementing the change lease ID operation.
        /// </returns>
        /// <exception cref="System.ArgumentException">accessCondition</exception>
        private RESTCommand<string> ChangeLeaseImpl(BlobAttributes attributes, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("accessCondition", accessCondition);
            CommonUtility.AssertNotNull("proposedLeaseId", proposedLeaseId);
            if (accessCondition.LeaseId == null)
            {
                throw new ArgumentException(SR.MissingLeaseIDChanging, "accessCondition");
            }

            RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Lease(uri, serverTimeout, LeaseAction.Change, proposedLeaseId, null /* leaseDuration */, null /* leaseBreakPeriod */, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                return BlobHttpResponseParsers.GetLeaseId(resp);
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand{T}"/> for releasing a lease.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> implementing the release lease operation.
        /// </returns>
        /// <exception cref="System.ArgumentException">accessCondition</exception>
        private RESTCommand<NullType> ReleaseLeaseImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("accessCondition", accessCondition);
            if (accessCondition.LeaseId == null)
            {
                throw new ArgumentException(SR.MissingLeaseIDReleasing, "accessCondition");
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Lease(uri, serverTimeout, LeaseAction.Release, null /* proposedLeaseId */, null /* leaseDuration */, null /* leaseBreakPeriod */, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand{T}"/> for breaking a lease.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="breakPeriod">The amount of time to allow the lease to remain, rounded down to seconds.
        /// If null, the break period is the remainder of the current lease, or zero for infinite leases.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> implementing the break lease operation.
        /// </returns>
        private RESTCommand<TimeSpan> BreakLeaseImpl(BlobAttributes attributes, TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options)
        {
            int? breakSeconds = null;
            if (breakPeriod.HasValue)
            {
                CommonUtility.AssertInBounds("breakPeriod", breakPeriod.Value, TimeSpan.FromSeconds(Constants.MinimumBreakLeasePeriod), TimeSpan.FromSeconds(Constants.MaximumBreakLeasePeriod));
                breakSeconds = (int)breakPeriod.Value.TotalSeconds;
            }

            RESTCommand<TimeSpan> putCmd = new RESTCommand<TimeSpan>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Lease(uri, serverTimeout, LeaseAction.Break, null /* proposedLeaseId */, null /* leaseDuration */, breakSeconds, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, TimeSpan.Zero, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);

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
    /// <param name="sourceContentMD5">An optional hash value used to ensure transactional integrity for the operation. May be <c>null</c> or an empty string.</param>
    /// <param name="incrementalCopy">A boolean indicating whether or not this is an incremental copy</param>
    /// <param name="syncCopy">A boolean to enable synchronous server copy of blobs.</param>
    /// <param name="premiumPageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
    /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source object. If <c>null</c>, no condition is used.</param>
    /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
    /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
    /// <returns>
    /// A <see cref="RESTCommand{T}"/> that starts to copy.
    /// </returns>
    /// <exception cref="System.ArgumentException">sourceAccessCondition</exception>
    internal RESTCommand<string> StartCopyImpl(BlobAttributes attributes, Uri source, string sourceContentMD5, bool incrementalCopy, bool syncCopy, PremiumPageBlobTier? premiumPageBlobTier, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options)
    {
        if (sourceAccessCondition != null && !string.IsNullOrEmpty(sourceAccessCondition.LeaseId))
        {
            throw new ArgumentException(SR.LeaseConditionOnSource, "sourceAccessCondition");
        }

        RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, attributes.StorageUri);

        options.ApplyToStorageCommand(putCmd);
        putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
        {
            StorageRequestMessage msg = BlobHttpRequestMessageFactory.CopyFrom(uri, serverTimeout, source, sourceContentMD5, incrementalCopy, syncCopy, premiumPageBlobTier, sourceAccessCondition, destAccessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            BlobHttpRequestMessageFactory.AddMetadata(msg, attributes.Metadata);
            return msg;
        };
        putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
        {
            HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, null /* retVal */, cmd, ex);
            CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
            CopyState state = BlobHttpResponseParsers.GetCopyAttributes(resp);
            attributes.CopyState = state;
            this.attributes.Properties.PremiumPageBlobTier = premiumPageBlobTier;
            if (premiumPageBlobTier.HasValue)
            {
                this.attributes.Properties.BlobTierInferred = false;
            }

            return state.CopyId;
        };

        return putCmd;
    }

    /// <summary>
    /// Implementation of the AbortCopy method. No result is produced.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    /// <param name="copyId">The copy ID of the copy operation to abort.</param>
    /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
    /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
    /// <returns>
    /// A <see cref="RESTCommand{T}"/> that aborts the copy.
    /// </returns>
    private RESTCommand<NullType> AbortCopyImpl(BlobAttributes attributes, string copyId, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("copyId", copyId);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.AbortCopy(uri, serverTimeout, copyId, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.Snapshot(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                BlobHttpRequestMessageFactory.AddMetadata(msg, metadata);

                return msg;
            };

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
        /// Implements the UndeleteBlob method.
        /// </summary>
        /// <param name="blobAttributes">The attributes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>
        /// A <see cref="RESTCommand{T}"/> that deletes the blob.
        /// </returns>
        private RESTCommand<NullType> UndeleteBlobImpl(BlobAttributes blobAttributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> unDeleteCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, blobAttributes.StorageUri);

            options.ApplyToStorageCommand(unDeleteCmd);
            unDeleteCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Undelete(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            unDeleteCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);

            return unDeleteCmd;
        }

        /// <summary>
        /// Validates that the AccessCondition and the RequestOptions passed into a KeyRotation operation are correct.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. 
        /// For this operation, there must not be an <see cref="AccessCondition.IfMatchETag"/>, <see cref="AccessCondition.IfNoneMatchETag"/>, 
        /// <see cref="AccessCondition.IfModifiedSinceTime"/>, or <see cref="AccessCondition.IfNotModifiedSinceTime"/> condition.  
        /// An <see cref="AccessCondition.IfMatchETag"/> condition will be added internally.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="encryptionMetadataAvailable">If the encryption metadata is available on the local blob.</param>
        private void ValidateKeyRotationArguments(AccessCondition accessCondition, BlobRequestOptions options, bool encryptionMetadataAvailable)
        {
            if (accessCondition != null && accessCondition.IsConditional)
            {
                throw new ArgumentException(SR.KeyRotationInvalidAccessCondition, "accessCondition");
            }
            if (options.EncryptionPolicy == null)
            {
                throw new ArgumentException(SR.KeyRotationNoEncryptionPolicy, "options.EncryptionPolicy");
            }
            if (options.EncryptionPolicy.Key == null)
            {
                throw new ArgumentException(SR.KeyRotationNoEncryptionKey, "options.EncryptionPolicy.Key");
            }
            if (options.EncryptionPolicy.KeyResolver == null)
            {
                throw new ArgumentException(SR.KeyRotationNoEncryptionKeyResolver, "options.EncryptionPolicy.KeyResolver");
            }
            if (this.Properties.ETag == null)
            {
                throw new InvalidOperationException(SR.KeyRotationNoEtag);
            }
            if (!encryptionMetadataAvailable)
            {
                throw new InvalidOperationException(SR.KeyRotationNoEncryptionMetadata);
            }
        }

        private struct WrappedKeyData
        {
            public byte[] encryptedKey;
            public string algorithm;
            public BlobEncryptionData encryptionData;
        }

        /// <summary>
        /// Run the elements of key rotation that are common to both the sync and async cases.
        /// Because KV only offers us async operations, our sync codepath needs to wrap the async version, and we can reuse some of the code.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. 
        /// For this operation, there must not be an <see cref="AccessCondition.IfMatchETag"/>, <see cref="AccessCondition.IfNoneMatchETag"/>, 
        /// <see cref="AccessCondition.IfModifiedSinceTime"/>, or <see cref="AccessCondition.IfNotModifiedSinceTime"/> condition.  
        /// An <see cref="AccessCondition.IfMatchETag"/> condition will be added internally.</param>
        /// <param name="modifiedOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. Must have already been processed with Apply Defaults.</param>
        /// <param name="cancellationToken">The cancellation token to use for the async requests.</param>
        /// <returns>The Task that generates the wrappped key.</returns>
        private async Task<WrappedKeyData> RotateEncryptionHelper(AccessCondition accessCondition, BlobRequestOptions modifiedOptions, CancellationToken cancellationToken)
        {
            // Validate arguments:
            String encryptionDataString;
            bool encryptionMetadataAvailable = this.Metadata.TryGetValue(Constants.EncryptionConstants.BlobEncryptionData, out encryptionDataString);
            this.ValidateKeyRotationArguments(accessCondition, modifiedOptions, encryptionMetadataAvailable);

            // Deserialize the old encryption data and validate:
            BlobEncryptionData encryptionData = Newtonsoft.Json.JsonConvert.DeserializeObject<BlobEncryptionData>(encryptionDataString);
            if (encryptionData.WrappedContentKey.EncryptedKey == null)
            {
                throw new InvalidOperationException(SR.KeyRotationNoKeyID);
            }
            
            // Use the key resolver to resolve the old KEK.
            Azure.KeyVault.Core.IKey oldKey = await modifiedOptions.EncryptionPolicy.KeyResolver.ResolveKeyAsync(encryptionData.WrappedContentKey.KeyId, cancellationToken).ConfigureAwait(false);
            if (oldKey == null)
            {
                throw new ArgumentException(SR.KeyResolverCannotResolveExistingKey);
            }

            // Use the old KEK to unwrap the CEK.
            byte[] unwrappedOldKey = await oldKey.UnwrapKeyAsync(encryptionData.WrappedContentKey.EncryptedKey, encryptionData.WrappedContentKey.Algorithm, cancellationToken).ConfigureAwait(false);

            // Use the new KEK to re-wrap the CEK.
            Tuple<byte[], string> wrappedNewKey = await modifiedOptions.EncryptionPolicy.Key.WrapKeyAsync(unwrappedOldKey, null /* algorithm */, cancellationToken).ConfigureAwait(false);
            return new WrappedKeyData() { encryptedKey = wrappedNewKey.Item1, algorithm = wrappedNewKey.Item2, encryptionData = encryptionData };
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
        /// <exception cref="System.InvalidOperationException"></exception>
        internal static void UpdateAfterFetchAttributes(BlobAttributes blobAttributes, HttpResponseMessage response)
        {
            BlobProperties properties = BlobHttpResponseParsers.GetProperties(response);

            // If BlobType is specified and the value returned from cloud is different, 
            // then it's a client error and we need to throw.
            if (blobAttributes.Properties.BlobType != BlobType.Unspecified && blobAttributes.Properties.BlobType != properties.BlobType)
            {
                throw new InvalidOperationException(SR.BlobTypeMismatch);
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
        internal static void UpdateETagLMTLengthAndSequenceNumber(BlobAttributes blobAttributes, HttpResponseMessage response, bool updateLength)
        {
            BlobProperties parsedProperties = BlobHttpResponseParsers.GetProperties(response);
            blobAttributes.Properties.ETag = parsedProperties.ETag ?? blobAttributes.Properties.ETag;
            blobAttributes.Properties.Created = parsedProperties.Created ?? blobAttributes.Properties.Created;
            blobAttributes.Properties.LastModified = parsedProperties.LastModified ?? blobAttributes.Properties.LastModified;
            blobAttributes.Properties.PageBlobSequenceNumber = parsedProperties.PageBlobSequenceNumber ?? blobAttributes.Properties.PageBlobSequenceNumber;
            blobAttributes.Properties.AppendBlobCommittedBlockCount = parsedProperties.AppendBlobCommittedBlockCount ?? blobAttributes.Properties.AppendBlobCommittedBlockCount;
            blobAttributes.Properties.IsServerEncrypted = parsedProperties.IsServerEncrypted;
            blobAttributes.Properties.IsIncrementalCopy = parsedProperties.IsIncrementalCopy;
            blobAttributes.Properties.DeletedTime = parsedProperties.DeletedTime ??
                                                    blobAttributes.Properties.DeletedTime;
            blobAttributes.Properties.RemainingDaysBeforePermanentDelete =
                parsedProperties.RemainingDaysBeforePermanentDelete ??
                blobAttributes.Properties.RemainingDaysBeforePermanentDelete;

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

        #region GetAccountProperties

        /// <summary>
        /// Begins an asynchronous operation to get properties for the account this blob resides on.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetAccountProperties(AsyncCallback callback, object state)
        {
            return this.BeginGetAccountProperties(null /* requestOptions */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to get properties for the account this blob resides on.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object to be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginGetAccountProperties(BlobRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            requestOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return CancellableAsyncResultTaskWrapper.Create(token => this.GetAccountPropertiesAsync(requestOptions, operationContext), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to get properties for the account this blob resides on.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="AccountProperties"/> object.</returns>
        public virtual AccountProperties EndGetAccountProperties(IAsyncResult asyncResult)
        {
            CommonUtility.AssertNotNull(nameof(asyncResult), asyncResult);
            return ((CancellableAsyncResultTaskWrapper<AccountProperties>)(asyncResult)).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to get properties for the account this blob resides on.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="AccountProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<AccountProperties> GetAccountPropertiesAsync()
        {
            return this.GetAccountPropertiesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get properties for the account this blob resides on.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="AccountProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<AccountProperties> GetAccountPropertiesAsync(CancellationToken cancellationToken)
        {
            return this.GetAccountPropertiesAsync(default(BlobRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get properties for the account this blob resides on.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="AccountProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<AccountProperties> GetAccountPropertiesAsync(BlobRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.GetAccountPropertiesAsync(requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to get properties for the account this blob resides on.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="AccountProperties"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<AccountProperties> GetAccountPropertiesAsync(BlobRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            requestOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            return Executor.ExecuteAsync(
                this.GetAccountPropertiesImpl(requestOptions),
                requestOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Get properties for the account this blob resides on.
        /// </summary>
        /// <param name="requestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An <see cref="AccountProperties"/> object.</returns>
        [DoesServiceRequest]
        public virtual AccountProperties GetAccountProperties(BlobRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            requestOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            return Executor.ExecuteSync(
                this.GetAccountPropertiesImpl(requestOptions),
                requestOptions.RetryPolicy,
                operationContext);
        }
#endif

        private RESTCommand<AccountProperties> GetAccountPropertiesImpl(BlobRequestOptions requestOptions)
        {
            RESTCommand<AccountProperties> retCmd = new RESTCommand<AccountProperties>(this.ServiceClient.Credentials, this.StorageUri);
            retCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            retCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetAccountProperties(uri, builder, serverTimeout, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            retCmd.RetrieveResponseStream = true;
            retCmd.PreProcessResponse =
                (cmd, resp, ex, ctx) =>
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);

            retCmd.PostProcessResponseAsync =
                (cmd, resp, ctx, ct) => Task.FromResult(BlobHttpResponseParsers.ReadAccountProperties(resp));
            requestOptions.ApplyToStorageCommand(retCmd);
            return retCmd;
        }
        #endregion
    }
}
