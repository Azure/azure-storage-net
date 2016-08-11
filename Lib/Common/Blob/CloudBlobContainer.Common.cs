//-----------------------------------------------------------------------
// <copyright file="CloudBlobContainer.Common.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Represents a container in the Microsoft Azure Blob service.
    /// </summary>
    public partial class CloudBlobContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobContainer"/> class.
        /// </summary>
        /// <param name="containerAddress">A <see cref="System.Uri"/> object specifying the absolute URI to the container.</param>
        public CloudBlobContainer(Uri containerAddress)
            : this(containerAddress, null /* credentials */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobContainer"/> class.
        /// </summary>
        /// <param name="containerAddress">A <see cref="System.Uri"/> object specifying the absolute URI to the container.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudBlobContainer(Uri containerAddress, StorageCredentials credentials)
            : this(new StorageUri(containerAddress), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobContainer"/> class.
        /// </summary>
        /// <param name="containerAddress">A <see cref="System.Uri"/> object specifying the absolute URI to the container.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudBlobContainer(StorageUri containerAddress, StorageCredentials credentials)
        {
            CommonUtility.AssertNotNull("containerAddress", containerAddress);
            CommonUtility.AssertNotNull("containerAddress", containerAddress.PrimaryUri);

            this.ParseQueryAndVerify(containerAddress, credentials);
            this.Metadata = new Dictionary<string, string>();
            this.Properties = new BlobContainerProperties();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobContainer"/> class.
        /// </summary>
        /// <param name="containerName">A string specifying the container name.</param>
        /// <param name="serviceClient">A <see cref="CloudBlobClient"/> object.</param>
        internal CloudBlobContainer(string containerName, CloudBlobClient serviceClient)
            : this(new BlobContainerProperties(), new Dictionary<string, string>(), containerName, serviceClient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobContainer"/> class.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="metadata">The metadata.</param>
        /// <param name="containerName">The container name.</param>
        /// <param name="serviceClient">The client to be used.</param>
        internal CloudBlobContainer(BlobContainerProperties properties, IDictionary<string, string> metadata, string containerName, CloudBlobClient serviceClient)
        {
            this.StorageUri = NavigationHelper.AppendPathToUri(serviceClient.StorageUri, containerName);
            this.ServiceClient = serviceClient;
            this.Name = containerName;
            this.Metadata = metadata;
            this.Properties = properties;
        }

        /// <summary>
        /// Gets the Blob service client for the container.
        /// </summary>
        /// <value>A <see cref="CloudBlobClient"/> object.</value>
        public CloudBlobClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets the container's URI for the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> specifying the absolute URI to the container at the primary location.</value>
        public Uri Uri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the container's URIs for both the primary and secondary locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing the container's URIs for both the primary and secondary locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Gets the name of the container.
        /// </summary>
        /// <value>A string containing the container name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the container's metadata.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> object containing the container's metadata.</value>
        public IDictionary<string, string> Metadata { get; private set; }

        /// <summary>
        /// Gets the container's system properties.
        /// </summary>
        /// <value>A <see cref="BlobContainerProperties"/> object.</value>
        public BlobContainerProperties Properties { get; private set; }

        /// <summary>
        /// Parse URI for SAS (Shared Access Signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="credentials">The credentials to use.</param>
        private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
        {
            StorageCredentials parsedCredentials;
            DateTimeOffset? parsedSnapshot;
            this.StorageUri = NavigationHelper.ParseBlobQueryAndVerify(address, out parsedCredentials, out parsedSnapshot);

            if (parsedCredentials != null && credentials != null)
            {
                string error = string.Format(CultureInfo.CurrentCulture, SR.MultipleCredentialsProvided);
                throw new ArgumentException(error);
            }

            this.ServiceClient = new CloudBlobClient(NavigationHelper.GetServiceClientBaseAddress(this.StorageUri, null /* usePathStyleUris */), credentials ?? parsedCredentials);
            this.Name = NavigationHelper.GetContainerNameFromContainerAddress(this.Uri, this.ServiceClient.UsePathStyleUris);
        }

        /// <summary>
        /// Returns the canonical name for shared access.
        /// </summary>
        /// <returns>The canonical name.</returns>
        private string GetSharedAccessCanonicalName()
        {
            string accountName = this.ServiceClient.Credentials.AccountName;
            string containerName = this.Name;

            string canonicalNameFormat = "/{0}/{1}/{2}";

            return string.Format(CultureInfo.InvariantCulture, canonicalNameFormat, SR.Blob, accountName, containerName);
        }

        /// <summary>
        /// Returns a shared access signature for the container.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy)
        {
            return this.GetSharedAccessSignature(policy, null /* groupPolicyIdentifier */);
        }

        /// <summary>
        /// Returns a shared access signature for the container.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="groupPolicyIdentifier">A container-level access policy.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, string groupPolicyIdentifier)
        {
            return this.GetSharedAccessSignature(policy, groupPolicyIdentifier, null /* protocols */, null /* ipAddressOrRange */);
        }

        /// <summary>
        /// Returns a shared access signature for the container.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="groupPolicyIdentifier">A container-level access policy.</param>
        /// <param name="protocols">The allowed protocols (https only, or http and https). Null if you don't want to restrict protocol.</param>
        /// <param name="ipAddressOrRange">The allowed IP address or IP address range. Null if you don't want to restrict based on IP address.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
        {
            if (!this.ServiceClient.Credentials.IsSharedKey)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.CannotCreateSASWithoutAccountKey);
                throw new InvalidOperationException(errorMessage);
            }

            string resourceName = this.GetSharedAccessCanonicalName();

            StorageAccountKey accountKey = this.ServiceClient.Credentials.Key;
            string signature = SharedAccessSignatureHelper.GetHash(policy, null /* headers */, groupPolicyIdentifier, resourceName, Constants.HeaderConstants.TargetStorageVersion, protocols, ipAddressOrRange, accountKey.KeyValue);
            string accountKeyName = accountKey.KeyName;

            // Future resource type changes from "c" => "container"
            UriQueryBuilder builder = SharedAccessSignatureHelper.GetSignature(policy, null /* headers */, groupPolicyIdentifier, "c", signature, accountKeyName, Constants.HeaderConstants.TargetStorageVersion, protocols, ipAddressOrRange);

            return builder.ToString();
        }

        /// <summary>
        /// Gets a reference to a page blob in this container.
        /// </summary>
        /// <param name="blobName">A string containing the name of the page blob.</param>
        /// <returns>A <see cref="CloudPageBlob"/> object.</returns>
        public CloudPageBlob GetPageBlobReference(string blobName)
        {
            return this.GetPageBlobReference(blobName, null /* snapshotTime */);
        }

        /// <summary>
        /// Returns a reference to a page blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">A string containing the name of the page blob.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <returns>A <see cref="CloudPageBlob"/> object.</returns>
        public CloudPageBlob GetPageBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);
            return new CloudPageBlob(blobName, snapshotTime, this);
        }

        /// <summary>
        /// Gets a reference to a block blob in this container.
        /// </summary>
        /// <param name="blobName">A string containing the name of the block blob.</param>
        /// <returns>A <see cref="CloudBlockBlob"/> object.</returns>
        public CloudBlockBlob GetBlockBlobReference(string blobName)
        {
            return this.GetBlockBlobReference(blobName, null /* snapshotTime */);
        }

        /// <summary>
        /// Gets a reference to a block blob in this container.
        /// </summary>
        /// <param name="blobName">A string containing the name of the block blob.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <returns>A <see cref="CloudBlockBlob"/> object.</returns>
        public CloudBlockBlob GetBlockBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);
            return new CloudBlockBlob(blobName, snapshotTime, this);
        }

        /// <summary>
        /// Gets a reference to an append blob in this container.
        /// </summary>
        /// <param name="blobName">A string containing the name of the append blob.</param>
        /// <returns>A <see cref="CloudAppendBlob"/> object.</returns>
        public CloudAppendBlob GetAppendBlobReference(string blobName)
        {
            return this.GetAppendBlobReference(blobName, null /* snapshotTime */);
        }

        /// <summary>
        /// Gets a reference to an append blob in this container.
        /// </summary>
        /// <param name="blobName">A string containing the name of the append blob.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <returns>A <see cref="CloudAppendBlob"/> object.</returns>
        public CloudAppendBlob GetAppendBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);
            return new CloudAppendBlob(blobName, snapshotTime, this);
        }

        /// <summary>
        /// Gets a reference to a blob in this container.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <returns>A <see cref="CloudBlob"/> object.</returns>
        public CloudBlob GetBlobReference(string blobName)
        {
            return this.GetBlobReference(blobName, null /* snapshotTime */);
        }

        /// <summary>
        /// Gets a reference to a blob in this container.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <returns>A <see cref="CloudBlob"/> object.</returns>
        public CloudBlob GetBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);
            return new CloudBlob(blobName, snapshotTime, this);
        }

        /// <summary>
        /// Gets a reference to a virtual blob directory beneath this container.
        /// </summary>
        /// <param name="relativeAddress">A string containing the name of the virtual blob directory.</param>
        /// <returns>A <see cref="CloudBlobDirectory"/> object.</returns>
        public CloudBlobDirectory GetDirectoryReference(string relativeAddress)
        {
            CommonUtility.AssertNotNull("relativeAddress", relativeAddress);
            if (!string.IsNullOrEmpty(relativeAddress) && !relativeAddress.EndsWith(this.ServiceClient.DefaultDelimiter, StringComparison.Ordinal))
            {
                relativeAddress = relativeAddress + this.ServiceClient.DefaultDelimiter;
            }

            StorageUri blobDirectoryUri = NavigationHelper.AppendPathToUri(this.StorageUri, relativeAddress);
            return new CloudBlobDirectory(blobDirectoryUri, relativeAddress, this);
        }
    }
}
