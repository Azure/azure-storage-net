//-----------------------------------------------------------------------
// <copyright file="ICloudBlob.Common.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using Microsoft.WindowsAzure.Storage;

    /// <summary>
    /// An interface required for Microsoft Azure blob types. The <see cref="CloudBlockBlob"/> and <see cref="CloudPageBlob"/> classes implement the <see cref="ICloudBlob"/> interface.
    /// </summary>
    public partial interface ICloudBlob : IListBlobItem
    {
        /// <summary>
        /// Gets the blob's name.
        /// </summary>
        /// <value>A string containing the name of the blob.</value>
        string Name { get; }

        /// <summary>
        /// Gets the <see cref="CloudBlobClient"/> object that represents the Blob service.
        /// </summary>
        /// <value>A <see cref="CloudBlobClient"/> object.</value>
        CloudBlobClient ServiceClient { get; }

        /// <summary>
        /// Gets or sets the number of bytes to buffer when writing to a page blob stream or
        /// the block size for writing to a block blob.
        /// </summary>
        /// <value>The number of bytes to buffer or the size of a block, in bytes.</value>
        int StreamWriteSizeInBytes { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of bytes to buffer when reading from a blob stream.
        /// </summary>
        /// <value>The minimum number of bytes to buffer.</value>
        int StreamMinimumReadSizeInBytes { get; set; }

        /// <summary>
        /// Gets the blob's system properties.
        /// </summary>
        /// <value>A <see cref="BlobProperties"/> object.</value>
        BlobProperties Properties { get; }

        /// <summary>
        /// Gets the user-defined metadata for the blob.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> object containing the blob's metadata as a collection of name-value pairs.</value>
        IDictionary<string, string> Metadata { get; }

        /// <summary>
        /// Gets the date and time that the blob snapshot was taken, if this blob is a snapshot.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> containing the blob's snapshot time if the blob is a snapshot; otherwise, <c>null</c>.</value>
        /// <remarks>
        /// If the blob is not a snapshot, the value of this property is <c>null</c>.
        /// </remarks>
        DateTimeOffset? SnapshotTime { get; }

        /// <summary>
        /// Gets a value indicating whether this blob is a snapshot.
        /// </summary>
        /// <value><c>true</c> if this blob is a snapshot; otherwise, <c>false</c>.</value>
        bool IsSnapshot { get; }

        /// <summary>
        /// Gets the absolute URI to the blob, including query string information if the blob is a snapshot.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> specifying the absolute URI to the blob, including snapshot query information if the blob is a snapshot.</value>
        Uri SnapshotQualifiedUri { get; }

        /// <summary>
        /// Gets the blob's URI for both the primary and secondary locations, including query string information if the blob is a snapshot.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing the blob's URIs for both the primary and secondary locations, 
        /// including snapshot query information if the blob is a snapshot.</value>
        StorageUri SnapshotQualifiedStorageUri { get; }

        /// <summary>
        /// Gets the state of the most recent or pending copy operation.
        /// </summary>
        /// <value>A <see cref="CopyState"/> object containing the copy state, or <c>null</c> if there is no copy state for the blob.</value>
        CopyState CopyState { get; }

        /// <summary>
        /// Gets the type of the blob.
        /// </summary>
        /// <value>A <see cref="BlobType"/> enumeration value.</value>
        BlobType BlobType { get; }

        /// <summary>
        /// Returns a shared access signature for the blob.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        string GetSharedAccessSignature(SharedAccessBlobPolicy policy);

        /// <summary>
        /// Returns a shared access signature for the blob.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="groupPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
#if WINDOWS_RT
        [Windows.Foundation.Metadata.DefaultOverload]
#endif
        string GetSharedAccessSignature(SharedAccessBlobPolicy policy, string groupPolicyIdentifier);

        /// <summary>
        /// Returns a shared access signature for the blob.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="headers">A <see cref="SharedAccessBlobHeaders"/> object specifying optional header values to set for a blob accessed with this SAS.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers);

        /// <summary>
        /// Returns a shared access signature for the blob.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="headers">A <see cref="SharedAccessBlobHeaders"/> object specifying optional header values to set for a blob accessed with this SAS.</param>
        /// <param name="groupPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string groupPolicyIdentifier);

        /// <summary>
        /// Returns a shared access signature for the blob.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="headers">A <see cref="SharedAccessBlobHeaders"/> object specifying optional header values to set for a blob accessed with this SAS.</param>
        /// <param name="groupPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <param name="protocols">The allowed protocols (https only, or http and https). Null if you don't want to restrict protocol.</param>
        /// <param name="ipAddressOrRange">The allowed IP address or IP address range. Null if you don't want to restrict based on IP address.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange);

    }
}
