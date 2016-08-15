//-----------------------------------------------------------------------
// <copyright file="CloudAppendBlob.cs" company="Microsoft">
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
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class CloudAppendBlob : CloudBlob, ICloudBlob
    {
#if SYNC
        /// <summary>
        /// Opens a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="CloudBlobStream"/> object.</returns>
        /// <remarks>
        /// Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributes(AccessCondition, BlobRequestOptions, OperationContext)"/> method under the covers.
        /// Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual CloudBlobStream OpenWrite(bool createNew, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, this.BlobType, this.ServiceClient, false);

#if !(WINDOWS_RT || NETCORE )
            ICryptoTransform transform = null;
#endif

            if (createNew)
            {
#if !(WINDOWS_RT || NETCORE )
                if (options != null && options.EncryptionPolicy != null)
                {
                    transform = options.EncryptionPolicy.CreateAndSetEncryptionContext(this.Metadata, false /* noPadding */);
                }
#endif
                this.CreateOrReplace(accessCondition, options, operationContext);
            }
            else
            {
                if (modifiedOptions.StoreBlobContentMD5.Value)
                {
                    throw new ArgumentException(SR.MD5NotPossible);
                }

#if !(WINDOWS_RT || NETCORE )
                if (modifiedOptions.EncryptionPolicy != null)
                {
                    throw new ArgumentException(SR.EncryptionNotSupportedForExistingBlobs);
                }
#endif
                // Although we don't need any properties from the service, we should make this call in order to honor the user specified conditional headers
                // while opening an existing stream and to get the append position for an existing blob if user didn't specify one.
                this.FetchAttributes(accessCondition, options, operationContext);
            }

            if (accessCondition != null)
            {
                accessCondition = new AccessCondition() { LeaseId = accessCondition.LeaseId, IfAppendPositionEqual = accessCondition.IfAppendPositionEqual, IfMaxSizeLessThanOrEqual = accessCondition.IfMaxSizeLessThanOrEqual };
            }

#if !(WINDOWS_RT || NETCORE )
            if (modifiedOptions.EncryptionPolicy != null)
            {
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
        /// Begins an asynchronous operation to open a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Note that this method always makes a call to the <see cref="CloudBlob.BeginFetchAttributes(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/> method under the covers.
        /// Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginOpenWrite(bool createNew, AsyncCallback callback, object state)
        {
            return this.BeginOpenWrite(createNew, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to open a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Note that this method always makes a call to the <see cref="CloudBlob.BeginFetchAttributes(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/> method under the covers.
        /// Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginOpenWrite(bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, this.BlobType, this.ServiceClient, false);

            StorageAsyncResult<CloudBlobStream> storageAsyncResult = new StorageAsyncResult<CloudBlobStream>(callback, state);
            ICancellableAsyncResult result;

#if !(WINDOWS_RT || NETCORE )
            ICryptoTransform transform = null;
#endif

            if (createNew)
            {
#if !(WINDOWS_RT || NETCORE )
                if (options != null && options.EncryptionPolicy != null)
                {
                    transform = options.EncryptionPolicy.CreateAndSetEncryptionContext(this.Metadata, false /* noPadding */);
                }
#endif
                result = this.BeginCreateOrReplace(
                    accessCondition,
                    options,
                    operationContext,
                    ar =>
                    {
                        storageAsyncResult.UpdateCompletedSynchronously(ar.CompletedSynchronously);

                        try
                        {
                            this.EndCreateOrReplace(ar);

                            if (accessCondition != null)
                            {
                                accessCondition = new AccessCondition() { LeaseId = accessCondition.LeaseId, IfAppendPositionEqual = accessCondition.IfAppendPositionEqual, IfMaxSizeLessThanOrEqual = accessCondition.IfMaxSizeLessThanOrEqual };
                            }

#if !(WINDOWS_RT || NETCORE )
                            if (modifiedOptions.EncryptionPolicy != null)
                            {
                                storageAsyncResult.Result = new BlobEncryptedWriteStream(this, accessCondition, modifiedOptions, operationContext, transform);
                            }
                            else
#endif
                            {
                                storageAsyncResult.Result = new BlobWriteStream(this, accessCondition, modifiedOptions, operationContext);
                            }

                            storageAsyncResult.OnComplete();
                        }
                        catch (Exception e)
                        {
                            storageAsyncResult.OnComplete(e);
                        }
                    },
                    null /* state */);
            }
            else
            {
                if (modifiedOptions.StoreBlobContentMD5.Value)
                {
                    throw new ArgumentException(SR.MD5NotPossible);
                }

#if !(WINDOWS_RT || NETCORE )
                if (modifiedOptions.EncryptionPolicy != null)
                {
                    throw new ArgumentException(SR.EncryptionNotSupportedForExistingBlobs);
                }
#endif
                // Although we don't need any properties from the service, we should make this call in order to honor the user specified conditional headers
                // while opening an existing stream and to get the append position for an existing blob if user didn't specify one.
                result = this.BeginFetchAttributes(
                    accessCondition,
                    options,
                    operationContext,
                    ar =>
                    {
                        storageAsyncResult.UpdateCompletedSynchronously(ar.CompletedSynchronously);

                        try
                        {
                            this.EndFetchAttributes(ar);

                            if (accessCondition != null)
                            {
                                accessCondition = new AccessCondition() { LeaseId = accessCondition.LeaseId, IfAppendPositionEqual = accessCondition.IfAppendPositionEqual };
                            }

                            storageAsyncResult.Result = new BlobWriteStream(this, accessCondition, modifiedOptions, operationContext);
                            storageAsyncResult.OnComplete();
                        }
                        catch (Exception e)
                        {
                            storageAsyncResult.OnComplete(e);
                        }
                    },
                    null /* state */);
            }

            storageAsyncResult.CancelDelegate = result.Cancel;
            return storageAsyncResult;
        }

        /// <summary>
        /// Ends an asynchronous operation to open a stream for writing to the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <returns>A <see cref="CloudBlobStream"/> object.</returns>
        public virtual CloudBlobStream EndOpenWrite(IAsyncResult asyncResult)
        {
            StorageAsyncResult<CloudBlobStream> storageAsyncResult = (StorageAsyncResult<CloudBlobStream>)asyncResult;
            storageAsyncResult.End();
            return storageAsyncResult.Result;
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to open a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlobStream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Note that this method always makes a call to the <see cref="CloudBlob.BeginFetchAttributes(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/> method under the covers.
        /// Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(bool createNew)
        {
            return this.OpenWriteAsync(createNew, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to open a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlobStream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.
        /// Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(bool createNew, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginOpenWrite, this.EndOpenWrite, createNew, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to open a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlobStream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.
        /// Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.OpenWriteAsync(createNew, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to open a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudBlobStream"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.
        /// Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginOpenWrite, this.EndOpenWrite, createNew, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStream(Stream, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void UploadFromStream(Stream source, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.UploadFromStreamHelper(source, null /* length */, true /* createNew */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStream(Stream, long, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void UploadFromStream(Stream source, long length, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.UploadFromStreamHelper(source, length, true /* createNew */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void AppendFromStream(Stream source, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.UploadFromStreamHelper(source, null /* length */, false /* createNew */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void AppendFromStream(Stream source, long length, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.UploadFromStreamHelper(source, length, false /* createNew */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="createNew"><c>true</c> if the append blob is newly created, <c>false</c> otherwise.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        internal void UploadFromStreamHelper(Stream source, long? length, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
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
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            using (CloudBlobStream blobStream = this.OpenWrite(createNew, accessCondition, modifiedOptions, operationContext))
            {
                using (ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions))
                {
                    source.WriteToSync(blobStream, length, null /* maxLength */, false, true, tempExecutionState, null /* streamCopyState */);
                    blobStream.Commit();
                }
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="BeginAppendFromStream(Stream, AsyncCallback, object)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, null /* length */, true /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="BeginAppendFromStream(Stream, AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, null /* length */, true /* createNew */, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">Specifies the number of bytes from the Stream source to upload from the start position.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="BeginAppendFromStream(Stream, long, AsyncCallback, object)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, long length, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, length, true /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">Specifies the number of bytes from the Stream source to upload from the start position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="BeginAppendFromStream(Stream, long, AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromStream(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, length, true /* createNew */, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendFromStream(Stream source, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, null /* length */, false /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendFromStream(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, null /* length */, false /* createNew */, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">Specifies the number of bytes from the Stream source to upload from the start position.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendFromStream(Stream source, long length, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, length, false /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">Specifies the number of bytes from the Stream source to upload from the start position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendFromStream(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromStreamHelper(source, length, false /* createNew */, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">Specifies the number of bytes from the Stream source to upload from the start position.</param>
        /// <param name="createNew"><c>true</c> if the append blob is newly created, <c>false</c> otherwise.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        [DoesServiceRequest]
        internal ICancellableAsyncResult BeginUploadFromStreamHelper(Stream source, long? length, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
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
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);

            ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);
            StorageAsyncResult<NullType> storageAsyncResult = new StorageAsyncResult<NullType>(callback, state);

            ICancellableAsyncResult result = this.BeginOpenWrite(
                createNew,
                accessCondition,
                modifiedOptions,
                operationContext,
                ar =>
                {
                    storageAsyncResult.UpdateCompletedSynchronously(ar.CompletedSynchronously);

                    lock (storageAsyncResult.CancellationLockerObject)
                    {
                        storageAsyncResult.CancelDelegate = null;
                        try
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
                                    storageAsyncResult.UpdateCompletedSynchronously(completedState.CompletedSynchronously);
                                    if (completedState.ExceptionRef != null)
                                    {
                                        storageAsyncResult.OnComplete(completedState.ExceptionRef);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            lock (storageAsyncResult.CancellationLockerObject)
                                            {
                                                storageAsyncResult.CancelDelegate = null;
                                                ICancellableAsyncResult commitResult = blobStream.BeginCommit(
                                                        CloudBlob.BlobOutputStreamCommitCallback,
                                                        storageAsyncResult);

                                                storageAsyncResult.CancelDelegate = commitResult.Cancel;
                                                if (storageAsyncResult.CancelRequested)
                                                {
                                                    storageAsyncResult.Cancel();
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            storageAsyncResult.OnComplete(e);
                                        }
                                    }
                                });

                            storageAsyncResult.CancelDelegate = tempExecutionState.Cancel;
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
            storageAsyncResult.CancelDelegate = result.Cancel;

            return storageAsyncResult;
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a stream to an append blob. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadFromStream(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> storageAsyncResult = (StorageAsyncResult<NullType>)asyncResult;
            storageAsyncResult.End();
        }

        /// <summary>
        /// Ends an asynchronous operation to append a stream to an append blob. 
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndAppendFromStream(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> storageAsyncResult = (StorageAsyncResult<NullType>)asyncResult;
            storageAsyncResult.End();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source)
        {
            return this.UploadFromStreamAsync(source, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromStream, this.EndUploadFromStream, source, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsync(source, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromStream, this.EndUploadFromStream, source, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, long)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length)
        {
            return this.UploadFromStreamAsync(source, length, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, long, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromStream, this.EndUploadFromStream, source, length, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, long, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsync(source, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, long, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromStream, this.EndUploadFromStream, source, length, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>        
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source)
        {
            return this.AppendFromStreamAsync(source, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.        
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAppendFromStream, this.EndAppendFromStream, source, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendFromStreamAsync(source, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAppendFromStream, this.EndAppendFromStream, source, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, long length)
        {
            return this.AppendFromStreamAsync(source, length, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, long length, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAppendFromStream, this.EndAppendFromStream, source, length, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendFromStreamAsync(source, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">A <see cref="System.IO.Stream"/> object providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAppendFromStream, this.EndAppendFromStream, source, length, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a file to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFile"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void UploadFromFile(string path, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("path", path);

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                this.UploadFromStream(fileStream, accessCondition, options, operationContext);
            }
        }

        /// <summary>
        /// Appends a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void AppendFromFile(string path, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("path", path);

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                this.AppendFromStream(fileStream, accessCondition, options, operationContext);
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a file to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>   
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="BeginAppendFromFile(string, AsyncCallback, object)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromFile(string path, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromFile(path, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a file to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="BeginAppendFromFile(string, AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/>.
        /// </remarks>
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
        /// Begins an asynchronous operation to append a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>   
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendFromFile(string path, AsyncCallback callback, object state)
        {
            return this.BeginAppendFromFile(path, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to append a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendFromFile(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("path", path);

            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            StorageAsyncResult<NullType> storageAsyncResult = new StorageAsyncResult<NullType>(callback, state)
            {
                OperationState = fileStream
            };

            try
            {
                ICancellableAsyncResult asyncResult = this.BeginAppendFromStream(fileStream, accessCondition, options, operationContext, this.UploadFromFileCallback, storageAsyncResult);
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
        /// Ends an asynchronous operation to upload a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        public virtual void EndUploadFromFile(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> res = (StorageAsyncResult<NullType>)asyncResult;
            res.End();
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        public virtual void EndAppendFromFile(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> res = (StorageAsyncResult<NullType>)asyncResult;
            res.End();
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload a file to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(string)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path)
        {
            return this.UploadFromFileAsync(path, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a file to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(string, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromFile, this.EndUploadFromFile, path, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a file to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(string, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromFileAsync(path, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a file to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(string, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromFile, this.EndUploadFromFile, path, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromFileAsync(string path)
        {
            return this.AppendFromFileAsync(path, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromFileAsync(string path, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAppendFromFile, this.EndAppendFromFile, path, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendFromFileAsync(path, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAppendFromFile, this.EndAppendFromFile, path, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads the contents of a byte array to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArray"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void UploadFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            using (SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count))
            {
                this.UploadFromStream(stream, accessCondition, options, operationContext);
            }
        }

        /// <summary>
        /// Appends the contents of a byte array to an append blob.Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void AppendFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            using (SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count))
            {
                this.AppendFromStream(stream, accessCondition, options, operationContext);
            }
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload the contents of a byte array to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="BeginAppendFromByteArray(byte[], int, int, AsyncCallback, object)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromByteArray(byte[] buffer, int index, int count, AsyncCallback callback, object state)
        {
            return this.BeginUploadFromByteArray(buffer, index, count, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload the contents of a byte array to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
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
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="BeginAppendFromByteArray(byte[], int, int, AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.BeginUploadFromStream(stream, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        public virtual void EndUploadFromByteArray(IAsyncResult asyncResult)
        {
            this.EndUploadFromStream(asyncResult);
        }

        /// <summary>
        /// Begins an asynchronous operation to append the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendFromByteArray(byte[] buffer, int index, int count, AsyncCallback callback, object state)
        {
            return this.BeginAppendFromByteArray(buffer, index, count, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to append the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
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
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.BeginAppendFromStream(stream, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to append the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndAppendFromByteArray(IAsyncResult asyncResult)
        {
            this.EndAppendFromStream(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload the contents of a byte array to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload the contents of a byte array to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromByteArray, this.EndUploadFromByteArray, buffer, index, count, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload the contents of a byte array to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload the contents of a byte array to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadFromByteArray, this.EndUploadFromByteArray, buffer, index, count, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            return this.AppendFromByteArrayAsync(buffer, index, count, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromByteArrayAsync(byte[] buffer, int index, int count, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAppendFromByteArray, this.EndAppendFromByteArray, buffer, index, count, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append the contents of a byte array to an append blob.This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload the contents of a byte array to an append blob.This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAppendFromByteArray, this.EndAppendFromByteArray, buffer, index, count, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Uploads a string of text to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendText"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void UploadText(string content, Encoding encoding = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            this.UploadFromByteArray(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Appends a string of text to an append blob. This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual void AppendText(string content, Encoding encoding = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            this.AppendFromByteArray(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to upload a string of text to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="BeginAppendText(string, AsyncCallback, object)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadText(string content, AsyncCallback callback, object state)
        {
            return this.BeginUploadText(content, null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to upload a string of text to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="BeginAppendText(string, Encoding, AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginUploadText(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.BeginUploadFromByteArray(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to upload a string of text to an append blob. This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndUploadText(IAsyncResult asyncResult)
        {
            this.EndUploadFromByteArray(asyncResult);
        }

        /// <summary>
        /// Begins an asynchronous operation to append a string of text to an append blob. This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendText(string content, AsyncCallback callback, object state)
        {
            return this.BeginAppendText(content, null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to append a string of text to an append blob. This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendText(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.BeginAppendFromByteArray(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous operation to append a string of text to an append blob. This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndAppendText(IAsyncResult asyncResult)
        {
            this.EndAppendFromByteArray(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to upload a string of text to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content)
        {
            return this.UploadTextAsync(content, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a string of text to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadText, this.EndUploadText, content, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a string of text to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string, Encoding, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to upload a string of text to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string, Encoding, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginUploadText, this.EndUploadText, content, encoding, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a string of text to an append blob. This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task AppendTextAsync(string content)
        {
            return this.AppendTextAsync(content, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a string of text to an append blob. This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task AppendTextAsync(string content, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAppendText, this.EndAppendText, content, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a string of text to an append blob. This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendTextAsync(content, encoding, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to append a string of text to an append blob. This API should be used strictly in a single writer scenario 
        /// because the API internally uses the append-offset conditional header to avoid duplicate blocks which does not work in a multiple writer scenario.
        /// </summary>
        /// <param name="content">A string containing the text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginAppendText, this.EndAppendText, content, encoding, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Creates an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual void CreateOrReplace(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            Executor.ExecuteSync(
                this.CreateImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext);
        }
#endif

        /// <summary>
        /// Begins an asynchronous operation to create an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting,
        /// use <see cref="BeginCreateOrReplace(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/>.
        /// </summary>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreateOrReplace(AsyncCallback callback, object state)
        {
            return this.BeginCreateOrReplace(null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to create an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginCreateOrReplace(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            return Executor.BeginExecuteAsync(
                this.CreateImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                callback,
                state);
        }

        /// <summary>
        /// Ends an asynchronous operation to create an append blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual void EndCreateOrReplace(IAsyncResult asyncResult)
        {
            Executor.EndExecuteAsync<NullType>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to create an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting,
        /// use <see cref="CreateOrReplaceAsync(AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateOrReplaceAsync()
        {
            return this.CreateOrReplaceAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create an append blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting,
        /// use <see cref="CreateOrReplaceAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateOrReplaceAsync(CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginCreateOrReplace, this.EndCreateOrReplace, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateOrReplaceAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.CreateOrReplaceAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task CreateOrReplaceAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromVoidApm(this.BeginCreateOrReplace, this.EndCreateOrReplace, accessCondition, options, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Commits a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request. If <c>null</c>, default options are applied to the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The offset at which the block was appended.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Append Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual long AppendBlock(Stream blockData, string contentMD5 = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            CommonUtility.AssertNotNull("blockData", blockData);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
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

                return Executor.ExecuteSync(
                    this.AppendBlockImpl(seekableStream, contentMD5, accessCondition, modifiedOptions),
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
        /// Begins an asynchronous operation to commit a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendBlock(Stream blockData, AsyncCallback callback, object state)
        {
            return this.BeginAppendBlock(blockData, null /* contentMD5 */, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to commit a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Append Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendBlock(Stream blockData, string contentMD5, AsyncCallback callback, object state)
        {
            return this.BeginAppendBlock(blockData, contentMD5, null /* accessCondition */, null /* options */, null /* operationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to commit a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Append Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Needed to ensure exceptions are not thrown on threadpool threads.")]
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginAppendBlock(Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("blockData", blockData);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            bool requiresContentMD5 = string.IsNullOrEmpty(contentMD5) && modifiedOptions.UseTransactionalMD5.Value;
            operationContext = operationContext ?? new OperationContext();
            StorageAsyncResult<long> storageAsyncResult = new StorageAsyncResult<long>(callback, state);

            if (blockData.CanSeek && !requiresContentMD5)
            {
                this.AppendBlockHandler(blockData, contentMD5, accessCondition, modifiedOptions, operationContext, storageAsyncResult);
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
                                this.AppendBlockHandler(seekableStream, contentMD5, accessCondition, modifiedOptions, operationContext, storageAsyncResult);
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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        private void AppendBlockHandler(Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, StorageAsyncResult<long> storageAsyncResult)
        {
            lock (storageAsyncResult.CancellationLockerObject)
            {
                ICancellableAsyncResult result = Executor.BeginExecuteAsync(
                    this.AppendBlockImpl(blockData, contentMD5, accessCondition, options),
                    options.RetryPolicy,
                    operationContext,
                    ar =>
                    {
                        storageAsyncResult.UpdateCompletedSynchronously(ar.CompletedSynchronously);

                        try
                        {
                            storageAsyncResult.Result = Executor.EndExecuteAsync<long>(ar);
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

        /// <summary>
        /// Ends an asynchronous operation to commit a new block of data to the end of the blob.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that references the pending asynchronous operation.</param>
        public virtual long EndAppendBlock(IAsyncResult asyncResult)
        {
            StorageAsyncResult<long> storageAsyncResult = (StorageAsyncResult<long>)asyncResult;

            try
            {
                storageAsyncResult.End();
                return storageAsyncResult.Result;
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

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to commit a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Append Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<long> AppendBlockAsync(Stream blockData, string contentMD5 = null)
        {
            return this.AppendBlockAsync(blockData, contentMD5, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to commit a new block of data to the end of the blob.
        /// </summary>
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
        public virtual Task<long> AppendBlockAsync(Stream blockData, string contentMD5, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginAppendBlock, this.EndAppendBlock, blockData, contentMD5, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to commit a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Append Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<long> AppendBlockAsync(Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendBlockAsync(blockData, contentMD5, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to commit a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A <see cref="System.IO.Stream"/> object that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value used to ensure transactional integrity for the block. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Clients may send the Content-MD5 header for a given Append Block operation as a means to ensure transactional integrity over the wire. 
        /// The <paramref name="contentMD5"/> parameter permits clients who already have access to a pre-computed MD5 value for a given byte range to provide it.
        /// If the <see cref="P:BlobRequestOptions.UseTransactionalMd5"/> property is set to <c>true</c> and the <paramref name="contentMD5"/> parameter is set 
        /// to <c>null</c>, then the client library will calculate the MD5 value internally.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<long> AppendBlockAsync(Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginAppendBlock, this.EndAppendBlock, blockData, contentMD5, accessCondition, options, operationContext, cancellationToken);
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
        /// Begins an operation to start copying another append blob's contents, properties, and metadata to this append blob.
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
        public virtual string StartCopy(CloudAppendBlob source, AccessCondition sourceAccessCondition = null, AccessCondition destAccessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            return this.StartCopy(CloudBlob.SourceBlobToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext);
        }

#endif
        /// <summary>
        /// Begins an asynchronous operation to start copying another append blob's contents, properties, and metadata to this append blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudAppendBlob"/> object.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(CloudAppendBlob source, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(CloudBlob.SourceBlobToUri(source), callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to start copying another append blob's contents, properties, and metadata to this append blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudAppendBlob"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="ICancellableAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginStartCopy(CloudAppendBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            return this.BeginStartCopy(CloudBlob.SourceBlobToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext, callback, state);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to start copying another append blob's contents, properties, and metadata to this append blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudAppendBlob"/> object.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudAppendBlob source)
        {
            return this.StartCopyAsync(source, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another append blob's contents, properties, and metadata to this append blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudAppendBlob"/> object.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudAppendBlob source, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginStartCopy, this.EndStartCopy, source, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another append blob's contents, properties, and metadata to this append blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudAppendBlob"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudAppendBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to start copying another append blob's contents, properties, and metadata to this append blob.
        /// </summary>
        /// <param name="source">A <see cref="CloudAppendBlob"/> object.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <c>string</c> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudAppendBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginStartCopy, this.EndStartCopy, source, sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
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
        /// <returns>A <see cref="CloudAppendBlob"/> object that is a blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual CloudAppendBlob CreateSnapshot(IDictionary<string, string> metadata = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
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
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
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
        /// <returns>A <see cref="CloudAppendBlob"/> object that is a blob snapshot.</returns>
        public virtual CloudAppendBlob EndCreateSnapshot(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<CloudAppendBlob>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudAppendBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudAppendBlob> CreateSnapshotAsync()
        {
            return this.CreateSnapshotAsync(CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to create a snapshot of the blob.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudAppendBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudAppendBlob> CreateSnapshotAsync(CancellationToken cancellationToken)
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
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudAppendBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudAppendBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
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
        /// <returns>A <see cref="Task{T}"/> object of type <see cref="CloudAppendBlob"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudAppendBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginCreateSnapshot, this.EndCreateSnapshot, metadata, accessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Implements the Create method.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that creates the blob.</returns>
        private RESTCommand<NullType> CreateImpl(AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.Put(uri, serverTimeout, this.Properties, BlobType.AppendBlob, 0, accessCondition, useVersionHeader, ctx);
            putCmd.SetHeaders = (r, ctx) => BlobHttpWebRequestFactory.AddMetadata(r, this.Metadata);
            putCmd.SignRequest = this.ServiceClient.AuthenticationHandler.SignRequest;
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                this.Properties.Length = 0;
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Commits the block to the end of the blob.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="contentMD5">The content MD5.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand{T}"/> that commits the block to the end of the blob.</returns>
        internal RESTCommand<long> AppendBlockImpl(Stream source, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options)
        {
            options.AssertNoEncryptionPolicyOrStrictMode();

            long offset = source.Position;

            RESTCommand<long> putCmd = new RESTCommand<long>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.SendStream = source;
            putCmd.RecoveryAction = (cmd, ex, ctx) => RecoveryActions.SeekStream(cmd, offset);
            putCmd.BuildRequestDelegate = (uri, builder, serverTimeout, useVersionHeader, ctx) => BlobHttpWebRequestFactory.AppendBlock(uri, serverTimeout, accessCondition, useVersionHeader, ctx);
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
                long appendOffset = -1;
                if (resp.Headers.AllKeys.Contains(Constants.HeaderConstants.BlobAppendOffset))
                {
                    appendOffset = long.Parse(resp.Headers[Constants.HeaderConstants.BlobAppendOffset], CultureInfo.InvariantCulture);
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, appendOffset, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                return appendOffset;
            };

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
        internal RESTCommand<CloudAppendBlob> CreateSnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<CloudAppendBlob> putCmd = new RESTCommand<CloudAppendBlob>(this.ServiceClient.Credentials, this.attributes.StorageUri);

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

                CloudAppendBlob snapshot = new CloudAppendBlob(this.Name, snapshotTime, this.Container);
                snapshot.attributes.Metadata = new Dictionary<string, string>(metadata ?? this.Metadata);
                snapshot.attributes.Properties = new BlobProperties(this.Properties);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(snapshot.attributes, resp, false);
                return snapshot;
            };

            return putCmd;
        }
    }
}
