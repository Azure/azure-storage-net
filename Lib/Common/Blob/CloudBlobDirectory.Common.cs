//-----------------------------------------------------------------------
// <copyright file="CloudBlobDirectory.Common.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;

    /// <summary>
    /// Represents a virtual directory of blobs on the client which emulates a hierarchical data store by using delimiter characters.
    /// </summary>
    public partial class CloudBlobDirectory : IListBlobItem
    {
        /// <summary>
        /// Stores the parent directory.
        /// </summary>
        private CloudBlobDirectory parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobDirectory"/> class given an address and a client.
        /// </summary>
        /// <param name="uri">The blob directory's Uri.</param>
        /// <param name="prefix">The blob directory's prefix.</param> 
        /// <param name="container">The container for the virtual directory.</param>
        internal CloudBlobDirectory(StorageUri uri, string prefix, CloudBlobContainer container)
        {
            CommonUtility.AssertNotNull("uri", uri);
            CommonUtility.AssertNotNull("prefix", prefix);
            CommonUtility.AssertNotNull("container", container);

            this.ServiceClient = container.ServiceClient;
            this.Container = container;
            this.Prefix = prefix;
            this.StorageUri = uri;
        }

        /// <summary>
        /// Gets the Blob service client for the virtual directory.
        /// </summary>
        /// <value>A <see cref="CloudBlobClient"/> object.</value>
        public CloudBlobClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets the URI that identifies the virtual directory for the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> containing the URI to the virtual directory, at the primary location.</value>
        public Uri Uri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the blob directory's URIs for both the primary and secondary locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing the blob directory's URIs for both the primary and secondary locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Gets the container for the virtual directory.
        /// </summary>
        /// <value>A <see cref="CloudBlobContainer"/> object.</value>
        public CloudBlobContainer Container { get; private set; }

        /// <summary>
        /// Gets the parent directory for the virtual directory.
        /// </summary>
        /// <value>A <see cref="CloudBlobDirectory"/> object.</value>
        public CloudBlobDirectory Parent
        {
            get
            {
                if (this.parent == null)
                {
                    string parentName;
                    StorageUri parentUri;
                    if (NavigationHelper.GetBlobParentNameAndAddress(this.StorageUri, this.ServiceClient.DefaultDelimiter, this.ServiceClient.UsePathStyleUris, out parentName, out parentUri))
                    {
                        this.parent = new CloudBlobDirectory(parentUri, parentName, this.Container);
                    }
                }

                return this.parent;
            }
        }

        /// <summary>
        /// Gets the prefix.
        /// </summary>
        /// <value>A string containing the prefix.</value>
        public string Prefix { get; private set; }

        /// <summary>
        /// Gets a reference to a page blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <returns>A <see cref="CloudPageBlob"/> object.</returns>
        public CloudPageBlob GetPageBlobReference(string blobName)
        {
            return this.GetPageBlobReference(blobName, null /* snapshotTime */);
        }

        /// <summary>
        /// Returns a reference to a page blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">The name of the page blob.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <returns>A <see cref="CloudPageBlob"/> object.</returns>
        public CloudPageBlob GetPageBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);

            StorageUri blobUri = NavigationHelper.AppendPathToUri(this.StorageUri, blobName, this.ServiceClient.DefaultDelimiter);
            return new CloudPageBlob(blobUri, snapshotTime, this.ServiceClient.Credentials);
        }

        /// <summary>
        /// Gets a reference to a block blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <returns>A <see cref="CloudBlockBlob"/> object.</returns>
        public CloudBlockBlob GetBlockBlobReference(string blobName)
        {
            return this.GetBlockBlobReference(blobName, null /* snapshotTime */);
        }

        /// <summary>
        /// Gets a reference to a block blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <returns>A <see cref="CloudBlockBlob"/> object.</returns>
        public CloudBlockBlob GetBlockBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);

            StorageUri blobUri = NavigationHelper.AppendPathToUri(this.StorageUri, blobName, this.ServiceClient.DefaultDelimiter);
            return new CloudBlockBlob(blobUri, snapshotTime, this.ServiceClient.Credentials);
        }

        /// <summary>
        /// Gets a reference to an append blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <returns>A <see cref="CloudAppendBlob"/> object.</returns>
        public CloudAppendBlob GetAppendBlobReference(string blobName)
        {
            return this.GetAppendBlobReference(blobName, null /* snapshotTime */);
        }

        /// <summary>
        /// Gets a reference to an append blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <returns>A <see cref="CloudAppendBlob"/> object.</returns>
        public CloudAppendBlob GetAppendBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);

            StorageUri blobUri = NavigationHelper.AppendPathToUri(this.StorageUri, blobName, this.ServiceClient.DefaultDelimiter);
            return new CloudAppendBlob(blobUri, snapshotTime, this.ServiceClient.Credentials);
        }

        /// <summary>
        /// Gets a reference to a blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <returns>A <see cref="CloudBlob"/> object.</returns>
        public CloudBlob GetBlobReference(string blobName)
        {
            return this.GetBlobReference(blobName, null /* snapshotTime */);
        }

        /// <summary>
        /// Gets a reference to a blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <returns>A <see cref="CloudBlob"/> object.</returns>
        public CloudBlob GetBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);

            StorageUri blobUri = NavigationHelper.AppendPathToUri(this.StorageUri, blobName, this.ServiceClient.DefaultDelimiter);
            return new CloudBlob(blobUri, snapshotTime, this.ServiceClient.Credentials);
        }

        /// <summary>
        /// Returns a virtual subdirectory within this virtual directory.
        /// </summary>
        /// <param name="itemName">The name of the virtual subdirectory.</param>
        /// <returns>A <see cref="CloudBlobDirectory"/> object representing the virtual subdirectory.</returns>
        public CloudBlobDirectory GetDirectoryReference(string itemName)
        {
            CommonUtility.AssertNotNullOrEmpty("itemName", itemName);
            if (!itemName.EndsWith(this.ServiceClient.DefaultDelimiter, StringComparison.Ordinal))
            {
                itemName = itemName + this.ServiceClient.DefaultDelimiter;
            }

            StorageUri subdirectoryUri = NavigationHelper.AppendPathToUri(this.StorageUri, itemName, this.ServiceClient.DefaultDelimiter);
            return new CloudBlobDirectory(subdirectoryUri, this.Prefix + itemName, this.Container);
        }
    }
}