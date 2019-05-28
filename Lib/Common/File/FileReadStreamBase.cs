//-----------------------------------------------------------------------
// <copyright file="FileReadStreamBase.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.File
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
    internal abstract class FileReadStreamBase : Stream
    {
        protected CloudFile file;
        protected FileProperties fileProperties;
        protected long currentOffset;
        protected MultiBufferMemoryStream internalBuffer;
        protected int streamMinimumReadSizeInBytes;
        protected AccessCondition accessCondition;
        protected FileRequestOptions options;
        protected OperationContext operationContext;
        protected ChecksumWrapper fileChecksum;
        protected Exception lastException;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileReadStreamBase"/> class.
        /// </summary>
        /// <param name="file">File reference to read from</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        protected FileReadStreamBase(CloudFile file, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            if (options.ChecksumOptions.UseTransactionalMD5.Value)
            {
                CommonUtility.AssertInBounds("StreamMinimumReadSizeInBytes", file.StreamMinimumReadSizeInBytes, 1, Constants.MaxRangeGetContentMD5Size);
            }
            if (options.ChecksumOptions.UseTransactionalCRC64.Value)
            {
                CommonUtility.AssertInBounds("StreamMinimumReadSizeInBytes", file.StreamMinimumReadSizeInBytes, 1, Constants.MaxRangeGetContentCRC64Size);
            }

            this.file = file;
            this.fileProperties = new FileProperties(file.Properties);
            this.currentOffset = 0;
            this.streamMinimumReadSizeInBytes = this.file.StreamMinimumReadSizeInBytes;
            this.internalBuffer = new MultiBufferMemoryStream(file.ServiceClient.BufferManager);
            this.accessCondition = accessCondition;
            this.options = options;
            this.operationContext = operationContext;
            this.fileChecksum =
                new ChecksumWrapper(
                    calcMd5: !(this.options.ChecksumOptions.DisableContentMD5Validation.Value || string.IsNullOrEmpty(this.fileProperties.ContentChecksum.MD5)),
                    calcCrc64: !(this.options.ChecksumOptions.DisableContentCRC64Validation.Value || string.IsNullOrEmpty(this.fileProperties.ContentChecksum.CRC64))
                    );
            this.lastException = null;
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get
            {
                return this.currentOffset;
            }

            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        /// <value>The length in bytes of the stream.</value>
        public override long Length
        {
            get
            {
                return this.fileProperties.Length;
            }
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type <c>SeekOrigin</c> indicating the reference
        /// point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        /// <remarks>Seeking in a FileReadStream disables checksum validation.</remarks>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (this.lastException != null)
            {
                throw this.lastException;
            }

            long newOffset;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newOffset = offset;
                    break;

                case SeekOrigin.Current:
                    newOffset = this.currentOffset + offset;
                    break;

                case SeekOrigin.End:
                    newOffset = this.Length + offset;
                    break;

                default:
                    CommonUtility.ArgumentOutOfRange("origin", origin);
                    throw new ArgumentOutOfRangeException("origin");
            }

            CommonUtility.AssertInBounds("offset", newOffset, 0, this.Length);

            if (newOffset != this.currentOffset)
            {
                long bufferOffset = this.internalBuffer.Position + (newOffset - this.currentOffset);
                if ((bufferOffset >= 0) && (bufferOffset < this.internalBuffer.Length))
                {
                    this.internalBuffer.Position = bufferOffset;
                }
                else
                {
                    this.internalBuffer.SetLength(0);
                }

                this.fileChecksum = null;
                this.currentOffset = newOffset;
            }

            return this.currentOffset;
        }

        /// <summary>
        /// This operation is not supported in FileReadStreamBase.
        /// </summary>
        /// <param name="value">Not used.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This operation is not supported in FileReadStreamBase.
        /// </summary>
        /// <param name="buffer">Not used.</param>
        /// <param name="offset">Not used.</param>
        /// <param name="count">Not used.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This operation is a no-op in FileReadStreamBase.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Read as much as we can from the internal buffer
        /// </summary>
        /// <param name="buffer">The buffer to read the data into.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing
        /// data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>Number of bytes read from the stream.</returns>
        protected int ConsumeBuffer(byte[] buffer, int offset, int count)
        {
            int readCount = this.internalBuffer.Read(buffer, offset, count);
            this.currentOffset += readCount;
            this.VerifyFileChecksum(buffer, offset, readCount);
            return readCount;
        }

        /// <summary>
        /// Calculates the number of bytes to read from the file.
        /// </summary>
        /// <returns>Number of bytes to read.</returns>
        protected int GetReadSize()
        {
            if (this.currentOffset < this.Length)
            {
                return (int)Math.Min(this.streamMinimumReadSizeInBytes, this.Length - this.currentOffset);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Updates the file checksum with newly downloaded content.
        /// </summary>
        /// <param name="buffer">The buffer to read the data from.</param>
        /// <param name="offset">The byte offset in buffer at which to begin reading data.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        protected void VerifyFileChecksum(byte[] buffer, int offset, int count)
        {
            if ((this.fileChecksum != null) && (this.lastException == null) && (count > 0))
            {
                this.fileChecksum.UpdateHash(buffer, offset, count);

                if ((this.currentOffset == this.Length) &&
                    !string.IsNullOrEmpty(this.fileProperties.ContentChecksum.MD5)
                    && this.fileChecksum.MD5 != default(MD5Wrapper))
                {
                    string computedMD5 = this.fileChecksum.MD5.ComputeHash();
                    this.fileChecksum.Dispose();
                    this.fileChecksum = null;

                    if (!computedMD5.Equals(this.fileProperties.ContentChecksum.MD5))
                    {
                        this.lastException = new IOException(string.Format(
                            CultureInfo.InvariantCulture,
                            SR.FileDataCorrupted,
                            this.fileProperties.ContentChecksum.MD5,
                            computedMD5));
                    }
                }

                if ((this.currentOffset == this.Length) &&
                  !string.IsNullOrEmpty(this.fileProperties.ContentChecksum.CRC64)
                  && this.fileChecksum.CRC64 != default(Crc64Wrapper))
                {
                    string computedCRC64 = this.fileChecksum.CRC64.ComputeHash();
                    this.fileChecksum.Dispose();
                    this.fileChecksum = null;

                    if (!computedCRC64.Equals(this.fileProperties.ContentChecksum.CRC64))
                    {
                        this.lastException = new IOException(string.Format(
                            CultureInfo.InvariantCulture,
                            SR.FileDataCorrupted,
                            this.fileProperties.ContentChecksum.CRC64,
                            computedCRC64));
                    }
                }
            }
        }

        /// <summary>
        /// Releases the file resources used by the Stream.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.internalBuffer != null)
                {
                    this.internalBuffer.Dispose();
                    this.internalBuffer = null;
                }

                if (this.fileChecksum != null)
                {
                    this.fileChecksum.Dispose();
                    this.fileChecksum = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
