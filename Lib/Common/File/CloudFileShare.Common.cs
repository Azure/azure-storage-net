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
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Represents a share in the Microsoft Azure File service.
    /// </summary>
    public partial class CloudFileShare
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileShare"/> class.
        /// </summary>
        /// <param name="shareAddress">The absolute URI to the share.</param>
        public CloudFileShare(Uri shareAddress)
            : this(shareAddress, null /* credentials */)
        {
        }

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
        public CloudFileShare(StorageUri shareAddress, StorageCredentials credentials)
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
        /// Returns the canonical name for shared access.
        /// </summary>
        /// <returns>The canonical name.</returns>
        private string GetSharedAccessCanonicalName()
        {
            string accountName = this.ServiceClient.Credentials.AccountName;
            string shareName = this.Name;

            return string.Format(CultureInfo.InvariantCulture, "/{0}/{1}/{2}", SR.File, accountName, shareName);
        }

        /// <summary>
        /// Returns a shared access signature for the share.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessFilePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessFilePolicy policy)
        {
            return this.GetSharedAccessSignature(policy, null /* groupPolicyIdentifier */);
        }

        /// <summary>
        /// Returns a shared access signature for the share.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessFilePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="groupPolicyIdentifier">A share-level access policy.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessFilePolicy policy, string groupPolicyIdentifier)
        {
            return this.GetSharedAccessSignature(policy, groupPolicyIdentifier, null, null);
        }

        /// <summary>
        /// Returns a shared access signature for the share.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessFilePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="groupPolicyIdentifier">A share-level access policy.</param>
        /// <param name="protocols">The allowed protocols (https only, or http and https). Null if you don't want to restrict protocol.</param>
        /// <param name="ipAddressOrRange">The allowed IP address or IP address range. Null if you don't want to restrict based on IP address.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessFilePolicy policy, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
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

            UriQueryBuilder builder = SharedAccessSignatureHelper.GetSignature(policy, null /* headers */, groupPolicyIdentifier, "s", signature, accountKeyName, Constants.HeaderConstants.TargetStorageVersion, protocols, ipAddressOrRange);

            return builder.ToString();
        }

        /// <summary>
        /// Parse URI for SAS (Shared Access Signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="credentials">The credentials to use.</param>
        private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
        {
            StorageCredentials parsedCredentials;
            this.StorageUri = NavigationHelper.ParseFileQueryAndVerify(address, out parsedCredentials);

            if (parsedCredentials != null && credentials != null)
            {
                string error = string.Format(CultureInfo.CurrentCulture, SR.MultipleCredentialsProvided);
                throw new ArgumentException(error);
            }
            
            if (this.ServiceClient == null)
            {
                this.ServiceClient = new CloudFileClient(NavigationHelper.GetServiceClientBaseAddress(this.StorageUri, null /* usePathStyleUris */), credentials ?? parsedCredentials);
            }

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
