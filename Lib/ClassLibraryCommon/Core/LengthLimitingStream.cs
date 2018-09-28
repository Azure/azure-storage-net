//-----------------------------------------------------------------------
// <copyright file="LengthLimitingStream.cs" company="Microsoft">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Stream that will be used for decrypting blob ranges. It is used to discard extra bytes from the beginning and end if required.
    /// </summary>
    internal class LengthLimitingStream : Stream
    {
        private readonly Stream wrappedStream;
        private long startOffset;
        private long? endOffset;
        private long position;
        private long? length;

        public LengthLimitingStream(Stream wrappedStream, long start, long? length = null)
        {
            this.wrappedStream = wrappedStream;
            this.startOffset = start;
            this.length = length;
            if (length.HasValue)
            {
                this.endOffset = this.startOffset + (this.length - 1);
            }
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
                return this.wrappedStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.wrappedStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return this.length.HasValue ? this.length.Value : this.wrappedStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.position;
            }

            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        public override void Flush()
        {
            this.wrappedStream.Flush();
        }

        public override void SetLength(long value)
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;

                case SeekOrigin.Current:
                    newPosition = this.position + offset;
                    break;

                case SeekOrigin.End:
                    newPosition = this.Length + offset;
                    break;

                default:
                    CommonUtility.ArgumentOutOfRange("origin", origin);
                    throw new ArgumentOutOfRangeException("origin");
            }

            CommonUtility.AssertInBounds("offset", newPosition, 0, this.Length);

            this.position = newPosition;
            return this.position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.wrappedStream.Read(buffer, offset, count);
        }

#if WINDOWS_DESKTOP
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.wrappedStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return this.wrappedStream.EndRead(asyncResult);
        }
#endif

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Discard bytes at the beginning if required.
            if (this.position < this.startOffset)
            {
                int discardBytes = (int)Math.Min(this.startOffset - this.position, count);
                offset += discardBytes;
                count -= discardBytes;

                this.position += discardBytes;
            }

            // Discard bytes at the end if required.
            if (this.endOffset.HasValue)
            {
                count = (int)Math.Min(this.endOffset.Value + 1 - this.position, count);
            }

            // If there are any bytes in the buffer left to be written, write to the underlying stream and update position.
            if (count > 0)
            {
                this.wrappedStream.Write(buffer, offset, count);
                this.position += count;
            }
        }

#if WINDOWS_DESKTOP
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            // Discard bytes at the beginning if required.
            if (this.position < this.startOffset)
            {
                int discardBytes = (int)Math.Min(this.startOffset - this.position, count);
                offset += discardBytes;
                count -= discardBytes;

                this.position += discardBytes;
            }

            // Discard bytes at the end if required.
            if (this.endOffset.HasValue)
            {
                count = (int)Math.Min(this.endOffset.Value + 1 - this.position, count);
            }

            StorageAsyncResult<NullType> storageAsyncResult = new StorageAsyncResult<NullType>(callback, state);
            if (count <= 0)
            {
                storageAsyncResult.OnComplete();
            }
            else
            {
                // If there are any bytes in the buffer left to be written, write to the underlying stream and update position.
                storageAsyncResult.OperationState = count;
                this.wrappedStream.BeginWrite(buffer, offset, count, this.WriteStreamCallback, storageAsyncResult);
            }

            return storageAsyncResult;
        }

        private void WriteStreamCallback(IAsyncResult ar)
        {
            StorageAsyncResult<NullType> storageAsyncResult = (StorageAsyncResult<NullType>)ar.AsyncState;
            storageAsyncResult.UpdateCompletedSynchronously(ar.CompletedSynchronously);

            Exception endException = null;
            try
            {
                this.wrappedStream.EndWrite(ar);
                this.position += (int)storageAsyncResult.OperationState;
            }
            catch (Exception e)
            {
                endException = e;
            }

            storageAsyncResult.OnComplete(endException);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            StorageAsyncResult<NullType> storageAsyncResult = (StorageAsyncResult<NullType>)asyncResult;
            storageAsyncResult.End();
        }
#endif

        protected override void Dispose(bool disposing)
        {
            // no-op
        }
    }
}
