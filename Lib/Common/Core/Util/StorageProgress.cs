//-----------------------------------------------------------------------
// <copyright file="StorageProgress.cs" company="Microsoft">
//    Copyright 2017 Microsoft Corporation
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

#if !WINDOWS_RT
namespace Microsoft.WindowsAzure.Storage.Core.Util
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Holds information about the progress data transfers for both request and response streams in a single operation.
    /// </summary>
    /// <remarks>
    /// ## Examples
    /// [!code-csharp[StorageProgress](~/azure-storage-net/Test/WindowsRuntime/Blob/BlobUploadDownloadTest.cs#sample_StorageProgress_NetCore "StorageProgress Sample")]
    /// </remarks>
    public sealed class StorageProgress
    {
        /// <summary>
        /// Progress in bytes of the request data transfer.
        /// </summary>
        public long BytesTransferred { get; private set; }

        /// <summary>
        /// Creates a <see cref="StorageProgress"/> object.
        /// </summary>
        /// <param name="bytesTransferred">The progress value being reported.</param>
        public StorageProgress(long bytesTransferred)
        {
            this.BytesTransferred = bytesTransferred;
        }

    }

    /// <summary>
    /// An accumulator for request and response data transfers.
    /// </summary>
    internal sealed class AggregatingProgressIncrementer : IProgress<long>
    {
        long currentValue;
        bool currentValueHasValue;

        IProgress<StorageProgress> innerHandler;

        public Stream CreateProgressIncrementingStream(Stream stream)
        {
            return new ProgressIncrementingStream(stream, this);
        }

        public AggregatingProgressIncrementer(IProgress<StorageProgress> innerHandler)
        {
            this.innerHandler = innerHandler;
        }

        /// <summary>
        /// Increments the current value and reports it to the progress handler
        /// </summary>
        /// <param name="bytes"></param>
        public void Report(long bytes)
        {
            Interlocked.Add(ref this.currentValue, bytes);
            Volatile.Write(ref this.currentValueHasValue, true);

            if (this.innerHandler != null)
            {
                StorageProgress current = this.Current;

                if (current != null)
                {
                    this.innerHandler.Report(current);
                }
            }
        }

        /// <summary>
        /// Zeroes out the current accumulation, and reports it to the progress handler
        /// </summary>
        public void Reset()
        {
            long currentActual = Volatile.Read(ref this.currentValue);

            this.Report(-currentActual);
        }

        static readonly AggregatingProgressIncrementer nullHandler = new AggregatingProgressIncrementer(default(IProgress<StorageProgress>));

        /// <summary>
        /// Returns an instance that no-ops accumulation.
        /// </summary>
        public static AggregatingProgressIncrementer None
        {
            get
            {
                return nullHandler;
            }
        }

        /// <summary>
        /// Returns a StorageProgress instance representing the current progress value.
        /// </summary>
        public StorageProgress Current
        {
            get
            {
                StorageProgress result = default(StorageProgress);

                if (this.currentValueHasValue)
                {
                    long currentActual = Volatile.Read(ref this.currentValue);

                    result = new StorageProgress(currentActual);
                }

                return result;
            }
        }
    }

    /// <summary>
    /// Wraps a stream, and reports position updates to a progress incrementer
    /// </summary>
    internal class ProgressIncrementingStream : Stream
    {
        Stream innerStream;
        AggregatingProgressIncrementer incrementer;

        public ProgressIncrementingStream(Stream stream, AggregatingProgressIncrementer incrementer)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (incrementer == null)
            {
                throw new ArgumentNullException("incrementer");
            }

            this.innerStream = stream;
            this.incrementer = incrementer;
        }

        public override bool CanRead
        {
            get
            {
                return this.innerStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this.innerStream.CanSeek;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return this.innerStream.CanTimeout;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.innerStream.CanWrite;
            }
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var oldPosition = this.innerStream.Position;

            await this.innerStream.CopyToAsync(destination, bufferSize, cancellationToken);

            var newPosition = this.innerStream.Position;

            this.incrementer.Report(newPosition - oldPosition);
        }

        protected override void Dispose(bool disposing)
        {
            this.innerStream.Dispose();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            var oldPosition = this.innerStream.Position;

            await this.innerStream.FlushAsync(cancellationToken);

            var newPosition = this.innerStream.Position;

            this.incrementer.Report(newPosition - oldPosition);
        }

        public override void Flush()
        {
            var oldPosition = this.innerStream.Position;

            this.innerStream.Flush();

            var newPosition = this.innerStream.Position;

            this.incrementer.Report(newPosition - oldPosition);
        }

        public override long Length
        {
            get
            {
                return this.innerStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.innerStream.Position;
            }

            set
            {
                var delta = value - this.innerStream.Position;

                this.innerStream.Position = value;

                this.incrementer.Report(delta);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = this.innerStream.Read(buffer, offset, count);
            this.incrementer.Report(n);
            return n;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var n = await this.innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            this.incrementer.Report(n);
            return n;
        }

        public override int ReadByte()
        {
            var b = this.innerStream.ReadByte();

            if (b != -1) // -1 = end of stream sentinel
            {
                this.incrementer.Report(1);
            }

            return b;
        }

        public override int ReadTimeout
        {
            get
            {
                return this.innerStream.ReadTimeout;
            }

            set
            {
                this.innerStream.ReadTimeout = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var oldPosition = this.innerStream.Position;

            var newPosition = this.innerStream.Seek(offset, origin);

            this.incrementer.Report(newPosition - oldPosition);

            return newPosition;
        }

        public override void SetLength(long value)
        {
            this.innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.innerStream.Write(buffer, offset, count);

            this.incrementer.Report(count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await this.innerStream.WriteAsync(buffer, offset, count, cancellationToken);

            this.incrementer.Report(count);
        }

        public override void WriteByte(byte value)
        {
            this.innerStream.WriteByte(value);

            this.incrementer.Report(1);
        }

        public override int WriteTimeout
        {
            get
            {
                return this.innerStream.WriteTimeout;
            }

            set
            {
                this.innerStream.WriteTimeout = value;
            }
        }
    }
}
#endif