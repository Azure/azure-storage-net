//-----------------------------------------------------------------------
// <copyright file="CloudFile.Common.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Represents a Microsoft Azure File.
    /// </summary>
    public partial class CloudFile : IListFileItem
    {
        /// <summary>
        /// Default is 4 MB.
        /// </summary>
        private int streamWriteSizeInBytes = Constants.DefaultWriteBlockSizeBytes;

        /// <summary>
        /// Default is 4 MB.
        /// </summary>
        private int streamMinimumReadSizeInBytes = Constants.DefaultWriteBlockSizeBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFile"/> class using an absolute URI to the file.
        /// </summary>
        /// <param name="fileAbsoluteUri">The absolute URI to the file.</param>
        public CloudFile(Uri fileAbsoluteUri)
            : this(fileAbsoluteUri, null /* credentials */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFile"/> class using an absolute URI to the file.
        /// </summary>
        /// <param name="fileAbsoluteUri">The absolute URI to the file.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudFile(Uri fileAbsoluteUri, StorageCredentials credentials)
            : this(new StorageUri(fileAbsoluteUri), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFile"/> class using an absolute URI to the file.
        /// </summary>
        /// <param name="fileAbsoluteUri">The absolute URI to the file.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudFile(StorageUri fileAbsoluteUri, StorageCredentials credentials)
        {
            this.attributes = new CloudFileAttributes();
            this.ParseQueryAndVerify(fileAbsoluteUri, credentials);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFile"/> class using the specified file name and
        /// the parent share reference.
        /// </summary>
        /// <param name="uri">The file's Uri.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="share">The reference to the parent share.</param>
        internal CloudFile(StorageUri uri, string fileName, CloudFileShare share)
        {
            CommonUtility.AssertNotNull("uri", uri);
            CommonUtility.AssertNotNullOrEmpty("fileName", fileName);
            CommonUtility.AssertNotNull("share", share);

            this.attributes = new CloudFileAttributes();
            this.attributes.StorageUri = uri;
            this.ServiceClient = share.ServiceClient;
            this.share = share;
            this.Name = fileName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFile"/> class.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="serviceClient">The service client.</param>
        internal CloudFile(CloudFileAttributes attributes, CloudFileClient serviceClient)
        {
            this.attributes = attributes;
            this.ServiceClient = serviceClient;

            this.ParseQueryAndVerify(this.StorageUri, this.ServiceClient.Credentials);
        }

        /// <summary>
        /// Stores the <see cref="CloudFileShare"/> that contains this file.
        /// </summary>
        private CloudFileShare share;

        /// <summary>
        /// Stores the file's parent <see cref="CloudFileDirectory"/>.
        /// </summary>
        private CloudFileDirectory parent;

        /// <summary>
        /// Stores the file's attributes.
        /// </summary>
        private readonly CloudFileAttributes attributes;

        /// <summary>
        /// Gets the <see cref="CloudFileClient"/> object that represents the File service.
        /// </summary>
        /// <value>A <see cref="CloudFileClient"/> object that specifies the File service endpoint.</value>
        public CloudFileClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets or sets the number of bytes to buffer when writing to a file stream.
        /// </summary>
        /// <value>The number of bytes to buffer, ranging from between 512 bytes and 4 MB inclusive.</value>
        public int StreamWriteSizeInBytes
        {
            get
            {
                return this.streamWriteSizeInBytes;
            }

            set
            {
                CommonUtility.AssertInBounds("StreamWriteSizeInBytes", value, Constants.PageSize, Constants.MaxBlockSize);
                this.streamWriteSizeInBytes = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum number of bytes to buffer when reading from a file stream.
        /// </summary>
        /// <value>The minimum number of bytes to buffer, being at least 16KB.</value>
        public int StreamMinimumReadSizeInBytes
        {
            get
            {
                return this.streamMinimumReadSizeInBytes;
            }

            set
            {
                CommonUtility.AssertInBounds("StreamMinimumReadSizeInBytes", value, 16 * Constants.KB);
                this.streamMinimumReadSizeInBytes = value;
            }
        }

        /// <summary>
        /// Gets the file's system properties.
        /// </summary>
        /// <value>A <see cref="FileProperties"/> object.</value>
        public FileProperties Properties
        {
            get
            {
                return this.attributes.Properties;
            }
        }

        /// <summary>
        /// Gets the user-defined metadata for the file.
        /// </summary>
        /// <value>The file's metadata, as a collection of name-value pairs.</value>
        public IDictionary<string, string> Metadata
        {
            get
            {
                return this.attributes.Metadata;
            }
        }

        /// <summary>
        /// Gets the file's URI.
        /// </summary>
        /// <value>The absolute URI to the file.</value>
        public Uri Uri
        {
            get
            {
                return this.attributes.Uri;
            }
        }

        /// <summary>
        /// Gets the absolute URI to the file.
        /// </summary>
        /// <value>A <see cref="StorageUri"/> object.</value>
        public StorageUri StorageUri
        {
            get
            {
                return this.attributes.StorageUri;
            }
        }

        /// <summary>
        /// Gets the state of the most recent or pending copy operation.
        /// </summary>
        /// <value>A <see cref="CopyState"/> object containing the copy state, or <c>null</c> if there is no copy state for the file.</value>
        public CopyState CopyState
        {
            get
            {
                return this.attributes.CopyState;
            }
        }

        /// <summary>
        /// Gets the file's name.
        /// </summary>
        /// <value>The file's name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a <see cref="CloudFileShare"/> object representing the file's share.
        /// </summary>
        /// <value>A <see cref="CloudFileShare"/> object.</value>
        public CloudFileShare Share
        {
            get
            {
                if (this.share == null)
                {
                    this.share = this.ServiceClient.GetShareReference(
                        NavigationHelper.GetShareName(this.Uri, this.ServiceClient.UsePathStyleUris));
                }

                return this.share;
            }
        }

        /// <summary>
        /// Gets the <see cref="CloudFileDirectory"/> object representing the
        /// parent directory for the file.
        /// </summary>
        /// <value>A <see cref="CloudFileDirectory"/> object.</value>
        public CloudFileDirectory Parent
        {
            get
            {
                if (this.parent == null)
                {
                    string parentName;
                    StorageUri parentUri;
                    if (NavigationHelper.GetFileParentNameAndAddress(this.StorageUri, this.ServiceClient.UsePathStyleUris, out parentName, out parentUri))
                    {
                        this.parent = new CloudFileDirectory(parentUri, parentName, this.Share);
                    }
                }

                return this.parent;
            }
        }

        /// <summary>
        /// Returns a shared access signature for the file.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessFilePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessFilePolicy policy)
        {
            return this.GetSharedAccessSignature(policy, null /* headers */, null /* groupPolicyIdentifier */);
        }

        /// <summary>
        /// Returns a shared access signature for the file.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessFilePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="groupPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
#if WINDOWS_RT
        [Windows.Foundation.Metadata.DefaultOverload]
#endif
        public string GetSharedAccessSignature(SharedAccessFilePolicy policy, string groupPolicyIdentifier)
        {
            return this.GetSharedAccessSignature(policy, null /* headers */, groupPolicyIdentifier);
        }

        /// <summary>
        /// Returns a shared access signature for the file.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessFilePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="headers">A <see cref="SharedAccessFileHeaders"/> object specifying optional header values to set for a file accessed with this SAS.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        public string GetSharedAccessSignature(SharedAccessFilePolicy policy, SharedAccessFileHeaders headers)
        {
            return this.GetSharedAccessSignature(policy, headers, null /* groupPolicyIdentifier */);
        }

        /// <summary>
        /// Returns a shared access signature for the file.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessFilePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="headers">A <see cref="SharedAccessFileHeaders"/> object specifying optional header values to set for a file accessed with this SAS.</param>
        /// <param name="groupPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        public string GetSharedAccessSignature(SharedAccessFilePolicy policy, SharedAccessFileHeaders headers, string groupPolicyIdentifier)
        {
            return GetSharedAccessSignature(policy, headers, groupPolicyIdentifier, null, null);
        }

        /// <summary>
        /// Returns a shared access signature for the file.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessFilePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="headers">A <see cref="SharedAccessFileHeaders"/> object specifying optional header values to set for a file accessed with this SAS.</param>
        /// <param name="groupPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <param name="protocols">The allowed protocols (https only, or http and https). Null if you don't want to restrict protocol.</param>
        /// <param name="ipAddressOrRange">The allowed IP address or IP address range. Null if you don't want to restrict based on IP address.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        public string GetSharedAccessSignature(SharedAccessFilePolicy policy, SharedAccessFileHeaders headers, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
        {
            if (!this.ServiceClient.Credentials.IsSharedKey)
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, SR.CannotCreateSASWithoutAccountKey);
                throw new InvalidOperationException(errorMessage);
            }

            string resourceName = this.GetCanonicalName();
            StorageAccountKey accountKey = this.ServiceClient.Credentials.Key;
            string signature = SharedAccessSignatureHelper.GetHash(policy, headers, groupPolicyIdentifier, resourceName, Constants.HeaderConstants.TargetStorageVersion, protocols, ipAddressOrRange, accountKey.KeyValue);

            UriQueryBuilder builder = SharedAccessSignatureHelper.GetSignature(policy, headers, groupPolicyIdentifier, "f", signature, accountKey.KeyName, Constants.HeaderConstants.TargetStorageVersion, protocols, ipAddressOrRange);

            return builder.ToString();
        }

        /// <summary>
        /// Gets the canonical name of the file, formatted as file/&lt;account-name&gt;/&lt;share-name&gt;/&lt;directory-name&gt;/&lt;file-name&gt;.
        /// <para>This is used by both Shared Access and Copy operations.</para>
        /// </summary>
        /// <returns>The canonical name of the file.</returns>
        private string GetCanonicalName()
        {
            string accountName = this.ServiceClient.Credentials.AccountName;
            string shareName = this.Share.Name;

            // Replace \ with / for uri compatibility when running under .net 4.5. 
            string fileAndDirectoryName = NavigationHelper.GetFileAndDirectoryName(this.Uri, this.ServiceClient.UsePathStyleUris).Replace('\\', '/');
            return string.Format(CultureInfo.InvariantCulture, "/{0}/{1}/{2}/{3}", SR.File, accountName, shareName, fileAndDirectoryName);
        }

        /// <summary>
        /// Parse URI.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="credentials">The credentials to use.</param>
        private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
        {
            StorageCredentials parsedCredentials;
            this.attributes.StorageUri = NavigationHelper.ParseFileQueryAndVerify(address, out parsedCredentials);

            if (parsedCredentials != null && credentials != null)
            {
                string error = string.Format(CultureInfo.CurrentCulture, SR.MultipleCredentialsProvided);
                throw new ArgumentException(error);
            }

            if (this.ServiceClient == null)
            {
                this.ServiceClient = new CloudFileClient(NavigationHelper.GetServiceClientBaseAddress(this.StorageUri, null /* usePathStyleUris */), credentials ?? parsedCredentials);
            }
            
            this.Name = NavigationHelper.GetFileName(this.Uri, this.ServiceClient.UsePathStyleUris);
        }
    }
}
