// -----------------------------------------------------------------------------------------
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
// -----------------------------------------------------------------------------------------

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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

#if WINDOWS_RT
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
    using Windows.Storage.Streams;
    using Windows.Storage;
#endif

    /// <summary>
    /// Represents an append blob, a type of blob where blocks of data are always committed to the end of the blob.
    /// </summary>
    public partial class CloudAppendBlob : CloudBlob, ICloudBlob
    {
        /// <summary>
        /// Opens a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <returns>A stream to be used for writing to the blob.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(bool createNew)
        {
            return this.OpenWriteAsync(createNew, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Opens a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// Note that this method always makes a call to the <see cref="CloudBlob.BeginFetchAttributes(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/> method under the covers.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// <returns>A stream to be used for writing to the blob.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.OpenWriteAsync(createNew, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Opens a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> if the append blob is newly created, <c>false</c> otherwise.</param>                
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A stream to be used for writing to the blob.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient, false);
            if (!createNew && modifiedOptions.StoreBlobContentMD5.Value)
            {
                throw new ArgumentException(SR.MD5NotPossible);
            }

            return Task.Run(async () =>
            {
                if (createNew)
                {
                    await this.CreateOrReplaceAsync(accessCondition, options, operationContext, cancellationToken);
                }
                else
                {
                    // Although we don't need any properties from the service, we should make this call in order to honor the user specified conditional headers
                    // while opening an existing stream and to get the append position for an existing blob if user didn't specify one.
                    await this.FetchAttributesAsync(accessCondition, options, operationContext, cancellationToken);
                }

                if (accessCondition != null)
                {
                    accessCondition = new AccessCondition() { LeaseId = accessCondition.LeaseId, IfAppendPositionEqual = accessCondition.IfAppendPositionEqual, IfMaxSizeLessThanOrEqual = accessCondition.IfMaxSizeLessThanOrEqual };
                }

                CloudBlobStream stream = new BlobWriteStream(this, accessCondition, modifiedOptions, operationContext);
                return stream;
            }, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, true /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, long)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length)
        {
            return this.UploadFromStreamAsyncHelper(source, length, true /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, true /* createNew */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, long, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, length, true /* createNew */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStream(Stream, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, true /* createNew */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStream(Stream, long, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, true /* createNew */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.                
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, false /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, long length)
        {
            return this.UploadFromStreamAsyncHelper(source, length, false /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, false /* createNew */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, length, false /* createNew */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, false /* createNew */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, false /* createNew */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="createNew"><c>true</c> if the append blob is newly created, <c>false</c> otherwise.</param>        
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal Task UploadFromStreamAsyncHelper(Stream source, long? length, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return UploadFromStreamAsyncHelper(source, length, createNew, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="createNew"><c>true</c> if the append blob is newly created, <c>false</c> otherwise.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal Task UploadFromStreamAsyncHelper(Stream source, long? length, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
            ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);

            return Task.Run(async () =>
            {
                using (CloudBlobStream blobStream = await this.OpenWriteAsync(createNew, accessCondition, options, operationContext, cancellationToken))
                {
                    // We should always call AsStreamForWrite with bufferSize=0 to prevent buffering. Our
                    // stream copier only writes 64K buffers at a time anyway, so no buffering is needed.
                    await source.WriteToAsync(blobStream, length, null /* maxLength */, false, tempExecutionState, null /* streamCopyState */, cancellationToken);
                    await blobStream.CommitAsync();
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Uploads a file to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(string)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path)
        {
            return this.UploadFromFileAsync(path, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(StorageFile)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(StorageFile source)
        {
            return this.UploadFromFileAsync(source, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#endif

        /// <summary>
        /// Uploads a file to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
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
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(StorageFile, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(StorageFile source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromFileAsync(source, accessCondition, options, operationContext, CancellationToken.None);
        }
#endif


        /// <summary>
        /// Uploads a file to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(string, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);

            return Task.Run(async () =>
            {
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    await this.UploadFromStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
                }
            }, cancellationToken);
        }
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(string, FileMode, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(StorageFile source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("source", source);

            return Task.Run(async () =>
            {
                using (IRandomAccessStreamWithContentType stream = await source.OpenReadAsync().AsTask(cancellationToken))
                {
                    await this.UploadFromStreamAsync(stream.AsStream(), accessCondition, options, operationContext, cancellationToken);
                }
            });
        }
#endif

        /// <summary>
        /// Appends a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task AppendFromFileAsync(string path)
        {
            return this.AppendFromFileAsync(path, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task AppendFromFileAsync(StorageFile source)
        {
            return this.AppendFromFileAsync(source, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#endif

        /// <summary>
        /// Appends a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendFromFileAsync(path, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromFileAsync(StorageFile source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendFromFileAsync(source, accessCondition, options, operationContext, CancellationToken.None);
        }

#endif

        /// <summary>
        /// Appends a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);

            return Task.Run(async () =>
            {
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    await this.AppendFromStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
                }
            }, cancellationToken);
        }
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromFileAsync(StorageFile source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {

            CommonUtility.AssertNotNull("source", source);

            return Task.Run(async () =>
            {
                using (IRandomAccessStreamWithContentType stream = await source.OpenReadAsync().AsTask(cancellationToken))
                {
                    await this.AppendFromStreamAsync(stream.AsStream(), accessCondition, options, operationContext, cancellationToken);
                }
            });
        }
#endif

        /// <summary>
        /// Uploads the contents of a byte array to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads the contents of a byte array to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
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
        /// Uploads the contents of a byte array to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.UploadFromStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Appends the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task AppendFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            return this.AppendFromByteArrayAsync(buffer, index, count, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Appends the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Appends the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.AppendFromStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a string of text to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload, encoded as a UTF-8 string.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content)
        {
            return this.UploadTextAsync(content, null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a string of text to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
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
        /// Uploads a string of text to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string, Encoding, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.UploadFromByteArrayAsync(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Appends a string of text to an append blob.
        /// </summary>
        /// <param name="content">The text to upload, encoded as a UTF-8 string.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task AppendTextAsync(string content)
        {
            return this.AppendTextAsync(content, null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Appends a string of text to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendTextAsync(content, encoding, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Appends a string of text to an append blob. 
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task AppendTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.AppendFromByteArrayAsync(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Creates an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception instead of overwriting the blob, 
        /// use <see cref="CreateOrReplaceAsync(AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateOrReplaceAsync()
        {
            return this.CreateOrReplaceAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Creates an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception instead of overwriting the blob,
        /// pass in an <see cref="AccessCondition"/> object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateOrReplaceAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.CreateOrReplaceAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception instead of overwriting the blob,
        /// pass in an <see cref="AccessCondition"/> object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateOrReplaceAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.CreateImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Commits a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A stream that provides the data for the block.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task<long> AppendBlockAsync(Stream blockData)
        {
            return this.AppendBlockAsync(blockData, null /* contentMD5 */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Commits a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A stream that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="BlobProperties.ContentMD5"/> property
        /// on the blob. May be <c>null</c> or an empty string.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task<long> AppendBlockAsync(Stream blockData, string contentMD5)
        {
            return this.AppendBlockAsync(blockData, contentMD5, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Commits a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A stream that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="BlobProperties.ContentMD5"/> property
        /// on the blob. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        public virtual Task<long> AppendBlockAsync(Stream blockData, string contentMD5, AccessCondition accesscondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendBlockAsync(blockData, contentMD5, accesscondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Commits a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A stream that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="BlobProperties.ContentMD5"/> property
        /// on the blob. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task<long> AppendBlockAsync(Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            bool requiresContentMD5 = string.IsNullOrEmpty(contentMD5) && modifiedOptions.UseTransactionalMD5.Value;
            operationContext = operationContext ?? new OperationContext();
            ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);

            return Task.Run(async () =>
            {
                Stream blockDataAsStream = blockData;
                Stream seekableStream = blockDataAsStream;
                bool seekableStreamCreated = false;

                try
                {
                    if (!blockDataAsStream.CanSeek || requiresContentMD5)
                    {
                        Stream writeToStream;
                        if (blockDataAsStream.CanSeek)
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
                        await blockDataAsStream.WriteToAsync(writeToStream, null /* copyLength */, Constants.MaxBlockSize, requiresContentMD5, tempExecutionState, streamCopyState, cancellationToken);
                        seekableStream.Position = startPosition;

                        if (requiresContentMD5)
                        {
                            contentMD5 = streamCopyState.Md5;
                        }
                    }

                    return await Executor.ExecuteAsync(
                        this.AppendBlockImpl(seekableStream, contentMD5, accessCondition, modifiedOptions),
                        modifiedOptions.RetryPolicy,
                        operationContext,
                        cancellationToken);
                }
                finally
                {
                    if (seekableStreamCreated)
                    {
                        seekableStream.Dispose();
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <returns>The contents of the blob, as a string.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync()
        {
            return this.DownloadTextAsync(null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The contents of the blob, as a string.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadTextAsync(encoding, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The contents of the blob, as a string.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                using (SyncMemoryStream stream = new SyncMemoryStream())
                {
                    await this.DownloadToStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
                    byte[] streamAsBytes = stream.ToArray();
                    return (encoding ?? Encoding.UTF8).GetString(streamAsBytes, 0, streamAsBytes.Length);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Begins an operation to start copying an existing block blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudAppendBlob source)
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source));
        }

        /// <summary>
        /// Begins an operation to start copying another block blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudAppendBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Begins an operation to start copying another append blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudAppendBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudAppendBlob> CreateSnapshotAsync()
        {
            return this.CreateSnapshotAsync(null /* metadata */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudAppendBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return CreateSnapshotAsync(metadata, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudAppendBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.CreateSnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

    /// <summary>
    /// Implements the Create method.
    /// </summary>
    /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
    /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
    /// <returns>A <see cref="TaskSequence"/> that creates the blob.</returns>
    private RESTCommand<NullType> CreateImpl(AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.Put(uri, serverTimeout, this.Properties, BlobType.AppendBlob, 0, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                BlobHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
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
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that uploads the block.</returns>
        internal RESTCommand<long> AppendBlockImpl(Stream source, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options)
        {
            long offset = source.Position;
            long length = source.Length - offset;

            RESTCommand<long> putCmd = new RESTCommand<long>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(source, offset, length, contentMD5, cmd, ctx);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.AppendBlock(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                long appendOffset = -1;
                if (resp.Headers.Contains(Constants.HeaderConstants.BlobAppendOffset))
                {
                    appendOffset = long.Parse(resp.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.BlobAppendOffset));
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, appendOffset, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                return appendOffset;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the CreateSnapshot method.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot, or null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the snapshot.</returns>
        /// <remarks>If the <c>metadata</c> parameter is <c>null</c> then no metadata is associated with the request.</remarks>
        internal RESTCommand<CloudAppendBlob> CreateSnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<CloudAppendBlob> putCmd = new RESTCommand<CloudAppendBlob>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.Snapshot(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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