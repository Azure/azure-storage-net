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


namespace Microsoft.Azure.Storage.Core
{
    using Microsoft.Azure.Storage.Core.Util;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Stream that will throw a <see cref="OperationCanceledException"/> if it has to wait longer than a configurable timeout to read or write more data
    /// </summary>
    internal class TimeoutStream : Stream
    {
        private readonly Stream wrappedStream;
        private TimeSpan readTimeout;
        private TimeSpan writeTimeout;
        private CancellationTokenSource cancellationTokenSource;

        public TimeoutStream(Stream wrappedStream, TimeSpan timeout)
            : this(wrappedStream, timeout, timeout) { }

        public TimeoutStream(Stream wrappedStream, TimeSpan readTimeout, TimeSpan writeTimeout)
        {
            CommonUtility.AssertNotNull("WrappedStream", wrappedStream);
            CommonUtility.AssertNotNull("ReadTimeout", readTimeout);
            CommonUtility.AssertNotNull("WriteTimeout", writeTimeout);
            this.wrappedStream = wrappedStream;
            this.readTimeout = readTimeout;
            this.writeTimeout = writeTimeout;
            this.UpdateReadTimeout();
            this.UpdateWriteTimeout();
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public override long Position
        {
            get { return this.wrappedStream.Position; }
            set { this.wrappedStream.Position = value; }
        }

        public override long Length
        {
            get { return this.wrappedStream.Length; }
        }

        public override bool CanWrite
        {
            get { return this.wrappedStream.CanWrite; }
        }

        public override bool CanTimeout
        {
            get { return this.wrappedStream.CanTimeout; }
        }

        public override bool CanSeek
        {
            get { return this.wrappedStream.CanSeek; }
        }

        public override bool CanRead
        {
            get { return this.wrappedStream.CanRead; }
        }

        public override int ReadTimeout
        {
            get { return (int) this.readTimeout.TotalMilliseconds; }
            set {
                this.readTimeout = TimeSpan.FromMilliseconds(value);
                this.UpdateReadTimeout();
            }
        }

        public override int WriteTimeout
        {
            get { return (int) this.writeTimeout.TotalMilliseconds; }
            set
            {
                this.writeTimeout = TimeSpan.FromMilliseconds(value);
                this.UpdateWriteTimeout();
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return this.wrappedStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override void Flush()
        {
            this.wrappedStream.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            var source = StartTimeout(cancellationToken, out bool dispose);
            try
            {
                await this.wrappedStream.FlushAsync(source.Token);
            }
            finally
            {
                StopTimeout(source, dispose);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return wrappedStream.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var source = StartTimeout(cancellationToken, out bool dispose);
            try
            {
                return await this.wrappedStream.ReadAsync(buffer, offset, count, source.Token);
            }
            finally
            {
                StopTimeout(source, dispose);
            }
        }

        public override int ReadByte()
        {
            return this.wrappedStream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.wrappedStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.wrappedStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.wrappedStream.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var source = StartTimeout(cancellationToken, out bool dispose);
            try
            {
                await this.wrappedStream.WriteAsync(buffer, offset, count, source.Token);
            }
            finally
            {
                StopTimeout(source, dispose);
            }
        }

        public override void WriteByte(byte value)
        {
            this.wrappedStream.WriteByte(value);
        }

        private CancellationTokenSource StartTimeout(CancellationToken additionalToken, out bool dispose)
        {
            if (this.cancellationTokenSource.IsCancellationRequested)
            {
                this.cancellationTokenSource = new CancellationTokenSource();
            }

            CancellationTokenSource source;
            if (additionalToken.CanBeCanceled)
            {
                source = CancellationTokenSource.CreateLinkedTokenSource(additionalToken, this.cancellationTokenSource.Token);
                dispose = true;
            }
            else
            {
                source = this.cancellationTokenSource;
                dispose = false;
            }

            this.cancellationTokenSource.CancelAfter(this.readTimeout);

            return source;
        }

        private void StopTimeout(CancellationTokenSource source, bool dispose)
        {
            this.cancellationTokenSource.CancelAfter(Timeout.InfiniteTimeSpan);
            if (dispose)
            {
                source.Dispose();
            }
        }

        private void UpdateReadTimeout()
        {
            if (this.wrappedStream.CanTimeout)
            {
                try
                {
                    this.wrappedStream.ReadTimeout = (int)this.readTimeout.TotalMilliseconds;
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void UpdateWriteTimeout()
        {
            if (this.wrappedStream.CanTimeout)
            {
                try
                {
                    this.wrappedStream.WriteTimeout = (int)this.writeTimeout.TotalMilliseconds;
                }
                catch
                {
                    // ignore
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.cancellationTokenSource.Dispose();
                this.wrappedStream.Dispose();
            }
        }
    }
}