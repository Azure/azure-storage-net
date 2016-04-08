//-----------------------------------------------------------------------
// <copyright file="CloudFileDirectory.Common.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Represents a directory of files, designated by a delimiter character.
    /// </summary>
    /// <remarks>Shares, which are encapsulated as <see cref="CloudFileShare"/> objects, hold directories, and directories hold files. Directories can also contain sub-directories.</remarks>
    public partial class CloudFileDirectory : IListFileItem
    {
        /// <summary>
        /// Stores the <see cref="CloudFileShare"/> that contains this directory.
        /// </summary>
        private CloudFileShare share;

        /// <summary>
        /// Stores the parent directory.
        /// </summary>
        private CloudFileDirectory parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileDirectory"/> class using an absolute URI to the directory.
        /// </summary>
        /// <param name="directoryAbsoluteUri">A <see cref="System.Uri"/> object containing the absolute URI to the directory.</param>
        public CloudFileDirectory(Uri directoryAbsoluteUri)
            : this(new StorageUri(directoryAbsoluteUri), null /* StorageCredentials */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileDirectory"/> class using an absolute URI to the directory.
        /// </summary>
        /// <param name="directoryAbsoluteUri">A <see cref="System.Uri"/> object containing the absolute URI to the directory.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudFileDirectory(Uri directoryAbsoluteUri, StorageCredentials credentials)
            : this(new StorageUri(directoryAbsoluteUri), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileDirectory"/> class using an absolute URI to the directory.
        /// </summary>
        /// <param name="directoryAbsoluteUri">A <see cref="System.Uri"/> object containing the absolute URI to the directory.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudFileDirectory(StorageUri directoryAbsoluteUri, StorageCredentials credentials)
        {
            this.Metadata = new Dictionary<string, string>();
            this.Properties = new FileDirectoryProperties();
            this.ParseQueryAndVerify(directoryAbsoluteUri, credentials);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileDirectory"/> class given an address and a client.
        /// </summary>
        /// <param name="uri">The file directory's Uri.</param>
        /// <param name="directoryName">Name of the directory.</param>
        /// <param name="share">The share for the directory.</param>
        internal CloudFileDirectory(StorageUri uri, string directoryName, CloudFileShare share)
        {
            CommonUtility.AssertNotNull("uri", uri);
            CommonUtility.AssertNotNull("directoryName", directoryName);
            CommonUtility.AssertNotNull("share", share);

            this.Metadata = new Dictionary<string, string>();
            this.Properties = new FileDirectoryProperties();
            this.StorageUri = uri;
            this.ServiceClient = share.ServiceClient;
            this.share = share;
            this.Name = directoryName;
        }

        /// <summary>
        /// Gets a <see cref="CloudFileClient"/> object that specifies the endpoint for the File service.
        /// </summary>
        /// <value>A <see cref="CloudFileClient"/> object.</value>
        public CloudFileClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets the directory's URI for the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> specifying the absolute URI to the directory at the primary location.</value>
        public Uri Uri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the file directory's URIs for all locations.
        /// </summary>
        /// <value>A <see cref="StorageUri"/> object containing the file directory's URIs for all locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Gets a <see cref="FileDirectoryProperties"/> object that represents the directory's system properties.
        /// </summary>
        /// <value>A <see cref="FileDirectoryProperties"/> object.</value>
        public FileDirectoryProperties Properties { get; internal set; }

        /// <summary>
        /// Gets the user-defined metadata for the directory.
        /// </summary>
        /// <value>The directory's metadata, as a collection of name-value pairs.</value>
        public IDictionary<string, string> Metadata { get; internal set; }

        /// <summary>
        /// Gets a <see cref="CloudFileShare"/> object that represents the share for the directory.
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
        /// Gets a <see cref="CloudFileDirectory"/> object that represents the parent directory for the directory.
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
        /// Gets the name of the directory.
        /// </summary>
        /// <value>A <see cref="System.String"/> containing the name of the directory.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Selects the protocol response.
        /// </summary>
        /// <param name="protocolItem">The protocol item.</param>
        /// <returns>The parsed <see cref="IListFileItem"/>.</returns>
        private IListFileItem SelectListFileItem(IListFileEntry protocolItem)
        {
            ListFileEntry file = protocolItem as ListFileEntry;
            if (file != null)
            {
                CloudFileAttributes attributes = file.Attributes;
                attributes.StorageUri = NavigationHelper.AppendPathToUri(this.StorageUri, file.Name);
                return new CloudFile(attributes, this.ServiceClient);
            }

            ListFileDirectoryEntry fileDirectory = protocolItem as ListFileDirectoryEntry;
            if (fileDirectory != null)
            {
                CloudFileDirectory directory = this.GetDirectoryReference(fileDirectory.Name);
                directory.Properties = fileDirectory.Properties;
                return directory;
            }

            throw new InvalidOperationException(SR.InvalidFileListItem);
        }

        /// <summary>
        /// Returns a <see cref="CloudFile"/> object that represents a file in this directory.
        /// </summary>
        /// <param name="fileName">A <see cref="System.String"/> containing the name of the file.</param>
        /// <returns>A <see cref="CloudFile"/> object.</returns>
        public CloudFile GetFileReference(string fileName)
        {
            CommonUtility.AssertNotNullOrEmpty("fileName", fileName);

            StorageUri subdirectoryUri = NavigationHelper.AppendPathToUri(this.StorageUri, fileName);
            return new CloudFile(subdirectoryUri, fileName, this.Share);
        }

        /// <summary>
        /// Returns a <see cref="CloudFileDirectory"/> object that represents a subdirectory within this directory.
        /// </summary>
        /// <param name="itemName">A <see cref="System.String"/> containing the name of the subdirectory.</param>
        /// <returns>A <see cref="CloudFileDirectory"/> object.</returns>
        public CloudFileDirectory GetDirectoryReference(string itemName)
        {
            CommonUtility.AssertNotNullOrEmpty("itemName", itemName);

            StorageUri subdirectoryUri = NavigationHelper.AppendPathToUri(this.StorageUri, itemName);
            return new CloudFileDirectory(subdirectoryUri, itemName, this.Share);
        }

        /// <summary>
        /// Parse URI.
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

            this.Name = NavigationHelper.GetFileName(this.Uri, this.ServiceClient.UsePathStyleUris);
        }
    }
}
