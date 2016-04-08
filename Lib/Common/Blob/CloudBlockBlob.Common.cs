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
    using Microsoft.WindowsAzure.Storage.Blob.Protocol;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

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
        /// <value>The size of a block, in bytes, ranging from between 16 KB and 4 MB inclusive.</value>
        public int StreamWriteSizeInBytes
        {
            get
            {
                return this.streamWriteSizeInBytes;
            }

            set
            {
                CommonUtility.AssertInBounds("StreamWriteSizeInBytes", value, 16 * Constants.KB, Constants.MaxBlockSize);
                this.streamWriteSizeInBytes = value;
            }
        }
    }
}
