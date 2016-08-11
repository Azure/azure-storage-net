// -----------------------------------------------------------------------------------------
// <copyright file="CloudBlockBlob.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Blob.Protocol;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;

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
    using Microsoft.WindowsAzure.Storage.File;    
#if NETCORE
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
    using Windows.Foundation.Metadata;
    using Windows.Storage;
    using Windows.Storage.Streams;
#endif

    public partial class CloudBlockBlob : CloudBlob, ICloudBlob
    {
        /// <summary>
        /// Opens a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <returns>A stream to be used for writing to the blob.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, see <see cref="OpenWriteAsync(AccessCondition, BlobRequestOptions, OperationContext)"/>.</para>        
        /// </remarks>
        public virtual Task<CloudBlobStream> OpenWriteAsync()
        {
            return this.OpenWriteAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Opens a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A stream to be used for writing to the blob.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.</para>
        /// </remarks>
        public virtual Task<CloudBlobStream> OpenWriteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.OpenWriteAsync(accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Opens a stream for writing to the blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A stream to be used for writing to the blob.</returns>
        /// <remarks>
        /// <para>Note that this method always makes a call to the <see cref="CloudBlob.FetchAttributesAsync(AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/> method under the covers.</para>
        /// <para>Set the <see cref="StreamWriteSizeInBytes"/> property before calling this method to specify the block size to write, in bytes, 
        /// ranging from between 16 KB and 4 MB inclusive.</para>
        /// <para>To throw an exception if the blob exists instead of overwriting it, pass in an <see cref="AccessCondition"/>
        /// object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.</para>
        /// </remarks>
        public virtual Task<CloudBlobStream> OpenWriteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            operationContext = operationContext ?? new OperationContext();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, this.BlobType, this.ServiceClient, false);

            if ((accessCondition != null) && accessCondition.IsConditional)
            {
                return Task.Run(async () =>
                {
                    try
                    {
                        await this.FetchAttributesAsync(accessCondition, options, operationContext, cancellationToken);
                    }
                    catch (Exception)
                    {
                        if ((operationContext.LastResult != null) &&
                            (operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.NotFound) &&
                            string.IsNullOrEmpty(accessCondition.IfMatchETag))
                        {
                            // If we got a 404 and the condition was not an If-Match,
                            // we should continue with the operation.
                        }
                        else
                        {
                            throw;
                        }
                    }

                    CloudBlobStream stream = new BlobWriteStream(this, accessCondition, modifiedOptions, operationContext);
                    return stream;
                }, cancellationToken);
            }
            else
            {
                CloudBlobStream stream = new BlobWriteStream(this, accessCondition, modifiedOptions, operationContext);
                return Task.FromResult(stream);
            }
        }

        /// <summary>
        /// Uploads a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length*/, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length)
        {
            return this.UploadFromStreamAsyncHelper(source, length, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal Task UploadFromStreamAsyncHelper(Stream source, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return UploadFromStreamAsyncHelper(source, length, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a stream to a block blob. 
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal Task UploadFromStreamAsyncHelper(Stream source, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("source", source);

            Stream sourceAsStream = source;
            if (length.HasValue)
            {
                CommonUtility.AssertInBounds("length", length.Value, 1);

                if (sourceAsStream.CanSeek && length > sourceAsStream.Length - sourceAsStream.Position)
                {
                    throw new ArgumentOutOfRangeException("length", SR.StreamLengthShortError);
                }
            }

            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);

            return Task.Run(async () =>
            {
                bool lessThanSingleBlobThreshold = sourceAsStream.CanSeek
                                                   && (length ?? sourceAsStream.Length - sourceAsStream.Position)
                                                   <= modifiedOptions.SingleBlobUploadThresholdInBytes;
                if (modifiedOptions.ParallelOperationThreadCount == 1 && lessThanSingleBlobThreshold)
                {
                    string contentMD5 = null;
                    if (modifiedOptions.StoreBlobContentMD5.Value)
                    {
                        StreamDescriptor streamCopyState = new StreamDescriptor();
                        long startPosition = sourceAsStream.Position;
                        await sourceAsStream.WriteToAsync(Stream.Null, length, null /* maxLength */, true, tempExecutionState, streamCopyState, cancellationToken);
                        sourceAsStream.Position = startPosition;
                        contentMD5 = streamCopyState.Md5;
                    }
                    else
                    {
                        if (modifiedOptions.UseTransactionalMD5.Value)
                        {
                            throw new ArgumentException(SR.PutBlobNeedsStoreBlobContentMD5, "options");
                        }
                    }

                    await Executor.ExecuteAsyncNullReturn(
                        this.PutBlobImpl(sourceAsStream, length, contentMD5, accessCondition, modifiedOptions),
                        modifiedOptions.RetryPolicy,
                        operationContext,
                        cancellationToken);
                }
                else
                {
                    using (CloudBlobStream blobStream = await this.OpenWriteAsync(accessCondition, options, operationContext, cancellationToken))
                    {
                        // We should always call AsStreamForWrite with bufferSize=0 to prevent buffering. Our
                        // stream copier only writes 64K buffers at a time anyway, so no buffering is needed.
                        await sourceAsStream.WriteToAsync(blobStream, length, null /* maxLength */, false, tempExecutionState, null /* streamCopyState */, cancellationToken);
                        await blobStream.CommitAsync();
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Uploads a file to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path)
        {
            return this.UploadFromFileAsync(path, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(StorageFile source)
        {
            return this.UploadFromFileAsync(source, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#endif
        /// <summary>
        /// Uploads a file to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
#if NETCORE
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromFileAsync(path, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(StorageFile source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromFileAsync(source, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a file to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">The file providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(StorageFile source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
        /// Uploads a file to a block blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
        /// Uploads the contents of a byte array to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads the contents of a byte array to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads the contents of a byte array to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.UploadFromStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload, encoded as a UTF-8 string.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content)
        {
            return this.UploadTextAsync(content, null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadTextAsync(content, null /* encoding */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a string of text to a blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload, encoded as a UTF-8 string.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.UploadFromByteArrayAsync(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <returns>The contents of the blob, as a string.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync()
        {
            return this.DownloadTextAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The contents of the blob, as a string.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadTextAsync(null /* encoding */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The contents of the blob, as a string.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadTextAsync(encoding, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The contents of the blob, as a string.</returns>
        [DoesServiceRequest]
        public virtual Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                using (SyncMemoryStream stream = new SyncMemoryStream())
                {
                    await this.DownloadToStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
                    byte[] streamAsBytes = stream.ToArray();
                    return (encoding ?? Encoding.UTF8).GetString(streamAsBytes, 0, streamAsBytes.Length);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlockBlob> CreateSnapshotAsync()
        {
            return this.CreateSnapshotAsync(null /* metadata */, null /* accessCondition */, null /* options */, null /* operationContext */);
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
        public virtual Task<CloudBlockBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return CreateSnapshotAsync(metadata, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request, or <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
        public virtual Task<CloudBlockBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.CreateSnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Uploads a single block.
        /// </summary>
        /// <param name="blockId">A base64-encoded block ID that identifies the block.</param>
        /// <param name="blockData">A stream that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="BlobProperties.ContentMD5"/> property
        /// on the blob. May be <c>null</c> or an empty string.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5)
        {
            return this.PutBlockAsync(blockId, blockData, contentMD5, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a single block.
        /// </summary>
        /// <param name="blockId">A base64-encoded block ID that identifies the block.</param>
        /// <param name="blockData">A stream that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="BlobProperties.ContentMD5"/> property
        /// on the blob. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.PutBlockAsync(blockId, blockData, contentMD5, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a single block.
        /// </summary>
        /// <param name="blockId">A base64-encoded block ID that identifies the block.</param>
        /// <param name="blockData">A stream that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="BlobProperties.ContentMD5"/> property
        /// on the blob. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockAsync(string blockId, Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            bool requiresContentMD5 = string.IsNullOrEmpty(contentMD5) && modifiedOptions.UseTransactionalMD5.Value;
            operationContext = operationContext ?? new OperationContext();
            ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);

            return Task.Run(async () =>
            {
                Stream blockDataAsStream = blockData;
                Stream seekableStream = blockDataAsStream;
                bool seekableStreamCreated = false;

                try
                {
                    if (!blockDataAsStream.CanSeek || requiresContentMD5)
                    {
                        Stream writeToStream;
                        if (blockDataAsStream.CanSeek)
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
                        await blockDataAsStream.WriteToAsync(writeToStream, null /* copyLength */, Constants.MaxBlockSize, requiresContentMD5, tempExecutionState, streamCopyState, cancellationToken);
                        seekableStream.Position = startPosition;

                        if (requiresContentMD5)
                        {
                            contentMD5 = streamCopyState.Md5;
                        }
                    }

                    await Executor.ExecuteAsyncNullReturn(
                        this.PutBlockImpl(seekableStream, blockId, contentMD5, accessCondition, modifiedOptions),
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
        /// Uploads a list of blocks to a new or existing blob. 
        /// </summary>
        /// <param name="blockList">An enumerable collection of block IDs, as base64-encoded strings.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockListAsync(IEnumerable<string> blockList)
        {
            return this.PutBlockListAsync(blockList, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a list of blocks to a new or existing blob. 
        /// </summary>
        /// <param name="blockList">An enumerable collection of block IDs, as base64-encoded strings.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockListAsync(IEnumerable<string> blockList, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.PutBlockListAsync(blockList, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a list of blocks to a new or existing blob. 
        /// </summary>
        /// <param name="blockList">An enumerable collection of block IDs, as base64-encoded strings.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public virtual Task PutBlockListAsync(IEnumerable<string> blockList, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            IEnumerable<PutBlockListItem> items = blockList.Select(i => new PutBlockListItem(i, BlockSearchMode.Latest));
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.PutBlockListImpl(items, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Returns an enumerable collection of the committed blocks comprising the blob.
        /// </summary>
        /// <returns>An enumerable collection of objects implementing <see cref="ListBlockItem"/>.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<ListBlockItem>> DownloadBlockListAsync()
        {
            return this.DownloadBlockListAsync(BlockListingFilter.Committed, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns an enumerable collection of the blob's blocks, using the specified block list filter.
        /// </summary>
        /// <param name="blockListingFilter">One of the enumeration values that indicates whether to return 
        /// committed blocks, uncommitted blocks, or both.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects implementing <see cref="ListBlockItem"/>.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<ListBlockItem>> DownloadBlockListAsync(BlockListingFilter blockListingFilter, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadBlockListAsync(blockListingFilter, accessCondition, options, operationContext,  CancellationToken.None);
        }

        /// <summary>
        /// Returns an enumerable collection of the blob's blocks, using the specified block list filter.
        /// </summary>
        /// <param name="blockListingFilter">One of the enumeration values that indicates whether to return 
        /// committed blocks, uncommitted blocks, or both.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>An enumerable collection of objects implementing <see cref="ListBlockItem"/>.</returns>
        [DoesServiceRequest]
        public virtual Task<IEnumerable<ListBlockItem>> DownloadBlockListAsync(BlockListingFilter blockListingFilter, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.BlockBlob, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.GetBlockListImpl(blockListingFilter, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Begins an operation to start copying an existing block blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudBlockBlob source)
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source));
        }

        /// <summary>
        /// Begins an operation to start copying a file's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source file.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        [DoesServiceRequest]
        public virtual Task<string> StartCopyAsync(CloudFile source)
        {
            return this.StartCopyAsync(CloudFile.SourceFileToUri(source));
        }

        /// <summary>
        /// Begins an operation to start copying another block blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source blob.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source blob. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        public virtual Task<string> StartCopyAsync(CloudBlockBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Begins an operation to start copying another block blob's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source blob.</param>
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
        public virtual Task<string> StartCopyAsync(CloudBlockBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Begins an operation to start copying a file's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source file.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
        /// <param name="destAccessCondition">An object that represents the access conditions for the destination blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The copy ID associated with the copy operation.</returns>
        /// <remarks>
        /// This method fetches the blob's ETag, last modified time, and part of the copy state.
        /// The copy ID and copy status fields are fetched, and the rest of the copy state is cleared.
        /// </remarks>
        public virtual Task<string> StartCopyAsync(CloudFile source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Begins an operation to start copying a file's contents, properties, and metadata to a new blob.
        /// </summary>
        /// <param name="source">The source file.</param>
        /// <param name="sourceAccessCondition">An object that represents the access conditions for the source file. If <c>null</c>, no condition is used.</param>
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
        public virtual Task<string> StartCopyAsync(CloudFile source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(CloudFile.SourceFileToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Implementation for the CreateSnapshot method.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot, or null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the snapshot.</returns>
        /// <remarks>If the <c>metadata</c> parameter is <c>null</c> then no metadata is associated with the request.</remarks>
        internal RESTCommand<CloudBlockBlob> CreateSnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<CloudBlockBlob> putCmd = new RESTCommand<CloudBlockBlob>(this.ServiceClient.Credentials, this.attributes.StorageUri);

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
                CloudBlockBlob snapshot = new CloudBlockBlob(this.Name, snapshotTime, this.Container);
                snapshot.attributes.Metadata = new Dictionary<string, string>(metadata ?? this.Metadata);
                snapshot.attributes.Properties = new BlobProperties(this.Properties);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(snapshot.attributes, resp, false);
                return snapshot;
            };

            return putCmd;
        }

        /// <summary>
        /// Uploads the full blob.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="SynchronousTask"/> that gets the stream.</returns>
        private RESTCommand<NullType> PutBlobImpl(Stream stream, long? length, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options)
        {
            long offset = stream.Position;
            length = length ?? stream.Length - offset;
            this.Properties.ContentMD5 = contentMD5;

            CappedLengthReadOnlyStream cappedStream = new CappedLengthReadOnlyStream(stream, length.Value + offset);

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(cappedStream, offset, length, null /* md5 */, cmd, ctx);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.Put(uri, serverTimeout, this.Properties, BlobType.BlockBlob, 0, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                BlobHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                this.Properties.Length = length.Value;
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Uploads the block.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="blockId">The block ID.</param>
        /// <param name="contentMD5">The content MD5.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that uploads the block.</returns>
        internal RESTCommand<NullType> PutBlockImpl(Stream source, string blockId, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options)
        {
            long offset = source.Position;
            long length = source.Length - offset;

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(source, offset, length, contentMD5, cmd, ctx);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.PutBlock(uri, serverTimeout, blockId, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Uploads the block list.
        /// </summary>
        /// <param name="blocks">The blocks to upload.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that uploads the block list.</returns>
        internal RESTCommand<NullType> PutBlockListImpl(IEnumerable<PutBlockListItem> blocks, AccessCondition accessCondition, BlobRequestOptions options)
        {
            MultiBufferMemoryStream memoryStream = new MultiBufferMemoryStream(null /* bufferManager */, (int)(1 * Constants.KB));
            BlobRequest.WriteBlockListBody(blocks, memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            string contentMD5 = null;

            if (options.UseTransactionalMD5.HasValue && options.UseTransactionalMD5.Value)
            {
                contentMD5 = memoryStream.ComputeMD5Hash();
            }

            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(memoryStream, 0, memoryStream.Length, contentMD5, cmd, ctx);
            putCmd.StreamToDispose = memoryStream;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                StorageRequestMessage msg = BlobHttpRequestMessageFactory.PutBlockList(uri, serverTimeout, this.Properties, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
                BlobHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                cmd.CurrentResult.IsRequestServerEncrypted = CloudBlob.ParseServerRequestEncrypted(resp);
                this.Properties.Length = -1;
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Gets the download block list.
        /// </summary>
        /// <param name="typesOfBlocks">The types of blocks.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="TaskSequence"/> that gets the download block list.</returns>
        internal RESTCommand<IEnumerable<ListBlockItem>> GetBlockListImpl(BlockListingFilter typesOfBlocks, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<IEnumerable<ListBlockItem>> getCmd = new RESTCommand<IEnumerable<ListBlockItem>>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(getCmd);
            getCmd.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
            getCmd.RetrieveResponseStream = true;
            getCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.GetBlockList(uri, serverTimeout, this.SnapshotTime, typesOfBlocks, accessCondition, cnt, ctx, this.ServiceClient.GetCanonicalizer(), this.ServiceClient.Credentials);
            getCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null /* retVal */, cmd, ex);
            getCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, true);
                return Task.Factory.StartNew(() =>
                {
                    GetBlockListResponse responseParser = new GetBlockListResponse(cmd.ResponseStream);
                    IEnumerable<ListBlockItem> blocks = new List<ListBlockItem>(responseParser.Blocks);
                    return blocks;
                });
            };

            return getCmd;
        }
    }
}
