// -----------------------------------------------------------------------------------------
// <copyright file="FileWriteStream.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class FileWriteStream : FileWriteStreamBase
    {
        /// <summary>
        /// Initializes a new instance of the FileWriteStream class for a file.
        /// </summary>
        /// <param name="file">File reference to write to.</param>
        /// <param name="fileSize">Size of the file.</param>
        /// <param name="createNew">Use <c>true</c> if the file is newly created, <c>false</c> otherwise.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        internal FileWriteStream(CloudFile file, long fileSize, bool createNew, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
            : base(file, fileSize, createNew, accessCondition, options, operationContext)
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
                if (this.fileMD5 != null)
                {
                    this.fileMD5.Dispose();
                    this.fileMD5 = null;
                }

                this.Flush();
            }

            this.currentOffset = newOffset;
            this.currentFileOffset = newOffset;
            return this.currentOffset;
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current
        /// position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from
        /// buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.WriteAsync(buffer, offset, count, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
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
            return CancellableAsyncResultTaskWrapper.Create(token => this.WriteAsync(buffer, offset, count, token), callback, state);
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
        /// Asynchronously writes a sequence of bytes to the current stream, advances the current
        /// position within this stream by the number of bytes written, and monitors cancellation requests.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from
        /// buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);
            CommonUtility.AssertInBounds("offset", offset, 0, buffer.Length);
            CommonUtility.AssertInBounds("count", count, 0, buffer.Length - offset);

            if (this.committed)
            {
                throw new InvalidOperationException(SR.FileStreamAlreadyCommitted);
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

                    await this.internalBuffer.WriteAsync(buffer, offset, bytesToWrite, cancellationToken).ConfigureAwait(false);
                    if (this.rangeMD5 != null)
                    {
                        this.rangeMD5.UpdateHash(buffer, offset, bytesToWrite);
                    }

                    count -= bytesToWrite;
                    offset += bytesToWrite;

                    if (bytesToWrite == maxBytesToWrite)
                    {
                        // Note that we do not await on temptask, nor do we store it.
                        // We do not await temptask so as to enable parallel reads and writes.
                        // We could store it and await on it later, but that ends up being more complicated
                        // than what we actually do, which is have each write operation manage its own exceptions.
                        Task temptask = this.DispatchWriteAsync(continueTCS, cancellationToken);

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

                        cancellationToken.ThrowIfCancellationRequested();
                        continueTCS = null;
                    }
                }
            }

            // Update transactional, then update full blob, in that order.
            // This way, if there's any bit corruption that happens in between the two, we detect it at PutBlock on the service, 
            // rather than GetBlob + validate on the client
            if (this.fileMD5 != null)
            {
                this.fileMD5.UpdateHash(buffer, initialOffset, initialCount);
            }

            if (continueTCS == null)
            {
                await continueTask;
            }
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying file.
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


        /// <summary>
        /// Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying device, and monitors cancellation requests.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (this.committed)
            {
                throw new InvalidOperationException(SR.BlobStreamAlreadyCommitted);
            }

            ThrowLastExceptionIfExists();
            await this.DispatchWriteAsync(null, cancellationToken).ConfigureAwait(false);
            await Task.Run(() => this.noPendingWritesEvent.Wait(), cancellationToken);
            ThrowLastExceptionIfExists();
        }

        /// <summary>
        /// Releases the file resources used by the Stream.
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
                        this.CommitAsync().GetAwaiter().GetResult();
                    }
                }
            }

            base.Dispose(disposing);
        }

#if SYNC
        /// <summary>
        /// Clears all buffers for this stream, causes any buffered data to be written to the underlying file, and commits the file.
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
        /// Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying file, and commits the file.
        /// </summary>
        /// <returns>A task that represents the asynchronous commit operation.</returns>
        public override async Task CommitAsync()
        {
            await this.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            this.committed = true;

            try
            {
                if (this.fileMD5 != null)
                {
                    this.file.Properties.ContentMD5 = this.fileMD5.ComputeHash();
                    await this.file.SetPropertiesAsync(this.accessCondition, this.options, this.operationContext).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                this.lastException = e;
                throw;
            }
        }

        /// <summary>
        /// Asynchronously dispatches a write operation.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
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

            MultiBufferMemoryStream bufferToUpload = this.internalBuffer;
            this.internalBuffer = new MultiBufferMemoryStream(this.file.ServiceClient.BufferManager);
            bufferToUpload.Seek(0, SeekOrigin.Begin);

            string bufferMD5 = null;
            if (this.rangeMD5 != null)
            {
                bufferMD5 = this.rangeMD5.ComputeHash();
                this.rangeMD5.Dispose();
                this.rangeMD5 = new MD5Wrapper();
            }

            long offset = this.currentFileOffset;
            this.currentFileOffset += bufferToUpload.Length;
            await this.WriteRangeAsync(continuetcs, bufferToUpload, offset, bufferMD5, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts an asynchronous WriteRange operation as soon as the parallel
        /// operation semaphore becomes available.
        /// </summary>
        /// <param name="rangeData">Data to be uploaded</param>
        /// <param name="offset">Offset within the file</param>
        /// <param name="contentMD5"> </param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteRangeAsync(TaskCompletionSource<bool> continuetcs, Stream rangeData, long offset, string contentMD5, CancellationToken token)
        {
            this.noPendingWritesEvent.Increment();
            await this.parallelOperationSemaphoreAsync.WaitAsync(async (bool runingInline, CancellationToken internalToken) =>
            {
                try
                {
                    if(continuetcs != null)
                    {
                        Task.Run(() => continuetcs.TrySetResult(true));
                    }

                    await this.file.WriteRangeAsync(rangeData, offset, contentMD5, this.accessCondition, this.options, this.operationContext, internalToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    this.lastException = e;
                }
                finally
                {
                    this.noPendingWritesEvent.Decrement();
                    await this.parallelOperationSemaphoreAsync.ReleaseAsync(internalToken).ConfigureAwait(false);
                }
            }, token).ConfigureAwait(false);
        }
    }
}
