// <copyright file="QueueEncryptionPolicy.cs" company="Microsoft">
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
    using Microsoft.Azure.KeyVault.Core;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Represents an encryption policy for performing envelope encryption/decryption of messages in Azure queue.
    /// </summary>
    public sealed class QueueEncryptionPolicy
    {
        /// <summary>
        /// An object of type <see cref="IKey"/> that is used to wrap/unwrap the content key during encryption.
        /// </summary>
        public IKey Key { get; private set; }

        /// <summary>
        /// Gets or sets the key resolver used to select the correct key for decrypting existing queue messages.
        /// </summary>
        /// <value>A resolver that returns an <see cref="IKeyResolver"/>, given a key ID.</value>
        public IKeyResolver KeyResolver { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueEncryptionPolicy"/> class with the specified key and resolver.
        /// </summary>
        /// <param name="key">An object of type <see cref="IKey"/> that is used to wrap/unwrap the content encryption key.</param>
        /// <param name="keyResolver">The key resolver used to select the correct key for decrypting existing queue messages.</param>
        /// <remarks>If the generated policy is to be used for encryption, users are expected to provide a key at the minimum.
        /// The absence of key will cause an exception to be thrown during encryption.<br/>
        /// If the generated policy is intended to be used for decryption, users can provide a key resolver. The client library will:<br/>
        /// 1. Invoke the key resolver, if specified, to get the key.<br/>
        /// 2. If resolver is not specified but a key is specified, the client library will match the key ID against the key and use the key.</remarks> 
        public QueueEncryptionPolicy(IKey key, IKeyResolver keyResolver)
        {
            this.Key = key;
            this.KeyResolver = keyResolver;
        }

        /// <summary>
        /// Return an encrypted base64 encoded message along with encryption related metadata given a plain text message.
        /// </summary>
        /// <param name="inputMessage">The input message in bytes.</param>
        /// <returns>The encrypted message that will be uploaded to the service.</returns>
        internal string EncryptMessage(byte[] inputMessage)
        {
            CommonUtility.AssertNotNull("inputMessage", inputMessage);

            if (this.Key == null)
            {
                throw new InvalidOperationException(SR.KeyMissingError, null);
            }

            CloudQueueEncryptedMessage encryptedMessage = new CloudQueueEncryptedMessage();
            EncryptionData encryptionData = new EncryptionData();
            encryptionData.EncryptionAgent = new EncryptionAgent(Constants.EncryptionConstants.EncryptionProtocolV1, EncryptionAlgorithm.AES_CBC_256);
            encryptionData.KeyWrappingMetadata = new Dictionary<string, string>();
            encryptionData.KeyWrappingMetadata[Constants.EncryptionConstants.AgentMetadataKey] = Constants.EncryptionConstants.AgentMetadataValue;

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
            using (AesCryptoServiceProvider myAes = new AesCryptoServiceProvider())
#else
            using (AesManaged myAes = new AesManaged())
#endif
            {
                encryptionData.ContentEncryptionIV = myAes.IV;

                // Wrap always happens locally, irrespective of local or cloud key. So it is ok to call it synchronously.
                Tuple<byte[], string> wrappedKey = CommonUtility.RunWithoutSynchronizationContext(() => this.Key.WrapKeyAsync(myAes.Key, null /* algorithm */, CancellationToken.None).Result);
                encryptionData.WrappedContentKey = new WrappedKey(this.Key.Kid, wrappedKey.Item1, wrappedKey.Item2);

                using (ICryptoTransform encryptor = myAes.CreateEncryptor())
                {
                    encryptedMessage.EncryptedMessageContents = Convert.ToBase64String(encryptor.TransformFinalBlock(inputMessage, 0, inputMessage.Length));
                }

                encryptedMessage.EncryptionData = encryptionData;
                return JsonConvert.SerializeObject(encryptedMessage);
            }
        }

        /// <summary>
        /// Returns a plain text message given an encrypted message.
        /// </summary>
        /// <param name="inputMessage">The encrypted message.</param>
        /// <param name="requireEncryption">A value to indicate that the data read from the server should be encrypted.</param>        
        /// <returns>The plain text message bytes.</returns>
        internal byte[] DecryptMessage(string inputMessage, bool? requireEncryption)
        {
            CommonUtility.AssertNotNull("inputMessage", inputMessage);

            try
            {
                CloudQueueEncryptedMessage encryptedMessage = JsonConvert.DeserializeObject<CloudQueueEncryptedMessage>(inputMessage);

                if (requireEncryption.HasValue && requireEncryption.Value && encryptedMessage.EncryptionData == null)
                {
                    throw new StorageException(SR.EncryptionDataNotPresentError, null) { IsRetryable = false };
                }

                if (encryptedMessage.EncryptionData != null)
                {
                    EncryptionData encryptionData = encryptedMessage.EncryptionData;

                    CommonUtility.AssertNotNull("ContentEncryptionIV", encryptionData.ContentEncryptionIV);
                    CommonUtility.AssertNotNull("EncryptedKey", encryptionData.WrappedContentKey.EncryptedKey);

                    // Throw if the encryption protocol on the message doesn't match the version that this client library understands
                    // and is able to decrypt.
                    if (encryptionData.EncryptionAgent.Protocol != Constants.EncryptionConstants.EncryptionProtocolV1)
                    {
                        throw new StorageException(SR.EncryptionProtocolVersionInvalid, null) { IsRetryable = false };
                    }

                    // Throw if neither the key nor the key resolver are set.
                    if (this.Key == null && this.KeyResolver == null)
                    {
                        throw new StorageException(SR.KeyAndResolverMissingError, null) { IsRetryable = false };
                    }

                    byte[] contentEncryptionKey = null;

                    // 1. Invoke the key resolver if specified to get the key. If the resolver is specified but does not have a
                    // mapping for the key id, an error should be thrown. This is important for key rotation scenario.
                    // 2. If resolver is not specified but a key is specified, match the key id on the key and and use it.
                    // Calling UnwrapKeyAsync synchronously is fine because for the storage client scenario, unwrap happens
                    // locally. No service call is made.
                    if (this.KeyResolver != null)
                    {
                        IKey keyEncryptionKey = CommonUtility.RunWithoutSynchronizationContext(() => this.KeyResolver.ResolveKeyAsync(encryptionData.WrappedContentKey.KeyId, CancellationToken.None).Result);

                        CommonUtility.AssertNotNull("keyEncryptionKey", keyEncryptionKey);
                        contentEncryptionKey = CommonUtility.RunWithoutSynchronizationContext(() => keyEncryptionKey.UnwrapKeyAsync(encryptionData.WrappedContentKey.EncryptedKey, encryptionData.WrappedContentKey.Algorithm, CancellationToken.None).Result);
                    }
                    else
                    {
                        if (this.Key.Kid == encryptionData.WrappedContentKey.KeyId)
                        {
                            contentEncryptionKey = CommonUtility.RunWithoutSynchronizationContext(() => this.Key.UnwrapKeyAsync(encryptionData.WrappedContentKey.EncryptedKey, encryptionData.WrappedContentKey.Algorithm, CancellationToken.None).Result);
                        }
                        else
                        {
                            throw new StorageException(SR.KeyMismatch, null) { IsRetryable = false };
                        }
                    }

                    switch (encryptionData.EncryptionAgent.EncryptionAlgorithm)
                    {
                        case EncryptionAlgorithm.AES_CBC_256:
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
                            using (AesCryptoServiceProvider myAes = new AesCryptoServiceProvider())
#else
                        using (AesManaged myAes = new AesManaged())
#endif
                            {
                                myAes.Key = contentEncryptionKey;
                                myAes.IV = encryptionData.ContentEncryptionIV;

                                byte[] src = Convert.FromBase64String(encryptedMessage.EncryptedMessageContents);
                                using (ICryptoTransform decryptor = myAes.CreateDecryptor())
                                {
                                    return decryptor.TransformFinalBlock(src, 0, src.Length);
                                }
                            }

                        default:
                            throw new StorageException(SR.InvalidEncryptionAlgorithm, null) { IsRetryable = false };
                    }
                }
                else
                {
                    return Convert.FromBase64String(encryptedMessage.EncryptedMessageContents);
                }
            }
            catch (JsonException ex)
            {
                throw new StorageException(SR.EncryptedMessageDeserializingError, ex) { IsRetryable = false };
            }
            catch (StorageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new StorageException(SR.DecryptionLogicError, ex) { IsRetryable = false };
            }
        }
    }
}
