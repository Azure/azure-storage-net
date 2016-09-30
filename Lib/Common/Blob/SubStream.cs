//-----------------------------------------------------------------------
// <copyright file="SubStream.cs" company="Microsoft">
//    Copyright 2016 Microsoft Corporation
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
    using System;
    using System.IO;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;

    /// <summary>
    /// A read-only wrapper class that creates a logical substream from a region within an existing stream.
    /// Allows for concurrent, asynchronous read and seek operations on a wrapped stream.
    /// Ensures thread-safe operations between related substream instances via a user supplied shared mutex.
    /// </summary>
    internal sealed class SubStream : Stream
    {

        // Stream to be logically wrapped.
        private Stream wrappedStream;

        // Position in the wrapped stream at which the SubStream should logically begin.
        private long streamBeginIndex;

        // Total length of the substream.
        private long substreamLength;

        // Non-blocking semaphore for controlling read operations between related SubStream instances.
        public SemaphoreSlim Mutex { get; set; }

        // Current relative position in the substream.
        public override long Position { get; set; }

        // Total length of the substream.
        public override long Length
        {
            get
            {
                return this.substreamLength;
            }
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        private void CheckDisposed()
        {
            if (wrappedStream == null)
            {
                throw new ObjectDisposedException("SubStreamWrapper");
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.wrappedStream = null;
        }


        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Creates a new SubStream instance.
        /// </summary>
        /// <param name="stream">A seekable source stream.</param>
        /// <param name="streamBeginIndex">The index in the wrapped stream where the logical SubStream should begin.</param>
        /// <param name="substreamLength">The length of the SubStream.</param>
        /// <param name="globalSemaphore"> A <see cref="SemaphoreSlim"/> object that is shared between related SubStream instances.</param>
        /// <remarks>
        /// The source stream to be wrapped must be seekable.
        /// The Semaphore object provided must have the initialCount thread parameter set to one to ensure only one concurrent request is granted at a time.
        /// </remarks>
        public SubStream(Stream stream, long streamBeginIndex, long substreamLength, SemaphoreSlim globalSemaphore)
        {
            if (stream == null)
            {
                throw new ArgumentNullException();
            }
            else if (!stream.CanSeek)
            {
                throw new NotSupportedException("Stream must be seekable.");
            }
            else if (globalSemaphore == null)
            {
                throw new ArgumentNullException();
            }

            CommonUtility.AssertInBounds("streamBeginIndex", streamBeginIndex , 0, stream.Length);
            CommonUtility.AssertInBounds("substreamLength", streamBeginIndex + substreamLength, 0, stream.Length);
            
            this.Position = 0;
            this.streamBeginIndex = streamBeginIndex;
            this.wrappedStream = stream;
            this.Mutex = globalSemaphore;
            this.substreamLength = substreamLength;
        }


#if !(WINDOWS_RT || NETCORE)
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.ReadAsync(buffer, offset, count).AsApm<int>(callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            CommonUtility.AssertNotNull("AsyncResult", asyncResult);
            return ((Task<int>)asyncResult).Result;
        }
#endif

        /// <summary>
        /// Reads a block of bytes from the wrapped stream within the substream bounds asynchronously and writes the data to a buffer.
        /// </summary>
        /// <param name="buffer">When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read.</param>
        /// <param name="cancellationToken">An object of type <see cref="CancellationToken"/> that propgates notification that operation should be cancelled.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the substream has been reached.</returns>
        /// <remarks>
        /// This method will allow only one read request to the underlying wrapped stream at a time, 
        /// while concurrent requests will be queued up by effect of the shared semaphore mutex.
        /// </remarks>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
           int result = 0;
            await Mutex.WaitAsync(cancellationToken);
            try
            {
                result = await this.ReadAsyncHelper(buffer, offset, count, cancellationToken);
            }
            finally
            {
                Mutex.Release();
            }

            return result;
        }

        private async Task<int> ReadAsyncHelper(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            long readEndAbsolutePosition = this.streamBeginIndex + this.Position + count;
            long streamEndAbsolutePosition = this.streamBeginIndex + this.Length;
            long currentAbsolutePosition = this.streamBeginIndex + this.Position;

            CommonUtility.AssertInBounds("offset", currentAbsolutePosition, this.streamBeginIndex, streamEndAbsolutePosition);

            // Only read bytes up to the end of the substream
            if (readEndAbsolutePosition >= streamEndAbsolutePosition)
            {
                count = (int)(streamEndAbsolutePosition - currentAbsolutePosition);
            }

            this.wrappedStream.Seek(currentAbsolutePosition, SeekOrigin.Begin);
            int result = await wrappedStream.ReadAsync(buffer, offset, count, cancellationToken);
            this.Position += result;
            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the position within the current substream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current substream.</returns>
        /// <exception cref="NotSupportedException">Thrown if using the unsupported <paramref name="origin"/> SeekOrigin.End </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> is invalid for SeekOrigin.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            long startIndex;

            // Map relative offset of the substream to an absolute offset in the wrapped stream.
            switch (origin)
            {
                case SeekOrigin.Begin:
                    startIndex = this.streamBeginIndex;
                    break;

                case SeekOrigin.Current:
                    startIndex = this.streamBeginIndex + this.Position;
                    break;

                case SeekOrigin.End:
                    throw new NotSupportedException();

                default:
                    throw new ArgumentOutOfRangeException();
            }

            CommonUtility.AssertInBounds("offset", startIndex + offset, this.streamBeginIndex, this.streamBeginIndex + this.Length);

            this.Position = startIndex + offset;
            return this.Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}