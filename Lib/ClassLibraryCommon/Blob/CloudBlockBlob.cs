//-----------------------------------------------------------------------
// <copyright file="CloudBlockBlob.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.File;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a blob that is uploaded as a set of blocks.
    /// </summary>
    public partial class CloudBlockBlob : CloudBlob, ICloudBlob
    {
#if SYNC
        /// <summary>
        /// Opens a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="CloudBlobStream"/> to be used for writing to the blob.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributes(AccessCondition, BlobRequestOptions, OperationContext)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual CloudBlobStream OpenWrite(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, this.BlobType, this.ServiceClient, false);

            if ((accessCondition != null) && accessCondition.IsConditional)
            {
                try
                {
                    this.FetchAttributes(accessCondition, options, operationContext);
                }
                catch (StorageException e)
                {
                    if ((e.RequestInformation != null) &&
                        (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound) &&
                        string.IsNullOrEmpty(accessCondition.IfMatchETag))
                    {
                        // If we got a 404 and the condition was not an If-Match,
                        // we should continue with the operation.
                    }
                    else
                    {
                        throw;
                    }
                }
            }

#if !(WINDOWS_RT || NETCORE )
            modifiedOptions.AssertPolicyIfRequired();

            if (modifiedOptions.EncryptionPolicy != null)
            {
                ICryptoTransform transform = modifiedOptions.EncryptionPolicy.CreateAndSetEncryptionContext(this.Metadata, false /* noPadding */);
                return new BlobEncryptedWriteStream(this, accessCondition, modifiedOptions, operationContext, transform);
            }
            else
#endif
            {
                return new BlobWriteStream(this, accessCondition, modifiedOptions, operationContext);
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to open a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.BeginFetchAttributes(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, see <see cref="BeginOpenWrite(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/>.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginOpenWrite(AsyncCallback callback, object state)
        {
            return this.BeginOpenWrite(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to open a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.BeginFetchAttributes(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.</para>
        /// </remarks>
        [DoesServiceRequest]        
        public virtual ICancellableAsyncResult BeginOpenWrite(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            this.attributes.AssertNoSnapshot();

            StorageAsyncResult<CloudBlobStream> storageAsyncResult = new StorageAsyncResult<CloudBlobStream>(callback, state);
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, this.BlobType, this.ServiceClient, false);

            Action<StorageAsyncResult<CloudBlobStream>> prepareStorageAsyncResult = localStorageAsyncResult =>
                {
                    modifiedOptions.AssertPolicyIfRequired();

                    if (modifiedOptions.EncryptionPolicy != null)
                    {
                        ICryptoTransform transform = modifiedOptions.EncryptionPolicy.CreateAndSetEncryptionContext(this.Metadata, false /* noPadding */);
                        localStorageAsyncResult.Result = new BlobEncryptedWriteStream(this, accessCondition, modifiedOptions, operationContext, transform);
                    }
                    else
                    {
                        localStorageAsyncResult.Result = new BlobWriteStream(this, accessCondition, modifiedOptions, operationContext);
                    }

                    localStorageAsyncResult.OnComplete();
                };

            if ((accessCondition != null) && accessCondition.IsConditional)
            {
                ICancellableAsyncResult result = this.BeginFetchAttributes(
                    accessCondition,
                    options,
                    operationContext,
                    ar =>
                    {
                        storageAsyncResult.UpdateCompletedSynchronously(ar.CompletedSynchronously);

                        try
                        {
                            this.EndFetchAttributes(ar);
                        }
                        catch (StorageException e)
                        {
                            if ((e.RequestInformation != null) &&
                                (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound) &&
                                string.IsNullOrEmpty(accessCondition.IfMatchETag))
                            {
                                // If we got a 404 and the condition was not an If-Match,
                                // we should continue with the operation.
                            }
                            else
                            {
                                storageAsyncResult.OnComplete(e);
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            storageAsyncResult.OnComplete(e);
                            return;
                        }

                        prepareStorageAsyncResult(storageAsyncResult);
                    },
                    null /* state */);

                storageAsyncResult.CancelDelegate = result.Cancel;
            }
            else
            {
                prepareStorageAsyncResult(storageAsyncResult);
            }

            return storageAsyncResult;
        }

        /// <summary>
        /// Ends an asynchronous operation to open a stream for writing to the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="CloudBlobStream"/> to be used for writing to the blob.</returns>
        public virtual CloudBlobStream EndOpenWrite(IAsyncResult asyncResult)
        {
            StorageAsyncResult<CloudBlobStream> storageAsyncResult = (StorageAsyncResult<CloudBlobStream>)asyncResult;
            storageAsyncResult.End();
            return storageAsyncResult.Result;
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to open a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlobStream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, see <see cref="OpenWriteAsync(AccessCondition, BlobRequestOptions, OperationContext)"/>.</para>        
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync()
        {
            return this.OpenWriteAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to open a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlobStream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, see <see cref="OpenWriteAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.</para>                
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginOpenWrite, this.EndOpenWrite, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to open a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlobStream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.OpenWriteAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to open a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlobStream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginOpenWrite, this.EndOpenWrite, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void UploadFromStream(Stream source, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.UploadFromStreamHelper(source, null /* length */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void UploadFromStream(Stream source, long length, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.UploadFromStreamHelper(source, length, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        internal void UploadFromStreamHelper(Stream source, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("source", source);

            if (length.HasValue)
            {
                CommonUtility.AssertInBounds("length", length.Value, 1);

                if (source.CanSeek && length > source.Length - source.Position)
                {
                    throw new ArgumentOutOfRangeException("length", SR.StreamLengthShortError);
                }
            }

            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            bool lessThanSingleBlobThreshold = CloudBlockBlob.IsLessThanSingleBlobThreshold(source, length, modifiedOptions, false);
            modifiedOptions.AssertPolicyIfRequired();

            if (modifiedOptions.ParallelOperationThreadCount.Value == 1 && lessThanSingleBlobThreshold)
            {
                bool usingEncryption = modifiedOptions.EncryptionPolicy != null;
                Stream sourceStream = source;
                using (MemoryStream tempStream = !usingEncryption ? null : new MemoryStream())
                {
                    // Encrypt if necessary
                    if (usingEncryption)
                    {
                        modifiedOptions.AssertPolicyIfRequired();
                        if (modifiedOptions.EncryptionPolicy.EncryptionMode != BlobEncryptionMode.FullBlob)
                        {
                            throw new InvalidOperationException(SR.InvalidEncryptionMode, null);
                        }

                        ICryptoTransform transform = modifiedOptions.EncryptionPolicy.CreateAndSetEncryptionContext(this.Metadata, false /* noPadding */);
                        CryptoStream cryptoStream = new CryptoStream(tempStream, transform, CryptoStreamMode.Write);
                        using (ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(options))
                        {
                            source.WriteToSync(cryptoStream, length, null, false, true, tempExecutionState, null);
                            cryptoStream.FlushFinalBlock();
                        }

                        // After the tempStream has been written to, we need to seek back to the beginning, so that it can be read from.
                        tempStream.Seek(0, SeekOrigin.Begin);
                        length = tempStream.Length;
                        sourceStream = tempStream;
                    }

                    // Calculate MD5 if necessary
                    // Note that we cannot do this while we encrypt, it must be a separate step, because we want the MD5 of the encrypted data, 
                    // not the unencrypted data.
                    string contentMD5 = null;
                    if (modifiedOptions.StoreBlobContentMD5.Value)
                    {
                        using (ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions))
                        {
                            StreamDescriptor streamCopyState = new StreamDescriptor();
                            long startPosition = sourceStream.Position;
                            sourceStream.WriteToSync(Stream.Null, length, null /* maxLength */, true, true, tempExecutionState, streamCopyState);
                            sourceStream.Position = startPosition;
                            contentMD5 = streamCopyState.Md5;
                        }
                    }
                    else
                    {
                        // Throw exception if we need to use Transactional MD5 but cannot store it
                        if (modifiedOptions.UseTransactionalMD5.Value)
                        {
                            throw new ArgumentException(SR.PutBlobNeedsStoreBlobContentMD5, "options");
                        }
                    }

                    // Execute the put blob.
                    Executor.ExecuteSync(
                        this.PutBlobImpl(sourceStream, length, contentMD5, accessCondition, modifiedOptions),
                        modifiedOptions.RetryPolicy,
                        operationContext);
                }
            }
            else
            {
                using (CloudBlobStream blobStream = this.OpenWrite(accessCondition, modifiedOptions, operationContext))
                {
                    using (ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions))
                    {
                        source.WriteToSync(blobStream, length, null /* maxLength */, false, true, tempExecutionState, null /* streamCopyState */);
                        blobStream.Commit();
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, null /* length */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, null /* length */, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, long length, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, length, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, length, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        internal ICancellableAsyncResult BeginUploadFromStreamHelper(Stream source, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("source", source);

            if (length.HasValue)
            {
                CommonUtility.AssertInBounds("length", length.Value, 1);

                if (source.CanSeek && length > source.Length - source.Position)
                {
                    throw new ArgumentOutOfRangeException("length", SR.StreamLengthShortError);
                }
            }

            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);

            ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);
            StorageAsyncResult<NullType> storageAsyncResult = new StorageAsyncResult<NullType>(callback, state);

            bool lessThanSingleBlobThreshold = CloudBlockBlob.IsLessThanSingleBlobThreshold(source, length, modifiedOptions, false);
            modifiedOptions.AssertPolicyIfRequired();
            
            if (modifiedOptions.ParallelOperationThreadCount.Value == 1 && lessThanSingleBlobThreshold)
            {
                // Because we may or may not want to calculate the MD5, and we may or may not want to encrypt, rather than have four branching code
                // paths, here we have an action that we will run, which continually gets added to, depending on which operations we need to do.
                // The confusing part is that we have to build it from the bottom up.

                string md5 = null;
                Stream sourceStream = source;
                Action actionToRun = null;

                Action uploadAction = () =>
                    {
                        if (md5 == null && modifiedOptions.UseTransactionalMD5.Value)
                        {
                            throw new ArgumentException(SR.PutBlobNeedsStoreBlobContentMD5, "options");
                        }

                        this.UploadFromStreamHandler(
                                            sourceStream,
                                            length,
                                            md5,
                                            accessCondition,
                                            operationContext,
                                            modifiedOptions,
                                            storageAsyncResult);

                    };
                actionToRun = uploadAction;

                if (modifiedOptions.StoreBlobContentMD5.Value)
                {
                    Action<Action> calculateMD5 = (continuation) =>
                    {
                        long startPosition = sourceStream.Position;
                        StreamDescriptor streamCopyState = new StreamDescriptor();
                        sourceStream.WriteToAsync(
                            Stream.Null,
                            length,
                            null /* maxLength */,
                            true,
                            tempExecutionState,
                            streamCopyState,
                            completedState =>
                            {
                                ContinueAsyncOperation(storageAsyncResult, completedState, () =>
                                    {
                                        if (completedState.ExceptionRef != null)
                                        {
                                            storageAsyncResult.OnComplete(completedState.ExceptionRef);
                                        }
                                        else
                                        {
                                            sourceStream.Position = startPosition;
                                            md5 = streamCopyState.Md5;
                                            continuation();
                                        }
                                    });
                            });

                        storageAsyncResult.CancelDelegate = tempExecutionState.Cancel;
                        if (storageAsyncResult.CancelRequested)
                        {
                            storageAsyncResult.Cancel();
                        }
                    };
                    Action oldActionToRun = actionToRun;
                    actionToRun = () => calculateMD5(oldActionToRun);
                }

                if (modifiedOptions.EncryptionPolicy != null)
                {
                    Action<Action> encryptStream = continuation =>
                        {
                            SyncMemoryStream syncMemoryStream = new SyncMemoryStream();
                            options.AssertPolicyIfRequired();

                            sourceStream = syncMemoryStream;

                            if (modifiedOptions.EncryptionPolicy.EncryptionMode != BlobEncryptionMode.FullBlob)
                            {
                                throw new InvalidOperationException(SR.InvalidEncryptionMode, null);
                            }

                            ICryptoTransform transform = options.EncryptionPolicy.CreateAndSetEncryptionContext(this.Metadata, false /* noPadding */);
                            CryptoStream cryptoStream = new CryptoStream(syncMemoryStream, transform, CryptoStreamMode.Write);
                            StreamDescriptor streamCopyState = new StreamDescriptor();

                            source.WriteToAsync(cryptoStream, length, null, false, tempExecutionState, streamCopyState, completedState =>
                                {
                                    ContinueAsyncOperation(storageAsyncResult, completedState, () =>
                                        {
                                            if (completedState.ExceptionRef != null)
                                            {
                                                storageAsyncResult.OnComplete(completedState.ExceptionRef);
                                            }
                                            else
                                            {
                                                // Flush the CryptoStream in order to make sure that the last block of data is flushed. This call is a sync call
                                                // but it is ok to have it because we're just writing to a memory stream.
                                                cryptoStream.FlushFinalBlock();

                                                // After the tempStream has been written to, we need to seek back to the beginning, so that it can be read from.
                                                sourceStream.Seek(0, SeekOrigin.Begin);
                                                length = syncMemoryStream.Length;
                                                continuation();
                                            }
                                        });
                                });

                            storageAsyncResult.CancelDelegate = tempExecutionState.Cancel;
                            if (storageAsyncResult.CancelRequested)
                            {
                                storageAsyncResult.Cancel();
                            }
                        };
                    Action oldActionToRun = actionToRun;
                    actionToRun = () => encryptStream(oldActionToRun);
                }

                actionToRun();
            }
            else
            {
                ICancellableAsyncResult result = this.BeginOpenWrite(
                    accessCondition,
                    modifiedOptions,
                    operationContext,
                    ar =>
                    {
                        ContinueAsyncOperation(storageAsyncResult, ar, () =>
                            {
                                CloudBlobStream blobStream = this.EndOpenWrite(ar);
                                storageAsyncResult.OperationState = blobStream;

                                source.WriteToAsync(
                                    blobStream,
                                    length,
                                    null /* maxLength */,
                                    false,
                                    tempExecutionState,
                                    null /* streamCopyState */,
                                    completedState =>
                                    {
                                        ContinueAsyncOperation(storageAsyncResult, completedState, () =>
                                        {
                                            if (completedState.ExceptionRef != null)
                                            {
                                                storageAsyncResult.OnComplete(completedState.ExceptionRef);
                                            }
                                            else
                                            {
                                                ICancellableAsyncResult commitResult = blobStream.BeginCommit(
                                                        CloudBlob.BlobOutputStreamCommitCallback,
                                                        storageAsyncResult);

                                                storageAsyncResult.CancelDelegate = commitResult.Cancel;
                                                if (storageAsyncResult.CancelRequested)
                                                {
                                                    storageAsyncResult.Cancel();
                                                }
                                            }
                                        });
                                    });

                                storageAsyncResult.CancelDelegate = tempExecutionState.Cancel;
                                if (storageAsyncResult.CancelRequested)
                                {
                                    storageAsyncResult.Cancel();
                                }
                            });
                    },
                    null /* state */);

                // We do not need to do this inside a lock, as storageAsyncResult is
                // not returned to the user yet.
                storageAsyncResult.CancelDelegate = result.Cancel;
            }

            return storageAsyncResult;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        private void UploadFromStreamHandler(Stream source, long? length, string contentMD5, AccessCondition accessCondition, OperationContext operationContext, BlobRequestOptions options, StorageAsyncResult<NullType> storageAsyncResult)
        {
            ICancellableAsyncResult result = Executor.BeginExecuteAsync(
                this.PutBlobImpl(source, length, contentMD5, accessCondition, options),
                options.RetryPolicy,
                operationContext,
                ar =>
                {
                    storageAsyncResult.UpdateCompletedSynchronously(ar.CompletedSynchronously);

                    try
                    {
                        Executor.EndExecuteAsync<NullType>(ar);
                        storageAsyncResult.OnComplete();
                    }
                    catch (Exception e)
                    {
                        storageAsyncResult.OnComplete(e);
                    }
                },
                null /* asyncState */);

            storageAsyncResult.CancelDelegate = result.Cancel;
            if (storageAsyncResult.CancelRequested)
            {
                storageAsyncResult.Cancel();
            }
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a stream to a block blob. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadFromStream(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> storageAsyncResult = (StorageAsyncResult<NullType>)asyncResult;
            storageAsyncResult.End();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source)
        {
            return this.UploadFromStreamAsync(source, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromStream, this.EndUploadFromStream, source, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsync(source, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromStream, this.EndUploadFromStream, source, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length)
        {
            return this.UploadFromStreamAsync(source, length, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromStream, this.EndUploadFromStream, source, length, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsync(source, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromStream, this.EndUploadFromStream, source, length, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a file to the Blob service. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void UploadFromFile(string path, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("path", path);

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                this.UploadFromStream(fileStream, accessCondition, options, operationContext);
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a file to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>        
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromFile(string path, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromFile(path, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a file to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromFile(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("path", path);

            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            StorageAsyncResult<NullType> storageAsyncResult = new StorageAsyncResult<NullType>(callback, state)
            {
                OperationState = fileStream
            };

            try
            {
                ICancellableAsyncResult asyncResult = this.BeginUploadFromStream(fileStream, accessCondition, options, operationContext, this.UploadFromFileCallback, storageAsyncResult);
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
        /// Called when the asynchronous UploadFromStream operation completes.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        private void UploadFromFileCallback(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> storageAsyncResult = (StorageAsyncResult<NullType>)asyncResult.AsyncState;
            Exception exception = null;

            try
            {
                this.EndUploadFromStream(asyncResult);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // We should do FileStream disposal in a separate try-catch block
            // because we want to close the file even if the operation fails.
            try
            {
                FileStream fileStream = (FileStream)storageAsyncResult.OperationState;
                fileStream.Dispose();
            }
            catch (Exception e)
            {
                exception = e;
            }

            storageAsyncResult.OnComplete(exception);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a file to a blob. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadFromFile(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> res = (StorageAsyncResult<NullType>)asyncResult;
            res.End();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload a file to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path)
        {
            return this.UploadFromFileAsync(path, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a file to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromFile, this.EndUploadFromFile, path, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a file to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromFileAsync(path, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a file to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromFile, this.EndUploadFromFile, path, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads the contents of a byte array to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void UploadFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            using (SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count))
            {
                this.UploadFromStream(stream, accessCondition, options, operationContext);
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload the contents of a byte array to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromByteArray(byte[] buffer, int index, int count, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromByteArray(buffer, index, count, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload the contents of a byte array to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.BeginUploadFromStream(stream, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload the contents of a byte array to a blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadFromByteArray(IAsyncResult asyncResult)
        {
            this.EndUploadFromStream(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload the contents of a byte array to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload the contents of a byte array to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromByteArray, this.EndUploadFromByteArray, buffer, index, count, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload the contents of a byte array to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload the contents of a byte array to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromByteArray, this.EndUploadFromByteArray, buffer, index, count, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void UploadText(string content, Encoding encoding = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            this.UploadFromByteArray(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadText(string content, AsyncCallback callback, object state)
        {
            return this.BeginUploadText(content, null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadText(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.BeginUploadFromByteArray(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a string of text to a blob. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadText(IAsyncResult asyncResult)
        {
            this.EndUploadFromByteArray(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content)
        {
            return this.UploadTextAsync(content, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadText, this.EndUploadText, content, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadText, this.EndUploadText, content, encoding, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The contents of the blob, as a string.</returns>
        [DoesServiceRequest]        
        public virtual string DownloadText(Encoding encoding = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
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
        /// Begins an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]        
        public virtual ICancellableAsyncResult BeginDownloadText(AsyncCallback callback, object state)
        {
            return this.BeginDownloadText(null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]        
        public virtual ICancellableAsyncResult BeginDownloadText(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            SyncMemoryStream stream = new SyncMemoryStream();
            StorageAsyncResult<string> storageAsyncResult = new StorageAsyncResult<string>(callback, state) { OperationState = Tuple.Create(stream, encoding) };

            ICancellableAsyncResult result = this.BeginDownloadToStream(
                stream,
                accessCondition,
                options,
                operationContext,
                this.DownloadTextCallback,
                storageAsyncResult);

            storageAsyncResult.CancelDelegate = result.Cancel;
            return storageAsyncResult;
        }

        /// <summary>
        /// Called when the asynchronous DownloadToStream operation completes.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        private void DownloadTextCallback(IAsyncResult asyncResult)
        {
            StorageAsyncResult<string> storageAsyncResult = (StorageAsyncResult<string>)asyncResult.AsyncState;

            try
            {
                this.EndDownloadToStream(asyncResult);

                Tuple<SyncMemoryStream, Encoding> state = (Tuple<SyncMemoryStream, Encoding>)storageAsyncResult.OperationState;
                byte[] streamAsBytes = state.Item1.GetBuffer();
                storageAsyncResult.Result = (state.Item2 ?? Encoding.UTF8).GetString(streamAsBytes, 0, (int)state.Item1.Length);
                storageAsyncResult.OnComplete();
            }
            catch (Exception e)
            {
                storageAsyncResult.OnComplete(e);
            }
        }

        /// <summary>
        /// Ends an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The contents of the blob, as a string.</returns>
        public virtual string EndDownloadText(IAsyncResult asyncResult)
        {
            StorageAsyncResult<string> res = (StorageAsyncResult<string>)asyncResult;
            res.End();
            return res.Result;
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync()
        {
            return this.DownloadTextAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDownloadText, this.EndDownloadText, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadTextAsync(encoding, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDownloadText, this.EndDownloadText, encoding, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void PutBlock(string blockId, Stream blockData, string contentMD5, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("blockData", blockData);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            bool requiresContentMD5 = string.IsNullOrEmpty(contentMD5) && modifiedOptions.UseTransactionalMD5.Value;
            operationContext = operationContext ?? new OperationContext();

            Stream seekableStream = blockData;
            bool seekableStreamCreated = false;

            try
            {
                if (!blockData.CanSeek || requiresContentMD5)
                {
                    ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);

                    Stream writeToStream;
                    if (blockData.CanSeek)
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
                    blockData.WriteToSync(writeToStream, null /* copyLength */, Constants.MaxBlockSize, requiresContentMD5, true, tempExecutionState, streamCopyState);
                    seekableStream.Position = startPosition;

                    if (requiresContentMD5)
                    {
                        contentMD5 = streamCopyState.Md5;
                    }
                }

                Executor.ExecuteSync(
                    this.PutBlockImpl(seekableStream, blockId, contentMD5, accessCondition, modifiedOptions),
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
        /// Begins an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginPutBlock(string blockId, Stream blockData, string contentMD5, AsyncCallback callback, object state)
        {
            return this.BeginPutBlock(blockId, blockData, contentMD5, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

#if SYNC
        /// <summary>
        /// Returns an enumerable collection of the blob's blocks, using the specified block list filter.
        /// </summary>
        /// <param name="blockListingFilter">A <see cref="BlockListingFilter"/> enumeration value that indicates whether to return 
        /// committed blocks, uncommitted blocks, or both.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects implementing <see cref="ListBlockItem"/>.</returns>
        [DoesServiceRequest]
        public virtual IEnumerable<ListBlockItem> DownloadBlockList(BlockListingFilter blockListingFilter = BlockListingFilter.Committed, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            return Executor.ExecuteSync(
                this.GetBlockListImpl(blockListingFilter, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginPutBlock(string blockId, Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("blockData", blockData);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            bool requiresContentMD5 = string.IsNullOrEmpty(contentMD5) && modifiedOptions.UseTransactionalMD5.Value;
            operationContext = operationContext ?? new OperationContext();
            StorageAsyncResult<NullType> storageAsyncResult = new StorageAsyncResult<NullType>(callback, state);

            if (blockData.CanSeek && !requiresContentMD5)
            {
                this.PutBlockHandler(blockId, blockData, contentMD5, accessCondition, modifiedOptions, operationContext, storageAsyncResult);
            }
            else
            {
                ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);
                storageAsyncResult.CancelDelegate = tempExecutionState.Cancel;

                Stream seekableStream;
                Stream writeToStream;
                if (blockData.CanSeek)
                {
                    seekableStream = blockData;
                    writeToStream = Stream.Null;
                }
                else
                {
                    seekableStream = new MultiBufferMemoryStream(this.ServiceClient.BufferManager);
                    storageAsyncResult.OperationState = seekableStream;
                    writeToStream = seekableStream;
                }

                long startPosition = seekableStream.Position;
                StreamDescriptor streamCopyState = new StreamDescriptor();
                blockData.WriteToAsync(
                    writeToStream,
                    null /* copyLength */,
                    Constants.MaxBlockSize,
                    requiresContentMD5,
                    tempExecutionState,
                    streamCopyState,
                    completedState =>
                    {
                        storageAsyncResult.UpdateCompletedSynchronously(completedState.CompletedSynchronously);

                        if (completedState.ExceptionRef != null)
                        {
                            storageAsyncResult.OnComplete(completedState.ExceptionRef);
                        }
                        else
                        {
                            try
                            {
                                if (requiresContentMD5)
                                {
                                    contentMD5 = streamCopyState.Md5;
                                }

                                seekableStream.Position = startPosition;
                                this.PutBlockHandler(blockId, seekableStream, contentMD5, accessCondition, modifiedOptions, operationContext, storageAsyncResult);
                            }
                            catch (Exception e)
                            {
                                storageAsyncResult.OnComplete(e);
                            }
                        }
                    });
            }

            return storageAsyncResult;
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndPutBlock(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> storageAsyncResult = (StorageAsyncResult<NullType>)asyncResult;

            try
            {
                storageAsyncResult.End();
            }
            finally
            {
                if (storageAsyncResult.OperationState != null)
                {
                    MultiBufferMemoryStream stream = (MultiBufferMemoryStream)storageAsyncResult.OperationState;
                    stream.Dispose();
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        private void PutBlockHandler(string blockId, Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, StorageAsyncResult<NullType> storageAsyncResult)
        {
            lock (storageAsyncResult.CancellationLockerObject)
            {
                ICancellableAsyncResult result = Executor.BeginExecuteAsync(
                    this.PutBlockImpl(blockData, blockId, contentMD5, accessCondition, options),
                    options.RetryPolicy,
                    operationContext,
                    ar =>
                    {
                        storageAsyncResult.UpdateCompletedSynchronously(ar.CompletedSynchronously);

                        try
                        {
                            Executor.EndExecuteAsync<NullType>(ar);
                            storageAsyncResult.OnComplete();
                        }
                        catch (Exception e)
                        {
                            storageAsyncResult.OnComplete(e);
                        }
                    },
                    null /* asyncState */);

                storageAsyncResult.CancelDelegate = result.Cancel;
                if (storageAsyncResult.CancelRequested)
                {
                    storageAsyncResult.Cancel();
                }
            }
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5)
        {
            return this.PutBlockAsync(blockId, blockData, contentMD5, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginPutBlock, this.EndPutBlock, blockId, blockData, contentMD5, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.PutBlockAsync(blockId, blockData, contentMD5, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginPutBlock, this.EndPutBlock, blockId, blockData, contentMD5, accessCondition, options, operationContext, cancellationToken);
        }
#endif


#if SYNC
        /// <summary>
        /// Begins an operation to start copying an Azure file's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last-modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual string StartCopy(CloudFile source, AccessCondition sourceAccessCondition = null, AccessCondition destAccessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            return this.StartCopy(CloudFile.SourceFileToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext);
        }

        /// <summary>
        /// Begins an operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
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
        public virtual string StartCopy(CloudBlockBlob source, AccessCondition sourceAccessCondition = null, AccessCondition destAccessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            return this.StartCopy(CloudBlob.SourceBlobToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to start copying a file's contents, properties, and metadata to this block blob.
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
        /// Begins an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(CloudBlockBlob source, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(CloudBlob.SourceBlobToUri(source), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying a file's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>        
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(CloudFile source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(CloudFile.SourceFileToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>        
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(CloudBlockBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(CloudBlob.SourceBlobToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext, callback, state);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to start copying a file's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudFile source)
        {
            return this.StartCopyAsync(source, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudBlockBlob source)
        {
            return this.StartCopyAsync(source, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying a file's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudFile source, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginStartCopy, this.EndStartCopy, source, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudBlockBlob source, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginStartCopy, this.EndStartCopy, source, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying a file's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudFile source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudBlockBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying a file's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudFile"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudFile source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginStartCopy, this.EndStartCopy, source, sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudBlockBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginStartCopy, this.EndStartCopy, source, sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to return an enumerable collection of the blob's blocks, 
        /// using the specified block list filter.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadBlockList(AsyncCallback callback, object state)
        {
            return this.BeginDownloadBlockList(BlockListingFilter.Committed, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to return an enumerable collection of the blob's blocks, 
        /// using the specified block list filter.
        /// </summary>
        /// <param name="blockListingFilter">A <see cref="BlockListingFilter"/> enumeration value that indicates whether to return 
        /// committed blocks, uncommitted blocks, or both.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginDownloadBlockList(BlockListingFilter blockListingFilter, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.GetBlockListImpl(blockListingFilter, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to return an enumerable collection of the blob's blocks, 
        /// using the specified block list filter.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>An enumerable collection of objects implementing <see cref="ListBlockItem"/>.</returns>
        public virtual IEnumerable<ListBlockItem> EndDownloadBlockList(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<IEnumerable<ListBlockItem>>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to return an enumerable collection of the blob's blocks, 
        /// using the specified block list filter.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object that is an enumerable collection of type <see cref="ListBlockItem"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<ListBlockItem>> DownloadBlockListAsync()
        {
            return this.DownloadBlockListAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return an enumerable collection of the blob's blocks, 
        /// using the specified block list filter.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that is an enumerable collection of type <see cref="ListBlockItem"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<ListBlockItem>> DownloadBlockListAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDownloadBlockList, this.EndDownloadBlockList, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return an enumerable collection of the blob's blocks, 
        /// using the specified block list filter.
        /// </summary>
        /// <param name="blockListingFilter">A <see cref="BlockListingFilter"/> enumeration value that indicates whether to return
        /// committed blocks, uncommitted blocks, or both.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object that is an enumerable collection of type <see cref="ListBlockItem"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<ListBlockItem>> DownloadBlockListAsync(BlockListingFilter blockListingFilter, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadBlockListAsync(blockListingFilter, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to return an enumerable collection of the blob's blocks, 
        /// using the specified block list filter.
        /// </summary>
        /// <param name="blockListingFilter">A <see cref="BlockListingFilter"/> enumeration value that indicates whether to return 
        /// committed blocks, uncommitted blocks, or both.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object that is an enumerable collection of type <see cref="ListBlockItem"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<ListBlockItem>> DownloadBlockListAsync(BlockListingFilter blockListingFilter, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginDownloadBlockList, this.EndDownloadBlockList, blockListingFilter, accessCondition, options, operationContext, cancellationToken);
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
        /// <returns>A <see cref="CloudBlockBlob"/> object that is a blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual CloudBlockBlob CreateSnapshot(IDictionary<string, string> metadata = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            return Executor.ExecuteSync(
                this.CreateSnapshotImpl(metadata, accessCondition, modifiedOptions),
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
        public virtual ICancellableAsyncResult BeginCreateSnapshot(AsyncCallback callback, object state)
        {
            return this.BeginCreateSnapshot(null /* metadata */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
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
        public virtual ICancellableAsyncResult BeginCreateSnapshot(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.CreateSnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="CloudBlockBlob"/> object that is a blob snapshot.</returns>
        public virtual CloudBlockBlob EndCreateSnapshot(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<CloudBlockBlob>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlockBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlockBlob> CreateSnapshotAsync()
        {
            return this.CreateSnapshotAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlockBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlockBlob> CreateSnapshotAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginCreateSnapshot, this.EndCreateSnapshot, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlockBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlockBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.CreateSnapshotAsync(metadata, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlockBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlockBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginCreateSnapshot, this.EndCreateSnapshot, metadata, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a list of blocks to a new or existing blob. 
        /// </summary>
        /// <param name="blockList">An enumerable collection of block IDs, as Base64-encoded strings.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void PutBlockList(IEnumerable<string> blockList, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            IEnumerable<PutBlockListItem> items = blockList.Select(i => new PutBlockListItem(i, BlockSearchMode.Latest));
            Executor.ExecuteSync(
                this.PutBlockListImpl(items, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a list of blocks to a new or existing blob. 
        /// </summary>
        /// <param name="blockList">An enumerable collection of block IDs, as Base64-encoded strings.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginPutBlockList(IEnumerable<string> blockList, AsyncCallback callback, object state)
        {
            return this.BeginPutBlockList(blockList, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a list of blocks to a new or existing blob. 
        /// </summary>
        /// <param name="blockList">An enumerable collection of block IDs, as Base64-encoded strings.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginPutBlockList(IEnumerable<string> blockList, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            IEnumerable<PutBlockListItem> items = blockList.Select(i => new PutBlockListItem(i, BlockSearchMode.Latest));
            return Executor.BeginExecuteAsync(
                this.PutBlockListImpl(items, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a list of blocks to a new or existing blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndPutBlockList(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload a list of blocks to a new or existing blob. 
        /// </summary>
        /// <param name="blockList">An enumerable collection of block IDs, as Base64-encoded strings.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockListAsync(IEnumerable<string> blockList)
        {
            return this.PutBlockListAsync(blockList, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a list of blocks to a new or existing blob. 
        /// </summary>
        /// <param name="blockList">An enumerable collection of block IDs, as Base64-encoded strings.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockListAsync(IEnumerable<string> blockList, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginPutBlockList, this.EndPutBlockList, blockList, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a list of blocks to a new or existing blob. 
        /// </summary>
        /// <param name="blockList">An enumerable collection of block IDs, as Base64-encoded strings.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockListAsync(IEnumerable<string> blockList, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.PutBlockListAsync(blockList, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a list of blocks to a new or existing blob. 
        /// </summary>
        /// <param name="blockList">An enumerable collection of block IDs, as Base64-encoded strings.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockListAsync(IEnumerable<string> blockList, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginPutBlockList, this.EndPutBlockList, blockList, accessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Uploads the full blob from a seekable stream.
        /// </summary>
        /// <param name="stream">The content stream. Must be seekable.</param>
        /// <param name="length">Number of bytes to upload from the content stream starting at its current position.</param>
        /// <param name="contentMD5">The content MD5.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that gets the stream.</returns>
        private RESTCommand<NullType> PutBlobImpl(Stream stream, long? length, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options)
        {
            long offset = stream.Position;
            this.Properties.ContentMD5 = contentMD5;

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.SendStream = stream;
            putCmd.SendStreamLength = length ?? stream.Length - offset;
            putCmd.RecoveryAction = (cmd, ex, ctx) => RecoveryActions.SeekStream(cmd, offset);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.Put(uri, serverTimeout, this.Properties, BlobType.BlockBlob, 0, accessCondition, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) => BlobHttpWebRequestFactory.AddMetadata(r, this.Metadata);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                this.Properties.Length = putCmd.SendStreamLength.Value;
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Uploads the block.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="blockId">The block ID.</param>
        /// <param name="contentMD5">The content MD5.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that uploads the block.</returns>
        internal RESTCommand<NullType> PutBlockImpl(Stream source, string blockId, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options)
        {
            options.AssertNoEncryptionPolicyOrStrictMode();

            long offset = source.Position;

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.SendStream = source;
            putCmd.RecoveryAction = (cmd, ex, ctx) => RecoveryActions.SeekStream(cmd, offset);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.PutBlock(uri, serverTimeout, blockId, accessCondition, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) =>
            {
                if (!string.IsNullOrEmpty(contentMD5))
                {
                    r.Headers[HttpRequestHeader.ContentMd5] = contentMD5;
                }
            };
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Uploads the block list.
        /// </summary>
        /// <param name="blocks">The blocks to upload.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that uploads the block list.</returns>
        internal RESTCommand<NullType> PutBlockListImpl(IEnumerable<PutBlockListItem> blocks, AccessCondition accessCondition, BlobRequestOptions options)
        {
            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(this.ServiceClient.BufferManager);
            BlobRequest.WriteBlockListBody(blocks, memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

#if !WINDOWS_PHONE
            string contentMD5 = null;
            if (options.UseTransactionalMD5.HasValue && options.UseTransactionalMD5.Value)
            {
                contentMD5 = memoryStream.ComputeMD5Hash();
                memoryStream.Seek(0, SeekOrigin.Begin);
            }
#endif

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.PutBlockList(uri, serverTimeout, this.Properties, accessCondition, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) =>
            {
#if !WINDOWS_PHONE
                if (contentMD5 != null)
                {
                    r.Headers[HttpRequestHeader.ContentMd5] = contentMD5;
                }
#endif
                BlobHttpWebRequestFactory.AddMetadata(r, this.Metadata);
            };
            putCmd.SendStream = memoryStream;
            putCmd.StreamToDispose = memoryStream;
            putCmd.RecoveryAction = RecoveryActions.RewindStream;
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                this.Properties.Length = -1;
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Gets the download block list.
        /// </summary>
        /// <param name="typesOfBlocks">The types of blocks.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that gets the download block list.</returns>
        internal RESTCommand<IEnumerable<ListBlockItem>> GetBlockListImpl(BlockListingFilter typesOfBlocks, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<IEnumerable<ListBlockItem>> getCmd = new RESTCommand<IEnumerable<ListBlockItem>>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.GetBlockList(uri, serverTimeout, this.SnapshotTime, typesOfBlocks, accessCondition, useVersionHeader, ctx);
            getCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, true);
                GetBlockListResponse responseParser = new GetBlockListResponse(cmd.ResponseStream);
                IEnumerable<ListBlockItem> blocks = new List<ListBlockItem>(responseParser.Blocks);
                return blocks;
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the Snapshot method.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot, or <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that creates the snapshot.</returns>
        /// <remarks>If the <c>metadata</c> parameter is <c>null</c> then no metadata is associated with the request.</remarks>
        internal RESTCommand<CloudBlockBlob> CreateSnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<CloudBlockBlob> putCmd = new RESTCommand<CloudBlockBlob>(this.ServiceClient.Credentials, this.attributes.StorageUri);

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

                CloudBlockBlob snapshot = new CloudBlockBlob(this.Name, snapshotTime, this.Container);
                snapshot.attributes.Metadata = new Dictionary<string, string>(metadata ?? this.Metadata);
                snapshot.attributes.Properties = new BlobProperties(this.Properties);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(snapshot.attributes, resp, false);
                return snapshot;
            };

            return putCmd;
        }

        private static bool IsLessThanSingleBlobThreshold(Stream source, long? length, BlobRequestOptions modifiedOptions, bool noPadding)
        {
            if (!source.CanSeek)
            {
                return false;
            }

            length = length ?? source.Length - source.Position;

            if (modifiedOptions.EncryptionPolicy != null)
            {
                length = modifiedOptions.EncryptionPolicy.GetEncryptedLength(length.Value, noPadding);
            }

            return length <= modifiedOptions.SingleBlobUploadThresholdInBytes.Value;
        }

        private static void ContinueAsyncOperation(StorageAsyncResult<NullType> storageAsyncResult, IAsyncResult result, Action actionToTakeInTheLock)
        {
            storageAsyncResult.UpdateCompletedSynchronously(result.CompletedSynchronously);
            try
            {
                lock (storageAsyncResult.CancellationLockerObject)
                {
                    storageAsyncResult.CancelDelegate = null;
                    actionToTakeInTheLock();
                }
            }
            catch (Exception e)
            {
                storageAsyncResult.OnComplete(e);
            }
        }
    }
}
