// -----------------------------------------------------------------------------------------
// <copyright file="CloudPageBlob.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Blob
{
    using Microsoft.Azure.Storage.Blob.Protocol;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Executor;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class CloudPageBlob : CloudBlob, ICloudBlob
    {
        /// <summary>
        /// Opens a stream for writing to the blob. If the blob already exists, then existing data in the blob may be overwritten.
        /// </summary>
        /// <param name="size">The size of the page blob, in bytes. The size must be a multiple of 512. If <c>null</c>, the page blob must already exist.</param>
        /// <returns>A stream to be used for writing to the blob.</returns>
        /// <remarks>
        /// To avoid overwriting and instead throw an error, see <see cref="OpenWriteAsync(long?, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(long? size)
        {
            return this.OpenWriteAsync(size, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Opens a stream for writing to the blob. If the blob already exists, then existing data in the blob may be overwritten.
        /// </summary>
        /// <param name="size">The size of the page blob, in bytes. The size must be a multiple of 512. If <c>null</c>, the page blob must already exist.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A stream to be used for writing to the blob.</returns>
        /// <remarks>
        /// To throw an exception if the blob exists, instead of overwriting, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(long? size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.OpenWriteAsync(size, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Opens a stream for writing to the blob.
        /// </summary>
        /// <param name="size">The size of the page blob, in bytes. The size must be a multiple of 512. If <c>null</c>, the page blob must already exist.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A stream to be used for writing to the blob.</returns>
        /// <remarks>
        /// To throw an exception if the blob exists, instead of overwriting, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<CloudBlobStream> OpenWriteAsync(long? size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.OpenWriteAsync(size, null /* premiumBlobTier */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Opens a stream for writing to the blob.
        /// </summary>
        /// <param name="size">The size of the page blob, in bytes. The size must be a multiple of 512. If <c>null</c>, the page blob must already exist.</param>
        /// <param name="premiumPageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A stream to be used for writing to the blob.</returns>
        /// <remarks>
        /// To throw an exception if the blob exists, instead of overwriting, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </remarks>
        [DoesServiceRequest]
        internal virtual async Task<CloudBlobStream> OpenWriteAsync(long? size, PremiumPageBlobTier? premiumPageBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            bool createNew = size.HasValue;
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient, false);
            if (!createNew && modifiedOptions.ChecksumOptions.StoreContentMD5.Value)
            {
                throw new ArgumentException(SR.MD5NotPossible);
            }

            if (createNew)
            {
                await this.CreateAsync(size.Value, premiumPageBlobTier, accessCondition, options, operationContext, cancellationToken).ConfigureAwait(false);
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

            CloudBlobStream stream = new BlobWriteStream(this, size.Value, createNew, accessCondition, modifiedOptions, operationContext);
            return stream;
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length)
        {
            return this.UploadFromStreamAsyncHelper(source, length, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /*length */, null /* premiumBlobTier */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, PremiumPageBlobTier? premiumBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /*length */, premiumBlobTier, accessCondition, options, operationContext, new AggregatingProgressIncrementer(progressHandler), cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, PremiumPageBlobTier? premiumBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /*length */, premiumBlobTier, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, null /* premiumBlobTier */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, PremiumPageBlobTier? premiumBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, premiumBlobTier, accessCondition, options, operationContext, new AggregatingProgressIncrementer(progressHandler), cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, PremiumPageBlobTier? premiumBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, premiumBlobTier, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal Task UploadFromStreamAsyncHelper(Stream source, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return UploadFromStreamAsyncHelper(source, length, null /* premiumBlobTier */, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a stream to a page blob. 
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal Task UploadFromStreamAsyncHelper(Stream source, long? length, PremiumPageBlobTier? premiumPageBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, premiumPageBlobTier, accessCondition, options, operationContext, default(AggregatingProgressIncrementer), cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a page blob. 
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressIncrementer"> An <see cref="AggregatingProgressIncrementer"/> object to gather progress deltas.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        private async Task UploadFromStreamAsyncHelper(Stream source, long? length, PremiumPageBlobTier? premiumPageBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AggregatingProgressIncrementer progressIncrementer, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("source", source);

            progressIncrementer = progressIncrementer ?? AggregatingProgressIncrementer.None;

            Stream sourceAsStream = source;
            if (!sourceAsStream.CanSeek)
            {
                throw new InvalidOperationException();
            }

            if (length.HasValue)
            {
                CommonUtility.AssertInBounds("length", length.Value, 1, sourceAsStream.Length - sourceAsStream.Position);
            }
            else
            {
                length = sourceAsStream.Length - sourceAsStream.Position;
            }

            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(modifiedOptions);

            if ((length % Constants.PageSize) != 0)
            {
                throw new ArgumentException(SR.InvalidPageSize, "source");
            }

            using (CloudBlobStream blobStream = await this.OpenWriteAsync(length, premiumPageBlobTier, accessCondition, options, operationContext, cancellationToken).ConfigureAwait(false))
            {
                // We should always call AsStreamForWrite with bufferSize=0 to prevent buffering. Our
                // stream copier only writes 64K buffers at a time anyway, so no buffering is needed.
                await sourceAsStream.WriteToAsync(progressIncrementer.CreateProgressIncrementingStream(blobStream), this.ServiceClient.BufferManager, length, null /* maxLength */, ChecksumRequested.None, tempExecutionState, null /* streamCopyState */, cancellationToken).ConfigureAwait(false);
                await blobStream.CommitAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Uploads a file to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path)
        {
            return this.UploadFromFileAsync(path, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a file to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromFileAsync(path, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a file to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromFileAsync(path, null /* premiumBlobTier */, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Uploads a file to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual async Task UploadFromFileAsync(string path, PremiumPageBlobTier? premiumBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);

            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                await this.UploadFromStreamAsync(stream, premiumBlobTier, accessCondition, options, operationContext, progressHandler, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Uploads the contents of a byte array to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads the contents of a byte array to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads the contents of a byte array to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, null /* premiumBlobTier */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads the contents of a byte array to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, PremiumPageBlobTier? premiumBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, premiumBlobTier, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken);
        }

        /// <summary>
        /// Uploads the contents of a byte array to a page blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, PremiumPageBlobTier? premiumBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.UploadFromStreamAsync(stream, premiumBlobTier, accessCondition, options, operationContext, progressHandler, cancellationToken);
        }

        /// <summary>
        /// Creates a page blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting,
        /// use <see cref="CreateAsync(long, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </summary>
        /// <param name="size">The maximum size of the page blob, in bytes.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size)
        {
            return this.CreateAsync(size, null /* premiumBlobTier */, null /* accessCondition */, null /* options */, null /* operationContext */, CancellationToken.None);
        }

        /// <summary>
        /// Creates a page blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="size">The maximum size of the page blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.CreateAsync(size, null /* premiumBlobTier */, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates a page blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="size">The maximum size of the page blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.CreateAsync(size, null /* premiumBlobTier */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Creates a page blob. If the blob already exists, this operation will overwrite it. To throw an exception if the blob exists, instead of overwriting, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="size">The maximum size of the page blob, in bytes.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size, PremiumPageBlobTier? premiumBlobTier, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);
            return Executor.ExecuteAsyncNullReturn(
                this.CreateImpl(size, premiumBlobTier, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Resizes the page blob to the specified size.
        /// </summary>
        /// <param name="size">The maximum size of the page blob, in bytes.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ResizeAsync(long size)
        {
            return this.ResizeAsync(size, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Resizes the page blob to the specified size.
        /// </summary>
        /// <param name="size">The maximum size of the page blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ResizeAsync(long size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ResizeAsync(size, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Resizes the page blob to the specified size.
        /// </summary>
        /// <param name="size">The maximum size of the page blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ResizeAsync(long size, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);
            return Executor.ExecuteAsyncNullReturn(
                this.ResizeImpl(size, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Sets the page blob's sequence number.
        /// </summary>
        /// <param name="sequenceNumberAction">A value of type <see cref="SequenceNumberAction"/>, indicating the operation to perform on the sequence number.</param>
        /// <param name="sequenceNumber">The sequence number. Set this parameter to <c>null</c> if <paramref name="sequenceNumberAction"/> is equal to <see cref="F:SequenceNumberAction.Increment"/>.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetSequenceNumberAsync(SequenceNumberAction sequenceNumberAction, long? sequenceNumber)
        {
            return this.SetSequenceNumberAsync(sequenceNumberAction, sequenceNumber, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Sets the page blob's sequence number.
        /// </summary>
        /// <param name="sequenceNumberAction">A value of type <see cref="SequenceNumberAction"/>, indicating the operation to perform on the sequence number.</param>
        /// <param name="sequenceNumber">The sequence number. Set this parameter to <c>null</c> if <paramref name="sequenceNumberAction"/> is equal to <see cref="F:SequenceNumberAction.Increment"/>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetSequenceNumberAsync(SequenceNumberAction sequenceNumberAction, long? sequenceNumber, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.SetSequenceNumberAsync(sequenceNumberAction, sequenceNumber, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Sets the page blob's sequence number.
        /// </summary>
        /// <param name="sequenceNumberAction">A value of type <see cref="SequenceNumberAction"/>, indicating the operation to perform on the sequence number.</param>
        /// <param name="sequenceNumber">The sequence number. Set this parameter to <c>null</c> if <paramref name="sequenceNumberAction"/> is equal to <see cref="F:SequenceNumberAction.Increment"/>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetSequenceNumberAsync(SequenceNumberAction sequenceNumberAction, long? sequenceNumber, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);
            return Executor.ExecuteAsyncNullReturn(
                this.SetSequenceNumberImpl(sequenceNumberAction, sequenceNumber, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Gets a collection of valid page ranges and their starting and ending bytes.
        /// </summary>
        /// <returns>An enumerable collection of page ranges.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<PageRange>> GetPageRangesAsync()
        {
            return this.GetPageRangesAsync(null /* offset */, null /* length */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets a collection of valid page ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="length">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of page ranges.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<PageRange>> GetPageRangesAsync(long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.GetPageRangesAsync(offset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets a collection of valid page ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="length">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An enumerable collection of page ranges.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<PageRange>> GetPageRangesAsync(long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.GetPageRangesImpl(offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Gets the collection of page ranges that differ between a specified snapshot and this object.
        /// </summary>
        /// <param name="previousSnapshotTime">A <see cref="DateTimeOffset"/> representing the snapshot timestamp to use as the starting point for the diff. If this CloudPageBlob represents a snapshot, the previousSnapshotTime parameter must be prior to the current snapshot timestamp.</param>
        /// <returns>An enumerable collection of page ranges.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<PageDiffRange>> GetPageRangesDiffAsync(DateTimeOffset previousSnapshotTime)
        {
            return this.GetPageRangesDiffAsync(previousSnapshotTime, null /* offset */, null /* length */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets the collection of page ranges that differ between a specified snapshot and this object.
        /// </summary>
        /// <param name="previousSnapshotTime">A <see cref="DateTimeOffset"/> representing the snapshot timestamp to use as the starting point for the diff. If this CloudPageBlob represents a snapshot, the previousSnapshotTime parameter must be prior to the current snapshot timestamp.</param>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="length">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of page ranges.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<PageDiffRange>> GetPageRangesDiffAsync(DateTimeOffset previousSnapshotTime, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.GetPageRangesDiffAsync(previousSnapshotTime, offset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Gets the collection of page ranges that differ between a specified snapshot and this object.
        /// </summary>
        /// <param name="previousSnapshotTime">A <see cref="DateTimeOffset"/> representing the snapshot timestamp to use as the starting point for the diff. If this CloudPageBlob represents a snapshot, the previousSnapshotTime parameter must be prior to the current snapshot timestamp.</param>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="length">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An enumerable collection of page ranges.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<PageDiffRange>> GetPageRangesDiffAsync(DateTimeOffset previousSnapshotTime, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.GetPageRangesDiffImpl(previousSnapshotTime, offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudPageBlob> CreateSnapshotAsync()
        {
            return this.CreateSnapshotAsync(null /* metadata */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudPageBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.CreateSnapshotAsync(metadata, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudPageBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.CreateSnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Writes pages to a page blob.
        /// </summary>
        /// <param name="pageData">A stream providing the page data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes. The offset must be a multiple of 512.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task WritePagesAsync(Stream pageData, long startOffset, Checksum contentChecksum)
        {
            return this.WritePagesAsync(pageData, startOffset, contentChecksum, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Writes pages to a page blob.
        /// </summary>
        /// <param name="pageData">A stream providing the page data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes. The offset must be a multiple of 512.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task WritePagesAsync(Stream pageData, long startOffset, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.WritePagesAsync(pageData, startOffset, contentChecksum, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Writes pages to a page blob.
        /// </summary>
        /// <param name="pageData">A stream providing the page data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes. The offset must be a multiple of 512.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual async Task WritePagesAsync(Stream pageData, long startOffset, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            await this.WritePagesAsync(pageData, startOffset, contentChecksum, accessCondition, options, operationContext, default(IProgress<StorageProgress>), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes pages to a page blob.
        /// </summary>
        /// <param name="pageData">A stream providing the page data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes. The offset must be a multiple of 512.</param>
        /// <param name="contentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="progressHandler"> A <see cref="System.IProgress{StorageProgress}"/> object to handle <see cref="StorageProgress"/> messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual async Task WritePagesAsync(Stream pageData, long startOffset, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);

            contentChecksum = contentChecksum ?? Checksum.None;

            ChecksumRequested requiresContentChecksum = new ChecksumRequested(md5: (contentChecksum.MD5 == null) && modifiedOptions.ChecksumOptions.UseTransactionalMD5.Value, crc64: (contentChecksum.CRC64 == null) && modifiedOptions.ChecksumOptions.UseTransactionalCRC64.Value);
            operationContext = operationContext ?? new OperationContext();

            ExecutionState<NullType> tempExecutionState = BlobCommonUtility.CreateTemporaryExecutionState(modifiedOptions);

            Stream pageDataAsStream = pageData;
            Stream seekableStream = pageDataAsStream;
            bool seekableStreamCreated = false;

            try
            {
                if (!pageDataAsStream.CanSeek || requiresContentChecksum.HasAny)
                {
                    Stream writeToStream;
                    if (pageDataAsStream.CanSeek)
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
                    await pageDataAsStream.WriteToAsync(writeToStream, this.ServiceClient.BufferManager, null /* copyLength */, Constants.MaxBlockSize, requiresContentChecksum, tempExecutionState, streamCopyState, cancellationToken).ConfigureAwait(false);
                    seekableStream.Position = startPosition;

                    contentChecksum = new Checksum(
                        md5: requiresContentChecksum.MD5 ? streamCopyState.Md5 : default(string),
                        crc64: requiresContentChecksum.CRC64 ? streamCopyState.Crc64 : default(string)
                        );
                }

                await Executor.ExecuteAsyncNullReturn(
                    this.PutPageImpl(new AggregatingProgressIncrementer(progressHandler).CreateProgressIncrementingStream(seekableStream), startOffset, contentChecksum, accessCondition, modifiedOptions),
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


        /// <summary>
        /// Initiates an asynchronous operation to write pages to a page blob.
        /// </summary>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset in the source at which to begin retrieving content.</param>
        /// <param name="count">The number of bytes from the source to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes. The offset must be a multiple of 512.</param>
        /// <param name="sourceContentChecksum">An optional hash value used to ensure transactional integrity. May be <c>null</c>.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task WritePagesAsync(Uri sourceUri, long offset, long count, long startOffset, Checksum sourceContentChecksum, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("sourceUri", sourceUri);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Executor.ExecuteAsync(
                this.PutPageImpl(sourceUri, offset, count, startOffset, sourceContentChecksum, sourceAccessCondition, destAccessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Clears pages from a page blob.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing pages, in bytes. The offset must be a multiple of 512.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes. The length must be a multiple of 512.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ClearPagesAsync(long startOffset, long length)
        {
            return this.ClearPagesAsync(startOffset, length, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Clears pages from a page blob.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing pages, in bytes. The offset must be a multiple of 512.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes. The length must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ClearPagesAsync(long startOffset, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ClearPagesAsync(startOffset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Clears pages from a page blob.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing pages, in bytes. The offset must be a multiple of 512.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes. The length must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ClearPagesAsync(long startOffset, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);
            return Executor.ExecuteAsyncNullReturn(
                this.ClearPageImpl(startOffset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Begins an operation to start copying an existing blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudPageBlob source)
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source));
        }

        /// <summary>
        /// Begins an operation to start copying a blob's contents, properties, and metadata to a new blob.
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
        public virtual Task<string> StartCopyAsync(CloudPageBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Begins an operation to start copying another page blob's contents, properties, and metadata to a new blob.
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
        public virtual Task<string> StartCopyAsync(CloudPageBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(source, null /* premiumBlobTier */, sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Begins an operation to start copying another page blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
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
        public virtual Task<string> StartCopyAsync(CloudPageBlob source, PremiumPageBlobTier? premiumBlobTier, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source), premiumBlobTier, default(StandardBlobTier?) /* standardBlockBlobTier */, default(RehydratePriority?), sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Begins an operation to start an incremental copy of an existing blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="sourceSnapshot">The source blob which must be a snapshot.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartIncrementalCopyAsync(CloudPageBlob sourceSnapshot)
        {
            return this.StartIncrementalCopyAsync(CloudBlob.SourceBlobToUri(sourceSnapshot));
        }

        /// <summary>
        /// Begins an operation to start an incremental copy of an existing blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="sourceSnapshot">The URI of a source blob which must be a snapshot.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartIncrementalCopyAsync(Uri sourceSnapshot)
        {
            return this.StartIncrementalCopyAsync(sourceSnapshot, null /* destAccessCondition */, null /* options */, null /* operationContext */, CancellationToken.None);
        }

        /// <summary>
        /// Begins an operation to start an incremental copy of another page blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="sourceSnapshot">The source blob which must be a snapshot.</param>
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
        public virtual Task<string> StartIncrementalCopyAsync(CloudPageBlob sourceSnapshot, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.StartIncrementalCopyAsync(CloudBlob.SourceBlobToUri(sourceSnapshot), destAccessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Begins an operation to start an incremental copy of a blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="sourceSnapshot">The URI of a source blob which must be a snapshot.</param>
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
        public virtual Task<string> StartIncrementalCopyAsync(Uri sourceSnapshot, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("sourceSnapshot", sourceSnapshot);
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.StartCopyImpl(this.attributes, sourceSnapshot, Checksum.None, true /*incrementalCopy */, false /* syncCopy */, default(PremiumPageBlobTier?) /* premiumPageBlobTier */, default(StandardBlobTier?) /* standardBlockBlobTier */, default(RehydratePriority?) /* rehydratePriority */, null /* sourceAccessCondition */, destAccessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Sets the tier for a blob.
        /// </summary>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPremiumBlobTierAsync(PremiumPageBlobTier premiumBlobTier)
        {
            return this.SetPremiumBlobTierAsync(premiumBlobTier, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Sets the tier for a blob.
        /// </summary>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPremiumBlobTierAsync(PremiumPageBlobTier premiumBlobTier, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.SetPremiumBlobTierAsync(premiumBlobTier, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Sets the tier for a premium blob.
        /// </summary>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPremiumBlobTierAsync(PremiumPageBlobTier premiumBlobTier, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.PageBlob, this.ServiceClient);
            return Executor.ExecuteAsync(
                this.SetBlobTierImpl(premiumBlobTier, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        /// <summary>
        /// Implements the Create method.
        /// </summary>
        /// <param name="sizeInBytes">The size in bytes.</param>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="TaskSequence"/> that creates the blob.</returns>
        private RESTCommand<NullType> CreateImpl(long sizeInBytes, PremiumPageBlobTier? premiumBlobTier, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                BlobRequest.VerifyHttpsCustomerProvidedKey(uri, options);
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.Put(uri, serverTimeout, this.Properties, BlobType.PageBlob, sizeInBytes, premiumBlobTier, accessCondition,
                    cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials, options);
                BlobHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);

                BlobResponse.ValidateCPKHeaders(resp, options, true);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                cmd.CurrentResult.EncryptionKeySHA256 = HttpResponseParsers.ParseEncryptionKeySHA256(resp);

                this.Properties.Length = sizeInBytes;
                this.attributes.Properties.PremiumPageBlobTier = premiumBlobTier;
                if (premiumBlobTier.HasValue)
                {
                    this.attributes.Properties.BlobTierInferred = false;
                }

                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Resize method.
        /// </summary>
        /// <param name="sizeInBytes">The size in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> ResizeImpl(long sizeInBytes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Resize(uri, serverTimeout, sizeInBytes, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                this.Properties.Length = sizeInBytes;
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the SetSequenceNumber method.
        /// </summary>
        /// <param name="sequenceNumberAction">A value of type <see cref="SequenceNumberAction"/>, indicating the operation to perform on the sequence number.</param>
        /// <param name="sequenceNumber">The sequence number. Set this parameter to <c>null</c> if this operation is an increment action.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetSequenceNumberImpl(SequenceNumberAction sequenceNumberAction, long? sequenceNumber, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.SetSequenceNumber(uri, serverTimeout, sequenceNumberAction, sequenceNumber, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the CreateSnapshot method.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot, or <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the snapshot.</returns>
        /// <remarks>If the <c>metadata</c> parameter is <c>null</c> then no metadata is associated with the request.</remarks>
        private RESTCommand<CloudPageBlob> CreateSnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<CloudPageBlob> putCmd = new RESTCommand<CloudPageBlob>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                BlobRequest.VerifyHttpsCustomerProvidedKey(uri, options);
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
                CloudPageBlob snapshot = new CloudPageBlob(this.Name, snapshotTime, this.Container);
                snapshot.attributes.Metadata = new Dictionary<string, string>(metadata ?? this.Metadata, StringComparer.OrdinalIgnoreCase);
                snapshot.attributes.Properties = new BlobProperties(this.Properties);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(snapshot.attributes, resp, false);
                return snapshot;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for get page ranges.
        /// </summary>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="length">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> for getting the page ranges.</returns>
        private RESTCommand<IEnumerable<PageRange>> GetPageRangesImpl(long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<IEnumerable<PageRange>> getCmd = new RESTCommand<IEnumerable<PageRange>>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.GetPageRanges(uri, serverTimeout, this.SnapshotTime, offset, length, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                return msg;
            };

            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) =>
            {
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, true);
                return GetPageRangesResponse.ParseAsync(cmd.ResponseStream, ct);
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the GetPageRangesDiff method.
        /// </summary>
        /// <param name="previousSnapshotTime">A <see cref="DateTimeOffset"/> representing the snapshot timestamp to use as the starting point for the diff. If this CloudPageBlob represents a snapshot, the previousSnapshotTime parameter must be prior to the current snapshot timestamp.</param>
        /// <param name="offset">The starting offset of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="length">The length of the data range over which to list page ranges, in bytes. Must be a multiple of 512.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> for getting the page ranges.</returns>
        private RESTCommand<IEnumerable<PageDiffRange>> GetPageRangesDiffImpl(DateTimeOffset previousSnapshotTime, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<IEnumerable<PageDiffRange>> getCmd = new RESTCommand<IEnumerable<PageDiffRange>>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.GetPageRangesDiff(uri, serverTimeout, this.SnapshotTime, previousSnapshotTime, offset, length, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                return msg;
            };

            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) =>
            {
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, true);
                return GetPageDiffRangesResponse.ParseAsync(cmd.ResponseStream, ct);
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation method for the WritePage methods.
        /// </summary>
        /// <param name="pageData">The page data.</param>
        /// <param name="startOffset">The start offset.</param> 
        /// <param name="contentChecksum">A hash value used to ensure transactional integrity. May be <c>null</c> or Checksum.None</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that writes the pages.</returns>
        private RESTCommand<NullType> PutPageImpl(Stream pageData, long startOffset, Checksum contentChecksum, AccessCondition accessCondition, BlobRequestOptions options)
        {
            if (startOffset % Constants.PageSize != 0)
            {
                CommonUtility.ArgumentOutOfRange("startOffset", startOffset);
            }

            long offset = pageData.Position;
            long length = pageData.Length - offset;

            PageRange pageRange = new PageRange(startOffset, startOffset + length - 1);
            PageWrite pageWrite = PageWrite.Update;

            if ((1 + pageRange.EndOffset - pageRange.StartOffset) % Constants.PageSize != 0 ||
                (1 + pageRange.EndOffset - pageRange.StartOffset) == 0)
            {
                CommonUtility.ArgumentOutOfRange("pageData", pageData);
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(pageData, offset, length, contentChecksum, cmd, ctx);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => 
            {
                BlobRequest.VerifyHttpsCustomerProvidedKey(uri, options);
                return BlobHttpRequestMessageFactory.PutPage(uri, serverTimeout, pageRange, pageWrite, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials, options);
            };
            
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                cmd.CurrentResult.EncryptionKeySHA256 = HttpResponseParsers.ParseEncryptionKeySHA256(resp);
                BlobResponse.ValidateCPKHeaders(resp, options, true);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation method for the WritePage methods.
        /// </summary>
        /// <param name="sourceUri">A <see cref="System.Uri"/> specifying the absolute URI to the source blob.</param>
        /// <param name="offset">The byte offset in the source at which to begin retrieving content.</param>
        /// <param name="count">The number of bytes from the source to return, or <c>null</c> to return all bytes through the end of the blob.</param>
        /// <param name="startOffset">The offset in the destination to begin writing.</param> 
        /// <param name="sourceContentChecksum">An optional hash value that will be used to set the checksum property on the blob. May be <c>null</c> or Checksum.None.</param>
        /// <param name="sourceAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that writes the pages.</returns>
        private RESTCommand<NullType> PutPageImpl(Uri sourceUri, long offset, long count, long startOffset, Checksum sourceContentChecksum, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options)
        {
            if (startOffset % Constants.PageSize != 0)
            {
                CommonUtility.ArgumentOutOfRange("startOffset", startOffset);
            }

            PageRange pageRange = new PageRange(startOffset, startOffset + count - 1);

            if ((1 + pageRange.EndOffset - pageRange.StartOffset) % Constants.PageSize != 0 ||
                (1 + pageRange.EndOffset - pageRange.StartOffset) == 0)

            {
                CommonUtility.ArgumentOutOfRange("EndOffset", pageRange.EndOffset);
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => 
            {
                BlobRequest.VerifyHttpsCustomerProvidedKey(uri, options);
                return BlobHttpRequestMessageFactory.PutPage(uri, sourceUri, offset, count, sourceContentChecksum, serverTimeout, pageRange, sourceAccessCondition, destAccessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials, options);
            };
            
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = HttpResponseParsers.ParseServerRequestEncrypted(resp);
                cmd.CurrentResult.EncryptionKeySHA256 = HttpResponseParsers.ParseEncryptionKeySHA256(resp);
                BlobResponse.ValidateCPKHeaders(resp, options, true);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation method for the ClearPage methods.
        /// </summary>
        /// <param name="startOffset">The start offset. Must be multiples of 512.</param>
        /// <param name="length">Length of the data range to be cleared. Must be multiples of 512.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that writes the pages.</returns>
        private RESTCommand<NullType> ClearPageImpl(long startOffset, long length, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("options", options);

            if (startOffset < 0 || startOffset % Constants.PageSize != 0)
            {
                CommonUtility.ArgumentOutOfRange("startOffset", startOffset);
            }

            if (length <= 0 || length % Constants.PageSize != 0)
            {
                CommonUtility.ArgumentOutOfRange("length", length);
            }

            PageRange pageRange = new PageRange(startOffset, startOffset + length - 1);
            PageWrite pageWrite = PageWrite.Clear;

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                BlobRequest.VerifyHttpsCustomerProvidedKey(uri, options);
                return BlobHttpRequestMessageFactory.PutPage(uri, serverTimeout, pageRange, pageWrite, accessCondition, cnt, ctx, 
                    this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials, options);
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation method for the SetBlobTier methods.
        /// </summary>
        /// <param name="premiumBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the blob tier.</returns>
        internal RESTCommand<NullType> SetBlobTierImpl(PremiumPageBlobTier premiumBlobTier, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri, this.ServiceClient.HttpClient);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.SetBlobTier(uri, serverTimeout, premiumBlobTier.ToString(), default(RehydratePriority?), cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);

                this.attributes.Properties.PremiumPageBlobTier = premiumBlobTier;
                this.attributes.Properties.BlobTierInferred = false;

                return NullType.Value;
            };

            return putCmd;
        }
    }
}
