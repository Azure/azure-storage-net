//-----------------------------------------------------------------------
// <copyright file="BlobEncryptedWriteStream.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;

#if WINDOWS_PHONE
    using System.Threading.Tasks;
#endif

    internal sealed class BlobEncryptedWriteStream : CloudBlobStream
    {
        private bool disposed;
        private BlobWriteStream writeStream;
        private CryptoStream cryptoStream;
        private ICryptoTransform transform;

        /// <summary>
        /// Initializes a new instance of the BlobWriteStream class for a block blob.
        /// </summary>
        /// <param name="blockBlob">Blob reference to write to.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="transform">The ICryptoTransform function for the request.</param>
        internal BlobEncryptedWriteStream(CloudBlockBlob blockBlob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, ICryptoTransform transform)
        {
            CommonUtility.AssertNotNull("transform", transform);

            if (options.EncryptionPolicy.EncryptionMode != BlobEncryptionMode.FullBlob)
            {
                throw new InvalidOperationException(SR.InvalidEncryptionMode, null);
            }

            // Since this is done on the copy of the options object that the client lib maintains and not on the user's options object and is done after getting 
            // the transform function, it should be fine. Setting this ensures that an error is not thrown when PutBlock is called internally from the write method on the stream.
            options.SkipEncryptionPolicyValidation = true;

            this.transform = transform;
            this.writeStream = new BlobWriteStream(blockBlob, accessCondition, options, operationContext) { IgnoreFlush = true };
            this.cryptoStream = new CryptoStream(this.writeStream, transform, CryptoStreamMode.Write);
        }

        /// <summary>
        /// Initializes a new instance of the BlobWriteStream class for a page blob.
        /// </summary>
        /// <param name="pageBlob">Blob reference to write to.</param>
        /// <param name="pageBlobSize">Size of the page blob.</param>
        /// <param name="createNew">Use <c>true</c> if the page blob is newly created, <c>false</c> otherwise.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="transform">The ICryptoTransform function for the request.</param>        
        internal BlobEncryptedWriteStream(CloudPageBlob pageBlob, long pageBlobSize, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, ICryptoTransform transform)
        {
            CommonUtility.AssertNotNull("transform", transform);

            if (options.EncryptionPolicy.EncryptionMode != BlobEncryptionMode.FullBlob)
            {
                throw new InvalidOperationException(SR.InvalidEncryptionMode, null);
            }

            // Since this is done on the copy of the options object that the client lib maintains and not on the user's options object and is done after getting 
            // the transform function, it should be fine. Setting this ensures that an error is not thrown when PutPage is called internally from the write method on the stream.
            options.SkipEncryptionPolicyValidation = true;

            this.transform = transform;
            this.writeStream = new BlobWriteStream(pageBlob, pageBlobSize, createNew, accessCondition, options, operationContext) { IgnoreFlush = true };
            this.cryptoStream = new CryptoStream(this.writeStream, transform, CryptoStreamMode.Write);
        }

        /// <summary>
        /// Initializes a new instance of the BlobWriteStream class for an append blob.
        /// </summary>
        /// <param name="appendBlob">Blob reference to write to.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="transform">The ICryptoTransform function for the request.</param>        
        internal BlobEncryptedWriteStream(CloudAppendBlob appendBlob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, ICryptoTransform transform)
        {
            CommonUtility.AssertNotNull("transform", transform);

            if (options.EncryptionPolicy.EncryptionMode != BlobEncryptionMode.FullBlob)
            {
                throw new InvalidOperationException(SR.InvalidEncryptionMode, null);
            }

            // Since this is done on the copy of the options object that the client lib maintains and not on the user's options object and is done after getting 
            // the transform function, it should be fine. Setting this ensures that an error is not thrown when AppendBlock is called internally from the write method on the stream.
            options.SkipEncryptionPolicyValidation = true;

            this.transform = transform;
            this.writeStream = new BlobWriteStream(appendBlob, accessCondition, options, operationContext) { IgnoreFlush = true };
            this.cryptoStream = new CryptoStream(this.writeStream, transform, CryptoStreamMode.Write);
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
                return false;
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
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// This operation is not supported in BlobWriteStreamBase.
        /// </summary>
        /// <param name="buffer">Not used.</param>
        /// <param name="offset">Not used.</param>
        /// <param name="count">Not used.</param>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This operation is not supported in BlobWriteStreamBase.
        /// </summary>
        /// <param name="value">Not used.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type <c>SeekOrigin</c> indicating the reference
        /// point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current
        /// position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from
        /// buffer to the current stream. </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.EndWrite(this.BeginWrite(buffer, offset, count, null /* callback */, null /* state */));
        }

        /// <summary>
        /// Begins an asynchronous write operation.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from
        /// buffer to the current stream. </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the write is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>An <c>IAsyncResult</c> that represents the asynchronous write, which could still be pending.</returns>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNull("buffer", buffer);
            CommonUtility.AssertInBounds("offset", offset, 0, buffer.Length);
            CommonUtility.AssertInBounds("count", count, 0, buffer.Length - offset);

            return this.cryptoStream.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous write to complete.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            this.cryptoStream.EndWrite(asyncResult);
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying blob.
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Begins an asynchronous flush operation.
        /// </summary>
        /// <param name="callback">An optional asynchronous callback, to be called when the flush is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous flush request from other requests.</param>
        /// <returns>An <c>ICancellableAsyncResult</c> that represents the asynchronous flush, which could still be pending.</returns>
        public override ICancellableAsyncResult BeginFlush(AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Waits for the pending asynchronous flush to complete.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> object containing a reference to the pending asynchronous request to finish.</param>
        public override void EndFlush(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }

#if SYNC
        /// <summary>
        /// Clears all buffers for this stream, causes any buffered data to be written to the underlying blob, and commits the blob. This should be the last operation
        /// on the stream.
        /// </summary>
        public override void Commit()
        {
            // Flush the CryptoStream in order to make sure that the last block of data is flushed.
            this.cryptoStream.FlushFinalBlock();

            // Since the blob should be committed, we should now allow flush to go through and make the service call.
            this.writeStream.IgnoreFlush = false;

            // Commit the BlobWriteStream in order to ensure that the blob is committed on the service.
            this.writeStream.Commit();
        }
#endif

        /// <summary>
        /// Begins an asynchronous commit operation.
        /// </summary>
        /// <param name="callback">An optional asynchronous callback, to be called when the commit is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous commit request from other requests.</param>
        /// <returns>An <c>ICancellableAsyncResult</c> that represents the asynchronous commit, which could still be pending.</returns>
        public override ICancellableAsyncResult BeginCommit(AsyncCallback callback, object state)
        {
            // Flush the CryptoStream in order to make sure that the last block of data is flushed. This call is a sync call
            // but it is ok to have it since we are not actually going to do any I/O (IgnoreFlush on WriteStream is true at this point).
            this.cryptoStream.FlushFinalBlock();

            // Since the blob should be committed, we should now allow flush to go through and make the service call.
            this.writeStream.IgnoreFlush = false;
            return this.writeStream.BeginCommit(callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous commit to complete.
        /// </summary>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> object containing a reference to the pending asynchronous request to finish.</param>
        public override void EndCommit(IAsyncResult asyncResult)
        {
            this.writeStream.EndCommit(asyncResult);
        }

        /// <summary>
        /// Releases the blob resources used by the Stream.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    // Since the blob should be committed, we should now allow flush to go through and make the service call.
                    this.writeStream.IgnoreFlush = false;

                    // The BlobWriteStream's Commit is only called from Dispose if it hasn't already been committed. So it is ok to call Dispose here which calls 
                    // the BlobWriteStream's Dispose.
                    this.cryptoStream.Dispose();

                    // Dispose the ICryptoTransform object created for encryption.
                    if (this.transform != null)
                    {
                        this.transform.Dispose();
                    }
                }
            }
        }
    }
}
