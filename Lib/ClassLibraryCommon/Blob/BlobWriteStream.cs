//-----------------------------------------------------------------------
// <copyright file="BlobWriteStream.cs" company="Microsoft">
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
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

#if WINDOWS_PHONE
    using System.Threading.Tasks;
#endif

    /// <summary>
    /// This class uses an algorithm designed to aid users in writing simple code that is also performant.
    /// The idea is that the caller can write the following pseudocode:
    /// 
    /// using (BlobWriteStream bws = Cloud*Blob.OpenWrite())
    /// {
    ///     while (moreData)
    ///     {
    ///         byte[] bytes = await GetDataAsync();
    ///         await bws.WriteAsync(bytes);
    ///     }
    /// }
    /// 
    /// The goal here is to have this code exhibit a sort of double-buffering functionality, where "GetDataAsync()"
    /// (whatever that is) is able to run in parallel with the data upload to Azure Storage.  This is accomplished
    /// via buffering data during the write, and then later uploading it to Storage.
    /// However, we also need to limit how much data is buffered, to avoid using too much memory. To do this,
    /// continuation after "await bws.WriteAsync(bytes)" will only continue if the stream hasn't buffered too much.
    /// 
    /// The limit on what is "too much" depends on the size of the input bytes[] data, the block size, 
    /// and the max parallelism factor. Roughly, it's the maximum of 1-2x the input byte array size, 
    /// and block size * parallelism factor. It's also fuzzy depending on whether or not you include any data that's currently
    /// being processed in WriteAsync(), any data that's currently being uploaded, or just data that's stored in the
    /// stream's buffer, waiting to be uploaded.
    /// 
    /// The biggest complication and/or likely location for bugs is around error handling, because errors might occur
    /// after "await WriteAsync()" has already continued.
    /// 
    /// If some day we consider a library re-design / re-architect, this code would likely be opt-in.
    /// 
    /// </summary>
    internal sealed class BlobWriteStream : BlobWriteStreamBase
    {
        /// <summary>
        /// This value is used mainly to provide async commit functionality(BeginCommit) to BlobEncryptedWriteStream. CryptoStream does not provide begin/end 
        /// flush. It only provides a blocking sync FlushFinalBlock call which calls the underlying stream's flush method (BlobWriteStream in this case). 
        /// By setting this to true while initiliazing the write stream, it is ensured that BlobWriteStream's Flush does not do anything and
        /// just returns. Therefore BeginCommit first just flushes all the data from the crypto stream's buffer to the blob write stream's buffer. The client 
        /// library then sets this property to false and calls BeginCommit on the write stream and returns the async result back to the user. This time flush actually
        /// does its work and sends the buffered data over to the service. 
        /// </summary>
        internal bool IgnoreFlush { get; set; }

        /// <summary>
        /// Initializes a new instance of the BlobWriteStream class for a block blob.
        /// </summary>
        /// <param name="blockBlob">Blob reference to write to.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        internal BlobWriteStream(CloudBlockBlob blockBlob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
            : base(blockBlob, accessCondition, options, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the BlobWriteStream class for a page blob.
        /// </summary>
        /// <param name="pageBlob">Blob reference to write to.</param>
        /// <param name="pageBlobSize">Size of the page blob.</param>
        /// <param name="createNew">Use <c>true</c> if the page blob is newly created, <c>false</c> otherwise.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        internal BlobWriteStream(CloudPageBlob pageBlob, long pageBlobSize, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
            : base(pageBlob, pageBlobSize, createNew, accessCondition, options, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the BlobWriteStream class for an append blob.
        /// </summary>
        /// <param name="appendBlob">Blob reference to write to.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        internal BlobWriteStream(CloudAppendBlob appendBlob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
            : base(appendBlob, accessCondition, options, operationContext)
        {
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type <c>SeekOrigin</c> indicating the reference
        /// point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long oldOffset = this.currentOffset;
            long newOffset = this.GetNewOffset(offset, origin);

            if (oldOffset != newOffset)
            {
                if (this.blobChecksum != null)
                {
                    this.blobChecksum.Dispose();
                    this.blobChecksum = null;
                }

                this.Flush();
            }

            this.currentOffset = newOffset;
            this.currentBlobOffset = newOffset;
            return this.currentOffset;
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current
        /// position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from
        /// buffer to the current stream. </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously Writes a sequence of bytes to the current stream and advances the current
        /// position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from
        /// buffer to the current stream. </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Task</returns>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            CommonUtility.AssertNotNull("buffer", buffer);
            CommonUtility.AssertInBounds("offset", offset, 0, buffer.Length);
            CommonUtility.AssertInBounds("count", count, 0, buffer.Length - offset);

            if (this.committed)
            {
                throw new InvalidOperationException(SR.BlobStreamAlreadyCommitted);
            }

            this.currentOffset += count;
            int initialOffset = offset;
            int initialCount = count;

            TaskCompletionSource<bool> continueTCS = new TaskCompletionSource<bool>();
            Task<bool> continueTask = continueTCS.Task;

            if (this.lastException == null)
            {
                while (count > 0)
                {
                    int maxBytesToWrite = this.streamWriteSizeInBytes - (int)this.internalBuffer.Length;
                    int bytesToWrite = Math.Min(count, maxBytesToWrite);

                    await this.internalBuffer.WriteAsync(buffer, offset, bytesToWrite, token).ConfigureAwait(false);
                    if (this.blockChecksum != null)
                    {
                        this.blockChecksum.UpdateHash(buffer, offset, bytesToWrite);
                    }

                    count -= bytesToWrite;
                    offset += bytesToWrite;

                    if (bytesToWrite == maxBytesToWrite)
                    {
                        // Note that we do not await on temptask, nor do we store it.
                        // We do not await temptask so as to enable parallel reads and writes.
                        // We could store it and await on it later, but that ends up being more complicated
                        // than what we actually do, which is have each write operation manage its own exceptions.
                        Task temptask = this.DispatchWriteAsync(continueTCS, token);

                        // We need to account for the fact that we're not awaiting on DispatchWriteAsync.
                        // DispatchWriteAsync is written in such a manner that any exceptions thrown after
                        // the first await point are handled internally.  This here is to account for
                        // exceptions that could happen inline, before an await point is encountered.
                        if (temptask.IsFaulted)
                        {
                            //We should make sure any exception thrown before the awaiting point in DispatchWriteAsync are stored in this.LastException
                            //We don't want to throw the tempTask.Exception directly since that would result in an aggregate exception
                            ThrowLastExceptionIfExists();
                        }

                        token.ThrowIfCancellationRequested();
                        continueTCS = null;
                    }
                }
            }

            // Update transactional, then update full blob, in that order.
            // This way, if there's any bit corruption that happens in between the two, we detect it at PutBlock on the service, 
            // rather than GetBlob + validate on the client
            if (this.blobChecksum != null)
            {
                this.blobChecksum.UpdateHash(buffer, initialOffset, initialCount);
            }

            if (continueTCS == null)
            {
                await continueTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Begins an asynchronous write operation.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from
        /// buffer to the current stream. </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the write is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>An <c>IAsyncResult</c> that represents the asynchronous write, which could still be pending.</returns>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => WriteAsync(buffer, offset, count, token), callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous write to complete.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying blob.
        /// </summary>
        public override void Flush()
        {
            this.FlushAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Begins an asynchronous flush operation.
        /// </summary>
        /// <param name="callback">An optional asynchronous callback, to be called when the flush is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous flush request from other requests.</param>
        /// <returns>An <c>ICancellableAsyncResult</c> that represents the asynchronous flush, which could still be pending.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Reviewed.")]
        public override ICancellableAsyncResult BeginFlush(AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.FlushAsync(CancellationToken.None), callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous flush to complete.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        public override void EndFlush(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }


        public override async Task FlushAsync(CancellationToken token)
        {
            if (this.committed)
            {
                throw new InvalidOperationException(SR.BlobStreamAlreadyCommitted);
            }

            if (!this.IgnoreFlush)
            {  
                this.ThrowLastExceptionIfExists();
                await this.DispatchWriteAsync(null, token).ConfigureAwait(false);
                await this.noPendingWritesEvent.WaitAsync().WithCancellation(token).ConfigureAwait(false);
                this.ThrowLastExceptionIfExists();
            }
        }

        /// <summary>
        /// Releases the blob resources used by the Stream.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    if (!this.committed)
                    {
                        this.CommitAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                }
            }

            base.Dispose(disposing);
        }

#if SYNC
        /// <summary>
        /// Clears all buffers for this stream, causes any buffered data to be written to the underlying blob, and commits the blob.
        /// </summary>
        public override void Commit()
        {
            this.CommitAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
#endif

        /// <summary>
        /// Begins an asynchronous commit operation.
        /// </summary>
        /// <param name="callback">An optional asynchronous callback, to be called when the commit is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous commit request from other requests.</param>
        /// <returns>An <c>ICancellableAsyncResult</c> that represents the asynchronous commit, which could still be pending.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "storageAsyncResult must be returned.")]
        public override ICancellableAsyncResult BeginCommit(AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.CommitAsync(), callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous commit to complete.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        public override void EndCommit(IAsyncResult asyncResult)
        {
            ((CancellableAsyncResultTaskWrapper)asyncResult).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously clears all buffers for this stream, causes any buffered 
        /// data to be written to the underlying blob, and commits the blob.
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>Task</returns>
        public override async Task CommitAsync()
        {
            await this.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            this.committed = true;

            try
            {
                if (this.blockBlob != null)
                {
                    // This block of code is for block blobs. PutBlockList needs to be called with the list of block IDs uploaded in order
                    // to commit the blocks.
                    if (this.blobChecksum != null)
                    {
                        if (this.blobChecksum.MD5 != null)
                        {
                            this.blockBlob.Properties.ContentChecksum.MD5 = this.blobChecksum.MD5.ComputeHash();
                        }

                        if (this.blobChecksum.CRC64 != null)
                        {
                            this.blockBlob.Properties.ContentChecksum.CRC64 = this.blobChecksum.CRC64.ComputeHash();
                        }
                    }

                    await this.blockBlob.PutBlockListAsync(
                        this.blockList,
                        this.accessCondition,
                        this.options,
                        this.operationContext).ConfigureAwait(false);
                }
                else
                {
                    // For Page blobs and append blobs, only if StoreBlobContentMD5/CRC64 is set to true, the stream would have caclculated a checksum
                    // which should be uploaded to the server using SetProperties.
                    if (this.blobChecksum != null)
                    {
                        if (this.blobChecksum.MD5 != null)
                        {
                            this.Blob.Properties.ContentChecksum.MD5 = this.blobChecksum.MD5.ComputeHash();
                        }

                        if (this.blobChecksum.CRC64 != null)
                        {
                            this.Blob.Properties.ContentChecksum.CRC64 = this.blobChecksum.CRC64.ComputeHash();
                        }

                        if (this.blobChecksum.HasAny)
                        {
                            await this.Blob.SetPropertiesAsync(
                                this.accessCondition,
                                this.options,
                                this.operationContext).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.lastException = e;
                throw;
            }
        }

        private async Task DispatchWriteAsync(TaskCompletionSource<bool> continuetcs, CancellationToken token)
        {
            if (this.internalBuffer.Length == 0)
            {
                if (continuetcs != null)
                {
                    Task.Run(() => continuetcs.TrySetResult(true));
                }
                return;
            }

            // bufferToUpload needs to be disposed, or we will leak memory
            // 
            // Unfortunately, because of the async nature of the work, we cannot safely
            // put this in a using block, so the Write*Async methods must handle disposal.
            MultiBufferMemoryStream bufferToUpload = this.internalBuffer;
            this.internalBuffer = new MultiBufferMemoryStream(this.Blob.ServiceClient.BufferManager);
            bufferToUpload.Seek(0, SeekOrigin.Begin);

            Checksum bufferChecksum = Checksum.None;
            if (this.blockChecksum != null)
            {
                bool computeCRC64 = false;
                bool computeMD5 = false;
                if (this.blockChecksum.MD5 != null)
                {
                    bufferChecksum.MD5 = this.blockChecksum.MD5.ComputeHash();
                    computeMD5 = true;
                }
                if (this.blockChecksum.CRC64 != null)
                {
                    bufferChecksum.CRC64 = this.blockChecksum.CRC64.ComputeHash();
                    computeCRC64 = true;
                }
                this.blockChecksum.Dispose();
                this.blockChecksum = new ChecksumWrapper(computeMD5, computeCRC64);
            }

            if (this.blockBlob != null)
            {
                string blockId = this.GetCurrentBlockId();
                this.blockList.Add(blockId);
                await this.WriteBlockAsync(continuetcs, bufferToUpload, blockId, bufferChecksum, token).ConfigureAwait(false);
            }
            else if (this.pageBlob != null)
            {
                if ((bufferToUpload.Length % Constants.PageSize) != 0)
                {
                    this.lastException = new IOException(SR.InvalidPageSize);
                    throw this.lastException;
                }

                long offset = this.currentBlobOffset;
                this.currentBlobOffset += bufferToUpload.Length;
                await this.WritePagesAsync(continuetcs, bufferToUpload, offset, bufferChecksum, token).ConfigureAwait(false);
            }
            else
            {
                long offset = this.currentBlobOffset;
                this.currentBlobOffset += bufferToUpload.Length;

                // We cannot differentiate between max size condition failing only in the retry versus failing in the first attempt and retry.  
                // So we will eliminate the latter and handle the former in the append operation callback.
                if (this.accessCondition.IfMaxSizeLessThanOrEqual.HasValue && this.currentBlobOffset > this.accessCondition.IfMaxSizeLessThanOrEqual.Value)
                {
                    this.lastException = new IOException(SR.InvalidBlockSize);
                    throw this.lastException;
                }

                await this.WriteAppendBlockAsync(continuetcs, bufferToUpload, offset, bufferChecksum, token).ConfigureAwait(false);
            }
        }

        private Task WriteBlockAsync(TaskCompletionSource<bool> continuetcs, Stream blockData, string blockId, Checksum blockChecksum, CancellationToken token)
        {
            this.noPendingWritesEvent.Increment();

            return this.parallelOperationSemaphoreAsync.WaitAsync(async (bool runningInline, CancellationToken internalToken) =>
            {
                try
                {
                    if (continuetcs != null)
                    {
                        Task.Run(() => continuetcs.TrySetResult(true));
                    }
                    await this.blockBlob.PutBlockAsync(blockId, blockData, blockChecksum, this.accessCondition, this.options, this.operationContext, internalToken).ConfigureAwait(false);
                    blockData.Dispose();
                    blockData = null;
                }
                catch (Exception e)
                {
                    this.lastException = e;
                }
                finally
                {
                    await this.noPendingWritesEvent.DecrementAsync().ConfigureAwait(false);
                    await this.parallelOperationSemaphoreAsync.ReleaseAsync(internalToken).ConfigureAwait(false);
                }
            }, token);
        }

        private Task WritePagesAsync(TaskCompletionSource<bool> continuetcs, Stream pageData, long offset, Checksum contentChecksum, CancellationToken token)
        {
            this.noPendingWritesEvent.Increment();
            return this.parallelOperationSemaphoreAsync.WaitAsync(async (bool runningInline, CancellationToken internalToken) =>
            {
                try
                {
                    if (continuetcs != null)
                    {
                        Task.Run(() => continuetcs.TrySetResult(true));
                    }
                    await this.pageBlob.WritePagesAsync(pageData, offset, contentChecksum, this.accessCondition, this.options, this.operationContext, internalToken).ConfigureAwait(false);
                    pageData.Dispose();
                    pageData = null;
                }
                catch (Exception e)
                {
                    this.lastException = e;
                }
                finally
                {
                    await this.noPendingWritesEvent.DecrementAsync().ConfigureAwait(false);
                    await this.parallelOperationSemaphoreAsync.ReleaseAsync(internalToken).ConfigureAwait(false);
                }
            }, token);
        }

        private Task WriteAppendBlockAsync(TaskCompletionSource<bool> continuetcs, Stream blockData, long offset, Checksum blockChecksum, CancellationToken token)
        {
            this.noPendingWritesEvent.Increment();
            return this.parallelOperationSemaphoreAsync.WaitAsync(async (bool runningInline, CancellationToken internalToken) =>
            {
                int previousResultsCount = this.operationContext.RequestResults.Count;
                try
                {
                    if (continuetcs != null)
                    {
                        Task.Run(() => continuetcs.TrySetResult(true));
                    }
                    this.accessCondition.IfAppendPositionEqual = offset;
                    await this.appendBlob.AppendBlockAsync(blockData, blockChecksum, this.accessCondition, this.options, this.operationContext, default(IProgress<StorageProgress>), internalToken).ConfigureAwait(false);
                    blockData.Dispose();
                    blockData = null;
                }
                catch (StorageException e)
                {
                    if (this.options.AbsorbConditionalErrorsOnRetry.Value
                        && e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                    {
                        StorageExtendedErrorInformation extendedInfo = e.RequestInformation.ExtendedErrorInformation;
                        if (extendedInfo != null
                            && (extendedInfo.ErrorCode == BlobErrorCodeStrings.InvalidAppendCondition || extendedInfo.ErrorCode == BlobErrorCodeStrings.InvalidMaxBlobSizeCondition)
                            && (this.operationContext.RequestResults.Count - previousResultsCount > 1))
                        {
                            // Pre-condition failure on a retry should be ignored in a single writer scenario since the request
                            // succeeded in the first attempt.
                            Logger.LogWarning(this.operationContext, SR.PreconditionFailureIgnored);
                            return;
                        }
                    }
                    this.lastException = e;
                }
                catch (Exception e)
                {
                    this.lastException = e;
                }
                finally
                {
                    await this.noPendingWritesEvent.DecrementAsync().ConfigureAwait(false);
                    await this.parallelOperationSemaphoreAsync.ReleaseAsync(internalToken).ConfigureAwait(false);
                }
            }, token);
        }
    }
}
