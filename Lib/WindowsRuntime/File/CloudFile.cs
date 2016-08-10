// -----------------------------------------------------------------------------------------
// <copyright file="CloudFile.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
#if NETCORE
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
    using Windows.Foundation.Metadata;
    using Windows.Storage;
    using Windows.Storage.Streams;
#endif

    public partial class CloudFile : IListFileItem
    {
        /// <summary>
        /// Opens a stream for reading from the file.
        /// </summary>
        /// <returns>A stream to be used for reading from the file.</returns>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync()
        {
            return this.OpenReadAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Opens a stream for reading from the file.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A stream to be used for reading from the file.</returns>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.OpenReadAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Opens a stream for reading from the file.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A stream to be used for reading from the file.</returns>
        [DoesServiceRequest]
        public virtual Task<Stream> OpenReadAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            operationContext = operationContext ?? new OperationContext();
            return Task.Run<Stream>(async () =>
            {
                await this.FetchAttributesAsync(accessCondition, options, operationContext);
                AccessCondition streamAccessCondition = AccessCondition.CloneConditionWithETag(accessCondition, this.Properties.ETag);
                FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient, false);
                return new FileReadStream(this, streamAccessCondition, modifiedOptions, operationContext);
            }, cancellationToken);
        }

        /// <summary>
        /// Opens a stream for writing to the file. If the file already exists, then existing data in the file may be overwritten.
        /// </summary>
        /// <param name="size">The size of the write operation, in bytes.</param>
        /// <returns>A stream to be used for writing to the file.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudFileStream> OpenWriteAsync(long? size)
        {
            return this.OpenWriteAsync(size, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Opens a stream for writing to the file. If the file already exists, then existing data in the file may be overwritten.
        /// </summary>
        /// <param name="size">The size of the write operation, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A stream to be used for writing to the file.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudFileStream> OpenWriteAsync(long? size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.OpenWriteAsync(size, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Opens a stream for writing to the file.
        /// </summary>
        /// <param name="size">The size of the write operation, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A stream to be used for writing to the file.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudFileStream> OpenWriteAsync(long? size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient, false);
            operationContext = operationContext ?? new OperationContext();
            bool createNew = size.HasValue;

            if (!createNew && modifiedOptions.StoreFileContentMD5.Value)
            {
                throw new ArgumentException(SR.MD5NotPossible);
            }
            return Task.Run(async () =>
            {
                if (createNew)
                {
                    await this.CreateAsync(size.Value, accessCondition, options, operationContext, cancellationToken);
                }
                else
                {
                    await this.FetchAttributesAsync(accessCondition, options, operationContext, cancellationToken);
                    size = this.Properties.Length;
                }

                if (accessCondition != null)
                {
                    accessCondition = AccessCondition.GenerateLeaseCondition(accessCondition.LeaseId);
                }

                CloudFileStream stream = new FileWriteStream(this, size.Value, createNew, accessCondition, modifiedOptions, operationContext);
                return stream;
            }, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length*/, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length)
        {
            return this.UploadFromStreamAsyncHelper(source, length, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to a file. 
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a file. 
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal Task UploadFromStreamAsyncHelper(Stream source, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a stream to a file. 
        /// </summary>
        /// <param name="source">The stream providing the file content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal Task UploadFromStreamAsyncHelper(Stream source, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("source", source);

            Stream sourceAsStream = source;
            if (!sourceAsStream.CanSeek)
            {
                throw new InvalidOperationException();
            }

            if (length.HasValue)
            {
                CommonUtility.AssertInBounds("length", length.Value, 1, sourceAsStream.Length - sourceAsStream.Position);
            }
            else
            {
                length = sourceAsStream.Length - sourceAsStream.Position;
            }

            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);

            return Task.Run(async () =>
            {
                using (CloudFileStream fileStream = await this.OpenWriteAsync(length, accessCondition, options, operationContext, cancellationToken))
                {
                    // We should always call AsStreamForWrite with bufferSize=0 to prevent buffering. Our
                    // stream copier only writes 64K buffers at a time anyway, so no buffering is needed.
                    await sourceAsStream.WriteToAsync(fileStream, length, null /* maxLength */, false, tempExecutionState, null /* streamCopyState */, cancellationToken);
                    await fileStream.CommitAsync();
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Uploads a file to the Azure File Service. If the file already exists on the service, it will be overwritten.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the path to the target file.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path)
        {
            return this.UploadFromFileAsync(path, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#else
        /// <param name="source">The file providing the file content.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(StorageFile source)
        {
            return this.UploadFromFileAsync(source, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#endif

        /// <summary>
        /// Uploads a file to the Azure File Service. If the file already exists on the service, it will be overwritten.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromFileAsync(path, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <param name="source">The file providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(StorageFile source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromFileAsync(source, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a file to the Azure File Service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="source">The file providing the file content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(StorageFile source, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("source", source);

            return Task.Run(async () =>
            {
                using (IRandomAccessStreamWithContentType stream = await source.OpenReadAsync().AsTask(cancellationToken))
                {
                    await this.UploadFromStreamAsync(stream.AsStream(), accessCondition, options, operationContext, cancellationToken);
                }
            });
        }
#endif

#if NETCORE
        /// <summary>
        /// Uploads a file to the Azure File Service. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the path to the target file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);

            return Task.Run(async () =>
            {
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    await this.UploadFromStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
                }
            }, cancellationToken);
        }
#endif

        /// <summary>
        /// Uploads the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads the contents of a byte array to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads the contents of a byte array to a file.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the file.</param>
        /// <param name="count">The number of bytes to be written to the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.UploadFromStreamAsync(stream, stream.Length, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload, encoded as a UTF-8 string.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content)
        {
            return this.UploadTextAsync(content, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload, encoded as a UTF-8 string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.UploadTextAsync(content, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a string of text to a file. If the file already exists on the service, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload, encoded as a UTF-8 string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = Encoding.UTF8.GetBytes(content);
            return this.UploadFromByteArrayAsync(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Downloads the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target)
        {
            return this.DownloadToStreamAsync(target, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToStreamAsync(target, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToStreamAsync(target, null /* offset */, null /* length */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Downloads the contents of a file to a file.
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
        /// Downloads the contents of a file to a file.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToFileAsync(path, mode, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <param name="target">The target file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(StorageFile target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToFileAsync(target, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the contents of a file to a file.
        /// </summary>
        /// <param name="target">The target file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(StorageFile target, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("target", target);

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
        /// Downloads the contents of a file to a file.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
            });
        }
#endif

        /// <summary>
        /// Downloads the contents of a file to a byte array.
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
        /// Downloads the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadToByteArrayAsync(target, index, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, null /* fileOffset */, null /* length */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Downloads the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the file, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads the file's contents as a string.
        /// </summary>
        /// <returns>The contents of the file, as a string.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync()
        {
            return this.DownloadTextAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads the file's contents as a string.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The contents of the file, as a string.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadTextAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the file's contents as a string.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The contents of the file, as a string.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                using (SyncMemoryStream stream = new SyncMemoryStream())
                {
                    await this.DownloadToStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
                    byte[] streamAsBytes = stream.ToArray();
                    return Encoding.UTF8.GetString(streamAsBytes, 0, streamAsBytes.Length);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Downloads the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the file, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadRangeToStreamAsync(target, offset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the contents of a file to a stream.
        /// </summary>
        /// <param name="target">The target stream.</param>
        /// <param name="offset">The offset at which to begin downloading the file, in bytes.</param>
        /// <param name="length">The length of the data to download from the file, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("target", target);

            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);

            // We should always call AsStreamForWrite with bufferSize=0 to prevent buffering. Our
            // stream copier only writes 64K buffers at a time anyway, so no buffering is needed.
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.GetFileImpl(target, offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Downloads the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, fileOffset, length, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadRangeToByteArrayAsync(target, index, fileOffset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the contents of a file to a byte array.
        /// </summary>
        /// <param name="target">The target byte array.</param>
        /// <param name="index">The starting offset in the byte array.</param>
        /// <param name="fileOffset">The starting offset of the data range, in bytes.</param>
        /// <param name="length">The length of the data range, in bytes.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        [DoesServiceRequest]
        public virtual Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? fileOffset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                using (SyncMemoryStream stream = new SyncMemoryStream(target, index))
                {
                    await this.DownloadRangeToStreamAsync(stream, fileOffset, length, accessCondition, options, operationContext, cancellationToken);
                    return (int)stream.Position;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Creates a file. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size)
        {
            return this.CreateAsync(size, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Creates a file. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.CreateAsync(size, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates a file. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task CreateAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.CreateImpl(size, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Checks existence of the file.
        /// </summary>
        /// <returns><c>true</c> if the file exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync()
        {
            return this.ExistsAsync(null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Checks existence of the file.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the file exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext)
        {
            return this.ExistsAsync(options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Checks existence of the file.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the file exists.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> ExistsAsync(FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.ExistsImpl(modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Populates a file's properties and metadata.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync()
        {
            return this.FetchAttributesAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Populates a file's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.FetchAttributesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Populates a file's properties and metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task FetchAttributesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.FetchAttributesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task DeleteAsync()
        {
            return this.DeleteAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task DeleteAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.DeleteFileImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Deletes the file if it already exists.
        /// </summary>
        /// <returns><c>true</c> if the file already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync()
        {
            return this.DeleteIfExistsAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Deletes the file if it already exists.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns><c>true</c> if the file already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.DeleteIfExistsAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the file if it already exists.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns><c>true</c> if the file already existed and was deleted; otherwise, <c>false</c>.</returns>
        [DoesServiceRequest]
        public virtual Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();

            return Task.Run(async () =>
            {
                bool exists = await this.ExistsAsync(modifiedOptions, operationContext, cancellationToken);
                if (!exists)
                {
                    return false;
                }

                try
                {
                    await this.DeleteAsync(accessCondition, modifiedOptions, operationContext, cancellationToken);
                    return true;
                }
                catch (Exception)
                {
                    if (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.NotFound)
                    {
                        StorageExtendedErrorInformation extendedInfo = operationContext.LastResult.ExtendedErrorInformation;
                        if ((extendedInfo == null) ||
                            (extendedInfo.ErrorCode == StorageErrorCodeStrings.ResourceNotFound))
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
            }, cancellationToken);
        }

        /// Gets a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <returns>An enumerable collection of ranges.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<FileRange>> ListRangesAsync()
        {
            return this.ListRangesAsync(null /* offset */, null /* length */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Gets a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="offset">The starting offset of the data range over which to list file ranges, in bytes.</param>
        /// <param name="length">The length of the data range over which to list file ranges, in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of ranges.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<FileRange>> ListRangesAsync(long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ListRangesAsync(offset, length, accessCondition, options, operationContext, CancellationToken.None);
        }


        /// <summary>
        /// Gets a collection of valid ranges and their starting and ending bytes.
        /// </summary>
        /// <param name="offset">The starting offset of the data range over which to list file ranges, in bytes.</param>
        /// <param name="length">The length of the data range over which to list file ranges, in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An enumerable collection of ranges.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<FileRange>> ListRangesAsync(long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.ListRangesImpl(offset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Updates the file's properties.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync()
        {
            return this.SetPropertiesAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Updates the file's properties.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetPropertiesAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Updates the file's properties.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task SetPropertiesAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.SetPropertiesImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Resizes a file.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        [DoesServiceRequest]
        public virtual Task ResizeAsync(long size)
        {
            return this.ResizeAsync(size, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Resizes a file.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task ResizeAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ResizeAsync(size, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Resizes a file.
        /// </summary>
        /// <param name="size">The maximum size of the file, in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task ResizeAsync(long size, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.ResizeImpl(size, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Updates the file's metadata.
        /// </summary>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync()
        {
            return this.SetMetadataAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Updates the file's metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.SetMetadataAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Updates the file's metadata.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task SetMetadataAsync(AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.SetMetadataImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Writes range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the range data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <code>null</code> or an empty string.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5)
        {
            return this.WriteRangeAsync(rangeData, startOffset, contentMD5, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Writes range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the range data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <code>null</code> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.WriteRangeAsync(rangeData, startOffset, contentMD5, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Writes range to a file.
        /// </summary>
        /// <param name="rangeData">A stream providing the range data.</param>
        /// <param name="startOffset">The offset at which to begin writing, in bytes.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <code>null</code> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        public virtual Task WriteRangeAsync(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("rangeData", rangeData);

            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            bool requiresContentMD5 = (contentMD5 == null) && modifiedOptions.UseTransactionalMD5.Value;
            operationContext = operationContext ?? new OperationContext();
            ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);

            return Task.Run(async () =>
            {
                DateTime streamCopyStartTime = DateTime.Now;

                Stream rangeDataAsStream = rangeData;
                Stream seekableStream = rangeDataAsStream;
                bool seekableStreamCreated = false;

                try
                {
                    if (!rangeDataAsStream.CanSeek || requiresContentMD5)
                    {
                        Stream writeToStream;
                        if (rangeDataAsStream.CanSeek)
                        {
                            writeToStream = Stream.Null;
                        }
                        else
                        {
                            seekableStream = new MultiBufferMemoryStream(this.ServiceClient.BufferManager);
                            seekableStreamCreated = true;
                            writeToStream = seekableStream;
                        }

                        StreamDescriptor streamCopyState = new StreamDescriptor();
                        long startPosition = seekableStream.Position;
                        await rangeDataAsStream.WriteToAsync(writeToStream, null /* copyLength */, Constants.MaxBlockSize, requiresContentMD5, tempExecutionState, streamCopyState, cancellationToken);
                        seekableStream.Position = startPosition;

                        if (requiresContentMD5)
                        {
                            contentMD5 = streamCopyState.Md5;
                        }

                        if (modifiedOptions.MaximumExecutionTime.HasValue)
                        {
                            modifiedOptions.MaximumExecutionTime -= DateTime.Now.Subtract(streamCopyStartTime);
                        }
                    }

                    await Executor.ExecuteAsyncNullReturn(
                        this.PutRangeImpl(seekableStream, startOffset, contentMD5, accessCondition, modifiedOptions),
                        modifiedOptions.RetryPolicy,
                        operationContext,
                        cancellationToken);
                }
                finally
                {
                    if (seekableStreamCreated)
                    {
                        seekableStream.Dispose();
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Clears ranges from a file.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing file ranges, in bytes.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes.</param>
        [DoesServiceRequest]
        public virtual Task ClearRangeAsync(long startOffset, long length)
        {
            return this.ClearRangeAsync(startOffset, length, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Clears ranges from a file.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing file ranges, in bytes.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task ClearRangeAsync(long startOffset, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.ClearRangeAsync(startOffset, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Clears ranges from a file.
        /// </summary>
        /// <param name="startOffset">The offset at which to begin clearing file ranges, in bytes.</param>
        /// <param name="length">The length of the data range to be cleared, in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        [DoesServiceRequest]
        public virtual Task ClearRangeAsync(long startOffset, long length, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.ClearRangeImpl(startOffset, length, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Begins an operation to start copying an existing blob or Azure file's contents, properties, and metadata to a new Azure file.
        /// </summary>
        /// <param name="source">The URI of a source object.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the file's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source)
        {
            return this.StartCopyAsync(source, null /* sourceAccessCondition */, null /* destAccessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Begins an operation to start copying an existing blob's contents, properties, and metadata to a new Azure file.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the file's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudBlob source)
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source));
        }

        /// <summary>
        /// Begins an operation to start copying an existing Azure file's contents, properties, and metadata to a new Azure file.
        /// </summary>
        /// <param name="source">The source file.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the file's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudFile source)
        {
            return this.StartCopyAsync(CloudFile.SourceFileToUri(source));
        }

        /// <summary>
        /// Begins an operation to start copying a blob or file's contents, properties, and metadata to a new Azure file.
        /// </summary>
        /// <param name="source">The URI of a source object.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source object. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the file's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Begins an operation to start copying a blob or file's contents, properties, and metadata to a new Azure file.
        /// </summary>
        /// <param name="source">The URI of a source object.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source object. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the file's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync<string>(
                this.StartCopyImpl(source, sourceAccessCondition, destAccessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Begins an operation to start copying a blob's contents, properties, and metadata to a new Azure file.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the file's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext);
        }

        /// <summary>
        /// Aborts an ongoing copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId)
        {
            return this.AbortCopyAsync(copyId, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Aborts an ongoing copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext)
        {
            return this.AbortCopyAsync(copyId, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Aborts an ongoing copy operation.
        /// </summary>
        /// <param name="copyId">A string identifying the copy operation.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task AbortCopyAsync(string copyId, AccessCondition accessCondition, FileRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            FileRequestOptions modifiedOptions = FileRequestOptions.ApplyDefaults(options, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
               this.AbortCopyImpl(copyId, accessCondition, modifiedOptions),
               modifiedOptions.RetryPolicy,
               operationContext,
               cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Implements getting the file.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="SynchronousTask"/> that gets the stream.</returns>
        private RESTCommand<NullType> GetFileImpl(Stream destStream, long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options)
        {
            string lockedETag = null;
            AccessCondition lockedAccessCondition = null;

            bool isRangeGet = offset.HasValue;
            bool arePropertiesPopulated = false;
            string storedMD5 = null;

            long startingOffset = offset.HasValue ? offset.Value : 0;
            long? startingLength = length;
            long? validateLength = null;

            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.DestinationStream = destStream;
            getCmd.CalculateMd5ForResponseStream = !options.DisableContentMD5Validation.Value;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.Get(uri, serverTimeout, offset, length, options.UseTransactionalMD5.Value, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
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

                getCmd.BuildRequest = (command, uri, builder, cnt, serverTimeout, context) => FileHttpRequestMessageFactory.Get(uri, serverTimeout, offset, length, options.UseTransactionalMD5.Value && !arePropertiesPopulated, accessCondition, cnt, context, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            };

            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(offset.HasValue ? HttpStatusCode.PartialContent : HttpStatusCode.OK, resp, NullType.Value, cmd, ex);

                if (!arePropertiesPopulated)
                {
                    this.UpdateAfterFetchAttributes(resp, isRangeGet);

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

                    // If the download fails and Get File needs to resume the download, going to the
                    // same storage location is important to prevent a possible ETag mismatch.
                    getCmd.CommandLocationMode = cmd.CurrentResult.TargetLocation == StorageLocation.Primary ? CommandLocationMode.PrimaryOnly : CommandLocationMode.SecondaryOnly;
                    lockedETag = attributes.Properties.ETag;
                    validateLength = resp.Content.Headers.ContentLength;

                    arePropertiesPopulated = true;
                }
                else
                {
                    if (!resp.Headers.ETag.ToString().Equals(lockedETag, StringComparison.Ordinal))
                    {
                        RequestResult reqResult = new RequestResult();
                        reqResult.HttpStatusMessage = null;
                        reqResult.HttpStatusCode = (int)HttpStatusCode.PreconditionFailed;
                        reqResult.ExtendedErrorInformation = null;
                        throw new StorageException(reqResult, SR.PreconditionFailed, null /* inner */);
                    }
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
        /// Implements the Create method.
        /// </summary>
        /// <param name="sizeInBytes">The size in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="TaskSequence"/> that creates the file.</returns>
        private RESTCommand<NullType> CreateImpl(long sizeInBytes, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.Create(uri, serverTimeout, this.Properties, sizeInBytes, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                this.UpdateETagLMTAndLength(resp, false);
                this.Properties.Length = sizeInBytes;
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implements the FetchAttributes method. The attributes are updated immediately.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that fetches the attributes.</returns>
        private RESTCommand<NullType> FetchAttributesImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> getCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.GetProperties(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.UpdateAfterFetchAttributes(resp, false);
                return NullType.Value;
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the Exists method. The attributes are updated immediately.
        /// </summary>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that checks existence.</returns>
        private RESTCommand<bool> ExistsImpl(FileRequestOptions options)
        {
            RESTCommand<bool> getCmd = new RESTCommand<bool>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.GetProperties(uri, serverTimeout, null /* accessCondition */, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, true, cmd, ex);
                this.UpdateAfterFetchAttributes(resp, false);
                return true;
            };

            return getCmd;
        }

        /// <summary>
        /// Implements the DeleteFile method.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that deletes the file.</returns>
        private RESTCommand<NullType> DeleteFileImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> deleteCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(deleteCmd);
            deleteCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.Delete(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            deleteCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex);

            return deleteCmd;
        }

        /// <summary>
        /// Implements the ListRanges method.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> for getting the ranges.</returns>
        private RESTCommand<IEnumerable<FileRange>> ListRangesImpl(long? offset, long? length, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<IEnumerable<FileRange>> getCmd = new RESTCommand<IEnumerable<FileRange>>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.ListRanges(uri, serverTimeout, offset, length, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };

            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                this.UpdateETagLMTAndLength(resp, true);
                return Task.Factory.StartNew(() =>
                {
                    ListRangesResponse listRangesResponse = new ListRangesResponse(cmd.ResponseStream);
                    IEnumerable<FileRange> ranges = listRangesResponse.Ranges.ToList();
                    return ranges;
                });
            };

            return getCmd;
        }

        /// <summary>
        /// Implementation for the SetProperties method.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetPropertiesImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.SetProperties(uri, serverTimeout, this.Properties, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.UpdateETagLMTAndLength(resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the Resize method.
        /// </summary>
        /// <param name="sizeInBytes">The size in bytes.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> ResizeImpl(long sizeInBytes, AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.Resize(uri, serverTimeout, sizeInBytes, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.UpdateETagLMTAndLength(resp, false);
                this.Properties.Length = sizeInBytes;
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the SetMetadata method.
        /// </summary>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that sets the metadata.</returns>
        private RESTCommand<NullType> SetMetadataImpl(AccessCondition accessCondition, FileRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.SetMetadata(uri, serverTimeout, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, NullType.Value, cmd, ex);
                this.UpdateETagLMTAndLength(resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation method for the WriteRange methods.
        /// </summary>
        /// <param name="rangeData">The range data.</param>
        /// <param name="startOffset">The start offset.</param> 
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="FileProperties.ContentMD5"/> property
        /// on the file. May be <code>null</code> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the file. If <code>null</code>, no condition is used.</param>
        /// <param name="options">A <see cref="FileRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that writes the range.</returns>
        private RESTCommand<NullType> PutRangeImpl(Stream rangeData, long startOffset, string contentMD5, AccessCondition accessCondition, FileRequestOptions options)
        {
            long offset = rangeData.Position;
            long length = rangeData.Length - offset;

            FileRange fileRange = new FileRange(startOffset, startOffset + length - 1);
            FileRangeWrite fileRangeWrite = FileRangeWrite.Update;

            if ((1 + fileRange.EndOffset - fileRange.StartOffset) == 0)
            {
                CommonUtility.ArgumentOutOfRange("rangeData", rangeData);
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(rangeData, offset, length, contentMD5, cmd, ctx);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.PutRange(uri, serverTimeout, fileRange, fileRangeWrite, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                this.UpdateETagLMTAndLength(resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation method for the ClearRange methods.
        /// </summary>
        /// <param name="startOffset">The start offset.</param>
        /// <param name="length">Length of the data range to be cleared.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that writes the ranges.</returns>
        private RESTCommand<NullType> ClearRangeImpl(long startOffset, long length, AccessCondition accessCondition, FileRequestOptions options)
        {
            CommonUtility.AssertNotNull("options", options);

            if (startOffset < 0)
            {
                CommonUtility.ArgumentOutOfRange("startOffset", startOffset);
            }

            if (length <= 0)
            {
                CommonUtility.ArgumentOutOfRange("length", length);
            }

            FileRange range = new FileRange(startOffset, startOffset + length - 1);
            FileRangeWrite fileWrite = FileRangeWrite.Clear;

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.PutRange(uri, serverTimeout, range, fileWrite, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                this.UpdateETagLMTAndLength(resp, false);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation of the StartCopy method. Result is a CloudFileAttributes object derived from the response headers.
        /// </summary>
        /// <param name="source">The URI of the source object.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source object. If null, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination file. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="setResult">A delegate for setting the CloudFileAttributes result.</param>
        /// <returns>A <see cref="RESTCommand"/> that starts to copy the object.</returns>
        internal RESTCommand<string> StartCopyImpl(Uri source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, FileRequestOptions options)
        {
            if (sourceAccessCondition != null && !string.IsNullOrEmpty(sourceAccessCondition.LeaseId))
            {
                throw new ArgumentException(SR.LeaseConditionOnSource, "sourceAccessCondition");
            }

            RESTCommand<string> putCmd = new RESTCommand<string>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = FileHttpRequestMessageFactory.CopyFrom(uri, serverTimeout, source, sourceAccessCondition, destAccessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                FileHttpRequestMessageFactory.AddMetadata(msg, attributes.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, null /* retVal */, cmd, ex);
                CopyState state = FileHttpResponseParsers.GetCopyAttributes(resp);
                this.attributes.Properties = FileHttpResponseParsers.GetProperties(resp);
                this.attributes.Metadata = FileHttpResponseParsers.GetMetadata(resp);
                this.attributes.CopyState = state;
                return state.CopyId;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation of the AbortCopy method. No result is produced.
        /// </summary>
        /// <param name="copyId">The copy ID of the copy operation to abort.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the operation. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <returns>A <see cref="TaskSequence"/> that copies the object.</returns>
        internal RESTCommand<NullType> AbortCopyImpl(string copyId, AccessCondition accessCondition, FileRequestOptions options)
        {
            CommonUtility.AssertNotNull("copyId", copyId);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => FileHttpRequestMessageFactory.AbortCopy(uri, serverTimeout, copyId, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex);

            return putCmd;
        }

        /// <summary>
        /// Converts the source file of a copy operation to an appropriate access URI, taking Shared Access Signature credentials into account.
        /// </summary>
        /// <param name="source">The source file.</param>
        /// <returns>A URI addressing the source file, using SAS if appropriate.</returns>
        internal static Uri SourceFileToUri(CloudFile source)
        {
            CommonUtility.AssertNotNull("source", source);
            return source.ServiceClient.Credentials.TransformUri(source.Uri);
        }

        /// <summary>
        /// Updates this file with the given attributes a the end of a fetch attributes operation.
        /// </summary>
        /// <param name="attributes">The new attributes.</param>
        private void UpdateAfterFetchAttributes(HttpResponseMessage response, bool ignoreMD5)
        {
            FileProperties properties = FileHttpResponseParsers.GetProperties(response);

            if (ignoreMD5)
            {
                properties.ContentMD5 = this.attributes.Properties.ContentMD5;
            }

            this.attributes.Properties = properties;
            this.attributes.Metadata = FileHttpResponseParsers.GetMetadata(response);
            this.attributes.CopyState = FileHttpResponseParsers.GetCopyAttributes(response);
        }

        /// <summary>
        /// Retrieve ETag, LMT and Length from response.
        /// </summary>
        /// <param name="response">The response to parse.</param>
        /// <param name="updateLength">If set to <c>true</c>, update the file length.</param>
        private void UpdateETagLMTAndLength(HttpResponseMessage response, bool updateLength)
        {
            FileProperties parsedProperties = FileHttpResponseParsers.GetProperties(response);
            this.Properties.ETag = parsedProperties.ETag ?? this.Properties.ETag;
            this.Properties.LastModified = parsedProperties.LastModified ?? this.Properties.LastModified;
            if (updateLength)
            {
                this.Properties.Length = parsedProperties.Length;
            }
        }
    }
}
