//-----------------------------------------------------------------------
// <copyright file="CloudBlob.Common.cs" company="Microsoft">
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
    /// Represents an Azure blob. A blob stores text or binary data, such as documents or media files.
    /// </summary>
    public partial class CloudBlob : IListBlobItem
    {
        /// <summary>
        /// Default is 4 MB.
        /// </summary>
        private int streamMinimumReadSizeInBytes = Constants.DefaultWriteBlockSizeBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlob"/> class using an absolute URI to the blob.
        /// </summary>
        /// <param name="blobAbsoluteUri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        public CloudBlob(Uri blobAbsoluteUri)
            : this(blobAbsoluteUri, null /* credentials */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlob"/> class using an absolute URI to the blob.
        /// </summary>
        /// <param name="blobAbsoluteUri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudBlob(Uri blobAbsoluteUri, StorageCredentials credentials)
            : this(blobAbsoluteUri, null /* snapshotTime */, credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlob"/> class using an absolute URI to the blob.
        /// </summary>
        /// <param name="blobAbsoluteUri">A <see cref="System.Uri"/> specifying the absolute URI to the blob.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudBlob(Uri blobAbsoluteUri, DateTimeOffset? snapshotTime, StorageCredentials credentials)
            : this(new StorageUri(blobAbsoluteUri), snapshotTime, credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlob"/> class using an absolute URI to the blob.
        /// </summary>
        /// <param name="blobAbsoluteUri">A <see cref="StorageUri"/> containing the absolute URI to the blob at both the primary and secondary locations.</param>
        /// <param name="snapshotTime">A <see cref="DateTimeOffset"/> specifying the snapshot timestamp, if the blob is a snapshot.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        /// <returns>A <see cref="CloudBlob"/> object.</returns>
        public CloudBlob(StorageUri blobAbsoluteUri, DateTimeOffset? snapshotTime, StorageCredentials credentials)
        {
            CommonUtility.AssertNotNull("blobAbsoluteUri", blobAbsoluteUri);
            CommonUtility.AssertNotNull("blobAbsoluteUri", blobAbsoluteUri.PrimaryUri);

            this.attributes = new BlobAttributes();
            this.SnapshotTime = snapshotTime;
            this.ParseQueryAndVerify(blobAbsoluteUri, credentials);
            this.Properties.BlobType = BlobType.Unspecified;
        }

         /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlob"/> class using the specified blob name and
        /// the parent container reference.
        /// If snapshotTime is not null, the blob instance represents a Snapshot.
        /// </summary>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="snapshotTime">Snapshot time in case the blob is a snapshot.</param>
        /// <param name="container">The reference to the parent container.</param>
        internal CloudBlob(string blobName, DateTimeOffset? snapshotTime, CloudBlobContainer container)
        {
            CommonUtility.AssertNotNullOrEmpty("blobName", blobName);
            CommonUtility.AssertNotNull("container", container);

            this.attributes = new BlobAttributes();
            this.attributes.StorageUri = NavigationHelper.AppendPathToUri(container.StorageUri, blobName);
            this.Name = blobName;
            this.ServiceClient = container.ServiceClient;
            this.container = container;
            this.SnapshotTime = snapshotTime;
            this.Properties.BlobType = BlobType.Unspecified;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlob"/> class.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="serviceClient">The service client.</param>
        internal CloudBlob(BlobAttributes attributes, CloudBlobClient serviceClient)
        {
            this.attributes = attributes;
            this.ServiceClient = serviceClient;

            this.ParseQueryAndVerify(this.StorageUri, this.ServiceClient.Credentials);
            this.Properties.BlobType = BlobType.Unspecified;
        }

        /// <summary>
        /// Stores the <see cref="CloudBlobContainer"/> that contains this blob.
        /// </summary>
        private CloudBlobContainer container;

        /// <summary>
        /// Stores the blob's parent <see cref="CloudBlobDirectory"/>.
        /// </summary>
        private CloudBlobDirectory parent;

        /// <summary>
        /// Stores the blob's attributes.
        /// </summary>
        internal readonly BlobAttributes attributes;

        /// <summary>
        /// Gets the <see cref="CloudBlobClient"/> object that represents the Blob service.
        /// </summary>
        /// <value>A <see cref="CloudBlobClient"/> object.</value>
        public CloudBlobClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets or sets the minimum number of bytes to buffer when reading from a blob stream.
        /// </summary>
        /// <value>The minimum number of bytes to buffer, being at least 16 KB.</value>
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
        /// Gets the blob's system properties.
        /// </summary>
        /// <value>A <see cref="BlobProperties"/> object.</value>
        public BlobProperties Properties
        {
            get
            {
                return this.attributes.Properties;
            }
        }

        /// <summary>
        /// Gets the user-defined metadata for the blob.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> object containing the blob's metadata as a collection of name-value pairs.</value>
        public IDictionary<string, string> Metadata
        {
            get
            {
                return this.attributes.Metadata;
            }
        }

        /// <summary>
        /// Gets the blob's URI for the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> specifying the absolute URI to the blob at the primary location.</value>
        public Uri Uri
        {
            get
            {
                return this.attributes.Uri;
            }
        }

        /// <summary>
        /// Gets the blob's URIs for both the primary and secondary locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing the blob's URIs for both the primary and secondary locations.</value>
        public StorageUri StorageUri
        {
            get
            {
                return this.attributes.StorageUri;
            }
        }

        /// <summary>
        /// Gets the date and time that the blob snapshot was taken, if this blob is a snapshot.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> containing the blob's snapshot time if the blob is a snapshot; otherwise, <c>null</c>.</value>
        /// <remarks>
        /// If the blob is not a snapshot, the value of this property is <c>null</c>.
        /// </remarks>
        public DateTimeOffset? SnapshotTime
        {
            get
            {
                return this.attributes.SnapshotTime;
            }

            private set
            {
                this.attributes.SnapshotTime = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this blob is a snapshot.
        /// </summary>
        /// <value><c>true</c> if this blob is a snapshot; otherwise, <c>false</c>.</value>
        public bool IsSnapshot
        {
            get
            {
                return this.SnapshotTime.HasValue;
            }
        }

        /// <summary>
        /// Gets the absolute URI to the blob, including query string information if the blob is a snapshot.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> specifying the absolute URI to the blob, including snapshot query information if the blob is a snapshot.</value>
        public Uri SnapshotQualifiedUri
        {
            get
            {
                if (this.SnapshotTime.HasValue)
                {
                    UriQueryBuilder builder = new UriQueryBuilder();
                    builder.Add("snapshot", Request.ConvertDateTimeToSnapshotString(this.SnapshotTime.Value));
                    return builder.AddToUri(this.Uri);
                }
                else
                {
                    return this.Uri;
                }
            }
        }

        /// <summary>
        /// Gets the blob's URI for both the primary and secondary locations, including query string information if the blob is a snapshot.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing the blob's URIs for both the primary and secondary locations, 
        /// including snapshot query information if the blob is a snapshot.</value>
        public StorageUri SnapshotQualifiedStorageUri
        {
            get
            {
                if (this.SnapshotTime.HasValue)
                {
                    UriQueryBuilder builder = new UriQueryBuilder();
                    builder.Add("snapshot", Request.ConvertDateTimeToSnapshotString(this.SnapshotTime.Value));
                    return builder.AddToUri(this.StorageUri);
                }
                else
                {
                    return this.StorageUri;
                }
            }
        }

        /// <summary>
        /// Gets the state of the most recent or pending copy operation.
        /// </summary>
        /// <value>A <see cref="CopyState"/> object containing the copy state, or <c>null</c> if there is no copy state for the blob.</value>
        public CopyState CopyState
        {
            get
            {
                return this.attributes.CopyState;
            }
        }

        /// <summary>
        /// Gets the name of the blob.
        /// </summary>
        /// <value>A string containing the name of the blob.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a <see cref="CloudBlobContainer"/> object representing the blob's container.
        /// </summary>
        /// <value>A <see cref="CloudBlobContainer"/> object.</value>
        public CloudBlobContainer Container
        {
            get
            {
                if (this.container == null)
                {
                    this.container = this.ServiceClient.GetContainerReference(
                        NavigationHelper.GetContainerName(this.Uri, this.ServiceClient.UsePathStyleUris));
                }

                return this.container;
            }
        }

        /// <summary>
        /// Gets the <see cref="CloudBlobDirectory"/> object representing the virtual parent directory for the blob.
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
        /// Gets the type of the blob.
        /// </summary>
        /// <value>A <see cref="BlobType"/> enumeration value.</value>
        public BlobType BlobType
        {
            get
            {
                return this.Properties.BlobType;
            }

            internal set
            {
                this.Properties.BlobType = value;
            }
        }

        /// <summary>
        /// Returns a shared access signature for the blob.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy)
        {
            return this.GetSharedAccessSignature(policy, null /* headers */, null /* groupPolicyIdentifier */);
        }

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
        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, string groupPolicyIdentifier)
        {
            return this.GetSharedAccessSignature(policy, null /* headers */, groupPolicyIdentifier);
        }

        /// <summary>
        /// Returns a shared access signature for the blob.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="headers">A <see cref="SharedAccessBlobHeaders"/> object specifying optional header values to set for a blob accessed with this SAS.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers)
        {
            return this.GetSharedAccessSignature(policy, headers, null /* groupPolicyIdentifier */);
        }

        /// <summary>
        /// Returns a shared access signature for the blob.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="headers">A <see cref="SharedAccessBlobHeaders"/> object specifying optional header values to set for a blob accessed with this SAS.</param>
        /// <param name="groupPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string groupPolicyIdentifier)
        {
            return this.GetSharedAccessSignature(policy, headers, groupPolicyIdentifier, null, null);
        }

        /// <summary>
        /// Returns a shared access signature for the blob.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessBlobPolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="headers">A <see cref="SharedAccessBlobHeaders"/> object specifying optional header values to set for a blob accessed with this SAS.</param>
        /// <param name="groupPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <param name="protocols">The allowed protocols (https only, or http and https). Null if you don't want to restrict protocol.</param>
        /// <param name="ipAddressOrRange">The allowed IP address or IP address range. Null if you don't want to restrict based on IP address.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
        {
            if (!this.ServiceClient.Credentials.IsSharedKey)
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, SR.CannotCreateSASWithoutAccountKey);
                throw new InvalidOperationException(errorMessage);
            }

            string resourceName = this.GetCanonicalName(true /* ignoreSnapshotTime */);
            StorageAccountKey accountKey = this.ServiceClient.Credentials.Key;
            string signature = SharedAccessSignatureHelper.GetHash(policy, headers, groupPolicyIdentifier, resourceName, Constants.HeaderConstants.TargetStorageVersion, protocols, ipAddressOrRange, accountKey.KeyValue);

            // Future resource type changes from "c" => "container"
            UriQueryBuilder builder = SharedAccessSignatureHelper.GetSignature(policy, headers, groupPolicyIdentifier, "b", signature, accountKey.KeyName, Constants.HeaderConstants.TargetStorageVersion, protocols, ipAddressOrRange);

            return builder.ToString();
        }

        /// <summary>
        /// Gets the canonical name of the blob, formatted as blob/&lt;account-name&gt;/&lt;container-name&gt;/&lt;blob-name&gt;.
        /// If <c>ignoreSnapshotTime</c> is <c>false</c> and this blob is a snapshot, the canonical name is augmented with a
        /// query of the form ?snapshot=&lt;snapshot-time&gt;.
        /// <para>This is used by both Shared Access and Copy blob operations.</para>
        /// </summary>
        /// <param name="ignoreSnapshotTime">Indicates if the snapshot time is ignored.</param>
        /// <returns>The canonical name of the blob.</returns>
        private string GetCanonicalName(bool ignoreSnapshotTime)
        {
            string accountName = this.ServiceClient.Credentials.AccountName;
            string containerName = this.Container.Name;
 
            // Replace \ with / for uri compatibility when running under .net 4.5. 
            string blobName = this.Name.Replace('\\', '/');
            string canonicalNameFormat = "/{0}/{1}/{2}/{3}";
            string canonicalName = string.Format(CultureInfo.InvariantCulture, canonicalNameFormat, SR.Blob, accountName, containerName, blobName);
 
             if (!ignoreSnapshotTime && this.SnapshotTime != null)
             {
                canonicalName += "?snapshot=" + Request.ConvertDateTimeToSnapshotString(this.SnapshotTime.Value);
            }

            return canonicalName;
        }

        /// <summary>
        /// Parse URI for SAS (Shared Access Signature) and snapshot information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="credentials">The credentials to use.</param>
        private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
        {
            StorageCredentials parsedCredentials;
            DateTimeOffset? parsedSnapshot;
            this.attributes.StorageUri = NavigationHelper.ParseBlobQueryAndVerify(address, out parsedCredentials, out parsedSnapshot);

            if (parsedCredentials != null && credentials != null)
            {
                string error = string.Format(CultureInfo.CurrentCulture, SR.MultipleCredentialsProvided);
                throw new ArgumentException(error);
            }

            if (parsedSnapshot.HasValue && this.SnapshotTime.HasValue && !parsedSnapshot.Value.Equals(this.SnapshotTime.Value))
            {
                string error = string.Format(CultureInfo.CurrentCulture, SR.MultipleSnapshotTimesProvided, parsedSnapshot, this.SnapshotTime);
                throw new ArgumentException(error);
            }

            if (parsedSnapshot.HasValue)
            {
                this.SnapshotTime = parsedSnapshot;
            }

            if (this.ServiceClient == null)
            {
                this.ServiceClient = new CloudBlobClient(NavigationHelper.GetServiceClientBaseAddress(this.StorageUri, null /* usePathStyleUris */), credentials ?? parsedCredentials);
            }

            this.Name = NavigationHelper.GetBlobName(this.Uri, this.ServiceClient.UsePathStyleUris);
        }
    }
}