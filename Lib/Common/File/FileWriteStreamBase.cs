//-----------------------------------------------------------------------
// <copyright file="FileWriteStreamBase.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
    internal abstract class FileWriteStreamBase :
        CloudFileStream
    {
        protected CloudFile file;
        protected long fileSize;
        protected bool newFile;
        protected long currentOffset;
        protected long currentFileOffset;
        protected int streamWriteSizeInBytes;
        protected MultiBufferMemoryStream internalBuffer;
        protected AccessCondition accessCondition;
        protected FileRequestOptions options;
        protected OperationContext operationContext;
        protected CounterEvent noPendingWritesEvent;
        protected MD5Wrapper fileMD5;
        protected MD5Wrapper rangeMD5;
        protected AsyncSemaphore parallelOperationSemaphore;
        protected volatile Exception lastException;
        protected volatile bool committed;
        protected bool disposed;

        /// <summary>
        /// Initializes a new instance of the FileWriteStreamBase class for a file.
        /// </summary>
        /// <param name="file">File reference to write to.</param>
        /// <param name="fileSize">Size of the file.</param>
        /// <param name="createNew">Use <c>true</c> if the file is newly created, <c>false</c> otherwise.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object for tracking the current operation.</param>
        protected FileWriteStreamBase(CloudFile file, long fileSize, bool createNew, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
            : base()
        {
            this.internalBuffer = new MultiBufferMemoryStream(file.ServiceClient.BufferManager);
            this.currentOffset = 0;
            this.accessCondition = accessCondition;
            this.options = options;
            this.operationContext = operationContext;
            this.noPendingWritesEvent = new CounterEvent();
            this.fileMD5 = this.options.StoreFileContentMD5.Value ? new MD5Wrapper() : null;
            this.rangeMD5 = this.options.UseTransactionalMD5.Value ? new MD5Wrapper() : null;
            this.parallelOperationSemaphore = new AsyncSemaphore(options.ParallelOperationThreadCount.Value);
            this.lastException = null;
            this.committed = false;
            this.disposed = false;
            this.currentFileOffset = 0;
            this.file = file;
            this.fileSize = fileSize;
            this.streamWriteSizeInBytes = file.StreamWriteSizeInBytes;
            this.newFile = createNew;
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return false;
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
                return true;
            }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length
        {
            get
            {
                return this.fileSize;
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
        /// This operation is not supported in FileWriteStreamBase.
        /// </summary>
        /// <param name="buffer">Not used.</param>
        /// <param name="offset">Not used.</param>
        /// <param name="count">Not used.</param>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Calculates the new position within the current stream for a Seek operation.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type <c>SeekOrigin</c> indicating the reference
        /// point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        protected long GetNewOffset(long offset, SeekOrigin origin)
        {
            if (!this.CanSeek)
            {
                throw new NotSupportedException();
            }

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

            return newOffset;
        }

        /// <summary>
        /// This operation is not supported in FileWriteStreamBase.
        /// </summary>
        /// <param name="value">Not used.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Releases the file resources used by the Stream.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.fileMD5 != null)
                {
                    this.fileMD5.Dispose();
                    this.fileMD5 = null;
                }

                if (this.rangeMD5 != null)
                {
                    this.rangeMD5.Dispose();
                    this.rangeMD5 = null;
                }

                if (this.internalBuffer != null)
                {
                    this.internalBuffer.Dispose();
                    this.internalBuffer = null;
                }

                if (this.noPendingWritesEvent != null)
                {
                    this.noPendingWritesEvent.Dispose();
                    this.noPendingWritesEvent = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
