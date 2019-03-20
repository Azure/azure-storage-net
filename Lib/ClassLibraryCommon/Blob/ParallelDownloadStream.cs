//-----------------------------------------------------------------------
// <copyright file="ParallelDownloadStream.cs" company="Microsoft">
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
#if !WINDOWS_PHONE && !WINDOWS_RT
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A stream wrapper used by <see cref="ParallelDownloadToFile"/> to monitor that progress is being made.
    /// </summary>
    internal sealed class ParallelDownloadStream : Stream
    {
        /// <summary>
        /// The <see cref="MemoryMappedViewStream"/> wrapped stream that is being written to.
        /// </summary>
        private readonly MemoryMappedViewStream downloadStream;

        /// <summary>
        /// A <see cref="CancellationToken"/> which fires if the stream has
        /// not been written to since <see cref="Constants.MaxIdleTimeMs"/>
        /// </summary>
        public CancellationToken HangingCancellationToken
        {
            get
            {
                return cts.Token;
            }
        }

        private int maxIdleTimeInMs;
        private readonly CancellationTokenSource cts;

        public ParallelDownloadStream(MemoryMappedViewStream downloadStream, int maxIdleTimeInMs)
        {
            this.downloadStream = downloadStream;
            this.maxIdleTimeInMs = maxIdleTimeInMs;
            this.cts = new CancellationTokenSource(this.maxIdleTimeInMs);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            this.cts.CancelAfter(this.maxIdleTimeInMs);
            return this.downloadStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.cts.CancelAfter(this.maxIdleTimeInMs);
            this.downloadStream.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            this.downloadStream.Flush();
        }

        public override void Close()
        {
            this.cts.Dispose();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.downloadStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.downloadStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.downloadStream.Read(buffer, offset, count);
        }

        public override bool CanRead
        {
            get
            {
                return this.downloadStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this.downloadStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.downloadStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return this.downloadStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.downloadStream.Position;
            }
            set
            {
                this.downloadStream.Position = value;
            }
        }
    }
#endif
}
