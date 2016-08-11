//-----------------------------------------------------------------------
// <copyright file="NonCloseableStream.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    internal class NonCloseableStream : Stream
    {
        private readonly Stream wrappedStream;

        /// <summary>
        /// Initializes a new instance of the NonCloseableStream class This stream ensures that the user stream
        /// is not closed even when the enclosing crypto stream is closed in order to flush the final block of data.
        /// </summary>
        /// <param name="wrappedStream">The stream to wrap.</param>
        public NonCloseableStream(Stream wrappedStream)
        {
            CommonUtility.AssertNotNull("WrappedStream", wrappedStream);

            this.wrappedStream = wrappedStream;
        }

        public override bool CanRead
        {
            get { return this.wrappedStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return this.wrappedStream.CanSeek; }
        }

        public override bool CanTimeout
        {
            get { return this.wrappedStream.CanTimeout; }
        }

        public override bool CanWrite
        {
            get { return this.wrappedStream.CanWrite; }
        }

        public override long Length
        {
            get { return this.wrappedStream.Length; }
        }

        public override long Position
        {
            get { return this.wrappedStream.Position; }
            set { this.wrappedStream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return this.wrappedStream.ReadTimeout; }
            set { this.wrappedStream.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return this.wrappedStream.WriteTimeout; }
            set { this.wrappedStream.WriteTimeout = value; }
        }

        public override void Flush()
        {
            this.wrappedStream.Flush();
        }

        public override void SetLength(long value)
        {
            this.wrappedStream.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.wrappedStream.Seek(offset, origin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.wrappedStream.Read(buffer, offset, count);
        }

#if WINDOWS_DESKTOP
        /// <summary>
        /// Begins an asynchronous read operation.
        /// </summary>
        /// <param name="buffer">When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the read is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An IAsyncResult that represents the asynchronous read, which could still be pending.</returns>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.wrappedStream.BeginRead(buffer, offset, count, callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous read to complete.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
        public override int EndRead(IAsyncResult asyncResult)
        {
            return this.wrappedStream.EndRead(asyncResult);
        }
#endif

#if WINDOWS_RT || NETCORE
        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer to write the data into.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing data from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.wrappedStream.ReadAsync(buffer, offset, count, cancellationToken);
        }
#endif

#if WINDOWS_DESKTOP
        /// <summary>
        /// Begins an asynchronous write operation.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the write is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>An IAsyncResult that represents the asynchronous write, which could still be pending.</returns>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.wrappedStream.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <summary>
        /// Ends an asynchronous write operation.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            this.wrappedStream.EndWrite(asyncResult);
        }
#endif

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.wrappedStream.Write(buffer, offset, count);
        }

#if WINDOWS_RT || NETCORE
        /// <summary>
        /// Asynchronously writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The zero-based byte offset in buffer from which to begin copying bytes to the stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.wrappedStream.WriteAsync(buffer, offset, count, cancellationToken);
        }
#endif

        protected override void Dispose(bool disposing)
        {
            // no-op
        }
    }
}
