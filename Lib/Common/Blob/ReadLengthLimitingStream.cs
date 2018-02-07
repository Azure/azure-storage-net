//-----------------------------------------------------------------------
// <copyright file="ReadLengthLimitingStream.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Core
{
    using Microsoft.Azure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Stream that will be used to limit read operations on a wrapped stream.
    /// </summary>
    internal class ReadLengthLimitingStream : Stream
    {
        private readonly Stream wrappedStream;
        private long streamBeginIndex;
        private long position;
        private long length;
        private bool disposed = false;

        public ReadLengthLimitingStream(Stream wrappedStream, long length)
        {
            if (!wrappedStream.CanSeek || !wrappedStream.CanRead)
            {
                throw new NotSupportedException();
            }
            
            CommonUtility.AssertNotNull("wrappedSream", wrappedStream);
            this.wrappedStream = wrappedStream;
            this.length = Math.Min(length, wrappedStream.Length - wrappedStream.Position);
            this.streamBeginIndex = wrappedStream.Position;
            this.Position = 0;
        }

        public override bool CanRead
        {
            get
            {
                return this.wrappedStream.CanRead;
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

        public override long Length
        {
            get
            {
                return this.length;
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Position
        {
            get
            {
                return this.position;
            }

            set
            {
                CommonUtility.AssertInBounds("position", value, 0, this.Length);
                this.position = value;
                this.wrappedStream.Position = this.streamBeginIndex + value;
            }
        }

        public override void Flush()
        {
            this.wrappedStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long startIndex;

            // Map offset to the specified SeekOrigin of the substream.
            switch (origin)
            {
                case SeekOrigin.Begin:
                    startIndex = 0;
                    break;

                case SeekOrigin.Current:
                    startIndex = this.Position;
                    break;

                case SeekOrigin.End:
                    startIndex = this.length - offset;
                    offset = 0;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.Position = startIndex + offset;
            return this.Position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            
            if (this.Position + count > this.Length)
            {
                count = (int)(this.Length - this.Position);
            }

            int bytesRead = this.wrappedStream.Read(buffer, offset, count);
            this.Position += bytesRead;
            return bytesRead;
        }

#if WINDOWS_DESKTOP
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (this.Position + count > this.Length)
            {
                count = (int)(this.Length - this.Position);
            }

            return this.wrappedStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            int bytesRead = this.wrappedStream.EndRead(asyncResult);
            this.Position += bytesRead;
            return bytesRead;
        }
#endif

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (this.Position + count > this.Length)
            {
                count = (int)(this.Length - this.Position);
            }

            int bytesRead = await this.wrappedStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            this.Position += bytesRead;
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                this.wrappedStream.Dispose();
            }

            disposed = true;
            base.Dispose(disposing);
        }
    }
}