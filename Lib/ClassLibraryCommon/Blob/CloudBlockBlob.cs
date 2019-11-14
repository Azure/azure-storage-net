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

namespace Microsoft.Azure.Storage.Blob
{
    using Microsoft.Azure.Storage.Blob.Protocol;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
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
        /// ranging from between 16 KB and 100 MB inclusive.</para>
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
                    // If the accessCondition is IsIfNotExists, the fetch call will always return 400
                    this.FetchAttributes(accessCondition.Clone().RemoveIsIfNotExistsCondition(), options, operationContext);

                    // In case the blob already exists and the access condition is "IfNotExists", we should fail fast before uploading any content for the blob 
                    if (accessCondition.IsIfNotExists)
                    {
                        throw GenerateExceptionForConflictFailure();
                    }
                }
                catch (StorageException e)
                {
                    if (!CloudBlockBlob.ContinueOpenWriteOnFailure(e, accessCondition))
                    {
                        throw;
                    }
                }
            }

            modifiedOptions.AssertPolicyIfRequired();

            if (modifiedOptions.EncryptionPolicy != null)
            {
                ICryptoTransform transform = modifiedOptions.EncryptionPolicy.CreateAndSetEncryptionContext(this.Metadata, false /* noPadding */);
                return new BlobEncryptedWriteStream(this, accessCondition, modifiedOptions, operationContext, transform);
            }
            else
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
        /// ranging from between 16 KB and 100 MB inclusive.</para>
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
        /// ranging from between 16 KB and 100 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginOpenWrite(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.OpenWriteAsync(accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to open a stream for writing to the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="CloudBlobStream"/> to be used for writing to the blob.</returns>
        public virtual CloudBlobStream EndOpenWrite(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<CloudBlobStream>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to open a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlobStream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 100 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, see <see cref="OpenWriteAsync(AccessCondition, BlobRequestOptions, OperationContext)"/>.</para>        
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync()
        {
            return this.OpenWriteAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to open a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlobStream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 100 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, see <see cref="OpenWriteAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.</para>                
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(CancellationToken cancellationToken)
        {
            return this.OpenWriteAsync(default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
        /// ranging from between 16 KB and 100 MB inclusive.</para>
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
        /// ranging from between 16 KB and 100 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.</para>
        /// </remarks>
        [DoesServiceRequest]
        public virtual async Task<CloudBlobStream> OpenWriteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, this.BlobType, this.ServiceClient, false);

            if ((accessCondition != null) && accessCondition.IsConditional)
            {
                try
                {
                    // If the accessCondition is IsIfNotExists, the fetch call will always return 400
                    await this.FetchAttributesAsync(accessCondition.Clone().RemoveIsIfNotExistsCondition(), options, operationContext, cancellationToken).ConfigureAwait(false);

                    // In case the blob already exists and the access condition is "IfNotExists", we should fail fast before uploading any content for the blob 
                    if (accessCondition.IsIfNotExists)
                    {
                        throw GenerateExceptionForConflictFailure();
                    }
                }
                catch (StorageException e)
                {
                    if (!CloudBlockBlob.ContinueOpenWriteOnFailure(e, accessCondition))
                    {
                        throw;
                    }
                }
            }

            modifiedOptions.AssertPolicyIfRequired();

            if (modifiedOptions.EncryptionPolicy != null)
            {
                ICryptoTransform transform = modifiedOptions.EncryptionPolicy.CreateAndSetEncryptionContext(this.Metadata, false /* noPadding */);
                return new BlobEncryptedWriteStream(this, accessCondition, modifiedOptions, operationContext, transform);
            }
            else
            {
                return new BlobWriteStream(this, accessCondition, modifiedOptions, operationContext);
            }
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

            this.CheckAdjustBlockSize(length ?? (source.CanSeek ? (source.Length - source.Position) : length));
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

                        using (ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(options))
                        {
                            source.WriteToSync(cryptoStream, length, null, ChecksumRequested.None, true, tempExecutionState, null);
                            cryptoStream.FlushFinalBlock();
                        }

                        // After the tempStream has been written to, we need to seek back to the beginning, so that it can be read from.
                        tempStream.Seek(0, SeekOrigin.Begin);
                        length = tempStream.Length;
                        sourceStream = tempStream;
                    }

                    // Calculate checksum if necessary
                    // Note that we cannot do this while we encrypt, it must be a separate step, because we want the checksum of the encrypted data, 
                    // not the unencrypted data.
                    Checksum contentChecksum = Checksum.None;

                    if (modifiedOptions.ChecksumOptions.StoreContentMD5.Value)
                    {
                        using (ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(options))
                        {
                            StreamDescriptor streamCopyState = new StreamDescriptor();
                            long startPosition = sourceStream.Position;
                            sourceStream.WriteToSync(Stream.Null, length, null /* maxLength */, new ChecksumRequested(md5: true, crc64: false), true, tempExecutionState, streamCopyState);
                            sourceStream.Position = startPosition;
                            contentChecksum.MD5 = streamCopyState.Md5;
                        }
                    }
                    else
                    {
                        // Throw exception if we need to use Transactional MD5 but cannot store it
                        if (modifiedOptions.ChecksumOptions.UseTransactionalMD5.Value)
                        {
                            throw new ArgumentException(SR.PutBlobNeedsStoreBlobContentMD5, "options");
                        }
                    }

                    //string contentCRC64 = null;
                    //if (modifiedOptions.ChecksumOptions.StoreContentCRC64.Value)
                    //{
                    //    using (ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(options))
                    //    {
                    //        StreamDescriptor streamCopyState = new StreamDescriptor();
                    //        long startPosition = sourceStream.Position;
                    //        sourceStream.WriteToSync(Stream.Null, length, null /* maxLength */, new ChecksumRequested(md5: true, crc64: true), true, tempExecutionState, streamCopyState);
                    //        sourceStream.Position = startPosition;
                    //        contentChecksum.CRC64 = streamCopyState.Crc64;
                    //    }
                    //}
                    //else
                    //{
                    //    // Throw exception if we need to use Transactional CRC64 but cannot store it
                    //    if (modifiedOptions.ChecksumOptions.UseTransactionalCRC64.Value)
                    //    {
                    //        throw new ArgumentException(SR.PutBlobNeedsStoreBlobContentCRC64, "options");
                    //    }
                    //}

                    // Execute the put blob.
                    Executor.ExecuteSync(
                        this.PutBlobImpl(sourceStream, length, contentChecksum, accessCondition, modifiedOptions),
                        modifiedOptions.RetryPolicy,
                        operationContext);
                }
            }
            else
            {
                bool useOpenWrite = modifiedOptions.EncryptionPolicy != null
                       || !source.CanSeek
                       || this.streamWriteSizeInBytes < Constants.MinLargeBlockSize
                       || (modifiedOptions.ChecksumOptions.StoreContentMD5.HasValue && modifiedOptions.ChecksumOptions.StoreContentMD5.Value)
                       || (modifiedOptions.ChecksumOptions.StoreContentCRC64.HasValue && modifiedOptions.ChecksumOptions.StoreContentCRC64.Value);

                if (useOpenWrite)
                {
                    using (CloudBlobStream blobStream = this.OpenWrite(accessCondition, modifiedOptions, operationContext))
                    {
                        using (ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(modifiedOptions))
                        {
                            source.WriteToSync(blobStream, length, null /* maxLength */, ChecksumRequested.None, true, tempExecutionState, null /* streamCopyState */);
                            blobStream.Commit();
                        }
                    }
                }
                else
                {
                    // Synchronization mutex required to ensure thread-safe, concurrent operations on related SubStream instances.
                    SemaphoreSlim streamReadThrottler = new SemaphoreSlim(1);
                    CommonUtility.RunWithoutSynchronizationContext(
                        () => this.UploadFromMultiStreamAsync(
                            this.OpenMultiSubStream(source, length, streamReadThrottler),
                            accessCondition,
                            modifiedOptions,
                            operationContext,
                            default(AggregatingProgressIncrementer),
                            CancellationToken.None).GetAwaiter().GetResult());
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
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginUploadFromStream(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, null /* length */, accessCondition, options, operationContext, progressHandler, callback, state);
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
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginUploadFromStream(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, length, accessCondition, options, operationContext, progressHandler, callback, state);
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
            return this.BeginUploadFromStreamHelper(source, length, accessCondition, options, operationContext, null /*progressHandler*/, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        private ICancellableAsyncResult BeginUploadFromStreamHelper(Stream source, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, new AggregatingProgressIncrementer(progressHandler), token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a stream to a block blob. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadFromStream(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), default(AggregatingProgressIncrementer), CancellationToken.None);
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
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), default(AggregatingProgressIncrementer), cancellationToken);
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
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, accessCondition, options, operationContext, default(AggregatingProgressIncrementer), CancellationToken.None);
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
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, accessCondition, options, operationContext, default(AggregatingProgressIncrementer), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, accessCondition, options, operationContext, new AggregatingProgressIncrementer(progressHandler), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressIncrementer"> An <see cref="AggregatingProgressIncrementer"/> object to gather progress deltas.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        internal virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AggregatingProgressIncrementer progressIncrementer, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /*length*/, accessCondition, options, operationContext, progressIncrementer, cancellationToken);
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
            return this.UploadFromStreamAsyncHelper(source, length, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, CancellationToken.None);
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
            return this.UploadFromStreamAsyncHelper(source, length, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, cancellationToken);
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
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, null /*progressHandler*/, CancellationToken.None);
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
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, null /*progressHandler*/, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, new AggregatingProgressIncrementer(progressHandler), cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a block blob. 
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressIncrementer"> An <see cref="AggregatingProgressIncrementer"/> object to gather progress deltas.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        private async Task UploadFromStreamAsyncHelper(Stream source, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AggregatingProgressIncrementer progressIncrementer, CancellationToken cancellationToken)
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

            progressIncrementer = progressIncrementer ?? AggregatingProgressIncrementer.None;

            this.CheckAdjustBlockSize(length ?? (source.CanSeek ? (source.Length - source.Position) : length));
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
                        using (ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(options))
                        {

                            await source.WriteToAsync(cryptoStream, this.ServiceClient.BufferManager, length, null, ChecksumRequested.None, tempExecutionState, null /*streamCopyState*/, cancellationToken).ConfigureAwait(false);
                            cryptoStream.FlushFinalBlock();
                        }

                        // After the tempStream has been written to, we need to seek back to the beginning, so that it can be read from.
                        tempStream.Seek(0, SeekOrigin.Begin);
                        length = tempStream.Length;
                        sourceStream = tempStream;
                    }

                    // Calculate checksum if necessary
                    // Note that we cannot do this while we encrypt, it must be a separate step, because we want the checksum of the encrypted data, 
                    // not the unencrypted data.
                    Checksum contentChecksum = Checksum.None;
                    if (modifiedOptions.ChecksumOptions.StoreContentMD5.Value)
                    {
                        using (ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(modifiedOptions))
                        {
                            StreamDescriptor streamCopyState = new StreamDescriptor();
                            long startPosition = sourceStream.Position;
                            await sourceStream.WriteToAsync(Stream.Null, this.ServiceClient.BufferManager, length, null /* maxLength */, new ChecksumRequested(md5: true, crc64: true), tempExecutionState, streamCopyState, cancellationToken).ConfigureAwait(false);
                            sourceStream.Position = startPosition;
                            contentChecksum.MD5 = streamCopyState.Md5;
                        }
                    }
                    else
                    {
                        // Throw exception if we need to use Transactional MD5 but cannot store it
                        if (modifiedOptions.ChecksumOptions.UseTransactionalMD5.Value)
                        {
                            throw new ArgumentException(SR.PutBlobNeedsStoreBlobContentMD5, "options");
                        }
                    }

                    //if (modifiedOptions.ChecksumOptions.StoreContentCRC64.Value)
                    //{
                    //    using (ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(modifiedOptions))
                    //    {
                    //        StreamDescriptor streamCopyState = new StreamDescriptor();
                    //        long startPosition = sourceStream.Position;
                    //        await sourceStream.WriteToAsync(Stream.Null, this.ServiceClient.BufferManager, length, null /* maxLength */, new ChecksumRequested(md5: true, crc64: true), tempExecutionState, streamCopyState, cancellationToken).ConfigureAwait(false);
                    //        sourceStream.Position = startPosition;
                    //        contentChecksum.CRC64 = streamCopyState.Crc64;
                    //    }
                    //}
                    //else
                    //{
                    //    // Throw exception if we need to use Transactional CRC64 but cannot store it
                    //    if (modifiedOptions.ChecksumOptions.UseTransactionalCRC64.Value)
                    //    {
                    //        throw new ArgumentException(SR.PutBlobNeedsStoreBlobContentCRC64, "options");
                    //    }
                    //}

                    // Execute the put blob.
                    await Executor.ExecuteAsync(
                        this.PutBlobImpl(progressIncrementer.CreateProgressIncrementingStream(sourceStream), length, contentChecksum, accessCondition, modifiedOptions),
                        modifiedOptions.RetryPolicy,
                        operationContext,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                bool useOpenWrite = modifiedOptions.EncryptionPolicy != null
                       || !source.CanSeek
                       || this.streamWriteSizeInBytes < Constants.MinLargeBlockSize
                       || (modifiedOptions.ChecksumOptions.StoreContentMD5.HasValue && modifiedOptions.ChecksumOptions.StoreContentMD5.Value)
                       || (modifiedOptions.ChecksumOptions.StoreContentCRC64.HasValue && modifiedOptions.ChecksumOptions.StoreContentCRC64.Value);

                if (useOpenWrite)
                {
                    using (CloudBlobStream blobStream = this.OpenWrite(accessCondition, modifiedOptions, operationContext))
                    {
                        using (ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(modifiedOptions))
                        {
                            await source.WriteToAsync(progressIncrementer.CreateProgressIncrementingStream(blobStream), this.ServiceClient.BufferManager, length, null /* maxLength */, ChecksumRequested.None, tempExecutionState, null /* streamCopyState */, cancellationToken).ConfigureAwait(false);
                            await blobStream.CommitAsync().ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    // Synchronization mutex required to ensure thread-safe, concurrent operations on related SubStream instances.
                    SemaphoreSlim streamReadThrottler = new SemaphoreSlim(1);
                    await this.UploadFromMultiStreamAsync(
                            this.OpenMultiSubStream(source, length, streamReadThrottler),
                            accessCondition,
                            modifiedOptions,
                            operationContext,
                            progressIncrementer,
                            cancellationToken).ConfigureAwait(false);
                }
            }
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
        /// <remarks>
        /// ## Examples
        /// [!code-csharp[Upload_From_File_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobUploadDownloadTest.cs#sample_UploadBlob_EndToEnd "Upload From File Sample")] 
        /// </remarks>
        [DoesServiceRequest]
        public virtual void UploadFromFile(string path, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("path", path);
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);

            // Determines whether to use the normal, single-stream upload approach or the new parallel, multi-stream strategy.
            bool useSingleStream =
                (modifiedOptions.ChecksumOptions.StoreContentMD5.HasValue && modifiedOptions.ChecksumOptions.StoreContentMD5.Value)
                || (modifiedOptions.ChecksumOptions.StoreContentCRC64.HasValue && modifiedOptions.ChecksumOptions.StoreContentCRC64.Value)
                || modifiedOptions.EncryptionPolicy != null
                || this.streamWriteSizeInBytes < Constants.MinLargeBlockSize;

            if (useSingleStream)
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    this.UploadFromStream(fileStream, accessCondition, modifiedOptions, operationContext);
                }
            }
            else
            {
                this.CheckAdjustBlockSize(new FileInfo(path).Length);
                CommonUtility.RunWithoutSynchronizationContext(
                    () => this.UploadFromMultiStreamAsync(
                        this.OpenMultiFileStream(path),
                        accessCondition,
                        modifiedOptions,
                        operationContext,
                        default(AggregatingProgressIncrementer),
                        CancellationToken.None).Wait());
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
            return this.BeginUploadFromFile(path, accessCondition, options, operationContext, null /*progressHandler*/, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a file to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginUploadFromFile(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromFileAsync(path, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a file to a blob. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadFromFile(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.UploadFromFileAsync(path, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), default(AggregatingProgressIncrementer), CancellationToken.None);
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
            return this.UploadFromFileAsync(path, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), default(AggregatingProgressIncrementer), cancellationToken);
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
            return this.UploadFromFileAsync(path, accessCondition, options, operationContext, default(AggregatingProgressIncrementer), CancellationToken.None);
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
            return this.UploadFromFileAsync(path, accessCondition, options, operationContext, default(AggregatingProgressIncrementer), cancellationToken);
        }


        /// <summary>
        /// Initiates an asynchronous operation to upload a file to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.UploadFromFileAsync(path, accessCondition, options, operationContext, new AggregatingProgressIncrementer(progressHandler), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a file to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressIncrementer"> An <see cref="AggregatingProgressIncrementer"/> object to gather progress deltas.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        internal virtual async Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AggregatingProgressIncrementer progressIncrementer, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);

            // Determines whether to use the normal, single-stream upload approach or the new parallel, multi-stream strategy.
            bool useSingleStream =
                (modifiedOptions.ChecksumOptions.StoreContentMD5.HasValue && modifiedOptions.ChecksumOptions.StoreContentMD5.Value)
                || (modifiedOptions.ChecksumOptions.StoreContentCRC64.HasValue && modifiedOptions.ChecksumOptions.StoreContentCRC64.Value)
                || modifiedOptions.EncryptionPolicy != null
                || this.streamWriteSizeInBytes < Constants.MinLargeBlockSize;

            if (useSingleStream)
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    await this.UploadFromStreamAsync(fileStream, accessCondition, modifiedOptions, operationContext, progressIncrementer, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                this.CheckAdjustBlockSize(new FileInfo(path).Length);
                await this.UploadFromMultiStreamAsync(
                        this.OpenMultiFileStream(path),
                        accessCondition,
                        modifiedOptions,
                        operationContext,
                        progressIncrementer,
                        cancellationToken).ConfigureAwait(false);
            }
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
            return this.BeginUploadFromByteArray(buffer, index, count, accessCondition, options, operationContext, null /*progerssHandler*/, callback, state);
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
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginUploadFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload the contents of a byte array to a blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadFromByteArray(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.UploadFromByteArrayAsync(buffer, index, count, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, CancellationToken.None);
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
            return this.UploadFromByteArrayAsync(buffer, index, count, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, cancellationToken);
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
            return this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, null /*progressHandler*/, CancellationToken.None);
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
            return this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, null /*progressHandler*/, cancellationToken);
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
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual async Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            using (SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count))
            {
                await this.UploadFromStreamAsync(stream, accessCondition, options, operationContext, progressHandler, cancellationToken).ConfigureAwait(false);
            }
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
            return this.BeginUploadText(content, encoding, accessCondition, options, operationContext, null /*progressHandler*/, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginUploadText(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, progressHandler, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a string of text to a blob. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadText(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.UploadTextAsync(content, null /*encoding*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, CancellationToken.None);
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
            return this.UploadTextAsync(content, null /*encoding*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, cancellationToken);
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
            return this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, null /*progressHandler*/, CancellationToken.None);
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
            return this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, null /*progressHandler*/, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual async Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            await this.UploadFromByteArrayAsync(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext, progressHandler, cancellationToken).ConfigureAwait(false);
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
            return this.BeginDownloadText(encoding, accessCondition, options, operationContext, null /*progressHandler*/, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginDownloadText(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.DownloadTextAsync(encoding, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>The contents of the blob, as a string.</returns>
        public virtual string EndDownloadText(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<string>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync()
        {
            return this.DownloadTextAsync(null /*encoding*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(CancellationToken cancellationToken)
        {
            return this.DownloadTextAsync(null /*encoding*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), null /*progressHandler*/, cancellationToken);
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
            return this.DownloadTextAsync(encoding, accessCondition, options, operationContext, null /*progressHandler*/, CancellationToken.None);
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
            return this.DownloadTextAsync(encoding, accessCondition, options, operationContext, null /*progressHandler*/, cancellationToken);
        }
        /// <summary>
        /// Initiates an asynchronous operation to download the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual async Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            using (SyncMemoryStream stream = new SyncMemoryStream())
            {
                await this.DownloadToStreamAsync(stream, accessCondition, options, operationContext, progressHandler, cancellationToken).ConfigureAwait(false);
                byte[] streamAsBytes = stream.GetBuffer();
                return (encoding ?? Encoding.UTF8).GetString(streamAsBytes, 0, (int)stream.Length);
            }
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentChecksum">A hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Clients may send the content checksum headers for a given operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentChecksum"/> parameter permits clients who already have access to a pre-computed checksum value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> or <see cref="P:BlobRequestOptions.UseTransactionalCrc64"/> properties are set to <c>true</c> and the corresponding content parameter is set 
        /// to <c>null</c>, then the client library will calculate the checksum value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void PutBlock(string blockId, Stream blockData, Checksum contentChecksum, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("blockData", blockData);

            contentChecksum = contentChecksum ?? Checksum.None;

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            ChecksumRequested requiresContentChecksum = new ChecksumRequested(
                md5: string.IsNullOrEmpty(contentChecksum.MD5) && modifiedOptions.ChecksumOptions.UseTransactionalMD5.Value,
                crc64: string.IsNullOrEmpty(contentChecksum.CRC64) && modifiedOptions.ChecksumOptions.UseTransactionalCRC64.Value
                );
            operationContext = operationContext ?? new OperationContext();

            Stream seekableStream = blockData;
            bool seekableStreamCreated = false;

            try
            {
                if (!blockData.CanSeek || requiresContentChecksum.HasAny)
                {
                    ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(modifiedOptions);

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
                    blockData.WriteToSync(writeToStream, null /* copyLength */, Constants.MaxBlockSize, requiresContentChecksum, true, tempExecutionState, streamCopyState);
                    seekableStream.Position = startPosition;

                    contentChecksum = new Checksum(
                        md5: requiresContentChecksum.MD5 ? streamCopyState.Md5 : default(string),
                        crc64: requiresContentChecksum.CRC64 ? streamCopyState.Crc64 : default(string)
                        );
                }

                Executor.ExecuteSync(
                    this.PutBlockImpl(seekableStream, blockId, contentChecksum, accessCondition, modifiedOptions),
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

        /// <summary>
        /// Uploads a single block, copying from a source URI.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void PutBlock(string blockId, Uri sourceUri, long? offset, long? count, string contentMD5, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.PutBlock(blockId, sourceUri, offset, count, new Checksum(md5: contentMD5), accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a single block, copying from a source URI.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentChecksum">A hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Clients may send the content checksum headers for a given operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentChecksum"/> parameter permits clients who already have access to a pre-computed checksum value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> or <see cref="P:BlobRequestOptions.UseTransactionalCrc64"/> properties are set to <c>true</c> and the corresponding content parameter is set 
        /// to <c>null</c>, then the client library will calculate the checksum value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void PutBlock(string blockId, Uri sourceUri, long? offset, long? count, Checksum contentChecksum, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("sourceUri", sourceUri);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);

            Executor.ExecuteSync(
                this.PutBlockImpl(sourceUri, offset, count, contentChecksum, blockId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentChecksum">A hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the content checksum headers for a given operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentChecksum"/> parameter permits clients who already have access to a pre-computed checksum value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> or <see cref="P:BlobRequestOptions.UseTransactionalCrc64"/> properties are set to <c>true</c> and the corresponding content parameter is set 
        /// to <c>null</c>, then the client library will calculate the checksum value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginPutBlock(string blockId, Stream blockData, Checksum contentChecksum, AsyncCallback callback, object state)
        {
            return this.BeginPutBlock(blockId, blockData, contentChecksum, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentChecksum">A hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the content checksum headers for a given operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentChecksum"/> parameter permits clients who already have access to a pre-computed checksum value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> or <see cref="P:BlobRequestOptions.UseTransactionalCrc64"/> properties are set to <c>true</c> and the corresponding content parameter is set 
        /// to <c>null</c>, then the client library will calculate the checksum value internally.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginPutBlock(string blockId, Stream blockData, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginPutBlock(blockId, blockData, contentChecksum, accessCondition, options, operationContext, null /*progressHanlder*/, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentChecksum">A hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> An <see cref="IProgress{StorageProgress}"/> object to gather progress deltas.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the content checksum headers for a given operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentChecksum"/> parameter permits clients who already have access to a pre-computed checksum value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> or <see cref="P:BlobRequestOptions.UseTransactionalCrc64"/> properties are set to <c>true</c> and the corresponding content parameter is set 
        /// to <c>null</c>, then the client library will calculate the checksum value internally.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginPutBlock(string blockId, Stream blockData, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.PutBlockAsync(blockId, blockData, contentChecksum, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentChecksum">A hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the content checksum headers for a given operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentChecksum"/> parameter permits clients who already have access to a pre-computed checksum value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> or <see cref="P:BlobRequestOptions.UseTransactionalCrc64"/> properties are set to <c>true</c> and the corresponding content parameter is set 
        /// to <c>null</c>, then the client library will calculate the checksum value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginPutBlock(string blockId, Uri sourceUri, long? offset, long? count, Checksum contentChecksum, AsyncCallback callback, object state)
        {
            return this.BeginPutBlock(blockId, sourceUri, offset, count, contentChecksum, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentChecksum">A hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the content checksum headers for a given operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentChecksum"/> parameter permits clients who already have access to a pre-computed checksum value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> or <see cref="P:BlobRequestOptions.UseTransactionalCrc64"/> properties are set to <c>true</c> and the corresponding content parameter is set 
        /// to <c>null</c>, then the client library will calculate the checksum value internally.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginPutBlock(string blockId, Uri sourceUri, long? offset, long? count, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("sourceUri", sourceUri);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            StorageAsyncResult<NullType> storageAsyncResult = new StorageAsyncResult<NullType>(callback, state);

            ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(modifiedOptions);

            storageAsyncResult.CancelDelegate = tempExecutionState.Cancel;

            try
            {
                this.PutBlockHandler(blockId, sourceUri, offset, count, contentChecksum, accessCondition, modifiedOptions, operationContext, storageAsyncResult);
            }
            catch (Exception e)
            {
                storageAsyncResult.OnComplete(e);
            }

            return storageAsyncResult;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        private void PutBlockHandler(string blockId, Uri sourceUri, long? offset, long? count, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, StorageAsyncResult<NullType> storageAsyncResult)
        {
            lock (storageAsyncResult.CancellationLockerObject)
            {
                ICancellableAsyncResult result = CancellableAsyncResultTaskWrapper.Create(
                    token => this.PutBlockAsync(blockId, sourceUri, offset, count, contentChecksum, accessCondition, options, operationContext, token),
                    ar =>
                    {
                        storageAsyncResult.UpdateCompletedSynchronously(ar.CompletedSynchronously);

                        try
                        {
                            ((CancellableAsyncResultTaskWrapper<AccountProperties>)(ar)).GetAwaiter().GetResult();
                            storageAsyncResult.OnComplete();
                        }
                        catch (Exception e)
                        {
                            storageAsyncResult.OnComplete(e);
                        }
                    },
                    default(object) /* asyncState */);

                storageAsyncResult.CancelDelegate = result.Cancel;
                if (storageAsyncResult.CancelRequested)
                {
                    storageAsyncResult.Cancel();
                }
            }
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndPutBlock(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
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
            return this.PutBlockAsync(blockId, blockData, new Checksum(md5: contentMD5));
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, Checksum contentChecksum = null)
        {
            return this.PutBlockAsync(blockId, blockData, contentChecksum, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), default(AggregatingProgressIncrementer), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Uri sourceUri, long? offset, long? count, string contentMD5)
        {
            return this.PutBlockAsync(blockId, sourceUri, offset, count, new Checksum(md5: contentMD5));
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Uri sourceUri, long? offset, long? count, Checksum contentChecksum = null)
        {
            return this.PutBlockAsync(blockId, sourceUri, offset, count, contentChecksum, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
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
            return this.PutBlockAsync(blockId, blockData, new Checksum(md5: contentMD5), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        internal Task PutBlockAsync(string blockId, Stream blockData, Checksum contentChecksum, CancellationToken cancellationToken)
        {
            return this.PutBlockAsync(blockId, blockData, contentChecksum, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Uri sourceUri, long? offset, long? count, string contentMD5, CancellationToken cancellationToken)
        {
            return this.PutBlockAsync(blockId, sourceUri, offset, count, new Checksum(md5: contentMD5), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Uri sourceUri, long? offset, long? count, Checksum contentChecksum, CancellationToken cancellationToken)
        {
            return this.PutBlockAsync(blockId, sourceUri, offset, count, contentChecksum, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
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
            return this.PutBlockAsync(blockId, blockData, new Checksum(md5: contentMD5), accessCondition, options, operationContext);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.PutBlockAsync(blockId, blockData, contentChecksum, accessCondition, options, operationContext, default(AggregatingProgressIncrementer), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Uri sourceUri, long? offset, long? count, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.PutBlockAsync(blockId, sourceUri, offset, count, new Checksum(md5: contentMD5), accessCondition, options, operationContext);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Uri sourceUri, long? offset, long? count, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.PutBlockAsync(blockId, sourceUri, offset, count, contentChecksum, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
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
            return this.PutBlockAsync(blockId, blockData, new Checksum(md5: contentMD5), accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        internal Task PutBlockAsync(string blockId, Stream blockData, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.PutBlockAsync(blockId, blockData, contentChecksum, accessCondition, options, operationContext, default(AggregatingProgressIncrementer), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Uri sourceUri, long? offset, long? count, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.PutBlockAsync(blockId, sourceUri, offset, count, new Checksum(md5: contentMD5), accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Uri sourceUri, long? offset, long? count, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("sourceUri", sourceUri);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            return Executor.ExecuteAsync(
                        this.PutBlockImpl(sourceUri, offset, count, contentChecksum, blockId, accessCondition, modifiedOptions),
                        modifiedOptions.RetryPolicy,
                        operationContext,
                        cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Put Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.PutBlockAsync(blockId, blockData, new Checksum(md5: contentMD5), accessCondition, options, operationContext, progressHandler, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.PutBlockAsync(blockId, blockData, contentChecksum, accessCondition, options, operationContext, new AggregatingProgressIncrementer(progressHandler), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a single block.
        /// </summary>
        /// <param name="blockId">A Base64-encoded string that identifies the block.</param>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressIncrementer"> An <see cref="AggregatingProgressIncrementer"/> object to gather progress deltas.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        private async Task PutBlockAsync(string blockId, Stream blockData, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AggregatingProgressIncrementer progressIncrementer, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("blockData", blockData);

            contentChecksum = contentChecksum ?? Checksum.None;

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            ChecksumRequested requiresContentChecksum = new ChecksumRequested(
                md5: string.IsNullOrEmpty(contentChecksum?.MD5) && modifiedOptions.ChecksumOptions.UseTransactionalMD5.Value,
                crc64: string.IsNullOrEmpty(contentChecksum?.CRC64) && modifiedOptions.ChecksumOptions.UseTransactionalCRC64.Value
                );
            operationContext = operationContext ?? new OperationContext();

            progressIncrementer = progressIncrementer ?? AggregatingProgressIncrementer.None;

            Stream seekableStream = blockData;
            bool seekableStreamCreated = false;

            try
            {
                if (!blockData.CanSeek || requiresContentChecksum.HasAny )
                {
                    ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(modifiedOptions);

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
                    await blockData.WriteToAsync(writeToStream, this.ServiceClient.BufferManager, null /* copyLength */, Constants.MaxBlockSize, requiresContentChecksum, tempExecutionState, streamCopyState, cancellationToken).ConfigureAwait(false);
                    seekableStream.Position = startPosition;

                    contentChecksum = new Checksum(
                        md5: requiresContentChecksum.MD5 ? streamCopyState.Md5 : default(string),
                        crc64: requiresContentChecksum.CRC64 ? streamCopyState.Crc64 : default(string)
                        );
                }

                await Executor.ExecuteAsync(
                    this.PutBlockImpl(progressIncrementer.CreateProgressIncrementingStream(seekableStream), blockId, contentChecksum, accessCondition, modifiedOptions),
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
        /// Sets the tier of the blob on a standard storage account.
        /// </summary>
        /// <param name="standardBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void SetStandardBlobTier(StandardBlobTier standardBlobTier, RehydratePriority? rehydratePriority = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            Executor.ExecuteSync(
                this.SetStandardBlobTierImpl(standardBlobTier, rehydratePriority, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to set the tier of the blob on a standard storage account.
        /// </summary>
        /// <param name="standardBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetStandardBlobTier(StandardBlobTier standardBlobTier, AsyncCallback callback, object state)
        {
            return this.BeginSetStandardBlobTier(standardBlobTier, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to set the tier of the blob on a standard storage account.
        /// </summary>
        /// <param name="standardBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginSetStandardBlobTier(StandardBlobTier standardBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.SetStandardBlobTierAsync(standardBlobTier, default(RehydratePriority?), accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to set the tier of the blob on a standard storage account.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndSetStandardBlobTier(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to set the tier of the blob on a standard storage account.
        /// </summary>
        /// <param name="standardBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetStandardBlobTierAsync(StandardBlobTier standardBlobTier)
        {
            return this.SetStandardBlobTierAsync(standardBlobTier, default(RehydratePriority?), default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to set the tier of the blob on a standard storage account.
        /// </summary>
        /// <param name="standardBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetStandardBlobTierAsync(StandardBlobTier standardBlobTier, CancellationToken cancellationToken)
        {
            return this.SetStandardBlobTierAsync(standardBlobTier, default(RehydratePriority?), default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to set the tier of the blob on a standard storage account.
        /// </summary>
        /// <param name="standardBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetStandardBlobTierAsync(StandardBlobTier standardBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.SetStandardBlobTierAsync(standardBlobTier, default(RehydratePriority?), accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to set the tier of the blob on a standard storage account.
        /// </summary>
        /// <param name="standardBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task SetStandardBlobTierAsync(StandardBlobTier standardBlobTier, RehydratePriority? rehydratePriority, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.SetStandardBlobTierImpl(standardBlobTier, rehydratePriority, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Begins an operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param> 
        /// <param name="standardBlockBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="rehydratePriority">The priority with which to rehydrate an archived blob.</param>
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
        public virtual string StartCopy(CloudBlockBlob source, StandardBlobTier? standardBlockBlobTier = null, RehydratePriority? rehydratePriority = null, AccessCondition sourceAccessCondition = null, AccessCondition destAccessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            return this.StartCopy(source, Checksum.None, false /* syncCopy */, standardBlockBlobTier, rehydratePriority, sourceAccessCondition, destAccessCondition, options, operationContext);
        }

        /// <summary>
        /// Begins an operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <param name="contentChecksum">An hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>
        /// <param name="syncCopy">A boolean to enable synchronous server copy of blobs.</param>
        /// <param name="standardBlockBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="rehydratePriority">The priority with which to rehydrate an archived blob.</param>
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
        public string StartCopy(CloudBlockBlob source, Checksum contentChecksum, bool syncCopy,  StandardBlobTier? standardBlockBlobTier, RehydratePriority? rehydratePriority, AccessCondition sourceAccessCondition = null, AccessCondition destAccessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            return this.StartCopy(CloudBlob.SourceBlobToUri(source), contentChecksum, syncCopy, default(PremiumPageBlobTier?), standardBlockBlobTier, rehydratePriority, sourceAccessCondition, destAccessCondition, options, operationContext);
        }
#endif
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
            return this.BeginStartCopy(source, default(StandardBlobTier?) /*standardBlockBlobTier*/, sourceAccessCondition, destAccessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <param name="standardBlockBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>        
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(CloudBlockBlob source, StandardBlobTier? standardBlockBlobTier, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(source, default(string) /* contentMD5 */, false /* incrementalCopy */, false /* syncCopy */, standardBlockBlobTier, sourceAccessCondition, destAccessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity. May be <c>null</c> or an empty string.</param>
        /// <param name="syncCopy">A boolean to enable synchronous server copy of blobs.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>        
        [DoesServiceRequest]
        private ICancellableAsyncResult BeginStartCopy(CloudBlockBlob source, string contentMD5, bool incrementalCopy, bool syncCopy, StandardBlobTier? standardBlockBlobTier, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(CloudBlob.SourceBlobToUri(source), contentMD5, incrementalCopy, syncCopy, default(PremiumPageBlobTier?), standardBlockBlobTier, sourceAccessCondition, destAccessCondition, options, operationContext, callback, state);
        }

#if TASK

        /// <summary>
        /// Initiates an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudBlockBlob source)
        {
            return this.StartCopyAsync(source, default(StandardBlobTier?), default(RehydratePriority?), default(AccessCondition) /*sourceAccessCondition*/, default(AccessCondition) /*destAccessCondition*/, default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
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
            return this.StartCopyAsync(source, default(StandardBlobTier?), default(RehydratePriority?), default(AccessCondition) /*sourceAccessCondition*/, default(AccessCondition) /*destAccessCondition*/, default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            return this.StartCopyAsync(source, default(StandardBlobTier?), default(RehydratePriority?), sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <param name="standardBlockBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="rehydratePriority">The priority with which to rehydrate an archived blob.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudBlockBlob source, StandardBlobTier? standardBlockBlobTier, RehydratePriority? rehydratePriority, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(source, Checksum.None, false /* incrementalCopy */, false /* syncCopy */, standardBlockBlobTier, rehydratePriority, sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another block blob's contents, properties, and metadata to this block blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudBlockBlob"/> object.</param>
        /// <param name="contentChecksum">A hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>        /// <param name="syncCopy">A boolean to enable synchronous server copy of blobs.</param>
        /// <param name="incrementalCopy">A boolean indicating whether or not this is an incremental copy.</param>
        /// <param name="standardBlockBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set. Only valid on block blobs.</param>
        /// <param name="rehydratePriority">The priority with which to rehydrate an archived blob.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public Task<string> StartCopyAsync(CloudBlockBlob source, Checksum contentChecksum, bool incrementalCopy, bool syncCopy, StandardBlobTier? standardBlockBlobTier, RehydratePriority? rehydratePriority, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("source", source);
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.StartCopyImpl(this.attributes, CloudBlob.SourceBlobToUri(source), contentChecksum, incrementalCopy, syncCopy, default(PremiumPageBlobTier?), standardBlockBlobTier, rehydratePriority, sourceAccessCondition, destAccessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

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
            return CancellableAsyncResultTaskWrapper.Create(token => DownloadBlockListAsync(blockListingFilter, accessCondition, options, operationContext), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to return an enumerable collection of the blob's blocks, 
        /// using the specified block list filter.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>An enumerable collection of objects implementing <see cref="ListBlockItem"/>.</returns>
        public virtual IEnumerable<ListBlockItem> EndDownloadBlockList(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<IEnumerable<ListBlockItem>>)asyncResult).GetAwaiter().GetResult();
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
            return this.DownloadBlockListAsync(BlockListingFilter.Committed, null /* AccessCondition */, null /* BlobRequestOptions */, null /* OperationContext */, cancellationToken);
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
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.GetBlockListImpl(blockListingFilter, accessCondition, modifiedOptions),
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.CreateSnapshotAsync(metadata, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="CloudBlockBlob"/> object that is a blob snapshot.</returns>
        public virtual CloudBlockBlob EndCreateSnapshot(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<CloudBlockBlob>)asyncResult).GetAwaiter().GetResult();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlockBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlockBlob> CreateSnapshotAsync()
        {
            return this.CreateSnapshotAsync(null /*metadata*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlockBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlockBlob> CreateSnapshotAsync(CancellationToken cancellationToken)
        {
            return this.CreateSnapshotAsync(null /*metadata*/, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.CreateSnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.PutBlockListAsync(blockList, accessCondition, options, operationContext, token), callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a list of blocks to a new or existing blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndPutBlockList(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
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
            return this.PutBlockListAsync(blockList, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), CancellationToken.None);
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
            return this.PutBlockListAsync(blockList, default(AccessCondition), default(BlobRequestOptions), default(OperationContext), cancellationToken);
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
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            IEnumerable<PutBlockListItem> items = blockList.Select(i => new PutBlockListItem(i, BlockSearchMode.Latest));
            return Executor.ExecuteAsync(
                this.PutBlockListImpl(items, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }
#endif

        /// <summary>
        /// Uploads the full blob from a seekable stream.
        /// </summary>
        /// <param name="stream">The content stream. Must be seekable.</param>
        /// <param name="length">Number of bytes to upload from the content stream starting at its current position.</param>
        /// <param name="contentChecksum">The content checksum.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that gets the stream.</returns>
        private RESTCommand<NullType> PutBlobImpl(Stream stream, long? length, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options)
        {
            long offset = stream.Position;
            length = length ?? stream.Length - offset;
            this.Properties.ContentChecksum = contentChecksum;

            CappedLengthReadOnlyStream cappedStream = new CappedLengthReadOnlyStream(stream, length.Value + offset);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(cappedStream, offset, length, Checksum.None, cmd, ctx);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                BlobRequest.VerifyHttpsCustomerProvidedKey(uri, options);
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.Put(uri, serverTimeout, this.Properties, BlobType.BlockBlob, 0, null, accessCondition, cnt, ctx, 
                    this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials, options);
                BlobHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                this.Properties.Length = length.Value;
                BlobResponse.ValidateCPKHeaders(resp, options, true);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Uploads the block.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="blockId">The block ID.</param>
        /// <param name="contentChecksum">The content checksum.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that uploads the block.</returns>
        internal RESTCommand<NullType> PutBlockImpl(Stream source, string blockId, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options)
        {
            options.AssertNoEncryptionPolicyOrStrictMode();

            long offset = source.Position;
            long length = source.Length - offset;

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(source, offset, length, contentChecksum, cmd, ctx);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => 
            {
                BlobRequest.VerifyHttpsCustomerProvidedKey(uri, options);
                return BlobHttpRequestMessageFactory.PutBlock(uri, serverTimeout, blockId, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials, options);
            };
            
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                cmd.CurrentResult.EncryptionKeySHA256 = HttpResponseParsers.ParseEncryptionKeySHA256(resp);
                BlobResponse.ValidateCPKHeaders(resp, options, true);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Uploads the block from a source Uri.
        /// </summary>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset at which to begin returning content.</param>
        /// <param name="count">The number of bytes to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="contentChecksum">A hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>
        /// <param name="blockId">The block ID.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that uploads the block.</returns>
        internal RESTCommand<NullType> PutBlockImpl(Uri sourceUri, long? offset, long? count, Checksum contentChecksum, string blockId, AccessCondition accessCondition, BlobRequestOptions options)
        {
            options.AssertNoEncryptionPolicyOrStrictMode();

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);

            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => 
            {
                BlobRequest.VerifyHttpsCustomerProvidedKey(uri, options);
                return BlobHttpRequestMessageFactory.PutBlock(uri, sourceUri, offset, count, contentChecksum, serverTimeout, blockId, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials, options);
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                cmd.CurrentResult.EncryptionKeySHA256 = HttpResponseParsers.ParseEncryptionKeySHA256(resp);
                BlobResponse.ValidateCPKHeaders(resp, options, true);
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
            Checksum contentChecksum = new Checksum(
                md5: (options.ChecksumOptions.UseTransactionalMD5.HasValue && options.ChecksumOptions.UseTransactionalMD5.Value) ? memoryStream.ComputeMD5Hash() : default(string),
                crc64: (options.ChecksumOptions.UseTransactionalCRC64.HasValue && options.ChecksumOptions.UseTransactionalCRC64.Value) ? memoryStream.ComputeCRC64Hash() : default(string)
                );

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, contentChecksum, cmd, ctx);
            putCmd.StreamToDispose = memoryStream;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                BlobRequest.VerifyHttpsCustomerProvidedKey(uri, options);
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.PutBlockList(uri, serverTimeout, this.Properties, accessCondition, cnt, ctx,
                    this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials, options);
                BlobHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                cmd.CurrentResult.EncryptionKeySHA256 = HttpResponseParsers.ParseEncryptionKeySHA256(resp);
                this.Properties.Length = -1;
                BlobResponse.ValidateCPKHeaders(resp, options, true);
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
            RESTCommand<IEnumerable<ListBlockItem>> getCmd = new RESTCommand<IEnumerable<ListBlockItem>>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetBlockList(uri, serverTimeout, this.SnapshotTime, typesOfBlocks, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) =>
            {
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, true);
                return GetBlockListResponse.ParseAsync(cmd.ResponseStream, ct);
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
            RESTCommand<CloudBlockBlob> putCmd = new RESTCommand<CloudBlockBlob>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.Snapshot(uri, serverTimeout, accessCondition, cnt, ctx, 
                    this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials, options);
                if (metadata != null)
                {
                    BlobHttpRequestMessageFactory.AddMetadata(msg, metadata);
                }

                return msg;
            };

            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, null /* retVal */, cmd, ex);
                DateTimeOffset snapshotTime = NavigationHelper.ParseSnapshotTime(BlobHttpResponseParsers.GetSnapshotTime(resp));
                CloudBlockBlob snapshot = new CloudBlockBlob(this.Name, snapshotTime, this.Container);
                snapshot.attributes.Metadata = new Dictionary<string, string>(metadata ?? this.Metadata, StringComparer.OrdinalIgnoreCase);
                snapshot.attributes.Properties = new BlobProperties(this.Properties);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(snapshot.attributes, resp, false);
                return snapshot;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation method for the SetBlobTier methods.
        /// </summary>
        /// <param name="standardBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="rehydratePriority">The priority with which to rehydrate an archived blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that sets the blob tier.</returns>
        internal RESTCommand<NullType> SetStandardBlobTierImpl(StandardBlobTier standardBlobTier, RehydratePriority? rehydratePriority, AccessCondition accessCondition, BlobRequestOptions options)        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.SetBlobTier(uri, serverTimeout, standardBlobTier.ToString(), rehydratePriority, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                // OK is returned when the tier on the blob is done immediately while accepted occurs when the process of setting the tier has started but not completed.
                HttpStatusCode[] expectedHttpStatusCodes = new HttpStatusCode[2];
                expectedHttpStatusCodes[0] = HttpStatusCode.OK;
                expectedHttpStatusCodes[1] = HttpStatusCode.Accepted;
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(expectedHttpStatusCodes, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);

                this.attributes.Properties.RehydrationStatus = null;
                this.attributes.Properties.BlobTierInferred = false;
                if (resp.StatusCode.Equals(HttpStatusCode.OK))
                {
                    this.attributes.Properties.StandardBlobTier = standardBlobTier;
                }
                else
                {
                    this.attributes.Properties.StandardBlobTier = StandardBlobTier.Archive;
                }

                return NullType.Value;
            };

            return putCmd;
        }

        private static bool IsLessThanSingleBlobThreshold(Stream source, long? length, BlobRequestOptions modifiedOptions, bool noPadding)
        {
            if (!source.CanSeek)
            {
                return false;
            }

            length = length ?? (source.Length - source.Position);

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

        /// <summary>
        /// Helper method to determine whether we should continue with an OpenWrite operation.
        /// 
        /// When we are opening a stream for writing, if there is an access condition, we first try a FetchAttributes on the blob.
        /// The purpose of this is to fail fast in the case where the access condition would fail the request at the very end.  (Access
        /// conditions aren't checked for PutBlock, only PutBlockList.)
        /// 
        /// If the FetchAttributes call succeeded, we should continue with the OpenWrite operation.  If the FetchAttributes call failed,
        /// we need to check if the failure is one of the allowed failure modes.  This method does that check.
        /// </summary>
        /// <param name="exception">The exception received from the FetchAttributes call.</param>
        /// <param name="accessCondition">The access condition used on the FetchAttributes call.</param>
        /// <returns>True if the operation should continue, false if the exception should be re-thrown.</returns>
        private static bool ContinueOpenWriteOnFailure(StorageException exception, AccessCondition accessCondition)
        {
            // If we don't have any request information, don't continue.
            if (exception.RequestInformation == null) return false;

            // If we got a 404 and the access condition was not an If-Match, continue.  (We don't want an if-none-match to interfere with the case where the blob doesn't exist)
            if ((exception.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound) && string.IsNullOrEmpty(accessCondition.IfMatchETag)) return true;

            // If we got a 403, continue.  This is to account for the case where our credentials give write, but not read permission.
            if (exception.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Forbidden) return true;

            return false;
        }
    }
}
