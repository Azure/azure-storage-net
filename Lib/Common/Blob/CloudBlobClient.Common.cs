//-----------------------------------------------------------------------
// <copyright file="CloudBlobClient.Common.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Provides a client-side logical representation of the Windows Azure Blob service. This client is used to configure and execute requests against the Blob service.
    /// </summary>
    /// <remarks>The service client encapsulates the endpoint or endpoints for the Blob service. If the service client will be used for authenticated access, it also encapsulates 
    /// the credentials for accessing the storage account.</remarks>
    public sealed partial class CloudBlobClient
    {
        /// <summary>
        /// Stores the default delimiter.
        /// </summary>
        private string defaultDelimiter;

        private AuthenticationScheme authenticationScheme;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobClient"/> class using the specified Blob service endpoint
        /// and anonymous credentials.
        /// </summary>
        /// <param name="baseUri">A <see cref="System.Uri"/> object containing the Blob service endpoint to use to create the client.</param>
        public CloudBlobClient(Uri baseUri)
            : this(baseUri, null /* credentials */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobClient"/> class using the specified Blob service endpoint
        /// and account credentials.
        /// </summary>
        /// <param name="baseUri">A <see cref="System.Uri"/> object containing the Blob service endpoint to use to create the client.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudBlobClient(Uri baseUri, StorageCredentials credentials)
            : this(new StorageUri(baseUri), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudBlobClient"/> class using the specified Blob service endpoint
        /// and account credentials.
        /// </summary>
        /// <param name="storageUri">A <see cref="StorageUri"/> object containing the Blob service endpoint to use to create the client.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
#if WINDOWS_RT || ASPNET_K
        /// <returns>A <see cref="CloudBlobClient"/> object.</returns>
        public static CloudBlobClient Create(StorageUri storageUri, StorageCredentials credentials)
        {
            return new CloudBlobClient(storageUri, credentials);
        }

        internal CloudBlobClient(StorageUri storageUri, StorageCredentials credentials)
#else
        public CloudBlobClient(StorageUri storageUri, StorageCredentials credentials)
#endif
        {
            this.StorageUri = storageUri;
            this.Credentials = credentials ?? new StorageCredentials();
            this.DefaultRequestOptions = new BlobRequestOptions();
            this.DefaultRequestOptions.RetryPolicy = new ExponentialRetry();
            this.DefaultRequestOptions.LocationMode = RetryPolicies.LocationMode.PrimaryOnly;
            this.DefaultRequestOptions.SingleBlobUploadThresholdInBytes = Constants.MaxSingleUploadBlobSize / 2;
            this.DefaultRequestOptions.ParallelOperationThreadCount = 1;
            this.DefaultDelimiter = NavigationHelper.Slash;
            this.AuthenticationScheme = AuthenticationScheme.SharedKey;
            this.UsePathStyleUris = CommonUtility.UsePathStyleAddressing(this.BaseUri);
        }

        /// <summary>
        /// Gets or sets a buffer manager that implements the <see cref="IBufferManager"/> interface,
        /// specifying a buffer pool for use with operations against the Blob service client.
        /// </summary>
        /// <value>An object of type <see cref="IBufferManager"/>.</value>
        public IBufferManager BufferManager { get; set; }

        /// <summary>
        /// Gets the account credentials used to create the Blob service client.
        /// </summary>
        /// <value>A <see cref="StorageCredentials"/> object.</value>
        public StorageCredentials Credentials { get; private set; }

        /// <summary>
        /// Gets the base URI for the Blob service client at the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> object containing the base URI used to construct the Blob service client at the primary location.</value>
        public Uri BaseUri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the Blob service endpoints for both the primary and secondary locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing Blob service URIs for both the primary and secondary locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Gets or sets the default request options for requests made via the Blob service client.
        /// </summary>
        /// <value>A <see cref="BlobRequestOptions"/> object.</value>
        public BlobRequestOptions DefaultRequestOptions { get; set; }

        /// <summary>
        /// Gets or sets the default retry policy for requests made via the Blob service client.
        /// </summary>
        /// <value>An object of type <see cref="IRetryPolicy"/>.</value>
        [Obsolete("Use DefaultRequestOptions.RetryPolicy.")]
        public IRetryPolicy RetryPolicy
        {
            get
            {
                return this.DefaultRequestOptions.RetryPolicy;
            }

            set
            {
                this.DefaultRequestOptions.RetryPolicy = value;
            }
        }

        /// <summary>
        /// Gets or sets the default location mode for requests made via the Blob service client.
        /// </summary>
        /// <value>A <see cref="Microsoft.WindowsAzure.Storage.RetryPolicies.LocationMode"/> enumeration value.</value>
        [Obsolete("Use DefaultRequestOptions.LocationMode.")]
        public LocationMode? LocationMode
        {
            get
            {
                return this.DefaultRequestOptions.LocationMode;
            }

            set
            {
                this.DefaultRequestOptions.LocationMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the default server timeout for requests made via the Blob service client.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the server timeout interval.</value>
        [Obsolete("Use DefaultRequestOptions.ServerTimeout.")]
        public TimeSpan? ServerTimeout
        {
            get
            {
                return this.DefaultRequestOptions.ServerTimeout;
            }

            set
            {
                this.DefaultRequestOptions.ServerTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum execution time across all potential retries.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the maximum execution time across all potential retries.</value>
        [Obsolete("Use DefaultRequestOptions.MaximumExecutionTime.")]
        public TimeSpan? MaximumExecutionTime
        {
            get
            {
                return this.DefaultRequestOptions.MaximumExecutionTime;
            }

            set
            {
                this.DefaultRequestOptions.MaximumExecutionTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the default delimiter that may be used to create a virtual directory structure of blobs.
        /// </summary>
        /// <value>A string containing the default delimiter for the Blob service.</value>
        public string DefaultDelimiter
        {
            get
            {
                return this.defaultDelimiter;
            }

            set
            {
                CommonUtility.AssertNotNullOrEmpty("DefaultDelimiter", value);
                CommonUtility.AssertNotNullOrEmpty("DefaultDelimiter", value);
                if (value == "\\")
                {
                    throw new ArgumentException(SR.InvalidDelimiter);
                }       

                this.defaultDelimiter = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum size of a blob in bytes that may be uploaded as a single blob. 
        /// </summary>
        /// <value>A long containing the maximum size of a blob, in bytes, that may be uploaded as a single blob,
        /// ranging from between 1 and 64 MB inclusive.</value>
        [Obsolete("Use DefaultRequestOptions.SingleBlobUploadThresholdInBytes.")]
        public long? SingleBlobUploadThresholdInBytes
        {
            get
            {
                return this.DefaultRequestOptions.SingleBlobUploadThresholdInBytes;
            }

            set
            {
                this.DefaultRequestOptions.SingleBlobUploadThresholdInBytes = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of blocks that may be simultaneously uploaded when uploading a blob that is greater than 
        /// the value specified by the <see cref="SingleBlobUploadThresholdInBytes"/> property in size.
        /// </summary>
        /// <value>An integer indicating the number of parallel operations that may proceed.</value>
        [Obsolete("Use DefaultRequestOptions.ParallelOperationThreadCount.")]
        public int? ParallelOperationThreadCount
        {
            get
            {
                return this.DefaultRequestOptions.ParallelOperationThreadCount;
            }

            set
            {
                this.DefaultRequestOptions.ParallelOperationThreadCount = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the service client is used with Path style or Host style.
        /// </summary>
        /// <value>Is <c>true</c> if use path style URIs; otherwise, <c>false</c>.</value>
        internal bool UsePathStyleUris { get; private set; }

        /// <summary>
        /// Returns a reference to the root container.
        /// </summary>
        /// <returns>A <see cref="CloudBlobContainer"/> object.</returns>
        /// <remarks>Note that the root container must be explicitly created, if it does not already exist, before
        /// you can read from it or write to it.</remarks>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Reviewed")]
        public CloudBlobContainer GetRootContainerReference()
        {
            return new CloudBlobContainer(NavigationHelper.RootContainerName, this);
        }

        /// <summary>
        /// Returns a reference to a <see cref="CloudBlobContainer"/> object with the specified name.
        /// </summary>
        /// <param name="containerName">A string containing the name of the container.</param>
        /// <returns>A <see cref="CloudBlobContainer"/> object.</returns>
        public CloudBlobContainer GetContainerReference(string containerName)
        {
            CommonUtility.AssertNotNullOrEmpty("containerName", containerName);
            return new CloudBlobContainer(containerName, this);
        }

        private ICanonicalizer GetCanonicalizer()
        {
            if (this.AuthenticationScheme == AuthenticationScheme.SharedKeyLite)
            {
                return SharedKeyLiteCanonicalizer.Instance;
            }

            return SharedKeyCanonicalizer.Instance;
        }

        /// <summary>
        /// Parses the user prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="listingPrefix">The listing prefix.</param>
        private static void ParseUserPrefix(string prefix, out string containerName, out string listingPrefix)
        {
            CommonUtility.AssertNotNull("prefix", prefix);
            containerName = null;
            listingPrefix = null;

            string[] prefixParts = prefix.Split(NavigationHelper.SlashAsSplitOptions, 2, StringSplitOptions.None);
            if (prefixParts.Length == 1)
            {
                // No slash in prefix
                // Case abc => container = $root, prefix=abc; Listing with prefix at root
                listingPrefix = prefixParts[0];
            }
            else
            {
                // Case "/abc" => container=$root, prefix=abc; Listing with prefix at root
                // Case "abc/" => container=abc, no prefix; Listing all under a container
                // Case "abc/def" => container = abc, prefix = def; Listing with prefix under a container
                // Case "/" => container=$root, no prefix; Listing all under root
                containerName = prefixParts[0];
                listingPrefix = prefixParts[1];
            }

            if (string.IsNullOrEmpty(containerName))
            {
                containerName = NavigationHelper.RootContainerName;
            }

            if (string.IsNullOrEmpty(listingPrefix))
            {
                listingPrefix = null;
            }
        }
    }
}
