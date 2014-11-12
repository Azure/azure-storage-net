//-----------------------------------------------------------------------
// <copyright file="CloudFileShare.Common.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a share in the Windows Azure File service.
    /// </summary>
    public sealed partial class CloudFileShare
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileShare"/> class.
        /// </summary>
        /// <param name="shareAddress">The absolute URI to the share.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudFileShare(Uri shareAddress, StorageCredentials credentials)
            : this(new StorageUri(shareAddress), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileShare"/> class.
        /// </summary>
        /// <param name="shareAddress">The absolute URI to the share.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
#if WINDOWS_RT || ASPNET_K
        /// <returns>A <see cref="CloudFileShare"/> object.</returns>
        public static CloudFileShare Create(StorageUri shareAddress, StorageCredentials credentials)
        {
            return new CloudFileShare(shareAddress, credentials);
        }

        internal CloudFileShare(StorageUri shareAddress, StorageCredentials credentials)
#else
        public CloudFileShare(StorageUri shareAddress, StorageCredentials credentials)
#endif
        {
            CommonUtility.AssertNotNull("shareAddress", shareAddress);
            CommonUtility.AssertNotNull("shareAddress", shareAddress.PrimaryUri);

            this.ParseQueryAndVerify(shareAddress, credentials);
            this.Metadata = new Dictionary<string, string>();
            this.Properties = new FileShareProperties();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileShare" /> class.
        /// </summary>
        /// <param name="shareName">The share name.</param>
        /// <param name="serviceClient">A client object that specifies the endpoint for the File service.</param>
        internal CloudFileShare(string shareName, CloudFileClient serviceClient)
            : this(new FileShareProperties(), new Dictionary<string, string>(), shareName, serviceClient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileShare" /> class.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="metadata">The metadata.</param>
        /// <param name="shareName">The share name.</param>
        /// <param name="serviceClient">The client to be used.</param>
        internal CloudFileShare(FileShareProperties properties, IDictionary<string, string> metadata, string shareName, CloudFileClient serviceClient)
        {
            this.StorageUri = NavigationHelper.AppendPathToUri(serviceClient.StorageUri, shareName);
            this.ServiceClient = serviceClient;
            this.Name = shareName;
            this.Metadata = metadata;
            this.Properties = properties;
        }

        /// <summary>
        /// Gets the service client for the share.
        /// </summary>
        /// <value>A client object that specifies the endpoint for the File service.</value>
        public CloudFileClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets the share's URI.
        /// </summary>
        /// <value>The absolute URI to the share.</value>
        public Uri Uri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the list of URIs for all locations.
        /// </summary>
        /// <value>The list of URIs for all locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Gets the name of the share.
        /// </summary>
        /// <value>The share's name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the share's metadata.
        /// </summary>
        /// <value>The share's metadata.</value>
        public IDictionary<string, string> Metadata { get; private set; }

        /// <summary>
        /// Gets the share's system properties.
        /// </summary>
        /// <value>The share's properties.</value>
        public FileShareProperties Properties { get; private set; }

        /// <summary>
        /// Parse URI for SAS (Shared Access Signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="credentials">The credentials to use.</param>
        private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
        {
            this.StorageUri = address;
            this.ServiceClient = new CloudFileClient(NavigationHelper.GetServiceClientBaseAddress(this.StorageUri, null), credentials);
            this.Name = NavigationHelper.GetShareNameFromShareAddress(this.Uri, this.ServiceClient.UsePathStyleUris);
        }

        /// <summary>
        /// Returns a reference to the root directory for this share.
        /// </summary>
        /// <returns>A reference to the root directory.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Reviewed")]
        public CloudFileDirectory GetRootDirectoryReference()
        {
            return new CloudFileDirectory(this.StorageUri, string.Empty, this);
        }
    }
}
