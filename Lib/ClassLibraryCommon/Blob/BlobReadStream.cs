//-----------------------------------------------------------------------
// <copyright file="BlobReadStream.cs" company="Microsoft">
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
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Util;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class BlobReadStream : BlobReadStreamBase
    {
        private volatile bool readPending = false;

        /// <summary>
        /// Initializes a new instance of the BlobReadStream class.
        /// </summary>
        /// <param name="blob">Blob reference to read from</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        internal BlobReadStream(CloudBlob blob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
            : base(blob, accessCondition, options, operationContext)
        {
        }        

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the
        /// position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer to read the data into.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing
        /// data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer. This can be
        /// less than the number of bytes requested if that many bytes are not
        /// currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Begins an asynchronous read operation.
        /// </summary>
        /// <param name="buffer">The buffer to read the data into.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing
        /// data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the read is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An <c>IAsyncResult</c> that represents the asynchronous read, which could still be pending.</returns>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return CancellableAsyncResultTaskWrapper.Create(token => this.ReadAsync(buffer, offset, count, token), callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous read to complete.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns>The total number of bytes read into the buffer. This can be
        /// less than the number of bytes requested if that many bytes are not
        /// currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((CancellableAsyncResultTaskWrapper<int>)asyncResult).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current stream, advances the
        /// position within the stream by the number of bytes read, and monitors cancellation requests.
        /// </summary>
        /// <remarks>In the returned <see cref="Task{TElement}"/> object, the value of the integer
        /// parameter contains the total number of bytes read into the buffer. The result value can be
        /// less than the number of bytes requested if the number of bytes currently available is less
        /// than the requested number, or it can be 0 (zero) if the end of the stream has been reached.</remarks>
        /// <param name="buffer">The buffer to read the data into.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing
        /// data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);
            CommonUtility.AssertInBounds("offset", offset, 0, buffer.Length);
            CommonUtility.AssertInBounds("count", count, 0, buffer.Length - offset);

            if (this.lastException != null)
            {
                throw this.lastException;
            }

            if ((this.currentOffset == this.Length) || (count == 0))
            {
                return Task.FromResult(0);
            }

            int readCount = this.ConsumeBuffer(buffer, offset, count);
            if (readCount > 0)
            {
                return Task.FromResult(readCount);
            }

            return this.DispatchReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// Dispatches a sync read operation that either reads from the cache or makes a call to
        /// the server.
        /// </summary>
        /// <param name="buffer">The buffer to read the data into.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing
        /// data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Number of bytes read from the stream.</returns>
        private async Task<int> DispatchReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                this.internalBuffer.SetLength(0);
                await this.blob.DownloadRangeToStreamAsync(
                    this.internalBuffer,
                    this.currentOffset,
                    this.GetReadSize(),
                    this.accessCondition,
                    this.options,
                    this.operationContext,
                    cancellationToken).ConfigureAwait(false);

                this.internalBuffer.Seek(0, SeekOrigin.Begin);
                return this.ConsumeBuffer(buffer, offset, count);
            }
            catch (Exception e)
            {
                this.lastException = e;
                throw;
            }
        }
    }
}
