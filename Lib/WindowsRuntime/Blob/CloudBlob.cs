//-----------------------------------------------------------------------
// <copyright file="CloudBlob.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Blob.Protocol;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

#if NETCORE
    using System.Threading;
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
    using Windows.Foundation.Metadata;
    using Windows.Storage;
    using Windows.Storage.Streams;
    using System.Threading;
#endif

    /// <summary>
    /// Represents a blob.
    /// </summary>
    public partial class CloudBlob : IListBlobItem
    {
        /// <summary>
        /// Opens a stream for reading from the blob.
        /// </summary>
        /// <returns>A stream to be used for reading from the blob.</returns>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync()
        {
            return this.OpenReadAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Opens a stream for reading from the blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A stream to be used for reading from the blob.</returns>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.OpenReadAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Opens a stream for reading from the blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A stream to be used for reading from the blob.</returns>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return Task.Run<Stream>(async () =>
            {
                await this.FetchAttributesAsync(accessCondition, options, operationContext, cancellationToken);
                AccessCondition streamAccessCondition = AccessCondition.CloneConditionWithETag(accessCondition, this.Properties.ETag);
                BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, this.BlobType, this.ServiceClient, false);
                return new BlobReadStream(this, streamAccessCondition, modifiedOptions, operationContext);
            }, cancellationToken);
        }

        /// <summary>
        /// Downloads the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target)
        {
            return this.DownloadToStreamAsync(target, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadRangeToStreamAsync(target, null /* offset */, null /* length */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Downloads the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToStreamAsync(target, null /* offset */, null /* length */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Downloads the contents of a blob to a file.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode)
        {
            return this.DownloadToFileAsync(path, mode, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#else
        /// <param name="target">The target file.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(StorageFile target)
        {
            return this.DownloadToFileAsync(target, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#endif

        /// <summary>
        /// Downloads the contents of a blob to a file.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToFileAsync(path, mode, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <param name="target">The target file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(StorageFile target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return DownloadToFileAsync(target, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the contents of a blob to a file.
        /// </summary>
        /// <param name="target">The target file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(StorageFile target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                using (StorageStreamTransaction transaction = await target.OpenTransactedWriteAsync().AsTask(cancellationToken))
                {
                    await this.DownloadToStreamAsync(transaction.Stream.AsStream(), accessCondition, options, operationContext, cancellationToken);
                    await transaction.CommitAsync();
                }
            });
        }
#endif

#if NETCORE
        /// <summary>
        /// Downloads the contents of a blob to a file.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);

            return Task.Run(async () =>
            {
                FileStream stream = new FileStream(path, mode, FileAccess.Write);

                try
                {
                    using (stream)
                    {
                        await this.DownloadToStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
                    }
                }
                catch (Exception)
                {
                    if (mode == FileMode.Create || mode == FileMode.CreateNew)
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch (Exception)
                        {
                            // Best effort to clean up in the event that download was unsuccessful.
                            // Do not throw as we want to throw original exception.
                        }
                    }

                    throw;
                }

            }, cancellationToken);
        }
#endif

        /// <summary>
        /// Downloads the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index)
        {
            return this.DownloadToByteArrayAsync(target, index, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, null /* blobOffset */, null /* length */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Downloads the contents of a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, null /* blobOffset */, null /* length */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Downloads a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("target", target);

            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);

            // We should always call AsStreamForWrite with bufferSize=0 to prevent buffering. Our
            // stream copier only writes 64K buffers at a time anyway, so no buffering is needed.
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.GetBlobImpl(this.attributes, target, offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Downloads a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, blobOffset, length, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, blobOffset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                using (SyncMemoryStream stream = new SyncMemoryStream(target, index))
                {
                    await this.DownloadRangeToStreamAsync(stream, blobOffset, length, accessCondition, options, operationContext, cancellationToken);
                    return (int)stream.Position;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Checks existence of the blob.
        /// </summary>
        /// <returns><c>true</c> if the blob exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
        {
            return this.ExistsAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Checks existence of the blob.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the blob exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(false, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Checks existence of the blob.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the blob exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.ExistsAsync(false, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Checks existence of the blob.
        /// </summary>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the blob exists.</returns>
        private Task<bool> ExistsAsync(bool primaryOnly, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.ExistsImpl(this.attributes, modifiedOptions, primaryOnly),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Populates a blob's properties and metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync()
        {
            return this.FetchAttributesAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Populates a blob's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Populates a blob's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.FetchAttributesImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Updates the blob's metadata.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync()
        {
            return this.SetMetadataAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Updates the blob's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Updates the blob's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.SetMetadataImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Updates the blob's properties.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync()
        {
            return this.SetPropertiesAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Updates the blob's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.SetPropertiesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Updates the blob's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.SetPropertiesImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Deletes the blob.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
        {
            return this.DeleteAsync(DeleteSnapshotsOption.None, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Deletes the blob.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(deleteSnapshotsOption, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the blob.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.DeleteBlobImpl(this.attributes, deleteSnapshotsOption, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Deletes the blob if it already exists.
        /// </summary>
        /// <returns><c>true</c> if the blob already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
        {
            return this.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Deletes the blob if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the blob already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(deleteSnapshotsOption, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the blob if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the blob already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, this.BlobType, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () =>
            {
                bool exists = await this.ExistsAsync(true, modifiedOptions, operationContext, cancellationToken);

                if (!exists)
                {
                    return false;
                }

                try
                {
                    await this.DeleteAsync(deleteSnapshotsOption, accessCondition, modifiedOptions, operationContext, cancellationToken);
                    return true;
                }
                catch (Exception)
                {
                    if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.NotFound)
                    {
                        StorageExtendedErrorInformation extendedInfo = operationContext.LastResult.ExtendedErrorInformation;
                        if ((extendedInfo == null) ||
                            (extendedInfo.ErrorCode == BlobErrorCodeStrings.BlobNotFound))
                        {
                            return false;
                        }
                        else
                        {
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            });
        }

        /// <summary>
        /// Acquires a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not <c>null</c>, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <returns>The ID of the acquired lease.</returns>
        [DoesServiceRequest]
        public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId = null)
        {
            return this.AcquireLeaseAsync(leaseTime, proposedLeaseId, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Acquires a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not <c>null</c>, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">The options for this operation. If <c>null</c>, default options will be used.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The ID of the acquired lease.</returns>
        [DoesServiceRequest]
        public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AcquireLeaseAsync(leaseTime, proposedLeaseId, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Acquires a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not <c>null</c>, this must be
        /// greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">The options for this operation. If <c>null</c>, default options will be used.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The ID of the acquired lease.</returns>
        [DoesServiceRequest]
        public virtual Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.AcquireLeaseImpl(this.attributes, leaseTime, proposedLeaseId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Renews a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task RenewLeaseAsync(AccessCondition accessCondition)
        {
            return this.RenewLeaseAsync(accessCondition, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Renews a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <param name="options">The options for this operation. If <c>null</c>, default options will be used.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.RenewLeaseAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Renews a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <param name="options">The options for this operation. If <c>null</c>, default options will be used.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.RenewLeaseImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Changes the lease ID on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease. This cannot be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <returns>The new lease ID.</returns>
        [DoesServiceRequest]
        public virtual Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition)
        {
            return this.ChangeLeaseAsync(proposedLeaseId, accessCondition, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Changes the lease ID on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease. This cannot be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <param name="options">The options for this operation. If <c>null</c>, default options will be used.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The new lease ID.</returns>
        [DoesServiceRequest]
        public virtual Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ChangeLeaseAsync(proposedLeaseId, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Changes the lease ID on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease. This cannot be <c>null</c>.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <param name="options">The options for this operation. If <c>null</c>, default options will be used.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The new lease ID.</returns>
        [DoesServiceRequest]
        public virtual Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.ChangeLeaseImpl(this.attributes, proposedLeaseId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Releases the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ReleaseLeaseAsync(AccessCondition accessCondition)
        {
            return this.ReleaseLeaseAsync(accessCondition, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Releases the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <param name="options">The options for this operation. If <c>null</c>, default options will be used.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.ReleaseLeaseAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Releases the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <param name="options">The options for this operation. If <c>null</c>, default options will be used.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.ReleaseLeaseImpl(this.attributes, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Breaks the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds. If <c>null</c>, the break period is the remainder of the current lease,
        /// or zero for infinite leases.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the amount of time before the lease ends, to the second.</returns>
        [DoesServiceRequest]
        public virtual Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod)
        {
            return this.BreakLeaseAsync(breakPeriod, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Breaks the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds. If null, the break period is the remainder of the current lease,
        /// or zero for infinite leases.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">The options for this operation. If null, default options will be used.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the amount of time before the lease ends, to the second.</returns>
        [DoesServiceRequest]
        public virtual Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.BreakLeaseAsync(breakPeriod, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Breaks the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds. If null, the break period is the remainder of the current lease,
        /// or zero for infinite leases.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">The options for this operation. If null, default options will be used.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the amount of time before the lease ends, to the second.</returns>
        [DoesServiceRequest]
        public virtual Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.BreakLeaseImpl(this.attributes, breakPeriod, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlob> SnapshotAsync()
        {
            return this.SnapshotAsync(null /* metadata */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlob> SnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.SnapshotAsync(metadata, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlob> SnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.SnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Begins an operation to start copying an existing blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The URI of a source blob.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source)
        {
            return this.StartCopyAsync(source, null /* sourceAccessCondition */, null /* destAccessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Begins an operation to start copying an existing blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The URI of a source blob.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Begins an operation to start copying a blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The URI of a source blob.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("source", source);
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.Unspecified, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.StartCopyImpl(this.attributes, source, sourceAccessCondition, destAccessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Aborts an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId)
        {
            return this.AbortCopyAsync(copyId, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Aborts an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AbortCopyAsync(copyId, accessCondition, options, operationContext, CancellationToken.None);

        }

        /// <summary>
        /// Aborts an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.AbortCopyImpl(this.attributes, copyId, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Implements getting the blob.
        /// </summary>
        /// 
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="destStream">The target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the blob, in bytes.</param>
        /// <param name="length">The length of the data to download from the blob, in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="SynchronousTask"/> that gets the stream.</returns>
        private RESTCommand<NullType> GetBlobImpl(BlobAttributes attributes, Stream destStream, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options)
        {
            string lockedETag = null;
            AccessCondition lockedAccessCondition = null;

            bool isRangeGet = offset.HasValue;
            bool arePropertiesPopulated = false;
            string storedMD5 = null;

            long startingOffset = offset.HasValue ? offset.Value : 0;
            long? startingLength = length;
            long? validateLength = null;

            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.DestinationStream = destStream;
            getCmd.CalculateMd5ForResponseStream = !options.DisableContentMD5Validation.Value;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Get(uri, serverTimeout, attributes.SnapshotTime, offset, length, options.UseTransactionalMD5.Value, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.RecoveryAction = (cmd, ex, ctx) =>
            {
                if ((lockedAccessCondition == null) && !string.IsNullOrEmpty(lockedETag))
                {
                    lockedAccessCondition = AccessCondition.GenerateIfMatchCondition(lockedETag);
                    if (accessCondition != null)
                    {
                        lockedAccessCondition.LeaseId = accessCondition.LeaseId;
                    }
                }

                if (cmd.StreamCopyState != null)
                {
                    offset = startingOffset + cmd.StreamCopyState.Length;
                    if (startingLength.HasValue)
                    {
                        length = startingLength.Value - cmd.StreamCopyState.Length;
                    }
                }

                getCmd.BuildRequest = (command, uri, builder, cnt, serverTimeout, context) => BlobHttpRequestMessageFactory.Get(uri, serverTimeout, attributes.SnapshotTime, offset, length, options.UseTransactionalMD5.Value && !arePropertiesPopulated, lockedAccessCondition ?? accessCondition, cnt, context, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            };

            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(offset.HasValue ? HttpStatusCode.PartialContent : HttpStatusCode.OK, resp, NullType.Value, cmd, ex);

                if (!arePropertiesPopulated)
                {
                    CloudBlob.UpdateAfterFetchAttributes(attributes, resp, isRangeGet);

                    if (resp.Content.Headers.ContentMD5 != null)
                    {
                        storedMD5 = Convert.ToBase64String(resp.Content.Headers.ContentMD5);
                    }

                    if (!options.DisableContentMD5Validation.Value &&
                        options.UseTransactionalMD5.Value &&
                        string.IsNullOrEmpty(storedMD5))
                    {
                        throw new StorageException(
                            cmd.CurrentResult,
                            SR.MD5NotPresentError,
                            null)
                        {
                            IsRetryable = false
                        };
                    }

                    // If the download fails and Get Blob needs to resume the download, going to the
                    // same storage location is important to prevent a possible ETag mismatch.
                    getCmd.CommandLocationMode = cmd.CurrentResult.TargetLocation == StorageLocation.Primary ? CommandLocationMode.PrimaryOnly : CommandLocationMode.SecondaryOnly;
                    lockedETag = attributes.Properties.ETag;
                    validateLength = resp.Content.Headers.ContentLength;

                    arePropertiesPopulated = true;
                }

                return NullType.Value;
            };

            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                HttpResponseParsers.ValidateResponseStreamMd5AndLength(validateLength, storedMD5, cmd);
                return Task.FromResult(NullType.Value);
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the FetchAttributes method. The attributes are updated immediately.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that fetches the attributes.</returns>
        private RESTCommand<NullType> FetchAttributesImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetProperties(uri, serverTimeout, attributes.SnapshotTime, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateAfterFetchAttributes(attributes, resp, false);
                return NullType.Value;
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the Exists method. The attributes are updated immediately.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="primaryOnly">If <c>true</c>, the command will be executed against the primary location.</param>
        /// <returns>A <see cref="RESTCommand"/> that checks existence.</returns>
        private RESTCommand<bool> ExistsImpl(BlobAttributes attributes, BlobRequestOptions options, bool primaryOnly)
        {
            RESTCommand<bool> getCmd = new RESTCommand<bool>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = primaryOnly ? CommandLocationMode.PrimaryOnly : CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetProperties(uri, serverTimeout, attributes.SnapshotTime, null /* accessCondition */, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, true, cmd, ex);
                CloudBlob.UpdateAfterFetchAttributes(attributes, resp, false);
                return true;
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the SetMetadata method.
        /// </summary>
        /// <param name="blob">The blob object that is calling this method.</param>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetMetadataImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.SetMetadata(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                BlobHttpRequestMessageFactory.AddMetadata(msg, attributes.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the SetProperties method.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetPropertiesImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.SetProperties(uri, serverTimeout, attributes.Properties, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                BlobHttpRequestMessageFactory.AddMetadata(msg, attributes.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implements the DeleteBlob method.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that deletes the blob.</returns>
        private RESTCommand<NullType> DeleteBlobImpl(BlobAttributes attributes, DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> deleteCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(deleteCmd);
            deleteCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Delete(uri, serverTimeout, attributes.SnapshotTime, deleteSnapshotsOption, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            deleteCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex);

            return deleteCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand"/> for acquiring a lease.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="leaseTime">A <see cref="TimeSpan"/> representing the span of time for which to acquire the lease, which will be rounded down to seconds. If null, an infinite lease will be acquired. If not null, this must be greater than zero.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease ID is proposed.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> implementing the acquire lease operation.</returns>
        private RESTCommand<string> AcquireLeaseImpl(BlobAttributes attributes, TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options)
        {
            int leaseDuration = -1;
            if (leaseTime.HasValue)
            {
                CommonUtility.AssertInBounds("leaseTime", leaseTime.Value, TimeSpan.FromSeconds(1), TimeSpan.MaxValue);
                leaseDuration = (int)leaseTime.Value.TotalSeconds;
            }

            RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Lease(uri, serverTimeout, LeaseAction.Acquire, proposedLeaseId, leaseDuration, null /* leaseBreakPeriod */, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, null /* retVal */, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                return BlobHttpResponseParsers.GetLeaseId(resp);
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand"/> for renewing a lease.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> implementing the renew lease operation.</returns>
        private RESTCommand<NullType> RenewLeaseImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("accessCondition", accessCondition);
            if (accessCondition.LeaseId == null)
            {
                throw new ArgumentException(SR.MissingLeaseIDRenewing, "accessCondition");
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Lease(uri, serverTimeout, LeaseAction.Renew, null /* proposedLeaseId */, null /* leaseDuration */, null /* leaseBreakPeriod */, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand"/> for changing a lease ID.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="proposedLeaseId">The proposed new lease ID.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> implementing the change lease ID operation.</returns>
        private RESTCommand<string> ChangeLeaseImpl(BlobAttributes attributes, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("accessCondition", accessCondition);
            CommonUtility.AssertNotNull("proposedLeaseId", proposedLeaseId);
            if (accessCondition.LeaseId == null)
            {
                throw new ArgumentException(SR.MissingLeaseIDChanging, "accessCondition");
            }

            RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Lease(uri, serverTimeout, LeaseAction.Change, proposedLeaseId, null /* leaseDuration */, null /* leaseBreakPeriod */, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                return BlobHttpResponseParsers.GetLeaseId(resp);
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand"/> for releasing a lease.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> implementing the release lease operation.</returns>
        private RESTCommand<NullType> ReleaseLeaseImpl(BlobAttributes attributes, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("accessCondition", accessCondition);
            if (accessCondition.LeaseId == null)
            {
                throw new ArgumentException(SR.MissingLeaseIDReleasing, "accessCondition");
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Lease(uri, serverTimeout, LeaseAction.Release, null /* proposedLeaseId */, null /* leaseDuration */, null /* leaseBreakPeriod */, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Generates a <see cref="RESTCommand"/> for breaking a lease.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="breakPeriod">The amount of time to allow the lease to remain, rounded down to seconds. If null, the break period is the remainder of the current lease, or zero for infinite leases.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> implementing the break lease operation.</returns>
        private RESTCommand<TimeSpan> BreakLeaseImpl(BlobAttributes attributes, TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options)
        {
            int? breakSeconds = null;
            if (breakPeriod.HasValue)
            {
                CommonUtility.AssertInBounds("breakPeriod", breakPeriod.Value, TimeSpan.Zero, TimeSpan.MaxValue);
                breakSeconds = (int)breakPeriod.Value.TotalSeconds;
            }

            RESTCommand<TimeSpan> putCmd = new RESTCommand<TimeSpan>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.Lease(uri, serverTimeout, LeaseAction.Break, null /* proposedLeaseId */, null /* leaseDuration */, breakSeconds, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, TimeSpan.Zero, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);

                int? remainingLeaseTime = BlobHttpResponseParsers.GetRemainingLeaseTime(resp);
                if (!remainingLeaseTime.HasValue)
                {
                    // Unexpected result from service.
                    throw new StorageException(cmd.CurrentResult, SR.LeaseTimeNotReceived, null /* inner */);
                }

                return TimeSpan.FromSeconds(remainingLeaseTime.Value);
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation of the StartCopy method. Result is a BlobAttributes object derived from the response headers.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="source">The URI of the source blob.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source blob. If null, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="setResult">A delegate for setting the BlobAttributes result.</param>
        /// <returns>A <see cref="RESTCommand"/> that starts to copy the blob.</returns>
        private RESTCommand<string> StartCopyImpl(BlobAttributes attributes, Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options)
        {
            if (sourceAccessCondition != null && !string.IsNullOrEmpty(sourceAccessCondition.LeaseId))
            {
                throw new ArgumentException(SR.LeaseConditionOnSource, "sourceAccessCondition");
            }

            RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.CopyFrom(uri, serverTimeout, source, sourceAccessCondition, destAccessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                BlobHttpRequestMessageFactory.AddMetadata(msg, attributes.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, null /* retVal */, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(attributes, resp, false);
                CopyState state = BlobHttpResponseParsers.GetCopyAttributes(resp);
                attributes.CopyState = state;
                return state.CopyId;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation of the AbortCopy method. No result is produced.
        /// </summary>
        /// <param name="attributes">The blob's attributes.</param>
        /// <param name="copyId">The copy ID of the copy operation to abort.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the operation. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="TaskSequence"/> that copies the blob.</returns>
        private RESTCommand<NullType> AbortCopyImpl(BlobAttributes attributes, string copyId, AccessCondition accessCondition, BlobRequestOptions options)
        {
            CommonUtility.AssertNotNull("copyId", copyId);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.AbortCopy(uri, serverTimeout, copyId, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Snapshot method.
        /// </summary>
        /// <param name="blobType">The type of the blob.</param>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot, or null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the snapshot.</returns>
        /// <remarks>If the <c>metadata</c> parameter is <c>null</c> then no metadata is associated with the request.</remarks>
        private RESTCommand<CloudBlob> SnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<CloudBlob> putCmd = new RESTCommand<CloudBlob>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.Snapshot(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                if (metadata != null)
                {
                    BlobHttpRequestMessageFactory.AddMetadata(msg, metadata);
                }

                return msg;
            };

            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, null /* retVal */, cmd, ex);
                DateTimeOffset snapshotTime = NavigationHelper.ParseSnapshotTime(BlobHttpResponseParsers.GetSnapshotTime(resp));
                CloudBlob snapshot = new CloudBlob(this.Name, snapshotTime, this.Container);
                snapshot.attributes.Metadata = new Dictionary<string, string>(metadata ?? this.Metadata);
                snapshot.attributes.Properties = new BlobProperties(this.Properties);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(snapshot.attributes, resp, false);
                return snapshot;
            };

            return putCmd;
        }

        /// <summary>
        /// Updates this blob with the given attributes at the end of a fetch attributes operation.
        /// </summary>
        /// <param name="attributes">The blob attributes to update.</param>
        /// <param name="response">The response to parse.</param>
        /// <param name="ignoreMD5">If set to <c>true</c>, do not parse MD5 header.</param>
        internal static void UpdateAfterFetchAttributes(BlobAttributes attributes, HttpResponseMessage response, bool ignoreMD5)
        {
            BlobProperties properties = BlobHttpResponseParsers.GetProperties(response);

            // If BlobType is specified and the value returned from cloud is different, 
            // then it's a client error and we need to throw.
            if (attributes.Properties.BlobType != BlobType.Unspecified && attributes.Properties.BlobType != properties.BlobType)
            {
                throw new InvalidOperationException(SR.BlobTypeMismatch);
            }

            if (ignoreMD5)
            {
                properties.ContentMD5 = attributes.Properties.ContentMD5;
            }

            attributes.Properties = properties;
            attributes.Metadata = BlobHttpResponseParsers.GetMetadata(response);
            attributes.CopyState = BlobHttpResponseParsers.GetCopyAttributes(response);
        }

        /// <summary>
        /// Retrieve ETag, LMT, Length and Sequence-Number from response.
        /// </summary>
        /// <param name="attributes">The blob attributes to update.</param>
        /// <param name="response">The response to parse.</param>
        /// <param name="updateLength">If set to <c>true</c>, update the blob length.</param>
        internal static void UpdateETagLMTLengthAndSequenceNumber(BlobAttributes attributes, HttpResponseMessage response, bool updateLength)
        {
            BlobProperties parsedProperties = BlobHttpResponseParsers.GetProperties(response);
            attributes.Properties.ETag = parsedProperties.ETag ?? attributes.Properties.ETag;
            attributes.Properties.LastModified = parsedProperties.LastModified ?? attributes.Properties.LastModified;
            attributes.Properties.PageBlobSequenceNumber = parsedProperties.PageBlobSequenceNumber ?? attributes.Properties.PageBlobSequenceNumber;
            attributes.Properties.AppendBlobCommittedBlockCount = parsedProperties.AppendBlobCommittedBlockCount ?? attributes.Properties.AppendBlobCommittedBlockCount;

            if (updateLength)
            {
                attributes.Properties.Length = parsedProperties.Length;
            }
        }

        /// <summary>
        /// Converts the source blob of a copy operation to an appropriate access URI, taking Shared Access Signature credentials into account.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <returns>A URI addressing the source blob, using SAS if appropriate.</returns>
        internal static Uri SourceBlobToUri(CloudBlob source)
        {
            CommonUtility.AssertNotNull("source", source);
            return source.ServiceClient.Credentials.TransformUri(source.SnapshotQualifiedUri);
        }

        /// <summary>
        /// Parses the server request encrypted response header.
        /// </summary>
        /// <param name="response">Response to be parsed.</param>
        /// <returns><c>true</c> if write content was encrypted by service or <c>false</c> if not.</returns>
        internal static bool ParseServerRequestEncrypted(HttpResponseMessage response)
        {
            string requestEncrypted = response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ServerRequestEncrypted);
            return string.Equals(requestEncrypted, Constants.HeaderConstants.TrueHeader, StringComparison.OrdinalIgnoreCase);
        }
    }
}
