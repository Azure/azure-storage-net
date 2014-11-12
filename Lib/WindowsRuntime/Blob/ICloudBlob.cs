// -----------------------------------------------------------------------------------------
// <copyright file="ICloudBlob.cs" company="Microsoft">
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
// -----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using System;
#if ASPNET_K
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
    using Windows.Storage;
    using Windows.Storage.Streams;
#endif
    /// <summary>
    /// An interface required for Windows Azure blob types. The <see cref="CloudBlockBlob"/> and <see cref="CloudPageBlob"/> classes implement the <see cref="ICloudBlob"/> interface.
    /// </summary>
    public partial interface ICloudBlob : IListBlobItem
#if !ASPNET_K
        , IRandomAccessStreamReference
#endif
    {
        /// <summary>
        /// Opens a stream for reading from the blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A stream to be used for reading from the blob.</returns>

#if ASPNET_K
        Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Uploads a stream to the Windows Azure Blob Service. 
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task UploadFromStreamAsync(Stream source);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction UploadFromStreamAsync(IInputStream source);
#endif

        /// <summary>
        /// Uploads a stream to a blob. 
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction UploadFromStreamAsync(IInputStream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Uploads a stream to a blob. 
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task UploadFromStreamAsync(Stream source, long length);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction UploadFromStreamAsync(IInputStream source, long length);
#endif

        /// <summary>
        /// Uploads a stream to a blob. 
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction UploadFromStreamAsync(IInputStream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// Uploads a file to the Windows Azure Blob Service. 
        /// </summary>
#if ASPNET_K
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task UploadFromFileAsync(string path, FileMode mode);
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction UploadFromFileAsync(StorageFile source);
#endif

        /// <summary>
        /// Uploads a file to a blob. 
        /// </summary>
#if ASPNET_K
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
#else
        /// <param name="source">The file providing the blob content.</param>
#endif
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task UploadFromFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction UploadFromFileAsync(StorageFile source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Uploads the contents of a byte array to a blob.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task UploadFromByteArrayAsync(byte[] buffer, int index, int count);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction UploadFromByteArrayAsync([ReadOnlyArray] byte[] buffer, int index, int count);
#endif

        /// <summary>
        /// Uploads the contents of a byte array to a blob.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction UploadFromByteArrayAsync([ReadOnlyArray] byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Downloads the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task DownloadToStreamAsync(Stream target);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction DownloadToStreamAsync(IOutputStream target);
#endif

        /// <summary>
        /// Downloads the contents of a blob to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction DownloadToStreamAsync(IOutputStream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Downloads the contents of a blob to a file.
        /// </summary>
#if ASPNET_K
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task DownloadToFileAsync(string path, FileMode mode);
#else
        /// <param name="target">The target file.</param>
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction DownloadToFileAsync(StorageFile target);
#endif

        /// <summary>
        /// Downloads the contents of a blob to a file.
        /// </summary>
#if ASPNET_K
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
#else
        /// <param name="target">The target file.</param>
#endif
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction DownloadToFileAsync(StorageFile target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Downloads a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
#if ASPNET_K
        Task<int> DownloadToByteArrayAsync(byte[] target, int index);
#else
        IAsyncOperation<int> DownloadToByteArrayAsync([WriteOnlyArray] byte[] target, int index);
#endif

        /// <summary>
        /// Downloads a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
#if ASPNET_K
        Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        IAsyncOperation<int> DownloadToByteArrayAsync([WriteOnlyArray] byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Downloads a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction DownloadRangeToStreamAsync(IOutputStream target, long? offset, long? length);
#endif

        /// <summary>
        /// Downloads a range of bytes from a blob to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction DownloadRangeToStreamAsync(IOutputStream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Downloads a range of bytes from a blob to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="blobOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
#if ASPNET_K
        Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length);
#else
        IAsyncOperation<int> DownloadRangeToByteArrayAsync([WriteOnlyArray] byte[] target, int index, long? blobOffset, long? length);
#endif

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
#if ASPNET_K
        Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        IAsyncOperation<int> DownloadRangeToByteArrayAsync([WriteOnlyArray] byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Checks whether the blob exists.
        /// </summary>
        /// <returns><c>true</c> if the blob exists.</returns>
#if ASPNET_K
        Task<bool> ExistsAsync();
#else
        IAsyncOperation<bool> ExistsAsync();
#endif

        /// <summary>
        /// Checks whether the blob exists.
        /// </summary>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the blob exists.</returns>
#if ASPNET_K
        Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext);
#else
        IAsyncOperation<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Populates a blob's properties and metadata.
        /// </summary>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task FetchAttributesAsync();
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction FetchAttributesAsync();
#endif

        /// <summary>
        /// Populates a blob's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Updates the blob's metadata.
        /// </summary>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task SetMetadataAsync();
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction SetMetadataAsync();
#endif

        /// <summary>
        /// Updates the blob's metadata.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Updates the blob's properties.
        /// </summary>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task SetPropertiesAsync();
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction SetPropertiesAsync();
#endif

        /// <summary>
        /// Updates the blob's properties.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Deletes the blob.
        /// </summary>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task DeleteAsync();
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction DeleteAsync();
#endif

        /// <summary>
        /// Deletes the blob.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Deletes the blob if it already exists.
        /// </summary>
        /// <returns><c>true</c> if the blob did not already exist and was created; otherwise <c>false</c>.</returns>
#if ASPNET_K
        Task<bool> DeleteIfExistsAsync();
#else
        IAsyncOperation<bool> DeleteIfExistsAsync();
#endif

        /// <summary>
        /// Deletes the blob if it already exists.
        /// </summary>
        /// <param name="deleteSnapshotsOption">Whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the container.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the blob did not already exist and was created; otherwise <c>false</c>.</returns>
#if ASPNET_K
        Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        IAsyncOperation<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Acquires a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease.</param>
        /// <returns>The ID of the acquired lease.</returns>
#if ASPNET_K
        Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId);
#else
        IAsyncOperation<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId);
#endif

        /// <summary>
        /// Acquires a lease on this blob.
        /// </summary>
        /// <param name="leaseTime">A <see cref="TimeSpan"/> representing the span of time for which to acquire the lease,
        /// which will be rounded down to seconds.</param>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The ID of the acquired lease.</returns>
#if ASPNET_K
        Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        IAsyncOperation<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Renews a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task RenewLeaseAsync(AccessCondition accessCondition);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction RenewLeaseAsync(AccessCondition accessCondition);
#endif

        /// <summary>
        /// Renews a lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Changes the lease ID on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <returns>The new lease ID.</returns>
#if ASPNET_K
        Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition);
#else
        IAsyncOperation<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition);
#endif

        /// <summary>
        /// Changes the lease ID on this blob.
        /// </summary>
        /// <param name="proposedLeaseId">A string representing the proposed lease ID for the new lease.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The new lease ID.</returns>
#if ASPNET_K
        Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        IAsyncOperation<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Releases the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task ReleaseLeaseAsync(AccessCondition accessCondition);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction ReleaseLeaseAsync(AccessCondition accessCondition);
#endif

        /// <summary>
        /// Releases the lease on this blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob, including a required lease ID.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Breaks the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the amount of time before the lease ends, to the second.</returns>
#if ASPNET_K
        Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod);
#else
        IAsyncOperation<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod);
#endif

        /// <summary>
        /// Breaks the current lease on this blob.
        /// </summary>
        /// <param name="breakPeriod">A <see cref="TimeSpan"/> representing the amount of time to allow the lease to remain,
        /// which will be rounded down to seconds.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the amount of time before the lease ends, to the second.</returns>
#if ASPNET_K
        Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        IAsyncOperation<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif

        /// <summary>
        /// Aborts an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task AbortCopyAsync(string copyId);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction AbortCopyAsync(string copyId);
#endif

        /// <summary>
        /// Aborts an ongoing blob copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        Task AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        IAsyncAction AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext);
#endif
    }
}
