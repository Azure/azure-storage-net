//-----------------------------------------------------------------------
// <copyright file="CloudFileClient.Common.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using System;

    /// <summary>
    /// Provides a client-side logical representation of the Microsoft Azure File service. This client is used to configure and execute requests against the File service.
    /// </summary>
    /// <remarks>The service client encapsulates the base URI for the File service. If the service client will be used for authenticated access, it also encapsulates 
    /// the credentials for accessing the storage account.</remarks>
    public partial class CloudFileClient
    {
        private AuthenticationScheme authenticationScheme;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileClient"/> class using the specified File service endpoint
        /// and account credentials.
        /// </summary>
        /// <param name="baseUri">The File service endpoint to use to create the client.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudFileClient(Uri baseUri, StorageCredentials credentials)
            : this(new StorageUri(baseUri), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileClient"/> class using the specified File service endpoint
        /// and account credentials.
        /// </summary>
        /// <param name="storageUri">The File service endpoint to use to create the client.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudFileClient(StorageUri storageUri, StorageCredentials credentials)
        {
            this.StorageUri = storageUri;
            this.Credentials = credentials ?? new StorageCredentials();
            this.DefaultRequestOptions = 
                new FileRequestOptions() 
                { 
                    RetryPolicy = new ExponentialRetry(),
                    LocationMode = FileRequestOptions.BaseDefaultRequestOptions.LocationMode,
                    ParallelOperationThreadCount = FileRequestOptions.BaseDefaultRequestOptions.ParallelOperationThreadCount
                };
            this.AuthenticationScheme = AuthenticationScheme.SharedKey;
            this.UsePathStyleUris = CommonUtility.UsePathStyleAddressing(this.BaseUri);
        }

        /// <summary>
        /// Gets or sets a buffer manager that implements the <see cref="IBufferManager"/> interface, 
        /// specifying a buffer pool for use with operations against the File service client.
        /// </summary>
        public IBufferManager BufferManager { get; set; }

        /// <summary>
        /// Gets the account credentials used to create the File service client.
        /// </summary>
        /// <value>The account credentials.</value>
        public StorageCredentials Credentials { get; private set; }

        /// <summary>
        /// Gets the base URI for the File service client.
        /// </summary>
        /// <value>The base URI used to construct the File service client.</value>
        public Uri BaseUri
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
        /// Gets or sets the default request options for requests made via the File service client.
        /// </summary>
        /// <value>A <see cref="FileRequestOptions"/> object.</value>
        public FileRequestOptions DefaultRequestOptions { get; set; }

        /// <summary>
        /// Gets a value indicating whether the service client is used with Path style or Host style.
        /// </summary>
        /// <value>Is <c>true</c> if use path style URIs; otherwise, <c>false</c>.</value>
        internal bool UsePathStyleUris { get; private set; }

        /// <summary>
        /// Returns a reference to a <see cref="CloudFileShare"/> object with the specified name.
        /// </summary>
        /// <param name="shareName">A string containing the name of the share.</param>
        /// <returns>A reference to a share.</returns>
        public CloudFileShare GetShareReference(string shareName)
        {
            CommonUtility.AssertNotNullOrEmpty("shareName", shareName);
            return new CloudFileShare(shareName, this);
        }

        internal ICanonicalizer GetCanonicalizer()
        {
            if (this.AuthenticationScheme == AuthenticationScheme.SharedKeyLite)
            {
                return SharedKeyLiteCanonicalizer.Instance;
            }

            return SharedKeyCanonicalizer.Instance;
        }
    }
}
