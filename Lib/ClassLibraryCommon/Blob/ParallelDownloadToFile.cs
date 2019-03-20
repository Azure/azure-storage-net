//-----------------------------------------------------------------------
// <copyright file="ParallelDownloadToFile.cs" company="Microsoft">
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
#if !WINDOWS_PHONE && !WINDOWS_RT && !WINDOWS_PHONE_RT
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Core;
    using System.Globalization;

    /// <summary>
    /// Used to download a single blob to a file in parallel.
    /// </summary>
    internal sealed class ParallelDownloadToFile
    {
        /// <summary>
        /// The Task to await for the entire download to complete.
        /// </summary>
        public Task Task { get; private set; }

        private CloudBlob Blob;

        /// <summary>
        /// The starting offset in the blob to start downloading from.
        /// </summary>
        private readonly long Offset;

        /// <summary>
        /// The total length of the download.
        /// </summary>
        private long? Length;

        private readonly string FilePath;

        private CancellationToken cancellationToken;

        private BlobRequestOptions blobRequestOptions;
        private OperationContext operationContext;
        private AccessCondition accessCondition;

        private int MaxIdleTimeInMs
        {
            get;
            set;
        }

        private ParallelDownloadToFile(CloudBlob blob, string filePath, long offset, long? length, int maxIdleTimeInMs, CancellationToken cancellationToken)
        {
            this.FilePath = filePath;
            this.Blob = blob;
            this.Offset = offset;
            this.Length = length;
            this.MaxIdleTimeInMs = maxIdleTimeInMs;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Starts the download of a blob to a file.
        /// </summary>
        /// <param name="blob">The <see cref="CloudBlob"/> to download.</param>
        /// <param name="filePath">A string containing the path to the target file.</param>
        /// <param name="fileMode">A <see cref="System.IO.FileMode"/> enumeration value that determines how to open or create the file.</param>
        /// <param name="parallelIOCount">The maximum number of ranges that can be downloaded concurrently</param>
        /// <param name="rangeSizeInBytes">The size of each individual range in bytes that is being dowloaded in parallel.
        /// The range size must be a multiple of 4 KB and a minimum of 4 MB with a default value of 16 MB.</param>
        /// <param name="offset">The offset of the blob.</param>
        /// <param name="length">The number of bytes to download.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="ParallelDownloadToFile"/> object which contains a task that can be awaited for completion.</returns>
        public static ParallelDownloadToFile Start(CloudBlob blob, string filePath, FileMode fileMode, int parallelIOCount, long? rangeSizeInBytes, long offset, long? length, int maxIdleTimeInMs, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            ParallelDownloadToFile parallelDownload = new ParallelDownloadToFile(blob, filePath, offset, length, maxIdleTimeInMs, cancellationToken);

            parallelDownload.operationContext = operationContext;
            parallelDownload.blobRequestOptions = options;
            parallelDownload.accessCondition = accessCondition;

            parallelDownload.Task = parallelDownload.StartAsync(fileMode, parallelIOCount, rangeSizeInBytes);

            return parallelDownload;
        }

        private async Task StartAsync(FileMode fileMode, int parallelIOCount, long? rangeSizeInBytes)
        {
            CommonUtility.AssertInBounds("parallelIOCount", parallelIOCount, 1);
            bool useTransactionalMD5 = this.blobRequestOptions != null && this.blobRequestOptions.UseTransactionalMD5.HasValue && this.blobRequestOptions.UseTransactionalMD5.Value;
            rangeSizeInBytes = this.ValidateOrGetRangeSize(useTransactionalMD5, rangeSizeInBytes);

            // always do a head request to have an ETag to lock-on to.
            // this code is designed for only large blobs so this request should be neglibile on perf
            await this.Blob.FetchAttributesAsync(this.accessCondition, this.blobRequestOptions, this.operationContext).ConfigureAwait(false);
            if (this.accessCondition == null)
            {
                this.accessCondition = new AccessCondition();
            }

            // it is ok to overwrite the user's IfMatchETag if they had provided one because the HEAD request would have failed
            this.accessCondition.IfMatchETag = this.Blob.Properties.ETag;

            long maxPossibleLength = this.Blob.Properties.Length - this.Offset;
            if (!this.Length.HasValue)
            {
                this.Length = maxPossibleLength;
            }
            else
            {
                this.Length = Math.Min(this.Length.Value, maxPossibleLength);
            }

            // if downloading a zero length blob, just create the file and return
            if (this.Offset == 0 && this.Length.Value == 0)
            {
                File.Create(this.FilePath).Close();
                return;
            }

            // zero size ranges are not allowed except when downloading an empty blob is allowed
            CommonUtility.AssertInBounds("length", this.Length.Value, 1, long.MaxValue);

            int totalIOReadCalls = (int)Math.Ceiling((double)this.Length.Value / (double)rangeSizeInBytes);

            // used to keep track of current ranges being downloaded.
            List<Task> downloadTaskList = new List<Task>();

            // used to dispose of each MemoryMappedViewStream when that range has completed.
            Dictionary<Task, MemoryMappedViewStream> taskToStream = new Dictionary<Task, MemoryMappedViewStream>();

            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(this.FilePath, fileMode, null, this.Length.Value))
            {
                try
                {
                    for (int i = 0; i < totalIOReadCalls; i++)
                    {
                        if (downloadTaskList.Count >= parallelIOCount)
                        {
                            // The number of on-going I/O operations has reached its maximum, wait until one completes or has an error.
                            Task downloadRangeTask = await Task.WhenAny(downloadTaskList).ConfigureAwait(false);

                            // The await on WhenAny does not await on the download task itself, hence exceptions must be repropagated.
                            await downloadRangeTask.ConfigureAwait(false);

                            taskToStream[downloadRangeTask].Dispose();
                            taskToStream.Remove(downloadRangeTask);
                            downloadTaskList.Remove(downloadRangeTask);
                        }

                        long streamBeginIndex = i * rangeSizeInBytes.Value;
                        long streamReadSize = rangeSizeInBytes.Value;

                        // last range may be smaller than the range size
                        if (i == totalIOReadCalls - 1)
                        {
                            streamReadSize = this.Length.Value - i * streamReadSize;
                        }

                        MemoryMappedViewStream viewStream = mmf.CreateViewStream(streamBeginIndex, streamReadSize);
                        Task downloadTask = this.DownloadToStreamWrapperAsync(
                            viewStream,
                            this.Offset + streamBeginIndex, streamReadSize,
                            this.accessCondition,
                            this.blobRequestOptions,
                            this.operationContext,
                            cancellationToken);

                        taskToStream.Add(downloadTask, viewStream);
                        downloadTaskList.Add(downloadTask);
                    }

                    while (downloadTaskList.Count > 0)
                    {
                        // All requests to download the blob have gone out, wait until one completes or has an error.
                        Task downloadRangeTask = await Task.WhenAny(downloadTaskList).ConfigureAwait(false);

                        // The await on WhenAny does not await on the download task itself, hence exceptions must be repropagated.
                        await downloadRangeTask.ConfigureAwait(false);

                        taskToStream[downloadRangeTask].Dispose();
                        taskToStream.Remove(downloadRangeTask);
                        downloadTaskList.Remove(downloadRangeTask);
                    }
                }
                finally
                {
                    foreach (KeyValuePair<Task, MemoryMappedViewStream> taskToStreamPair in taskToStream)
                    {
                        taskToStreamPair.Value.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Wraps the downloadToStream logic to retry/recover the download operation
        /// in the case that the last time the input stream has been written to exceeds a threshold.
        /// </summary>
        private async Task DownloadToStreamWrapperAsync(MemoryMappedViewStream viewStream, long blobOffset, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            long startingOffset = blobOffset;
            long startingLength = length;
            ParallelDownloadStream largeDownloadStream = null;
            try
            {

                while (true)
                {
                    try
                    {
                        largeDownloadStream = new ParallelDownloadStream(viewStream, this.MaxIdleTimeInMs);
                        using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(largeDownloadStream.HangingCancellationToken, cancellationToken))
                        {
                            await this.Blob.DownloadRangeToStreamAsync(
                                largeDownloadStream,
                                blobOffset,
                                length,
                                this.accessCondition,
                                this.blobRequestOptions,
                                this.operationContext,
                                cts.Token).ConfigureAwait(false);
                        }

                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        // only catch if the stream triggered the cancellation
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            blobOffset = startingOffset + largeDownloadStream.Position;
                            length = startingLength - largeDownloadStream.Position;
                            if (length == 0)
                            {
                                break;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            finally
            {
                if (largeDownloadStream != null)
                {
                    largeDownloadStream.Close();
                }
            }
        }

        /// <summary>
        /// If the rangeSizeInBytes has a value, validate it.
        /// Otherwise set the rangeSizeInBytes to the appropriate default vlaue.
        /// </summary>
        /// <param name="useTransactionalMD5">Indicates if transactional MD5 validation is to be used.</param>
        /// <param name="rangeSizeInBytes">The range size in bytes to be used for each download operation
        /// or null to use the default value.</param>
        /// <returns>The rangeSizeInBytes value that was passed in if not null, otherwise the appropriate default value.</returns>
        private long ValidateOrGetRangeSize(bool useTransactionalMD5, long? rangeSizeInBytes)
        {
            if (rangeSizeInBytes.HasValue)
            {
                CommonUtility.AssertInBounds("rangeSizeInBytes", rangeSizeInBytes.Value, Constants.MaxRangeGetContentMD5Size);
                if (useTransactionalMD5 && rangeSizeInBytes.Value != Constants.MaxRangeGetContentMD5Size)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.RangeSizeIsInvalidMD5, rangeSizeInBytes, Constants.MaxRangeGetContentMD5Size));
                }
                else if (rangeSizeInBytes % (4 * Constants.KB) != 0)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.RangeSizeIsInvalid, rangeSizeInBytes, Constants.MaxRangeGetContentMD5Size));
                }
            }
            else if (useTransactionalMD5)
            {
                rangeSizeInBytes = Constants.MaxRangeGetContentMD5Size;
            }
            else
            {
                rangeSizeInBytes = Constants.DefaultParallelDownloadRangeSizeBytes;
            }

            return rangeSizeInBytes.Value;
        }
    }
#endif
}
