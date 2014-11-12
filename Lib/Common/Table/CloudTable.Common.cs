// -----------------------------------------------------------------------------------------
// <copyright file="CloudTable.Common.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// Represents a Windows Azure table.
    /// </summary>
    public sealed partial class CloudTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTable"/> class.
        /// </summary>
        /// <param name="tableAddress">A <see cref="System.Uri"/> specifying the absolute URI to the table.</param>
        public CloudTable(Uri tableAddress)
            : this(tableAddress, null /* credentials */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTable"/> class.
        /// </summary>
        /// <param name="tableAbsoluteUri">A <see cref="System.Uri"/> specifying the absolute URI to the table.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudTable(Uri tableAbsoluteUri, StorageCredentials credentials)
            : this(new StorageUri(tableAbsoluteUri), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTable"/> class.
        /// </summary>
        /// <param name="tableAddress">A <see cref="StorageUri"/> containing the absolute URI to the table at both the primary and secondary locations.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
#if WINDOWS_RT || ASPNET_K
        /// <returns>A <see cref="CloudTable"/> object.</returns>
        public static CloudTable Create(StorageUri tableAddress, StorageCredentials credentials)
        {
            return new CloudTable(tableAddress, credentials);
        }

        internal CloudTable(StorageUri tableAddress, StorageCredentials credentials)
#else
        public CloudTable(StorageUri tableAddress, StorageCredentials credentials)
#endif
        {
            this.ParseQueryAndVerify(tableAddress, credentials);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTable"/> class.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="client">The client.</param>
        internal CloudTable(string tableName, CloudTableClient client)
        {
            CommonUtility.AssertNotNull("tableName", tableName);
            CommonUtility.AssertNotNull("client", client);
            this.Name = tableName;
            this.StorageUri = NavigationHelper.AppendPathToUri(client.StorageUri, tableName);
            this.ServiceClient = client;
        }

        /// <summary>
        /// Gets the <see cref="CloudTableClient"/> object that represents the Table service.
        /// </summary>
        /// <value>A <see cref="CloudTableClient"/> object .</value>
        public CloudTableClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>A string containing the name of the table.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the table URI for the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> specifying the absolute URI to the table at the primary location.</value>
        public Uri Uri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the table's URIs for both the primary and secondary locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing the table's URIs for both the primary and secondary locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Returns a shared access signature for the table.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessTablePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the current credentials don't support creating a shared access signature.</exception>
        public string GetSharedAccessSignature(SharedAccessTablePolicy policy)
        {
            return this.GetSharedAccessSignature(
                policy,
                null /* accessPolicyIdentifier */,
                null /* startPartitionKey */,
                null /* startRowKey */,
                null /* endPartitionKey */,
                null /* endRowKey */,
                null /* sasVersion */);
        }

        /// <summary>
        /// Returns a shared access signature for the table.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessTablePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="accessPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the current credentials don't support creating a shared access signature.</exception>
        public string GetSharedAccessSignature(SharedAccessTablePolicy policy, string accessPolicyIdentifier)
        {
            return this.GetSharedAccessSignature(
                policy,
                accessPolicyIdentifier,
                null /* startPartitionKey */,
                null /* startRowKey */,
                null /* endPartitionKey */,
                null /* endRowKey */,
                null /* sasVersion */);
        }

        /// <summary>
        /// Returns a shared access signature for the table.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessTablePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="accessPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <param name="startPartitionKey">A string specifying the start partition key, or <c>null</c>.</param>
        /// <param name="startRowKey">A string specifying the start row key, or <c>null</c>.</param>
        /// <param name="endPartitionKey">A string specifying the end partition key, or <c>null</c>.</param>
        /// <param name="endRowKey">A string specifying the end row key, or <c>null</c>.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the current credentials don't support creating a shared access signature.</exception>
        public string GetSharedAccessSignature(
            SharedAccessTablePolicy policy,
            string accessPolicyIdentifier,
            string startPartitionKey,
            string startRowKey,
            string endPartitionKey,
            string endRowKey)
        {
            return this.GetSharedAccessSignature(
                policy,
                accessPolicyIdentifier,
                startPartitionKey,
                startRowKey,
                endPartitionKey,
                endRowKey,
                null /* sasVersion */);
        }

        /// <summary>
        /// Returns a shared access signature for the table.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessTablePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="accessPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <param name="startPartitionKey">A string specifying the start partition key, or <c>null</c>.</param>
        /// <param name="startRowKey">A string specifying the start row key, or <c>null</c>.</param>
        /// <param name="endPartitionKey">A string specifying the end partition key, or <c>null</c>.</param>
        /// <param name="endRowKey">A string specifying the end row key, or <c>null</c>.</param>
        /// <param name="sasVersion">A string indicating the desired SAS version to use, in storage service version format. Value must be <c>2012-02-12</c> or later.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the current credentials don't support creating a shared access signature.</exception>
        public string GetSharedAccessSignature(
            SharedAccessTablePolicy policy,
            string accessPolicyIdentifier,
            string startPartitionKey,
            string startRowKey,
            string endPartitionKey,
            string endRowKey,
            string sasVersion)
        {
            if (!this.ServiceClient.Credentials.IsSharedKey)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.CannotCreateSASWithoutAccountKey);
                throw new InvalidOperationException(errorMessage);
            }

            string resourceName = this.GetCanonicalName();
            StorageAccountKey accountKey = this.ServiceClient.Credentials.Key;
            string validatedSASVersion = SharedAccessSignatureHelper.ValidateSASVersionString(sasVersion);
         
            string signature = SharedAccessSignatureHelper.GetHash(
                policy,
                accessPolicyIdentifier,
                startPartitionKey,
                startRowKey,
                endPartitionKey,
                endRowKey,
                resourceName,
                validatedSASVersion,
                accountKey.KeyValue);

            UriQueryBuilder builder = SharedAccessSignatureHelper.GetSignature(
                policy,
                this.Name,
                accessPolicyIdentifier,
                startPartitionKey,
                startRowKey,
                endPartitionKey,
                endRowKey,
                signature,
                accountKey.KeyName,
                validatedSASVersion);

            return builder.ToString();
        }

        /// <summary>
        /// Returns the name of the table.
        /// </summary>
        /// <returns>A string containing the name of the table.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Parse URI for SAS (Shared Access Signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="credentials">The credentials to use.</param>
        private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
        {
            StorageCredentials parsedCredentials;
            this.StorageUri = NavigationHelper.ParseQueueTableQueryAndVerify(address, out parsedCredentials);

            if ((parsedCredentials != null) && (credentials != null) && !parsedCredentials.Equals(credentials))
            {
                string error = string.Format(CultureInfo.CurrentCulture, SR.MultipleCredentialsProvided);
                throw new ArgumentException(error);
            }

            this.ServiceClient = new CloudTableClient(NavigationHelper.GetServiceClientBaseAddress(this.StorageUri, null /* usePathStyleUris */), credentials ?? parsedCredentials);
            this.Name = NavigationHelper.GetTableNameFromUri(this.Uri, this.ServiceClient.UsePathStyleUris);
        }

        /// <summary>
        /// Gets the canonical name of the table, formatted as /&lt;account-name&gt;/&lt;table-name&gt;.
        /// </summary>
        /// <returns>The canonical name of the table.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower", Justification = "ToLower(CultureInfo) is not present in RT and ToLowerInvariant() also violates FxCop")]
        private string GetCanonicalName()
        {
            string accountName = this.ServiceClient.Credentials.AccountName;
            string tableNameLowerCase = this.Name.ToLower();

            return string.Format(CultureInfo.InvariantCulture, "/{0}/{1}", accountName, tableNameLowerCase);
        }
    }
}
