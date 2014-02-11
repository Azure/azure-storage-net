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
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Represents a container in the Windows Azure Blob service.
    /// </summary>
    public sealed partial class CloudBlobContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobContainer"/> class.
        /// </summary>
        /// <param name="containerAddress">The absolute URI to the container.</param>
        public CloudBlobContainer(Uri containerAddress)
            : this(containerAddress, null /* credentials */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobContainer"/> class.
        /// </summary>
        /// <param name="containerAddress">The absolute URI to the container.</param>
        /// <param name="credentials">The account credentials.</param>
        public CloudBlobContainer(Uri containerAddress, StorageCredentials credentials)
            : this(new StorageUri(containerAddress), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobContainer"/> class.
        /// </summary>
        /// <param name="containerAddress">The absolute URI to the container.</param>
        /// <param name="credentials">The account credentials.</param>
#if WINDOWS_RT
        /// <returns>A <see cref="CloudBlobContainer"/> object.</returns>
        public static CloudBlobContainer Create(StorageUri containerAddress, StorageCredentials credentials)
        {
            return new CloudBlobContainer(containerAddress, credentials);
        }

        internal CloudBlobContainer(StorageUri containerAddress, StorageCredentials credentials)
#else
        public CloudBlobContainer(StorageUri containerAddress, StorageCredentials credentials)
#endif
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
        /// <param name="containerName">The container name.</param>
        /// <param name="serviceClient">A client object that specifies the endpoint for the Blob service.</param>
        internal CloudBlobContainer(string containerName, CloudBlobClient serviceClient)
            : this(new BlobContainerProperties(), new Dictionary<string, string>(), containerName, serviceClient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobContainer" /> class.
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
        /// Gets the service client for the container.
        /// </summary>
        /// <value>A client object that specifies the endpoint for the Blob service.</value>
        public CloudBlobClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets the container's URI for the primary location.
        /// </summary>
        /// <value>The absolute URI to the container, at the primary location.</value>
        public Uri Uri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the container's URIs for all locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing the container's URIs for all locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Gets the name of the container.
        /// </summary>
        /// <value>The container's name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the container's metadata.
        /// </summary>
        /// <value>The container's metadata.</value>
        public IDictionary<string, string> Metadata { get; private set; }

        /// <summary>
        /// Gets the container's system properties.
        /// </summary>
        /// <value>The container's properties.</value>
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

            if ((parsedCredentials != null) && (credentials != null) && !parsedCredentials.Equals(credentials))
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

            return string.Format(CultureInfo.InvariantCulture, "/{0}/{1}", accountName, containerName);
        }

        /// <summary>
        /// Returns a shared access signature for the container.
        /// </summary>
        /// <param name="policy">The access policy for the shared access signature.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy)
        {
            return this.GetSharedAccessSignature(policy, null /* groupPolicyIdentifier */);
        }

        /// <summary>
        /// Returns a shared access signature for the container.
        /// </summary>
        /// <param name="policy">The access policy for the shared access signature.</param>
        /// <param name="groupPolicyIdentifier">A container-level access policy.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, string groupPolicyIdentifier)
        {
            if (!this.ServiceClient.Credentials.IsSharedKey)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.CannotCreateSASWithoutAccountKey);
                throw new InvalidOperationException(errorMessage);
            }

            string resourceName = this.GetSharedAccessCanonicalName();
            StorageAccountKey accountKey = this.ServiceClient.Credentials.Key;
            string signature = SharedAccessSignatureHelper.GetHash(policy, null /* headers */, groupPolicyIdentifier, resourceName, accountKey.KeyValue);
            string accountKeyName = accountKey.KeyName;

            // Future resource type changes from "c" => "container"
            UriQueryBuilder builder = SharedAccessSignatureHelper.GetSignature(policy, null /* headers */, groupPolicyIdentifier, "c", signature, accountKeyName);

            return builder.ToString();
        }

        /// <summary>
        /// Gets a reference to a page blob in this container.
        /// </summary>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>A reference to a page blob.</returns>
        public CloudPageBlob GetPageBlobReference(string blobName)
        {
            return this.GetPageBlobReference(blobName, null /* snapshotTime */);
        }

        /// <summary>
        /// Returns a reference to a page blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">The name of the page blob.</param>
        /// <param name="snapshotTime">The snapshot timestamp, if the blob is a snapshot.</param>
        /// <returns>A reference to a page blob.</returns>
        public CloudPageBlob GetPageBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);
            return new CloudPageBlob(blobName, snapshotTime, this);
        }

        /// <summary>
        /// Gets a reference to a block blob in this container.
        /// </summary>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>A reference to a block blob.</returns>
        public CloudBlockBlob GetBlockBlobReference(string blobName)
        {
            return this.GetBlockBlobReference(blobName, null /* snapshotTime */);
        }

        /// <summary>
        /// Gets a reference to a block blob in this container.
        /// </summary>
        /// <param name="blobName">The name of the blob.</param>
        /// <param name="snapshotTime">The snapshot timestamp, if the blob is a snapshot.</param>
        /// <returns>A reference to a block blob.</returns>
        public CloudBlockBlob GetBlockBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);
            return new CloudBlockBlob(blobName, snapshotTime, this);
        }

        /// <summary>
        /// Gets a reference to a virtual blob directory beneath this container.
        /// </summary>
        /// <param name="relativeAddress">The name of the virtual blob directory.</param>
        /// <returns>A reference to a virtual blob directory.</returns>
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
