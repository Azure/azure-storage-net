//-----------------------------------------------------------------------
// <copyright file="BlobDecryptStream.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;

    /// <summary>
    /// Stream that will be used for decrypting blob ranges. It buffers 16 bytes of IV (if required) before creating a crypto stream and routing the 
    /// rest of the data through it.
    /// </summary>
    internal class BlobDecryptStream : Stream
    {
        private readonly Stream userStream;
        private readonly IDictionary<string, string> metadata;
        private long position;
        private long? userProvidedLength;
        private byte[] iv = new byte[16];
        private BlobEncryptionPolicy encryptionPolicy;
        private int discardFirst;
        private Stream cryptoStream;
        private bool bufferIV;
        private bool noPadding;
        private bool disposed;
        private bool? requireEncryption;
        private ICryptoTransform transform;

        public BlobDecryptStream(Stream userStream, IDictionary<string, string> metadata, long? userProvidedLength, int discardFirst, bool bufferIV, bool noPadding, BlobEncryptionPolicy policy, bool? requireEncryption)
        {
            this.userStream = userStream;
            this.metadata = metadata;
            this.userProvidedLength = userProvidedLength;
            this.discardFirst = discardFirst;
            this.encryptionPolicy = policy;
            this.bufferIV = bufferIV;
            this.noPadding = noPadding;
            this.requireEncryption = requireEncryption;
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get 
            { 
                return true; 
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
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
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
            this.userStream.Flush();
        }

        public override void SetLength(long value)
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Keep buffering until we have 16 bytes of IV.
            if (this.bufferIV && this.position < 16)
            {
                int bytesToCopy = 16 - (int)this.position;
                bytesToCopy = count > bytesToCopy ? bytesToCopy : count;
                Array.Copy(buffer, offset, this.iv, (int)this.position, bytesToCopy);
                this.position += bytesToCopy;
                offset += bytesToCopy;
                count -= bytesToCopy;
            }

            // Wrap user stream with LengthLimitingStream. This stream will be used to discard the extra bytes we downloaded in order to deal with AES block size.
            // Create crypto stream around the length limiting stream once per download and start writing to it. During retries, the state is maintained and 
            // new crypto streams will not be created each time. 
            if (this.cryptoStream == null)
            {
                LengthLimitingStream lengthLimitingStream = new LengthLimitingStream(this.userStream, this.discardFirst, this.userProvidedLength);
                this.cryptoStream = this.encryptionPolicy.DecryptBlob(lengthLimitingStream, this.metadata, out this.transform, this.requireEncryption, iv: !this.bufferIV ? null : this.iv, noPadding: this.noPadding);
            }

            // Route the remaining data through the crypto stream.
            if (count > 0)
            {
                this.cryptoStream.Write(buffer, offset, count);
                this.position += count;
            }
        }

#if WINDOWS_DESKTOP
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            // Keep buffering until we have 16 bytes of IV.
            if (this.bufferIV && this.position < 16)
            {
                int bytesToCopy = 16 - (int)this.position;
                bytesToCopy = count > bytesToCopy ? bytesToCopy : count;
                Array.Copy(buffer, offset, this.iv, (int)this.position, bytesToCopy);
                this.position += bytesToCopy;
                offset += bytesToCopy;
                count -= bytesToCopy;
            }

            // Wrap user stream with LengthLimitingStream. This stream will be used to discard the extra bytes we downloaded in order to deal with AES block size.
            // Create crypto stream around the length limiting stream once per download and start writing to it. During retries, the state is maintained and 
            // new crypto streams will not be created each time. 
            if (this.cryptoStream == null)
            {
                LengthLimitingStream lengthLimitingStream = new LengthLimitingStream(this.userStream, this.discardFirst, this.userProvidedLength);
                this.cryptoStream = this.encryptionPolicy.DecryptBlob(lengthLimitingStream, this.metadata, out this.transform, this.requireEncryption, iv: !this.bufferIV ? null : this.iv, noPadding: this.noPadding);
            }

            StorageAsyncResult<NullType> storageAsyncResult = new StorageAsyncResult<NullType>(callback, state);
            if (count <= 0)
            {
                storageAsyncResult.OnComplete();
            }
            else
            {
                // Route the remaining data through the crypto stream.
                storageAsyncResult.OperationState = count;
                this.cryptoStream.BeginWrite(buffer, offset, count, this.WriteStreamCallback, storageAsyncResult);
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
                this.cryptoStream.EndWrite(ar);
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
            if (!this.disposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    this.cryptoStream.Close();
                }

                // Dispose the ICryptoTransform object created for decryption.
                if (this.transform != null)
                {
                    this.transform.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
