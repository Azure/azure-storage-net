//-----------------------------------------------------------------------
// <copyright file="ByteCountingStream.cs" company="Microsoft">
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

    /// <summary>
    /// This class provides a wrapper that will update the Ingress / Egress bytes of a given request result as the stream is used.
    /// Note this is not supported for Windows RT / .Net 4.5 as some Async methods may not be able to be intercepted.
    /// </summary>
    internal class ByteCountingStream : Stream
    {
        private readonly Stream wrappedStream;
        private readonly RequestResult requestObject;

        /// <summary>
        /// Initializes a new instance of the ByteCountingStream class with an expandable capacity initialized to zero.
        /// </summary>
        public ByteCountingStream(Stream wrappedStream, RequestResult requestObject)
            : base()
        {
            CommonUtility.AssertNotNull("WrappedStream", wrappedStream);
            CommonUtility.AssertNotNull("RequestObject", requestObject);
            this.wrappedStream = wrappedStream;
            this.requestObject = requestObject;
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
            int read = this.wrappedStream.Read(buffer, offset, count);
            this.requestObject.IngressBytes += read;
            return read;
        }

        public override int ReadByte()
        {
            int val = this.wrappedStream.ReadByte();

            if (val != -1)
            {
                ++this.requestObject.IngressBytes;
            }

            return val;
        }

        public override void Close()
        {
            this.wrappedStream.Close();
        }

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
            int read = this.wrappedStream.EndRead(asyncResult);
            this.requestObject.IngressBytes += read;
            return read;
        }

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
            IAsyncResult res = this.wrappedStream.BeginWrite(buffer, offset, count, callback, state);
            this.requestObject.EgressBytes += count;
            return res;
        }

        /// <summary>
        /// Ends an asynchronous write operation.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            this.wrappedStream.EndWrite(asyncResult);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.wrappedStream.Write(buffer, offset, count);
            this.requestObject.EgressBytes += count;
        }

        public override void WriteByte(byte value)
        {
            this.wrappedStream.WriteByte(value);
            ++this.requestObject.EgressBytes;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.wrappedStream.Dispose();
            }
        }
    }
}