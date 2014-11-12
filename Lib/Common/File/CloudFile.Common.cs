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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a Windows Azure File.
    /// </summary>
    public sealed partial class CloudFile : IListFileItem
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
#if WINDOWS_RT || ASPNET_K
        /// <returns>A <see cref="CloudFile"/> object.</returns>
        public static CloudFile Create(StorageUri fileAbsoluteUri, StorageCredentials credentials)
        {
            return new CloudFile(fileAbsoluteUri, credentials);
        }

        internal CloudFile(StorageUri fileAbsoluteUri, StorageCredentials credentials)
#else
        public CloudFile(StorageUri fileAbsoluteUri, StorageCredentials credentials)
#endif
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
        /// Parse URI.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="credentials">The credentials to use.</param>
        private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
        {
            this.attributes.StorageUri = address;
            if (this.ServiceClient == null)
            {
                this.ServiceClient = new CloudFileClient(NavigationHelper.GetServiceClientBaseAddress(this.StorageUri, null /* usePathStyleUris */), credentials);
            }
            
            this.Name = NavigationHelper.GetFileName(this.Uri, this.ServiceClient.UsePathStyleUris);
        }
    }
}
