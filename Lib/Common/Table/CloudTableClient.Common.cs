// -----------------------------------------------------------------------------------------
// <copyright file="CloudTableClient.Common.cs" company="Microsoft">
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
// -----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Table
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using System;

    /// <summary>
    /// Provides a client-side logical representation of the Microsoft Azure Table service. This client is used to configure and execute requests against the Table service.
    /// </summary>
    /// <remarks>The CloudTableClient object encapsulates the base URI for the Table service. If the service client will be used for authenticated access, 
    /// it also encapsulates the credentials for accessing the storage account.</remarks>    
    public partial class CloudTableClient
    {
        private AuthenticationScheme authenticationScheme;

        private string accountName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTableClient"/> class using the specified Table service endpoint
        /// and account credentials.
        /// </summary>
        /// <param name="baseUri">A <see cref="System.Uri"/> object containing the Table service endpoint to use to create the client.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudTableClient(Uri baseUri, StorageCredentials credentials)
            : this(new StorageUri(baseUri), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTableClient"/> class using the specified Table service endpoint
        /// and account credentials.
        /// </summary>
        /// <param name="storageUri">A <see cref="StorageUri"/> object containing the Table service endpoint to use to create the client.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudTableClient(StorageUri storageUri, StorageCredentials credentials)
        {
            this.StorageUri = storageUri;
            this.Credentials = credentials ?? new StorageCredentials();
            this.DefaultRequestOptions =
                new TableRequestOptions(TableRequestOptions.BaseDefaultRequestOptions) 
                { 
                    RetryPolicy = new ExponentialRetry()
                };
            this.AuthenticationScheme = AuthenticationScheme.SharedKey;
            this.UsePathStyleUris = CommonUtility.UsePathStyleAddressing(this.BaseUri);

            if (!this.Credentials.IsSharedKey)
            {
                this.AccountName = NavigationHelper.GetAccountNameFromUri(this.BaseUri, this.UsePathStyleUris);
            }
        }

        /// <summary>
        /// Gets or sets a buffer manager that implements the <see cref="IBufferManager"/> interface, 
        /// specifying a buffer pool for use with operations against the Table service client.
        /// </summary>
        /// <value>An object of type <see cref="IBufferManager"/>.</value>
        public IBufferManager BufferManager { get; set; }

        /// <summary>
        /// Gets the account credentials used to create the Table service client.
        /// </summary>
        /// <value>A <see cref="StorageCredentials"/> object.</value>
        public StorageCredentials Credentials { get; private set; }

        /// <summary>
        /// Gets the base URI for the Table service client at the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> object containing the base URI used to construct the Table service client at the primary location.</value>
        public Uri BaseUri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the Table service endpoints for both the primary and secondary locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing Table service URIs for both the primary and secondary locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Gets or sets the default request options for requests made via the Table service client.
        /// </summary>
        /// <value>A <see cref="TableRequestOptions"/> object.</value>
        public TableRequestOptions DefaultRequestOptions { get; set; }

        /// <summary>
        /// Gets a value indicating whether the service client is used with Path style or Host style.
        /// </summary>
        /// <value>Is <c>true</c> if use path style URIs; otherwise, <c>false</c>.</value>
        internal bool UsePathStyleUris { get; private set; }

        /// <summary>
        /// Gets the associated account name for the client.
        /// </summary>
        /// <value>The account name.</value>
        internal string AccountName
        {
            get
            {
                return this.accountName ?? this.Credentials.AccountName;
            }

            private set
            {
                this.accountName = value;
            }
        }

        /// <summary>
        /// Gets a reference to the specified table.
        /// </summary>
        /// <param name="tableName">A string containing the name of the table.</param>
        /// <returns>A <see cref="CloudTable"/> object.</returns>
        public CloudTable GetTableReference(string tableName)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", tableName);
            return new CloudTable(tableName, this);
        }

        internal ICanonicalizer GetCanonicalizer()
        {
            if (this.AuthenticationScheme == AuthenticationScheme.SharedKeyLite)
            {
                return SharedKeyLiteTableCanonicalizer.Instance;
            }

            return SharedKeyTableCanonicalizer.Instance;
        }
    }
}
