// -----------------------------------------------------------------------------------------
// <copyright file="CloudAppendBlob.cs" company="Microsoft">
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
    using System.Net;
    using System.Net.Http;
    using System.Text;

#if ASPNET_K || PORTABLE
    using System.Threading;
    using System.Threading.Tasks;
#else
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
    using Windows.Storage.Streams;
    using Windows.Storage;
#endif

    /// <summary>
    /// Represents an append blob, a type of blob where blocks of data are always committed to the end of the blob.
    /// </summary>
    public sealed partial class CloudAppendBlob : CloudBlob, ICloudBlob
    {
        /// <summary>
        /// Opens a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <returns>A stream to be used for writing to the blob.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<CloudBlobStream> OpenWriteAsync(bool createNew)
#else
        public IAsyncOperation<ICloudBlobStream> OpenWriteAsync(bool createNew)
#endif
        {
            return this.OpenWriteAsync(createNew, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Opens a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> to create a new append blob or overwrite an existing one, <c>false</c> to append to an existing blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// Note that this method always makes a call to the <see cref="CloudBlob.BeginFetchAttributes(AccessCondition, BlobRequestOptions, OperationContext, AsyncCallback, object)"/> method under the covers.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// <returns>A stream to be used for writing to the blob.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<CloudBlobStream> OpenWriteAsync(bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.OpenWriteAsync(createNew, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Opens a stream for writing to the blob.
        /// </summary>
        /// <param name="createNew">Use <c>true</c> if the append blob is newly created, <c>false</c> otherwise.</param>                
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A stream to be used for writing to the blob.</returns>
        [DoesServiceRequest]
        public Task<CloudBlobStream> OpenWriteAsync(bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
#else
        public IAsyncOperation<ICloudBlobStream> OpenWriteAsync(bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#endif
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient, false);
            if (!createNew && modifiedOptions.StoreBlobContentMD5.Value)
            {
                throw new ArgumentException(SR.MD5NotPossible);
            }

#if ASPNET_K || PORTABLE
            return Task.Run(async () =>
#else
            return AsyncInfo.Run(async (token) =>
#endif
            {
                if (createNew)
                {
#if ASPNET_K || PORTABLE
                    await this.CreateOrReplaceAsync(accessCondition, options, operationContext, cancellationToken);
#else
                    await this.CreateOrReplaceAsync(accessCondition, options, operationContext).AsTask(token);
#endif
                }
                else
                {
                    // Although we don't need any properties from the service, we should make this call in order to honor the user specified conditional headers
                    // while opening an existing stream and to get the append position for an existing blob if user didn't specify one.
#if ASPNET_K || PORTABLE
                    await this.FetchAttributesAsync(accessCondition, options, operationContext, cancellationToken);
#else
                    await this.FetchAttributesAsync(accessCondition, options, operationContext).AsTask(token);
#endif
                }

                if (accessCondition != null)
                {
                    accessCondition = new AccessCondition() { LeaseId = accessCondition.LeaseId, IfAppendPositionEqual = accessCondition.IfAppendPositionEqual, IfMaxSizeLessThanOrEqual = accessCondition.IfMaxSizeLessThanOrEqual };
                }

#if ASPNET_K || PORTABLE
                CloudBlobStream stream = new BlobWriteStream(this, accessCondition, modifiedOptions, operationContext);
#else
                ICloudBlobStream stream = new BlobWriteStreamHelper(this, accessCondition, modifiedOptions, operationContext);
#endif
                return stream;
#if ASPNET_K || PORTABLE
            }, cancellationToken);
#else
            });
#endif
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromStreamAsync(Stream source)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(IInputStream)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction UploadFromStreamAsync(IInputStream source)
#endif
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, true /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, long)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromStreamAsync(Stream source, long length)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(IInputStream, long)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction UploadFromStreamAsync(IInputStream source, long length)
#endif
        {
            return this.UploadFromStreamAsyncHelper(source, length, true /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(IInputStream, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction UploadFromStreamAsync(IInputStream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#endif
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, true /* createNew */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(Stream, long, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStreamAsync(IInputStream, long, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction UploadFromStreamAsync(IInputStream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#endif
        {
            return this.UploadFromStreamAsyncHelper(source, length, true /* createNew */, accessCondition, options, operationContext);
        }

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStream(Stream, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, true /* createNew */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to an append blob. If the blob already exists, it will be overwritten. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromStream(Stream, long, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, true /* createNew */, accessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.                
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendFromStreamAsync(Stream source)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction AppendFromStreamAsync(IInputStream source)
#endif
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, false /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendFromStreamAsync(Stream source, long length)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction AppendFromStreamAsync(IInputStream source, long length)
#endif
        {
            return this.UploadFromStreamAsyncHelper(source, length, false /* createNew */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction AppendFromStreamAsync(IInputStream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#endif
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, false /* createNew */, accessCondition, options, operationContext);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction AppendFromStreamAsync(IInputStream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#endif
        {
            return this.UploadFromStreamAsyncHelper(source, length, false /* createNew */, accessCondition, options, operationContext);
        }

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, null /* length */, false /* createNew */, accessCondition, options, operationContext, cancellationToken);
        }

        /// <summary>
        /// Appends a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.UploadFromStreamAsyncHelper(source, length, false /* createNew */, accessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Uploads a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="createNew"><c>true</c> if the append blob is newly created, <c>false</c> otherwise.</param>        
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal Task UploadFromStreamAsyncHelper(Stream source, long? length, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return UploadFromStreamAsyncHelper(source, length, createNew, accessCondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a stream to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="source">The stream providing the blob content.</param>
        /// <param name="length">The number of bytes to write from the source stream at its current position.</param>
        /// <param name="createNew"><c>true</c> if the append blob is newly created, <c>false</c> otherwise.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal Task UploadFromStreamAsyncHelper(Stream source, long? length, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        internal IAsyncAction UploadFromStreamAsyncHelper(IInputStream source, long? length, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#endif
        {
            CommonUtility.AssertNotNull("source", source);

            Stream sourceAsStream = source.AsStreamForRead();
            if (length.HasValue)
            {
                CommonUtility.AssertInBounds("length", length.Value, 1);

                if (sourceAsStream.CanSeek && length > sourceAsStream.Length - sourceAsStream.Position)
                {
                    throw new ArgumentOutOfRangeException("length", SR.StreamLengthShortError);
                }
            }

            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            operationContext = operationContext ?? new OperationContext();
            ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);

#if ASPNET_K || PORTABLE
            return Task.Run(async () =>
            {
                using (CloudBlobStream blobStream = await this.OpenWriteAsync(createNew, accessCondition, options, operationContext, cancellationToken))
                {
                    // We should always call AsStreamForWrite with bufferSize=0 to prevent buffering. Our
                    // stream copier only writes 64K buffers at a time anyway, so no buffering is needed.
                    await sourceAsStream.WriteToAsync(blobStream, length, null /* maxLength */, false, tempExecutionState, null /* streamCopyState */, cancellationToken);
                    await blobStream.CommitAsync();
                }
            }, cancellationToken);
#else
            return AsyncInfo.Run(async (token) =>
            {
                using (ICloudBlobStream blobStream = await this.OpenWriteAsync(createNew, accessCondition, options, operationContext).AsTask(token))
                {
                    // We should always call AsStreamForWrite with bufferSize=0 to prevent buffering. Our
                    // stream copier only writes 64K buffers at a time anyway, so no buffering is needed.
                    await sourceAsStream.WriteToAsync(blobStream.AsStreamForWrite(0), length, null /* maxLength */, false, tempExecutionState, null /* streamCopyState */, token);
                    await blobStream.CommitAsync().AsTask(token);
                }
            });
#endif
        }

#if !PORTABLE
        /// <summary>
        /// Uploads a file to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
#if ASPNET_K
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(string, FileMode)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromFileAsync(string path, FileMode mode)
        {
            return this.UploadFromFileAsync(path, mode, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(StorageFile)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction UploadFromFileAsync(StorageFile source)
        {
            return this.UploadFromFileAsync(source, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#endif

        /// <summary>
        /// Uploads a file to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(string, FileMode, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromFileAsync(path, mode, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(StorageFile, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction UploadFromFileAsync(StorageFile source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("source", source);

            return AsyncInfo.Run(async (token) =>
            {
                using (IRandomAccessStreamWithContentType stream = await source.OpenReadAsync().AsTask(token))
                {
                    await this.UploadFromStreamAsync(stream, accessCondition, options, operationContext).AsTask(token);
                }
            });
        }
#endif

#if ASPNET_K
        /// <summary>
        /// Uploads a file to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromFileAsync(string, FileMode, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);

            return Task.Run(async () =>
            {
                using (Stream stream = new FileStream(path, mode, FileAccess.Read))
                {
                    await this.UploadFromStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
                }
            }, cancellationToken);
        }
#endif

        /// <summary>
        /// Appends a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
#if ASPNET_K
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task AppendFromFileAsync(string path, FileMode mode)
        {
            return this.AppendFromFileAsync(path, mode, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#else
        /// <param name="source">The file providing the blob content.</param>
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction AppendFromFileAsync(StorageFile source)
        {
            return this.AppendFromFileAsync(source, null /* accessCondition */, null /* options */, null /* operationContext */);
        }
#endif

        /// <summary>
        /// Appends a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendFromFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendFromFileAsync(path, mode, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction AppendFromFileAsync(StorageFile source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("source", source);

            return AsyncInfo.Run(async (token) =>
            {
                using (IRandomAccessStreamWithContentType stream = await source.OpenReadAsync().AsTask(token))
                {
                    await this.AppendFromStreamAsync(stream, accessCondition, options, operationContext).AsTask(token);
                }
            });
        }
#endif

#if ASPNET_K
        /// <summary>
        /// Appends a file to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="path">A string containing the file path providing the blob content.</param>
        /// <param name="mode">A <see cref="System.IO.FileMode"/> enumeration value that specifies how to open the file.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendFromFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("path", path);

            return Task.Run(async () =>
            {
                using (Stream stream = new FileStream(path, mode, FileAccess.Read))
                {
                    await this.AppendFromStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
                }
            }, cancellationToken);
        }
#endif
#endif

        /// <summary>
        /// Uploads the contents of a byte array to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction UploadFromByteArrayAsync([ReadOnlyArray] byte[] buffer, int index, int count)
#endif
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads the contents of a byte array to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction UploadFromByteArrayAsync([ReadOnlyArray] byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.UploadFromStreamAsync(stream.AsInputStream(), accessCondition, options, operationContext);
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Uploads the contents of a byte array to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendFromByteArrayAsync(byte[], int, int, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.UploadFromStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Appends the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task AppendFromByteArrayAsync(byte[] buffer, int index, int count)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction AppendFromByteArrayAsync([ReadOnlyArray] byte[] buffer, int index, int count)
#endif
        {
            return this.AppendFromByteArrayAsync(buffer, index, count, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Appends the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendFromByteArrayAsync(buffer, index, count, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction AppendFromByteArrayAsync([ReadOnlyArray] byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.AppendFromStreamAsync(stream.AsInputStream(), accessCondition, options, operationContext);
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Appends the contents of a byte array to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="index">The zero-based byte offset in buffer at which to begin uploading bytes to the blob.</param>
        /// <param name="count">The number of bytes to be written to the blob.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);

            SyncMemoryStream stream = new SyncMemoryStream(buffer, index, count);
            return this.AppendFromStreamAsync(stream, accessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Uploads a string of text to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload, encoded as a UTF-8 string.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadTextAsync(string content)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction UploadTextAsync(string content)
#endif
        {
            return this.UploadTextAsync(content, null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Uploads a string of text to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string, Encoding, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.UploadTextAsync(content, encoding, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string, Encoding, AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.UploadFromByteArrayAsync(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext);
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Uploads a string of text to an append blob. If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// To append data to an append blob that already exists, see <see cref="AppendTextAsync(string, Encoding, AccessCondition, BlobRequestOptions, OperationContext, CancellationToken)"/>.
        /// </remarks>
        [DoesServiceRequest]
        public Task UploadTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.UploadFromByteArrayAsync(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Appends a string of text to an append blob.
        /// </summary>
        /// <param name="content">The text to upload, encoded as a UTF-8 string.</param>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// </remarks>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task AppendTextAsync(string content)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction AppendTextAsync(string content)
#endif
        {
            return this.AppendTextAsync(content, null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Appends a string of text to an append blob. Recommended only for single-writer scenarios.
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendTextAsync(content, encoding, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public IAsyncAction AppendTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.AppendFromByteArrayAsync(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext);
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Appends a string of text to an append blob. 
        /// </summary>
        /// <param name="content">The text to upload.</param>
        /// <param name="encoding">A <see cref="System.Text.Encoding"/> object that indicates the text encoding to use. If <c>null</c>, UTF-8 will be used.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        /// <remarks>
        /// Use this method only in single-writer scenarios. Internally, this method uses the append-offset conditional header to avoid duplicate blocks, which may cause problems in multiple-writer scenarios.        
        /// If you have a single-writer scenario, see <see cref="BlobRequestOptions.AbsorbConditionalErrorsOnRetry"/> to determine whether setting this flag to <c>true</c> is acceptable for your scenario.
        /// </remarks>
        [DoesServiceRequest]
        public Task AppendTextAsync(string content, Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("content", content);

            byte[] contentAsBytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            return this.AppendFromByteArrayAsync(contentAsBytes, 0, contentAsBytes.Length, accessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Creates an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception instead of overwriting the blob, 
        /// use <see cref="CreateOrReplaceAsync(AccessCondition, BlobRequestOptions, OperationContext)"/>.
        /// </summary>
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task CreateOrReplaceAsync()
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncAction CreateOrReplaceAsync()
#endif
        {
            return this.CreateOrReplaceAsync(null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Creates an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception instead of overwriting the blob,
        /// pass in an <see cref="AccessCondition"/> object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task CreateOrReplaceAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.CreateOrReplaceAsync(accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>        
        [DoesServiceRequest]
        public IAsyncAction CreateOrReplaceAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsyncNullReturn(
                this.CreateImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Creates an empty append blob. If the blob already exists, this operation will overwrite it. To throw an exception instead of overwriting the blob,
        /// pass in an <see cref="AccessCondition"/> object generated using <see cref="AccessCondition.GenerateIfNotExistsCondition"/>.
        /// </summary>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task CreateOrReplaceAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsyncNullReturn(
                this.CreateImpl(accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

        /// <summary>
        /// Commits a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A stream that provides the data for the block.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task<long> AppendBlockAsync(Stream blockData)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncOperation<long> AppendBlockAsync(IInputStream blockData)
#endif
        {
            return this.AppendBlockAsync(blockData, null /* contentMD5 */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Commits a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A stream that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="BlobProperties.ContentMD5"/> property
        /// on the blob. May be <c>null</c> or an empty string.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task<long> AppendBlockAsync(Stream blockData, string contentMD5)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncOperation<long> AppendBlockAsync(IInputStream blockData, string contentMD5)
#endif
        {
            return this.AppendBlockAsync(blockData, contentMD5, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Commits a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A stream that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="BlobProperties.ContentMD5"/> property
        /// on the blob. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
#if ASPNET_K || PORTABLE
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        public Task<long> AppendBlockAsync(Stream blockData, string contentMD5, AccessCondition accesscondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.AppendBlockAsync(blockData, contentMD5, accesscondition, options, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Commits a new block of data to the end of the blob.
        /// </summary>
        /// <param name="blockData">A stream that provides the data for the block.</param>
        /// <param name="contentMD5">An optional hash value that will be used to set the <see cref="BlobProperties.ContentMD5"/> property
        /// on the blob. May be <c>null</c> or an empty string.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public Task<long> AppendBlockAsync(Stream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
#else
        /// <returns>An <see cref="IAsyncAction"/> that represents an asynchronous action.</returns>
        [DoesServiceRequest]
        public IAsyncOperation<long> AppendBlockAsync(IInputStream blockData, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
#endif
        {
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            bool requiresContentMD5 = string.IsNullOrEmpty(contentMD5) && modifiedOptions.UseTransactionalMD5.Value;
            operationContext = operationContext ?? new OperationContext();
            ExecutionState<NullType> tempExecutionState = CommonUtility.CreateTemporaryExecutionState(modifiedOptions);

#if ASPNET_K || PORTABLE
            return Task.Run(async () =>
#else
           return AsyncInfo.Run(async (cancellationToken) =>
#endif
            {
                Stream blockDataAsStream = blockData.AsStreamForRead();
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

                    return await Executor.ExecuteAsync(
                        this.AppendBlockImpl(seekableStream, contentMD5, accessCondition, modifiedOptions),
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
#if ASPNET_K || PORTABLE
            }, cancellationToken);
#else
        });
#endif
        }

        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <returns>The contents of the blob, as a string.</returns>
#if ASPNET_K || PORTABLE
        public Task<string> DownloadTextAsync()
#else
        public IAsyncOperation<string> DownloadTextAsync()
#endif
        {
            return this.DownloadTextAsync(null /* encoding */, null /* accessCondition */, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>The contents of the blob, as a string.</returns>
#if ASPNET_K || PORTABLE
        public Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.DownloadTextAsync(encoding, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return AsyncInfo.Run(async (token) =>
            {
                using (SyncMemoryStream stream = new SyncMemoryStream())
                {
                    await this.DownloadToStreamAsync(stream.AsOutputStream(), accessCondition, options, operationContext).AsTask(token);
                    byte[] streamAsBytes = stream.ToArray();
                    return (encoding ?? Encoding.UTF8).GetString(streamAsBytes, 0, streamAsBytes.Length);
                }
            });
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Downloads the blob's contents as a string.
        /// </summary>
        /// <param name="encoding">An object that indicates the text encoding to use.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>The contents of the blob, as a string.</returns>
        public Task<string> DownloadTextAsync(Encoding encoding, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
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
#endif

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
#if ASPNET_K || PORTABLE
        public Task<string> StartCopyAsync(CloudAppendBlob source)
#else
        public IAsyncOperation<string> StartCopyAsync(CloudAppendBlob source)
#endif
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source));
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
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<string> StartCopyAsync(CloudAppendBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(source, sourceAccessCondition, destAccessCondition, options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<string> StartCopyAsync(CloudAppendBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext);
        }
#endif

#if ASPNET_K || PORTABLE
        /// <summary>
        /// Begins an operation to start copying another append blob's contents, properties, and metadata to a new blob.
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
        public Task<string> StartCopyAsync(CloudAppendBlob source, AccessCondition sourceAccessCondition, AccessCondition destAccessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return this.StartCopyAsync(CloudBlob.SourceBlobToUri(source), sourceAccessCondition, destAccessCondition, options, operationContext, cancellationToken);
        }
#endif

        /// <summary>
        /// Creates a snapshot of the blob.
        /// </summary>
        /// <returns>A blob snapshot.</returns>
        [DoesServiceRequest]
#if ASPNET_K || PORTABLE
        public Task<CloudAppendBlob> CreateSnapshotAsync()
#else
        public IAsyncOperation<CloudAppendBlob> CreateSnapshotAsync()
#endif
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
#if ASPNET_K || PORTABLE
        public Task<CloudAppendBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            return CreateSnapshotAsync(metadata, accessCondition, options, operationContext, CancellationToken.None);
        }
#else
        public IAsyncOperation<CloudAppendBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            return AsyncInfo.Run(async (token) => await Executor.ExecuteAsync(
                this.CreateSnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                token));
        }
#endif

#if ASPNET_K || PORTABLE
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
        public Task<CloudAppendBlob> CreateSnapshotAsync(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            this.attributes.AssertNoSnapshot();
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(options, BlobType.AppendBlob, this.ServiceClient);
            return Task.Run(async () => await Executor.ExecuteAsync(
                this.CreateSnapshotImpl(metadata, accessCondition, modifiedOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken), cancellationToken);
        }
#endif

    /// <summary>
    /// Implements the Create method.
    /// </summary>
    /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If null, no condition is used.</param>
    /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
    /// <returns>A <see cref="TaskSequence"/> that creates the blob.</returns>
    private RESTCommand<NullType> CreateImpl(AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<NullType> putCmd = new RESTCommand<NullType>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                HttpRequestMessage msg = BlobHttpRequestMessageFactory.Put(uri, serverTimeout, this.Properties, BlobType.AppendBlob, 0, accessCondition, cnt, ctx);
                BlobHttpRequestMessageFactory.AddMetadata(msg, this.Metadata);
                return msg;
            };
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, NullType.Value, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                this.Properties.Length = 0;
                return NullType.Value;
            };

            return putCmd;
        }

        /// <summary>
        /// Commits the block to the end of the blob.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="contentMD5">The content MD5.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that uploads the block.</returns>
        internal RESTCommand<long> AppendBlockImpl(Stream source, string contentMD5, AccessCondition accessCondition, BlobRequestOptions options)
        {
            long offset = source.Position;
            long length = source.Length - offset;

            RESTCommand<long> putCmd = new RESTCommand<long>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildContent = (cmd, ctx) => HttpContentFactory.BuildContentFromStream(source, offset, length, contentMD5, cmd, ctx);
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.AppendBlock(uri, serverTimeout, accessCondition, cnt, ctx);
            putCmd.PreProcessResponse = (cmd, resp, ex, ctx) =>
            {
                long appendOffset = -1;
                if (resp.Headers.Contains(Constants.HeaderConstants.BlobAppendOffset))
                {
                    appendOffset = long.Parse(resp.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.BlobAppendOffset));
                }

                HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Created, resp, appendOffset, cmd, ex);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(this.attributes, resp, false);
                return appendOffset;
            };

            return putCmd;
        }

        /// <summary>
        /// Implementation for the CreateSnapshot method.
        /// </summary>
        /// <param name="metadata">A collection of name-value pairs defining the metadata of the snapshot, or null.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the access conditions for the blob. If <c>null</c>, no condition is used.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="RESTCommand"/> that creates the snapshot.</returns>
        /// <remarks>If the <c>metadata</c> parameter is <c>null</c> then no metadata is associated with the request.</remarks>
        internal RESTCommand<CloudAppendBlob> CreateSnapshotImpl(IDictionary<string, string> metadata, AccessCondition accessCondition, BlobRequestOptions options)
        {
            RESTCommand<CloudAppendBlob> putCmd = new RESTCommand<CloudAppendBlob>(this.ServiceClient.Credentials, this.attributes.StorageUri);

            options.ApplyToStorageCommand(putCmd);
            putCmd.Handler = this.ServiceClient.AuthenticationHandler;
            putCmd.BuildClient = HttpClientFactory.BuildHttpClient;
            putCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) =>
            {
                HttpRequestMessage msg = BlobHttpRequestMessageFactory.Snapshot(uri, serverTimeout, accessCondition, cnt, ctx);
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
                CloudAppendBlob snapshot = new CloudAppendBlob(this.Name, snapshotTime, this.Container);
                snapshot.attributes.Metadata = new Dictionary<string, string>(metadata ?? this.Metadata);
                snapshot.attributes.Properties = new BlobProperties(this.Properties);
                CloudBlob.UpdateETagLMTLengthAndSequenceNumber(snapshot.attributes, resp, false);
                return snapshot;
            };

            return putCmd;
        }
    }
}