//-----------------------------------------------------------------------
// <copyright file="CloudBlockBlob.Common.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a blob that is uploaded as a set of blocks.
    /// </summary>
    public partial class CloudBlockBlob : CloudBlob, ICloudBlob
    {
        /// <summary>
        /// Default is 4 MB.
        /// </summary>
        private int streamWriteSizeInBytes = Constants.DefaultWriteBlockSizeBytes;

        /// <summary>
        /// Flag to determine if the block size was modified.
        /// </summary>
        private bool isStreamWriteSizeModified = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlockBlob"/> class using an absolute URI to the blob.
        /// </summary>
        /// <param name="blobAbsoluteUri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        public CloudBlockBlob(Uri blobAbsoluteUri)
            : this(blobAbsoluteUri, null /* credentials */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlockBlob"/> class using an absolute URI to the blob.
        /// </summary>
        /// <param name="blobAbsoluteUri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudBlockBlob(Uri blobAbsoluteUri, StorageCredentials credentials)
            : this(blobAbsoluteUri, null /* snapshotTime */, credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlockBlob"/> class using an absolute URI to the blob.
        /// </summary>
        /// <param name="blobAbsoluteUri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudBlockBlob(Uri blobAbsoluteUri, DateTimeOffset? snapshotTime, StorageCredentials credentials)
            : this(new StorageUri(blobAbsoluteUri), snapshotTime, credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlockBlob"/> class using an absolute URI to the blob.
        /// </summary>
        /// <param name="blobAbsoluteUri">A <see cref="StorageUri"/> containing the absolute URI to the blob at both the primary and secondary locations.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudBlockBlob(StorageUri blobAbsoluteUri, DateTimeOffset? snapshotTime, StorageCredentials credentials)
            : base(blobAbsoluteUri, snapshotTime, credentials)
        {
            this.Properties.BlobType = BlobType.BlockBlob;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlockBlob"/> class using the specified blob name and
        /// the parent container reference.
        /// If snapshotTime is not null, the blob instance represents a Snapshot.
        /// </summary>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="snapshotTime">Snapshot time in case the blob is a snapshot.</param>
        /// <param name="container">The reference to the parent container.</param>
        internal CloudBlockBlob(string blobName, DateTimeOffset? snapshotTime, CloudBlobContainer container)
            : base(blobName, snapshotTime, container)
        {
            this.Properties.BlobType = BlobType.BlockBlob;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlockBlob"/> class.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="serviceClient">The service client.</param>
        internal CloudBlockBlob(BlobAttributes attributes, CloudBlobClient serviceClient)
            : base(attributes, serviceClient)
        {
            this.Properties.BlobType = BlobType.BlockBlob;
        }

        /// <summary>
        /// Gets or sets the block size for writing to a block blob.
        /// </summary>
        /// <value>The size of a block, in bytes, ranging from between 16 KB and 100 MB inclusive.</value>
        public int StreamWriteSizeInBytes
        {
            get
            {
                return this.streamWriteSizeInBytes;
            }

            set
            {
                CommonUtility.AssertInBounds("StreamWriteSizeInBytes", value, 16 * Constants.KB, Constants.MaxBlockSize);
                this.isStreamWriteSizeModified = true;
                this.streamWriteSizeInBytes = value;
            }
        }

        /// <summary>
        /// Gets the modified block size flag.
        /// </summary>
        internal bool IsStreamWriteSizeModified
        {
            get
            {
                return this.isStreamWriteSizeModified;
            }
        }

        /// <summary>
        /// Uploads an enumerable collection of seekable streams.
        /// </summary>
        /// <param name="streamList">An enumerable collection of seekable streams to be uploaded.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning", Justification = "Reviewed.")]
        [DoesServiceRequest]
        internal async Task UploadFromMultiStreamAsync(IEnumerable<Stream> streamList, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("streamList", streamList);
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            int parallelOperations = modifiedOptions.ParallelOperationThreadCount.Value;
            List<string> blockList = new List<string>();
            List<Task> uploadTaskList = new List<Task>();
            int blockNum = 0;

            foreach (Stream block in streamList)
            {
                if (uploadTaskList.Count == parallelOperations)
                {
                    await Task.WhenAny(uploadTaskList.ToArray()).ConfigureAwait(false);
                    uploadTaskList.RemoveAll(putBlockUpload => putBlockUpload.IsCompleted);
                }

                string blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("Block_{0}", (++blockNum).ToString("00000"))));
                blockList.Add(blockId);

                // Capture stream.
                Stream localBlock = block;

                try
                {
                    Task uploadTask = this.PutBlockAsync(blockId, block, null, accessCondition, modifiedOptions, operationContext, cancellationToken);
                    Task cleanupTask = uploadTask.ContinueWith(finishedUpload =>
                    {
                        localBlock.Dispose();
                    });

                    uploadTaskList.Add(uploadTask);
                }
                catch (Exception)
                {
                    // This is necessary in case an exception is thrown in PutBlockAsync before the continuation is registered.
                    localBlock.Dispose();
                    throw;
                }
            }

            await Task.WhenAll(uploadTaskList).ConfigureAwait(false);
            await this.PutBlockListAsync(blockList, accessCondition, modifiedOptions, operationContext, cancellationToken).ConfigureAwait(false);
        }

#if !WINDOWS_RT
        /// <summary>
        /// Returns an enumerable collection of unique FileStream handles that represent the specified file in logical blocks.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <returns>An enumerable collection of <see cref="FileStream"/> objects each positioned at multiples of the StreamWriteSizeInBytes.</returns>
        private IEnumerable<Stream> OpenMultiFileStream(string path)
        {
            long length = new System.IO.FileInfo(path).Length;

            int totalBlocks = (int)Math.Ceiling((double)length / this.streamWriteSizeInBytes);

            for (long i = 0; i < totalBlocks; i++)
            {
                FileStream f = new FileStream(path, FileMode.Open, FileAccess.Read);
                f.Seek(i * this.streamWriteSizeInBytes, SeekOrigin.Begin);
                yield return new ReadLengthLimitingStream(f, this.streamWriteSizeInBytes);
            }
        }
#endif

        /// <summary>
        /// Returns an enumerable collection of SubStream handles that wraps a seekable stream.
        /// This method is intended for usage within the Large BlockBlob upload algorithm.
        /// </summary>
        /// <param name="wrappedStream">The seekable <see cref="Stream"/> object to be wrapped.</param>
        /// <param name="length">The length (copyValue) of the stream.</param>
        /// <param name="mutex">A <see cref="SemaphoreSlim"/> object which serves as an intrinsic lock/mutex to manage concurrent operations.</param>
        /// <returns>
        /// An enumerable collection of <see cref="SubStream"/> objects,
        /// each representing multiples of the StreamWriteSizeInBytes (blocks) in the wrapped stream.
        /// </returns>
        private IEnumerable<Stream> OpenMultiSubStream(Stream wrappedStream, long? length, SemaphoreSlim mutex)
        {
            if (!wrappedStream.CanSeek)
            {
                throw new ArgumentException();
            }

            long streamLength = length ?? (wrappedStream.Length - wrappedStream.Position);
            int totalBlocks = (int)Math.Ceiling((double)streamLength / (double)this.streamWriteSizeInBytes);
            long offset = wrappedStream.Position;
            SemaphoreSlim streamReadThrottler = new SemaphoreSlim(1);

            for (long i = 0; i < totalBlocks; i++)
            {
                // Stream abstraction to create a logical substream of a region within an underlying stream.
                yield return new SubStream(
                    wrappedStream,
                    offset + (i * this.streamWriteSizeInBytes),
                    this.streamWriteSizeInBytes,
                    streamReadThrottler);
            }
        }

        /// <summary>
        /// Check if the total required blocks for the upload exceeds the maximum allowable block limit.
        /// Adjusts the block size to ensure a successful upload only if the value has not been explicitly set.
        /// Otherwise, throws a StorageException if the default value has been changed or if the blob size exceeds the maximum capacity.
        /// </summary>
        /// <param name="streamLength">The length of the stream.</param>
        internal void CheckAdjustBlockSize(long? streamLength)
        {
            if (streamLength.HasValue)
            {
                long totalBlocks = (int) Math.Ceiling((double) streamLength/(double) this.streamWriteSizeInBytes);

                // Check if the total required blocks for the upload exceeds the maximum allowable block limit.
                if (totalBlocks > Constants.MaxBlockNumber)
                {
                    if (this.IsStreamWriteSizeModified || streamLength > Constants.MaxBlobSize)
                    {
                        throw new StorageException(SR.BlobOverMaxBlockLimit);
                    }
                    else
                    {
                        // Scale the block size to ensure a successful upload (only if the user did not specify a value).
                        this.streamWriteSizeInBytes = (int) Math.Ceiling((double) streamLength/(double) Constants.MaxBlockNumber);
                    }
                }
            }
        }
    }
}
