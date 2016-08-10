// -----------------------------------------------------------------------------------------
// <copyright file="CloudQueue.Common.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Queue
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Queue.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// Represents a queue in the Microsoft Azure Queue service.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Reviewed.")]
    public partial class CloudQueue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueue"/> class.
        /// </summary>
        /// <param name="queueAddress">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        public CloudQueue(Uri queueAddress)
            : this(queueAddress, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueue"/> class.
        /// </summary>
        /// <param name="queueAddress">A <see cref="System.Uri"/> specifying the absolute URI to the queue.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudQueue(Uri queueAddress, StorageCredentials credentials)
            : this(new StorageUri(queueAddress), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueue"/> class.
        /// </summary>
        /// <param name="queueAddress">A <see cref="StorageUri"/> containing the absolute URI to the queue at both the primary and secondary locations.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        /// <returns>A <see cref="CloudQueue"/> object.</returns>
        public CloudQueue(StorageUri queueAddress, StorageCredentials credentials)
        {
            this.ParseQueryAndVerify(queueAddress, credentials);
            this.Metadata = new Dictionary<string, string>();
            this.EncodeMessage = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueue"/> class.
        /// </summary>
        /// <param name="queueName">The queue name.</param>
        /// <param name="serviceClient">A client object that specifies the endpoint for the Queue service.</param>
        internal CloudQueue(string queueName, CloudQueueClient serviceClient)
            : this(new Dictionary<string, string>(), queueName, serviceClient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueue"/> class.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="serviceClient">A client object that specifies the endpoint for the Queue service.</param>
        internal CloudQueue(IDictionary<string, string> metadata, string queueName, CloudQueueClient serviceClient)
        {
            this.StorageUri = NavigationHelper.AppendPathToUri(serviceClient.StorageUri, queueName);
            this.ServiceClient = serviceClient;
            this.Name = queueName;
            this.Metadata = metadata;
            this.EncodeMessage = true;
        }

        /// <summary>
        /// Gets the <see cref="CloudQueueClient"/> object that represents the Queue service.
        /// </summary>
        /// <value>A <see cref="CloudQueueClient"/> object.</value>
        public CloudQueueClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets the queue URI for the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> specifying the absolute URI to the queue at the primary location.</value>
        public Uri Uri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the queue URIs for both the primary and secondary locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing the queue's URIs for both the primary and secondary locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Gets the name of the queue.
        /// </summary>
        /// <value>A string containing the name of the queue.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the approximate message count for the queue.
        /// </summary>
        /// <value>The approximate message count.</value>
        public int? ApproximateMessageCount { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to apply base64 encoding when adding or retrieving messages.
        /// </summary>
        /// <value><c>true</c> to encode messages; otherwise, <c>false</c>. The default value is <c>true</c>.</value>
        public bool EncodeMessage { get; set; }

        /// <summary>
        /// Gets the queue's metadata.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> object containing the queue's metadata.</value>
        public IDictionary<string, string> Metadata { get; private set; }

        /// <summary>
        /// Uri for the messages.
        /// </summary>
        private StorageUri messageRequestAddress;

        /// <summary>
        /// Gets the Uri for general message operations.
        /// </summary>
        internal StorageUri GetMessageRequestAddress()
        {
            if (this.messageRequestAddress == null)
            {
                this.messageRequestAddress = NavigationHelper.AppendPathToUri(this.StorageUri, Constants.Messages);
            }

            return this.messageRequestAddress;
        }

        /// <summary>
        /// Gets the individual message address.
        /// </summary>
        /// <param name="messageId">A string specifying the message ID.</param>
        /// <returns>The URI of the message.</returns>
        internal StorageUri GetIndividualMessageAddress(string messageId)
        {
            return NavigationHelper.AppendPathToUri(this.GetMessageRequestAddress(), messageId);
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

            if (parsedCredentials != null && credentials != null)
            {
                string error = string.Format(CultureInfo.CurrentCulture, SR.MultipleCredentialsProvided);
                throw new ArgumentException(error);
            }

            this.ServiceClient = new CloudQueueClient(NavigationHelper.GetServiceClientBaseAddress(this.StorageUri, null /* usePathStyleUris */), credentials ?? parsedCredentials);
            this.Name = NavigationHelper.GetQueueNameFromUri(this.Uri, this.ServiceClient.UsePathStyleUris);
        }

        /// <summary>
        /// Returns the canonical name for shared access.
        /// </summary>
        /// <returns>The canonical name.</returns>
        private string GetCanonicalName()
        {
            string accountName = this.ServiceClient.Credentials.AccountName;
            string queueName = this.Name;
            string canonicalNameFormat = "/{0}/{1}/{2}";
            
            return string.Format(CultureInfo.InvariantCulture, canonicalNameFormat, SR.Queue, accountName, queueName);
       }

        /// <summary>
        /// Selects the get message response.
        /// </summary>
        /// <param name="protocolMessage">The protocol message.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>The parsed message.</returns>
        private CloudQueueMessage SelectGetMessageResponse(QueueMessage protocolMessage, QueueRequestOptions options = null)
        {
            CloudQueueMessage message = this.SelectPeekMessageResponse(protocolMessage, options);
            message.PopReceipt = protocolMessage.PopReceipt;

            if (protocolMessage.NextVisibleTime.HasValue)
            {
                message.NextVisibleTime = protocolMessage.NextVisibleTime.Value;
            }

            return message;
        }

        /// <summary>
        /// Selects the peek message response.
        /// </summary>
        /// <param name="protocolMessage">The protocol message.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>The parsed message.</returns>
        private CloudQueueMessage SelectPeekMessageResponse(QueueMessage protocolMessage, QueueRequestOptions options = null)
        {
            CloudQueueMessage message = null;
            byte[] dest = null;
#if !(WINDOWS_RT || NETCORE)
            if (options != null && options.EncryptionPolicy != null)
            {
                // If EncryptionPolicy is set, decrypt the message and set it.
                dest = options.EncryptionPolicy.DecryptMessage(protocolMessage.Text, options.RequireEncryption);
            }
#endif

            if (this.EncodeMessage)
            {
                if (dest!= null)
                {
                    protocolMessage.Text = Convert.ToBase64String(dest, 0, dest.Length);
                }

                // if EncodeMessage is true, we assume the string returned from server is Base64 encoding of original message;
                // if this is not true, exception will likely be thrown.
                // it is user's responsibility to make sure EncodeMessage setting matches the queue that is being read.
                message = new CloudQueueMessage(protocolMessage.Text, true);
            }
            else
            {
#if !(WINDOWS_RT || NETCORE)
                if (dest != null)
                {
                    message = new CloudQueueMessage(dest);
                }
                else
#endif
                {
                    message = new CloudQueueMessage(protocolMessage.Text);
                }
            }

            message.Id = protocolMessage.Id;
            message.InsertionTime = protocolMessage.InsertionTime;
            message.ExpirationTime = protocolMessage.ExpirationTime;
            message.DequeueCount = protocolMessage.DequeueCount;

            // PopReceipt and TimeNextVisible are not returned during peek
            return message;
        }

        /// <summary>
        /// Returns a shared access signature for the queue.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessQueuePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessQueuePolicy policy)
        {
            return this.GetSharedAccessSignature(policy, null /* accessPolicyIdentifier */);
        }

        /// <summary>
        /// Returns a shared access signature for the queue.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessQueuePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="accessPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessQueuePolicy policy, string accessPolicyIdentifier)
        {
            return this.GetSharedAccessSignature(policy, accessPolicyIdentifier, null, null);
        }

        /// <summary>
        /// Returns a shared access signature for the queue.
        /// </summary>
        /// <param name="policy">A <see cref="SharedAccessQueuePolicy"/> object specifying the access policy for the shared access signature.</param>
        /// <param name="accessPolicyIdentifier">A string identifying a stored access policy.</param>
        /// <param name="protocols">The allowed protocols (https only, or http and https). Null if you don't want to restrict protocol.</param>
        /// <param name="ipAddressOrRange">The allowed IP address or IP address range. Null if you don't want to restrict based on IP address.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        public string GetSharedAccessSignature(SharedAccessQueuePolicy policy, string accessPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
        {
            if (!this.ServiceClient.Credentials.IsSharedKey)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.CannotCreateSASWithoutAccountKey);
                throw new InvalidOperationException(errorMessage);
            }

            string resourceName = this.GetCanonicalName();
            StorageAccountKey accountKey = this.ServiceClient.Credentials.Key;

            string signature = SharedAccessSignatureHelper.GetHash(
                policy,
                accessPolicyIdentifier,
                resourceName,
                Constants.HeaderConstants.TargetStorageVersion,
                protocols,
                ipAddressOrRange,
                accountKey.KeyValue);

            string accountKeyName = accountKey.KeyName;

            UriQueryBuilder builder = SharedAccessSignatureHelper.GetSignature(
                policy,
                accessPolicyIdentifier,
                signature,
                accountKeyName,
                Constants.HeaderConstants.TargetStorageVersion,
                protocols,
                ipAddressOrRange);

            return builder.ToString();
        }
    }
}
