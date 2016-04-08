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
            this.WriteAsync(buffer, offset, count).Wait();
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

            if (this.lastException != null)
            {
                throw this.lastException;
            }

            if (this.fileMD5 != null)
            {
                this.fileMD5.UpdateHash(buffer, offset, count);
            }

            this.currentOffset += count;
            while (count > 0)
            {
                int maxBytesToWrite = this.streamWriteSizeInBytes - (int)this.internalBuffer.Length;
                int bytesToWrite = Math.Min(count, maxBytesToWrite);

                this.internalBuffer.Write(buffer, offset, bytesToWrite);
                if (this.rangeMD5 != null)
                {
                    this.rangeMD5.UpdateHash(buffer, offset, bytesToWrite);
                }

                count -= bytesToWrite;
                offset += bytesToWrite;

                if (bytesToWrite == maxBytesToWrite)
                {
                    await this.DispatchWriteAsync();
                }
            }
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying file.
        /// </summary>
        public override void Flush()
        {
            this.FlushAsync().Wait();
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
                throw new InvalidOperationException(SR.FileStreamAlreadyCommitted);
            }

            if (this.lastException != null)
            {
                throw this.lastException;
            }

            await this.DispatchWriteAsync();
            await Task.Run(() => this.noPendingWritesEvent.Wait(), cancellationToken);

            if (this.lastException != null)
            {
                throw this.lastException;
            }
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
                        this.CommitAsync().Wait();
                    }
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying file, and commits the file.
        /// </summary>
        /// <returns>A task that represents the asynchronous commit operation.</returns>
        public override async Task CommitAsync()
        {
            await this.FlushAsync();
            this.committed = true;

            try
            {
                if (this.fileMD5 != null)
                {
                    this.file.Properties.ContentMD5 = this.fileMD5.ComputeHash();
                    await this.file.SetPropertiesAsync(this.accessCondition, this.options, this.operationContext);
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
        private async Task DispatchWriteAsync()
        {
            if (this.internalBuffer.Length == 0)
            {
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
            await this.WriteRangeAsync(bufferToUpload, offset, bufferMD5);
        }

        /// <summary>
        /// Starts an asynchronous WriteRange operation as soon as the parallel
        /// operation semaphore becomes available.
        /// </summary>
        /// <param name="rangeData">Data to be uploaded</param>
        /// <param name="offset">Offset within the file</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteRangeAsync(Stream rangeData, long offset, string contentMD5)
        {
            this.noPendingWritesEvent.Increment();
            await this.parallelOperationSemaphore.WaitAsync();
            Task writePagesTask = this.file.WriteRangeAsync(rangeData, offset, contentMD5, this.accessCondition, this.options, this.operationContext).ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    this.lastException = task.Exception;
                }

                this.noPendingWritesEvent.Decrement();
                this.parallelOperationSemaphore.Release();
            });
        }
    }
}
